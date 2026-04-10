using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TranslatorCsV2.Ai;

public sealed class OpenAiClient : IDisposable
{
    private const string Host = "https://api.openai.com";
    private const string Endpoint = Host + "/v1/chat/completions";

    private readonly HttpClient _http;

    public string? LastError { get; private set; }

    public OpenAiClient()
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            EnableMultipleHttp2Connections = true,
            ConnectTimeout = TimeSpan.FromSeconds(6),
            AutomaticDecompression = DecompressionMethods.All,
        };

        _http = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30),
            DefaultRequestVersion = HttpVersion.Version20,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
        };
        _http.DefaultRequestHeaders.ExpectContinue = false;
        _http.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, br");
    }

    public async Task PreWarm()
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, Host);
            using var _ = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
        }
        catch { }
    }

    public async Task<bool> StreamTranslate(
        string apiKey, string model, string system, string user,
        Action<string> onToken, CancellationToken ct = default)
    {
        LastError = null;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            LastError = "No API key set. Open settings.";
            return false;
        }

        var payload = new
        {
            model,
            temperature = 0.2,
            stream = true,
            messages = new object[]
            {
                new { role = "system", content = system },
                new { role = "user",   content = user   },
            },
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        try
        {
            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                LastError = ExtractError(body) ?? $"HTTP {(int)resp.StatusCode}";
                return false;
            }

            await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var reader = new StreamReader(stream);

            while (true)
            {
                var line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
                if (line == null) break;
                if (line.Length == 0 || !line.StartsWith("data: ", StringComparison.Ordinal)) continue;

                var chunk = line.AsSpan(6);
                if (chunk.SequenceEqual("[DONE]")) break;

                var token = ExtractDelta(chunk);
                if (!string.IsNullOrEmpty(token)) onToken(token);
            }
            return true;
        }
        catch (TaskCanceledException)
        {
            LastError = "Timed out";
            return false;
        }
        catch (HttpRequestException ex)
        {
            LastError = ex.Message;
            return false;
        }
    }

    private static string? ExtractDelta(ReadOnlySpan<char> json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json.ToString());
            var choices = doc.RootElement.GetProperty("choices");
            if (choices.GetArrayLength() == 0) return null;
            if (!choices[0].TryGetProperty("delta", out var delta)) return null;
            if (!delta.TryGetProperty("content", out var content)) return null;
            return content.GetString();
        }
        catch (JsonException) { return null; }
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
