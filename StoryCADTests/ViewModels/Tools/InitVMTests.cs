using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Services;
using StoryCADLib.Services.Backend;
using StoryCADLib.ViewModels.Tools;
using StoryCADTests.Services.Backend;

#nullable disable

namespace StoryCADTests.ViewModels.Tools;

[TestClass]
public class InitVMTests
{
    /// <summary>
    ///     Issue #1596: startup provisions StoreUserGuid before the first-run page saves. The save
    ///     writes a new PreferencesModel, which must keep the GUID or beta Join sends an empty one.
    /// </summary>
    [TestMethod]
    public async Task Save_WhenStoreUserGuidProvisioned_KeepsStoreUserGuid()
    {
        // Arrange
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
        var original = preferenceService.Model;
        var backend = new BackendService(new TestLogService(), Ioc.Default.GetRequiredService<AppState>(),
            preferenceService, new TestMySqlIo());
        var root = Path.Combine(Path.GetTempPath(), "InitVMTests_" + Guid.NewGuid().ToString("N"));
        var guid = Guid.NewGuid().ToString();
        preferenceService.Model.StoreUserGuid = guid;
        var vm = new InitVM(preferenceService, backend)
        {
            ProjectDir = Path.Combine(root, "Projects"),
            BackupDir = Path.Combine(root, "Backups")
        };

        try
        {
            // Act
            await vm.Save();

            // Assert
            Assert.AreEqual(guid, preferenceService.Model.StoreUserGuid);
        }
        finally
        {
            preferenceService.Model = original;
            await new StoryCADLib.DAL.PreferencesIo().WritePreferences(original);
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }
}
