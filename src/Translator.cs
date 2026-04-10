using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Application = System.Windows.Application;
using TranslatorCsV2.Ai;
using TranslatorCsV2.Clipboard;
using TranslatorCsV2.Config;
using TranslatorCsV2.Input;
using TranslatorCsV2.Overlay;
using TranslatorCsV2.Tray;
using TranslatorCsV2.Ui;

namespace TranslatorCsV2;

public sealed class Translator : IDisposable
{
    private readonly AppConfig _config;
    private readonly GlobalHotkey _hotkey = new();
    private readonly OpenAiClient _ai = new();
    private readonly OverlayWindow _overlay = new();
    private readonly TrayIcon _tray = new();
    private readonly SemaphoreSlim _gate = new(1, 1);

    public Translator()
    {
        _config = ConfigStore.Load();
    }

    public void Start()
    {
        _hotkey.Install();
        _hotkey.OverlayPressed    += OnOverlayHotkey;
        _hotkey.TranslatorPressed += OnTranslatorHotkey;

        _tray.SettingsRequested += () => Application.Current.Dispatcher.Invoke(ShowSettings);
        _tray.ExitRequested     += () => Application.Current.Dispatcher.Invoke(Application.Current.Shutdown);

        ApplyConfig();

        if (string.IsNullOrWhiteSpace(_config.ApiKey) ||
            (string.IsNullOrWhiteSpace(_config.OverlayHotkey) && string.IsNullOrWhiteSpace(_config.TranslatorHotkey)))
            ShowSettings();
    }

    private void ApplyConfig()
    {
        var overlay = KeyCombo.Parse(_config.OverlayHotkey);
        var translator = KeyCombo.Parse(_config.TranslatorHotkey);
        _hotkey.SetOverlayCombo(overlay);
        _hotkey.SetTranslatorCombo(translator);
        _tray.SetHotkeyLabel($"overlay {overlay} · translator {translator}");
    }

    private void ShowSettings()
    {
        var win = new SettingsWindow(_config, _hotkey);
        win.ShowDialog();
        if (!win.Saved) return;

        ConfigStore.Save(_config);
        ApplyConfig();
    }

    private void OnOverlayHotkey() => Application.Current.Dispatcher.InvokeAsync(HandleOverlay);
    private void OnTranslatorHotkey() => Application.Current.Dispatcher.InvokeAsync(HandleTranslator);

    private async Task HandleOverlay()
    {
        if (!await _gate.WaitAsync(0)) return;

        try
        {
            _overlay.ShowLoading();

            var text = await Selection.CaptureAsync();
            if (string.IsNullOrWhiteSpace(text))
            {
                _overlay.Hide();
                return;
            }

            var system = Prompts.TranslateTo(_config.TargetLanguage);
            var temp = _config.TargetLanguage == "English" ? 0.2 : 0.3;
            var result = await _ai.Chat(_config.ApiKey, _config.Model, system, text, temp);

            if (result == null)
            {
                _overlay.ShowError(_ai.LastError ?? "Unknown error");
                return;
            }

            if (IsRefusal(result))
            {
                _overlay.ShowBlocked();
                return;
            }

            _overlay.ShowResult(result);
        }
        catch (Exception ex)
        {
            _overlay.ShowError(ex.Message);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task HandleTranslator()
    {
        if (!await _gate.WaitAsync(0)) return;

        try
        {
            _overlay.ShowLoading();

            var raw = await Selection.CaptureAsync(restoreClipboard: false);
            if (string.IsNullOrWhiteSpace(raw))
            {
                _overlay.Hide();
                return;
            }

            var (stripped, ops) = OpParser.Parse(raw);
            if (stripped == null || ops.Count == 0)
            {
                _overlay.Hide();
                return;
            }

            var cur = stripped;
            foreach (var op in ops)
            {
                var (system, temp) = SystemFor(op);
                var next = await _ai.Chat(_config.ApiKey, _config.Model, system, cur, temp);

                if (next == null)
                {
                    _overlay.ShowError(_ai.LastError ?? "Unknown error");
                    return;
                }

                if (IsRefusal(next))
                {
                    _overlay.ShowBlocked();
                    return;
                }

                cur = next;
            }

            await Selection.ReplaceAsync(cur);
            _overlay.Hide();
        }
        catch (Exception ex)
        {
            _overlay.ShowError(ex.Message);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static (string system, double temp) SystemFor(Op op) => op.Kind switch
    {
        OpKind.Improve     => (Prompts.Improve, 0.95),
        OpKind.Answer      => (Prompts.Answer, 0.4),
        OpKind.Deformalise => (Prompts.Deformalise, 0.8),
        OpKind.Prompt      => (Prompts.StructurePrompt, 0.4),
        OpKind.Translate   => (Prompts.TranslateTo(op.Lang ?? "English"),
                               op.Lang == "English" ? 0.2 : 0.3),
        _                  => (Prompts.Improve, 0.5),
    };

    private static bool IsRefusal(string text)
    {
        var t = text.Trim();
        if (t == "[ERROR]") return true;
        if (t.Length > 300) return false;

        var lower = t.ToLowerInvariant();
        return lower.StartsWith("i cannot")      || lower.StartsWith("i can't") ||
               lower.StartsWith("i'm sorry")     || lower.StartsWith("i am sorry") ||
               lower.StartsWith("sorry, i")      || lower.StartsWith("i'm unable") ||
               lower.StartsWith("i am unable")   || lower.StartsWith("i must decline") ||
               lower.StartsWith("as an ai")      || lower.StartsWith("i won't") ||
               lower.StartsWith("i will not");
    }

    public void Dispose()
    {
        _hotkey.Dispose();
        _ai.Dispose();
        _tray.Dispose();
        _gate.Dispose();
    }
}
