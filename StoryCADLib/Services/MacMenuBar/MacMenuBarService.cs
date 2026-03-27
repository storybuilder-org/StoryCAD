using System.ComponentModel;
using StoryCADLib.Services.Logging;
using StoryCADLib.ViewModels;
using static StoryCADLib.Services.MacMenuBar.ObjCRuntime;

namespace StoryCADLib.Services.MacMenuBar;

/// <summary>
/// Builds and manages the native macOS menu bar (NSMenu) for StoryCAD.
/// Replicates the CommandBar menu structure using ObjC runtime P/Invoke.
/// All methods are no-ops on non-macOS platforms.
/// </summary>
public class MacMenuBarService
{
    private readonly ShellViewModel _shellVm;
    private readonly ILogService _logger;
    private MacMenuActionHandler _actionHandler;
    private bool _initialized;

    // IntPtr references to dynamic menu items (state updated at runtime)
    private IntPtr _addFolderItem;
    private IntPtr _addProblemItem;
    private IntPtr _addCharacterItem;
    private IntPtr _addSettingItem;
    private IntPtr _addSceneItem;
    private IntPtr _addWebItem;
    private IntPtr _addNotesItem;
    private IntPtr _addStoryWorldItem;
    private IntPtr _addSectionItem;
    private IntPtr _deleteItem;
    private IntPtr _restoreItem;
    private IntPtr _addToNarrativeItem;
    private IntPtr _removeFromNarrativeItem;
    private IntPtr _convertToSceneItem;
    private IntPtr _convertToProblemItem;
    private IntPtr _collaboratorItem;
    private IntPtr _collaboratorSeparator;


    public MacMenuBarService(ShellViewModel shellVm, ILogService logger)
    {
        _shellVm = shellVm;
        _logger = logger;
    }

