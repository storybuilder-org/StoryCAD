using Microsoft.SemanticKernel.ChatCompletion;
using StoryCADLib.Models;
using StoryCollaborator.Models;
using StoryCollaborator.Workflows;

namespace StoryCollaborator.Services;

/// <summary>What <see cref="WorkflowSessionService"/> wants shown, in order.</summary>
internal enum SessionMessageKind
{
    /// <summary>One progress line. Consecutive lines roll up into one group in the pane.</summary>
    Status,

    /// <summary>A Collaborator bubble.</summary>
    Collaborator,

    /// <summary>An error bubble.</summary>
    Error,

    /// <summary>Empty the conversation before the next message.</summary>
    ClearConversation
}

/// <summary>One line for the conversation. The text is empty for <see cref="SessionMessageKind.ClearConversation"/>.</summary>
internal sealed record SessionMessage(SessionMessageKind Kind, string Text = "");

/// <summary>
/// Collaborator #291: the data of one Collaborator run on one outline, from the run to the
/// last Accept or Skip. It holds no logic. <see cref="WorkflowSessionService"/> reads and
/// writes it; the view models show it (<see cref="WorkflowViewModelWiring"/>); the MCP
/// server holds one per run.
/// </summary>
public class WorkflowSession
{
    public WorkflowSession(Workflow workflow, StoryModel model)
    {
        Workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
        Model = model ?? throw new ArgumentNullException(nameof(model));
    }

    public Workflow Workflow { get; }
    public StoryModel Model { get; }

    /// <summary>The outline file. Every write saves it; null saves nothing.</summary>
    public string? FilePath { get; set; }

    /// <summary>The save that the last write started. A caller that must not outrun it awaits it.</summary>
    public Task? PendingSave { get; set; }

    /// <summary>The input elements, by the workflow's input labels.</summary>
    public Dictionary<string, StoryElement> Elements { get; set; } = new();

    /// <summary>Extra request arguments that ride with every run (the interview transcript).</summary>
    public Action<Dictionary<string, string>>? ExtraArgs { get; set; }

    /// <summary>Keep the chat pane's contents when proposals arrive.</summary>
    public bool PreserveConversation { get; set; }

    /// <summary>What the writer rejected with Try Again, one line each. Null on a first run.</summary>
    public string? RejectedProposals { get; set; }

    /// <summary>The runner of the run. It classifies, applies and reads the touch fingerprint.</summary>
    internal WorkflowRunner? Runner { get; set; }

    /// <summary>The result of the last run. Its pending list holds the open proposals.</summary>
    public WorkflowResult? Result { get; set; }

    /// <summary>Every proposal of the run with its status: open, accepted or skipped.</summary>
    public SessionProposalSet? Proposals { get; set; }

    /// <summary>Protect rows the writer accepted, held until the writer confirms the overwrite.</summary>
    public OverwriteAcceptanceSession Stage { get; set; } = new();

    /// <summary>The keys the last apply wrote.</summary>
    public List<string> LastAppliedKeys { get; } = new();

    /// <summary>The Scene Builder orphan bind runs once per run.</summary>
    public bool OrphanBindDone { get; set; }

    /// <summary>
    /// Up by one for each apply that wrote something. A caller that has a screen to refresh
    /// compares it before and after a call.
    /// </summary>
    public int WriteCount { get; set; }

