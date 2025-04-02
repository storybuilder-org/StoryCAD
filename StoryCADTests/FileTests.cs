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
using StoryCAD.Services.Outline;
using StoryCAD.ViewModels.SubViewModels;
using StoryCAD.Services.API;
using Octokit;
using static System.Net.Mime.MediaTypeNames;

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
        OutlineViewModel OutlineVM = Ioc.Default.GetRequiredService<OutlineViewModel>();

        //Get ShellVM and clear the StoryModel
        StoryModel storyModel = new();

        OutlineVM.StoryModelFile = Path.Combine(App.ResultsDir, "TestProject.stbx");
        
		string name = Path.GetFileNameWithoutExtension(OutlineVM.StoryModelFile);
        OverviewModel overview = new(name, storyModel, null)
        {
            DateCreated = DateTime.Today.ToString("yyyy-MM-dd"),
            Author = "StoryCAD Tests"
        };

        storyModel.ExplorerView.Add(overview.Node);
        TrashCanModel trash = new(storyModel, null);
        storyModel.ExplorerView.Add(trash.Node); // The trashcan is the second root
        FolderModel narrative = new("Narrative View", storyModel, StoryItemType.Folder, null);
        storyModel.NarratorView.Add(narrative.Node);
        
        //Add three test nodes.
        StoryElement _Problem = new ProblemModel("TestProblem", storyModel, overview.Node);
        CharacterModel _character = new CharacterModel("TestCharacter", storyModel, overview.Node);
        SceneModel _scene = new SceneModel("TestScene", storyModel, overview.Node); 
        //storyModel.ExplorerView.Add(new(_character, overviewNode,StoryItemType.Character));
        //storyModel.ExplorerView.Add(new(_problem, overviewNode,StoryItemType.Problem));
        //storyModel.ExplorerView.Add(new(_scene, overviewNode,StoryItemType.Scene));


        //Check is loaded correctly
        Assert.IsTrue(storyModel.StoryElements.Count == 6);
        Assert.IsTrue(storyModel.StoryElements[0].ElementType == StoryItemType.StoryOverview);

        //Because we have created a file in this way we must populate ProjectFolder and ProjectFile.
        string dir = Path.GetDirectoryName(OutlineVM.StoryModelFile);
		if (File.Exists(OutlineVM.StoryModelFile))
        {
			File.Delete(OutlineVM.StoryModelFile);
        }

		//Write file.
		StoryIO _storyIO = Ioc.Default.GetRequiredService<StoryIO>();
		_storyIO.WriteStory(OutlineVM.StoryModelFile, storyModel).GetAwaiter().GetResult();

        //Sleep to ensure file is written.
        Thread.Sleep(10000);

        //Check file was really written to the disk.
        Assert.IsTrue(File.Exists(OutlineVM.StoryModelFile));
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
    public void InvalidFileAccessTest()
    {
        Assert.IsTrue(StoryIO.IsValidPath("C:\\"));
        Assert.IsFalse(StoryIO.IsValidPath("C:\\:::StoryCADTests::\\//"));
    }


	/// <summary>
	/// Tests full.stbx is loaded correctly.
	/// </summary>
	/// <returns></returns>
    [TestMethod]
    public async Task FullFileTest()
    {
	    string Dir = AppDomain.CurrentDomain.BaseDirectory;
		StorageFile file = await StorageFile.GetFileFromPathAsync(Path.Combine(App.InputDir, "Full.stbx"));
		StoryModel model = await Ioc.Default.GetRequiredService<StoryIO>().ReadStory(file);

		//Overview Model Test
		Assert.IsTrue(((OverviewModel)model.StoryElements[0]).Author == "jake shaw");
		Assert.IsTrue(((OverviewModel)model.StoryElements[0]).DateCreated == "2025-01-03");
		Assert.IsTrue(((OverviewModel)model.StoryElements[0]).StoryIdea.Contains("Test"));
		Assert.IsTrue(((OverviewModel)model.StoryElements[0]).Concept.Contains("Test"));
		Assert.IsTrue(((OverviewModel)model.StoryElements[0]).Premise.Contains("Test"));
		Assert.IsTrue(((OverviewModel)model.StoryElements[0]).StoryType == "Short Story");
		Assert.IsTrue(((OverviewModel)model.StoryElements[0]).Viewpoint.Contains("Limited third person"));
		Assert.IsTrue(((OverviewModel)model.StoryElements[0]).StoryGenre == "Mainstream");
		Assert.IsTrue(((OverviewModel)model.StoryElements[0]).LiteraryDevice == "Metafiction");
		Assert.IsTrue(((OverviewModel)model.StoryElements[0]).Voice == "Third person subjective");
		Assert.IsTrue(((OverviewModel)model.StoryElements[0]).Tense == "Present");
		Assert.IsTrue(((OverviewModel)model.StoryElements[0]).Style == "Mystery");
		Assert.IsTrue(((OverviewModel)model.StoryElements[0]).StructureNotes.Contains("Test"));
		Assert.IsTrue(((OverviewModel)model.StoryElements[0]).Tense == "Present");
		Assert.IsTrue(((OverviewModel)model.StoryElements[0]).Tone == "Indignant");
		Assert.IsTrue(((OverviewModel)model.StoryElements[0]).Notes.Contains("This is a test outline, " +
		"it should have everything populated."));

		//Folder
		FolderModel Fold =
			(FolderModel)model.StoryElements.First(se =>
				se.ElementType == StoryItemType.Folder && !se.Name.Contains("Narrative"));
		Assert.IsTrue(Fold.Name == "New Folder");
		Assert.IsTrue(Fold.Notes.Contains("Test"));

		//Problem Model Test
		ProblemModel prob = (ProblemModel)model.StoryElements.First(se => se.ElementType == StoryItemType.Problem);
		Assert.IsTrue(prob.ProblemType == "Decision");
		Assert.IsTrue(prob.ConflictType == "Person vs. Machine");
		Assert.IsTrue(prob.ProblemCategory == "Complication");
		Assert.IsTrue(prob.Subject == "Abuse");
		Assert.IsTrue(prob.StoryQuestion.Contains("Test"));
		Assert.IsTrue(prob.ProtGoal.Contains("Relief from a false acquisition"));
		Assert.IsTrue(prob.ProtMotive.Contains("Beating a diagnosis or condition"));
		Assert.IsTrue(prob.ProtConflict.Contains("Test"));
		Assert.IsTrue(prob.AntagGoal.Contains("Relief from danger"));
		Assert.IsTrue(prob.AntagConflict.Contains("Test"));
		Assert.IsTrue(prob.AntagMotive.Contains("Avoiding certain death"));
		Assert.IsTrue(prob.Outcome.Contains("Protagonist declines morally"));
		Assert.IsTrue(prob.Method.Contains("Captures or disarms the opponent"));
		Assert.IsTrue(prob.Theme.Contains("Ambition overcomes poverty."));
		Assert.IsTrue(prob.Premise.Contains("Test"));
		Assert.IsTrue(prob.StructureTitle.Contains("Save The Cat"));
		Assert.IsTrue(prob.Notes.Contains("Test"));

		//Character Model Test
		CharacterModel Char = (CharacterModel)model.StoryElements.First(se => se.ElementType == StoryItemType.Character);
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
		SettingModel Sett = (SettingModel)model.StoryElements.First(se => se.ElementType == StoryItemType.Setting);
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
		SceneModel Scen = (SceneModel)model.StoryElements.First(se => se.ElementType == StoryItemType.Scene);
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
		FolderModel Note = (FolderModel)model.StoryElements.First(se => se.ElementType == StoryItemType.Notes);
		Assert.IsTrue(Note.Notes.Contains("Test"));

		//Web Folder
		WebModel Web = (WebModel)model.StoryElements.First(se => se.ElementType == StoryItemType.Web);
        Assert.IsTrue(Web.URL.ToString() == "https://github.com/Rarisma");
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
        StoryModel storyModel = new();
        // Create and add an overview node
		OverviewModel overview = new("Saved Test Project", storyModel, null)
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
		CharacterModel character = new("Aria Windrunner", storyModel, overview.Node)
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

		ProblemModel problem = new("Lost Artifact", storyModel, overview.Node)
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

		SceneModel scene = new("The Hidden Temple", storyModel, overview.Node)
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
		await storyIO.WriteStory(projectFile.Path, storyModel);

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

    [TestMethod]
    public async Task TestAPIWrite()
    {
		//Set up file
        OutlineService outlineService = Ioc.Default.GetRequiredService<OutlineService>();
        StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(Path.GetTempPath());
        StorageFile file = await folder.CreateFileAsync("Test.stbx", CreationCollisionOption.GenerateUniqueName);

		//Create Model
        OperationResult<StoryModel> model = await
            OperationResult<StoryModel>.SafeExecuteAsync(outlineService.CreateModel("Test", "Test", 3));
		Assert.IsTrue(model.IsSuccess);

        OperationResult<bool> write = await OperationResult<bool>.SafeExecuteAsync(outlineService.WriteModel(model.Payload, file.Path));
        Assert.IsTrue(write.IsSuccess);
		Assert.IsTrue(File.Exists(file.Path));
    }
}
