using System.Text.Json;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.DAL;
using StoryCAD.Models;
using StoryCAD.Models.Tools;
using StoryCAD.Services;

namespace StoryCADTests.DAL;

[TestClass]
public class PreferencesIOTests
{
    /// <summary>
    ///     Tests that WritePreferences saves JSON with the expected values.
    /// </summary>
    [TestMethod]
    public async Task WritePreferences_SavesFileCorrectly()
    {
        // Arrange
        var prefsIo = new PreferencesIo();

        var expectedModel = new PreferencesModel
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane.doe@example.com",
            BackupOnOpen = true
        };

        // Act
        await prefsIo.WritePreferences(expectedModel);

        // Assert file exists
        var filePath = Path.Combine(Ioc.Default.GetRequiredService<AppState>().RootDirectory, "Preferences.json");
        Assert.IsTrue(File.Exists(filePath), "Preferences.json was not created!");

        // Read raw JSON to confirm
        var rawJson = File.ReadAllText(filePath);
        var actual = JsonSerializer.Deserialize<PreferencesModel>(rawJson);

        Assert.IsNotNull(actual, "Deserialization returned null model!");
        Assert.AreEqual(expectedModel.FirstName, actual.FirstName);
        Assert.AreEqual(expectedModel.LastName, actual.LastName);
        Assert.AreEqual(expectedModel.Email, actual.Email);
        Assert.AreEqual(expectedModel.BackupOnOpen, actual.BackupOnOpen);
    }

    /// <summary>
    ///     Tests that ReadPreferences loads values from an existing Preferences.json.
    ///     Note that if PreferencesIo still calls Ioc.Default.GetRequiredService
    ///     <T>
    ///         (),
    ///         you may need to remove/comment out that code or wrap it in try/catch.
    /// </summary>
    [TestMethod]
    public async Task ReadPreferences_LoadsFileCorrectly()
    {
        // Arrange: manually create a valid Preferences.json
        var expectedModel = new PreferencesModel
        {
            FirstName = "Alice",
            LastName = "Smith",
            Email = "alice@example.com",
            AutoSave = true,
            AutoSaveInterval = 10
        };
        var filePath = Path.Combine(Ioc.Default.GetRequiredService<AppState>().RootDirectory, "Preferences.json");
        File.WriteAllText(filePath, JsonSerializer.Serialize(expectedModel));

        var prefsIo = new PreferencesIo();

        // Act
        var actual = await prefsIo.ReadPreferences();

        // Assert
        Assert.AreEqual(expectedModel.FirstName, actual.FirstName);
        Assert.AreEqual(expectedModel.LastName, actual.LastName);
        Assert.AreEqual(expectedModel.Email, actual.Email);
        Assert.AreEqual(expectedModel.AutoSave, actual.AutoSave);
        Assert.AreEqual(expectedModel.AutoSaveInterval, actual.AutoSaveInterval);
    }

    /// <summary>
    ///     Tests for some of the issues that can occur in
    ///     https://github.com/storybuilder-org/StoryCAD/issues/973
    /// </summary>
    [TestMethod]
    public async Task ReadPreferences_ReturnsDefault_WhenFileMissing()
    {
        var _sut = new PreferencesIo();
        var model = await _sut.ReadPreferences();

        Assert.IsNotNull(model);
        Assert.AreEqual(ElementTheme.Default, model.ThemePreference);
        Assert.AreSame(model, Ioc.Default.GetRequiredService<PreferenceService>().Model); // service was updated
    }

    /// <summary>
    ///     Tests for some of the issues that can occur in
    ///     https://github.com/storybuilder-org/StoryCAD/issues/973
    /// </summary>
    [TestMethod]
    public async Task ReadPreferences_DeserialisesJson_WhenFilePresent()
    {
        var _sut = new PreferencesIo();

        // arrange
        var expected = new PreferencesModel { ThemePreference = ElementTheme.Dark };
        var json = JsonSerializer.Serialize(expected);
        await File.WriteAllTextAsync(Path.Combine(Ioc.Default.GetRequiredService<AppState>().RootDirectory,
            "Preferences.json"), json);

        // act
        var model = await _sut.ReadPreferences();

        // assert
        Assert.AreEqual(expected.ThemePreference, model.ThemePreference);
        Assert.AreSame(model, Ioc.Default.GetRequiredService<PreferenceService>().Model);
    }
}
