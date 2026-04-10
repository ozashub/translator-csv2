using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace TranslatorCsV2.Overlay;

public partial class OverlayWindow : Window
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;

    private readonly DispatcherTimer _hide;

    public OverlayWindow()
    {
        InitializeComponent();
        _hide = new DispatcherTimer { Interval = TimeSpan.FromSeconds(12) };
        _hide.Tick += (_, _) => Hide();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        int style = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, style | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);
    }

    public void ShowLoading()
    {
        Caption.Text = "TRANSLATING";
        Body.Text = "…";
        Reveal();
    }

    public void ShowResult(string text)
    {
        Caption.Text = "TRANSLATION";
        Body.Text = text;
        Reveal();
    }

    public void ShowError(string err)
    {
        Caption.Text = "ERROR";
        Body.Text = err;
        Reveal();
    }

    private void Reveal()
    {
        if (!IsVisible) Show();
        UpdateLayout();
        Anchor.PlaceAtCursor(this, ActualWidth, ActualHeight);
        _hide.Stop();
        _hide.Start();
    }

    private void OnClickDismiss(object sender, MouseButtonEventArgs e)
    {
        _hide.Stop();
        Hide();
    }

    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}
