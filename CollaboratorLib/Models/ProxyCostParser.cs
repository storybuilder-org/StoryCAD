using System.Text.Json;

namespace StoryCollaborator.Models
{
    /// <summary>
    /// Single parse shared by both cost transports. The Worker emits an identical payload
    /// on each: as the <c>collab_cost</c> SSE event on <c>/v1/workflow</c> and the chat
    /// route's streaming branch, and as the <c>X-Collab-Cost</c> response header on the
    /// non-streaming chat path (which returns the upstream body untouched and so has
    /// nowhere in-band to put it).
    ///
    /// Every failure returns null rather than throwing. A missing or malformed cost is a
    /// legitimate state — an old Worker, an unpriced model, absent usage — and never a
    /// reason to fail the call that carried it.
    /// </summary>
    internal static class ProxyCostParser
    {
        internal const string CostHeaderName = "X-Collab-Cost";

        /// <summary>
        /// Reads the cost payload from a JSON object holding the collab_cost members.
        /// <c>workflow</c> may be null (the chat route has no workflow); <c>model</c> may
        /// not, because without it the display cannot say what was billed.
        /// </summary>
        internal static ProxyCostInfo? TryParse(JsonElement payload)
        {
            try
            {
                if (!payload.TryGetProperty("model", out var modelElement))
                    return null;

                var model = modelElement.ValueKind == JsonValueKind.String ? modelElement.GetString() : null;
                if (model is null)
                    return null;

                string? workflow = null;
                if (payload.TryGetProperty("workflow", out var workflowElement) &&
                    workflowElement.ValueKind == JsonValueKind.String)
                {
                    workflow = workflowElement.GetString();
                }

                return new ProxyCostInfo(
                    workflow,
                    model,
                    payload.GetProperty("input_tokens").GetInt32(),
                    payload.GetProperty("output_tokens").GetInt32(),
                    payload.GetProperty("cost_microdollars").GetInt64());
            }
            catch (Exception)
            {
                // Missing or non-numeric member on an unexpected payload shape.
                return null;
            }
        }

        /// <summary>
        /// Reads the <c>X-Collab-Cost</c> response header, or null when the response does
        /// not carry one.
        /// </summary>
        internal static ProxyCostInfo? TryParseHeader(HttpResponseMessage? response)
        {
            if (response is null ||
                !response.Headers.TryGetValues(CostHeaderName, out var values))
            {
                return null;
            }

            var raw = values.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            try
            {
                using var document = JsonDocument.Parse(raw);
                return TryParse(document.RootElement);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
