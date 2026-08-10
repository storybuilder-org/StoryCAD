using System.Globalization;

namespace StoryCollaborator.Models
{
    /// <summary>
    /// Session-scoped accumulator behind the Collaborator shell's cost line
    /// (devdocs/collaborator_workflow_cost_display_design.md sections 5.1, 7, 8).
    /// "Session" means one Collaborator session — <see cref="Collaborator.OpenAsync"/>
    /// through <see cref="Collaborator.Close"/> — matching SessionService's scope.
    ///
    /// Formatting lives here rather than in the ViewModel because the dependency runs
    /// CollaboratorLib -> StoryCADLib and never back: WorkflowShellViewModel cannot see
    /// <see cref="ProxyCostInfo"/>. The line crosses the boundary as a plain string, the
    /// same way StatusText already does.
    /// </summary>
    internal sealed class WorkflowCostTracker
    {
        /// <summary>
        /// Costs below this render as "&lt;$0.0001" rather than "$0.0000", which reads as free.
        /// 100 microdollars is $0.0001, the smallest figure four decimal places can show.
        /// </summary>
        private const long DisplayThresholdMicrodollars = 100;

        private long _sessionTotalMicrodollars;

        /// <summary>
        /// Running total for the session, in microdollars — the unit the Worker computes,
        /// journals, and debits in, so the client's arithmetic matches the ledger's exactly.
        /// </summary>
        internal long SessionTotalMicrodollars => _sessionTotalMicrodollars;

        /// <summary>
        /// Records one completed workflow run and returns the line to display.
        /// A null <paramref name="cost"/> is a legitimate state, not an error: the proxy
        /// sends no cost event for an unpriced model, absent or malformed usage, the
        /// direct-OpenAI fallback path, or a truncated stream. The run still succeeded,
        /// so the session total survives it untouched.
        /// </summary>
        internal string Record(ProxyCostInfo? cost)
        {
            if (cost == null)
            {
                return _sessionTotalMicrodollars == 0
                    ? "cost unavailable"
                    : $"cost unavailable · {FormatMoney(_sessionTotalMicrodollars)} session";
            }

            _sessionTotalMicrodollars += cost.CostMicrodollars;

            // Model and token counts describe the run just completed, not the session.
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} · {1:N0} in / {2:N0} out · {3} this run · {4} session",
                cost.Model,
                cost.InputTokens,
                cost.OutputTokens,
                FormatMoney(cost.CostMicrodollars),
                FormatMoney(_sessionTotalMicrodollars));
        }

        /// <summary>
        /// Records one completed chat turn and returns the line to display.
        ///
        /// Two sources may report a turn. <paramref name="cost"/> comes from the Worker's
        /// <c>X-Collab-Cost</c> header and is authoritative — it is computed from the same
        /// numbers that journal and debit, so the displayed figure and the billed row
        /// cannot disagree, and it folds into the session total like a workflow run.
        /// <paramref name="fallbackInputTokens"/>/<paramref name="fallbackOutputTokens"/>
        /// come from Semantic Kernel's response metadata and are used only when the header
        /// is absent — an old Worker, or a turn the proxy could not price. Tokens without
        /// dollars are then all the client honestly has, and the session total must not
        /// move for spend it cannot account for.
        /// </summary>
        internal string RecordChat(ProxyCostInfo? cost, int fallbackInputTokens, int fallbackOutputTokens)
        {
            if (cost != null)
            {
                _sessionTotalMicrodollars += cost.CostMicrodollars;

                return string.Format(
                    CultureInfo.InvariantCulture,
                    "chat · {0:N0} in / {1:N0} out · {2} this turn · {3} session",
                    cost.InputTokens,
                    cost.OutputTokens,
                    FormatMoney(cost.CostMicrodollars),
                    FormatMoney(_sessionTotalMicrodollars));
            }

            var line = string.Format(
                CultureInfo.InvariantCulture,
                "chat · {0:N0} in / {1:N0} out · unpriced",
                fallbackInputTokens,
                fallbackOutputTokens);

            return _sessionTotalMicrodollars == 0
                ? line
                : $"{line} · {FormatMoney(_sessionTotalMicrodollars)} session";
        }

        /// <summary>
        /// Clears the session total. Called at the start of a Collaborator session; a fresh
        /// Collaborator instance is built per session, so this is belt-and-braces.
        /// </summary>
        internal void Reset() => _sessionTotalMicrodollars = 0;

        private static string FormatMoney(long microdollars) =>
            microdollars < DisplayThresholdMicrodollars
                ? "<$0.0001"
                : string.Format(CultureInfo.InvariantCulture, "${0:F4}", microdollars / 1_000_000m);
    }
}
