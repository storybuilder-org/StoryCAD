using CommunityToolkit.Mvvm.Messaging;
using StoryCAD.DAL;
using StoryCAD.Models;
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

        OverviewModel overview = new(name, model, null)
        {
            DateCreated = DateTime.Today.ToString("yyyy-MM-dd"),
            Author = author
        };

        model.ExplorerView.Add(overview.Node);
        TrashCanModel trash = new(model, null);
        StoryNodeItem trashNode = new(trash, null);
        model.ExplorerView.Add(trashNode); // The trashcan is the second root
        FolderModel narrative = new("Narrative View", model, StoryItemType.Folder, null);
        StoryNodeItem narrativeNode = new(narrative, null) { IsRoot = true };
        model.NarratorView.Add(narrativeNode);

        // For non-blank projects, add a StoryProblem with characters.
        if (selectedTemplateIndex != 0)
        {
            StoryElement storyProblem = new ProblemModel("Story Problem", model, overview.Node);
            StoryElement storyProtag = new CharacterModel("Protagonist", model, overview.Node);
            StoryElement storyAntag = new CharacterModel("Antagonist", model, overview.Node);
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
                    overview.Node.Children.Add(storyProblem.Node);
                    storyProblem.Node.Children.Add(storyProtag.Node);
                    storyProblem.Node.Children.Add(storyAntag.Node);
                    storyProblem.Node.Parent = overview.Node;
                    storyProtag.Node.Parent = storyProblem.Node;
                    storyAntag.Node.Parent = storyProblem.Node;
                    storyProblem.Node.IsExpanded = true;
                    break;
                case 2:
                    StoryElement problems = new FolderModel("Problems", model, StoryItemType.Folder, overview.Node);
                    StoryElement characters = new FolderModel("Characters", model, StoryItemType.Folder, overview.Node);
                    StoryElement settings = new FolderModel("Settings", model, StoryItemType.Folder, overview.Node);
                    StoryElement scenes = new FolderModel("Scenes", model, StoryItemType.Folder, overview.Node);
                    overview.StoryProblem = storyProblem.Uuid;
                    overview.Node.Children.Add(storyProblem.Node);
                    overview.Node.Children.Add(storyProtag.Node);
                    overview.Node.Children.Add(storyAntag.Node);
                    problems.Node.IsExpanded = true;
                    characters.Node.IsExpanded = true;
                    break;
                case 3:
                    storyProblem.Node.Name = "External Problem";
                    storyProblem.Node.IsExpanded = true;
                    storyProblem.Node.Parent = overview.Node;
                    overview.Node.Children.Add(storyProblem.Node);
                    storyProblem.Node.Children.Add(storyProtag.Node);
                    storyProblem.Node.Children.Add(storyAntag.Node);
                    problem = storyProblem as ProblemModel;
                    problem.Name = "External Problem";
                    overview.StoryProblem = problem.Uuid;
                    problem.Protagonist = storyProtag.Uuid;
                    problem.Antagonist = storyAntag.Uuid;
                    storyProtag.Node.Parent = storyProblem.Node;
                    storyAntag.Node.Parent = storyProblem.Node;
                    StoryElement internalProblem = new ProblemModel("Internal Problem", model, overview.Node);
                    problem = (ProblemModel)internalProblem;
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
                    overview.Node.Children.Add(storyProtag.Node);
                    overview.Node.Children.Add(storyAntag.Node);
                    storyProtag.Node.Children.Add(storyProblem.Node);
                    storyProtag.Node.IsExpanded = true;
                    storyProblem.Node.Parent = storyProtag.Node;
                    storyProtag.Node.Parent = overview.Node;
                    storyAntag.Node.Parent = overview.Node;
                    break;
                case 5:
                    StoryElement problemsFolder = new FolderModel("Problems", model, StoryItemType.Folder, overview.Node);
                    StoryElement charactersFolder = new FolderModel("Characters", model, StoryItemType.Folder, overview.Node);
                    StoryElement settingsFolder = new FolderModel("Settings", model, StoryItemType.Folder, overview.Node);
                    StoryElement scenesFolder = new FolderModel("Scenes", model, StoryItemType.Folder, overview.Node);
                    storyProblem.Node.Name = "External Problem";
                    storyProblem.Node.IsExpanded = true;
                    problemsFolder.Node.Children.Add(storyProblem.Node);
                    storyProblem.Node.Parent = problemsFolder.Node;
                    problem = storyProblem as ProblemModel;
                    problem.Name = "External Problem";
                    problem.Protagonist = storyProtag.Uuid;
                    problem.Antagonist = storyAntag.Uuid;
                    overview.StoryProblem = problem.Uuid;
                    StoryElement internalProblem2 = new ProblemModel("Internal Problem", model, problemsFolder.Node);
                    problem = internalProblem2 as ProblemModel;
                    problem.Protagonist = storyProtag.Uuid;
                    problem.Antagonist = storyProtag.Uuid;
                    problem.ConflictType = "Person vs. Self";
                    problem.Premise =
                        @"Your [protagonist] grapples with an [internal conflict] and is their own antagonist, marred by self-doubt and fears " +
                        @"or having a [goal] that masks this conflict rather than a real need. The [climax scene] is often a moment of introspection in which " +
                        @"he or she makes a decision or discovery that resolves the internal conflict [outcome]. Resolving this problem may enable your " +
                        @"[protagonist] to resolve another (external) problem.";
                    problemsFolder.Node.Children.Add(storyProblem.Node);
                    problemsFolder.Node.Children.Add(storyProblem.Node);
                    storyProblem.Node.Parent = problemsFolder.Node;
                    storyProblem.Node.Parent = problemsFolder.Node;
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