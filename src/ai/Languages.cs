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

    public static readonly string[] Sources = Build();

    private static string[] Build()
    {
        var list = new string[Targets.Length + 1];
        list[0] = "Auto";
        System.Array.Copy(Targets, 0, list, 1, Targets.Length);
        return list;
    }

    public static string BuildPrompt(string source, string target)
    {
        var from = source == "Auto" ? "the source language" : source;
        return
            $"You are a translator. Translate the user's text from {from} into {target}.\n" +
            "Output only the translation. No commentary, no quotes, no labels.\n" +
            "Preserve tone, punctuation, and line breaks. If the text is already in " +
            $"{target}, return it unchanged.";
    }
}
