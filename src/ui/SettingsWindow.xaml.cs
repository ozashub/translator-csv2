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
    private KeyCombo _combo;
    private CancellationTokenSource? _captureCts;

    public bool Saved { get; private set; }

    public SettingsWindow(AppConfig config, GlobalHotkey hotkey)
    {
        InitializeComponent();

        _config = config;
        _hotkey = hotkey;
        _combo = KeyCombo.Parse(config.Hotkey);

        ApiKeyBox.Password = config.ApiKey;
        HotkeyBox.Text = _combo.IsEmpty ? "click to set" : _combo.ToString();

        TargetBox.ItemsSource = Languages.Targets;
        TargetBox.SelectedItem = config.TargetLanguage;
        if (TargetBox.SelectedIndex < 0) TargetBox.SelectedIndex = 0;
    }

    private async void OnHotkeyFocus(object sender, RoutedEventArgs e)
    {
        HotkeyBox.Text = "press any key…";
        _captureCts = new CancellationTokenSource();

        try
        {
            var c = await _hotkey.CaptureAsync(_captureCts.Token);
            _combo = c;
            HotkeyBox.Text = c.ToString();
        }
        catch (OperationCanceledException)
        {
            HotkeyBox.Text = _combo.IsEmpty ? "click to set" : _combo.ToString();
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
        _config.Hotkey = _combo.Serialize();
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
