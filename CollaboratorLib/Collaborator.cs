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
using StoryCADLib.Collaborator.ViewModels;
using StoryCADLib.Collaborator.Models;

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

    // State
    private IStoryCADAPI? _storyApi;
    private StoryModel? _storyModel;
    private ElementResolver? _elementResolver;
    private string? _filePath;
    private Window? _hostWindow;
    private bool _disposed;
    private StoryCADLib.Services.Logging.ILogService? _auditLogger;

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
                viewModel.MenuItems.Clear();
                foreach (var workflow in WorkflowRegistry.All)
                {
                    viewModel.MenuItems.Add(new Microsoft.UI.Xaml.Controls.NavigationViewItem
                    {
                        Content = workflow.Title,
                        Tag = workflow
                    });
                }
                _logger.LogInformation("Populated {Count} workflows in menu", viewModel.MenuItems.Count);

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
                    if (viewModel.ContentFrame != null && workflowTag is Workflow workflow)
                    {
                        // Gather required input elements before navigating
                        var gatherResult = await GatherWorkflowInputsAsync(workflow, hostFrame.XamlRoot!);
                        if (gatherResult.Cancelled)
                        {
                            // User cancelled element selection
                            _logger?.LogInformation("Workflow cancelled - user did not select required elements");
                            return;
                        }

                        // Navigate to WorkflowPage
                        viewModel.ContentFrame.Navigate(typeof(StoryCADLib.Collaborator.Views.WorkflowPage));

                        // Get the page and populate its ViewModel
                        if (viewModel.ContentFrame.Content is StoryCADLib.Collaborator.Views.WorkflowPage page
                            && page.ViewModel != null)
                        {
                            PopulateWorkflowViewModel(page.ViewModel, workflow, gatherResult.Elements);
                            WireUpChatCallback(page.ViewModel, workflow, gatherResult.Elements);

                            // Add status messages from gathering phase
                            foreach (var message in gatherResult.StatusMessages)
                            {
                                page.ViewModel.ConversationList.Add(ChatMessage.FromCollaborator(message));
                            }

                            // Auto-execute the workflow and show progress
                            await ExecuteWorkflowWithFeedback(page.ViewModel, workflow, gatherResult.Elements);
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
        viewModel.Description = workflow.Description;
        viewModel.Explanation = workflow.Explanation;

        // Build summary of selected elements
        if (gatheredElements != null && gatheredElements.Count > 0)
        {
            var lines = gatheredElements
                .Select(kvp => $"{kvp.Key}: {kvp.Value?.Name ?? "(none)"}")
                .ToList();
            viewModel.SelectedElementsSummary = string.Join("\n", lines);
        }
    }

    /// <summary>
    /// Wires up the chat callback for a WorkflowViewModel.
    /// Creates a new ChatHistory for each workflow and handles message processing.
    /// </summary>
    private void WireUpChatCallback(
        StoryCADLib.Collaborator.ViewModels.WorkflowViewModel viewModel,
        Workflow workflow,
        Dictionary<string, StoryElement> gatheredElements)
    {
        // Create fresh chat history for this workflow session
        _chatHistory = new ChatHistory();

        // Build context from gathered elements
        var elementContext = BuildElementContext(gatheredElements);

        // Add system message with workflow context and story elements
        _chatHistory.AddSystemMessage(
            $"You are a story development assistant helping with the '{workflow.Title}' workflow. " +
            $"{workflow.Description}\n\n" +
            $"## Current Story Context\n{elementContext}\n\n" +
            "Provide helpful, constructive feedback to help the writer develop their story. " +
            "Reference the story elements above when giving advice.");

        // Wire up the callback
        viewModel.OnSendMessage = async (userMessage) =>
        {
            try
            {
                _chatHistory?.AddUserMessage(userMessage);
                _logger?.LogDebug("User message added to chat: {Message}", userMessage);

                var response = await _chatService!.GetChatMessageContentAsync(_chatHistory!);
                var responseText = response.Content ?? "No response received.";

                _chatHistory?.AddAssistantMessage(responseText);
                _logger?.LogDebug("Assistant response: {Response}", responseText);

                return responseText;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error processing chat message");
                throw TranslateChatException(ex);
            }
        };

        _logger?.LogInformation("Chat callback wired for workflow: {Workflow} with {Count} elements in context",
            workflow.Title, gatheredElements.Count);
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
            viewModel.ConversationList.Add(ChatMessage.FromCollaborator($"Running {workflow.Title}..."));
            viewModel.ProgressVisibility = Microsoft.UI.Xaml.Visibility.Visible;

            // Execute via WorkflowRunner
            var runnerLogger = _loggerFactory?.CreateLogger<WorkflowRunner>();
            var runner = new WorkflowRunner(_storyModel!, workflow, _storyApi!, runnerLogger, _settings, _auditLogger);
            _auditLogger?.Log(StoryCADLib.Services.Logging.LogLevel.Info,
                $"Workflow started: {workflow.Title} with {gatheredElements.Count} elements");
            var result = await runner.RunAsync(gatheredElements);

            // Hide progress
            viewModel.ProgressVisibility = Microsoft.UI.Xaml.Visibility.Collapsed;

            if (result.Success)
            {
                // Show status messages
                viewModel.ConversationList.Add(ChatMessage.FromCollaborator($"Workflow completed successfully."));

                foreach (var msg in result.StatusMessages)
                {
                    viewModel.ConversationList.Add(ChatMessage.FromCollaborator($"  {msg}"));
                }

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
                if (result.UpdatedProperties.Count > 0)
                {
                    // Populate ViewModel's pending updates panel
                    viewModel.SetPendingUpdates(result.UpdatedProperties);

                    // Wire up command callbacks using closures (captures local state)
                    viewModel.OnAcceptAll = () =>
                    {
                        try
                        {
                            var count = runner.ApplyUpdates(result, gatheredElements);

                            // Build detailed message listing applied updates for chat log
                            var sb = new System.Text.StringBuilder();
                            sb.AppendLine($"Applied {count} property updates to your outline:");
                            sb.AppendLine();
                            foreach (var kvp in result.UpdatedProperties)
                            {
                                var valuePreview = kvp.Value?.ToString() ?? "(empty)";
                                // Truncate long values for readability
                                if (valuePreview.Length > 200)
                                    valuePreview = valuePreview.Substring(0, 200) + "...";
                                sb.AppendLine($"**{kvp.Key}**: {valuePreview}");
                                sb.AppendLine();
                            }
                            viewModel.ConversationList.Add(ChatMessage.FromCollaborator(sb.ToString().TrimEnd()));
                            _logger?.LogInformation("AcceptAll: Applied {Count} property updates", count);
                            _auditLogger?.Log(StoryCADLib.Services.Logging.LogLevel.Info,
                                $"Applied {count} updates from workflow: {workflow.Title}");

                            // Mark updates as applied (keeps panel visible in applied state)
                            viewModel.MarkUpdatesApplied();

                            // Refresh StoryCAD UI to show changes
                            _storyModel?.RefreshCurrentView();

                            // Auto-save to bypass ViewModel flush (Issue #55)
                            _ = Task.Run(async () =>
                            {
                                var saved = await SaveAsync();
                                if (saved)
                                {
                                    _logger?.LogInformation("Auto-save completed after Accept All");
                                }
                            });
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
                            viewModel.ClearPendingUpdates();
                            viewModel.ConversationList.Add(ChatMessage.FromCollaborator("Re-running workflow..."));

                            // Re-execute with the same elements
                            await ExecuteWorkflowWithFeedback(viewModel, workflow, gatheredElements);
                        }
                        catch (Exception ex)
                        {
                            viewModel.ConversationList.Add(ChatMessage.Error($"Error re-running workflow: {ex.Message}"));
                            _logger?.LogError(ex, "Error in TryAgain handler");
                        }
                    };

                    viewModel.OnAcceptProperty = (propertyKey) =>
                    {
                        if (string.IsNullOrEmpty(propertyKey))
                        {
                            _logger?.LogWarning("AcceptProperty called with empty key");
                            return;
                        }

                        try
                        {
                            if (!result.UpdatedProperties.TryGetValue(propertyKey, out var value))
                            {
                                _logger?.LogWarning("Property key not found in pending updates: {Key}", propertyKey);
                                return;
                            }

                            // Parse key format: "ElementLabel.PropertyName"
                            var parts = propertyKey.Split('.', 2);
                            if (parts.Length != 2)
                            {
                                _logger?.LogWarning("Invalid property key format: {Key}", propertyKey);
                                return;
                            }

                            var elementLabel = parts[0];
                            var propName = parts[1];

                            if (!gatheredElements.TryGetValue(elementLabel, out var element))
                            {
                                _logger?.LogWarning("Element not found for property update: {Label}", elementLabel);
                                return;
                            }

                            // Apply via API
                            var updateResult = _storyApi?.UpdateElementProperty(element.Uuid, propName, value);

                            if (updateResult?.IsSuccess == true)
                            {
                                viewModel.ConversationList.Add(ChatMessage.FromCollaborator($"Applied {propertyKey}"));
                                _logger?.LogInformation("AcceptProperty: Applied {Key}", propertyKey);
                                _auditLogger?.Log(StoryCADLib.Services.Logging.LogLevel.Info,
                                    $"Applied update: {propertyKey}");

                                // Remove from pending updates to prevent duplicate application
                                result.UpdatedProperties.Remove(propertyKey);
                                viewModel.SetPendingUpdates(result.UpdatedProperties);

                                // Refresh StoryCAD UI to show changes
                                _storyModel?.RefreshCurrentView();
                            }
                            else
                            {
                                viewModel.ConversationList.Add(ChatMessage.Error($"Failed to apply {propertyKey}: {updateResult?.ErrorMessage}"));
                                _logger?.LogWarning("Failed to apply {Key}: {Error}", propertyKey, updateResult?.ErrorMessage);
                            }
                        }
                        catch (Exception ex)
                        {
                            viewModel.ConversationList.Add(ChatMessage.Error($"Error applying {propertyKey}: {ex.Message}"));
                            _logger?.LogError(ex, "Error in AcceptProperty handler for {Key}", propertyKey);
                        }
                    };

                    viewModel.OnSkipProperty = (propertyKey) =>
                    {
                        if (string.IsNullOrEmpty(propertyKey))
                        {
                            _logger?.LogWarning("SkipProperty called with empty key");
                            return;
                        }

                        try
                        {
                            // Remove from pending updates without applying
                            if (result.UpdatedProperties.Remove(propertyKey))
                            {
                                viewModel.ConversationList.Add(ChatMessage.FromCollaborator($"Skipped {propertyKey}"));
                                _logger?.LogInformation("SkipProperty: Skipped {Key}", propertyKey);

                                // Update the pending updates panel
                                viewModel.SetPendingUpdates(result.UpdatedProperties);
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

                    viewModel.ConversationList.Add(ChatMessage.FromCollaborator(
                        $"Found {result.UpdatedProperties.Count} property updates. Review them above and choose Accept All, Review Each, or Try Again."));
                }
                else
                {
                    viewModel.ConversationList.Add(ChatMessage.FromCollaborator("No property updates were extracted from the response."));
                }

                viewModel.ConversationList.Add(ChatMessage.FromCollaborator("You can ask questions or request changes using the chat below."));
            }
            else
            {
                viewModel.ConversationList.Add(ChatMessage.Error(result.ErrorMessage ?? "Unknown error"));
                foreach (var msg in result.StatusMessages)
                {
                    viewModel.ConversationList.Add(ChatMessage.FromCollaborator($"  {msg}"));
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
                    statusMessages.Add($"Found {requirement.ElementLabel}: {currentElement.Name}");
                    _logger?.LogDebug("Resolved {Label} via {Ref} to '{Name}'",
                        requirement.ElementLabel, requirement.ReferencedElementLabel, currentElement.Name);
                    return currentElement;
                }

                // For OPTIONAL inputs, we'll show picker with pre-selection
                _logger?.LogDebug("Found current {Label}: '{Name}', showing picker for potential change",
                    requirement.ElementLabel, currentElement.Name);
            }
            else
            {
                // Reference was empty/missing - fall through to picker
                _logger?.LogDebug("Reference {Ref} empty, will prompt for {Label}",
                    requirement.ReferencedElementLabel, requirement.ElementLabel);
            }
        }

        // Show ElementPicker - pass currentGuid for pre-selection if available
        var pickerVM = new ElementPickerVM();
        var selectedGuid = await pickerVM.ShowPicker(_storyModel!, xamlRoot,
            requirement.ElementType, requirement.ElementLabel, currentGuid);

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
        }
        else
        {
            _logger?.LogWarning("Failed to update {Source}.{Property}: {Error}",
                sourceLabel, propertyName, result?.ErrorMessage);
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
