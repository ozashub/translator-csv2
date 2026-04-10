using System;
using System.Runtime.InteropServices;
using System.Text;

namespace TranslatorCsV2.Input;

public readonly record struct KeyCombo(bool Ctrl, bool Shift, bool Alt, bool Win, uint ScanCode, uint VirtualKey)
{
    public bool IsEmpty => ScanCode == 0;

    public override string ToString()
    {
        if (IsEmpty) return "(none)";
        var sb = new StringBuilder();
        if (Ctrl) sb.Append("Ctrl + ");
        if (Shift) sb.Append("Shift + ");
        if (Alt) sb.Append("Alt + ");
        if (Win) sb.Append("Win + ");
        sb.Append(KeyLabel(VirtualKey));
        return sb.ToString();
    }

    public string Serialize() => IsEmpty ? "" : $"{(Ctrl ? 1 : 0)}|{(Shift ? 1 : 0)}|{(Alt ? 1 : 0)}|{(Win ? 1 : 0)}|{ScanCode}|{VirtualKey}";

    public static KeyCombo Parse(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return default;
        var p = s.Split('|');
        if (p.Length < 6) return default;
        return new KeyCombo(p[0] == "1", p[1] == "1", p[2] == "1", p[3] == "1", uint.Parse(p[4]), uint.Parse(p[5]));
    }

    private static string KeyLabel(uint vk)
    {
        if (vk >= 'A' && vk <= 'Z') return ((char)vk).ToString();
        if (vk >= '0' && vk <= '9') return ((char)vk).ToString();
        if (vk >= 0x70 && vk <= 0x87) return "F" + (vk - 0x6F);

        return vk switch
        {
            0x20 => "Space",
            0x0D => "Enter",
            0x09 => "Tab",
            0x08 => "Backspace",
            0x1B => "Escape",
            0x2D => "Insert",
            0x2E => "Delete",
            0x24 => "Home",
            0x23 => "End",
            0x21 => "PageUp",
            0x22 => "PageDown",
            0x25 => "Left",
            0x26 => "Up",
            0x27 => "Right",
            0x28 => "Down",
            0xBA => ";",
            0xBB => "=",
            0xBC => ",",
            0xBD => "-",
            0xBE => ".",
            0xBF => "/",
            0xC0 => "`",
            0xDB => "[",
            0xDC => "\\",
            0xDD => "]",
            0xDE => "'",
            _ => FromScan(vk),
        };
    }

    [DllImport("user32.dll")] private static extern int GetKeyNameTextW(int lParam, StringBuilder lpString, int cchSize);
    [DllImport("user32.dll")] private static extern uint MapVirtualKeyW(uint uCode, uint uMapType);

    private static string FromScan(uint vk)
    {
        var scan = MapVirtualKeyW(vk, 0);
        var buf = new StringBuilder(32);
        int lp = (int)(scan << 16);
        return GetKeyNameTextW(lp, buf, buf.Capacity) > 0 ? buf.ToString() : $"VK{vk:X2}";
    }
}
