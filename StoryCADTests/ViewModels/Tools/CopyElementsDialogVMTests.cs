using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Services;
using StoryCADLib.Services.API;
using StoryCADLib.Services.Dialogs;
using StoryCADLib.Services.Logging;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels;
using StoryCADLib.ViewModels.Tools;

namespace StoryCADTests.ViewModels.Tools;

/// <summary>
/// Tests for CopyElementsDialogVM - the ViewModel for copying story elements
/// between outlines (Issue #482).
/// </summary>
[TestClass]
public class CopyElementsDialogVMTests
{
    #region Constructor Tests

    [TestMethod]
    public void Constructor_HasCorrectDISignature_WithRequiredDependencies()
    {
        // Arrange
        var constructors = typeof(CopyElementsDialogVM).GetConstructors();

        // Act
        var diConstructor = constructors.FirstOrDefault(c =>
            c.GetParameters().Any(p => p.ParameterType == typeof(AppState)) &&
            c.GetParameters().Any(p => p.ParameterType == typeof(OutlineService)) &&
            c.GetParameters().Any(p => p.ParameterType == typeof(StoryCADApi)) &&
            c.GetParameters().Any(p => p.ParameterType == typeof(Windowing)) &&
            c.GetParameters().Any(p => p.ParameterType == typeof(ILogService)));

        // Assert
        Assert.IsNotNull(diConstructor,
            "CopyElementsDialogVM should have a constructor with AppState, OutlineService, StoryCADApi, Windowing, and ILogService parameters");
    }

    [TestMethod]
    public void Constructor_WithValidDependencies_InitializesProperties()
    {
        // Arrange & Act
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();

        // Assert
        Assert.IsNotNull(vm.CopyableTypes, "CopyableTypes should be initialized");
        Assert.IsNotNull(vm.SourceElements, "SourceElements should be initialized");
        Assert.IsNotNull(vm.TargetElements, "TargetElements should be initialized");
    }

    #endregion

    #region CopyableTypes Tests

    [TestMethod]
    public void CopyableTypes_OnGet_ReturnsExpectedTypes()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();
        var expectedTypes = new[]
        {
            StoryItemType.Character,
            StoryItemType.Setting,
            StoryItemType.StoryWorld,
            StoryItemType.Problem,
            StoryItemType.Notes,
            StoryItemType.Web
        };

        // Act
        var copyableTypes = vm.CopyableTypes;

