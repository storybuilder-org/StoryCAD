using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.DAL;
using StoryCADLib.Models;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services;
using StoryCADLib.Services.Backend;
using StoryCADLib.Services.Json;
using StoryCADLib.Services.Ratings;
using StoryCADLib.ViewModels.Tools;
using StoryCADTests.Services.Backend;

#nullable disable

namespace StoryCADTests.ViewModels;

[TestClass]
public class PreferencesViewModelTests
{
    #region A1: LoadModel Tests

    [TestMethod]
    public void LoadModel_WithPopulatedModel_CopiesAllUserFields()
    {
        // Arrange
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
        var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
        preferenceService.Model.FirstName = "Jane";
        preferenceService.Model.LastName = "Doe";
        preferenceService.Model.Email = "jane@example.com";
        vm.CurrentModel = preferenceService.Model;

        // Act
        vm.LoadModel();

        // Assert
        Assert.AreEqual("Jane", vm.FirstName);
        Assert.AreEqual("Doe", vm.LastName);
        Assert.AreEqual("jane@example.com", vm.Email);
    }

    [TestMethod]
    public void LoadModel_WithPopulatedModel_CopiesConsentFlags()
    {
        // Arrange
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
        var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
        preferenceService.Model.ErrorCollectionConsent = true;
        preferenceService.Model.Newsletter = true;
        vm.CurrentModel = preferenceService.Model;

        // Act
        vm.LoadModel();

        // Assert
        Assert.IsTrue(vm.ErrorCollectionConsent);
        Assert.IsTrue(vm.Newsletter);
    }

    [TestMethod]
    public void LoadModel_WithPopulatedModel_CopiesBackupSettings()
    {
        // Arrange
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
        var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
        preferenceService.Model.AutoSave = true;
        preferenceService.Model.AutoSaveInterval = 30;
        preferenceService.Model.BackupOnOpen = true;
        preferenceService.Model.TimedBackup = true;
        preferenceService.Model.TimedBackupInterval = 15;
        vm.CurrentModel = preferenceService.Model;

        // Act
        vm.LoadModel();

        // Assert
        Assert.IsTrue(vm.AutoSave);
        Assert.AreEqual(30, vm.AutoSaveInterval);
        Assert.IsTrue(vm.BackupOnOpen);
        Assert.IsTrue(vm.TimedBackup);
        Assert.AreEqual(15, vm.TimedBackupInterval);
    }

    [TestMethod]
    public void LoadModel_WithPopulatedModel_CopiesDirectoryPaths()
    {
        // Arrange
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
        var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
        preferenceService.Model.ProjectDirectory = @"C:\Projects";
        preferenceService.Model.BackupDirectory = @"C:\Backups";
        vm.CurrentModel = preferenceService.Model;

        // Act
        vm.LoadModel();

        // Assert
        Assert.AreEqual(@"C:\Projects", vm.ProjectDirectory);
        Assert.AreEqual(@"C:\Backups", vm.BackupDirectory);
    }

    [TestMethod]
    public void LoadModel_WithPopulatedModel_CopiesDisplaySettings()
    {
        // Arrange
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
        var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
        preferenceService.Model.ThemePreference = ElementTheme.Dark;
        preferenceService.Model.PreferredSearchEngine = BrowserType.Google;
        preferenceService.Model.AdvancedLogging = true;
        preferenceService.Model.ShowStartupDialog = false;
        preferenceService.Model.ShowFilePickerOnStartup = true;
        preferenceService.Model.UseBetaDocumentation = true;
        vm.CurrentModel = preferenceService.Model;

        // Act
        vm.LoadModel();

        // Assert
        Assert.AreEqual(2, vm.PreferredThemeIndex); // Dark = 2
        Assert.AreEqual(BrowserType.Google, vm.PreferredSearchEngine);
        Assert.IsTrue(vm.AdvancedLogging);
        Assert.IsFalse(vm.ShowStartupPage);
        Assert.IsTrue(vm.ShowFilePickerOnStartup);
        Assert.IsTrue(vm.UseBetaDocumentation);
    }

    #endregion

    #region A2: SaveModel Tests

    [TestMethod]
    public void SaveModel_AfterPropertyChanges_WritesUserFieldsBackToModel()
    {
        // Arrange
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
        var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
        vm.CurrentModel = preferenceService.Model;
        vm.LoadModel();
        vm.FirstName = "Updated";
        vm.LastName = "Name";
        vm.Email = "updated@example.com";

        // Act
        vm.SaveModel();

        // Assert
        Assert.AreEqual("Updated", preferenceService.Model.FirstName);
        Assert.AreEqual("Name", preferenceService.Model.LastName);
        Assert.AreEqual("updated@example.com", preferenceService.Model.Email);
    }

    [TestMethod]
    public void SaveModel_AfterPropertyChanges_WritesConsentFlagsBackToModel()
    {
        // Arrange
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
        var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
        vm.CurrentModel = preferenceService.Model;
        vm.LoadModel();
        vm.ErrorCollectionConsent = true;
        vm.Newsletter = false;

        // Act
        vm.SaveModel();

        // Assert
        Assert.IsTrue(preferenceService.Model.ErrorCollectionConsent);
        Assert.IsFalse(preferenceService.Model.Newsletter);
    }

