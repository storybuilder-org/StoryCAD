using System.Text.Json;
using System.Text.RegularExpressions;

namespace StoryCollaborator.Services;

/// <summary>
/// Collaborator #145: extract JSON patches from a chat model reply and a human-visible remainder.
/// </summary>
public static class ChatPatchParser
{
    private static readonly Regex FencedJson = new(
        @"```(?:json)?\s*(\{[\s\S]*?\})\s*```",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex LoosePatchesObject = new(
        @"\{\s*""patches""\s*:\s*\[[\s\S]*?\]\s*\}",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public readonly record struct Patch(string Key, string Value);

    /// <summary>
    /// Always returns true for non-null input; patches may be empty.
    /// </summary>
    public static bool TryParse(string? raw, out string displayText, out IReadOnlyList<Patch> patches)
    {
        displayText = raw?.Trim() ?? string.Empty;
        patches = Array.Empty<Patch>();
        if (string.IsNullOrWhiteSpace(raw))
            return true;

        var list = new List<Patch>();
        var working = raw;

        foreach (Match m in FencedJson.Matches(raw))
        {
            if (TryReadPatchesObject(m.Groups[1].Value, list))
                working = working.Replace(m.Value, "\n");
        }

        // Unfenced object containing "patches"
        foreach (Match m in LoosePatchesObject.Matches(working))
        {
            if (TryReadPatchesObject(m.Value, list))
                working = working.Replace(m.Value, "\n");
        }

        // Entire reply is the JSON object
        if (list.Count == 0 && raw.TrimStart().StartsWith('{'))
            TryReadPatchesObject(raw.Trim(), list);

        patches = list;
        // Collaborator #237 item 5: a reply that is only JSON leaves the display empty. The
        // caller says what changed from what it applied, never from what was parsed; a
        // parsed patch whose key matched nothing used to be reported as "Updated".
        displayText = CollapseBlankLines(working.Trim());
        return true;
    }

    /// <summary>
    /// Collaborator #237: a reply that names patches but yields none was a silent drop. The
    /// caller tells the writer the change could not be read instead of saying nothing.
    /// </summary>
    public static bool HasUnreadPatchBlock(string? raw, IReadOnlyList<Patch> parsed) =>
        parsed.Count == 0 &&
        !string.IsNullOrEmpty(raw) &&
        raw.Contains("\"patches\"", StringComparison.OrdinalIgnoreCase);

    private static readonly JsonDocumentOptions Lenient = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    private static bool TryReadPatchesObject(string json, List<Patch> into)
    {
        // Collaborator #237: models put raw line breaks inside JSON strings (the Concept
        // what-ifs are one per line). That is invalid JSON and used to drop the whole patch
        // set. Escape control characters inside strings and parse again before giving up.
        return TryReadPatchesObjectOnce(json, into) ||
               TryReadPatchesObjectOnce(EscapeControlCharsInsideStrings(json), into);
    }

    private static bool TryReadPatchesObjectOnce(string json, List<Patch> into)
    {
        try
        {
            using var doc = JsonDocument.Parse(json, Lenient);
            if (!doc.RootElement.TryGetProperty("patches", out var arr) ||
                arr.ValueKind != JsonValueKind.Array)
                return false;

            var any = false;
            foreach (var el in arr.EnumerateArray())
            {
                if (!el.TryGetProperty("key", out var k) || !el.TryGetProperty("value", out var v))
                    continue;
                var key = k.GetString();
                if (string.IsNullOrWhiteSpace(key))
                    continue;
                var value = v.ValueKind == JsonValueKind.String
                    ? v.GetString() ?? string.Empty
                    : v.ToString();
                into.Add(new Patch(key.Trim(), value));
                any = true;
            }
            return any;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string EscapeControlCharsInsideStrings(string json)
    {
        var sb = new System.Text.StringBuilder(json.Length + 16);
        var inString = false;
        var escaped = false;
        foreach (var c in json)
        {
            if (!inString)
            {
                if (c == '"') inString = true;
                sb.Append(c);
                continue;
            }
            if (escaped)
            {
                sb.Append(c);
                escaped = false;
                continue;
            }
            switch (c)
            {
                case '\\':
                    escaped = true;
                    sb.Append(c);
                    break;
                case '"':
                    inString = false;
                    sb.Append(c);
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

    private static string CollapseBlankLines(string s)
    {
        var lines = s.Replace("\r\n", "\n").Split('\n')
            .Select(l => l.TrimEnd())
            .ToList();
        var sb = new System.Text.StringBuilder();
        var blank = false;
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                if (!blank && sb.Length > 0)
                {
                    sb.AppendLine();
                    blank = true;
                }
                continue;
            }
            sb.AppendLine(line);
            blank = false;
        }
        return sb.ToString().Trim();
    }
}