    /// <summary>
    /// Builds the native macOS menu bar. Call once from Shell_Loaded.
    /// </summary>
    public void Initialize()
    {
        if (!OperatingSystem.IsMacOS() || _initialized) return;

        try
        {
            _logger.Log(LogLevel.Info, "MacMenuBarService: Initializing native macOS menu bar");

            _actionHandler = new MacMenuActionHandler(_logger);
            _actionHandler.Initialize();

            if (_actionHandler.Instance == IntPtr.Zero)
            {
                _logger.Log(LogLevel.Error, "MacMenuBarService: Failed to create action handler");
                return;
            }

            IntPtr mainMenu = CreateMenu("MainMenu");
            SetAutoenablesItems(mainMenu, false);

            // Build each top-level menu
            AddAppMenu(mainMenu);
            AddFileMenu(mainMenu);
            AddEditMenu(mainMenu);
            AddStoryMenu(mainMenu);
            AddMoveMenu(mainMenu);
            AddToolsMenu(mainMenu);
            AddReportsMenu(mainMenu);
            AddViewMenu(mainMenu);
            AddHelpMenu(mainMenu);

            // Set as the application's main menu
            IntPtr nsApp = objc_msgSend(objc_getClass("NSApplication"), sel_registerName("sharedApplication"));
            objc_msgSend(nsApp, sel_registerName("setMainMenu:"), mainMenu);

            // Subscribe to ViewModel property changes for dynamic state
            _shellVm.PropertyChanged += OnShellVmPropertyChanged;

            // Initial state sync
            UpdateDynamicMenuItems();

            _initialized = true;
            _logger.Log(LogLevel.Info, "MacMenuBarService: Native macOS menu bar initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, $"MacMenuBarService: Failed to initialize: {ex.Message}\n{ex.StackTrace}");
        }
    }

    #region Menu Construction

    private void AddAppMenu(IntPtr mainMenu)
    {
        IntPtr appMenu = CreateMenu("StoryCAD");
        IntPtr appMenuItem = CreateMenuItem("StoryCAD", IntPtr.Zero, "");
        SetSubmenu(appMenuItem, appMenu);
        AddItemToMenu(mainMenu, appMenuItem);

        // About StoryCAD
        IntPtr aboutSel = _actionHandler.RegisterAction("aboutStoryCAD:", () =>
            _shellVm.HelpCommand.Execute(null));
        IntPtr aboutItem = CreateMenuItem("About StoryCAD", aboutSel, "");
        SetTarget(aboutItem, _actionHandler.Instance);
        AddItemToMenu(appMenu, aboutItem);

        AddItemToMenu(appMenu, CreateSeparatorItem());

        // Preferences
        IntPtr prefsSel = _actionHandler.RegisterAction("openPreferences:", () =>
            _shellVm.PreferencesCommand.Execute(null));
        IntPtr prefsItem = CreateMenuItem("Preferences...", prefsSel, ",");
        SetTarget(prefsItem, _actionHandler.Instance);
        SetKeyEquivalentModifierMask(prefsItem, NSEventModifierFlagCommand);
        AddItemToMenu(appMenu, prefsItem);

        AddItemToMenu(appMenu, CreateSeparatorItem());

        // Quit
        IntPtr quitSel = _actionHandler.RegisterAction("quitStoryCAD:", () =>
            _shellVm.ExitCommand.Execute(null));
        IntPtr quitItem = CreateMenuItem("Quit StoryCAD", quitSel, "q");
        SetTarget(quitItem, _actionHandler.Instance);
        SetKeyEquivalentModifierMask(quitItem, NSEventModifierFlagCommand);
        AddItemToMenu(appMenu, quitItem);
    }

    private void AddFileMenu(IntPtr mainMenu)
    {
        IntPtr fileMenu = CreateMenu("File");
        SetAutoenablesItems(fileMenu, false);
        IntPtr fileMenuItem = CreateMenuItem("File", IntPtr.Zero, "");
        SetSubmenu(fileMenuItem, fileMenu);
        AddItemToMenu(mainMenu, fileMenuItem);

        // Open/Create
        IntPtr openSel = _actionHandler.RegisterAction("openFile:", () =>
            _shellVm.OpenFileOpenMenuCommand.Execute(null));
        IntPtr openItem = CreateMenuItem("Open/Create...", openSel, "o");
        SetTarget(openItem, _actionHandler.Instance);
        SetKeyEquivalentModifierMask(openItem, NSEventModifierFlagCommand);
        AddItemToMenu(fileMenu, openItem);

        // Save
        IntPtr saveSel = _actionHandler.RegisterAction("saveFile:", () =>
            _shellVm.SaveFileCommand.Execute(null));
        IntPtr saveItem = CreateMenuItem("Save", saveSel, "s");
        SetTarget(saveItem, _actionHandler.Instance);
        SetKeyEquivalentModifierMask(saveItem, NSEventModifierFlagCommand);
        AddItemToMenu(fileMenu, saveItem);

        // Save As
        IntPtr saveAsSel = _actionHandler.RegisterAction("saveFileAs:", () =>
            _shellVm.SaveAsCommand.Execute(null));
        IntPtr saveAsItem = CreateMenuItem("Save As...", saveAsSel, "S");
        SetTarget(saveAsItem, _actionHandler.Instance);
        SetKeyEquivalentModifierMask(saveAsItem, NSEventModifierFlagCommand | NSEventModifierFlagShift);
        AddItemToMenu(fileMenu, saveAsItem);

        // Create Backup
        IntPtr backupSel = _actionHandler.RegisterAction("createBackup:", () =>
            _shellVm.CreateBackupCommand.Execute(null));
        IntPtr backupItem = CreateMenuItem("Create Backup", backupSel, "b");
        SetTarget(backupItem, _actionHandler.Instance);
        SetKeyEquivalentModifierMask(backupItem, NSEventModifierFlagCommand);
        AddItemToMenu(fileMenu, backupItem);

        AddItemToMenu(fileMenu, CreateSeparatorItem());

        // Close Story
        IntPtr closeSel = _actionHandler.RegisterAction("closeStory:", () =>
            _shellVm.CloseCommand.Execute(null));
        IntPtr closeItem = CreateMenuItem("Close Story", closeSel, "");
        SetTarget(closeItem, _actionHandler.Instance);
        AddItemToMenu(fileMenu, closeItem);
    }

    private void AddEditMenu(IntPtr mainMenu)
    {
        IntPtr editMenu = CreateMenu("Edit");
        SetAutoenablesItems(editMenu, false);
        IntPtr editMenuItem = CreateMenuItem("Edit", IntPtr.Zero, "");
        SetSubmenu(editMenuItem, editMenu);
        AddItemToMenu(mainMenu, editMenuItem);
    }

    private void AddStoryMenu(IntPtr mainMenu)
    {
        IntPtr storyMenu = CreateMenu("Story");
        SetAutoenablesItems(storyMenu, false);
        IntPtr storyMenuItem = CreateMenuItem("Story", IntPtr.Zero, "");
        SetSubmenu(storyMenuItem, storyMenu);
        AddItemToMenu(mainMenu, storyMenuItem);

        // Add elements (dynamic visibility)
        _addFolderItem = AddActionItem(storyMenu, "Add Folder", "addFolder:", "f",
            NSEventModifierFlagOption, () => _shellVm.AddFolderCommand.Execute(null));

        _addSectionItem = AddActionItem(storyMenu, "Add Section", "addSection:", "a",
            NSEventModifierFlagOption, () => _shellVm.AddSectionCommand.Execute(null));

        _addProblemItem = AddActionItem(storyMenu, "Add Problem", "addProblem:", "p",
            NSEventModifierFlagOption, () => _shellVm.AddProblemCommand.Execute(null));

        _addCharacterItem = AddActionItem(storyMenu, "Add Character", "addCharacter:", "c",
            NSEventModifierFlagOption, () => _shellVm.AddCharacterCommand.Execute(null));

        _addSettingItem = AddActionItem(storyMenu, "Add Setting", "addSetting:", "l",
            NSEventModifierFlagOption, () => _shellVm.AddSettingCommand.Execute(null));

        _addSceneItem = AddActionItem(storyMenu, "Add Scene", "addScene:", "s",
            NSEventModifierFlagOption, () => _shellVm.AddSceneCommand.Execute(null));

        _addWebItem = AddActionItem(storyMenu, "Add Website", "addWeb:", "w",
            NSEventModifierFlagOption, () => _shellVm.AddWebCommand.Execute(null));

        _addNotesItem = AddActionItem(storyMenu, "Add Notes", "addNotes:", "n",
            NSEventModifierFlagOption, () => _shellVm.AddNotesCommand.Execute(null));

        _addStoryWorldItem = AddActionItem(storyMenu, "Add StoryWorld", "addStoryWorld:", "b",
            NSEventModifierFlagOption, () => _shellVm.AddStoryWorldCommand.Execute(null));

        AddItemToMenu(storyMenu, CreateSeparatorItem());

        // Element management (dynamic visibility)
        _deleteItem = AddActionItem(storyMenu, "Delete Element", "deleteElement:", "",
            0, () => _shellVm.RemoveStoryElementCommand.Execute(null));

        _restoreItem = AddActionItem(storyMenu, "Restore Element", "restoreElement:", "",
            0, () => _shellVm.RestoreStoryElementCommand.Execute(null));

        AddItemToMenu(storyMenu, CreateSeparatorItem());

        _addToNarrativeItem = AddActionItem(storyMenu, "Add to Narrative", "addToNarrative:", "",
            0, () => _shellVm.AddToNarrativeCommand.Execute(null));

        _removeFromNarrativeItem = AddActionItem(storyMenu, "Remove from Narrative", "removeFromNarrative:", "",
            0, () => _shellVm.RemoveFromNarrativeCommand.Execute(null));

        AddItemToMenu(storyMenu, CreateSeparatorItem());

        _convertToSceneItem = AddActionItem(storyMenu, "Convert to Scene", "convertToScene:", "",
            0, () => _shellVm.ConvertToSceneCommand.Execute(null));

        _convertToProblemItem = AddActionItem(storyMenu, "Convert to Problem", "convertToProblem:", "",
            0, () => _shellVm.ConvertToProblemCommand.Execute(null));
    }

    private void AddMoveMenu(IntPtr mainMenu)
    {
        IntPtr moveMenu = CreateMenu("Move");
        SetAutoenablesItems(moveMenu, false);
        IntPtr moveMenuItem = CreateMenuItem("Move", IntPtr.Zero, "");
        SetSubmenu(moveMenuItem, moveMenu);
        AddItemToMenu(mainMenu, moveMenuItem);

        AddActionItem(moveMenu, "Move Up", "moveUp:", "",
            0, () => _shellVm.MoveUpCommand.Execute(null));
        AddActionItem(moveMenu, "Move Down", "moveDown:", "",
            0, () => _shellVm.MoveDownCommand.Execute(null));
        AddActionItem(moveMenu, "Move Left", "moveLeft:", "",
            0, () => _shellVm.MoveLeftCommand.Execute(null));
        AddActionItem(moveMenu, "Move Right", "moveRight:", "",
            0, () => _shellVm.MoveRightCommand.Execute(null));
    }

    private void AddToolsMenu(IntPtr mainMenu)
    {
        IntPtr toolsMenu = CreateMenu("Tools");
        SetAutoenablesItems(toolsMenu, false);
        IntPtr toolsMenuItem = CreateMenuItem("Tools", IntPtr.Zero, "");
        SetSubmenu(toolsMenuItem, toolsMenu);
        AddItemToMenu(mainMenu, toolsMenuItem);

        // Collaborator (dynamic visibility)
        _collaboratorItem = AddActionItem(toolsMenu, "Collaborator", "launchCollaborator:", "",
            0, () => _shellVm.CollaboratorCommand.Execute(null));

        _collaboratorSeparator = CreateSeparatorItem();
        AddItemToMenu(toolsMenu, _collaboratorSeparator);

        // Narrative Editor
        AddActionItem(toolsMenu, "Narrative Editor", "narrativeEditor:", "n",
            NSEventModifierFlagCommand, () => _shellVm.NarrativeToolCommand.Execute(null));

        // Key Questions
        AddActionItem(toolsMenu, "Key Questions", "keyQuestions:", "k",
            NSEventModifierFlagCommand, () => _shellVm.KeyQuestionsCommand.Execute(null));

        // Topic Information
        AddActionItem(toolsMenu, "Topic Information", "topicInfo:", "",
            0, () => _shellVm.TopicsCommand.Execute(null));

        // Copy Elements
        AddActionItem(toolsMenu, "Copy Elements to Another Outline", "copyElements:", "",
            0, () => _shellVm.CopyElementsCommand.Execute(null));

        AddItemToMenu(toolsMenu, CreateSeparatorItem());

        // Plotting Aids submenu
        IntPtr plottingMenu = CreateMenu("Plotting Aids");
        SetAutoenablesItems(plottingMenu, false);
        IntPtr plottingMenuItem = CreateMenuItem("Plotting Aids", IntPtr.Zero, "");
        SetSubmenu(plottingMenuItem, plottingMenu);
        AddItemToMenu(toolsMenu, plottingMenuItem);

        AddActionItem(plottingMenu, "Master Plots", "masterPlots:", "m",
            NSEventModifierFlagCommand, () => _shellVm.MasterPlotsCommand.Execute(null));
        AddActionItem(plottingMenu, "Dramatic Situations", "dramaticSituations:", "d",
            NSEventModifierFlagCommand, () => _shellVm.DramaticSituationsCommand.Execute(null));
        AddActionItem(plottingMenu, "Stock Scenes", "stockScenes:", "l",
            NSEventModifierFlagCommand, () => _shellVm.StockScenesCommand.Execute(null));
    }

    private void AddReportsMenu(IntPtr mainMenu)
    {
        IntPtr reportsMenu = CreateMenu("Reports");
        SetAutoenablesItems(reportsMenu, false);
        IntPtr reportsMenuItem = CreateMenuItem("Reports", IntPtr.Zero, "");
        SetSubmenu(reportsMenuItem, reportsMenu);
        AddItemToMenu(mainMenu, reportsMenuItem);

        // Print Reports (Cmd+Shift+P)
        AddActionItem(reportsMenu, "Print Reports", "printReports:", "P",
            NSEventModifierFlagCommand | NSEventModifierFlagShift, () => _shellVm.PrintReportsCommand.Execute(null));

        // PDF Reports (Cmd+P)
        AddActionItem(reportsMenu, "PDF Reports", "pdfReports:", "p",
            NSEventModifierFlagCommand, () => _shellVm.ExportReportsToPdfCommand.Execute(null));

        // Scrivener Reports (Cmd+R)
        AddActionItem(reportsMenu, "Scrivener Reports", "scrivenerReports:", "r",
            NSEventModifierFlagCommand, () => _shellVm.ScrivenerReportsCommand.Execute(null));
    }

    private void AddViewMenu(IntPtr mainMenu)
    {
        IntPtr viewMenu = CreateMenu("View");
        SetAutoenablesItems(viewMenu, false);
        IntPtr viewMenuItem = CreateMenuItem("View", IntPtr.Zero, "");
        SetSubmenu(viewMenuItem, viewMenu);
        AddItemToMenu(mainMenu, viewMenuItem);

        // Toggle Navigation Pane
        AddActionItem(viewMenu, "Toggle Navigation Pane", "togglePane:", "",
            0, () => _shellVm.TogglePaneCommand.Execute(null));
    }

    private void AddHelpMenu(IntPtr mainMenu)
    {
        IntPtr helpMenu = CreateMenu("Help");
        SetAutoenablesItems(helpMenu, false);
        IntPtr helpMenuItem = CreateMenuItem("Help", IntPtr.Zero, "");
        SetSubmenu(helpMenuItem, helpMenu);
        AddItemToMenu(mainMenu, helpMenuItem);

        // StoryCAD Help
        AddActionItem(helpMenu, "StoryCAD Help", "showHelp:", "",
            0, () => _shellVm.HelpCommand.Execute(null));
    }

    /// <summary>
    /// Helper: creates a menu item with an action, adds it to a menu, and returns its IntPtr.
    /// </summary>
    private IntPtr AddActionItem(IntPtr menu, string title, string selectorName,
        string keyEquivalent, ulong modifierMask, Action action)
    {
        IntPtr sel = _actionHandler.RegisterAction(selectorName, action);
        IntPtr item = CreateMenuItem(title, sel, keyEquivalent);
        SetTarget(item, _actionHandler.Instance);
        if (modifierMask != 0 && !string.IsNullOrEmpty(keyEquivalent))
        {
            SetKeyEquivalentModifierMask(item, modifierMask);
        }
        AddItemToMenu(menu, item);
        return item;
    }

    #endregion

    #region Dynamic State

    private void OnShellVmPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ShellViewModel.ExplorerVisibility):
            case nameof(ShellViewModel.NarratorVisibility):
            case nameof(ShellViewModel.TrashButtonVisibility):
            case nameof(ShellViewModel.CollaboratorVisibility):
                UpdateDynamicMenuItems();
                break;
        }
    }

    private void UpdateDynamicMenuItems()
    {
        if (!OperatingSystem.IsMacOS() || !_initialized && _actionHandler == null) return;

        try
        {
            bool explorerVisible = _shellVm.ExplorerVisibility == Microsoft.UI.Xaml.Visibility.Visible;
            bool narratorVisible = _shellVm.NarratorVisibility == Microsoft.UI.Xaml.Visibility.Visible;
            bool trashVisible = _shellVm.TrashButtonVisibility == Microsoft.UI.Xaml.Visibility.Visible;
            bool collaboratorVisible = _shellVm.CollaboratorVisibility == Microsoft.UI.Xaml.Visibility.Visible;

            // Add element items: show in Explorer, hide in Narrator/Trash
            SetHidden(_addFolderItem, !explorerVisible);
            SetHidden(_addProblemItem, !explorerVisible);
            SetHidden(_addCharacterItem, !explorerVisible);
            SetHidden(_addSettingItem, !explorerVisible);
            SetHidden(_addSceneItem, !explorerVisible);
            SetHidden(_addWebItem, !explorerVisible);
            SetHidden(_addNotesItem, !explorerVisible);
            SetHidden(_addStoryWorldItem, !explorerVisible);

            // Section: only in Narrator view
            SetHidden(_addSectionItem, !narratorVisible);

            // Element management
            SetHidden(_deleteItem, !explorerVisible);
            SetHidden(_restoreItem, !trashVisible);
            SetHidden(_addToNarrativeItem, !explorerVisible);
            SetHidden(_removeFromNarrativeItem, !narratorVisible);
            SetHidden(_convertToSceneItem, !explorerVisible);
            SetHidden(_convertToProblemItem, !explorerVisible);

            // Collaborator
            SetHidden(_collaboratorItem, !collaboratorVisible);
            SetHidden(_collaboratorSeparator, !collaboratorVisible);
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, $"MacMenuBarService: Failed to update dynamic items: {ex.Message}");
        }
    }

    #endregion
}
