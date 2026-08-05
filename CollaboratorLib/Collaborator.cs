using System.Linq;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using NLog.Extensions.Logging;
using StoryCADLib.Models;
using StoryCADLib.Services.Collaborator.Contracts;
using StoryCADLib.Services.Store;
using StoryCollaborator.Services;
using StoryCollaborator.Models;
using StoryCollaborator.Workflows;
using CollaboratorLib.Context;
using StoryCADLib.Collaborator.ViewModels;
using StoryCADLib.Collaborator.Models;
using StoryCADLib.Collaborator.Views;

namespace StoryCollaborator;

/// <summary>
/// Implements <see cref="ICollaborator"/> for StoryCAD's AI features.
/// Call <see cref="OpenAsync"/> to start a session: it initializes Semantic Kernel,
/// navigates the host frame to <c>WorkflowShell</c>, populates the workflow menu,
/// and wires navigation and callback handlers for the UI.
/// </summary>
public class Collaborator : ICollaborator
{
    // Services (resolved from DI in OpenAsync)
    private ILogger<Collaborator>? _logger;
    private ILoggerFactory? _loggerFactory;
    private SessionService? _sessionService;

    // Semantic Kernel (lazy initialized - expensive to create)
    private Kernel? _kernel;
    private IChatCompletionService? _chatService;
    private ChatHistory? _chatHistory;
    private bool _kernelInitialized;
    private readonly object _kernelLock = new();

    /// <summary>Collaborator #145: last workflow run proposals for proposal-chat.</summary>
    private SessionProposalSet? _sessionProposals;

    /// <summary>Live result for the open workflow page (pending list + chat patches).</summary>
    private WorkflowResult? _activeWorkflowResult;

    private StoryCADLib.Collaborator.ViewModels.WorkflowViewModel? _activeWorkflowViewModel;

    // State
    private IStoryCADAPI? _storyApi;
    private StoryModel? _storyModel;
    private ElementResolver? _elementResolver;
    private string? _filePath;
    private Window? _hostWindow;
    private Frame? _hostFrame;
    private WorkflowShellViewModel? _shellViewModel;
    private bool _disposed;
    private StoryCADLib.Services.Logging.ILogService? _auditLogger;

    /// <summary>
    /// Issue #116: fields Collaborator successfully wrote this plugin session
    /// (SessionTouchKey = "{uuid:N}.{Property}"). Not persisted across app restarts.
    /// </summary>
    private readonly HashSet<string> _sessionTouchedFields =
        new(StringComparer.OrdinalIgnoreCase);

    // Settings
    private CollaboratorSettings _settings = CollaboratorSettings.Default;

    // Debug control - initialized from env var, tests can override directly
    internal static bool CollabDebug =
        Environment.GetEnvironmentVariable("COLLAB_DEBUG") == "1";

    public Collaborator()
    {
        // Note: Workflows are registered via WorkflowRegistry.All static initializer
        // Semantic Kernel is initialized lazily in EnsureKernelInitialized() to avoid
        // slow constructor (7+ minutes) which impacts unit tests and startup time
    }

