using CommunityToolkit.Mvvm.Messaging;
using StoryCAD.DAL;
using StoryCAD.Services.Messages;
using Windows.Storage;

namespace StoryCAD.Services.Outline;

public class OutlineService
{

    /// <summary>
    /// Creates a new story model
    /// </summary>
    /// <param name="name">name of the outline, i.e old man and the sea</param>
    /// <param name="author">Creator of the outline</param>
    /// <param name="selectedTemplateIndex">Template index</param>
    /// <returns>A story outline variable.</returns>
    public async Task<StoryModel> CreateModel(string name, string author, int selectedTemplateIndex)
    {
        //TODO: Make template index an enum.
        StoryModel model = new();

        OverviewModel overview = new(name, model)
        {
            DateCreated = DateTime.Today.ToString("yyyy-MM-dd"),
            Author = author
        };

        StoryNodeItem overviewNode = new(overview, null) { IsExpanded = true, IsRoot = true };
        model.ExplorerView.Add(overviewNode);
        TrashCanModel trash = new(model);
        StoryNodeItem trashNode = new(trash, null);
        model.ExplorerView.Add(trashNode); // The trashcan is the second root
        FolderModel narrative = new("Narrative View", model, StoryItemType.Folder);
        StoryNodeItem narrativeNode = new(narrative, null) { IsRoot = true };
        model.NarratorView.Add(narrativeNode);

        // For non-blank projects, add a StoryProblem with characters.
        if (selectedTemplateIndex != 0)
        {
            StoryElement storyProblem = new ProblemModel("Story Problem", model);
            StoryNodeItem storyProblemNode = new(storyProblem, null);
            StoryElement storyProtag = new CharacterModel("Protagonist", model);
            StoryNodeItem storyProtagNode = new StoryNodeItem(storyProtag, null);
            StoryElement storyAntag = new CharacterModel("Antagonist", model);
            StoryNodeItem storyAntagNode = new StoryNodeItem(storyAntag, null);
            overview.StoryProblem = storyProblem.Uuid;
            var problem = storyProblem as ProblemModel;
            problem.Protagonist = storyProtag.Uuid;
            problem.Antagonist = storyAntag.Uuid;
            problem.Premise =
                @"Your[protagonist] in a situation[genre, setting] wants something[goal], which brings him" +
                @"into [conflict] with a second character[antagonist]. After a series of conflicts[additional " +
                @"problems], the final battle[climax scene] erupts, and the[protagonist] finally resolves the " +
                @"conflict[outcome].";

            switch (selectedTemplateIndex)
            {
                case 1:
                    overviewNode.Children.Add(storyProblemNode);
                    storyProblemNode.Children.Add(storyProtagNode);
                    storyProblemNode.Children.Add(storyAntagNode);
                    storyProblemNode.Parent = overviewNode;
                    storyProtagNode.Parent = storyProblemNode;
                    storyAntagNode.Parent = storyProblemNode;
                    storyProblemNode.IsExpanded = true;
                    break;
                case 2:
                    StoryElement problems = new FolderModel("Problems", model, StoryItemType.Folder);
                    StoryNodeItem problemsNode = new(problems, overviewNode);
                    storyProblemNode.Parent = problemsNode;
                    StoryElement characters = new FolderModel("Characters", model, StoryItemType.Folder);
                    StoryNodeItem charactersNode = new(characters, overviewNode);
                    storyProtagNode.Parent = charactersNode;
                    storyAntagNode.Parent = charactersNode;
                    StoryElement settings = new FolderModel("Settings", model, StoryItemType.Folder);
                    StoryNodeItem settingsNode = new(settings, overviewNode);
                    StoryElement scenes = new FolderModel("Scenes", model, StoryItemType.Folder);
                    StoryNodeItem scenesNode = new(scenes, overviewNode);
                    overview.StoryProblem = storyProblem.Uuid;
                    problemsNode.Children.Add(storyProblemNode);
                    charactersNode.Children.Add(storyProtagNode);
                    charactersNode.Children.Add(storyAntagNode);
                    problemsNode.IsExpanded = true;
                    charactersNode.IsExpanded = true;
                    break;
                case 3:
                    storyProblemNode.Name = "External Problem";
                    storyProblemNode.IsExpanded = true;
                    storyProblemNode.Parent = overviewNode;
                    overviewNode.Children.Add(storyProblemNode);
                    storyProblemNode.Children.Add(storyProtagNode);
                    storyProblemNode.Children.Add(storyAntagNode);
                    problem = storyProblem as ProblemModel;
                    problem.Name = "External Problem";
                    overview.StoryProblem = problem.Uuid;
                    problem.Protagonist = storyProtag.Uuid;
                    problem.Antagonist = storyAntag.Uuid;
                    storyProtagNode.Parent = storyProblemNode;
                    storyAntagNode.Parent = storyProblemNode;
                    StoryElement internalProblem = new ProblemModel("Internal Problem", model);
                    StoryNodeItem internalProblemNode = new(internalProblem, overviewNode);
                    problem = internalProblem as ProblemModel;
                    problem.Protagonist = storyProtag.Uuid;
                    problem.Antagonist = storyProtag.Uuid;
                    problem.ConflictType = "Person vs. Self";
                    problem.Premise =
                        @"Your [protagonist] grapples with an [internal conflict] and is their own antagonist, marred by self-doubt and fears " +
                        @"or having a [goal] that masks this conflict rather than a real need. The [climax scene] is often a moment of introspection in which " +
                        @"he or she makes a decision or discovery that resolves the internal conflict [outcome]. Resolving this problem may enable your " +
                        @"[protagonist] to resolve another (external) problem.";
                    break;
                case 4:
                    overviewNode.Children.Add(storyProtagNode);
                    overviewNode.Children.Add(storyAntagNode);
                    storyProtagNode.Children.Add(storyProblemNode);
                    storyProtagNode.IsExpanded = true;
                    storyProblemNode.Parent = storyProtagNode;
                    storyProtagNode.Parent = overviewNode;
                    storyAntagNode.Parent = overviewNode;
                    break;
                case 5:
                    StoryElement problemsFolder = new FolderModel("Problems", model, StoryItemType.Folder);
                    StoryNodeItem problemsFolderNode = new(problemsFolder, overviewNode) { IsExpanded = true };
                    StoryElement charactersFolder = new FolderModel("Characters", model, StoryItemType.Folder);
                    StoryNodeItem charactersFolderNode = new(charactersFolder, overviewNode) { IsExpanded = true };
                    StoryElement settingsFolder = new FolderModel("Settings", model, StoryItemType.Folder);
                    StoryNodeItem settingsFolderNode = new(settingsFolder, overviewNode);
                    StoryElement scenesFolder = new FolderModel("Scenes", model, StoryItemType.Folder);
                    StoryNodeItem scenesFolderNode = new(scenesFolder, overviewNode);
                    storyProblemNode.Name = "External Problem";
                    storyProblemNode.IsExpanded = true;
                    problemsFolderNode.Children.Add(storyProblemNode);
                    storyProblemNode.Parent = problemsFolderNode;
                    problem = storyProblem as ProblemModel;
                    problem.Name = "External Problem";
                    problem.Protagonist = storyProtag.Uuid;
                    problem.Antagonist = storyAntag.Uuid;
                    overview.StoryProblem = problem.Uuid;
                    StoryElement internalProblem2 = new ProblemModel("Internal Problem", model);
                    StoryNodeItem internalProblemNode2 = new(internalProblem2, problemsFolderNode);
                    problem = internalProblem2 as ProblemModel;
                    problem.Protagonist = storyProtag.Uuid;
                    problem.Antagonist = storyProtag.Uuid;
                    problem.ConflictType = "Person vs. Self";
                    problem.Premise =
                        @"Your [protagonist] grapples with an [internal conflict] and is their own antagonist, marred by self-doubt and fears " +
                        @"or having a [goal] that masks this conflict rather than a real need. The [climax scene] is often a moment of introspection in which " +
                        @"he or she makes a decision or discovery that resolves the internal conflict [outcome]. Resolving this problem may enable your " +
                        @"[protagonist] to resolve another (external) problem.";
                    charactersFolderNode.Children.Add(storyProtagNode);
                    charactersFolderNode.Children.Add(storyAntagNode);
                    storyProtagNode.Parent = charactersFolderNode;
                    storyAntagNode.Parent = charactersFolderNode;
                    break;
            }
        }
        return model;
    }

    /// <summary>
    /// Writes the StoryModel JSON file to disk.
    /// Returns true if the write is successful or throws an exception.
    /// </summary>
    public async Task<bool> WriteModel(StoryModel model, string file)
    {
        StoryIO wtr = Ioc.Default.GetRequiredService<StoryIO>();
        await wtr.WriteStory(file, model);
        return true;
    }


    /// <summary>
    /// Opens a StoryCAD Outline File.
    /// </summary>
    /// <param name="path">Path to Outline</param>
    /// <returns>StoryModel Object</returns>
    /// <exception cref="FileNotFoundException">Thrown if path doesn't exist.</exception>
    public async Task<StoryModel> OpenFile(string path)
    {
        //Check file exists.
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Cannot file Outline File: " + path);
        }

        //Get content
        StorageFile file = await StorageFile.GetFileFromPathAsync(path);
        StoryIO io = new();

        //Return deserialized model.
        return await io.ReadStory(file);
    }
}