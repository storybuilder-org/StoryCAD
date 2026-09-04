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

                        if (gatherResult.Failed)
                        {
                            viewModel.StatusText = string.IsNullOrWhiteSpace(gatherResult.BailReason)
                                ? FormatGatherStatusForShell(gatherResult.StatusMessages)
                                : gatherResult.BailReason;
                            _logger?.LogInformation("SceneBuilder bail: {BailReason}", gatherResult.BailReason);
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
                        WorkflowRegistry.StarMigrationVersion)).ToList();
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
        var navItem = WrappingNavItem(item.Title, item.Workflow);
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
    /// After Collaborator Accept writes the model via API, reload the host page ViewModel
    /// so the Character (etc.) form and later AutoSave flushes see the new values.
    /// Close already reloads; Accept did not — empty VM then overwrote applied fields.
    /// </summary>
    private void ReloadHostViewModelFromModel()
    {
        try
        {
            var appState = Ioc.Default.GetService<AppState>();
            if (appState?.CurrentSaveable is IReloadable reloadable)
            {
                reloadable.ReloadFromModel();
                _logger?.LogInformation("Reloaded host ViewModel from Model after Accept apply");
            }
            else
            {
                _logger?.LogDebug("No IReloadable CurrentSaveable after Accept apply");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Host ViewModel reload after Accept failed");
        }
    }

    /// <summary>
    /// Sets Collaborator settings. Can be called before or after OpenAsync.
    /// </summary>
    public void SetSettings(CollaboratorSettings settings)
    {
        _settings = settings ?? CollaboratorSettings.Default;
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

                // Cost line (shown when CollaboratorSettings.ShowCostDetails is on). The proxy's
                // X-Collab-Cost header is read by
                // ActivationJwtHandler inside this scope (Semantic Kernel hides the response
                // from us); Semantic Kernel's own usage metadata is the fallback when the
                // header is absent, giving tokens without dollars.
                Microsoft.SemanticKernel.ChatMessageContent response;
                ProxyCostInfo? chatCost;
                using (var costScope = ChatCostCapture.Begin())
                {
                    response = await _chatService!.GetChatMessageContentAsync(_chatHistory!);
                    chatCost = costScope.Cost;
                }

                var responseText = response.Content ?? "No response received.";

                // Nothing to report leaves the previous line alone rather than blanking it:
                // a chat turn that cannot account for itself should not erase the last
                // workflow's figure.
                if (_shellViewModel != null)
                {
                    var usageRead = ChatUsageReader.TryRead(response.Metadata, out var chatIn, out var chatOut);
                    if (chatCost != null || usageRead)
                        _shellViewModel.CostSummary = _costTracker.RecordChat(chatCost, chatIn, chatOut);
                }

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
        _sessionProposals.ReplaceFromPending(result.PendingUpdates, ResolveElementName);

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
        // ProposedText is already ValueDisplay-formatted at capture; do not rebind Value to that string
        // (List properties must keep typed Values for Accept / SimpleList).
        var u = e.Update;
        var proposed = TruncateForChat(e.ProposedText, 500);
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
            PropertyDisplayName = u.DisplayNameOverride ?? u.Spec.Property,
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

            // Cost line describes the run in flight, not the previous one.
            if (_shellViewModel != null)
                _shellViewModel.CostSummary = string.Empty;

            // Execute via WorkflowRunner
            var runnerLogger = _loggerFactory?.CreateLogger<WorkflowRunner>();
            var runner = new WorkflowRunner(_storyModel!, workflow, _storyApi!, runnerLogger, _settings, _auditLogger);
            _auditLogger?.Log(StoryCADLib.Services.Logging.LogLevel.Info,
                $"Workflow started: {workflow.Title} with {gatheredElements.Count} elements");
            var result = await runner.RunAsync(gatheredElements);

            // Cost line (devdocs/collaborator_workflow_cost_display_design.md).
            // Recorded for every run, priced or not: a null Cost still advances the display
            // to "cost unavailable" rather than leaving the previous run's figure showing.
            if (_shellViewModel != null)
                _shellViewModel.CostSummary = _costTracker.Record(result.Cost);

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

                    var sceneBuilderOrphanBindDone = false;

                    int ApplyPendingList(IReadOnlyList<PendingUpdate> list)
                    {
                        int applied = 0;
                        if (list.Count > 0)
                        {
                            var slice = WorkflowResult.Succeeded();
                            foreach (var u in list)
                                slice.PendingUpdates.Add(u);
                            applied = runner.ApplyUpdates(slice, gatheredElements);
                            foreach (var u in list)
                                _sessionTouchedFields.Add(u.SessionTouchKey);
                        }
                        if (!sceneBuilderOrphanBindDone
                            && string.Equals(workflow.Label, "SceneBuilder", StringComparison.Ordinal))
                        {
                            var bindMsg = runner.TryApplySceneBuilderOrphanBind(result, gatheredElements);
                            sceneBuilderOrphanBindDone = true;
                            if (!string.IsNullOrEmpty(bindMsg))
                                viewModel.AddStatusMessage(bindMsg);
                        }
                        // Accept writes the model via API. Host element VMs stay stale until
                        // reload — AutoSave FlushCurrentEdits then SaveModel's empty VM values
                        // over the applied text (Character.BackStory wipe after #184 Accept).
                        if (applied > 0)
                            ReloadHostViewModelFromModel();
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
                            // Writer refused overwrites: session was already MarkAccepted for Review
                            // Each advance — flip to Skipped so chat/snapshot match the outline.
                            foreach (var u in staged)
                                _sessionProposals?.MarkSkipped(u.Key);
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
                                // Mark Accepted in the session set now so Review Each can leave the row
                                // (IsSettled). Without this, KindLabel stays "Has your text",
                                // AdvancePastSettledRows never moves, and Accept looks stuck.
                                stageSession.StageProtect(pending);
                                RemovePendingKeys(new[] { propertyKey });
                                _sessionProposals?.MarkAccepted(propertyKey);
                                viewModel.AddStatusMessage($"Queued overwrite: {propertyKey}");
                                _logger?.LogInformation("AcceptProperty: Staged Protect {Key}", propertyKey);
                                PushSessionSetToViewModel(viewModel);
                                await FlushStagedIfQueueDoneAsync();
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
                            {
                                stageSession.StageProtect(u);
                                _sessionProposals?.MarkAccepted(u.Key);
                            }
                            RemovePendingKeys(protect.Select(u => u.Key));

                            if (freeCount > 0)
                            {
                                viewModel.ConversationList.Add(ChatMessage.FromCollaborator(
                                    $"Applied {freeCount} free update(s)."));
                            }

                            // Prefer session set so staged Protect show as Accepted for Review Each.
                            if (_sessionProposals != null && _sessionProposals.Count > 0)
                                PushSessionSetToViewModel(viewModel);
                            else
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

    /// <summary>
    /// Readable text for a typed pending-update value; lists render per entry and
    /// element GUIDs resolve to outline names (#129).
    /// </summary>
    private string? ResolveElementName(Guid guid) =>
        _storyModel?.StoryElements?.StoryElementGuids != null
        && _storyModel.StoryElements.StoryElementGuids.TryGetValue(guid, out var element)
            ? element?.Name
            : null;

    private string FormatValueForDisplay(object? value) =>
        ValueDisplay.Format(value, ResolveElementName);

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
        public bool Failed { get; set; }
        public string? BailReason { get; set; }
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

        if (string.Equals(workflow.Label, "SceneBuilder", StringComparison.Ordinal)
            && result.Elements.TryGetValue("Scene", out var sceneBuilderScene)
            && _storyApi != null)
        {
            InjectSceneBuilder(sceneBuilderScene, result);
        }

        // #201: Story Problem + seats when Overview.StoryProblem resolves (thin gather).
        if (string.Equals(workflow.Label, "DefineStoryWorld", StringComparison.Ordinal)
            && _storyApi != null)
        {
            InjectStoryProblemSeatsForWorld(result);
        }

        return result;
    }

    /// <summary>
    /// Collaborator #201: inject Problem / Protagonist / Antagonist when Overview links them.
    /// Does not create elements. Does not open pickers.
    /// </summary>
    private void InjectStoryProblemSeatsForWorld(GatherResult result)
    {
        if (!result.Elements.TryGetValue("Overview", out var overviewElement)
            || overviewElement is not OverviewModel overview)
            return;

        if (overview.StoryProblem == Guid.Empty)
            return;

        var problemResult = _storyApi!.GetStoryElement(overview.StoryProblem);
        if (!problemResult.IsSuccess || problemResult.Payload is not ProblemModel problem)
            return;

        result.Elements["Problem"] = problem;
        result.StatusMessages.Add($"Using Problem: {problem.Name}");

        if (problem.Protagonist != Guid.Empty)
        {
            var prot = _storyApi.GetStoryElement(problem.Protagonist);
            if (prot.IsSuccess && prot.Payload != null)
            {
                result.Elements["Protagonist"] = prot.Payload;
                result.StatusMessages.Add($"Using Protagonist: {prot.Payload.Name}");
            }
        }

        if (problem.Antagonist != Guid.Empty)
        {
            var antag = _storyApi.GetStoryElement(problem.Antagonist);
            if (antag.IsSuccess && antag.Payload != null)
            {
                result.Elements["Antagonist"] = antag.Payload;
                result.StatusMessages.Add($"Using Antagonist: {antag.Payload.Name}");
            }
        }
    }

    /// <summary>
    /// Collaborator #201: create the StoryWorld singleton under Overview when missing.
    /// </summary>
    private StoryElement? TryCreateStoryWorldSingleton(List<string> statusMessages)
    {
        if (_storyApi == null || _storyModel == null)
            return null;

        var existing = _storyApi.GetStoryWorld();
        if (existing.IsSuccess && existing.Payload != null)
            return existing.Payload;

        // Parent: Overview (explorer root).
        Guid overviewGuid = Guid.Empty;
        if (_storyModel.ExplorerView.Count > 0)
            overviewGuid = _storyModel.ExplorerView[0].Uuid;
        if (overviewGuid == Guid.Empty)
        {
            var overviewList = _storyApi.GetElementsByType(StoryItemType.StoryOverview);
            if (overviewList.IsSuccess && overviewList.Payload is { Count: > 0 })
                overviewGuid = overviewList.Payload[0].Uuid;
        }

        if (overviewGuid == Guid.Empty)
        {
            statusMessages.Add("Cannot create StoryWorld: Overview not found");
            return null;
        }

        var add = _storyApi.AddElement(StoryItemType.StoryWorld, overviewGuid.ToString(), "Story World");
        if (!add.IsSuccess)
        {
            statusMessages.Add($"Cannot create StoryWorld: {add.ErrorMessage}");
            _logger?.LogWarning("AddElement StoryWorld failed: {Error}", add.ErrorMessage);
            return null;
        }

        var created = _storyApi.GetStoryElement(add.Payload);
        if (!created.IsSuccess || created.Payload == null)
        {
            statusMessages.Add("StoryWorld was created but could not be reloaded");
            return null;
        }

        statusMessages.Add($"Created StoryWorld: {created.Payload.Name}");
        _logger?.LogInformation("Created StoryWorld {Guid}", created.Payload.Uuid);
        return created.Payload;
    }

    /// <summary>
    /// Collaborator #208: inject Overview, owner, contributing Problems, neighbors, seats, Setting.
    /// Sets Failed on Story Problem / empty-category bail. Does not open pickers.
    /// </summary>
    private void InjectSceneBuilder(StoryElement sceneElement, GatherResult result)
    {
        InjectOverviewIfMissing(result);

        var resolver = new Services.SceneStructureNeighborResolver(_storyApi!);
        var resolved = resolver.ResolveForSceneBuilder(sceneElement);

        foreach (var line in resolved.StatusLines)
            result.StatusMessages.Add(line);

        if (resolved.OwnerState is Services.SceneStructureNeighborResolver.SceneBuilderOwnerState.StoryProblemBail
            or Services.SceneStructureNeighborResolver.SceneBuilderOwnerState.EmptyCategoryBail)
        {
            result.Failed = true;
            result.BailReason = resolved.BailReason;
            return;
        }

        if (resolved.OwnerProblem != null)
            result.Elements["Problem"] = resolved.OwnerProblem;
        if (resolved.PrecedingScene != null)
            result.Elements["PrecedingScene"] = resolved.PrecedingScene;
        if (resolved.NextScene != null)
            result.Elements["NextScene"] = resolved.NextScene;

        InjectSceneBuilderSeatsAndCast(sceneElement, result);
        InjectSceneBuilderContributingLabels(sceneElement, resolver, resolved, result);
        InjectStoryProblemAncestor(result);
    }

    private void InjectOverviewIfMissing(GatherResult result)
    {
        if (result.Elements.ContainsKey("Overview") || _storyApi == null)
            return;

        var overviews = _storyApi.GetElementsByType(StoryItemType.StoryOverview);
        if (!overviews.IsSuccess || overviews.Payload == null || overviews.Payload.Count == 0)
            return;

        result.Elements["Overview"] = overviews.Payload[0];
        result.StatusMessages.Add($"Using Overview: {overviews.Payload[0].Name}");
    }

    private void InjectStoryProblemAncestor(GatherResult result)
    {
        if (_storyApi == null)
            return;
        if (!result.Elements.TryGetValue("Overview", out var overviewEl)
            || overviewEl is not OverviewModel overview
            || overview.StoryProblem == Guid.Empty)
            return;
        if (result.Elements.TryGetValue("Problem", out var owner)
            && owner.Uuid == overview.StoryProblem)
            return;

        var sp = _storyApi.GetStoryElement(overview.StoryProblem);
        if (!sp.IsSuccess || sp.Payload is not ProblemModel problem)
            return;

        result.Elements["StoryProblem"] = problem;
        result.StatusMessages.Add($"Using StoryProblem (ancestor): {problem.Name}");
    }

    private void InjectSceneBuilderSeatsAndCast(StoryElement sceneElement, GatherResult result)
    {
        if (_storyApi == null || sceneElement is not SceneModel scene)
            return;

        if (scene.Setting != Guid.Empty)
        {
            var settingResult = _storyApi.GetStoryElement(scene.Setting);
            if (settingResult.IsSuccess && settingResult.Payload is SettingModel setting)
            {
                result.Elements["Setting"] = setting;
                result.StatusMessages.Add($"Using Setting: {setting.Name}");
            }
        }

        var used = new HashSet<Guid>();
        TryInjectCharacterSeat(scene.Protagonist, "Protagonist", result, used);
        TryInjectCharacterSeat(scene.Antagonist, "Antagonist", result, used);
        TryInjectCharacterSeat(scene.ViewpointCharacter, "ViewpointCharacter", result, used);

        int n = 1;
        foreach (var guid in scene.CastMembers ?? new List<Guid>())
        {
            if (guid == Guid.Empty || used.Contains(guid))
                continue;
            var member = _storyApi.GetStoryElement(guid);
            if (!member.IsSuccess || member.Payload == null)
                continue;
            result.Elements[$"CastMember{n}"] = member.Payload;
            result.StatusMessages.Add($"Using CastMember{n}: {member.Payload.Name}");
            used.Add(guid);
            n++;
        }
    }

    private void TryInjectCharacterSeat(
        Guid guid,
        string label,
        GatherResult result,
        HashSet<Guid> used)
    {
        if (guid == Guid.Empty || _storyApi == null)
            return;
        var got = _storyApi.GetStoryElement(guid);
        if (!got.IsSuccess || got.Payload is not CharacterModel character)
            return;
        result.Elements[label] = character;
        result.StatusMessages.Add($"Using {label}: {character.Name}");
        used.Add(guid);
    }

    private void InjectSceneBuilderContributingLabels(
        StoryElement sceneElement,
        Services.SceneStructureNeighborResolver resolver,
        Services.SceneStructureNeighborResolver.SceneBuilderResolveResult resolved,
        GatherResult result)
    {
        var owners = resolved.ContributingProblems.ToList();
        if (owners.Count == 0)
            return;

        var ordered = new List<ProblemModel>();
        void AddIfOwner(ProblemModel? problem)
        {
            if (problem == null)
                return;
            var match = owners.FirstOrDefault(o => o.Uuid == problem.Uuid);
            if (match != null && ordered.All(o => o.Uuid != match.Uuid))
                ordered.Add(match);
        }

        AddIfOwner(resolved.OwnerProblem);
        AddIfOwner(resolver.GetExplorerParentProblem(sceneElement));
        foreach (var owner in owners)
            AddIfOwner(owner);

        const int cap = 8;
        int k = Math.Min(cap, ordered.Count);
        for (int i = 0; i < k; i++)
        {
            result.Elements[$"ContributingProblem{i + 1}"] = ordered[i];
            result.StatusMessages.Add($"Using ContributingProblem{i + 1}: {ordered[i].Name}");
        }

        if (ordered.Count > cap)
        {
            result.StatusMessages.Add(
                $"Scene Builder: {ordered.Count} contributing Problems; labeled first {cap}.");
        }
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

        // #201: StoryWorld is a singleton. Create when missing; never ElementPicker.
        if (requirement.ElementType == StoryItemType.StoryWorld)
        {
            if (requirement.CreateIfMissing && _storyApi != null)
            {
                var created = TryCreateStoryWorldSingleton(statusMessages);
                if (created != null)
                {
                    gatheredElements[requirement.ElementLabel] = created;
                    return created;
                }
            }

            statusMessages.Add($"{requirement.ElementLabel}: StoryWorld missing and could not be created");
            _logger?.LogWarning("StoryWorld gather failed (create={Create})", requirement.CreateIfMissing);
            return null;
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
