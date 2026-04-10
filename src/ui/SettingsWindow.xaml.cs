using System;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using TranslatorCsV2.Ai;
using TranslatorCsV2.Config;
using TranslatorCsV2.Input;

namespace TranslatorCsV2.Ui;

public partial class SettingsWindow : Window
{
    private readonly GlobalHotkey _hotkey;
    private readonly AppConfig _config;
    private KeyCombo _overlayCombo;
    private KeyCombo _translatorCombo;
    private CancellationTokenSource? _captureCts;

    public bool Saved { get; private set; }

    public SettingsWindow(AppConfig config, GlobalHotkey hotkey)
    {
        InitializeComponent();

        _config = config;
        _hotkey = hotkey;
        _overlayCombo = KeyCombo.Parse(config.OverlayHotkey);
        _translatorCombo = KeyCombo.Parse(config.TranslatorHotkey);

        ApiKeyBox.Password = config.ApiKey;
        OverlayHotkeyBox.Text    = _overlayCombo.IsEmpty    ? "click to set" : _overlayCombo.ToString();
        TranslatorHotkeyBox.Text = _translatorCombo.IsEmpty ? "click to set" : _translatorCombo.ToString();

        TargetBox.ItemsSource = Languages.Targets;
        TargetBox.SelectedItem = config.TargetLanguage;
        if (TargetBox.SelectedIndex < 0) TargetBox.SelectedIndex = 0;
    }

    private async void OnOverlayHotkeyFocus(object sender, RoutedEventArgs e) =>
        await Capture(OverlayHotkeyBox, c => _overlayCombo = c, () => _overlayCombo);

    private async void OnTranslatorHotkeyFocus(object sender, RoutedEventArgs e) =>
        await Capture(TranslatorHotkeyBox, c => _translatorCombo = c, () => _translatorCombo);

    private async System.Threading.Tasks.Task Capture(
        System.Windows.Controls.TextBox box, Action<KeyCombo> setter, Func<KeyCombo> getter)
    {
        box.Text = "press any key…";
        _captureCts = new CancellationTokenSource();

        try
        {
            var c = await _hotkey.CaptureAsync(_captureCts.Token);
            setter(c);
            box.Text = c.ToString();
        }
        catch (OperationCanceledException)
        {
            var cur = getter();
            box.Text = cur.IsEmpty ? "click to set" : cur.ToString();
        }
    }

    private void OnHotkeyBlur(object sender, RoutedEventArgs e)
    {
        _captureCts?.Cancel();
        _hotkey.CancelCapture();
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        _config.ApiKey = ApiKeyBox.Password;
        _config.OverlayHotkey    = _overlayCombo.Serialize();
        _config.TranslatorHotkey = _translatorCombo.Serialize();
        _config.TargetLanguage = (string)TargetBox.SelectedItem;
        Saved = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();

    private void OnDragTitle(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }
}