    /// <summary>
    /// Opens a Collaborator session for the specified story context.
    /// </summary>
    /// <remarks>
    /// When a logger is provided, the following audit events are written to StoryCAD's log:
    ///
    /// | Event              | Level | Message                                                    | Source            |
    /// |--------------------|-------|------------------------------------------------------------|-------------------|
    /// | Session open       | Info  | "Collaborator session opened"                              | OpenAsync         |
    /// | Session close      | Info  | "Collaborator session closed"                              | Close             |
    /// | Workflow start     | Info  | "Workflow started: {title} with {count} elements"          | ExecuteWorkflow   |
    /// | Accept All         | Info  | "Applied {count} updates from workflow: {title}"           | OnAcceptAll       |
    /// | Accept Property    | Info  | "Applied update: {propertyKey}"                            | OnAcceptProperty  |
    /// | Workflow failure   | Error | "Workflow failed: {title}" + exception                     | WorkflowRunner    |
    /// | SK invocation fail | Error | "Semantic Kernel invocation failed for workflow: {label}"   | WorkflowRunner    |
    ///
    /// No prompt content, SK payloads, or story data values are logged — only operational audit events.
    /// </remarks>
    public async Task<Window> OpenAsync(IStoryCADAPI api, StoryModel model, Window hostWindow, Frame hostFrame, string filePath, StoryCADLib.Services.Logging.ILogService? logger = null)
    {
#if DEBUG
        // Attach or break into debugger when COLLAB_DEBUG=1.
        if (CollabDebug)
        {
            if (!System.Diagnostics.Debugger.IsAttached)
                System.Diagnostics.Debugger.Launch();
            else
                System.Diagnostics.Debugger.Break();
        }
#endif

        _storyApi = api;
        _storyModel = model;
        _filePath = filePath;
        _hostWindow = hostWindow;
        _hostFrame = hostFrame;
        _auditLogger = logger;

        // Initialize logging and services
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            if (NLog.LogManager.Configuration != null)
                builder.AddNLog(NLog.LogManager.Configuration);
        });
        _logger = _loggerFactory.CreateLogger<Collaborator>();
        _elementResolver = new ElementResolver(api, _loggerFactory.CreateLogger<ElementResolver>());
        _sessionService = new SessionService(_loggerFactory.CreateLogger<SessionService>());

        // Initialize Semantic Kernel lazily (expensive operation, ~7 min if done in constructor)
        EnsureKernelInitialized();

        _sessionService.StartSession();

        // Navigate the host-provided frame to the shell
        hostFrame.Navigate(typeof(StoryCADLib.Collaborator.Views.WorkflowShell));

        // After Navigate, populate the ViewModel with workflow data
        // (ViewModel is in StoryCADLib, but Collaborator has access to WorkflowRegistry.All)
        if (hostFrame.Content is StoryCADLib.Collaborator.Views.WorkflowShell shell)
        {
            var viewModel = shell.DataContext as StoryCADLib.Collaborator.ViewModels.WorkflowShellViewModel;
            if (viewModel != null)
            {
                _shellViewModel = viewModel;
                // Outline gaps (if any) then #129 groups by element type.
                RebuildWorkflowMenu(viewModel);
                _logger.LogInformation("Populated {Count} menu items", viewModel.MenuItems.Count);

                // Set up settings - pass current settings and wire up change callback
                viewModel.CurrentSettings = _settings;
                viewModel.OnSettingsChanged = (newSettings) =>
                {
                    SetSettings(newSettings);
                };

                // Set up Save callback (Issue #55)
                viewModel.OnSave = () =>
                {
                    _ = Task.Run(async () =>
                    {
                        var saved = await SaveAsync();
                        _logger?.LogInformation("Manual save {Result}", saved ? "succeeded" : "failed");
                    });
                };

                // Set up Exit callback (Issue #55)
                viewModel.OnExit = () =>
                {
                    _logger?.LogInformation("Exit callback invoked");
                    _hostWindow?.Close();
                };

                // Set up navigation callback - when user selects a workflow, navigate to WorkflowPage
                viewModel.OnWorkflowSelected = async (workflowTag) =>
                {
                    if (viewModel.ContentFrame == null)
                        return;

                    // Issue #107 phase 6: Outline gaps (navigate only; no LLM)
                    if (workflowTag is string tag &&
                        string.Equals(tag, GapWorkflowOwnership.OutlineGapsTag, StringComparison.Ordinal))
                    {
                        await OpenOutlineGapsPageAsync(viewModel);
                        return;
                    }

                    if (workflowTag is Workflow workflow)
                    {
                        // Short name on top bar (Label, not long Title path).
                        viewModel.ActiveWorkflowName = FormatWorkflowShortName(workflow.Label);
                        viewModel.HasPendingUpdates = false;

                        // Clear shell status; gather cancel has no chat page (#123).
                        viewModel.StatusText = string.Empty;

                        // Gather required input elements before navigating
                        var gatherResult = await GatherWorkflowInputsAsync(workflow, hostFrame.XamlRoot!);
                        if (gatherResult.Cancelled)
                        {
                            // No WorkflowPage / chat — surface cancel on the shell status line.
                            viewModel.StatusText = FormatGatherStatusForShell(gatherResult.StatusMessages);
                            _logger?.LogInformation("Workflow cancelled - user did not select required elements");
                            return;
                        }

                        viewModel.StatusText = string.Empty;

                        // Navigate to WorkflowPage
                        viewModel.ContentFrame.Navigate(typeof(StoryCADLib.Collaborator.Views.WorkflowPage));

                        // Get the page and populate its ViewModel
                        if (viewModel.ContentFrame.Content is StoryCADLib.Collaborator.Views.WorkflowPage page
                            && page.ViewModel != null)
                        {
                            PopulateWorkflowViewModel(page.ViewModel, workflow, gatherResult.Elements);
                            WireUpChatCallback(page.ViewModel, workflow, gatherResult.Elements);
                            WireShellWorkflowActions(page.ViewModel);

                            // Add status messages from gathering phase (rolled up, #129)
                            foreach (var message in gatherResult.StatusMessages)
                            {
                                page.ViewModel.AddStatusMessage(message);
                            }

                            // Auto-execute the workflow and show progress
                            await ExecuteWorkflowWithFeedback(page.ViewModel, workflow, gatherResult.Elements);

                            // Gaps may have closed after Accept — refresh nav
                            RebuildWorkflowMenu(viewModel);
                        }

                        _logger.LogInformation("Navigated to workflow: {Workflow} with {Count} input elements",
                            workflow.Title, gatherResult.Elements.Count);
                    }
                };
            }
        }

        _logger.LogInformation("Collaborator session opened");
        _auditLogger?.Log(StoryCADLib.Services.Logging.LogLevel.Info, "Collaborator session opened");

        return hostWindow;
    }

    /// <summary>
    /// Closes the Collaborator session and returns results.
    /// </summary>
    public CollaboratorResult Close()
    {
        if (_sessionService == null || !_sessionService.IsActive)
        {
            return new CollaboratorResult
            {
                Completed = false,
                Summary = "No active session"
            };
        }

        _sessionService.EndSession();

        var result = new CollaboratorResult
        {
            Completed = true,
            Summary = "Collaborator session closed.",
            Messages = _sessionService.GetMessagesArray()
        };

        _logger?.LogInformation("Collaborator session closed");
        _auditLogger?.Log(StoryCADLib.Services.Logging.LogLevel.Info, "Collaborator session closed");

        // Refresh StoryCAD's UI to show changes made during session
        _storyModel?.RefreshCurrentView();

        Dispose();
        return result;
    }

    /// <summary>
    /// Rebuild nav: Outline gaps first (when any), then workflows grouped by element type (#129).
    /// </summary>
    private void RebuildWorkflowMenu(WorkflowShellViewModel viewModel)
    {
        // Clearing the menu throws away the selected container, so the pane loses its
        // highlight on every post-run gap refresh. Re-select the same tag afterwards.
        var selectedTag = viewModel.CurrentItem?.Tag;
        viewModel.MenuItems.Clear();

        if (_storyApi != null && _storyModel != null)
        {
            var gapDetails = RequiredFieldGapScanner.FindGapDetails(_storyApi, _storyModel);
            if (gapDetails.Count > 0)
            {
                viewModel.MenuItems.Add(WrappingNavItem(
                    $"{GapWorkflowOwnership.OutlineGapsNavTitle} ({gapDetails.Count})",
                    GapWorkflowOwnership.OutlineGapsTag));
            }
        }

        // Workflows grouped by story element type; group headers expand/collapse only (#129).
        Microsoft.UI.Xaml.Controls.NavigationViewItem? group = null;
        StoryItemType? groupType = null;
        foreach (var workflow in WorkflowRegistry.All)
        {
            if (group == null || workflow.PrimaryElementType != groupType)
            {
                groupType = workflow.PrimaryElementType;
                group = new Microsoft.UI.Xaml.Controls.NavigationViewItem
                {
                    Content = GroupTitle(groupType.Value),
                    SelectsOnInvoked = false,
                    IsExpanded = true
                };
                viewModel.MenuItems.Add(group);
            }

            group.MenuItems.Add(WrappingNavItem(workflow.Title, workflow));
        }

        viewModel.RestoreSelection(selectedTag);
    }

    /// <summary>
    /// Nav item whose title wraps instead of being trimmed to one line. A string Content is
    /// rendered by the item template as a single non-wrapping line, so the text has to be a
    /// TextBlock we control; Height=Auto lets the item grow past the one-line default.
    /// AutomationProperties.Name keeps the title available to automation and screen readers.
    /// </summary>
    private static Microsoft.UI.Xaml.Controls.NavigationViewItem WrappingNavItem(string title, object tag)
    {
        var item = new Microsoft.UI.Xaml.Controls.NavigationViewItem
        {
            Content = new Microsoft.UI.Xaml.Controls.TextBlock
            {
                Text = title,
                TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap
            },
            Tag = tag,
            Height = double.NaN
        };
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(item, title);
        return item;
    }

    /// <summary>
    /// Opens the Outline gaps page (no gather, no LLM).
    /// </summary>
    private Task OpenOutlineGapsPageAsync(WorkflowShellViewModel shellViewModel)
    {
        shellViewModel.StatusText = string.Empty;
        if (_storyApi == null || _storyModel == null || shellViewModel.ContentFrame == null)
            return Task.CompletedTask;

        var details = RequiredFieldGapScanner.FindGapDetails(_storyApi, _storyModel);
        var groups = details.Select(d =>
        {
            // Option 2: each missing field is a link (workflow if mapped, else host element)
            var fields = new List<GapFieldLink>();
            for (var i = 0; i < d.MissingProperties.Count; i++)
            {
                var prop = d.MissingProperties[i];
                var display = i < d.MissingPropertyLabels.Count
                    ? d.MissingPropertyLabels[i]
                    : prop;
                var helpers = GapWorkflowOwnership.WorkflowsFor(d.ElementType, prop);
                var workflowLabel = helpers.Count > 0 ? helpers[0] : string.Empty;
                var wf = string.IsNullOrEmpty(workflowLabel)
                    ? null
                    : WorkflowRegistry.Get(workflowLabel);

                fields.Add(new GapFieldLink
                {
                    DisplayLabel = display,
                    PropertyName = prop,
                    ElementGuid = d.ElementGuid,
                    WorkflowLabel = workflowLabel,
                    WorkflowTitle = wf?.Title ?? workflowLabel
                });
            }

            return new GapElementGroup
            {
                ElementGuid = d.ElementGuid,
                ElementName = d.ElementName,
                ElementTypeLabel = d.ElementType.ToString(),
                MissingFields = fields
            };
        }).ToList();

        shellViewModel.ContentFrame.Navigate(typeof(GapWorkflowPage));
        if (shellViewModel.ContentFrame.Content is GapWorkflowPage page && page.ViewModel != null)
        {
            page.ViewModel.Load(new GapWorkflowPayload { Groups = groups });
            page.ViewModel.OnOpenElement = guid =>
            {
                var result = _storyApi.SelectStoryElement(guid);
                if (!result.IsSuccess)
                    shellViewModel.StatusText = result.ErrorMessage ?? "Could not open element in StoryCAD.";
                else
                    shellViewModel.StatusText = string.Empty;
            };
            page.ViewModel.OnOpenWorkflow = async label =>
            {
                var workflow = WorkflowRegistry.Get(label);
                if (workflow == null)
                {
                    shellViewModel.StatusText = $"Unknown workflow: {label}";
                    return;
                }

                // Select matching nav item if present (workflows are nested under type groups, #129)
                var navItem = FindWorkflowNavItem(shellViewModel, label);
                if (navItem != null)
                    shellViewModel.CurrentItem = navItem;

                if (shellViewModel.OnWorkflowSelected != null)
                    await shellViewModel.OnWorkflowSelected(workflow);
            };
        }

        _logger?.LogInformation("Opened Outline gaps page with {Count} gappy elements", groups.Count);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Saves the story outline via the API.
    /// Bypasses StoryCAD's ViewModel flush to ensure API-applied changes persist.
    /// </summary>
    private async Task<bool> SaveAsync()
    {
        if (string.IsNullOrEmpty(_filePath))
        {
            _logger?.LogWarning("Save failed: No file path available");
            return false;
        }

        if (_storyApi == null)
        {
            _logger?.LogWarning("Save failed: No API available");
            return false;
        }

        try
        {
            var result = await _storyApi.WriteOutline(_filePath);
            if (result.IsSuccess)
            {
                _logger?.LogInformation("Outline saved successfully to {FilePath}", _filePath);
                return true;
            }
            else
            {
                _logger?.LogError("Save failed: {Error}", result.ErrorMessage);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving outline to {FilePath}", _filePath);
            return false;
        }
    }

    /// <summary>
    /// Sets Collaborator settings. Can be called before or after OpenAsync.
    /// </summary>
    public void SetSettings(CollaboratorSettings settings)
    {
        _settings = settings ?? CollaboratorSettings.Default;
        _logger?.LogInformation("Settings updated: Terseness={Terseness}, ContentPreservation={Preservation}",
            _settings.Terseness, _settings.ContentPreservation);
    }

    /// <summary>
    /// Gets the current Collaborator settings.
    /// </summary>
    public CollaboratorSettings GetSettings()
    {
        return _settings;
    }

    /// <summary>
    /// Disposes resources used by the Collaborator.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Null assignments commented out - likely unnecessary since Collaborator
        // won't outlive its fields. The _disposed flag prevents re-entry.
        // See: https://blog.stephencleary.com/2010/02/q-should-i-set-variables-to-null-to.html
        // _storyApi = null;
        // _storyModel = null;
        // _kernel = null;
        // _chatService = null;
        // _chatHistory = null;
    }

    /// <summary>
    /// Populates a WorkflowViewModel with data from a Workflow.
    /// This bridges the assembly boundary - WorkflowViewModel is in StoryCADLib,
    /// Workflow is in CollaboratorLib. Collaborator has access to both.
    /// </summary>
    /// <param name="viewModel">The ViewModel to populate (from StoryCADLib)</param>
    /// <param name="workflow">The workflow data source (from CollaboratorLib)</param>
    /// <param name="gatheredElements">Elements selected by user for this workflow</param>
    public static void PopulateWorkflowViewModel(
        StoryCADLib.Collaborator.ViewModels.WorkflowViewModel viewModel,
        Workflow workflow,
        Dictionary<string, StoryElement>? gatheredElements = null)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(workflow);

        viewModel.Title = workflow.Title;
        // Brief purpose only; long registry Explanation is not shown (topical strip instead).
        viewModel.Description = workflow.Description;

        // Selected elements live in topical Explanation (#129), not a separate card.
        if (gatheredElements != null && gatheredElements.Count > 0)
        {
            var lines = gatheredElements
                .Select(kvp => $"{kvp.Key}: {kvp.Value?.Name ?? "(none)"}")
                .ToList();
            viewModel.SelectedElementsSummary = string.Join("\n", lines);
        }
        else
        {
            viewModel.SelectedElementsSummary = string.Empty;
        }

        viewModel.RefreshTopicalExplanation();
    }

    /// <summary>
    /// Wires chat for a workflow. Send stays disabled until
    /// <see cref="BeginProposalChatSession"/> after the one-shot produces proposals (#145).
    /// </summary>
    private void WireUpChatCallback(
        StoryCADLib.Collaborator.ViewModels.WorkflowViewModel viewModel,
        Workflow workflow,
        Dictionary<string, StoryElement> gatheredElements)
    {
        _sessionProposals = null;
        _activeWorkflowResult = null;
        _activeWorkflowViewModel = viewModel;
        _chatHistory = new ChatHistory();
        viewModel.IsChatEnabled = false;
        viewModel.ChatPlaceholder = "Waiting for proposals…";

        viewModel.OnSendMessage = async (userMessage) =>
        {
            try
            {
                if (_sessionProposals == null || _sessionProposals.Count == 0)
                {
                    return "Chat unlocks after the workflow produces property proposals.";
                }

                EnsureKernelInitialized();
                _chatHistory?.AddUserMessage(userMessage);
                _logger?.LogDebug("User message added to chat: {Message}", userMessage);

                var response = await _chatService!.GetChatMessageContentAsync(_chatHistory!);
                var responseText = response.Content ?? "No response received.";

                ChatPatchParser.TryParse(responseText, out var display, out var patches);
                _chatHistory?.AddAssistantMessage(display);

                if (patches.Count > 0 && _sessionProposals != null)
                {
                    var applied = 0;
                    foreach (var p in patches)
                    {
                        if (_sessionProposals.TryApplyPatch(p.Key, p.Value, out _))
                            applied++;
                        else
                            _logger?.LogDebug("Ignored chat patch for unknown key {Key}", p.Key);
                    }

                    if (applied > 0)
                    {
                        SyncWorkflowResultFromSession();
                        RefreshProposalSnapshotInHistory();
                        display = string.IsNullOrWhiteSpace(display)
                            ? $"Updated {applied} proposal(s). Accept to write the outline."
                            : display + $"\n\n({applied} proposal(s) updated — Accept to write the outline.)";
                    }
                }

                _logger?.LogDebug("Assistant response (display): {Response}", display);
                return display;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error processing chat message");
                throw TranslateChatException(ex);
            }
        };

        _logger?.LogInformation(
            "Chat callback wired for workflow: {Workflow} (proposal-chat; Send locked until seed)",
            workflow.Title);
    }

    /// <summary>
    /// #145: clear chat, seed system + proposal snapshot, unlock Send.
    /// Call after the workflow one-shot has classified pending updates.
    /// </summary>
    private void BeginProposalChatSession(
        StoryCADLib.Collaborator.ViewModels.WorkflowViewModel viewModel,
        Workflow workflow,
        WorkflowResult result)
    {
        _activeWorkflowResult = result;
        _activeWorkflowViewModel = viewModel;

        if (result.PendingUpdates.Count == 0)
        {
            viewModel.IsChatEnabled = false;
            viewModel.ChatPlaceholder = "No proposals to edit in chat";
            _sessionProposals = null;
            return;
        }

        EnsureKernelInitialized();
        _sessionProposals = new SessionProposalSet();
        _sessionProposals.ReplaceFromPending(result.PendingUpdates);

        _chatHistory = new ChatHistory();
        _chatHistory.AddSystemMessage(SessionProposalSet.BuildSystemInstructions(workflow.Title));
        _chatHistory.AddSystemMessage(_sessionProposals.BuildSnapshotText());

        viewModel.ConversationList.Clear();
        viewModel.ConversationList.Add(ChatMessage.FromCollaborator(
            "Proposals are ready. Ask about them or request changes (for example, rename a field). " +
            "Accept still writes the outline. This chat is only for these proposals."));

        viewModel.IsChatEnabled = true;
        viewModel.ChatPlaceholder = "Ask about or change proposals…";
        _logger?.LogInformation(
            "Proposal chat seeded for {Workflow} with {Count} keys",
            workflow.Title, _sessionProposals.Count);
    }

    /// <summary>
    /// Mid-chat: append an updated proposal snapshot (keeps user/assistant turns).
    /// </summary>
    private void RefreshProposalSnapshotInHistory()
    {
        if (_chatHistory == null || _sessionProposals == null)
            return;
        _chatHistory.AddSystemMessage(
            "Updated property proposals:\n" + _sessionProposals.BuildSnapshotText());
    }

    /// <summary>
    /// Push session proposals into WorkflowResult (open only for Accept) and
    /// Property Updates UI (all statuses so Skip does not blank the panel — #145).
    /// </summary>
    private void SyncWorkflowResultFromSession()
    {
        if (_sessionProposals == null || _activeWorkflowResult == null || _activeWorkflowViewModel == null)
            return;

        var open = _sessionProposals.OpenAsPendingUpdates().ToList();
        _activeWorkflowResult.PendingUpdates.Clear();
        _activeWorkflowResult.UpdatedProperties.Clear();
        foreach (var u in open)
        {
            _activeWorkflowResult.PendingUpdates.Add(u);
            _activeWorkflowResult.UpdatedProperties[u.Key] = FormatValueForDisplay(u.Value);
        }

        PushSessionSetToViewModel(_activeWorkflowViewModel);
    }

    /// <summary>
    /// Show the full session proposal set on the left (open + accepted + skipped).
    /// Accept handlers still use <see cref="_activeWorkflowResult"/> open rows only.
    /// </summary>
    private void PushSessionSetToViewModel(
        StoryCADLib.Collaborator.ViewModels.WorkflowViewModel viewModel)
    {
        if (_sessionProposals == null || _sessionProposals.Count == 0)
        {
            viewModel.SetPendingUpdates(Array.Empty<PendingUpdateItem>());
            SyncShellPending(viewModel);
            return;
        }

        var items = _sessionProposals.All
            .OrderBy(e => e.Update.Key, StringComparer.OrdinalIgnoreCase)
            .Select(ToSessionPendingUpdateItem)
            .ToList();
        viewModel.SetPendingUpdates(items);
        // Shell Accept All only when something is still open
        if (_shellViewModel != null)
            _shellViewModel.HasPendingUpdates = _sessionProposals.OpenCount > 0;
    }

    private PendingUpdateItem ToSessionPendingUpdateItem(SessionProposalSet.Entry e)
    {
        var u = e.Update with { Value = e.ProposedText };
        var proposed = TruncateForChat(FormatValueForDisplay(u.Value), 500);
        var current = TruncateForChat(u.CurrentDisplay ?? string.Empty, 300);
        var kindLabel = e.Status switch
        {
            ProposalSessionStatus.Accepted => "Accepted",
            ProposalSessionStatus.Skipped => "Skipped",
            _ => u.Kind switch
            {
                UpdateKind.Fill => "New",
                UpdateKind.Refresh => "Refresh",
                UpdateKind.Protect => "Has your text",
                _ => "Update"
            }
        };
        return new PendingUpdateItem
        {
            Key = u.Key,
            ElementName = u.ElementLabel,
            PropertyDisplayName = u.Spec.Property,
            ProposedDisplay = proposed,
            CurrentDisplay = current,
            KindLabel = kindLabel,
            IsProtected = e.Status == ProposalSessionStatus.Open && u.Kind == UpdateKind.Protect,
            CraftExplanation = u.CraftExplanation ?? string.Empty,
            SummaryLine = kindLabel
        };
    }

    /// <summary>
    /// Issue #90 design section 10 "The cutoff" (ruling of 2026-07-15, step 10): the shipped chat
    /// sidebar sends through Semantic Kernel to the Worker's /v1/chat/completions, which refuses
    /// with 429 (before any upstream dispatch) when the caller's balance is at or below zero.
    /// Semantic Kernel wraps a non-success HTTP response in <see cref="HttpOperationException"/>
    /// (its <see cref="HttpOperationException.StatusCode"/> carries the code); this recognizes the
    /// 429 shape and translates it to <see cref="OutOfCreditsException"/> so
    /// WorkflowViewModel.SendButtonClicked's <c>ChatMessage.Error(ex.Message)</c> shows a message
    /// naming the credits screen instead of the raw HTTP exception text. Every other exception
    /// passes through unchanged. internal static and side-effect-free, so it is testable without a
    /// live kernel call: construct an HttpOperationException directly (its public 4-arg
    /// constructor takes the status code) and assert on the returned exception's type/message.
    /// </summary>
    internal static Exception TranslateChatException(Exception ex) =>
        ex is HttpOperationException { StatusCode: HttpStatusCode.TooManyRequests }
            ? new OutOfCreditsException()
            : ex;

    /// <summary>
    /// Builds a readable text context from gathered story elements for the chat system message.
    /// </summary>
    private string BuildElementContext(Dictionary<string, StoryElement> elements)
    {
        if (elements == null || elements.Count == 0)
            return "No story elements available yet.";

        var sb = new System.Text.StringBuilder();

        foreach (var (label, element) in elements)
        {
            if (element == null) continue;

            sb.AppendLine($"### {label}: {element.Name}");

            // Add key properties based on element type
            switch (element)
            {
                case OverviewModel overview:
                    if (!string.IsNullOrWhiteSpace(overview.Description))
                        sb.AppendLine($"- Story Idea: {overview.Description}");
                    if (!string.IsNullOrWhiteSpace(overview.Concept))
                        sb.AppendLine($"- Concept: {overview.Concept}");
                    if (!string.IsNullOrWhiteSpace(overview.Premise))
                        sb.AppendLine($"- Premise: {overview.Premise}");
                    if (!string.IsNullOrWhiteSpace(overview.StoryGenre))
                        sb.AppendLine($"- Genre: {overview.StoryGenre}");
                    break;

                case ProblemModel problem:
                    if (!string.IsNullOrWhiteSpace(problem.ProblemType))
                        sb.AppendLine($"- Problem Type: {problem.ProblemType}");
                    if (!string.IsNullOrWhiteSpace(problem.Description))
                        sb.AppendLine($"- Description: {problem.Description}");
                    if (!string.IsNullOrWhiteSpace(problem.ProtGoal))
                        sb.AppendLine($"- Protagonist Goal: {problem.ProtGoal}");
                    if (!string.IsNullOrWhiteSpace(problem.ProtMotive))
                        sb.AppendLine($"- Protagonist Motive: {problem.ProtMotive}");
                    if (!string.IsNullOrWhiteSpace(problem.ProtConflict))
                        sb.AppendLine($"- Protagonist Conflict: {problem.ProtConflict}");
                    if (!string.IsNullOrWhiteSpace(problem.AntagGoal))
                        sb.AppendLine($"- Antagonist Goal: {problem.AntagGoal}");
                    if (!string.IsNullOrWhiteSpace(problem.Premise))
                        sb.AppendLine($"- Premise: {problem.Premise}");
                    break;

                case CharacterModel character:
                    if (!string.IsNullOrWhiteSpace(character.Role))
                        sb.AppendLine($"- Role: {character.Role}");
                    if (!string.IsNullOrWhiteSpace(character.Archetype))
                        sb.AppendLine($"- Archetype: {character.Archetype}");
                    if (!string.IsNullOrWhiteSpace(character.Description))
                        sb.AppendLine($"- Description: {character.Description}");
                    break;

                default:
                    // For other element types, just include description if available
                    if (!string.IsNullOrWhiteSpace(element.Description))
                        sb.AppendLine($"- Description: {element.Description}");
                    break;
            }

            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Executes the workflow and provides feedback to the user via the conversation list.
    /// </summary>
    private async Task ExecuteWorkflowWithFeedback(
        StoryCADLib.Collaborator.ViewModels.WorkflowViewModel viewModel,
        Workflow workflow,
        Dictionary<string, StoryElement> gatheredElements)
    {
        try
        {
            // Show progress
            viewModel.AddStatusMessage($"Running {workflow.Title}...");
            viewModel.ProgressVisibility = Microsoft.UI.Xaml.Visibility.Visible;

            // Execute via WorkflowRunner
            var runnerLogger = _loggerFactory?.CreateLogger<WorkflowRunner>();
            var runner = new WorkflowRunner(_storyModel!, workflow, _storyApi!, runnerLogger, _settings, _auditLogger);
            _auditLogger?.Log(StoryCADLib.Services.Logging.LogLevel.Info,
                $"Workflow started: {workflow.Title} with {gatheredElements.Count} elements");
            var result = await runner.RunAsync(gatheredElements);

            // #116: classify scalars against live outline + session-touch (after extract/enrich).
            if (result.Success)
                runner.ClassifyScalarUpdates(result, _sessionTouchedFields, workflow.Label);

            // Hide progress
            viewModel.ProgressVisibility = Microsoft.UI.Xaml.Visibility.Collapsed;

            if (result.Success)
            {
                // Show status messages rolled up (omit noisy per-field classify lines)
                foreach (var msg in result.StatusMessages)
                {
                    if (msg.StartsWith("Classified ", StringComparison.Ordinal)
                        || msg.StartsWith("No-op ", StringComparison.Ordinal))
                        continue;
                    viewModel.AddStatusMessage(msg);
                }
                viewModel.AddStatusMessage("Workflow completed successfully.");

                // Add AI explanation if available in raw response
                if (!string.IsNullOrEmpty(result.RawResponse))
                {
                    var explanation = ExtractExplanationFromResponse(result.RawResponse);
                    if (!string.IsNullOrEmpty(explanation))
                    {
                        viewModel.ConversationList.Add(ChatMessage.FromCollaborator(explanation));
                    }
                }

                // If there are property updates, populate the pending updates panel
                if (result.PendingUpdates.Count > 0)
                {
                    PushPendingToViewModel(viewModel, result);

                    // #140 / #116 rev: Protect accepts are staged until end-of-queue confirm.
                    var stageSession = new OverwriteAcceptanceSession();

                    int ApplyPendingList(IReadOnlyList<PendingUpdate> list)
                    {
                        if (list.Count == 0) return 0;
                        var slice = WorkflowResult.Succeeded();
                        foreach (var u in list)
                            slice.PendingUpdates.Add(u);
                        var applied = runner.ApplyUpdates(slice, gatheredElements);
                        foreach (var u in list)
                            _sessionTouchedFields.Add(u.SessionTouchKey);
                        return applied;
                    }

                    void RemovePendingKeys(IEnumerable<string> keys)
                    {
                        var set = keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
                        result.PendingUpdates.RemoveAll(u => set.Contains(u.Key));
                        foreach (var key in set)
                            result.UpdatedProperties.Remove(key);
                    }

                    void FinishPendingUi()
                    {
                        // Keep full session set visible (skipped/accepted stay on the left).
                        if (_sessionProposals != null && _sessionProposals.Count > 0)
                            PushSessionSetToViewModel(viewModel);
                        else if (result.PendingUpdates.Count == 0 && !stageSession.HasStaged)
                        {
                            viewModel.MarkUpdatesApplied();
                            SyncShellPending(viewModel);
                        }
                        else
                            PushPendingToViewModel(viewModel, result);
                        _storyModel?.RefreshCurrentView();
                    }

                    void AutoSaveFireAndForget(string reason)
                    {
                        _ = Task.Run(async () =>
                        {
                            var saved = await SaveAsync();
                            if (saved)
                                _logger?.LogInformation("Auto-save completed after {Reason}", reason);
                        });
                    }

                    async Task FlushStagedIfQueueDoneAsync()
                    {
                        if (!stageSession.ShouldConfirmStaged(result.PendingUpdates.Count))
                            return;

                        var staged = stageSession.StagedProtect.ToList();
                        var ok = await ConfirmOverwriteAsync(staged);
                        if (ok)
                        {
                            var n = ApplyPendingList(staged);
                            foreach (var u in staged)
                                _sessionProposals?.MarkAccepted(u.Key);
                            viewModel.ConversationList.Add(ChatMessage.FromCollaborator(
                                n > 0
                                    ? $"Applied {n} overwrite(s) after confirmation."
                                    : "No overwrites were applied."));
                            _logger?.LogInformation("StagedProtect confirmed: Applied {Count}", n);
                            _auditLogger?.Log(StoryCADLib.Services.Logging.LogLevel.Info,
                                $"Applied {n} confirmed overwrites from workflow: {workflow.Title}");
                            stageSession.ClearStage();
                            RefreshProposalSnapshotInHistory();
                            AutoSaveFireAndForget("confirmed overwrites");
                        }
                        else
                        {
                            viewModel.ConversationList.Add(ChatMessage.FromCollaborator(
                                $"Cancelled: left {staged.Count} existing field(s) unchanged."));
                            _logger?.LogInformation("StagedProtect cancelled: {Count}", staged.Count);
                            stageSession.ClearStage();
                        }

                        FinishPendingUi();
                    }

                    // Wire up command callbacks using closures (captures local state)
                    viewModel.OnAcceptAll = async () =>
                    {
                        try
                        {
                            var (free, protect) = OverwriteAcceptanceSession.Partition(result.PendingUpdates);
                            stageSession.ClearStage();

                            var applyList = new List<PendingUpdate>(free);
                            if (protect.Count > 0)
                            {
                                if (!await ConfirmOverwriteAsync(protect))
                                {
                                    // Cancel overwrites: still apply free rows; leave Protect pending.
                                    var freeOnly = ApplyPendingList(free);
                                    foreach (var u in free)
                                        _sessionProposals?.MarkAccepted(u.Key);
                                    RemovePendingKeys(free.Select(u => u.Key));
                                    viewModel.ConversationList.Add(ChatMessage.FromCollaborator(
                                        freeOnly > 0
                                            ? $"Applied {freeOnly} free update(s). Left {protect.Count} field(s) with existing text unchanged."
                                            : $"Left {protect.Count} field(s) with existing text unchanged."));
                                    _logger?.LogInformation(
                                        "AcceptAll cancelled overwrites: free={Free} protect={Protect}",
                                        freeOnly, protect.Count);
                                    RefreshProposalSnapshotInHistory();
                                    FinishPendingUi();
                                    if (freeOnly > 0)
                                        AutoSaveFireAndForget("Accept All free-only");
                                    return;
                                }

                                applyList.AddRange(protect);
                            }

                            var count = ApplyPendingList(applyList);
                            foreach (var u in applyList)
                                _sessionProposals?.MarkAccepted(u.Key);
                            result.PendingUpdates.Clear();
                            result.UpdatedProperties.Clear();

                            var sb = new System.Text.StringBuilder();
                            if (count > 0)
                            {
                                sb.AppendLine($"Applied {count} update(s) to your outline:");
                                sb.AppendLine();
                                foreach (var u in applyList)
                                {
                                    var valuePreview = TruncateForChat(FormatValueForDisplay(u.Value));
                                    sb.AppendLine($"**{u.Key}**: {valuePreview}");
                                    sb.AppendLine();
                                }
                            }
                            else
                                sb.AppendLine("No updates to apply.");

                            viewModel.ConversationList.Add(ChatMessage.FromCollaborator(sb.ToString().TrimEnd()));
                            _logger?.LogInformation(
                                "AcceptAll: Applied {Count} (free={Free} protect={Protect})",
                                count, free.Count, protect.Count);
                            _auditLogger?.Log(StoryCADLib.Services.Logging.LogLevel.Info,
                                $"Applied {count} updates from workflow: {workflow.Title}");

                            PushSessionSetToViewModel(viewModel);
                            RefreshProposalSnapshotInHistory();
                            _storyModel?.RefreshCurrentView();
                            if (count > 0)
                                AutoSaveFireAndForget("Accept All");
                        }
                        catch (Exception ex)
                        {
                            viewModel.ConversationList.Add(ChatMessage.Error($"Error applying updates: {ex.Message}"));
                            _logger?.LogError(ex, "Error in AcceptAll handler");
                        }
                    };

                    viewModel.OnTryAgain = async () =>
                    {
                        try
                        {
                            stageSession.ClearStage();
                            viewModel.ClearPendingUpdates();
                            viewModel.AddStatusMessage("Re-running workflow...");
                            await ExecuteWorkflowWithFeedback(viewModel, workflow, gatheredElements);
                        }
                        catch (Exception ex)
                        {
                            viewModel.ConversationList.Add(ChatMessage.Error($"Error re-running workflow: {ex.Message}"));
                            _logger?.LogError(ex, "Error in TryAgain handler");
                        }
                    };

                    viewModel.OnAcceptProperty = async (propertyKey) =>
                    {
                        if (string.IsNullOrEmpty(propertyKey))
                        {
                            _logger?.LogWarning("AcceptProperty called with empty key");
                            return;
                        }

                        try
                        {
                            var pending = result.PendingUpdates
                                .FirstOrDefault(u => string.Equals(u.Key, propertyKey, StringComparison.OrdinalIgnoreCase));
                            if (pending == null)
                            {
                                _logger?.LogWarning("Property key not found in pending updates: {Key}", propertyKey);
                                return;
                            }

                            if (pending.Kind == UpdateKind.Protect)
                            {
                                // Stage overwrite; confirm once when the queue is fully decided (#140).
                                stageSession.StageProtect(pending);
                                RemovePendingKeys(new[] { propertyKey });
                                viewModel.AddStatusMessage($"Queued overwrite: {propertyKey}");
                                _logger?.LogInformation("AcceptProperty: Staged Protect {Key}", propertyKey);
                                PushSessionSetToViewModel(viewModel);
                                await FlushStagedIfQueueDoneAsync();
                                // Staged confirm marks accepted when applied
                                return;
                            }

                            var applied = ApplyPendingList(new[] { pending });
                            if (applied > 0)
                            {
                                viewModel.AddStatusMessage($"Applied {propertyKey}");
                                _logger?.LogInformation("AcceptProperty: Applied {Key}", propertyKey);
                                _auditLogger?.Log(StoryCADLib.Services.Logging.LogLevel.Info,
                                    $"Applied update: {propertyKey}");
                                _sessionProposals?.MarkAccepted(propertyKey);
                                RemovePendingKeys(new[] { propertyKey });
                                PushSessionSetToViewModel(viewModel);
                                RefreshProposalSnapshotInHistory();
                                _storyModel?.RefreshCurrentView();
                                await FlushStagedIfQueueDoneAsync();
                                // Save on each confirmed update, not just the last one in the
                                // queue: there is no manual Save button to fall back on.
                                AutoSaveFireAndForget("AcceptProperty");
                            }
                            else
                            {
                                viewModel.ConversationList.Add(
                                    ChatMessage.Error($"Failed to apply {propertyKey}"));
                                _logger?.LogWarning("Failed to apply {Key}", propertyKey);
                            }
                        }
                        catch (Exception ex)
                        {
                            viewModel.ConversationList.Add(ChatMessage.Error($"Error applying {propertyKey}: {ex.Message}"));
                            _logger?.LogError(ex, "Error in AcceptProperty handler for {Key}", propertyKey);
                        }
                    };

                    viewModel.OnSkipProperty = async (propertyKey) =>
                    {
                        if (string.IsNullOrEmpty(propertyKey))
                        {
                            _logger?.LogWarning("SkipProperty called with empty key");
                            return;
                        }

                        try
                        {
                            var removed = result.PendingUpdates.RemoveAll(u =>
                                string.Equals(u.Key, propertyKey, StringComparison.OrdinalIgnoreCase));
                            result.UpdatedProperties.Remove(propertyKey);
                            if (removed > 0)
                            {
                                _sessionProposals?.MarkSkipped(propertyKey);
                                viewModel.AddStatusMessage($"Skipped {propertyKey}");
                                _logger?.LogInformation("SkipProperty: Skipped {Key}", propertyKey);
                                PushSessionSetToViewModel(viewModel);
                                RefreshProposalSnapshotInHistory();
                                await FlushStagedIfQueueDoneAsync();
                            }
                            else
                            {
                                _logger?.LogWarning("Property key not found for skip: {Key}", propertyKey);
                            }
                        }
                        catch (Exception ex)
                        {
                            viewModel.ConversationList.Add(ChatMessage.Error($"Error skipping {propertyKey}: {ex.Message}"));
                            _logger?.LogError(ex, "Error in SkipProperty handler for {Key}", propertyKey);
                        }
                    };

                    // Accept Remaining: free apply now; remaining Protect staged → end confirm if queue empty.
                    viewModel.OnAcceptRemainingFree = async () =>
                    {
                        try
                        {
                            var (free, protect) = OverwriteAcceptanceSession.Partition(result.PendingUpdates);
                            var freeCount = ApplyPendingList(free);
                            RemovePendingKeys(free.Select(u => u.Key));

                            foreach (var u in protect)
                                stageSession.StageProtect(u);
                            RemovePendingKeys(protect.Select(u => u.Key));

                            if (freeCount > 0)
                            {
                                viewModel.ConversationList.Add(ChatMessage.FromCollaborator(
                                    $"Applied {freeCount} free update(s)."));
                            }

                            PushPendingToViewModel(viewModel, result);
                            _storyModel?.RefreshCurrentView();
                            await FlushStagedIfQueueDoneAsync();

                            if (result.PendingUpdates.Count == 0 && !stageSession.HasStaged)
                            {
                                viewModel.MarkUpdatesApplied();
                                SyncShellPending(viewModel);
                            }

                            if (freeCount > 0)
                                AutoSaveFireAndForget("Accept Remaining free");
                        }
                        catch (Exception ex)
                        {
                            viewModel.ConversationList.Add(ChatMessage.Error($"Error applying remaining: {ex.Message}"));
                            _logger?.LogError(ex, "Error in AcceptRemainingFree handler");
                        }
                    };

                    var fillCount = result.PendingUpdates.Count(u => u.Kind is UpdateKind.Fill or UpdateKind.Refresh or UpdateKind.Unclassified);
                    var protectCount = result.PendingUpdates.Count(u => u.Kind == UpdateKind.Protect);
                    // #145: clear chat, seed proposal set, unlock Send; show full set on the left
                    BeginProposalChatSession(viewModel, workflow, result);
                    PushSessionSetToViewModel(viewModel);
                    viewModel.ConversationList.Add(ChatMessage.FromCollaborator(
                        $"Found {result.PendingUpdates.Count} property update(s) " +
                        $"({fillCount} free, {protectCount} replace existing text — confirmation required). " +
                        "Choose Accept All, Review Each, or Try Again. Chat can revise these proposals."));
                }
                else
                {
                    viewModel.IsChatEnabled = false;
                    viewModel.ChatPlaceholder = "No proposals to edit in chat";
                    viewModel.ConversationList.Add(ChatMessage.FromCollaborator("No property updates were extracted from the response."));
                }
            }
            else
            {
                viewModel.ConversationList.Add(ChatMessage.Error(result.ErrorMessage ?? "Unknown error"));
                foreach (var msg in result.StatusMessages)
                {
                    viewModel.AddStatusMessage(msg);
                }
            }

            _logger?.LogInformation("Workflow {Workflow} completed. Success: {Success}",
                workflow.Title, result.Success);
        }
        catch (Exception ex)
        {
            viewModel.ProgressVisibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            viewModel.ConversationList.Add(ChatMessage.Error($"Error executing workflow: {ex.Message}"));
            _logger?.LogError(ex, "Error executing workflow {Workflow}", workflow.Title);
        }
    }

    /// <summary>
    /// #140: confirm before applying Protect (non-empty field) overwrites.
    /// Headless / no XamlRoot → false (do not silent-overwrite).
    /// </summary>
    internal async Task<bool> ConfirmOverwriteAsync(IReadOnlyList<PendingUpdate> protect)
    {
        if (protect == null || protect.Count == 0)
            return true;

        var xamlRoot = _hostFrame?.XamlRoot;
        if (xamlRoot == null)
        {
            _logger?.LogWarning(
                "ConfirmOverwriteAsync: no XamlRoot; refusing {Count} Protect overwrite(s)",
                protect.Count);
            return false;
        }

        var dialog = new ContentDialog
        {
            Title = "Replace existing content?",
            Content = OverwriteAcceptanceSession.BuildConfirmMessage(protect),
            PrimaryButtonText = "Replace",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    /// <summary>
    /// Pushes classified pending updates into the workflow panel (issue #116)
    /// and enables shell Accept All / Review Each / Try Again.
    /// </summary>
    private void PushPendingToViewModel(
        StoryCADLib.Collaborator.ViewModels.WorkflowViewModel viewModel,
        WorkflowResult result)
    {
        var items = result.PendingUpdates.Select(ToPendingUpdateItem).ToList();
        viewModel.SetPendingUpdates(items);
        SyncShellPending(viewModel);
    }

    /// <summary>
    /// Routes top-bar Accept All / Review Each / Try Again to the active page VM.
    /// </summary>
    private void WireShellWorkflowActions(WorkflowViewModel pageVm)
    {
        if (_shellViewModel == null) return;

        _shellViewModel.OnAcceptAll = () => pageVm.AcceptAllCommand.Execute(null);
        _shellViewModel.OnReviewEach = () => pageVm.ReviewEachCommand.Execute(null);
        _shellViewModel.OnTryAgain = async () =>
        {
            if (pageVm.OnTryAgain != null)
                await pageVm.OnTryAgain();
            else
                pageVm.TryAgainCommand.Execute(null);
        };
        SyncShellPending(pageVm);
    }

    private void SyncShellPending(WorkflowViewModel pageVm)
    {
        if (_shellViewModel != null)
            _shellViewModel.HasPendingUpdates = pageVm.HasPendingUpdates;
    }

    private PendingUpdateItem ToPendingUpdateItem(PendingUpdate u)
    {
        var proposed = TruncateForChat(FormatValueForDisplay(u.Value), 300);
        var current = TruncateForChat(u.CurrentDisplay ?? string.Empty, 300);
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
            PropertyDisplayName = ValueDisplay.SplitPascalCase(u.Spec.Property),
            ProposedDisplay = string.IsNullOrEmpty(proposed) ? "(empty)" : proposed,
            CurrentDisplay = current,
            KindLabel = kindLabel,
            IsProtected = u.Kind == UpdateKind.Protect,
            CraftExplanation = u.CraftExplanation,
            SummaryLine = summary
        };
    }

    /// <summary>
    /// Readable text for a typed pending-update value; lists render per entry and
    /// element GUIDs resolve to outline names (#129).
    /// </summary>
    private string FormatValueForDisplay(object? value) =>
        ValueDisplay.Format(value, guid =>
            _storyModel?.StoryElements?.StoryElementGuids != null
            && _storyModel.StoryElements.StoryElementGuids.TryGetValue(guid, out var element)
                ? element?.Name
                : null);

    private static string TruncateForChat(string? text, int max = 200)
    {
        var value = text ?? "(empty)";
        if (value.Length > max)
            return value.Substring(0, max) + "...";
        return value;
    }

    /// <summary>
    /// Extracts the explanation field from a JSON AI response.
    /// </summary>
    private string? ExtractExplanationFromResponse(string response)
    {
        try
        {
            var jsonStart = response.IndexOf("{");
            var jsonEnd = response.LastIndexOf("}");
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonText = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                using var doc = System.Text.Json.JsonDocument.Parse(jsonText);

                if (doc.RootElement.TryGetProperty("explanation", out var explanation))
                {
                    return explanation.GetString();
                }
            }
        }
        catch
        {
            // Ignore parse errors - explanation is optional
        }
        return null;
    }

    /// <summary>
    /// Result of gathering workflow inputs - includes both elements and status messages.
    /// </summary>
    private class GatherResult
    {
        public Dictionary<string, StoryElement> Elements { get; set; } = new();
        public List<string> StatusMessages { get; set; } = new();
        public bool Cancelled { get; set; }
    }

    /// <summary>
    /// Short chrome label from registry Label (e.g. StoryProblem → "Story Problem").
    /// Not the long Title path used in the nav list.
    /// </summary>
    private static Microsoft.UI.Xaml.Controls.NavigationViewItem? FindWorkflowNavItem(
        WorkflowShellViewModel shellViewModel,
        string workflowLabel)
    {
        foreach (var top in shellViewModel.MenuItems)
        {
            if (top.Tag is Workflow w &&
                string.Equals(w.Label, workflowLabel, StringComparison.OrdinalIgnoreCase))
                return top;

            foreach (var child in top.MenuItems.OfType<Microsoft.UI.Xaml.Controls.NavigationViewItem>())
            {
                if (child.Tag is Workflow cw &&
                    string.Equals(cw.Label, workflowLabel, StringComparison.OrdinalIgnoreCase))
                    return child;
            }
        }

        return null;
    }

    private static string FormatWorkflowShortName(string? label) =>
        ValueDisplay.SplitPascalCase(label);

    /// <summary>Nav group header for a workflow's primary element type (#129).</summary>
    private static string GroupTitle(StoryItemType type) => type switch
    {
        StoryItemType.StoryOverview => "Overview",
        StoryItemType.Unknown => "Other",
        _ => ValueDisplay.SplitPascalCase(type.ToString())
    };

    /// <summary>
    /// One-line shell status from gather messages (skip section headers). #123
    /// </summary>
    private static string FormatGatherStatusForShell(List<string> statusMessages)
    {
        if (statusMessages == null || statusMessages.Count == 0)
            return "Selection cancelled.";

        // Prefer the last non-header line (e.g. "Cancelled: Protagonist is required.")
        for (var i = statusMessages.Count - 1; i >= 0; i--)
        {
            var m = statusMessages[i];
            if (string.IsNullOrWhiteSpace(m) || m.StartsWith("---", StringComparison.Ordinal))
                continue;
            return m;
        }

        return "Selection cancelled.";
    }

    /// <summary>
    /// Gathers input elements for a workflow. Uses ElementPicker dialogs for required inputs,
    /// and for optional inputs follows Guid references with picker fallback if not set.
    /// Collects status messages for display in chat interface.
    /// </summary>
    /// <param name="workflow">The workflow to gather inputs for</param>
    /// <param name="xamlRoot">XamlRoot for showing dialogs</param>
    /// <returns>GatherResult with elements, status messages, and cancelled flag</returns>
    private async Task<GatherResult> GatherWorkflowInputsAsync(
        Workflow workflow,
        Microsoft.UI.Xaml.XamlRoot xamlRoot)
    {
        var result = new GatherResult();
        var workflowIO = workflow.GetIO();

        // Add section header
        result.StatusMessages.Add("--- Gathering input elements ---");

        // Phase 1: Gather required inputs via ElementPicker
        foreach (var requirement in workflowIO.RequiredInputs)
        {
            var gathered = await GatherElementAsync(requirement, xamlRoot, result.Elements, result.StatusMessages, isRequired: true);
            if (gathered == null && !requirement.CreateIfMissing)
            {
                // User cancelled a required element
                result.Cancelled = true;
                result.StatusMessages.Add($"Cancelled: {requirement.ElementLabel} is required.");
                return result;
            }
        }

        // Phase 2: Gather optional inputs - try reference first, then picker fallback
        foreach (var requirement in workflowIO.OptionalInputs)
        {
            // Skip if already gathered (e.g., duplicate label in required)
            if (result.Elements.ContainsKey(requirement.ElementLabel))
                continue;

            await GatherElementAsync(requirement, xamlRoot, result.Elements, result.StatusMessages, isRequired: false);
        }

        return result;
    }

    /// <summary>
    /// Gathers a single element based on its requirement.
    /// Uses ElementResolver for auto-resolution (StoryOverview, referenced elements).
    /// For required inputs: uses auto-resolved element if available, otherwise shows picker.
    /// For optional inputs: always shows picker with current selection pre-selected.
    /// </summary>
    private async Task<StoryElement?> GatherElementAsync(
        ElementRequirement requirement,
        Microsoft.UI.Xaml.XamlRoot xamlRoot,
        Dictionary<string, StoryElement> gatheredElements,
        List<string> statusMessages,
        bool isRequired)
    {
        // Try auto-resolution via ElementResolver (handles StoryOverview and other singletons)
        var autoResolved = _elementResolver?.ResolveRequirement(requirement, gatheredElements);
        if (autoResolved != null)
        {
            gatheredElements[requirement.ElementLabel] = autoResolved;
            statusMessages.Add($"Using {requirement.ElementLabel}: {autoResolved.Name}");
            _logger?.LogDebug("Auto-resolved {Label} to '{Name}'",
                requirement.ElementLabel, autoResolved.Name);
            return autoResolved;
        }

        // Try to get referenced element for pre-selection (e.g., "Problem.Protagonist")
        StoryElement? currentElement = null;
        Guid? currentGuid = null;
        if (!string.IsNullOrEmpty(requirement.ReferencedElementLabel))
        {
            currentElement = _elementResolver?.GetReferencedElement(requirement, gatheredElements);
            if (currentElement != null)
            {
                currentGuid = currentElement.Uuid;

                // For REQUIRED inputs, use existing reference without prompting
                if (isRequired)
                {
                    gatheredElements[requirement.ElementLabel] = currentElement;
                    statusMessages.Add(
                        $"Found {requirement.ElementLabel}: {currentElement.Name} (from {requirement.ReferencedElementLabel})");
                    _logger?.LogDebug("Resolved {Label} via {Ref} to '{Name}'",
                        requirement.ElementLabel, requirement.ReferencedElementLabel, currentElement.Name);
                    if (string.Equals(
                            requirement.ReferencedElementLabel, "Overview.StoryProblem",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        ApplyStoryProblemCategory(currentElement.Uuid, statusMessages);
                    }
                    return currentElement;
                }

                // For OPTIONAL inputs, we'll show picker with pre-selection
                _logger?.LogDebug("Found current {Label}: '{Name}', showing picker for potential change",
                    requirement.ElementLabel, currentElement.Name);
            }
            else
            {
                // Reference empty/missing — user-visible (#123); then picker for required
                // (or optional) so we never run with a silent empty character.
                statusMessages.Add(
                    $"{requirement.ReferencedElementLabel} is not set — select {requirement.ElementLabel}.");
                _logger?.LogDebug("Reference {Ref} empty, will prompt for {Label}",
                    requirement.ReferencedElementLabel, requirement.ElementLabel);
            }
        }

        // Show ElementPicker - pass API so Create works; currentGuid for pre-selection
        var pickerVM = new ElementPickerVM();
        var selectedGuid = await pickerVM.ShowPicker(_storyModel!, xamlRoot,
            requirement.ElementType, requirement.ElementLabel, currentGuid, _storyApi);

        // WinUI: sequential ContentDialogs can swallow the next ShowAsync if the previous
        // dialog has not fully torn down. Brief yield so Problem → Protagonist → Antagonist
        // each actually appear.
        await Task.Delay(100);

        if (string.IsNullOrEmpty(selectedGuid))
        {
            // User cancelled
            if (isRequired)
            {
                statusMessages.Add($"{requirement.ElementLabel}: selection cancelled");
            }
            else
            {
                statusMessages.Add($"{requirement.ElementLabel}: not selected (optional)");
            }
            _logger?.LogDebug("User cancelled selection for {Label}", requirement.ElementLabel);
            return null;
        }

        // Look up the selected element
        if (Guid.TryParse(selectedGuid, out var guid))
        {
            var result = _storyApi?.GetStoryElement(guid);
            if (result?.IsSuccess == true && result.Payload != null)
            {
                var element = result.Payload;
                gatheredElements[requirement.ElementLabel] = element;
                statusMessages.Add($"Selected {requirement.ElementLabel}: {element.Name}");
                _logger?.LogDebug("User selected {Type} '{Name}' for {Label}",
                    element.ElementType, element.Name, requirement.ElementLabel);

                // If this was a reference fallback, update the source element's Guid property
                if (!string.IsNullOrEmpty(requirement.ReferencedElementLabel))
                {
                    UpdateReferenceProperty(requirement.ReferencedElementLabel, gatheredElements, element.Uuid, statusMessages);
                }

                return element;
            }
        }

        _logger?.LogWarning("Could not find element with GUID {Guid}", selectedGuid);
        return null;
    }

    /// <summary>
    /// Updates a Guid reference property on a source element.
    /// For example, "Problem.Protagonist" sets Problem.Protagonist = pickedElementGuid.
    /// </summary>
    private void UpdateReferenceProperty(
        string referencedElementLabel,
        Dictionary<string, StoryElement> gatheredElements,
        Guid pickedElementGuid,
        List<string> statusMessages)
    {
        var parts = referencedElementLabel.Split('.');
        if (parts.Length != 2)
        {
            _logger?.LogWarning("Invalid reference format for update: {Reference}", referencedElementLabel);
            return;
        }

        var sourceLabel = parts[0];
        var propertyName = parts[1];

        if (!gatheredElements.TryGetValue(sourceLabel, out var sourceElement))
        {
            _logger?.LogWarning("Source element '{Label}' not found for property update", sourceLabel);
            return;
        }

        var result = _storyApi?.UpdateElementProperty(sourceElement.Uuid, propertyName, pickedElementGuid);
        if (result?.IsSuccess == true)
        {
            statusMessages.Add($"  (Updated {sourceLabel}.{propertyName})");
            _logger?.LogDebug("Updated {Source}.{Property} = {Guid}", sourceLabel, propertyName, pickedElementGuid);

            // Linking Overview.StoryProblem means this Problem is the main story problem —
            // ProblemCategory is that structural fact (Lists.json), not an Accept/Protect field.
            if (string.Equals(sourceLabel, "Overview", StringComparison.OrdinalIgnoreCase)
                && string.Equals(propertyName, "StoryProblem", StringComparison.OrdinalIgnoreCase))
            {
                ApplyStoryProblemCategory(pickedElementGuid, statusMessages);
            }
        }
        else
        {
            _logger?.LogWarning("Failed to update {Source}.{Property}: {Error}",
                sourceLabel, propertyName, result?.ErrorMessage);
        }
    }

    /// <summary>
    /// Exact Lists.json ProblemCategory value when a Problem is the Overview StoryProblem.
    /// </summary>
    internal const string StoryProblemCategoryListValue = "Story problem";

    /// <summary>
    /// Writes Problem.ProblemCategory = "Story problem" immediately (not pending / not Accept).
    /// Call whenever Overview.StoryProblem is linked to this Problem.
    /// </summary>
    internal void ApplyStoryProblemCategory(Guid problemGuid, List<string>? statusMessages = null)
    {
        var catResult = _storyApi?.UpdateElementProperty(
            problemGuid, "ProblemCategory", StoryProblemCategoryListValue);
        if (catResult?.IsSuccess == true)
        {
            statusMessages?.Add($"  (Set Problem.ProblemCategory = {StoryProblemCategoryListValue})");
            _logger?.LogDebug(
                "Set Problem {Guid} ProblemCategory = {Category}",
                problemGuid, StoryProblemCategoryListValue);
        }
        else
        {
            _logger?.LogWarning(
                "Failed to set ProblemCategory on {Guid}: {Error}",
                problemGuid, catResult?.ErrorMessage);
        }
    }

    /// <summary>
    /// Ensures Semantic Kernel is initialized. Thread-safe, only initializes once.
    /// Called lazily when kernel is actually needed (not in constructor).
    /// </summary>
    private void EnsureKernelInitialized()
    {
        if (_kernelInitialized) return;

        lock (_kernelLock)
        {
            if (_kernelInitialized) return;

            ILoggerFactory? loggerFactory = null;
            try
            {
                loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.SetMinimumLevel(LogLevel.Warning); // SK is verbose, only log warnings+
                    if (NLog.LogManager.Configuration != null)
                    {
                        builder.AddNLog(NLog.LogManager.Configuration);
                    }
                });
            }
            catch
            {
                loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.SetMinimumLevel(LogLevel.Warning);
                });
            }

            // Single construction site: delegate to the shared factory, which also
            // logs the active path (direct vs proxy, endpoint host) per D6.
            _kernel = KernelInitializer.EnsureBuilt(loggerFactory);
            _chatService = _kernel.GetRequiredService<IChatCompletionService>();
            _kernelInitialized = true;

            _logger?.LogInformation("Semantic Kernel initialized");
        }
    }
}
