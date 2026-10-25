using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Collaborator.Models;
using StoryCADLib.Collaborator.ViewModels;
using StoryCADLib.Models;
using StoryCADLib.Services;
using StoryCollaborator.Models;

namespace StoryCollaborator.Services;

/// <summary>
/// Collaborator #291: connects the Collaborator view models to <see cref="WorkflowSessionService"/>.
/// <c>WorkflowViewModel</c> is in StoryCADLib, which cannot refer to CollaboratorLib, so the view
/// models keep their delegates and this class sets each one to a call of the service. After a
/// call it copies the session to the view model, and after a write it does the two steps that
/// only a screen needs: reload the page of the host and refresh the outline view.
/// Nothing here needs a window.
/// </summary>
public static class WorkflowViewModelWiring
{
    /// <summary>
    /// Sets every delegate of the page view model and of the shell to a call of the service.
    /// </summary>
    /// <param name="viewModel">The workflow page.</param>
    /// <param name="shell">The shell with the top bar. Null in a host that has none.</param>
    /// <param name="service">The session service.</param>
    /// <param name="session">The session that the page shows.</param>
    /// <param name="confirmOverwrite">
    /// Asks the writer to replace text that they wrote, and returns the answer. It is called once,
    /// when the last proposal is decided, with the Protect rows that wait on the stage.
    /// </param>
    public static void Wire(
        WorkflowViewModel viewModel,
        WorkflowShellViewModel? shell,
        WorkflowSessionService service,
        WorkflowSession session,
        Func<IReadOnlyList<PendingUpdate>, Task<bool>> confirmOverwrite)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(confirmOverwrite);

        session.Changed = () => Sync(viewModel, shell, session);
        Sync(viewModel, shell, session);

        viewModel.OnSendMessage = async text =>
        {
            try
            {
                return await service.ChatAsync(session, text);
            }
            finally
            {
                Sync(viewModel, shell, session);
                if (session.ProposalsChanged)
                {
                    session.ProposalsChanged = false;
                    PushSessionSet(viewModel, shell, session);
                }
            }
        };

        viewModel.OnSaveInterview = async () =>
        {
            await service.SaveInterviewAsync(session);
            Sync(viewModel, shell, session);
        };

        viewModel.OnAcceptProperty = key => Guard(viewModel, session, $"Error applying {key}", async () =>
        {
            var writes = session.WriteCount;
            var accepted = await service.AcceptAsync(session, key);
            Sync(viewModel, shell, session);
            if (!accepted)
                return;

            PushSessionSet(viewModel, shell, session);
            AfterWrite(session, writes);
            await ConfirmStagedWhenDoneAsync(viewModel, shell, service, session, confirmOverwrite);
        });

        viewModel.OnSkipProperty = key => Guard(viewModel, session, $"Error skipping {key}", async () =>
        {
            var skipped = service.Skip(session, key);
            Sync(viewModel, shell, session);
            if (!skipped)
                return;

            PushSessionSet(viewModel, shell, session);
            await ConfirmStagedWhenDoneAsync(viewModel, shell, service, session, confirmOverwrite);
        });

        viewModel.OnAcceptAll = () => Guard(viewModel, session, "Error applying updates", async () =>
        {
            // Free rows apply without a question. Rows with text the writer wrote need a yes.
            var protect = OverwriteAcceptanceSession.Partition(
                session.Result?.PendingUpdates ?? new List<PendingUpdate>()).Protect;
            var overwrite = protect.Count > 0 && await confirmOverwrite(protect);

            var writes = session.WriteCount;
            await service.AcceptAllAsync(session, overwrite);
            Sync(viewModel, shell, session);
            PushSessionSet(viewModel, shell, session);
            AfterWrite(session, writes);
        });

        // Accept Remaining: free apply now; remaining Protect staged → end confirm if queue empty.
        viewModel.OnAcceptRemainingFree = () => Guard(viewModel, session, "Error applying remaining", async () =>
        {
            var writes = session.WriteCount;
            await service.AcceptRemainingFreeAsync(session);
            Sync(viewModel, shell, session);

            // Prefer session set so staged Protect show as Accepted for Review Each.
            if (session.Proposals != null && session.Proposals.Count > 0)
                PushSessionSet(viewModel, shell, session);
            else if (session.Result != null)
                PushPending(viewModel, shell, session);
            AfterWrite(session, writes);
            await ConfirmStagedWhenDoneAsync(viewModel, shell, service, session, confirmOverwrite);

            if (session.Result != null && session.Result.PendingUpdates.Count == 0 && !session.Stage.HasStaged)
            {
                viewModel.MarkUpdatesApplied();
                SyncShellPending(viewModel, shell);
            }
        });

        viewModel.OnTryAgain = async () =>
        {
            viewModel.ClearPendingUpdates();
            await service.TryAgainAsync(session);
            Show(viewModel, shell, session);
        };

