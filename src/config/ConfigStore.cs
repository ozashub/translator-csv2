using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TranslatorCsV2.Config;

public static class ConfigStore
{
    private static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TranslatorCsV2");

    private static readonly string File = Path.Combine(Dir, "config.json");

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static AppConfig Load()
    {
        if (!System.IO.File.Exists(File))
            return new AppConfig();

        try
        {
            var raw = System.IO.File.ReadAllText(File);
            var cfg = JsonSerializer.Deserialize<AppConfig>(raw) ?? new AppConfig();
            cfg.ApiKey = Decrypt(cfg.ApiKey);
            return cfg;
        }
        catch
        {
            return new AppConfig();
        }
    }

    public static void Save(AppConfig cfg)
    {
        Directory.CreateDirectory(Dir);
        var copy = new AppConfig
        {
            ApiKey = Encrypt(cfg.ApiKey),
            Model = cfg.Model,
            Hotkey = cfg.Hotkey,
            SourceLanguage = cfg.SourceLanguage,
            TargetLanguage = cfg.TargetLanguage,
        };
        System.IO.File.WriteAllText(File, JsonSerializer.Serialize(copy, Json));
    }

    private static string Encrypt(string plain)
    {
        if (string.IsNullOrEmpty(plain)) return "";
        var bytes = Encoding.UTF8.GetBytes(plain);
        var enc = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(enc);
    }

    private static string Decrypt(string cipher)
    {
        if (string.IsNullOrEmpty(cipher)) return "";
        try
        {
            var bytes = Convert.FromBase64String(cipher);
            var dec = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(dec);
        }
        catch
        {
            return "";
        }
    }
}
