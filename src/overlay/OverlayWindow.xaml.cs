using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace TranslatorCsV2.Overlay;

public partial class OverlayWindow : Window
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int VK_LBUTTON = 0x01;

    private readonly DispatcherTimer _hide;
    private IntPtr _hwnd;
    private CancellationTokenSource? _followCts;
    private bool _timerResRaised;

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
        _hwnd = new WindowInteropHelper(this).Handle;
        int style = GetWindowLong(_hwnd, GWL_EXSTYLE);
        SetWindowLong(_hwnd, GWL_EXSTYLE, style | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_TRANSPARENT);
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

    public void StartResult()
    {
        Caption.Text = "TRANSLATION";
        Body.Text = "";
        Reveal();
    }

    public void AppendResult(string token)
    {
        Body.Text += token;
        _hide.Stop();
        _hide.Start();
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
        Anchor.PlaceAtCursor(_hwnd);
        _hide.Stop();
        _hide.Start();
    }

    private void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue) StartFollow();
        else StopFollow();
    }

    private void StartFollow()
    {
        if (_followCts != null) return;
        _followCts = new CancellationTokenSource();
        var ct = _followCts.Token;

        if (!_timerResRaised)
        {
            timeBeginPeriod(1);
            _timerResRaised = true;
        }

        var hwnd = _hwnd;
        bool lastDown = (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0;

        new Thread(() =>
        {
            while (!ct.IsCancellationRequested)
            {
                Anchor.PlaceAtCursor(hwnd);

                bool down = (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0;
                if (down && !lastDown)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        _hide.Stop();
                        Hide();
                    }));
                    return;
                }
                lastDown = down;

                Thread.Sleep(1);
            }
        })
        { IsBackground = true, Name = "OverlayFollow" }.Start();
    }

    private void StopFollow()
    {
        var cts = _followCts;
        if (cts == null) return;
        _followCts = null;
        cts.Cancel();

        if (_timerResRaised)
        {
            timeEndPeriod(1);
            _timerResRaised = false;
        }
    }

    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int vKey);

    [DllImport("winmm.dll")] private static extern uint timeBeginPeriod(uint uPeriod);
    [DllImport("winmm.dll")] private static extern uint timeEndPeriod(uint uPeriod);
}