        WireShellActions(viewModel, shell);
    }

    /// <summary>
    /// Routes top-bar Accept All / Review Each / Try Again to the active page view model.
    /// </summary>
    private static void WireShellActions(WorkflowViewModel viewModel, WorkflowShellViewModel? shell)
    {
        if (shell == null) return;

        shell.OnAcceptAll = () => viewModel.AcceptAllCommand.Execute(null);
        shell.OnReviewEach = () => viewModel.ReviewEachCommand.Execute(null);
        shell.OnTryAgain = async () =>
        {
            if (viewModel.OnTryAgain != null)
                await viewModel.OnTryAgain();
            else
                viewModel.TryAgainCommand.Execute(null);
        };
        SyncShellPending(viewModel, shell);
    }

    /// <summary>
    /// Runs the workflow of the session and shows the outcome on the page.
    /// </summary>
    internal static async Task RunAsync(
        WorkflowViewModel viewModel,
        WorkflowShellViewModel? shell,
        WorkflowSessionService service,
        WorkflowSession session)
    {
        await service.RunAsync(session);
        Show(viewModel, shell, session);
    }

    /// <summary>
    /// Puts the page into interview mode and opens the interview (#119). Call it for a
    /// Conversational workflow, in place of <see cref="RunAsync"/>.
    /// </summary>
    internal static void BeginInterview(
        WorkflowViewModel viewModel,
        WorkflowShellViewModel? shell,
        WorkflowSessionService service,
        WorkflowSession session)
    {
        viewModel.BeginInterviewSession();
        service.StartInterview(session);
        Sync(viewModel, shell, session);
    }

    /// <summary>
    /// Copies the session to the page: the lines for the conversation, the state of the chat and the
    /// interview, the cost line, and, when a run left proposals, the proposal list.
    /// </summary>
    internal static void Show(WorkflowViewModel viewModel, WorkflowShellViewModel? shell, WorkflowSession session)
    {
        Sync(viewModel, shell, session);
        if (session.Result is { Success: true } result
            && result.PendingUpdates.Count > 0
            && session.Proposals != null)
        {
            PushSessionSet(viewModel, shell, session);
        }
    }

    /// <summary>
    /// Copies what a long call changed: new lines, flags and the cost line.
    /// </summary>
    private static void Sync(WorkflowViewModel viewModel, WorkflowShellViewModel? shell, WorkflowSession session)
    {
        foreach (var message in session.Messages.ToList())
        {
            switch (message.Kind)
            {
                case SessionMessageKind.Status:
                    viewModel.AddStatusMessage(message.Text);
                    break;
                case SessionMessageKind.Collaborator:
                    viewModel.ConversationList.Add(ChatMessage.FromCollaborator(message.Text));
                    break;
                case SessionMessageKind.Error:
                    viewModel.ConversationList.Add(ChatMessage.Error(message.Text));
                    break;
                case SessionMessageKind.ClearConversation:
                    viewModel.ConversationList.Clear();
                    break;
            }
        }
        session.Messages.Clear();

        viewModel.IsChatEnabled = session.ChatEnabled;
        viewModel.ChatPlaceholder = session.ChatPlaceholder;
        viewModel.ProgressVisibility = session.Busy
            ? Microsoft.UI.Xaml.Visibility.Visible
            : Microsoft.UI.Xaml.Visibility.Collapsed;
        // Collaborator #286: the elapsed seconds of a run, blank when nothing runs.
        viewModel.ProgressText = session.Busy && session.RunSeconds > 0
            ? $"{session.RunSeconds} s"
            : string.Empty;
        viewModel.IsInterviewTurnRunning = session.InterviewTurnRunning;
        viewModel.CanSaveInterview = session.CanSaveInterview;

        if (session.PendingCostSummary != null)
        {
            if (shell != null)
                shell.CostSummary = session.PendingCostSummary;
            session.PendingCostSummary = null;
        }
    }

    /// <summary>
    /// Asks the writer about the staged Protect rows, once, when the last proposal is decided,
    /// and gives the answer to the service.
    /// </summary>
    private static async Task ConfirmStagedWhenDoneAsync(
        WorkflowViewModel viewModel,
        WorkflowShellViewModel? shell,
        WorkflowSessionService service,
        WorkflowSession session,
        Func<IReadOnlyList<PendingUpdate>, Task<bool>> confirmOverwrite)
    {
        if (!service.ShouldConfirmStaged(session))
            return;

        var staged = session.Stage.StagedProtect.ToList();
        var writes = session.WriteCount;
        var confirmed = await confirmOverwrite(staged);
        await service.ApplyStagedAsync(session, confirmed);
        Sync(viewModel, shell, session);
        AfterWrite(session, writes);
        FinishPendingUi(viewModel, shell, session);
    }

    /// <summary>
    /// Keep full session set visible (skipped/accepted stay on the left).
    /// </summary>
    private static void FinishPendingUi(WorkflowViewModel viewModel, WorkflowShellViewModel? shell, WorkflowSession session)
    {
        if (session.Proposals != null && session.Proposals.Count > 0)
            PushSessionSet(viewModel, shell, session);
        else if (session.Result != null && session.Result.PendingUpdates.Count == 0 && !session.Stage.HasStaged)
        {
            viewModel.MarkUpdatesApplied();
            SyncShellPending(viewModel, shell);
        }
        else
            PushPending(viewModel, shell, session);
        session.Model.RefreshCurrentView();
    }

    /// <summary>
    /// After a write, the host page must load the new values (else a later flush of its stale
    /// view model overwrites them) and the outline view must redraw. Neither is the service's job.
    /// </summary>
    private static void AfterWrite(WorkflowSession session, int writesBefore)
    {
        if (session.WriteCount == writesBefore)
            return;

        ReloadHostViewModelFromModel();
        session.Model.RefreshCurrentView();
    }

    /// <summary>
    /// After Collaborator Accept writes the model via API, reload the host page ViewModel
    /// so the Character (etc.) form and later AutoSave flushes see the new values.
    /// Close already reloads; Accept did not — empty VM then overwrote applied fields.
    /// </summary>
    private static void ReloadHostViewModelFromModel()
    {
        try
        {
            var appState = Ioc.Default.GetService<AppState>();
            if (appState?.CurrentSaveable is IReloadable reloadable)
                reloadable.ReloadFromModel();
        }
        catch
        {
            // The write stands; only the page of the host stays stale until it is reopened.
        }
    }

    /// <summary>
    /// Runs one handler. An error shows as a line in the conversation, as it always did, and
    /// never escapes to the command that called it.
    /// </summary>
    private static async Task Guard(
        WorkflowViewModel viewModel, WorkflowSession session, string what, Func<Task> handler)
    {
        try
        {
            await handler();
        }
        catch (Exception ex)
        {
            viewModel.ConversationList.Add(ChatMessage.Error($"{what}: {ex.Message}"));
        }
    }

    /// <summary>
    /// Show the full session proposal set on the left (open + accepted + skipped).
    /// </summary>
    internal static void PushSessionSet(WorkflowViewModel viewModel, WorkflowShellViewModel? shell, WorkflowSession session)
    {
        if (session.Proposals == null || session.Proposals.Count == 0)
        {
            viewModel.SetPendingUpdates(Array.Empty<PendingUpdateItem>());
            SyncShellPending(viewModel, shell);
            return;
        }

        var items = session.Proposals.All
            .OrderBy(e => e.Update.Key, StringComparer.OrdinalIgnoreCase)
            .Select(WorkflowSessionService.ToSessionPendingUpdateItem)
            .ToList();
        viewModel.SetPendingUpdates(items);
        // Shell Accept All only when something is still open
        if (shell != null)
            shell.HasPendingUpdates = session.Proposals.OpenCount > 0;
    }

    /// <summary>
    /// Pushes classified pending updates into the workflow panel (issue #116)
    /// and enables shell Accept All / Review Each / Try Again.
    /// </summary>
    private static void PushPending(WorkflowViewModel viewModel, WorkflowShellViewModel? shell, WorkflowSession session)
    {
        var items = session.Result!.PendingUpdates.Select(u => ToPendingUpdateItem(session, u)).ToList();
        viewModel.SetPendingUpdates(items);
        SyncShellPending(viewModel, shell);
    }

    private static void SyncShellPending(WorkflowViewModel viewModel, WorkflowShellViewModel? shell)
    {
        if (shell != null)
            shell.HasPendingUpdates = viewModel.HasPendingUpdates;
    }

    private static PendingUpdateItem ToPendingUpdateItem(WorkflowSession session, PendingUpdate u)
    {
        var proposed = WorkflowSessionService.TruncateForChat(
            WorkflowSessionService.FormatValueForDisplay(session.Model, u.Value), 300);
        var current = WorkflowSessionService.TruncateForChat(u.CurrentDisplay ?? string.Empty, 300);
        var kindLabel = u.Kind switch
        {
            UpdateKind.Fill => "New",
            UpdateKind.Refresh => "Refresh",
            UpdateKind.Protect => "Has your text",
            UpdateKind.Unclassified => "Update",
            _ => u.Kind.ToString()
        };
        var summary = u.Kind == UpdateKind.Protect
            ? $"{kindLabel} — review before replace"
            : kindLabel;
        if (!string.IsNullOrWhiteSpace(u.CraftExplanation) && u.Kind == UpdateKind.Protect)
            summary += " (craft note)";

        return new PendingUpdateItem
        {
            Key = u.Key,
            ElementName = ValueDisplay.SplitPascalCase(u.ElementLabel),
            // Collaborator #217 section 5.7: a beat row names itself ("Beat 3: Set-Up").
            PropertyDisplayName = u.DisplayNameOverride ?? ValueDisplay.SplitPascalCase(u.Spec.Property),
            ProposedDisplay = string.IsNullOrEmpty(proposed) ? "(empty)" : proposed,
            CurrentDisplay = current,
            KindLabel = kindLabel,
            IsProtected = u.Kind == UpdateKind.Protect,
            CraftExplanation = u.CraftExplanation,
            SummaryLine = summary
        };
    }
}
