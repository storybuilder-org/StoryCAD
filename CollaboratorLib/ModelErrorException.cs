using System.Text.Json;

namespace StoryCollaborator
{
    /// <summary>
    /// Collaborator #300: the Worker could not get an answer from the AI model. The Worker
    /// sends its own message (502 or 503 JSON body, or an <c>error</c> event on a stream that
    /// was already open), and the writer sees that message as-is: it says whether to try
    /// again and that nothing was charged. Before #300 the writer saw
    /// "Response status code does not indicate success: 502 (Bad Gateway)."
    /// </summary>
    public sealed class ModelErrorException : Exception
    {
        /// <summary>The Worker's error codes for a model failure.</summary>
        internal static readonly string[] WorkerCodes = { "model_busy", "upstream_error" };

        public ModelErrorException(string message, string code) : base(message)
        {
            Code = code;
        }

        /// <summary><c>model_busy</c> or <c>upstream_error</c>.</summary>
        public string Code { get; }

        /// <summary>
        /// Reads <c>{"error":{"code":"model_busy","message":"..."}}</c>. Returns null when the
        /// element is not a Worker model error, so any other error body keeps its old handling.
        /// </summary>
        internal static ModelErrorException? FromErrorElement(JsonElement error)
        {
            if (error.ValueKind != JsonValueKind.Object) return null;
            if (!error.TryGetProperty("code", out var code) || code.ValueKind != JsonValueKind.String) return null;
            if (!error.TryGetProperty("message", out var message) || message.ValueKind != JsonValueKind.String) return null;
            var codeText = code.GetString() ?? string.Empty;
            var messageText = message.GetString();
            if (!WorkerCodes.Contains(codeText) || string.IsNullOrWhiteSpace(messageText)) return null;
            return new ModelErrorException(messageText, codeText);
        }

        /// <summary>
        /// Reads a whole JSON body with a top-level <c>error</c> object. Returns null for any
        /// other body, including text that is not JSON.
        /// </summary>
        internal static ModelErrorException? FromBody(string? body)
        {
            if (string.IsNullOrWhiteSpace(body)) return null;
            try
            {
                using var doc = JsonDocument.Parse(body);
                return doc.RootElement.ValueKind == JsonValueKind.Object
                       && doc.RootElement.TryGetProperty("error", out var error)
                    ? FromErrorElement(error)
                    : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
