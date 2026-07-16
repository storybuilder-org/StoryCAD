using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services;

#nullable disable

namespace StoryCADTests.Services;

/// <summary>
///     Covers <see cref="PreferenceService.EnsureUserGuidProvisionedAsync" />, the explicit
///     startup step issue #90 D8 (as amended by the design review pass) added so client-side
///     GUID provisioning is a separate, serialized write rather than a side effect of
///     <see cref="StoryCADLib.DAL.PreferencesIo.ReadPreferences" />. Uses a fresh
///     <see cref="PreferenceService" /> instance (not the shared IoC singleton) and an injected
///     write delegate, so nothing here touches a real preferences file.
/// </summary>
[TestClass]
public class PreferenceServiceTests
{
    [TestMethod]
    public async Task StartupProvisioning_EmptyGuid_GeneratesAndPersists()
    {
        var service = new PreferenceService { Model = new PreferencesModel { StoreUserGuid = string.Empty } };
        var writeCallCount = 0;
        PreferencesModel written = null;

        await service.EnsureUserGuidProvisionedAsync(model =>
        {
            writeCallCount++;
            written = model;
            return Task.CompletedTask;
        });

        Assert.IsFalse(string.IsNullOrEmpty(service.Model.StoreUserGuid),
            "an empty GUID must be generated");
        Assert.IsTrue(Guid.TryParse(service.Model.StoreUserGuid, out _),
            "the generated value must be a valid GUID");
        Assert.AreEqual(1, writeCallCount, "the write must happen exactly once");
        Assert.AreSame(service.Model, written, "the write must persist the same model that was updated");
    }

    [TestMethod]
    public async Task StartupProvisioning_ExistingGuid_Unchanged()
    {
        const string existingGuid = "11111111-1111-1111-1111-111111111111";
        var service = new PreferenceService { Model = new PreferencesModel { StoreUserGuid = existingGuid } };
        var writeCallCount = 0;

        await service.EnsureUserGuidProvisionedAsync(_ =>
        {
            writeCallCount++;
            return Task.CompletedTask;
        });

        Assert.AreEqual(existingGuid, service.Model.StoreUserGuid, "an existing GUID must not be overwritten");
        Assert.AreEqual(0, writeCallCount, "provisioning must not write when nothing changed");
    }
}
