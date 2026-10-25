using System.Linq;
using System.Net;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using NLog.Extensions.Logging;
using StoryCADLib.Models;
using StoryCADLib.Services;
using StoryCADLib.Services.Collaborator;
using StoryCADLib.Services.Collaborator.Contracts;
using StoryCADLib.Services.Store;
using StoryCollaborator.Services;
using StoryCollaborator.Models;
using StoryCollaborator.Workflows;
using CollaboratorLib.Context;
using StoryCADLib.Collaborator;
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

    /// <summary>The session logic. The window and the MCP server call the same service.</summary>
    private WorkflowSessionService? _service;

    /// <summary>The session of the workflow page on screen.</summary>
    private WorkflowSession? _session;

    // State
    private IStoryCADAPI? _storyApi;
    private StoryModel? _storyModel;
    private string? _filePath;
    private Window? _hostWindow;
    private Frame? _hostFrame;
    private WorkflowShellViewModel? _shellViewModel;
    private bool _disposed;
    private StoryCADLib.Services.Logging.ILogService? _auditLogger;

    /// <summary>
    /// Issue #116: fields Collaborator successfully wrote this plugin session
    /// (SessionTouchKey = "{uuid:N}.{Property}"). Not persisted across app restarts.
    /// Collaborator #272: each key maps to the fingerprint of the value Collaborator wrote
    /// (<see cref="WorkflowRunner.ReadTouchFingerprint"/>), so a later writer edit is not
    /// mistaken for Collaborator's own text.
    /// </summary>
    private readonly Dictionary<string, string> _sessionTouchedFields =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Per-run cost accumulator for the shell's developer-build cost line. Session-scoped
    /// like <see cref="_sessionTouchedFields"/>; reset in OpenAsync so a second session in
    /// one process starts from zero.
    /// </summary>
    private readonly WorkflowCostTracker _costTracker = new();

    // Settings
    private CollaboratorSettings _settings = CollaboratorSettings.Default;

    /// <summary>
    /// Labels of the workflows shown in the pane's starred band. Loaded from preferences on open
    /// and kept in step with every star edit, so a menu rebuild does not need a disk read.
    /// </summary>
    private List<string> _starredWorkflows = new();

    // Debug control - initialized from env var, tests can override directly
    internal static bool CollabDebug =
        Environment.GetEnvironmentVariable("COLLAB_DEBUG") == "1";

    public Collaborator()
    {
        // Note: Workflows are registered via WorkflowRegistry.All static initializer
        // Semantic Kernel is initialized lazily in WorkflowSessionService.EnsureChatService() to avoid
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
    /// | Workflow start     | Info  | "Workflow started: {title} with {count} elements"          | RunAsync          |
    /// | Accept All         | Info  | "Applied {count} updates from workflow: {title}"           | AcceptAllAsync    |
    /// | Accept Property    | Info  | "Applied update: {propertyKey}"                            | AcceptAsync       |
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
        _sessionService = new SessionService(_loggerFactory.CreateLogger<SessionService>());
        _service = new WorkflowSessionService(
            api, _loggerFactory.CreateLogger<WorkflowSessionService>(), _settings, _loggerFactory, logger);
        _session = null;

        // Initialize Semantic Kernel lazily (expensive operation, ~7 min if done in constructor)
        _service.EnsureChatService();

        _sessionService.StartSession();
        _costTracker.Reset();

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
                // Stars decide which workflows sit in the top band, so they must be loaded
                // before the first menu build.
                await LoadStarredWorkflowsAsync();
                // Outline gaps (if any), then starred, then #129 groups by element type.
                RebuildWorkflowMenu(viewModel);
                viewModel.OnStarsChanged = labels => ApplyStarredWorkflowsAsync(viewModel, labels);
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
                        // A save that an accept started finishes first, so two writes never overlap.
                        if (_session?.PendingSave != null)
                        {
                            try { await _session.PendingSave; } catch { /* logged by the save */ }
                        }
                        var saved = await _service!.SaveAsync(_filePath);
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
                        // Collaborator #237 item 8: one name per workflow everywhere; the
                        // list shows the title, so the top bar does too.
                        viewModel.ActiveWorkflowName = workflow.Title;
                        viewModel.HasPendingUpdates = false;

                        // Clear shell status; gather cancel has no chat page (#123).
                        viewModel.StatusText = string.Empty;

                        // The costs and the touched fields belong to the Collaborator session, not to
                        // one workflow page, so a later page sees what an earlier one wrote.
                        var session = new WorkflowSession(workflow, _storyModel!)
                        {
                            FilePath = _filePath,
                            TouchedFields = _sessionTouchedFields,
                            CostTracker = _costTracker
                        };

                        // Gather required input elements before navigating
                        var gatherResult = await GatherWorkflowInputsAsync(session, hostFrame.XamlRoot!);
                        if (gatherResult.Cancelled)
                        {
                            // No WorkflowPage / chat — surface cancel on the shell status line.
                            viewModel.StatusText = FormatGatherStatusForShell(gatherResult.StatusMessages);
                            _logger?.LogInformation("Workflow cancelled - user did not select required elements");
                            return;
                        }

                        if (gatherResult.Failed)
                        {
                            viewModel.StatusText = string.IsNullOrWhiteSpace(gatherResult.BailReason)
                                ? FormatGatherStatusForShell(gatherResult.StatusMessages)
                                : gatherResult.BailReason;
                            _logger?.LogInformation("SceneBuilder bail: {BailReason}", gatherResult.BailReason);
                            return;
                        }

                        viewModel.StatusText = string.Empty;

                        // #119: navigating to another workflow abandons an interview just as
                        // surely as closing the window does. Write it before the page goes.
                        EndInterviewSession();

                        // Navigate to WorkflowPage
                        viewModel.ContentFrame.Navigate(typeof(StoryCADLib.Collaborator.Views.WorkflowPage));

                        // Get the page and populate its ViewModel
                        if (viewModel.ContentFrame.Content is StoryCADLib.Collaborator.Views.WorkflowPage page
                            && page.ViewModel != null)
                        {
                            PopulateWorkflowViewModel(page.ViewModel, workflow, gatherResult.Elements);
                            _session = session;
                            WorkflowViewModelWiring.Wire(
                                page.ViewModel, _shellViewModel, _service!, session, ConfirmOverwriteAsync);

                            // Add status messages from gathering phase (rolled up, #129)
                            foreach (var message in gatherResult.StatusMessages)
                            {
                                page.ViewModel.AddStatusMessage(message);
                            }

                            // #119: a conversational workflow asks rather than proposes, so
                            // it opens an interview instead of running once and extracting.
                            if (workflow.Mode == WorkflowMode.Conversational)
                                WorkflowViewModelWiring.BeginInterview(page.ViewModel, _shellViewModel, _service!, session);
                            else
                                await WorkflowViewModelWiring.RunAsync(page.ViewModel, _shellViewModel, _service!, session);

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

        // #119: before EndSession, because an unsaved interview is written to the outline
        // here and RefreshCurrentView below is what puts the new node on screen.
        EndInterviewSession();

        _sessionService.EndSession();

        // The accepts of this session save in the background. Let the last one finish before
        // the host closes the outline, so the file holds every accepted proposal.
        try
        {
            _session?.PendingSave?.Wait(TimeSpan.FromSeconds(10));
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "A background save failed before close");
        }

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
                // Collaborator #237 item 10: the count is the number of missing fields, which is
                // what the page lists. It used to count elements, so an Overview with five
                // empty fields read as "(1)".
                viewModel.MenuItems.Add(WrappingNavItem(
                    $"{GapWorkflowOwnership.OutlineGapsNavTitle} ({RequiredFieldGapScanner.MissingFieldCount(gapDetails)})",
                    GapWorkflowOwnership.OutlineGapsTag,
                    CollaboratorAutomationIds.OutlineGapsNavItem));
            }
        }

        // Starred band first, then element-type groups holding the rest (#129 grouping kept).
        // Groups start collapsed so the pane opens on a short list of next actions rather than
        // the whole catalog.
        var bands = WorkflowMenuComposer.Compose(WorkflowRegistry.All, _starredWorkflows, GroupTitle);
        foreach (var band in bands)
        {
            var group = new Microsoft.UI.Xaml.Controls.NavigationViewItem
            {
                Content = band.Title,
                SelectsOnInvoked = false,
                IsExpanded = band.IsExpanded
            };
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(
                group, CollaboratorAutomationIds.GroupNavItem(band.Title));
            viewModel.MenuItems.Add(group);

            foreach (var item in band.Items)
            {
                group.MenuItems.Add(WorkflowNavItem(viewModel, item));
            }
        }

        RefreshStarEntries(viewModel);
        viewModel.RestoreSelection(selectedTag);
    }

    /// <summary>
    /// Republishes the Customize workflows dialog's list so it opens on current star state.
    /// </summary>
    private void RefreshStarEntries(WorkflowShellViewModel viewModel)
    {
        viewModel.StarEntries.Clear();
        foreach (var workflow in WorkflowRegistry.All)
        {
            viewModel.StarEntries.Add(new WorkflowStarEntry
            {
                Label = workflow.Label,
                Title = workflow.Title,
                Description = workflow.Description,
                GroupTitle = GroupTitle(workflow.PrimaryElementType),
                IsStarred = _starredWorkflows.Contains(workflow.Label)
            });
        }
    }

    /// <summary>
    /// Loads the user's starred workflows, seeding the registry defaults on first run.
    /// Falls back to the defaults when preferences are unavailable (Ioc.Default is not configured
    /// in every host), so the pane is never left without a starred band.
    /// </summary>
    private async Task LoadStarredWorkflowsAsync()
    {
        try
        {
            var starService = Ioc.Default.GetService<WorkflowStarService>();
            if (starService != null)
            {
                _starredWorkflows =
                    (await starService.GetStarredAsync(
                        WorkflowRegistry.DefaultStarredLabels,
                        WorkflowRegistry.RetiredWorkflowReplacements,
                        WorkflowRegistry.StarMigrationVersion,
                        WorkflowRegistry.StarsAddedByVersion)).ToList();
                return;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Could not load starred workflows; using defaults");
        }

        _starredWorkflows = WorkflowRegistry.DefaultStarredLabels.ToList();
    }

    /// <summary>
    /// Adopts a new starred set and rebuilds the pane, then persists. Used by the Customize
    /// workflows dialog; the per-row star button drives the two halves separately so it can drop
    /// its navigation suppression as soon as the rebuild is done.
    /// </summary>
    private async Task ApplyStarredWorkflowsAsync(WorkflowShellViewModel viewModel, IEnumerable<string> labels)
    {
        ApplyStarredWorkflows(viewModel, labels);
        await PersistStarredWorkflowsAsync();
    }

    /// <summary>
    /// Adopts a new starred set and rebuilds the pane so the change shows immediately.
    /// Synchronous on purpose: the pane must answer the click on the click's own turn.
    /// </summary>
    private void ApplyStarredWorkflows(WorkflowShellViewModel viewModel, IEnumerable<string> labels)
    {
        var next = labels?.ToList() ?? new List<string>();

        // Neither the pane nor the Customize dialog can show a star whose label matches no
        // registry workflow, so a caller working from either surface cannot include one. Carry
        // those labels over rather than deleting the star for a workflow that is only withdrawn
        // for this release — the contract PreferencesModel.StarredCollaboratorWorkflows states.
        var known = new HashSet<string>(
            WorkflowRegistry.All.Select(w => w.Label), StringComparer.Ordinal);
        var carried = new HashSet<string>(next, StringComparer.Ordinal);
        foreach (var label in _starredWorkflows)
        {
            if (!known.Contains(label) && carried.Add(label))
                next.Add(label);
        }

        _starredWorkflows = next;
        RebuildWorkflowMenu(viewModel);
    }

    /// <summary>
    /// Saves the current starred set. Separate from the rebuild because the write queues behind
    /// StoryCAD's serialization lock, which an in-flight autosave can hold for seconds.
    /// </summary>
    private async Task PersistStarredWorkflowsAsync()
    {
        try
        {
            var starService = Ioc.Default.GetService<WorkflowStarService>();
            if (starService != null)
                await starService.SetStarredAsync(_starredWorkflows);
        }
        catch (Exception ex)
        {
            // The session keeps the new set; only the saved copy is lost.
            _logger?.LogWarning(ex, "Could not persist starred workflows");
        }
    }

    /// <summary>
    /// A workflow row: wrapping title plus a star button that adds or removes the workflow from
    /// the starred band.
    /// </summary>
    private Microsoft.UI.Xaml.Controls.NavigationViewItem WorkflowNavItem(
        WorkflowShellViewModel viewModel,
        WorkflowMenuItem item)
    {
        var navItem = WrappingNavItem(
            item.Title, item.Workflow, CollaboratorAutomationIds.WorkflowNavItem(item.Label));
        var star = StarToggleButton(viewModel, item);

        // Replace the plain TextBlock content with title + star in one row.
        var layout = new Microsoft.UI.Xaml.Controls.Grid();
        layout.ColumnDefinitions.Add(new Microsoft.UI.Xaml.Controls.ColumnDefinition
        {
            Width = new Microsoft.UI.Xaml.GridLength(1, Microsoft.UI.Xaml.GridUnitType.Star)
        });
        layout.ColumnDefinitions.Add(new Microsoft.UI.Xaml.Controls.ColumnDefinition
        {
            Width = Microsoft.UI.Xaml.GridLength.Auto
        });

        if (navItem.Content is Microsoft.UI.Xaml.UIElement title)
        {
            navItem.Content = null;
            layout.Children.Add(title);
        }

        Microsoft.UI.Xaml.Controls.Grid.SetColumn(star, 1);
        layout.Children.Add(star);
        navItem.Content = layout;

        // An unstarred star would otherwise clutter every row; it appears on hover and on focus
        // so it stays reachable from the keyboard.
        if (!item.IsStarred)
        {
            navItem.PointerEntered += (_, _) => star.Opacity = 1;
            navItem.PointerExited += (_, _) => star.Opacity = 0;
            navItem.GotFocus += (_, _) => star.Opacity = 1;
            navItem.LostFocus += (_, _) => star.Opacity = 0;
        }

        return navItem;
    }

    /// <summary>
    /// The star button for one workflow row. Toggling writes preferences and rebuilds the pane.
    /// </summary>
    private Microsoft.UI.Xaml.Controls.Button StarToggleButton(
        WorkflowShellViewModel viewModel,
        WorkflowMenuItem item)
    {
        var starred = item.IsStarred;
        var label = starred ? "Remove from starred" : "Add to starred";
        var button = new Microsoft.UI.Xaml.Controls.Button
        {
            // FontIcon defaults to SymbolThemeFontFamily, the family the shell's other icons
            // use; naming a font here would diverge on the desktop head.
            Content = new Microsoft.UI.Xaml.Controls.FontIcon
            {
                // FavoriteStarFill / FavoriteStar
                Glyph = starred ? "\uE735" : "\uE734",
                FontSize = 14
            },
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
            BorderThickness = new Microsoft.UI.Xaml.Thickness(0),
            Padding = new Microsoft.UI.Xaml.Thickness(4, 0, 4, 0),
            MinWidth = 0,
            VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center,
            Opacity = starred ? 1 : 0
        };
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(button, $"{label}: {item.Title}");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(
            button, CollaboratorAutomationIds.WorkflowStarButton(item.Label));
        Microsoft.UI.Xaml.Controls.ToolTipService.SetToolTip(button, label);

        // Stop the tap reaching the NavigationViewItem. Button.Click carries a plain
        // RoutedEventArgs with nothing to mark handled, so the pointer event is where bubbling
        // gets cut; Click still fires for keyboard activation, which never bubbles as a tap.
        button.Tapped += (_, tapped) => tapped.Handled = true;

        button.Click += (_, _) =>
        {
            // Marking the tap handled is not enough on its own — WinUI and Skia disagree on
            // whether the item still invokes — so the shell also suppresses navigation. It stays
            // suppressed across the deferred rebuild below.
            viewModel.SuppressWorkflowNavigation = true;

            // Rebuilding clears MenuItems, which unparents this very button. Doing that inside
            // its own Click handler tears down the element the handler is still running against,
            // so the rebuild waits for the handler to unwind. The new set is read inside the
            // callback, not here: two stars clicked before the queue drains would otherwise both
            // build on the same stale snapshot and the second would undo the first.
            var enqueued = button.DispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    var next = new List<string>(_starredWorkflows);
                    if (starred)
                        next.RemoveAll(l => string.Equals(l, item.Label, StringComparison.Ordinal));
                    else if (!next.Contains(item.Label, StringComparer.Ordinal))
                        next.Add(item.Label);

                    ApplyStarredWorkflows(viewModel, next);
                }
                catch (Exception ex)
                {
                    // This lambda is async void to the dispatcher; an escaping exception would
                    // take the process down over a star toggle.
                    _logger?.LogError(ex, "Could not apply star toggle for {Label}", item.Label);
                    return;
                }
                finally
                {
                    // Dropped here rather than after the save below: the flag exists to cover the
                    // rebuild, and holding it across a write that queues behind an autosave would
                    // silently swallow real workflow clicks for as long as that save runs.
                    viewModel.SuppressWorkflowNavigation = false;
                }

                await PersistStarredWorkflowsAsync();
            });

            // A refused enqueue (queue shutting down) means the callback never runs, and a stuck
            // flag would ignore every later workflow click for the rest of the session.
            if (!enqueued)
                viewModel.SuppressWorkflowNavigation = false;
        };

        return button;
    }

    /// <summary>
    /// Nav item whose title wraps instead of being trimmed to one line. A string Content is
    /// rendered by the item template as a single non-wrapping line, so the text has to be a
    /// TextBlock we control; Height=Auto lets the item grow past the one-line default.
    /// AutomationProperties.Name keeps the title available to automation and screen readers.
    /// </summary>
    private static Microsoft.UI.Xaml.Controls.NavigationViewItem WrappingNavItem(
        string title, object tag, string automationId)
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
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(item, automationId);
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

        var guess = new StoryContextBuilder(_storyApi).Classify(_storyModel);
        _logger?.LogDebug(
            "Outline gaps guess Earliest={Earliest} OpenSteps={OpenSteps}",
            guess.Earliest,
            string.Join(",", guess.OpenSteps));

        shellViewModel.ContentFrame.Navigate(typeof(GapWorkflowPage));
        if (shellViewModel.ContentFrame.Content is GapWorkflowPage page && page.ViewModel != null)
        {
            page.ViewModel.Load(new GapWorkflowPayload
            {
                Groups = groups,
                GuessSentence = guess.GapsSentence
            });
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
    /// Called when the workflow page goes away (#119). The service writes an unsaved
    /// interview rather than dropping it.
    /// </summary>
    private void EndInterviewSession()
    {
        if (_session != null)
            _service?.EndInterview(_session);
    }

    /// <summary>
    /// Sets Collaborator settings. Can be called before or after OpenAsync.
    /// </summary>
    public void SetSettings(CollaboratorSettings settings)
    {
        _settings = settings ?? CollaboratorSettings.Default;
        if (_service != null)
            _service.Settings = _settings;
        _logger?.LogInformation("Settings updated: Terseness={Terseness}", _settings.Terseness);
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

        var result = await ShowDialogAsync(dialog);
        return result == ContentDialogResult.Primary;
    }

    /// <summary>
    /// Shows one Collaborator dialog with workflow navigation held off until it closes
    /// (StoryCAD #1660: a second ContentDialog on the same window ends the process).
    /// </summary>
    private async Task<ContentDialogResult> ShowDialogAsync(ContentDialog dialog)
    {
        if (_shellViewModel != null)
            _shellViewModel.IsDialogOpen = true;
        try
        {
            return await dialog.ShowAsync();
        }
        finally
        {
            if (_shellViewModel != null)
                _shellViewModel.IsDialogOpen = false;
        }
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
                {
                    // Element-type groups start collapsed, so the caller selecting this child
                    // would highlight something the user cannot see. Same fix-up RestoreSelection
                    // makes for the rebuild path.
                    top.IsExpanded = true;
                    return child;
                }
            }
        }

        return null;
    }

    /// <summary>Nav group header for a workflow's primary element type (#129).</summary>
    internal static string GroupTitle(StoryItemType type) => type switch
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
    /// Gathers input elements for a workflow. The service finds what it can; the element
    /// picker asks the writer for the rest: required inputs it cannot find, and optional
    /// inputs with the current selection pre-selected.
    /// </summary>
    /// <param name="session">The session whose workflow needs its inputs</param>
    /// <param name="xamlRoot">XamlRoot for showing dialogs</param>
    /// <returns>The elements, status messages, and cancelled flag</returns>
    private Task<InputResolution> GatherWorkflowInputsAsync(
        WorkflowSession session,
        Microsoft.UI.Xaml.XamlRoot xamlRoot) =>
        _service!.ResolveInputsAsync(session, null, null, prompt => PickElementAsync(prompt, xamlRoot));

    /// <summary>
    /// Shows the ElementPicker for one input. Null means the writer cancelled.
    /// </summary>
    private async Task<Guid?> PickElementAsync(InputPrompt prompt, Microsoft.UI.Xaml.XamlRoot xamlRoot)
    {
        var requirement = prompt.Requirement;

        // Show ElementPicker - pass API so Create works; currentGuid for pre-selection
        var pickerVM = new ElementPickerVM();
        if (_shellViewModel != null)
            _shellViewModel.IsDialogOpen = true;
        string? selectedGuid;
        try
        {
            selectedGuid = await pickerVM.ShowPicker(_storyModel!, xamlRoot,
                requirement.ElementType, requirement.ElementLabel, prompt.Preselect, _storyApi);
        }
        finally
        {
            if (_shellViewModel != null)
                _shellViewModel.IsDialogOpen = false;
        }

        // WinUI: sequential ContentDialogs can swallow the next ShowAsync if the previous
        // dialog has not fully torn down. Brief yield so Problem → Protagonist → Antagonist
        // each actually appear.
        await Task.Delay(100);

        if (string.IsNullOrEmpty(selectedGuid))
            return null;

        // An unreadable GUID finds no element, which the service reports.
        return Guid.TryParse(selectedGuid, out var guid) ? guid : Guid.Empty;
    }

}
