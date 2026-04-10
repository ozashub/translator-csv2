using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace TranslatorCsV2.Input;

public sealed class GlobalHotkey : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;

    private const int VK_LCTRL = 0xA2, VK_RCTRL = 0xA3;
    private const int VK_LSHIFT = 0xA0, VK_RSHIFT = 0xA1;
    private const int VK_LALT = 0xA4, VK_RALT = 0xA5;
    private const int VK_LWIN = 0x5B, VK_RWIN = 0x5C;

    private readonly LowLevelKeyboardProc _proc;
    private IntPtr _hook = IntPtr.Zero;
    private Thread? _thread;
    private uint _threadId;

    private KeyCombo _combo;
    private TaskCompletionSource<KeyCombo>? _capture;

    public event Action? Pressed;

    public GlobalHotkey()
    {
        _proc = Callback;
    }

    public void Install()
    {
        if (_thread != null) return;

        var ready = new ManualResetEventSlim();
        _thread = new Thread(() =>
        {
            _threadId = GetCurrentThreadId();
            _hook = SetWindowsHookExW(WH_KEYBOARD_LL, _proc, GetModuleHandleW(null), 0);
            ready.Set();

            while (GetMessageW(out var msg, IntPtr.Zero, 0, 0) > 0)
            {
                TranslateMessage(ref msg);
                DispatchMessageW(ref msg);
            }
        })
        { IsBackground = true, Name = "Hotkey" };
        _thread.Start();
        ready.Wait();
    }

    public void SetCombo(KeyCombo c) => _combo = c;

    public Task<KeyCombo> CaptureAsync(CancellationToken ct = default)
    {
        _capture = new TaskCompletionSource<KeyCombo>(TaskCreationOptions.RunContinuationsAsynchronously);
        ct.Register(() => _capture?.TrySetCanceled());
        return _capture.Task;
    }

    public void CancelCapture() => _capture?.TrySetCanceled();

    private IntPtr Callback(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code < 0) return CallNextHookEx(_hook, code, wParam, lParam);

        int msg = wParam.ToInt32();
        if (msg != WM_KEYDOWN && msg != WM_SYSKEYDOWN)
            return CallNextHookEx(_hook, code, wParam, lParam);

        var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
        uint vk = data.vkCode;
        uint scan = data.scanCode;

        if (IsModifier(vk))
            return CallNextHookEx(_hook, code, wParam, lParam);

        bool ctrl = Down(VK_LCTRL) || Down(VK_RCTRL);
        bool shift = Down(VK_LSHIFT) || Down(VK_RSHIFT);
        bool alt = Down(VK_LALT) || Down(VK_RALT);
        bool win = Down(VK_LWIN) || Down(VK_RWIN);

        var cap = _capture;
        if (cap != null)
        {
            _capture = null;
            if (vk == 0x1B) cap.TrySetCanceled();
            else cap.TrySetResult(new KeyCombo(ctrl, shift, alt, win, scan, vk));
            return (IntPtr)1;
        }

        if (!_combo.IsEmpty &&
            ctrl == _combo.Ctrl &&
            shift == _combo.Shift &&
            alt == _combo.Alt &&
            win == _combo.Win &&
            scan == _combo.ScanCode)
        {
            ThreadPool.QueueUserWorkItem(_ => Pressed?.Invoke());
            return (IntPtr)1;
        }

        return CallNextHookEx(_hook, code, wParam, lParam);
    }

    private static bool IsModifier(uint vk) =>
        vk is >= 0xA0 and <= 0xA5 or 0x5B or 0x5C or 0x10 or 0x11 or 0x12;

    private static bool Down(int vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;

    public void Dispose()
    {
        if (_hook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }
        if (_threadId != 0)
            PostThreadMessageW(_threadId, 0x0012, IntPtr.Zero, IntPtr.Zero);
        _thread?.Join(200);
        _thread = null;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd; public uint message; public IntPtr wParam; public IntPtr lParam;
        public uint time; public int x; public int y;
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SetWindowsHookExW(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
    [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")] private static extern int GetMessageW(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);
    [DllImport("user32.dll")] private static extern bool TranslateMessage(ref MSG lpMsg);
    [DllImport("user32.dll")] private static extern IntPtr DispatchMessageW(ref MSG lpMsg);
    [DllImport("user32.dll")] private static extern bool PostThreadMessageW(uint idThread, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandleW(string? lpModuleName);
}
