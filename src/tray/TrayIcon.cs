using System;
using System.Drawing;
using System.Windows.Forms;

namespace TranslatorCsV2.Tray;

public sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly ToolStripMenuItem _hotkeyLabel;

    public event Action? SettingsRequested;
    public event Action? ExitRequested;

    public TrayIcon()
    {
        var menu = new ContextMenuStrip();

        _hotkeyLabel = new ToolStripMenuItem("Hotkey: (none)") { Enabled = false };
        menu.Items.Add(_hotkeyLabel);
        menu.Items.Add(new ToolStripSeparator());

        var settings = new ToolStripMenuItem("Settings");
        settings.Click += (_, _) => SettingsRequested?.Invoke();
        menu.Items.Add(settings);

        var exit = new ToolStripMenuItem("Quit");
        exit.Click += (_, _) => ExitRequested?.Invoke();
        menu.Items.Add(exit);

        _icon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "Translator",
            Visible = true,
            ContextMenuStrip = menu,
        };
        _icon.DoubleClick += (_, _) => SettingsRequested?.Invoke();
    }

    public void SetHotkeyLabel(string label)
    {
        _hotkeyLabel.Text = $"Hotkey: {label}";
        _icon.Text = $"Translator  —  {label}";
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
