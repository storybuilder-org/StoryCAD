namespace StoryCollaborator.Models
{
    /// <summary>
    /// Per-call cost reported by the proxy on the SSE stream's <c>collab_cost</c> event.
    /// Null on <see cref="WorkflowResult"/> when the proxy sent no cost event
    /// (old Worker, unpriced model, or the direct-OpenAI fallback path).
    /// </summary>
    public sealed record ProxyCostInfo(
        string Workflow,
        string Model,
        int InputTokens,
        int OutputTokens,
        long CostMicrodollars)
    {
        /// <summary>
        /// <see cref="CostMicrodollars"/> converted to dollars for display.
        /// </summary>
        public decimal CostUsd => CostMicrodollars / 1_000_000m;
    }
}
