using CommunityToolkit.Mvvm.Messaging;
using StoryCADLib.Models;
using StoryCADLib.Services.Messages;
using static CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger;

namespace StoryCADLib.Services.Dialogs;

/// <summary>
///     Service for validating tool usage prerequisites.
///     Reads navigation state (CurrentViewType, CurrentNode, RightTappedNode) from AppState.
///     Issue #1146: Properties moved from ShellViewModel to AppState for service-layer access.
/// </summary>
public class ToolValidationService
{
    private readonly AppState _appState;
    private readonly ILogService _logger;

    public ToolValidationService(AppState appState, ILogService logger)
    {
        _appState = appState;
        _logger = logger;
    }

    /// <summary>
    ///     Verifies that prerequisites are met for tool usage.
    ///     Reads CurrentViewType, CurrentNode, and RightTappedNode from AppState.
    ///     When nodeRequired is true and RightTappedNode is null but CurrentNode exists,
    ///     sets RightTappedNode = CurrentNode as a fallback (side effect).
    /// </summary>
    /// <param name="explorerViewOnly">If true, tool can only be used in Explorer view</param>
    /// <param name="nodeRequired">If true, a node must be selected</param>
    /// <param name="checkOutlineIsOpen">If true, checks that an outline is open</param>
    /// <returns>true if all prerequisites are met, false otherwise</returns>
    public bool VerifyToolUse(
        bool explorerViewOnly,
        bool nodeRequired,
        bool checkOutlineIsOpen = true)
    {
        try
        {
            var currentViewType = _appState.CurrentViewType;
            var model = _appState.CurrentDocument?.Model;

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
                if (_appState.RightTappedNode == null)
                {
                    if (_appState.CurrentNode != null)
                    {
                        // Fallback: use selected node as the target (side effect)
                        _appState.RightTappedNode = _appState.CurrentNode;
                    }
                    else
                    {
                        // Both null - validation failure
                        Default.Send(
                            new StatusChangedMessage(new StatusMessage("You need to select a node first", LogLevel.Warn)));
                        return false;
                    }
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogException(LogLevel.Error, ex, "Error in ToolValidationService.VerifyToolUse()");
            return false;
        }
    }
}
