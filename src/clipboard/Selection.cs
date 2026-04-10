using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

namespace TranslatorCsV2.Clipboard;

public static class Selection
{
    private const ushort VK_CONTROL = 0x11;
    private const ushort VK_C = 0x43;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    public static async Task<string?> CaptureAsync()
    {
        var saved = TryReadClipboardText();
        var seq = GetClipboardSequenceNumber();

        SendCtrlC();

        var text = await WaitForClipboardChange(seq, 400);

        if (saved != null)
            TryWriteClipboardText(saved);
        else
            TryClearClipboard();

        return text;
    }

    private static async Task<string?> WaitForClipboardChange(uint before, int timeoutMs)
    {
        var start = Environment.TickCount;
        while (Environment.TickCount - start < timeoutMs)
        {
            if (GetClipboardSequenceNumber() != before)
                return TryReadClipboardText();
            await Task.Delay(8);
        }
        return null;
    }

    private static void SendCtrlC()
    {
        var seq = new[]
        {
            MakeKey(VK_CONTROL, false),
            MakeKey(VK_C, false),
            MakeKey(VK_C, true),
            MakeKey(VK_CONTROL, true),
        };

        SendInput((uint)seq.Length, seq, Marshal.SizeOf<INPUT>());
    }

    private static INPUT MakeKey(ushort vk, bool up) => new()
    {
        type = 1,
        U = new InputUnion
        {
            ki = new KEYBDINPUT
            {
                wVk = vk,
                wScan = 0,
                dwFlags = up ? KEYEVENTF_KEYUP : 0,
                time = 0,
                dwExtraInfo = IntPtr.Zero,
            }
        }
    };

    private static string? TryReadClipboardText()
    {
        for (int i = 0; i < 5; i++)
        {
            try
            {
                if (System.Windows.Clipboard.ContainsText())
                    return System.Windows.Clipboard.GetText();
                return null;
            }
            catch (COMException)
            {
                System.Threading.Thread.Sleep(10);
            }
        }
        return null;
    }

    private static void TryWriteClipboardText(string text)
    {
        for (int i = 0; i < 5; i++)
        {
            try
            {
                System.Windows.Clipboard.SetText(text);
                return;
            }
            catch (COMException)
            {
                System.Threading.Thread.Sleep(10);
            }
        }
    }

    private static void TryClearClipboard()
    {
        try { System.Windows.Clipboard.Clear(); }
        catch (COMException) { }
    }

    [DllImport("user32.dll")] private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
    [DllImport("user32.dll")] private static extern uint GetClipboardSequenceNumber();

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT { public uint type; public InputUnion U; }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx, dy;
        public uint mouseData, dwFlags, time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL, wParamH;
    }
}
