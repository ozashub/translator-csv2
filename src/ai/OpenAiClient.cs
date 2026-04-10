using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TranslatorCsV2.Ai;

public sealed class OpenAiClient : IDisposable
{
    private const string Endpoint = "https://api.openai.com/v1/chat/completions";

    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };

    public string? LastError { get; private set; }

    public async Task<string?> Chat(string apiKey, string model, string system, string user, double temperature = 0.2, CancellationToken ct = default)
    {
        LastError = null;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            LastError = "No API key set. Open settings.";
            return null;
        }

        var payload = new
        {
            model,
            temperature,
            messages = new object[]
            {
                new { role = "system", content = system },
                new { role = "system", content = Prompts.RefusalFallback },
                new { role = "user",   content = user   },
            },
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        try
        {
            using var resp = await _http.SendAsync(req, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                LastError = ExtractError(body) ?? $"HTTP {(int)resp.StatusCode}";
                return null;
            }

            return ExtractContent(body);
        }
        catch (TaskCanceledException)
        {
            LastError = "Timed out";
            return null;
        }
        catch (HttpRequestException ex)
        {
            LastError = ex.Message;
            return null;
        }
    }

    private static string? ExtractContent(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var choices = doc.RootElement.GetProperty("choices");
        if (choices.GetArrayLength() == 0) return null;
        return choices[0].GetProperty("message").GetProperty("content").GetString()?.Trim();
    }

    private static string? ExtractError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var err) &&
                err.TryGetProperty("message", out var msg))
                return msg.GetString();
        }
        catch { }
        return null;
    }

    public void Dispose() => _http.Dispose();
}
