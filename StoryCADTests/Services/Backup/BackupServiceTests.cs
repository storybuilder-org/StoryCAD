using System.IO.Compression;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Services;
using StoryCADLib.Services.Backup;

#nullable disable

namespace StoryCADTests.Services.Backup;

[TestClass]
public class BackupServiceTests
{
    private BackupService _backupService;
    private AppState _appState;
    private PreferenceService _prefs;
    private string _testBackupDir;
    private string _originalBackupDir;

    [TestInitialize]
    public void Setup()
    {
        _backupService = Ioc.Default.GetRequiredService<BackupService>();
        _appState = Ioc.Default.GetRequiredService<AppState>();
        _prefs = Ioc.Default.GetRequiredService<PreferenceService>();

        // Preserve original backup directory
        _originalBackupDir = _prefs.Model.BackupDirectory;

        // Create isolated temp directory for each test
        _testBackupDir = Path.Combine(Path.GetTempPath(), $"BackupTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testBackupDir);
        _prefs.Model.BackupDirectory = _testBackupDir;

        // Point CurrentDocument at a real .stbx file from TestInputs
        var stbxPath = Path.Combine(App.InputDir, "OpenTest.stbx");
        var model = new StoryModel();
        _appState.CurrentDocument = new StoryDocument(model, stbxPath);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _appState.CurrentDocument = null;
        _prefs.Model.BackupDirectory = _originalBackupDir;

        if (Directory.Exists(_testBackupDir))
        {
            try { Directory.Delete(_testBackupDir, true); }
            catch { /* ignore cleanup errors */ }
        }
    }

    [TestMethod]
    public async Task BackupProject_WithDefaults_CreatesZipAtBackupDirectory()
    {
        // Act
        await _backupService.BackupProject();

        // Assert
        var zipFiles = Directory.GetFiles(_testBackupDir, "*.zip");
        Assert.AreEqual(1, zipFiles.Length, "Expected exactly one zip file in backup directory");
        Assert.IsTrue(Path.GetFileName(zipFiles[0]).StartsWith("OpenTest as of"),
            "Zip filename should start with story name and 'as of'");
    }

    [TestMethod]
    public async Task BackupProject_ZipContainsStbxFile()
    {
        // Act
        await _backupService.BackupProject();

        // Assert
        var zipFile = Directory.GetFiles(_testBackupDir, "*.zip").Single();
        using var archive = ZipFile.OpenRead(zipFile);
        Assert.IsTrue(archive.Entries.Any(e => e.Name.EndsWith(".stbx")),
            "Zip archive should contain the .stbx file");
    }

    [TestMethod]
    public async Task BackupProject_CreatesBackupDirectoryIfMissing()
    {
        // Arrange — point to a directory that doesn't exist yet
        var missingDir = Path.Combine(_testBackupDir, "SubDir");
        Assert.IsFalse(Directory.Exists(missingDir));

        // Act
        await _backupService.BackupProject(FilePath: missingDir);

        // Assert
        Assert.IsTrue(Directory.Exists(missingDir), "Backup should have created the missing directory");
        var zipFiles = Directory.GetFiles(missingDir, "*.zip");
        Assert.AreEqual(1, zipFiles.Length, "Expected one zip file in newly created directory");
    }

    [TestMethod]
    public async Task BackupProject_WithExplicitFilename_UsesProvidedName()
    {
        // Act
        await _backupService.BackupProject(Filename: "MyCustomBackup");

        // Assert
        var zipPath = Path.Combine(_testBackupDir, "MyCustomBackup.zip");
        Assert.IsTrue(File.Exists(zipPath), $"Expected zip at {zipPath}");
    }

    [TestMethod]
    public async Task BackupProject_WithExplicitPath_UsesProvidedPath()
    {
        // Arrange
        var customDir = Path.Combine(_testBackupDir, "CustomPath");
        Directory.CreateDirectory(customDir);

        // Act
        await _backupService.BackupProject(FilePath: customDir);

        // Assert
        var zipFiles = Directory.GetFiles(customDir, "*.zip");
        Assert.AreEqual(1, zipFiles.Length, "Expected one zip file at the custom path");
    }
}
