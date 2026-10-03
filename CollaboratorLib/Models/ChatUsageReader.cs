using System.Reflection;

namespace StoryCollaborator.Models
{
    /// <summary>
    /// Pulls token counts out of the Metadata bag Semantic Kernel hangs off
    /// ChatMessageContent. The chat sidebar calls /v1/chat/completions non-streaming, and
    /// that route returns no cost to the client (proxy/src/index.js:4665) — but the raw
    /// upstream body carries usage, and SK's OpenAI connector surfaces it under "Usage".
    /// Tokens are therefore reachable client-side; dollars are not, because MODEL_MAP
    /// pricing lives only in the Worker.
    ///
    /// Read by property name rather than against OpenAI.Chat.ChatTokenUsage directly: that
    /// type arrives transitively through Semantic Kernel and its shape has changed across
    /// SDK versions (InputTokenCount/OutputTokenCount today, PromptTokens/CompletionTokens
    /// before). A diagnostic line is not worth pinning a transitive package version, and an
    /// unrecognised shape must hide the line rather than throw inside a chat turn.
    /// </summary>
    internal static class ChatUsageReader
    {
        private const string UsageKey = "Usage";

        /// <summary>Property-name pairs to try, newest SDK shape first.</summary>
        private static readonly (string Input, string Output)[] KnownShapes =
        {
            ("InputTokenCount", "OutputTokenCount"),
            ("PromptTokens", "CompletionTokens"),
        };

        /// <summary>
        /// Reads input and output token counts, or returns false if the metadata does not
        /// carry a usage object in a shape this knows. Both counts must be present: a
        /// half-read line would misreport the turn.
        /// </summary>
        internal static bool TryRead(
            IReadOnlyDictionary<string, object?>? metadata,
            out int inputTokens,
            out int outputTokens)
        {
            inputTokens = 0;
            outputTokens = 0;

            if (metadata == null ||
                !metadata.TryGetValue(UsageKey, out var usage) ||
                usage == null)
            {
                return false;
            }

            var type = usage.GetType();
            foreach (var (inputName, outputName) in KnownShapes)
            {
                if (TryReadInt(type, usage, inputName, out var input) &&
                    TryReadInt(type, usage, outputName, out var output))
                {
                    inputTokens = input;
                    outputTokens = output;
                    return true;
                }
            }

            return false;
        }

        private static bool TryReadInt(Type type, object instance, string propertyName, out int value)
        {
            value = 0;
            var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
                return false;

            try
            {
                var raw = property.GetValue(instance);
                if (raw == null)
                    return false;

                value = Convert.ToInt32(raw);
                return true;
            }
            catch (Exception)
            {
                // Non-numeric or inaccessible property on an unexpected shape: treat as absent.
                return false;
            }
        }
    }
}
