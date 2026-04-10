using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace TranslatorCsV2.Overlay;

public static class Anchor
{
    public static void PlaceAtCursor(Window w, double dipW, double dipH, int offX = 18, int offY = 22)
    {
        if (!GetCursorPos(out var cursor)) return;

        var hMon = MonitorFromPoint(cursor, MONITOR_DEFAULTTONEAREST);
        GetDpiForMonitor(hMon, 0, out uint dpiX, out uint dpiY);
        double sx = dpiX / 96.0;
        double sy = dpiY / 96.0;

        int pw = (int)Math.Ceiling(dipW * sx);
        int ph = (int)Math.Ceiling(dipH * sy);

        var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        GetMonitorInfo(hMon, ref mi);

        int x = cursor.x + offX;
        int y = cursor.y + offY;

        if (x + pw > mi.rcWork.right)  x = cursor.x - offX - pw;
        if (y + ph > mi.rcWork.bottom) y = cursor.y - offY - ph;
        if (x < mi.rcWork.left) x = mi.rcWork.left + 4;
        if (y < mi.rcWork.top)  y = mi.rcWork.top + 4;

        var hwnd = new WindowInteropHelper(w).Handle;
        if (hwnd == IntPtr.Zero) return;

        SetWindowPos(hwnd, IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
    }

    private const int MONITOR_DEFAULTTONEAREST = 2;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;

    [DllImport("user32.dll")] private static extern bool GetCursorPos(out POINT lpPoint);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromPoint(POINT pt, int dwFlags);
    [DllImport("user32.dll")] private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("shcore.dll")] private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

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
