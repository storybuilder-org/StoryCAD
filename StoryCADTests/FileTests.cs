using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.DAL;
using StoryCAD.Models;
using StoryCAD.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using StoryCAD.Services;
using StoryCAD.Services.Logging;

namespace StoryCADTests;

[TestClass]
public class FileTests
{

    /// <summary>
    /// This creates a new STBX File to assure file creation works.
    /// </summary>
    [TestMethod]
    public void FileCreation()
    {
        //Get ShellVM and clear the StoryModel
        StoryModel StoryModel = new()
        {
            ProjectFilename ="TestProject.stbx",
            ProjectPath = Ioc.Default.GetRequiredService<AppState>().RootDirectory
        };

        OverviewModel _overview = new(Path.GetFileNameWithoutExtension("TestProject"), StoryModel)
        { DateCreated = DateTime.Today.ToString("yyyy-MM-dd"), Author = "StoryCAD Tests" };

        StoryNodeItem _overviewNode = new(_overview, null, StoryItemType.StoryOverview) {IsRoot = true };
        StoryModel.ExplorerView.Add(_overviewNode);
        TrashCanModel _trash = new(StoryModel);
        StoryNodeItem _trashNode = new(_trash, null);
        StoryModel.ExplorerView.Add(_trashNode);     // The trashcan is the second root
        FolderModel _narrative = new("Narrative View", StoryModel, StoryItemType.Folder);
        StoryNodeItem _narrativeNode = new(_narrative, null) { IsRoot = true };
        StoryModel.NarratorView.Add(_narrativeNode);

        //Add three test nodes.
        CharacterModel _character = new("TestCharacter", StoryModel);
        ProblemModel _problem = new("TestProblem", StoryModel);
        SceneModel _scene = new(StoryModel) {Name="TestScene" };
        StoryModel.ExplorerView.Add(new(_character, _overviewNode,StoryItemType.Character));
        StoryModel.ExplorerView.Add(new(_problem, _overviewNode,StoryItemType.Problem));
        StoryModel.ExplorerView.Add(new(_scene, _overviewNode,StoryItemType.Scene));


        //Check is loaded correctly
        Assert.IsTrue(StoryModel.StoryElements.Count == 6);
        Assert.IsTrue(StoryModel.StoryElements[0].Type == StoryItemType.StoryOverview);

        //Because we have created a file in this way we must populate ProjectFolder and ProjectFile.
        Directory.CreateDirectory(StoryModel.ProjectPath);

        //Populate file/folder vars.
        StoryModel.ProjectFolder = StorageFolder.GetFolderFromPathAsync(StoryModel.ProjectPath).GetAwaiter().GetResult();
        StoryModel.ProjectFile = StoryModel.ProjectFolder.CreateFileAsync("TestProject.stbx",
	        CreationCollisionOption.ReplaceExisting).GetAwaiter().GetResult();

		//Write file.
		StoryIO _storyIO = Ioc.Default.GetRequiredService<StoryIO>();
		_storyIO.WriteStory(StoryModel.ProjectFile, StoryModel).GetAwaiter().GetResult();

        //Sleep to ensure file is written.
        Thread.Sleep(10000);

        //Check file was really written to the disk.
        Assert.IsTrue(File.Exists(Path.Combine(StoryModel.ProjectPath, StoryModel.ProjectFilename)));
    }


    /// <summary>
    /// This tests a file load to ensure file creation works.
    /// </summary>
    [TestMethod]
    public async Task FileLoad()
    {
        // Arrange
        string filePath = Path.Combine(App.InputDir, "OpenTest.stbx"); // Ensure this file exists and is accessible
        Assert.IsTrue(File.Exists(filePath), "Test file does not exist.");

        StorageFile file = await StorageFile.GetFileFromPathAsync(filePath);
        StoryIO _rdr = Ioc.Default.GetRequiredService<StoryIO>();

        // Act
        StoryModel storyModel = await _rdr.ReadStory(file);

        // Assert
        Assert.AreEqual(6, storyModel.StoryElements.Count, "Story elements count mismatch."); 
        Assert.AreEqual(5, storyModel.ExplorerView.Count, "Overview Children count mismatch"); 
    }


