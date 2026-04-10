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
        _hotkey.Pressed += OnHotkey;

        _tray.SettingsRequested += () => Application.Current.Dispatcher.Invoke(ShowSettings);
        _tray.ExitRequested += () => Application.Current.Dispatcher.Invoke(Application.Current.Shutdown);

        ApplyConfig();

        if (string.IsNullOrWhiteSpace(_config.ApiKey) || string.IsNullOrWhiteSpace(_config.Hotkey))
            ShowSettings();
    }

    private void ApplyConfig()
    {
        var combo = KeyCombo.Parse(_config.Hotkey);
        _hotkey.SetCombo(combo);
        _tray.SetHotkeyLabel(combo.ToString());
    }

    private void ShowSettings()
    {
        var win = new SettingsWindow(_config, _hotkey) { Owner = null };
        win.ShowDialog();
        if (!win.Saved) return;

        ConfigStore.Save(_config);
        ApplyConfig();
    }

    private void OnHotkey() => Application.Current.Dispatcher.InvokeAsync(Handle);

    private async Task Handle()
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

            var system = Languages.BuildPrompt(_config.TargetLanguage);
            var result = await _ai.Translate(_config.ApiKey, _config.Model, system, text);

            if (result != null) _overlay.ShowResult(result);
            else _overlay.ShowError(_ai.LastError ?? "Unknown error");
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

    public void Dispose()
    {
        _hotkey.Dispose();
        _ai.Dispose();
        _tray.Dispose();
        _gate.Dispose();
    }
}