    [TestMethod]
    public void SaveModel_AfterPropertyChanges_WritesBackupSettingsBackToModel()
    {
        // Arrange
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
        var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
        vm.CurrentModel = preferenceService.Model;
        vm.LoadModel();
        vm.AutoSave = true;
        vm.AutoSaveInterval = 45;
        vm.BackupOnOpen = false;
        vm.TimedBackup = true;
        vm.TimedBackupInterval = 30;

        // Act
        vm.SaveModel();

        // Assert
        Assert.IsTrue(preferenceService.Model.AutoSave);
        Assert.AreEqual(45, preferenceService.Model.AutoSaveInterval);
        Assert.IsFalse(preferenceService.Model.BackupOnOpen);
        Assert.IsTrue(preferenceService.Model.TimedBackup);
        Assert.AreEqual(30, preferenceService.Model.TimedBackupInterval);
    }

    [TestMethod]
    public void SaveModel_WhenThemeUnchanged_ThemeChangedIsFalse()
    {
        // Arrange
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
        var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
        preferenceService.Model.ThemePreference = ElementTheme.Light;
        vm.CurrentModel = preferenceService.Model;
        vm.LoadModel();

        // Act
        vm.SaveModel();

        // Assert
        Assert.IsFalse(vm.ThemeChanged);
    }

    [TestMethod]
    public void SaveModel_WhenThemeChanged_ThemeChangedIsTrue()
    {
        // Arrange
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
        var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
        preferenceService.Model.ThemePreference = ElementTheme.Light;
        vm.CurrentModel = preferenceService.Model;
        vm.LoadModel();
        vm.PreferredThemeIndex = 2; // Dark

        // Act
        vm.SaveModel();

        // Assert
        Assert.IsTrue(vm.ThemeChanged);
        Assert.AreEqual(ElementTheme.Dark, preferenceService.Model.ThemePreference);
    }

    #endregion

    #region A3: SaveAsync Tests

    [TestMethod]
    public async Task SaveAsync_WithValidModel_PersistsPreferencesToDisk()
    {
        // Arrange
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
        var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
        preferenceService.Model.FirstName = "SaveTest";
        preferenceService.Model.Email = "save@example.com";
        vm.CurrentModel = preferenceService.Model;
        vm.LoadModel();
        vm.SaveModel();

        // Act
        await vm.SaveAsync();

        // Assert — read back from disk
        var prefsIo = new PreferencesIo();
        var reloaded = await prefsIo.ReadPreferences();
        Assert.AreEqual("SaveTest", reloaded.FirstName);
        Assert.AreEqual("save@example.com", reloaded.Email);
    }

    [TestMethod]
    public async Task SaveAsync_AfterSave_SetsRecordPreferencesStatusToFalse()
    {
        // Arrange
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
        var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
        preferenceService.Model.RecordPreferencesStatus = true;
        vm.CurrentModel = preferenceService.Model;
        vm.LoadModel();

        // Act
        await vm.SaveAsync();

        // Assert
        Assert.IsFalse(preferenceService.Model.RecordPreferencesStatus);
    }

    #endregion

    #region A4: Computed Property Tests

    [TestMethod]
    public void SearchEngineIndex_WhenSet_UpdatesPreferredSearchEngine()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();

        // Act
        vm.SearchEngineIndex = 1; // Google

