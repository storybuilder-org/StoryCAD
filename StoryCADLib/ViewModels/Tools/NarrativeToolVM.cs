using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoryCADLib.Services.Dialogs;
using StoryCADLib.Services.Dialogs.Tools;
using StoryCADLib.Services.Locking;
using StoryCADLib.Services.Outline;

namespace StoryCADLib.ViewModels.Tools;

/// <summary>
///     This is the ViewModel for the Narrative tool.
///     It contains data used via the NarrativeTool ContentDialog.
///     NarrativeTool is used to construct the Narrator view, which is
///     the order a story is really told in rather than explorer.
///     Explorer view is the main view and is how the planning.
///     To open Narrative Tool you can press CTRL + N or access it
///     through the Shell via the Tools Menu (Spanner Icon)
/// </summary>
public class NarrativeToolVM : ObservableRecipient
{
    private readonly AppState _appState;

    private readonly ILogService _logger;

    // ShellViewModel required for CurrentViewType, CurrentNode, and RightTappedNode
    // See ToolValidationService.cs:11-17 for refactoring plan (deferred - requires ~123 changes)
    private readonly ShellViewModel _shellVM;
    private readonly ToolValidationService _toolValidationService;
    private readonly Windowing _windowing;

    private string _message;


    private string _newSectionName;
    public bool IsNarratorSelected = false;
    public StoryNodeItem SelectedNode; //Currently selected node

    public NarrativeToolVM(ShellViewModel shellVM, AppState appState, Windowing windowing,
        ToolValidationService toolValidationService, ILogService logger)
    {
        _shellVM = shellVM;
        _appState = appState;
        _windowing = windowing;
        _toolValidationService = toolValidationService;
        _logger = logger;

        CreateFlyout = new RelayCommand(MakeSection);
        CopyCommand = new RelayCommand(Copy);
        CopyAllUnusedCommand = new RelayCommand(CopyAllUnused);
        DeleteCommand = new RelayCommand(Delete);
    }

    /// <summary>
    ///     Name of a new section
    ///     Bound to the new section name in the new section flyout area.
    /// </summary>
    public string NewSectionName
    {
        get => _newSectionName;
        set => SetProperty(ref _newSectionName, value);
    }

