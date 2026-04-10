namespace TranslatorCsV2.Ai;

public static class Languages
{
    public static readonly string[] Targets =
    {
        "English", "Spanish", "French", "German", "Italian", "Portuguese",
        "Dutch", "Polish", "Czech", "Russian", "Ukrainian", "Turkish",
        "Arabic", "Hebrew", "Greek", "Swedish", "Norwegian", "Danish",
        "Finnish", "Romanian", "Hungarian", "Japanese", "Korean",
        "Chinese (Simplified)", "Chinese (Traditional)", "Vietnamese",
        "Thai", "Indonesian", "Hindi",
    };

    public static string BuildPrompt(string target) =>
        $"You are a translator. Detect the source language of the user's text and translate it into {target}.\n" +
        "Output only the translation. No commentary, no quotes, no labels, no source language name.\n" +
        $"Preserve tone, punctuation, and line breaks. If the text is already in {target}, return it unchanged.";
}
