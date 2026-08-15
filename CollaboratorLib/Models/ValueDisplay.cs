using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace StoryCollaborator.Models;

/// <summary>
/// Human-readable display text for pending-update values and identifiers (#129).
/// Values reach the UI typed per WriteVia (see <see cref="PendingUpdate"/>); rendering
/// them with ToString leaks CLR type names like "System.Collections.Generic.List`1[...]".
/// </summary>
internal static class ValueDisplay
{
    /// <summary>
    /// Inserts spaces into a PascalCase identifier (StructureTitle → "Structure Title").
    /// Consecutive capitals stay together (GMCNotes → "GMC Notes").
    /// </summary>
    internal static string SplitPascalCase(string? identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return string.Empty;

        var sb = new StringBuilder(identifier.Length + 8);
        for (var i = 0; i < identifier.Length; i++)
        {
            var c = identifier[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(identifier[i - 1]))
                sb.Append(' ');
            else if (i > 0 && char.IsUpper(c) && i + 1 < identifier.Length && char.IsLower(identifier[i + 1])
                     && char.IsUpper(identifier[i - 1]))
                sb.Append(' ');
            sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Formats a pending-update value for display. Lists render one entry per line;
    /// element GUIDs resolve to names through <paramref name="resolveElementName"/>.
    /// </summary>
    internal static string Format(object? value, Func<Guid, string?>? resolveElementName = null)
    {
        switch (value)
        {
            case null:
                return string.Empty;

            case string s:
                return s;

            case List<string> entries:
                return Bullets(entries);

            case List<BeatInfo> beats:
                return string.Join("\n", beats.Select((beat, i) =>
                    string.IsNullOrWhiteSpace(beat.Description)
                        ? $"{i + 1}. {beat.Title}"
                        : $"{i + 1}. {beat.Title} — {beat.Description}"));

            case List<Guid> guids:
                return Bullets(guids.Select(g => ResolveName(g, resolveElementName)));

            case List<RelationshipInfo> relationships:
                return Bullets(relationships.Select(rel =>
                {
                    var name = ResolveName(rel.RecipientGuid, resolveElementName);
                    var type = rel.RelationType ?? string.Empty;
                    var notes = rel.Notes ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(notes))
                        return string.IsNullOrWhiteSpace(type) ? $"{name} — {notes}" : $"{name} ({type}) — {notes}";
                    return string.IsNullOrWhiteSpace(type) ? name : $"{name} — {type}";
                }));

            case List<JsonElement> jsonEntries:
                return Bullets(jsonEntries.Select(FormatJson));

            case IEnumerable enumerable:
                return Bullets(enumerable.Cast<object?>().Select(item => item?.ToString() ?? string.Empty));

            default:
                return value.ToString() ?? string.Empty;
        }
    }

    private static string Bullets(IEnumerable<string> entries) =>
        string.Join("\n", entries.Select(e => $"• {e}"));

    private static string ResolveName(Guid guid, Func<Guid, string?>? resolve)
    {
        var name = resolve?.Invoke(guid);
        return string.IsNullOrWhiteSpace(name) ? guid.ToString("D") : name;
    }

    private static string FormatJson(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            return string.Join("; ", element.EnumerateObject()
                .Select(prop => $"{SplitPascalCase(prop.Name)}: {prop.Value}"));
        }
        return element.ToString();
    }
}