    /// <summary>
    ///     Message show to the user on the UI
    /// </summary>
    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    /// <summary>
    ///     This opens the Narrative Tool Content Dialog.
    /// </summary>
    public async Task OpenNarrativeTool()
    {
        // Use ToolValidationService instead of direct OutlineViewModel dependency
        // Note: Still requires ShellViewModel for state, see ToolValidationService docs for future refactoring
        if (_toolValidationService.VerifyToolUse(
                _shellVM.CurrentViewType,
                _shellVM.CurrentNode,
                _shellVM.RightTappedNode,
                _appState.CurrentDocument?.Model,
                false, // explorerViewOnly
                false)) // nodeRequired
        {
            using (new SerializationLock(_logger))
            {
                try
                {
                    await _windowing.ShowContentDialog(new()
                    {
                        Title = "Narrative Editor",
                        PrimaryButtonText = "Done",
                        Content = new NarrativeTool()
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogException(LogLevel.Error, ex, "Error in OpenNarrativeTool()");
                }
            }
        }
    }


    /// <summary>
    ///     Removes a node from the Narrator view.
    ///     Note: This does NOT trash the element - it only removes it from the narrative sequence.
    ///     The element remains in its original location in the Explorer view.
    ///     Uses StoryNodeItem.Delete(NarratorView) which handles view-specific removal.
    /// </summary>
    internal void Delete()
    {
        try
        {
            if (SelectedNode != null)
            {
                if (SelectedNode.Type == StoryItemType.TrashCan || SelectedNode.IsRoot)
                {
                    Message = "You can't delete this node!";
                }

                if (IsNarratorSelected)
                {
                    SelectedNode.Delete(StoryViewType.NarratorView);
                    Message = $"Deleted {SelectedNode}";
                }
                else
                {
                    Message = "You can't delete from here!";
                }
            }
            else
            {
                _logger.Log(LogLevel.Warn, "Selected node was null, doing nothing");
            }
        }
        catch (Exception _ex)
        {
            _logger.LogException(LogLevel.Error, _ex, "Error in NarrativeToolVM.Delete()");
        }
    }


    /// <summary>
    /// Copies scenes to the narrative view using StoryNodeItem business logic.
    /// If a single scene is selected, copies it.
    /// If a folder/section is selected, copies all its child scenes.
    /// </summary>
    private void Copy()
    {
        try
        {
            _logger.Log(LogLevel.Info, "Starting to copy node between trees.");

            //Check if selection is null
            if (SelectedNode == null)
            {
                _logger.Log(LogLevel.Warn, "No node selected");
                return;
            }

            _logger.Log(LogLevel.Info, $"Node Selected is a {SelectedNode.Type}");

            if (SelectedNode.Type == StoryItemType.Scene)
            {
                // Copy single scene
                if (SelectedNode.CopyToNarratorView(_appState.CurrentDocument.Model))
                {
                    Message = $"Copied {SelectedNode.Name}";
                }
                else
                {
                    Message = "This scene already appears in the narrative view.";
                }
            }
            else if (SelectedNode.Type is StoryItemType.Folder or StoryItemType.Section)
            {
                // Copy all child scenes from folder/section
                var copied = SelectedNode.CopyAllScenesToNarratorView(_appState.CurrentDocument.Model);
                Message = copied > 0
                    ? $"Copied {copied} scene(s) from {SelectedNode.Name}."
                    : "No new scenes to copy.";
            }
            else
            {
                _logger.Log(LogLevel.Warn,
                    $"Node {SelectedNode.Name} ({SelectedNode.Uuid}) wasn't copied, it was a {SelectedNode.Type}");
                Message = "You can't copy that.";
            }
        }
        catch (Exception _ex)
        {
            _logger.LogException(LogLevel.Error, _ex, "Error in NarrativeTool.Copy()");
        }

        _logger.Log(LogLevel.Info, "NarrativeTool.Copy() complete.");
    }

    /// <summary>
    ///     Copies all unused scenes from the explorer view to the narrator view.
    ///     Traverses all scenes in the explorer view and copies those not already in narrator view.
    /// </summary>
    private void CopyAllUnused()
    {
        try
        {
            var explorerRoot = _appState.CurrentDocument.Model.ExplorerView[0];
            var allScenes = explorerRoot.GetAllScenes();
            var copied = 0;

            foreach (var scene in allScenes)
            {
                if (!scene.IsInNarratorView(_appState.CurrentDocument.Model))
                {
                    if (scene.CopyToNarratorView(_appState.CurrentDocument.Model))
                    {
                        copied++;
                    }
                }
            }

            Message = copied > 0
                ? $"Copied {copied} unused scene(s) to narrative view."
                : "No unused scenes to copy.";

            _logger.Log(LogLevel.Info, $"Copied {copied} unused scenes to narrator view");
        }
        catch (Exception _e)
        {
            _logger.LogException(LogLevel.Error, _e, "Error in CopyAllUnused");
        }
    }

    /// <summary>
    ///     Creates new section, with the name of NewSectionName
    ///     in the NarratorView tree
    /// </summary>
    private void MakeSection()
    {
        if (_appState.CurrentDocument?.Model?.CurrentView == null ||
            _appState.CurrentDocument.Model.CurrentView.Count < 0)
        {
            _logger.Log(LogLevel.Warn, "DataSource is empty or null, not adding section");
            return;
        }

        //Check section name isn't empty
        if (string.IsNullOrWhiteSpace(NewSectionName))
        {
            Message = "Please name your section";
            return;
        }

        new FolderModel(NewSectionName, _appState.CurrentDocument.Model, StoryItemType.Folder,
            _appState.CurrentDocument.Model.NarratorView[0]);
        NewSectionName = string.Empty;
        Message = string.Empty;
    }

    #region Relay Commands

    public RelayCommand CopyCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand CopyAllUnusedCommand { get; }
    public RelayCommand CreateFlyout { get; }

    #endregion
}