        // Assert
        Assert.AreEqual(expectedTypes.Length, copyableTypes.Count,
            "CopyableTypes should contain exactly 6 types");
        foreach (var type in expectedTypes)
        {
            Assert.IsTrue(copyableTypes.Contains(type),
                $"CopyableTypes should contain {type}");
        }
    }

    [TestMethod]
    public void CopyableTypes_OnGet_ExcludesNonCopyableTypes()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();
        var excludedTypes = new[]
        {
            StoryItemType.StoryOverview,
            StoryItemType.Scene,
            StoryItemType.Folder,
            StoryItemType.Section,
            StoryItemType.TrashCan,
            StoryItemType.Unknown
        };

        // Act
        var copyableTypes = vm.CopyableTypes;

        // Assert
        foreach (var type in excludedTypes)
        {
            Assert.IsFalse(copyableTypes.Contains(type),
                $"CopyableTypes should NOT contain {type}");
        }
    }

    #endregion

    #region Initial State Tests

    [TestMethod]
    public void SourceElements_Initially_IsEmpty()
    {
        // Arrange & Act
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();

        // Assert
        Assert.AreEqual(0, vm.SourceElements.Count,
            "SourceElements should be empty initially");
    }

    [TestMethod]
    public void TargetElements_Initially_IsEmpty()
    {
        // Arrange & Act
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();

        // Assert
        Assert.AreEqual(0, vm.TargetElements.Count,
            "TargetElements should be empty initially");
    }

    [TestMethod]
    public void TargetFilePath_Initially_IsNullOrEmpty()
    {
        // Arrange & Act
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();

        // Assert
        Assert.IsTrue(string.IsNullOrEmpty(vm.TargetFilePath),
            "TargetFilePath should be null or empty initially");
    }

    [TestMethod]
    public void TargetModel_Initially_IsNull()
    {
        // Arrange & Act
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();

        // Assert
        Assert.IsNull(vm.TargetModel,
            "TargetModel should be null initially");
    }

    [TestMethod]
    public void CopiedCount_Initially_IsZero()
    {
        // Arrange & Act
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();

        // Assert
        Assert.AreEqual(0, vm.CopiedCount,
            "CopiedCount should be zero initially");
    }

    [TestMethod]
    public void StatusMessage_Initially_ShowsGuidance()
    {
        // Arrange & Act
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();

        // Assert
        Assert.IsTrue(vm.StatusMessage.Contains("filter") && vm.StatusMessage.Contains("target"),
            "StatusMessage should show guidance about filter and target");
    }

    #endregion

    #region Filter Selection Tests

    [TestMethod]
    public void SelectedFilterType_WhenSet_UpdatesProperty()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();

        // Act
        vm.SelectedFilterType = StoryItemType.Character;

        // Assert
        Assert.AreEqual(StoryItemType.Character, vm.SelectedFilterType,
            "SelectedFilterType should update when set");
    }

    [TestMethod]
    public async Task RefreshSourceElements_WithCharacterFilter_ReturnsOnlyCharacters()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();

        // Create a model with mixed elements
        var model = await outlineService.CreateModel("Test Story", "Test Author", 0);
        new CharacterModel("John Smith", model, model.ExplorerView[0]);
        new CharacterModel("Jane Doe", model, model.ExplorerView[0]);
        new SettingModel("City Park", model, model.ExplorerView[0]);
        appState.CurrentDocument = new StoryDocument(model, "test.stbx");

        // Act
        vm.SelectedFilterType = StoryItemType.Character;
        vm.RefreshSourceElements();

        // Assert
        Assert.AreEqual(2, vm.SourceElements.Count,
            "Should have exactly 2 characters");
        Assert.IsTrue(vm.SourceElements.All(e => e.ElementType == StoryItemType.Character),
            "All elements should be Characters");
    }

    [TestMethod]
    public async Task RefreshSourceElements_WithSettingFilter_ReturnsOnlySettings()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();

        // Create a model with mixed elements
        var model = await outlineService.CreateModel("Test Story", "Test Author", 0);
        new CharacterModel("John Smith", model, model.ExplorerView[0]);
        new SettingModel("City Park", model, model.ExplorerView[0]);
        new SettingModel("Beach House", model, model.ExplorerView[0]);
        appState.CurrentDocument = new StoryDocument(model, "test.stbx");

        // Act
        vm.SelectedFilterType = StoryItemType.Setting;
        vm.RefreshSourceElements();

        // Assert
        Assert.AreEqual(2, vm.SourceElements.Count,
            "Should have exactly 2 settings");
        Assert.IsTrue(vm.SourceElements.All(e => e.ElementType == StoryItemType.Setting),
            "All elements should be Settings");
    }

    [TestMethod]
    public async Task RefreshSourceElements_WithNoCurrentDocument_SourceElementsEmpty()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        appState.CurrentDocument = null;

        // Act
        vm.SelectedFilterType = StoryItemType.Character;
        vm.RefreshSourceElements();

        // Assert
        Assert.AreEqual(0, vm.SourceElements.Count,
            "SourceElements should be empty when no document is open");
    }

    #endregion

    #region Command Existence Tests

    [TestMethod]
    public void BrowseTargetCommand_Exists_AndIsNotNull()
    {
        // Arrange & Act
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();

        // Assert
        Assert.IsNotNull(vm.BrowseTargetCommand,
            "BrowseTargetCommand should exist");
    }

    [TestMethod]
    public void CopyElementCommand_Exists_AndIsNotNull()
    {
        // Arrange & Act
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();

        // Assert
        Assert.IsNotNull(vm.CopyElementCommand,
            "CopyElementCommand should exist");
    }

    [TestMethod]
    public void RemoveElementCommand_Exists_AndIsNotNull()
    {
        // Arrange & Act
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();

        // Assert
        Assert.IsNotNull(vm.RemoveElementCommand,
            "RemoveElementCommand should exist");
    }

    [TestMethod]
    public void MoveUpCommand_Exists_AndIsNotNull()
    {
        // Arrange & Act
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();

        // Assert
        Assert.IsNotNull(vm.MoveUpCommand,
            "MoveUpCommand should exist");
    }

    [TestMethod]
    public void MoveDownCommand_Exists_AndIsNotNull()
    {
        // Arrange & Act
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();

        // Assert
        Assert.IsNotNull(vm.MoveDownCommand,
            "MoveDownCommand should exist");
    }

    [TestMethod]
    public void SaveCommand_Exists_AndIsNotNull()
    {
        // Arrange & Act
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();

        // Assert
        Assert.IsNotNull(vm.SaveCommand,
            "SaveCommand should exist");
    }

    [TestMethod]
    public void CancelCommand_Exists_AndIsNotNull()
    {
        // Arrange & Act
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();

        // Assert
        Assert.IsNotNull(vm.CancelCommand,
            "CancelCommand should exist");
    }

    #endregion

    #region Dialog Title Tests

    [TestMethod]
    public async Task DialogTitle_WithCurrentDocument_IncludesStoryName()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();

        var model = await outlineService.CreateModel("My Great Story", "Test Author", 0);
        appState.CurrentDocument = new StoryDocument(model, "test.stbx");

        // Act
        var title = vm.DialogTitle;

        // Assert
        Assert.IsTrue(title.Contains("My Great Story"),
            $"DialogTitle should contain story name. Actual: {title}");
    }

    #endregion

    #region OpenCopyElementsDialog Tests

    [TestMethod]
    public void OpenCopyElementsDialog_MethodExists_AndReturnsTask()
    {
        // Arrange
        var method = typeof(CopyElementsDialogVM).GetMethod("OpenCopyElementsDialog");

        // Assert
        Assert.IsNotNull(method, "OpenCopyElementsDialog method should exist");
        Assert.AreEqual(typeof(Task), method.ReturnType,
            "OpenCopyElementsDialog should return Task");
    }

    #endregion

    #region Phase 2: File Loading Tests

    [TestMethod]
    public async Task LoadTargetFile_WithValidPath_SetsTargetModel()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();

        // Create a test file
        var testModel = await outlineService.CreateModel("Target Story", "Test Author", 0);
        var testPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StoryCAD", "TestTarget.stbx");
        await outlineService.WriteModel(testModel, testPath);

        try
        {
            // Act
            await vm.LoadTargetFileAsync(testPath);

            // Assert
            Assert.IsNotNull(vm.TargetModel, "TargetModel should be set after loading");
            Assert.AreEqual(testPath, vm.TargetFilePath, "TargetFilePath should be set");
        }
        finally
        {
            // Cleanup
            if (File.Exists(testPath))
                File.Delete(testPath);
        }
    }

    [TestMethod]
    public async Task LoadTargetFile_WithValidPath_UpdatesStatusMessage()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();

        var testModel = await outlineService.CreateModel("Target Story", "Test Author", 0);
        var testPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StoryCAD", "TestTarget2.stbx");
        await outlineService.WriteModel(testModel, testPath);

        try
        {
            // Act
            await vm.LoadTargetFileAsync(testPath);

            // Assert
            Assert.IsTrue(vm.StatusMessage.Contains("loaded") || vm.StatusMessage.Contains("Target"),
                $"StatusMessage should indicate file loaded. Actual: {vm.StatusMessage}");
        }
        finally
        {
            if (File.Exists(testPath))
                File.Delete(testPath);
        }
    }

    [TestMethod]
    public async Task LoadTargetFile_WithInvalidPath_SetsErrorStatus()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();
        var invalidPath = @"C:\NonExistent\FakeFile.stbx";

        // Act
        await vm.LoadTargetFileAsync(invalidPath);

        // Assert
        Assert.IsNull(vm.TargetModel, "TargetModel should be null for invalid path");
        Assert.IsTrue(vm.StatusMessage.Contains("error") || vm.StatusMessage.Contains("Error") ||
                      vm.StatusMessage.Contains("not found") || vm.StatusMessage.Contains("failed"),
            $"StatusMessage should indicate error. Actual: {vm.StatusMessage}");
    }

    [TestMethod]
    public async Task LoadTargetFile_WithCurrentFile_BlocksAndShowsError()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();

        // Set up current document and save it to disk
        var model = await outlineService.CreateModel("Current Story", "Test Author", 0);
        var currentPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StoryCAD", "CurrentFile.stbx");
        await outlineService.WriteModel(model, currentPath);
        appState.CurrentDocument = new StoryDocument(model, currentPath);

        try
        {
            // Act - try to load current file as target
            await vm.LoadTargetFileAsync(currentPath);

            // Assert
            Assert.IsNull(vm.TargetModel, "Should not load current file as target");
            Assert.IsTrue(vm.StatusMessage.Contains("current") || vm.StatusMessage.Contains("same"),
                $"StatusMessage should warn about same file. Actual: {vm.StatusMessage}");
        }
        finally
        {
            // Cleanup
            if (File.Exists(currentPath))
                File.Delete(currentPath);
        }
    }

    [TestMethod]
    public async Task RefreshTargetElements_AfterLoadingFile_PopulatesTargetList()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();

        // Create target file with characters
        var testModel = await outlineService.CreateModel("Target Story", "Test Author", 0);
        new CharacterModel("Target Character 1", testModel, testModel.ExplorerView[0]);
        new CharacterModel("Target Character 2", testModel, testModel.ExplorerView[0]);

        var testPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StoryCAD", "TestTargetWithChars.stbx");
        await outlineService.WriteModel(testModel, testPath);

        try
        {
            // Act
            await vm.LoadTargetFileAsync(testPath);
            vm.SelectedFilterType = StoryItemType.Character;
            vm.RefreshTargetElements();

            // Assert
            Assert.AreEqual(2, vm.TargetElements.Count,
                "Should have 2 characters from target file");
        }
        finally
        {
            if (File.Exists(testPath))
                File.Delete(testPath);
        }
    }

    #endregion

    #region Phase 3: Copy Logic Tests

    [TestMethod]
    public async Task CopyElement_WithSelectedCharacter_AddsToTargetModel()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();

        // Set up source (current) with a character
        var sourceModel = await outlineService.CreateModel("Source Story", "Test Author", 0);
        var sourceChar = new CharacterModel("John Smith", sourceModel, sourceModel.ExplorerView[0]);
        appState.CurrentDocument = new StoryDocument(sourceModel, "source.stbx");

        // Set up target
        var targetModel = await outlineService.CreateModel("Target Story", "Test Author", 0);
        var targetPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StoryCAD", "CopyTestTarget.stbx");
        await outlineService.WriteModel(targetModel, targetPath);

        try
        {
            await vm.LoadTargetFileAsync(targetPath);
            vm.SelectedFilterType = StoryItemType.Character;
            vm.RefreshSourceElements();
            vm.SelectedSourceElement = sourceChar;

            var initialTargetCount = vm.TargetModel.StoryElements
                .Count(e => e.ElementType == StoryItemType.Character);

            // Act
            vm.CopyElement();

            // Assert
            var newTargetCount = vm.TargetModel.StoryElements
                .Count(e => e.ElementType == StoryItemType.Character);
            Assert.AreEqual(initialTargetCount + 1, newTargetCount,
                "Target should have one more character after copy");
        }
        finally
        {
            if (File.Exists(targetPath))
                File.Delete(targetPath);
        }
    }

    [TestMethod]
    public async Task CopyElement_PreservesOriginalUuid_SameAsSource()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();

        var sourceModel = await outlineService.CreateModel("Source Story", "Test Author", 0);
        var sourceChar = new CharacterModel("Jane Doe", sourceModel, sourceModel.ExplorerView[0]);
        var sourceUuid = sourceChar.Uuid;
        appState.CurrentDocument = new StoryDocument(sourceModel, "source.stbx");

        var targetModel = await outlineService.CreateModel("Target Story", "Test Author", 0);
        var targetPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StoryCAD", "CopyUuidTest.stbx");
        await outlineService.WriteModel(targetModel, targetPath);

        try
        {
            await vm.LoadTargetFileAsync(targetPath);
            vm.SelectedFilterType = StoryItemType.Character;
            vm.RefreshSourceElements();
            vm.SelectedSourceElement = sourceChar;

            // Act
            vm.CopyElement();

            // Assert - UUID should be preserved across fictional universe
            var copiedChar = vm.TargetModel.StoryElements
                .FirstOrDefault(e => e.ElementType == StoryItemType.Character && e.Name == "Jane Doe");
            Assert.IsNotNull(copiedChar, "Copied character should exist");
            Assert.AreEqual(sourceUuid, copiedChar.Uuid,
                "Copied element should preserve original UUID across fictional universe");
        }
        finally
        {
            if (File.Exists(targetPath))
                File.Delete(targetPath);
        }
    }

    [TestMethod]
    public async Task CopyElement_IncrementsCopieedCount()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();

        var sourceModel = await outlineService.CreateModel("Source Story", "Test Author", 0);
        var sourceChar = new CharacterModel("Bob Wilson", sourceModel, sourceModel.ExplorerView[0]);
        appState.CurrentDocument = new StoryDocument(sourceModel, "source.stbx");

        var targetModel = await outlineService.CreateModel("Target Story", "Test Author", 0);
        var targetPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StoryCAD", "CopyCountTest.stbx");
        await outlineService.WriteModel(targetModel, targetPath);

        try
        {
            await vm.LoadTargetFileAsync(targetPath);
            vm.SelectedFilterType = StoryItemType.Character;
            vm.RefreshSourceElements();
            vm.SelectedSourceElement = sourceChar;

            var initialCount = vm.CopiedCount;

            // Act
            vm.CopyElement();

            // Assert
            Assert.AreEqual(initialCount + 1, vm.CopiedCount,
                "CopiedCount should increment after copy");
        }
        finally
        {
            if (File.Exists(targetPath))
                File.Delete(targetPath);
        }
    }

    [TestMethod]
    public async Task CopyElement_WithNoSelection_DoesNothing()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();

        var sourceModel = await outlineService.CreateModel("Source Story", "Test Author", 0);
        appState.CurrentDocument = new StoryDocument(sourceModel, "source.stbx");

        var targetModel = await outlineService.CreateModel("Target Story", "Test Author", 0);
        var targetPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StoryCAD", "CopyNoSelectTest.stbx");
        await outlineService.WriteModel(targetModel, targetPath);

        try
        {
            await vm.LoadTargetFileAsync(targetPath);
            vm.SelectedSourceElement = null; // No selection

            var initialCount = vm.CopiedCount;

            // Act
            vm.CopyElement();

            // Assert
            Assert.AreEqual(initialCount, vm.CopiedCount,
                "CopiedCount should not change when nothing selected");
        }
        finally
        {
            if (File.Exists(targetPath))
                File.Delete(targetPath);
        }
    }

    [TestMethod]
    public async Task CopyElement_WithNoTargetLoaded_ShowsError()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();

        var sourceModel = await outlineService.CreateModel("Source Story", "Test Author", 0);
        var sourceChar = new CharacterModel("Test Char", sourceModel, sourceModel.ExplorerView[0]);
        appState.CurrentDocument = new StoryDocument(sourceModel, "source.stbx");

        vm.SelectedFilterType = StoryItemType.Character;
        vm.RefreshSourceElements();
        vm.SelectedSourceElement = sourceChar;
        vm.TargetModel = null; // Explicitly ensure no target

        // Act
        vm.CopyElement();

        // Assert
        Assert.IsTrue(vm.StatusMessage.Contains("target") || vm.StatusMessage.Contains("Target"),
            $"Should show error about no target. Actual: {vm.StatusMessage}");
    }

    [TestMethod]
    public async Task CopyElement_StoryWorld_WhenTargetHasOne_BlocksAndShowsError()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();

        // Source with StoryWorld
        var sourceModel = await outlineService.CreateModel("Source Story", "Test Author", 0);
        var sourceWorld = new StoryWorldModel("Source World", sourceModel, sourceModel.ExplorerView[0]);
        appState.CurrentDocument = new StoryDocument(sourceModel, "source.stbx");

        // Target already has a StoryWorld
        var targetModel = await outlineService.CreateModel("Target Story", "Test Author", 0);
        var existingWorld = new StoryWorldModel("Existing World", targetModel, targetModel.ExplorerView[0]);
        var targetPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StoryCAD", "StoryWorldTest.stbx");
        await outlineService.WriteModel(targetModel, targetPath);

        try
        {
            await vm.LoadTargetFileAsync(targetPath);
            vm.SelectedFilterType = StoryItemType.StoryWorld;
            vm.RefreshSourceElements();
            vm.SelectedSourceElement = sourceWorld;

            var initialCount = vm.CopiedCount;

            // Act
            vm.CopyElement();

            // Assert
            Assert.AreEqual(initialCount, vm.CopiedCount,
                "Should not copy StoryWorld when target already has one");
            Assert.IsTrue(vm.StatusMessage.Contains("StoryWorld") || vm.StatusMessage.Contains("already"),
                $"Should warn about existing StoryWorld. Actual: {vm.StatusMessage}");
        }
        finally
        {
            if (File.Exists(targetPath))
                File.Delete(targetPath);
        }
    }

    [TestMethod]
    public async Task CopyElement_TracksSessionCopiedIds()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();

        var sourceModel = await outlineService.CreateModel("Source Story", "Test Author", 0);
        var sourceChar = new CharacterModel("Session Track Test", sourceModel, sourceModel.ExplorerView[0]);
        appState.CurrentDocument = new StoryDocument(sourceModel, "source.stbx");

        var targetModel = await outlineService.CreateModel("Target Story", "Test Author", 0);
        var targetPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StoryCAD", "SessionTrackTest.stbx");
        await outlineService.WriteModel(targetModel, targetPath);

        try
        {
            await vm.LoadTargetFileAsync(targetPath);
            vm.SelectedFilterType = StoryItemType.Character;
            vm.RefreshSourceElements();
            vm.SelectedSourceElement = sourceChar;

            // Act
            vm.CopyElement();

            // Assert - check that the copied element can be identified as session-copied
            var copiedChar = vm.TargetModel.StoryElements
                .FirstOrDefault(e => e.ElementType == StoryItemType.Character && e.Name == "Session Track Test");
            Assert.IsNotNull(copiedChar, "Copied character should exist");
            Assert.IsTrue(vm.IsSessionCopied(copiedChar.Uuid),
                "Copied element should be tracked as session-copied");
        }
        finally
        {
            if (File.Exists(targetPath))
                File.Delete(targetPath);
        }
    }

    #endregion

    #region Phase 4: Remove and Navigation Tests

    [TestMethod]
    public async Task RemoveElement_WithSessionCopiedElement_RemovesFromTarget()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();

        // Set up source with a character
        var sourceModel = await outlineService.CreateModel("Source Story", "Test Author", 0);
        var sourceChar = new CharacterModel("Remove Test Char", sourceModel, sourceModel.ExplorerView[0]);
        appState.CurrentDocument = new StoryDocument(sourceModel, "source.stbx");

        // Set up target
        var targetModel = await outlineService.CreateModel("Target Story", "Test Author", 0);
        var targetPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StoryCAD", "RemoveTest.stbx");
        await outlineService.WriteModel(targetModel, targetPath);

        try
        {
            await vm.LoadTargetFileAsync(targetPath);
            vm.SelectedFilterType = StoryItemType.Character;
            vm.RefreshSourceElements();
            vm.SelectedSourceElement = sourceChar;

            // Copy the element first
            vm.CopyElement();
            var copiedChar = vm.TargetModel.StoryElements
                .FirstOrDefault(e => e.ElementType == StoryItemType.Character && e.Name == "Remove Test Char");
            Assert.IsNotNull(copiedChar, "Setup: copied character should exist");

            var countBefore = vm.TargetModel.StoryElements.Count(e => e.ElementType == StoryItemType.Character);

            // Act - select the copied element and remove it
            vm.SelectedTargetElement = copiedChar;
            vm.RemoveElement();

            // Assert
            var countAfter = vm.TargetModel.StoryElements.Count(e => e.ElementType == StoryItemType.Character);
            Assert.AreEqual(countBefore - 1, countAfter, "Should have one less character after remove");
        }
        finally
        {
            if (File.Exists(targetPath))
                File.Delete(targetPath);
        }
    }

    [TestMethod]
    public async Task RemoveElement_WithNonSessionElement_ShowsErrorAndDoesNotRemove()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();

        var sourceModel = await outlineService.CreateModel("Source Story", "Test Author", 0);
        appState.CurrentDocument = new StoryDocument(sourceModel, "source.stbx");

        // Create target with an existing character (not copied this session)
        var targetModel = await outlineService.CreateModel("Target Story", "Test Author", 0);
        var existingChar = new CharacterModel("Existing Char", targetModel, targetModel.ExplorerView[0]);
        var targetPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StoryCAD", "RemoveNonSessionTest.stbx");
        await outlineService.WriteModel(targetModel, targetPath);

        try
        {
            await vm.LoadTargetFileAsync(targetPath);
            vm.SelectedFilterType = StoryItemType.Character;
            vm.RefreshTargetElements();

            var countBefore = vm.TargetModel.StoryElements.Count(e => e.ElementType == StoryItemType.Character);

            // Select an existing element (not copied this session)
            var targetChar = vm.TargetModel.StoryElements
                .FirstOrDefault(e => e.ElementType == StoryItemType.Character);
            vm.SelectedTargetElement = targetChar;

            // Act
            vm.RemoveElement();

            // Assert
            var countAfter = vm.TargetModel.StoryElements.Count(e => e.ElementType == StoryItemType.Character);
            Assert.AreEqual(countBefore, countAfter, "Should not remove non-session element");
            Assert.IsTrue(vm.StatusMessage.Contains("session") || vm.StatusMessage.Contains("Session"),
                $"Should show error about session. Actual: {vm.StatusMessage}");
        }
        finally
        {
            if (File.Exists(targetPath))
                File.Delete(targetPath);
        }
    }

    [TestMethod]
    public async Task RemoveElement_WithNoSelection_DoesNothing()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();

        var sourceModel = await outlineService.CreateModel("Source Story", "Test Author", 0);
        appState.CurrentDocument = new StoryDocument(sourceModel, "source.stbx");

        var targetModel = await outlineService.CreateModel("Target Story", "Test Author", 0);
        new CharacterModel("Target Char", targetModel, targetModel.ExplorerView[0]);
        var targetPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StoryCAD", "RemoveNoSelectTest.stbx");
        await outlineService.WriteModel(targetModel, targetPath);

        try
        {
            await vm.LoadTargetFileAsync(targetPath);
            vm.SelectedTargetElement = null; // No selection

            var countBefore = vm.TargetModel.StoryElements.Count(e => e.ElementType == StoryItemType.Character);

            // Act
            vm.RemoveElement();

            // Assert
            var countAfter = vm.TargetModel.StoryElements.Count(e => e.ElementType == StoryItemType.Character);
            Assert.AreEqual(countBefore, countAfter, "Count should not change when nothing selected");
        }
        finally
        {
            if (File.Exists(targetPath))
                File.Delete(targetPath);
        }
    }

    [TestMethod]
    public async Task RemoveElement_DecrementsCopiedCount()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();

        var sourceModel = await outlineService.CreateModel("Source Story", "Test Author", 0);
        var sourceChar = new CharacterModel("Count Test Char", sourceModel, sourceModel.ExplorerView[0]);
        appState.CurrentDocument = new StoryDocument(sourceModel, "source.stbx");

        var targetModel = await outlineService.CreateModel("Target Story", "Test Author", 0);
        var targetPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StoryCAD", "RemoveCountTest.stbx");
        await outlineService.WriteModel(targetModel, targetPath);

        try
        {
            await vm.LoadTargetFileAsync(targetPath);
            vm.SelectedFilterType = StoryItemType.Character;
            vm.RefreshSourceElements();
            vm.SelectedSourceElement = sourceChar;

            // Copy the element first
            vm.CopyElement();
            var countAfterCopy = vm.CopiedCount;

            var copiedChar = vm.TargetModel.StoryElements
                .FirstOrDefault(e => e.ElementType == StoryItemType.Character && e.Name == "Count Test Char");

            // Act - remove the copied element
            vm.SelectedTargetElement = copiedChar;
            vm.RemoveElement();

            // Assert
            Assert.AreEqual(countAfterCopy - 1, vm.CopiedCount, "CopiedCount should decrement after remove");
        }
        finally
        {
            if (File.Exists(targetPath))
                File.Delete(targetPath);
        }
    }

    [TestMethod]
    public async Task MoveUp_InSourceList_SelectsPreviousElement()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();

        var sourceModel = await outlineService.CreateModel("Source Story", "Test Author", 0);
        var char1 = new CharacterModel("Alpha", sourceModel, sourceModel.ExplorerView[0]);
        var char2 = new CharacterModel("Beta", sourceModel, sourceModel.ExplorerView[0]);
        var char3 = new CharacterModel("Gamma", sourceModel, sourceModel.ExplorerView[0]);
        appState.CurrentDocument = new StoryDocument(sourceModel, "source.stbx");

        vm.SelectedFilterType = StoryItemType.Character;
        vm.RefreshSourceElements();

        // Select the second element
        vm.SelectedSourceElement = vm.SourceElements[1];
        var originalIndex = vm.SourceElements.IndexOf(vm.SelectedSourceElement);

        // Act
        vm.MoveUp();

        // Assert
        var newIndex = vm.SourceElements.IndexOf(vm.SelectedSourceElement);
        Assert.AreEqual(originalIndex - 1, newIndex, "Should select previous element");
    }

    [TestMethod]
    public async Task MoveDown_InSourceList_SelectsNextElement()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();

        var sourceModel = await outlineService.CreateModel("Source Story", "Test Author", 0);
        var char1 = new CharacterModel("Alpha", sourceModel, sourceModel.ExplorerView[0]);
        var char2 = new CharacterModel("Beta", sourceModel, sourceModel.ExplorerView[0]);
        var char3 = new CharacterModel("Gamma", sourceModel, sourceModel.ExplorerView[0]);
        appState.CurrentDocument = new StoryDocument(sourceModel, "source.stbx");

        vm.SelectedFilterType = StoryItemType.Character;
        vm.RefreshSourceElements();

        // Select the first element
        vm.SelectedSourceElement = vm.SourceElements[0];
        var originalIndex = vm.SourceElements.IndexOf(vm.SelectedSourceElement);

        // Act
        vm.MoveDown();

        // Assert
        var newIndex = vm.SourceElements.IndexOf(vm.SelectedSourceElement);
        Assert.AreEqual(originalIndex + 1, newIndex, "Should select next element");
    }

    [TestMethod]
    public async Task MoveUp_AtFirstElement_StaysAtFirst()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();

        var sourceModel = await outlineService.CreateModel("Source Story", "Test Author", 0);
        var char1 = new CharacterModel("First", sourceModel, sourceModel.ExplorerView[0]);
        var char2 = new CharacterModel("Second", sourceModel, sourceModel.ExplorerView[0]);
        appState.CurrentDocument = new StoryDocument(sourceModel, "source.stbx");

        vm.SelectedFilterType = StoryItemType.Character;
        vm.RefreshSourceElements();

        // Select the first element
        vm.SelectedSourceElement = vm.SourceElements[0];

        // Act
        vm.MoveUp();

        // Assert
        Assert.AreEqual(0, vm.SourceElements.IndexOf(vm.SelectedSourceElement),
            "Should stay at first element");
    }

    [TestMethod]
    public async Task MoveDown_AtLastElement_StaysAtLast()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();

        var sourceModel = await outlineService.CreateModel("Source Story", "Test Author", 0);
        var char1 = new CharacterModel("First", sourceModel, sourceModel.ExplorerView[0]);
        var char2 = new CharacterModel("Last", sourceModel, sourceModel.ExplorerView[0]);
        appState.CurrentDocument = new StoryDocument(sourceModel, "source.stbx");

        vm.SelectedFilterType = StoryItemType.Character;
        vm.RefreshSourceElements();

        // Select the last element
        var lastIndex = vm.SourceElements.Count - 1;
        vm.SelectedSourceElement = vm.SourceElements[lastIndex];

        // Act
        vm.MoveDown();

        // Assert
        Assert.AreEqual(lastIndex, vm.SourceElements.IndexOf(vm.SelectedSourceElement),
            "Should stay at last element");
    }

    #endregion

    #region Phase 5: Save and Edge Cases Tests

    [TestMethod]
    public async Task SaveAsync_WithTargetLoaded_WritesTargetFile()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();
        var api = Ioc.Default.GetRequiredService<StoryCADApi>();

        // Set up source with a character
        var sourceModel = await outlineService.CreateModel("Source Story", "Test Author", 0);
        var sourceChar = new CharacterModel("Save Test Char", sourceModel, sourceModel.ExplorerView[0]);
        appState.CurrentDocument = new StoryDocument(sourceModel, "source.stbx");

        // Set up target
        var targetModel = await outlineService.CreateModel("Target Story", "Test Author", 0);
        var targetPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StoryCAD", "SaveTest.stbx");
        await outlineService.WriteModel(targetModel, targetPath);

        try
        {
            await vm.LoadTargetFileAsync(targetPath);

            // Diagnostic: Verify API CurrentModel is the target after load
            Assert.IsNotNull(api.CurrentModel, "API.CurrentModel should be set after LoadTargetFileAsync");
            Assert.AreSame(vm.TargetModel, api.CurrentModel,
                "vm.TargetModel and api.CurrentModel should be the same object after load");

            vm.SelectedFilterType = StoryItemType.Character;
            vm.RefreshSourceElements();
            vm.SelectedSourceElement = sourceChar;

            // Copy an element
            vm.CopyElement();

            // Diagnostic: Verify API CurrentModel still matches after copy
            Assert.AreSame(vm.TargetModel, api.CurrentModel,
                "vm.TargetModel and api.CurrentModel should still be same after copy");

            // Verify copy worked before testing save
            var copiedInMemory = vm.TargetModel.StoryElements
                .FirstOrDefault(e => e.ElementType == StoryItemType.Character && e.Name == "Save Test Char");
            Assert.IsNotNull(copiedInMemory, "Setup: character should be copied to target in memory");

            // Diagnostic: Check if character is also in API.CurrentModel
            var copiedInApi = api.CurrentModel.StoryElements
                .FirstOrDefault(e => e.ElementType == StoryItemType.Character && e.Name == "Save Test Char");
            Assert.IsNotNull(copiedInApi, "Character should also be in api.CurrentModel");

            // Act - save to original path
            await vm.SaveAsync();

            // Assert - verify file exists
            Assert.IsTrue(File.Exists(targetPath), "Target file should exist after save");

            // Diagnostic: Check what's in the file
            var fileContent = File.ReadAllText(targetPath);
            Assert.IsTrue(fileContent.Contains("Save Test Char"),
                $"File should contain 'Save Test Char'. File length: {fileContent.Length}, " +
                $"Contains Elements: {fileContent.Contains("\"Elements\"")}, " +
                $"Contains FlattenedExplorerView: {fileContent.Contains("FlattenedExplorerView")}");

            // Verify copied element persisted by reloading
            var reloadedModel = await outlineService.OpenFile(targetPath);
            var charCount = reloadedModel.StoryElements.Count(e => e.ElementType == StoryItemType.Character);
            var savedChar = reloadedModel.StoryElements
                .FirstOrDefault(e => e.ElementType == StoryItemType.Character && e.Name == "Save Test Char");
            Assert.IsNotNull(savedChar, $"Copied character should be in saved file. Reloaded model has {charCount} characters.");
        }
        finally
        {
            if (File.Exists(targetPath))
                File.Delete(targetPath);
        }
    }

    [TestMethod]
    public async Task SaveAsync_WithNoTargetLoaded_DoesNotThrow()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();
        vm.TargetModel = null;

        // Act & Assert - should not throw
        await vm.SaveAsync();
    }

    [TestMethod]
    public async Task SaveAsync_UpdatesStatusMessage()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();

        var sourceModel = await outlineService.CreateModel("Source Story", "Test Author", 0);
        appState.CurrentDocument = new StoryDocument(sourceModel, "source.stbx");

        var targetModel = await outlineService.CreateModel("Target Story", "Test Author", 0);
        var targetPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StoryCAD", "SaveStatusTest.stbx");
        await outlineService.WriteModel(targetModel, targetPath);

        try
        {
            await vm.LoadTargetFileAsync(targetPath);

            // Act
            await vm.SaveAsync();

            // Assert
            Assert.IsTrue(vm.StatusMessage.Contains("Saved") || vm.StatusMessage.Contains("saved"),
                $"Status should indicate save. Actual: {vm.StatusMessage}");
        }
        finally
        {
            if (File.Exists(targetPath))
                File.Delete(targetPath);
        }
    }

    [TestMethod]
    public void Cancel_ResetsVMState()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();

        // Set some state
        vm.CopiedCount = 5;
        vm.StatusMessage = "Some status";

        // Act
        vm.Cancel();

        // Assert - state should be reset
        Assert.AreEqual(0, vm.CopiedCount, "CopiedCount should be reset");
        Assert.IsNull(vm.TargetModel, "TargetModel should be null");
        Assert.IsTrue(string.IsNullOrEmpty(vm.TargetFilePath) || vm.TargetFilePath == string.Empty,
            "TargetFilePath should be empty");
    }

    #endregion
}
