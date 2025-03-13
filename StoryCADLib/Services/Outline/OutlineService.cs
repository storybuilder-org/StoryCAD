﻿using StoryCAD.DAL;
using StoryCAD.Services.Search;
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
        model.ExplorerView.Add(trash.Node); // The trashcan is the second root
        FolderModel narrative = new("Narrative View", model, StoryItemType.Folder, null);
        model.NarratorView.Add(narrative.Node);

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
            problem.Premise = """
                              Your[protagonist] in a situation[genre, setting] wants something[goal], which brings him
                              into [conflict] with a second character[antagonist]. After a series of conflicts[additional
                              problems], the final battle[climax scene] erupts, and the[protagonist] finally resolves the 
                              conflict[outcome].
                              """;

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
                    problem.Premise = """
                                      Your [protagonist] grapples with an [internal conflict] and is their own antagonist, marred by self-doubt and fears
                                      or having a [goal] that masks this conflict rather than a real need. The [climax scene] is often a moment of
                                      introspection in which he or she makes a decision or discovery that resolves the internal conflict [outcome]. 
                                      Resolving this problem may enable your [protagonist] to resolve another (external) problem.
                                      """;
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

    /// <summary>
    /// Adds a new StoryElement to the StoryModel
    /// </summary>
    /// <param name="typeToAdd">Type of StoryElement that should be created</param>
    /// <param name="parent">Parent of the node we are creating</param>
    /// <param name="model">StoryModel we are using</param>
    /// <returns></returns>
    public StoryElement AddStoryElement(StoryModel model, StoryItemType typeToAdd, StoryNodeItem parent)
    {
        if (parent == null)
        {
            throw new ArgumentNullException(nameof(parent));
        }

        if (model == null || model.StoryElements.Count == 0)
        {
            throw new ArgumentNullException(nameof(model));
        }

        if (StoryNodeItem.RootNodeType(parent) == StoryItemType.TrashCan)
        {
            throw new InvalidOperationException("Cannot add a new node to the Trash Can.");
        }

        StoryElement newElement;
        switch (typeToAdd)
        {
            case StoryItemType.Folder:
                newElement = new FolderModel(model, parent);
                break;
            case StoryItemType.Section:
                newElement = new FolderModel("New Section", model, StoryItemType.Folder, parent);
                break;
            case StoryItemType.Problem:
                newElement = new ProblemModel(model, parent);
                break;
            case StoryItemType.Character:
                newElement = new CharacterModel(model, parent);
                break;
            case StoryItemType.Setting:
                newElement = new SettingModel(model, parent);
                break;
            case StoryItemType.Scene:
                newElement = new SceneModel(model, parent);
                break;
            case StoryItemType.Web:
                newElement = new WebModel(model, parent);
                break;
            case StoryItemType.Notes:
                newElement = new FolderModel("New Note", model, StoryItemType.Notes, parent);
                break;
            default:
                throw new InvalidOperationException("Cannot add a new element of type " + typeToAdd);
                break;
        }

        //return new element
        return newElement;
    }

    /// <summary>
    /// Finds if an element can be safely deleted.
    /// </summary>
    public List<StoryElement> FindElementReferences(StoryModel Model, Guid elementGUID)
    {
        StoryElement ElementToDelete  = StoryElement.GetByGuid(elementGUID);
        if (StoryNodeItem.RootNodeType(ElementToDelete.Node) == StoryItemType.TrashCan)
        {
            throw new InvalidOperationException("Cannot delete a node from the Trash Can.");
        }
        if (ElementToDelete.Node.IsRoot)
        {
            throw new InvalidOperationException("Cannot delete a root node.");
        }

        List<StoryElement> _foundNodes = new();
        foreach (StoryElement element in Model.StoryElements) //Gets all nodes in the tree TODO: MAKE RECURSIVE
        {
            if (Ioc.Default.GetRequiredService<DeletionService>().SearchStoryElement(element.Node, elementGUID, Model))
            {
                _foundNodes.Add(element);
            }
        }
        return _foundNodes;
    }


    /// <summary>
    /// Removes a reference to an element from the StoryModel
    /// </summary>
    /// <param name="elementToRemove">Element you are removing references to</param>
    /// <param name="model">StoryModel you are updating</param>
    /// <returns>bool indicating success</returns>
    public bool RemoveReferenceToElement(Guid elementToRemove, StoryModel model)
    {
        if (elementToRemove == Guid.Empty)
        {
            throw new ArgumentNullException(nameof(elementToRemove));
        }

        if (model == null || model.StoryElements.Count == 0)
        {
            throw new ArgumentNullException(nameof(model));
        }

        //Iterate through and remove refs.
        foreach (StoryElement element in model.StoryElements)
        {
            Ioc.Default.GetRequiredService<DeletionService>()
                .SearchStoryElement(element.Node, elementToRemove, model, true);
        }

        return true;
    }

    /// <summary>
    /// Deletes an element
    /// </summary>
    /// <remarks>Element is moved to trashcan node.</remarks>
    /// <param name="elementToRemove">Element you want to remove</param>
    /// <param name="Source">StoryModel you are deleting from.</param>
    /// <param name="View">View you are deleting from</param>
    /// <returns></returns>
    public bool RemoveElement(StoryElement elementToRemove, StoryViewType View, StoryNodeItem Source)
    {
        elementToRemove.Node.Delete(View, Source);
        return true;
    }
}