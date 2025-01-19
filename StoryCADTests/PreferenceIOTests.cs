using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.DAL;
using StoryCAD.Models;
using StoryCAD.Models.Tools;

namespace StoryCADTests;

[TestClass]
public class PreferencesIoTests
{
	/// <summary>
	/// Tests that WritePreferences saves JSON with the expected values.
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
		var filePath = Path.Combine(Ioc.Default.GetRequiredService<AppState>().RootDirectory,"Preferences.json");
		Assert.IsTrue(File.Exists(filePath), "Preferences.json was not created!");

		// Read raw JSON to confirm
		var rawJson = File.ReadAllText(filePath);
		PreferencesModel actual = System.Text.Json.JsonSerializer.Deserialize<PreferencesModel>(rawJson);

		Assert.IsNotNull(actual, "Deserialization returned null model!");
		Assert.AreEqual(expectedModel.FirstName, actual.FirstName);
		Assert.AreEqual(expectedModel.LastName, actual.LastName);
		Assert.AreEqual(expectedModel.Email, actual.Email);
		Assert.AreEqual(expectedModel.BackupOnOpen, actual.BackupOnOpen);
	}

	/// <summary>
	/// Tests that ReadPreferences loads values from an existing Preferences.json.
	/// Note that if PreferencesIo still calls Ioc.Default.GetRequiredService<T>(),
	/// you may need to remove/comment out that code or wrap it in try/catch.
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
		File.WriteAllText(filePath, System.Text.Json.JsonSerializer.Serialize<PreferencesModel>(expectedModel));

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
}