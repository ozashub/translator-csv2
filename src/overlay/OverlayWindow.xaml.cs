using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using CompositionTarget = System.Windows.Media.CompositionTarget;

namespace TranslatorCsV2.Overlay;

public partial class OverlayWindow : Window
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int VK_LBUTTON = 0x01;

    private readonly DispatcherTimer _hide;
    private bool _lastMouseDown;
    private long _shownAtTicks;
    private bool _hwndShown;
    private bool _revealed;

    public OverlayWindow()
    {
        InitializeComponent();
        _hide = new DispatcherTimer { Interval = TimeSpan.FromSeconds(12) };
        _hide.Tick += (_, _) => Conceal();
        Opacity = 0;
        new WindowInteropHelper(this).EnsureHandle();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        int style = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, style | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_TRANSPARENT);
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

    public new void Hide() => Conceal();

    private void Reveal()
    {
        UpdateLayout();
        Anchor.PlaceAtCursor(this, ActualWidth, ActualHeight);

        if (!_hwndShown)
        {
            _hwndShown = true;
            base.Show();
        }

        if (!_revealed)
        {
            _revealed = true;
            CompositionTarget.Rendering -= OnFrame;
            CompositionTarget.Rendering += OnFrame;
            Dispatcher.Invoke(() => { }, DispatcherPriority.Render);
            Opacity = 1;
        }

        _shownAtTicks = Environment.TickCount64;
        _lastMouseDown = IsMouseDown();
        _hide.Stop();
        _hide.Start();
    }

    private void Conceal()
    {
        if (!_revealed) return;
        _revealed = false;
        CompositionTarget.Rendering -= OnFrame;
        Opacity = 0;
        _hide.Stop();
    }

    private void OnFrame(object? sender, EventArgs e)
    {
        Anchor.PlaceAtCursor(this, ActualWidth, ActualHeight);

        bool down = IsMouseDown();
        if (Environment.TickCount64 - _shownAtTicks < 180)
        {
            _lastMouseDown = down;
            return;
        }

        if (down && !_lastMouseDown)
        {
            Conceal();
            return;
        }
        _lastMouseDown = down;
    }

    private static bool IsMouseDown() => (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0;

    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int vKey);
}