    [TestMethod]
    public Task InvalidFileAccessTest()
    {
	    string Dir = AppDomain.CurrentDomain.BaseDirectory;
	    UnifiedVM UVM = new()
	    {
		    ProjectName = "TestProject",
		    ProjectPath = Path.Combine(Dir, "TestProject")
	    };

		//Check file path validity
		UVM.CheckValidity(null,null);

	    //Check Project Path was reset to default.
		Assert.IsTrue(UVM.ProjectPath == Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
		return null;
    }


	/// <summary>
	/// Tests full.stbx is loaded correctly.
	/// </summary>
	/// <returns></returns>
    [TestMethod]
    public Task FullFileTest()
    {
	    string Dir = AppDomain.CurrentDomain.BaseDirectory;
		StorageFile File = StorageFile.GetFileFromPathAsync(Path.Combine(Dir, "TestInputs", "Full.stbx")).GetAwaiter().GetResult();
		StoryModel Model = Ioc.Default.GetRequiredService<StoryIO>().ReadStory(File).GetAwaiter().GetResult();

		//Overview Model Test
		Assert.IsTrue(((OverviewModel)Model.StoryElements[0]).Author == "jake shaw");
		Assert.IsTrue(((OverviewModel)Model.StoryElements[0]).DateCreated == "2025-01-03");
		Assert.IsTrue(((OverviewModel)Model.StoryElements[0]).StoryIdea.Contains("Test"));
		Assert.IsTrue(((OverviewModel)Model.StoryElements[0]).Concept.Contains("Test"));
		Assert.IsTrue(((OverviewModel)Model.StoryElements[0]).Premise.Contains("Test"));
		Assert.IsTrue(((OverviewModel)Model.StoryElements[0]).StoryType == "Short Story");
		Assert.IsTrue(((OverviewModel)Model.StoryElements[0]).Viewpoint.Contains("Limited third person"));
		Assert.IsTrue(((OverviewModel)Model.StoryElements[0]).StoryGenre == "Mainstream");
		Assert.IsTrue(((OverviewModel)Model.StoryElements[0]).LiteraryDevice == "Metafiction");
		Assert.IsTrue(((OverviewModel)Model.StoryElements[0]).Voice == "Third person subjective");
		Assert.IsTrue(((OverviewModel)Model.StoryElements[0]).Tense == "Present");
		Assert.IsTrue(((OverviewModel)Model.StoryElements[0]).Style == "Mystery");
		Assert.IsTrue(((OverviewModel)Model.StoryElements[0]).StructureNotes.Contains("Test"));
		Assert.IsTrue(((OverviewModel)Model.StoryElements[0]).Tense == "Present");
		Assert.IsTrue(((OverviewModel)Model.StoryElements[0]).Tone == "Indignant");
		Assert.IsTrue(((OverviewModel)Model.StoryElements[0]).Notes.Contains("This is a test outline, " +
		"it should have everything populated."));

		//Folder
		FolderModel Fold =
			(FolderModel)Model.StoryElements.First(se =>
				se.Type == StoryItemType.Folder && !se.Name.Contains("Narrative"));
		Assert.IsTrue(Fold.Name == "New Folder");
		Assert.IsTrue(Fold.Notes.Contains("Test"));

		//Problem Model Test
		ProblemModel Prob = (ProblemModel)Model.StoryElements.First(se => se.Type == StoryItemType.Problem);
		Assert.IsTrue(Prob.ProblemType == "Decision");
		Assert.IsTrue(Prob.ConflictType == "Person vs. Machine");
		Assert.IsTrue(Prob.ProblemCategory == "Complication");
		Assert.IsTrue(Prob.Subject == "Abuse");
		Assert.IsTrue(Prob.StoryQuestion.Contains("Test"));
		Assert.IsTrue(Prob.ProtGoal.Contains("Relief from a false acquisition"));
		Assert.IsTrue(Prob.ProtMotive.Contains("Beating a diagnosis or condition"));
		Assert.IsTrue(Prob.ProtConflict.Contains("Test"));
		Assert.IsTrue(Prob.AntagGoal.Contains("Relief from danger"));
		Assert.IsTrue(Prob.AntagConflict.Contains("Test"));
		Assert.IsTrue(Prob.AntagMotive.Contains("Avoiding certain death"));
		Assert.IsTrue(Prob.Outcome.Contains("Protagonist declines morally"));
		Assert.IsTrue(Prob.Method.Contains("Captures or disarms the opponent"));
		Assert.IsTrue(Prob.Theme.Contains("Ambition overcomes poverty."));
		Assert.IsTrue(Prob.Premise.Contains("Test"));
		Assert.IsTrue(Prob.StructureTitle.Contains("Save The Cat"));
		Assert.IsTrue(Prob.Notes.Contains("Test"));

		//Character Model Test
		CharacterModel Char = (CharacterModel)Model.StoryElements.First(se => se.Type == StoryItemType.Character);
		Assert.IsTrue(Char.Role == "Adman");
		Assert.IsTrue(Char.StoryRole == "Supporting Role");
		Assert.IsTrue(Char.Archetype == "Shapeshifter");
		Assert.IsTrue(Char.CharacterSketch.Contains("Test"));
		Assert.IsTrue(Char.Age == "Test");
		Assert.IsTrue(Char.Sex == "Test");
		Assert.IsTrue(Char.CharHeight == "Test");
		Assert.IsTrue(Char.Weight == "Test");
		Assert.IsTrue(Char.Eyes == "Brown");
		Assert.IsTrue(Char.Hair == "Black");
		Assert.IsTrue(Char.Build == "Muscular");
		Assert.IsTrue(Char.Complexion == "Fair");
		Assert.IsTrue(Char.Race == "African");
		Assert.IsTrue(Char.Nationality  == "Albania");
		Assert.IsTrue(Char.Health  == "Test");
		Assert.IsTrue(Char.PhysNotes.Contains("Test"));
		Assert.IsTrue(Char.Flaw.Contains("Cracking under pressure"));
		Assert.IsTrue(Char.BackStory.Contains("Test"));
		Assert.IsTrue(Char.Economic.Contains("Test"));
		Assert.IsTrue(Char.Education.Contains("Test"));
		Assert.IsTrue(Char.Ethnic.Contains("Test"));
		Assert.IsTrue(Char.Religion.Contains("Test"));
		Assert.IsTrue(Char.Intelligence == "Boorish");
		Assert.IsTrue(Char.Values == "Courage");
		Assert.IsTrue(Char.Focus == "Control");
		Assert.IsTrue(Char.Abnormality == "Depressive");
		Assert.IsTrue(Char.PhysNotes.Contains("Test"));
		Assert.IsTrue(Char.Adventureousness=="Test");
		Assert.IsTrue(Char.Confidence=="Test");
		Assert.IsTrue(Char.Creativity=="Test");
		Assert.IsTrue(Char.Enthusiasm=="Test");
		Assert.IsTrue(Char.Sensitivity=="Test");
		Assert.IsTrue(Char.Sociability=="Test");
		Assert.IsTrue(Char.Aggression=="Test");
		Assert.IsTrue(Char.Conscientiousness=="Test");
		Assert.IsTrue(Char.Adventureousness=="Test");
		Assert.IsTrue(Char.Dominance=="Test");
		Assert.IsTrue(Char.Assurance=="Test");
		Assert.IsTrue(Char.Shrewdness=="Test");
		Assert.IsTrue(Char.Stability=="Test");
		Assert.IsTrue(Char.TraitList[0] == "(Other) Test");
		Assert.IsTrue(Char.Notes.Contains("Test"));

		//Setting Model
		SettingModel Sett = (SettingModel)Model.StoryElements.First(se => se.Type == StoryItemType.Setting);
		Assert.IsTrue(Sett.Locale == "Aboard plane");
		Assert.IsTrue(Sett.Season == "Late Summer");
		Assert.IsTrue(Sett.Period == "Test");
		Assert.IsTrue(Sett.Lighting == "Test");
		Assert.IsTrue(Sett.Weather == "Test");
		Assert.IsTrue(Sett.Temperature == "Test");
		Assert.IsTrue(Sett.Props == "Test");
		Assert.IsTrue(Sett.Summary.Contains("Test"));
		Assert.IsTrue(Sett.Sights.Contains("Test"));
		Assert.IsTrue(Sett.Sounds.Contains("Test"));
		Assert.IsTrue(Sett.Touch.Contains("Test"));
		Assert.IsTrue(Sett.SmellTaste.Contains("Test"));
		Assert.IsTrue(Sett.Notes.Contains("Test"));

		//Scene Model
		SceneModel Scen = (SceneModel)Model.StoryElements.First(se => se.Type == StoryItemType.Scene);
		Assert.IsTrue(Scen.Date == "Test");
		Assert.IsTrue(Scen.Time == "Test");
		Assert.IsTrue(Scen.SceneType == "Contemplative (or sequel) scene");
		Assert.IsTrue(Scen.ValueExchange == "Approval - Disapproval");
		Assert.IsTrue(Scen.Consequences.Contains("Test"));
		Assert.IsTrue(Scen.ProtagGoal == "Relief from a destructive habit or trait");
		Assert.IsTrue(Scen.Opposition == "Disadvantage of birth or station");
		Assert.IsTrue(Scen.ProtagEmotion == "Ambition");
		Assert.IsTrue(Scen.AntagGoal == "Relief from a destructive habit or trait");
		Assert.IsTrue(Scen.Outcome == "Protagonist 'comes to realize'");
		Assert.IsTrue(Scen.Review.Contains("Test"));
		Assert.IsTrue(Scen.NewGoal == "Relief from a destructive habit or trait");
		Assert.IsTrue(Scen.Notes.Contains("Test"));
		
		//Note Folder
		FolderModel Note = (FolderModel)Model.StoryElements.First(se => se.Type == StoryItemType.Notes);
		Assert.IsTrue(Note.Notes.Contains("Test"));

		//Web Folder
		WebModel Web = (WebModel)Model.StoryElements.First(se => se.Type == StoryItemType.Web);
		Assert.IsTrue(Web.URL.ToString() == "https://github.com/Rarisma");
		return null;
    }

    [TestMethod]
    public async Task StructureModelIsLoadedCorrectly()
    {
		// Arrange: load the STBX file that contains the Hero's Journey beats
		string filePath = Path.Combine(App.InputDir, "StructureTests.stbx");
	    Assert.IsTrue(File.Exists(filePath), "Test file does not exist at the given path.");

	    StorageFile file = await StorageFile.GetFileFromPathAsync(filePath);
	    StoryIO storyIO = new StoryIO();

	    // Act: read the story and find the “Main Problem” that has the Hero’s Journey data
	    StoryModel model = await storyIO.ReadStory(file);
	    ProblemModel mainProblem = model.StoryElements
		    .OfType<ProblemModel>()
		    .FirstOrDefault(p => p.Name == "Main Problem");

	    // Assert: confirm structure data is loaded
	    Assert.IsNotNull(mainProblem, "Main Problem with structure data not found.");
	    Assert.AreEqual("Hero's Journey", mainProblem.StructureTitle, "StructureTitle mismatch.");

	    // Check that the beats exist
	    Assert.IsNotNull(mainProblem.StructureBeats, "StructureBeats collection was null.");
	    Assert.IsTrue(mainProblem.StructureBeats.Count == 12, "No structure beats found.");

	    // Quick sample checks on beat data
	    Assert.AreEqual("Ordinary World", mainProblem.StructureBeats[0].Title, "First beat title mismatch.");
            Assert.AreEqual(Guid.Parse("ea818c91-0dd4-47f2-8bcc-7c5841030e09"), mainProblem.StructureBeats[0].Guid, "First bound Beat GUID mismatch");
	    Assert.AreEqual("Refusal of the Call", mainProblem.StructureBeats[2].Title, "Third beat title mismatch.");
            Assert.AreEqual(Guid.Parse("4e7c0217-64e8-4c74-8438-debb584cf3b8"), mainProblem.StructureBeats[2].Guid, "Third bound Beat GUID mismatch");
    }
	
    [TestMethod]
    public async Task MigrationTests()
    {
	    Ioc.Default.GetRequiredService<PreferenceService>().Model.ProjectDirectory = App.InputDir;
	    Ioc.Default.GetRequiredService<PreferenceService>().Model.BackupDirectory = App.ResultsDir;
	    foreach (var file in Directory.GetFiles(Path.Combine(App.InputDir, "Migrations")))
	    {
		    // Load XML
		    StorageFile sample = await StorageFile.GetFileFromPathAsync(file);
		    StoryModel xmlModel = await new LegacyXMLReader(Ioc.Default.GetRequiredService<LogService>()).ReadFile(sample);

		    // Convert
		    StoryIO io = new();
		    StoryModel jsonModel = await io.MigrateModel(sample);

		    // Assert they are the same
		    Assert.AreEqual(xmlModel.StoryElements.Count(), jsonModel.StoryElements.Count(), "Story elements count mismatch.");
		    Assert.IsTrue(xmlModel.ExplorerView.Count() == jsonModel.ExplorerView.Count(), "ExplorerView mismatch.");
		    Assert.IsTrue(xmlModel.NarratorView.Count() == jsonModel.NarratorView.Count(), "NarratorView mismatch.");
	    }
	}


	[TestMethod]
	public async Task FileSaveTest()
	{
		// Arrange
		string testProjectPath = Path.Combine(App.ResultsDir, "TestProject");
		Directory.CreateDirectory(testProjectPath);

		StoryModel storyModel = new()
		{
			ProjectFilename = "SavedTest.stbx",
			ProjectPath = testProjectPath
		};

		// Create and add an overview node
		OverviewModel overview = new("Saved Test Project", storyModel)
		{
			DateCreated = DateTime.Today.ToString("yyyy-MM-dd"),
			Author = "Jane Doe",
			StoryIdea = "A thrilling adventure of self-discovery.",
			Concept = "Exploring the depths of human resilience.",
			Premise = "What defines a hero when facing insurmountable odds?",
			StoryType = "Adventure",
			Viewpoint = "First person",
			StoryGenre = "Fantasy",
			LiteraryDevice = "Foreshadowing",
			Voice = "Narrative",
			Tense = "Past",
			Style = "Descriptive",
			StructureNotes = "Follows a three-act structure.",
			Tone = "Inspirational",
			Notes = "Initial project setup."
		};
		StoryNodeItem overviewNode = new(overview, null, StoryItemType.StoryOverview) { IsRoot = true };
		storyModel.ExplorerView.Add(overviewNode);

		// Add test elements to the story
		CharacterModel character = new("Aria Windrunner", storyModel)
		{
			Role = "Protagonist",
			StoryRole = "Hero",
			Archetype = "The Explorer",
			CharacterSketch = "Brave and curious, with a knack for solving mysteries.",
			Age = "28",
			Sex = "Female",
			CharHeight = "5'7\"",
			Weight = "130 lbs",
			Eyes = "Green",
			Hair = "Chestnut",
			Build = "Athletic",
			Complexion = "Fair",
			Race = "Caucasian",
			Nationality = "American",
			Health = "Excellent",
			PhysNotes = "Scar on left cheek from a past adventure.",
			Flaw = "Impulsive decision-making",
			BackStory = "Grew up in a small town, always yearning for adventure.",
			Economic = "Middle-class background",
			Education = "Bachelor's in Archaeology",
			Ethnic = "British descent",
			Religion = "Agnostic",
			Intelligence = "Highly intelligent",
			Values = "Courage and loyalty",
			Focus = "Discovery",
			Abnormality = "Obsessive tendencies",
			Adventureousness = "High",
			Confidence = "Moderate",
			Creativity = "High",
			Enthusiasm = "Energetic",
			Sensitivity = "High",
			Sociability = "Outgoing",
			Aggression = "Low",
			Conscientiousness = "Very conscientious",
			Dominance = "Moderate",
			Assurance = "Confident",
			Shrewdness = "Sharp",
			Stability = "Stable",
			TraitList = new List<string> { "Resourceful", "Quick-thinking" },
			Notes = "Primary character for testing."
		};

		ProblemModel problem = new("Lost Artifact", storyModel)
		{
			ProblemType = "Mystery",
			ConflictType = "Person vs. Nature",
			ProblemCategory = "Discovery",
			Subject = "Ancient civilizations",
			StoryQuestion = "Can Aria find the lost artifact before it falls into the wrong hands?",
			ProtGoal = "Recover the artifact to preserve history.",
			ProtMotive = "Passion for archaeology and discovery.",
			ProtConflict = "Harsh environmental conditions.",
			AntagGoal = "Obtain the artifact for personal gain.",
			AntagConflict = "Rival archaeologist competing for the same artifact.",
			AntagMotive = "Desire for recognition and wealth.",
			//Antagonist = "Dr. Victor Blackthorn",
			Outcome = "Aria successfully retrieves the artifact but faces moral dilemmas.",
			Method = "Use of ancient maps and modern technology.",
			Theme = "The true value of discovery lies in preservation, not possession.",
			Premise = "In the quest for knowledge, ethical boundaries must be maintained.",
			StructureTitle = "Hero's Journey",
			Notes = "Sets the main conflict for the story."
		};

		SceneModel scene = new(storyModel)
		{
			Name = "The Hidden Temple",
			Date = "June 21, 2025",
			Time = "Dusk",
			SceneType = "Climactic confrontation",
			ValueExchange = "Trust vs. Betrayal",
			Consequences = "Revelation of the artifact's true power.",
			ProtagGoal = "Secure the artifact safely.",
			Opposition = "Natural obstacles and human adversaries.",
			ProtagEmotion = "Determination",
			AntagGoal = "Take possession of the artifact.",
			Outcome = "Artifact is secured, but at a personal cost.",
			Review = "Reflect on the journey and its impacts.",
			NewGoal = "Use the artifact to benefit society.",
			Notes = "Final scene tying up the narrative."
		};

		storyModel.ExplorerView.Add(new(character, overviewNode, StoryItemType.Character));
		storyModel.ExplorerView.Add(new(problem, overviewNode, StoryItemType.Problem));
		storyModel.ExplorerView.Add(new(scene, overviewNode, StoryItemType.Scene));

		// Prepare storage file
		StorageFolder projectFolder = await StorageFolder.GetFolderFromPathAsync(testProjectPath);
		StorageFile projectFile = await projectFolder.CreateFileAsync("SavedTest.stbx",
			CreationCollisionOption.ReplaceExisting);

		// Act
		StoryIO storyIO = Ioc.Default.GetRequiredService<StoryIO>();
		await storyIO.WriteStory(projectFile, storyModel);

		// Assert
		Assert.IsTrue(File.Exists(Path.Combine(testProjectPath, "SavedTest.stbx")), "The file was not saved correctly.");

		// Optional: Load the file back to verify its contents
		StoryModel loadedModel = await storyIO.ReadStory(projectFile);
		Assert.AreEqual(storyModel.StoryElements.Count, loadedModel.StoryElements.Count, "Loaded story elements count mismatch.");

		// Additional Assertions to verify populated fields
		var loadedCharacter = loadedModel.StoryElements
			.OfType<CharacterModel>()
			.FirstOrDefault(c => c.Name == "Aria Windrunner");
		Assert.IsNotNull(loadedCharacter, "Character 'Aria Windrunner' not found.");
		Assert.AreEqual("Protagonist", loadedCharacter.Role);
		Assert.AreEqual("Hero", loadedCharacter.StoryRole);
		Assert.AreEqual("The Explorer", loadedCharacter.Archetype);
		Assert.AreEqual("28", loadedCharacter.Age);
		Assert.AreEqual("Female", loadedCharacter.Sex);
		Assert.AreEqual("Green", loadedCharacter.Eyes);
		Assert.AreEqual("Chestnut", loadedCharacter.Hair);
		Assert.AreEqual("Athletic", loadedCharacter.Build);
		Assert.AreEqual("Fair", loadedCharacter.Complexion);
		Assert.AreEqual("Caucasian", loadedCharacter.Race);
		Assert.AreEqual("American", loadedCharacter.Nationality);
		Assert.AreEqual("Excellent", loadedCharacter.Health);
	}

    [TestMethod]
    public async Task CheckFileAvailability()
    {
        var _storyIO = Ioc.Default.GetRequiredService<StoryIO>();
        string _legacyFilePath = Path.Combine(App.InputDir,"Migrations","LegacyTest.stbx");
        bool result = await _storyIO.CheckFileAvailability(_legacyFilePath);
        Assert.IsTrue(result, $"Expected legacy file at {_legacyFilePath} to be available.");
    }

}