        // Assert
        Assert.AreEqual(BrowserType.Google, vm.PreferredSearchEngine);
    }

    [TestMethod]
    public void SearchEngineIndex_WhenRead_ReturnsEnumAsInt()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
        vm.PreferredSearchEngine = BrowserType.Bing;

        // Act & Assert
        Assert.AreEqual(2, vm.SearchEngineIndex);
    }

    [TestMethod]
    public void PreferredThemeIndex_WhenSet_UpdatesModelOnSave()
    {
        // Arrange
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
        var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
        vm.CurrentModel = preferenceService.Model;
        vm.LoadModel();
        vm.PreferredThemeIndex = 1; // Light

        // Act
        vm.SaveModel();

        // Assert
        Assert.AreEqual(ElementTheme.Light, preferenceService.Model.ThemePreference);
    }

    [TestMethod]
    public void AppStoreReviewButtonText_OnWindows_ReturnsWindowsStoreText()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();

        // Act
        var text = vm.AppStoreReviewButtonText;

        // Assert — test runs on Windows
        if (OperatingSystem.IsWindows())
            Assert.AreEqual("Review StoryCAD on the Microsoft Store", text);
    }

    #endregion

    #region A5: PropertyChanged Notification Tests

    [TestMethod]
    public void FirstName_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
        vm.FirstName = "Before";
        var propertyChanged = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(PreferencesViewModel.FirstName))
                propertyChanged = true;
        };

        // Act
        vm.FirstName = "After";

        // Assert
        Assert.IsTrue(propertyChanged);
    }

    [TestMethod]
    public void ErrorCollectionConsent_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
        vm.ErrorCollectionConsent = false;
        var propertyChanged = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(PreferencesViewModel.ErrorCollectionConsent))
                propertyChanged = true;
        };

        // Act
        vm.ErrorCollectionConsent = true;

        // Assert
        Assert.IsTrue(propertyChanged);
    }

    [TestMethod]
    public void AutoSave_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
        vm.AutoSave = false;
        var propertyChanged = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(PreferencesViewModel.AutoSave))
                propertyChanged = true;
        };

        // Act
        vm.AutoSave = true;

        // Assert
        Assert.IsTrue(propertyChanged);
    }

    #endregion

    #region C1: DeleteMyDataAsync Tests

    [TestMethod]
    public async Task DeleteMyDataAsync_WhenBackendSucceeds_ClearsLocalDataAndReturnsTrue()
    {
        // Arrange
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
        var testLogger = new TestLogService();
        var testSqlIo = new TestMySqlIo();
        testSqlIo.SetConnectionString("fake");
        var backendService = new BackendService(testLogger,
            Ioc.Default.GetRequiredService<AppState>(), preferenceService, testSqlIo);
        var vm = new PreferencesViewModel(preferenceService, backendService,
            Ioc.Default.GetRequiredService<RatingService>(),
            Ioc.Default.GetRequiredService<Windowing>());

        preferenceService.Model.FirstName = "Test";
        preferenceService.Model.LastName = "User";
        preferenceService.Model.Email = "test@example.com";
        preferenceService.Model.UserId = 42;
        preferenceService.Model.PreferencesInitialized = true;
        preferenceService.Model.ErrorCollectionConsent = true;
        preferenceService.Model.Newsletter = true;
        vm.CurrentModel = preferenceService.Model;
        vm.LoadModel();

        // Act
        var result = await vm.DeleteMyDataAsync();

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(string.Empty, preferenceService.Model.FirstName);
        Assert.AreEqual(string.Empty, preferenceService.Model.LastName);
        Assert.AreEqual(string.Empty, preferenceService.Model.Email);
        Assert.AreEqual(0, preferenceService.Model.UserId);
        Assert.IsFalse(preferenceService.Model.PreferencesInitialized);
        Assert.IsFalse(preferenceService.Model.ErrorCollectionConsent);
        Assert.IsFalse(preferenceService.Model.Newsletter);
    }

    [TestMethod]
    public async Task DeleteMyDataAsync_WhenBackendFails_DoesNotClearLocalDataAndReturnsFalse()
    {
        // Arrange
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
        var testLogger = new TestLogService();
        var testSqlIo = new TestMySqlIo();
        testSqlIo.SetConnectionString("fake");
        testSqlIo.ExceptionToThrow = new Exception("Connection lost");
        var backendService = new BackendService(testLogger,
            Ioc.Default.GetRequiredService<AppState>(), preferenceService, testSqlIo);
        var vm = new PreferencesViewModel(preferenceService, backendService,
            Ioc.Default.GetRequiredService<RatingService>(),
            Ioc.Default.GetRequiredService<Windowing>());

        preferenceService.Model.FirstName = "Test";
        preferenceService.Model.LastName = "User";
        preferenceService.Model.Email = "test@example.com";
        preferenceService.Model.UserId = 42;
        preferenceService.Model.PreferencesInitialized = true;
        vm.CurrentModel = preferenceService.Model;
        vm.LoadModel();

        // Act
        var result = await vm.DeleteMyDataAsync();

        // Assert — local data should NOT be cleared
        Assert.IsFalse(result);
        Assert.AreEqual("Test", preferenceService.Model.FirstName);
        Assert.AreEqual("test@example.com", preferenceService.Model.Email);
        Assert.IsTrue(preferenceService.Model.PreferencesInitialized);
    }

    [TestMethod]
    public async Task DeleteMyDataAsync_WhenBackendNotConfigured_ClearsLocalDataAndReturnsTrue()
    {
        // Arrange — no backend configured means no remote data exists
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
        var testLogger = new TestLogService();
        var testSqlIo = new TestMySqlIo(); // not configured
        var backendService = new BackendService(testLogger,
            Ioc.Default.GetRequiredService<AppState>(), preferenceService, testSqlIo);
        var vm = new PreferencesViewModel(preferenceService, backendService,
            Ioc.Default.GetRequiredService<RatingService>(),
            Ioc.Default.GetRequiredService<Windowing>());

        preferenceService.Model.FirstName = "Test";
        preferenceService.Model.LastName = "User";
        preferenceService.Model.Email = "test@example.com";
        preferenceService.Model.PreferencesInitialized = true;
        vm.CurrentModel = preferenceService.Model;
        vm.LoadModel();

        // Act
        var result = await vm.DeleteMyDataAsync();

        // Assert — local data cleared since no remote data existed
        Assert.IsTrue(result);
        Assert.AreEqual(string.Empty, preferenceService.Model.FirstName);
        Assert.AreEqual(string.Empty, preferenceService.Model.Email);
        Assert.IsFalse(preferenceService.Model.PreferencesInitialized);
    }

    #endregion
}
