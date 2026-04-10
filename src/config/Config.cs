namespace TranslatorCsV2.Config;

public sealed class AppConfig
{
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "gpt-4o-mini";
    public string OverlayHotkey { get; set; } = "";
    public string TranslatorHotkey { get; set; } = "";
    public string TargetLanguage { get; set; } = "English";
}
