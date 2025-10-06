using CommunityToolkit.Mvvm.Messaging;
using StoryCADLib.Services.Messages;
using static CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger;

namespace StoryCADLib.Services.Dialogs;

/// <summary>
///     Service for validating tool usage prerequisites.
///     IMPORTANT: This service is a temporary solution that still requires ShellViewModel parameters.
///     This is part of the DI refactoring (Issue #1063) to extract validation logic from ViewModels.
///     TODO: In a future refactoring, the CurrentViewType, CurrentNode, and RightTappedNode properties
///     should be moved from ShellViewModel to AppState to eliminate the ViewModel dependency entirely.
///     This would require updating ~123 references across the codebase, so it's deferred to a separate task.
///     This intermediate step allows us to:
///     1. Extract the validation logic to a testable service
///     2. Remove the direct dependency between NarrativeToolVM and OutlineViewModel
///     3. Enable future incremental refactoring
/// </summary>
public class ToolValidationService
{
    private readonly ILogService _logger;

    public ToolValidationService(ILogService logger)
    {
        _logger = logger;
    }

    /// <summary>
    ///     Verifies that prerequisites are met for tool usage.
    /// </summary>
    /// <param name="currentViewType">Current story view type from ShellViewModel</param>
    /// <param name="currentNode">Currently selected node from ShellViewModel</param>
    /// <param name="rightTappedNode">Right-clicked node from ShellViewModel</param>
    /// <param name="model">Current story model from AppState</param>
    /// <param name="explorerViewOnly">If true, tool can only be used in Explorer view</param>
    /// <param name="nodeRequired">If true, a node must be selected</param>
    /// <param name="checkOutlineIsOpen">If true, checks that an outline is open</param>
    /// <returns>true if all prerequisites are met, false otherwise</returns>
    public bool VerifyToolUse(
        StoryViewType? currentViewType,
        StoryNodeItem currentNode,
        StoryNodeItem rightTappedNode,
        StoryModel model,
        bool explorerViewOnly,
        bool nodeRequired,
        bool checkOutlineIsOpen = true)
    {
        try
        {
            // Check if tool requires Explorer view
            if (explorerViewOnly && currentViewType != StoryViewType.ExplorerView)
            {
                Default.Send(new StatusChangedMessage(new StatusMessage(
                    "This tool can only be run in Story Explorer view", LogLevel.Warn)));
                return false;
            }

            // Check if an outline is open
            if (checkOutlineIsOpen)
            {
                if (model == null)
                {
                    Default.Send(
                        new StatusChangedMessage(new StatusMessage("Open or create an outline first", LogLevel.Warn)));
                    return false;
                }

                if (currentViewType == StoryViewType.ExplorerView && model.ExplorerView.Count == 0)
                {
                    Default.Send(
                        new StatusChangedMessage(new StatusMessage("Open or create an outline first", LogLevel.Warn)));
                    return false;
                }

                if (currentViewType == StoryViewType.NarratorView && model.NarratorView.Count == 0)
                {
                    Default.Send(
                        new StatusChangedMessage(new StatusMessage("Open or create an outline first", LogLevel.Warn)));
                    return false;
                }
            }

            // Check if a node is required and selected
            if (nodeRequired)
            {
                // Use rightTappedNode if available, otherwise use currentNode
                var nodeToUse = rightTappedNode ?? currentNode;

                if (nodeToUse == null)
                {
                    Default.Send(
                        new StatusChangedMessage(new StatusMessage("You need to select a node first", LogLevel.Warn)));
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogException(LogLevel.Error, ex, "Error in ToolValidationService.VerifyToolUse()");
            return false; // Return false to prevent any issues
        }
    }
}
