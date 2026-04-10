using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using CompositionTarget = System.Windows.Media.CompositionTarget;

namespace TranslatorCsV2.Overlay;

public partial class OverlayWindow : Window
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int VK_LBUTTON = 0x01;

    private static readonly Brush NormalText  = Frozen(Color.FromRgb(0xF2, 0xF3, 0xF5));
    private static readonly Brush BlockedText = Frozen(Color.FromRgb(0xFF, 0x5D, 0x5D));

    private static Brush Frozen(Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }

    private readonly DispatcherTimer _hide;
    private bool _lastMouseDown;

    public OverlayWindow()
    {
        InitializeComponent();
        _hide = new DispatcherTimer { Interval = TimeSpan.FromSeconds(12) };
        _hide.Tick += (_, _) => Hide();
        IsVisibleChanged += OnVisibleChanged;
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
        Body.Foreground = NormalText;
        Reveal();
    }

    public void ShowError(string err)
    {
        Caption.Text = "ERROR";
        Body.Text = err;
        Body.Foreground = NormalText;
        Reveal();
    }

    public void ShowBlocked()
    {
        Caption.Text = "BLOCKED";
        Body.Text = "This content may violate the AI's guidelines";
        Body.Foreground = BlockedText;
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

    private void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        CompositionTarget.Rendering -= OnFrame;
        if ((bool)e.NewValue)
        {
            _lastMouseDown = IsMouseDown();
            CompositionTarget.Rendering += OnFrame;
        }
    }

    private void OnFrame(object? sender, EventArgs e)
    {
        Anchor.PlaceAtCursor(this, ActualWidth, ActualHeight);

        bool down = IsMouseDown();
        if (down && !_lastMouseDown)
        {
            _hide.Stop();
            Hide();
            return;
        }
        _lastMouseDown = down;
    }

    private static bool IsMouseDown() => (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0;

    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int vKey);
}