    /// <summary>
    /// Issue #116: fields Collaborator wrote this session (SessionTouchKey = "{uuid:N}.{Property}").
    /// Collaborator #272: each key maps to the fingerprint of the value Collaborator wrote, so
    /// a later writer edit is not mistaken for Collaborator's own text.
    /// </summary>
    public Dictionary<string, string> TouchedFields { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The text of the last failure (a failed run, a stream that stopped, out of credits, an
    /// interview turn that failed), or null. A run, a try again and a chat call clear it
    /// when they start. A host with no screen reads it where the app shows an error bubble.
    /// </summary>
    public string? LastError { get; private set; }

    internal void SetLastError(string? text) => LastError = text;

    /// <summary>
    /// The cost of the last chat call, in the unit a run reports (<see cref="WorkflowResult.Cost"/>).
    /// Null when the call reported none. A chat call clears it when it starts.
    /// </summary>
    public ProxyCostInfo? LastChatCost { get; internal set; }

    /// <summary>Per-run cost accumulator for the cost line.</summary>
    internal WorkflowCostTracker CostTracker { get; set; } = new();

    /// <summary>The cost line to show next. The screen takes it and sets it back to null.</summary>
    internal string? PendingCostSummary { get; set; }

    #region Chat

    public ChatHistory? ChatHistory { get; set; }
    public IChatCompletionService? ChatService { get; set; }
    public bool ChatEnabled { get; set; }
    public string ChatPlaceholder { get; set; } = "Waiting for proposals…";

    /// <summary>A chat patch changed the proposals. The screen takes it and sets it back to false.</summary>
    internal bool ProposalsChanged { get; set; }

    #endregion

    #region Screen state

    /// <summary>A run or an interview turn is in flight.</summary>
    internal bool Busy { get; set; }

    /// <summary>
    /// Collaborator #286: whole seconds since the run started, while <see cref="Busy"/>. The
    /// screen shows it beside the progress bar so a long run is seen to be alive.
    /// </summary>
    internal int RunSeconds { get; set; }

    /// <summary>Lines for the conversation, in order. The screen takes them and clears the list.</summary>
    internal List<SessionMessage> Messages { get; } = new();

    /// <summary>Raised when something changes during a long call, so a screen can show it at once.</summary>
    internal Action? Changed { get; set; }

    #endregion

    #region Interview (#119)

    /// <summary>The interview in flight, if any.</summary>
    public InterviewTranscript? InterviewTranscript { get; set; }

    public bool IsInterview => InterviewTranscript != null;
    public string InterviewCharacterName { get; set; } = string.Empty;
    public Guid InterviewCharacterGuid { get; set; }

    /// <summary>
    /// The cursor. The Worker is stateless per turn and owns the question bank, so the
    /// client carries the position back on every call and never holds question text.
    /// </summary>
    public InterviewCursor InterviewCursor { get; } = new();

    /// <summary>
    /// The question on screen with no answer yet. A turn is only recorded once the writer
    /// replies, so this is what the next answer gets paired with.
    /// </summary>
    public string InterviewPendingQuestion { get; set; } = string.Empty;

    /// <summary>
    /// The Notes element this session writes to, once it exists. One note per session
    /// (Terry, 2026-09-05): Save updates it in place, and close writes the turns since.
    /// </summary>
    public Guid InterviewNoteGuid { get; set; }

    /// <summary>Turns recorded since the note was last written.</summary>
    public bool InterviewHasUnsavedTurns { get; set; }

    /// <summary>
    /// The last answer was recorded but the Worker never asked the next question (a failed
    /// or empty reply). The writer's next message extends that answer instead of opening a
    /// second turn on the same question.
    /// </summary>
    public bool InterviewAnswerAwaitingQuestion { get; set; }

    /// <summary>
    /// The fields this session pursues, in order (#119, design section 25). Terry's
    /// order until the writer names targets in chat or the Worker names them on a
    /// "you choose" opening; the client holds the list from then on.
    /// </summary>
    public InterviewPlan InterviewPlan { get; set; } = InterviewPlan.Default();

    /// <summary>The choice list is on screen and the writer has not answered it yet.</summary>
    public bool InterviewAwaitingChoice { get; set; }

    /// <summary>The writer said "you choose": the opening reply carries the targets.</summary>
    public bool InterviewModelChooses { get; set; }

    /// <summary>A turn is in flight. A second message in that time is refused.</summary>
    public bool InterviewTurnRunning { get; set; }

    /// <summary>There are recorded turns to save.</summary>
    public bool CanSaveInterview { get; set; }

    #endregion
}
