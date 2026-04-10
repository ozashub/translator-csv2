using System;
using System.Collections.Generic;
using System.Linq;

namespace TranslatorCsV2.Ai;

public enum OpKind { Improve, Answer, Deformalise, Translate, Prompt }

public sealed record Op(OpKind Kind, string? Suffix, string? Lang);

public static class OpParser
{
    private static readonly Dictionary<string, string> Langs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["-en"]  = "English",
        ["-es"]  = "Spanish",
        ["-fr"]  = "French",
        ["-de"]  = "German",
        ["-it"]  = "Italian",
        ["-pt"]  = "Portuguese",
        ["-ru"]  = "Russian",
        ["-ja"]  = "Japanese",
        ["-ko"]  = "Korean",
        ["-zh"]  = "Chinese",
        ["-nl"]  = "Dutch",
        ["-sv"]  = "Swedish",
        ["-no"]  = "Norwegian",
        ["-pl"]  = "Polish",
        ["-uk"]  = "Ukrainian",
        ["-tr"]  = "Turkish",
        ["-ar"]  = "Arabic",
        ["-he"]  = "Hebrew",
        ["-el"]  = "Greek",
        ["-da"]  = "Danish",
        ["-fi"]  = "Finnish",
        ["-ro"]  = "Romanian",
        ["-hu"]  = "Hungarian",
        ["-vi"]  = "Vietnamese",
        ["-th"]  = "Thai",
        ["-id"]  = "Indonesian",
        ["-hi"]  = "Hindi",
        ["-cs"]  = "Czech",
        ["-jam"] = "Jamaican Patois",
    };

    public static (string? text, List<Op> ops) Parse(string text)
    {
        var ops = new List<Op>();

        while (true)
        {
            var low = text.ToLowerInvariant();
            bool hit = false;

            if (low.EndsWith("--prompt"))
            {
                ops.Insert(0, new Op(OpKind.Prompt, "--prompt", null));
                text = text[..^8].Trim();
                hit = true;
            }
            else if (low.EndsWith("-df"))
            {
                ops.Insert(0, new Op(OpKind.Deformalise, "-df", null));
                text = text[..^3].Trim();
                hit = true;
            }
            else if (low.EndsWith("-r"))
            {
                ops.Insert(0, new Op(OpKind.Answer, "-r", null));
                text = text[..^2].Trim();
                hit = true;
            }
            else
            {
                foreach (var (sfx, lang) in Langs)
                {
                    if (!low.EndsWith(sfx)) continue;
                    ops.Insert(0, new Op(OpKind.Translate, sfx, lang));
                    text = text[..^sfx.Length].Trim();
                    hit = true;
                    break;
                }
            }

            if (!hit) break;
        }

        if (string.IsNullOrWhiteSpace(text) && ops.Count > 0)
            return (null, new List<Op>());

        if (ops.Count == 0)
            ops.Add(new Op(OpKind.Improve, null, null));

        return (text, ops);
    }

    public static string Describe(List<Op> ops) =>
        string.Join(" \u2192 ", ops.Select(o => o.Kind switch
        {
            OpKind.Translate => o.Lang ?? "Translate",
            _ => o.Kind.ToString(),
        }));
}
