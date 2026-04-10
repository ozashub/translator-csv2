using System;
using System.Runtime.InteropServices;

namespace TranslatorCsV2.Overlay;

public static class Anchor
{
    public static void PlaceAtCursor(IntPtr hwnd, int offX = 18, int offY = 22)
    {
        if (hwnd == IntPtr.Zero) return;
        if (!GetCursorPos(out var cursor)) return;
        if (!GetWindowRect(hwnd, out var win)) return;

        int pw = win.right - win.left;
        int ph = win.bottom - win.top;
        if (pw <= 0 || ph <= 0) return;

        var hMon = MonitorFromPoint(cursor, MONITOR_DEFAULTTONEAREST);
        var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        GetMonitorInfo(hMon, ref mi);

        int x = cursor.x + offX;
        int y = cursor.y + offY;

        if (x + pw > mi.rcWork.right)  x = cursor.x - offX - pw;
        if (y + ph > mi.rcWork.bottom) y = cursor.y - offY - ph;
        if (x < mi.rcWork.left) x = mi.rcWork.left + 4;
        if (y < mi.rcWork.top)  y = mi.rcWork.top + 4;

        SetWindowPos(hwnd, IntPtr.Zero, x, y, 0, 0,
            SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_ASYNCWINDOWPOS);
    }

    private const int MONITOR_DEFAULTTONEAREST = 2;
    private const uint SWP_NOSIZE        = 0x0001;
    private const uint SWP_NOZORDER      = 0x0004;
    private const uint SWP_NOACTIVATE    = 0x0010;
    private const uint SWP_ASYNCWINDOWPOS = 0x4000;

    [DllImport("user32.dll")] private static extern bool GetCursorPos(out POINT lpPoint);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromPoint(POINT pt, int dwFlags);
    [DllImport("user32.dll")] private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int x, y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int left, top, right, bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }
}
