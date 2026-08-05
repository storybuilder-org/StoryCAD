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
        displayText = CollapseBlankLines(working.Trim());
        if (string.IsNullOrWhiteSpace(displayText) && list.Count > 0)
            displayText = list.Count == 1
                ? $"Updated {list[0].Key}."
                : $"Updated {list.Count} proposals.";
        return true;
    }

    private static bool TryReadPatchesObject(string json, List<Patch> into)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
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
