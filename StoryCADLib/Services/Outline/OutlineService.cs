using StoryCAD.DAL;
using StoryCAD.Services.Search;
using StoryCAD.ViewModels.Tools;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;
using Windows.Storage;

namespace StoryCAD.Services.Outline;
/// <summary>
/// Service for interfacing with outlines at a low level.
/// </summary>
public class OutlineService
{
    private LogService _log = Ioc.Default.GetRequiredService<LogService>();

    /// <summary>
    /// Creates a new story model
    /// </summary>
    /// <param name="name">name of the outline, i.e. old man and the sea</param>
    /// <param name="author">Creator of the outline</param>
    /// <param name="selectedTemplateIndex">Template index</param>
    /// <returns>A story outline variable.</returns>
    public Task<StoryModel> CreateModel(string name, string author, int selectedTemplateIndex)
    {  
        _log.Log(LogLevel.Info, $"Creating new model: {name} by {author} with index {selectedTemplateIndex}");
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

        if (selectedTemplateIndex != 0)
        {
            StoryElement storyProblem = new ProblemModel("Story Problem", model, overview.Node);
            StoryElement storyProtag = new CharacterModel("Protagonist", model, overview.Node);
            StoryElement storyAntag = new CharacterModel("Antagonist", model, overview.Node);
            overview.StoryProblem = storyProblem.Uuid;
            var problem = (ProblemModel)storyProblem;
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
                    problem = (ProblemModel)storyProblem;
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
                        """
                        Your [protagonist] grapples with an [internal conflict] and is their own antagonist,
                        marred by self-doubt and fears or having a [goal] that masks this conflict rather than
                        a real need. The [climax scene] is often a moment of introspection in which he or she
                        makes a decision or discovery that resolves the internal conflict [outcome]. Resolving
                        this problem may enable your [protagonist] to resolve another (external) problem.
                        """;
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
                    problem = (ProblemModel)storyProblem;
                    problem.Name = "External Problem";
                    problem.Protagonist = storyProtag.Uuid;
                    problem.Antagonist = storyAntag.Uuid;
                    overview.StoryProblem = problem.Uuid;
                    StoryElement internalProblem2 = new ProblemModel("Internal Problem", model, problemsFolder.Node);
                    problem = (ProblemModel)internalProblem2;
                    problem.Protagonist = storyProtag.Uuid;
                    problem.Antagonist = storyProtag.Uuid;
                    problem.ConflictType = "Person vs. Self";
                    problem.Premise =
                        """
                        Your [protagonist] grapples with an [internal conflict] and is their own antagonist,
                        marred by self-doubt and fears or having a [goal] that masks this conflict rather than
                        a real need. The [climax scene] is often a moment of introspection in which he or she
                        makes a decision or discovery that resolves the internal conflict [outcome]. Resolving
                        this problem may enable your [protagonist] to resolve another (external) problem.
                        """;
                    storyProblem.Node.Parent = problemsFolder.Node;
                    break;
            }
        }

        _log.Log(LogLevel.Info, $"Model created, element count {model.StoryElements.Count}");
        return Task.FromResult(model);
    }

    /// <summary>
    /// Writes the StoryModel JSON file to disk.
    /// Returns true if write is successful or throws an exception.
    /// </summary>
    public async Task<bool> WriteModel(StoryModel model, string file)
    {
        _log.Log(LogLevel.Info, $"Writing model to {file}");
        var wtr = Ioc.Default.GetRequiredService<StoryIO>();
        await wtr.WriteStory(file, model);
        _log.Log(LogLevel.Info, "Model write success.");
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
        _log.Log(LogLevel.Info, $"Opening file {path}");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Cannot file Outline File: " + path);
        }

        var file = await StorageFile.GetFileFromPathAsync(path);
        StoryModel model = await new StoryIO().ReadStory(file);
        _log.Log(LogLevel.Info, $"Opened model contains {model.StoryElements.Count} elements.");
        return model;
    }

    /// <summary>
    /// Adds a new StoryElement to the StoryModel.
    /// </summary>
    /// <param name="model">StoryModel we are using</param>
    /// <param name="typeToAdd">Type of StoryElement that should be created</param>
    /// <param name="parent">Parent of the node we are creating</param>
    /// <returns>Newly created StoryElement</returns>
    public StoryElement AddStoryElement(StoryModel model, StoryItemType typeToAdd, StoryNodeItem parent)
    {
        _log.Log(LogLevel.Info, "AddStoryElement called.");
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

        StoryElement newElement = typeToAdd switch
        {
            StoryItemType.Folder => new FolderModel(model, parent),
            StoryItemType.Section => new FolderModel("New Section", model, StoryItemType.Folder, parent),
            StoryItemType.Problem => new ProblemModel(model, parent),
            StoryItemType.Character => new CharacterModel(model, parent),
            StoryItemType.Setting => new SettingModel(model, parent),
            StoryItemType.Scene => new SceneModel(model, parent),
            StoryItemType.Web => new WebModel(model, parent),
            StoryItemType.Notes => new FolderModel("New Note", model, StoryItemType.Notes, parent),
            _ => throw new InvalidOperationException("Cannot add a new element of type " + typeToAdd)
        };
        _log.Log(LogLevel.Info, "AddStoryElement completed.");
        return newElement;
    }

    /// <summary>
    /// Finds if an element can be safely deleted.
    /// </summary>
    public List<StoryElement> FindElementReferences(StoryModel model, Guid elementGuid)
    {
        _log.Log(LogLevel.Info, $"FindElementReferences called for element {elementGuid}.");
        StoryElement elementToDelete = StoryElement.GetByGuid(elementGuid);
        if (StoryNodeItem.RootNodeType(elementToDelete.Node) == StoryItemType.TrashCan)
        {
            throw new InvalidOperationException("Cannot delete a node from the Trash Can.");
        }
        if (elementToDelete.Node.IsRoot)
        {
            throw new InvalidOperationException("Cannot delete a root node.");
        }

        List<StoryElement> foundNodes = new();
        foreach (StoryElement element in model.StoryElements)
        {
            if (Ioc.Default.GetRequiredService<DeletionService>().SearchStoryElement(element.Node, elementGuid, model))
            {
                foundNodes.Add(element);
            }
        }
        _log.Log(LogLevel.Info, $"FindElementReferences completed. Found {foundNodes.Count} references.");
        return foundNodes;
    }

    /// <summary>
    /// Removes a reference to an element from the StoryModel.
    /// </summary>
    /// <param name="elementToRemove">Element you are removing references to</param>
    /// <param name="model">StoryModel you are updating</param>
    /// <returns>bool indicating success</returns>
    public bool RemoveReferenceToElement(Guid elementToRemove, StoryModel model)
    {
        _log.Log(LogLevel.Info, $"RemoveReferenceToElement called for element {elementToRemove}.");
        if (elementToRemove == Guid.Empty)
        {
            throw new ArgumentNullException(nameof(elementToRemove));
        }

        if (model == null || model.StoryElements.Count == 0)
        {
            throw new ArgumentNullException(nameof(model));
        }

        foreach (StoryElement element in model.StoryElements)
        {
            Ioc.Default.GetRequiredService<DeletionService>()
                .SearchStoryElement(element.Node, elementToRemove, model, true);
        }
        _log.Log(LogLevel.Info, "RemoveReferenceToElement completed.");
        return true;
    }

    /// <summary>
    /// Deletes an element.
    /// <remarks>Element is moved to trashcan node.</remarks>
    /// </summary>
    /// <param name="elementToRemove">Element you want to remove</param>
    /// <param name="view">View you are deleting from</param>
    /// <param name="source">StoryModel you are deleting from.</param>
    /// <returns>true if successful.</returns>
    public bool RemoveElement(StoryElement elementToRemove, StoryViewType view, StoryNodeItem source)
    {
        _log.Log(LogLevel.Info, $"RemoveElement called for element {elementToRemove.Uuid}.");
        if (elementToRemove == null)
        {
            throw new ArgumentNullException(nameof(elementToRemove));
        }

        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (source.IsRoot)
        {
            throw new InvalidOperationException("Cannot delete a root node.");
        }

        if (source.Type is StoryItemType.StoryOverview or StoryItemType.TrashCan)
        {
            throw new InvalidOperationException("Cannot delete a trash or overview node.");
        }

        bool result = elementToRemove.Node.Delete(view);
        _log.Log(LogLevel.Info, $"RemoveElement completed for element {elementToRemove.Uuid}.");
        return result;
    }

    /// <summary>
    /// Adds a relationship between two elements.
    /// </summary>
    /// <param name="Model">StoryModel</param>
    /// <param name="source">Character you want to add relationship to.</param>
    /// <param name="recipient">Relationship character is with</param>
    /// <param name="desc">Relationship description</param>
    /// <param name="mirror">Create same relationship on recipient</param>
    /// <returns></returns>
    public bool AddRelationship(StoryModel Model, Guid source, Guid recipient, string desc, bool mirror = false)
    {
        _log.Log(LogLevel.Info, $"AddRelationship called from {source} to {recipient}.");
        if (source == Guid.Empty)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (recipient == Guid.Empty)
        {
            throw new ArgumentNullException(nameof(recipient));
        }

        StoryElement sourceElement = Model.StoryElements.StoryElementGuids[source];
        StoryElement recipientElement = Model.StoryElements.StoryElementGuids[recipient];

        if (sourceElement.ElementType != StoryItemType.Character)
        {
            throw new InvalidOperationException("Source must be a character.");
        }

        if (recipientElement.ElementType != StoryItemType.Character)
        {
            throw new InvalidOperationException("Recipient must be a character.");
        }

        RelationshipModel relationship = new(recipient, desc);
        ((CharacterModel)sourceElement).RelationshipList.Add(relationship);

        if (mirror)
        {
            RelationshipModel mirrorRelationship = new(source, desc);
            ((CharacterModel)recipientElement).RelationshipList.Add(mirrorRelationship);
        }
        _log.Log(LogLevel.Info, $"AddRelationship completed from {source} to {recipient}.");
        return true;
    }

    /// <summary>
    /// Adds a new cast member to a scene.
    /// </summary>
    /// <param name="source">Scene element you are adding the cast member to </param>
    /// <param name="castMember">Cast member you want to add.</param>
    /// <returns></returns>
    public bool AddCastMember(StoryModel Model, StoryElement source, Guid castMember)
    {
        _log.Log(LogLevel.Info, $"AddCastMember called for cast member {castMember} on source {source.Uuid}.");
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (castMember == Guid.Empty)
        {
            throw new ArgumentNullException(nameof(castMember));
        }

        if (source.ElementType != StoryItemType.Scene)
        {
            throw new InvalidOperationException("Source must be a scene.");
        }

        StoryElement character = Model.StoryElements.StoryElementGuids[castMember];
        if (character.ElementType != StoryItemType.Character)
        {
            throw new InvalidOperationException("castMember must be a character.");
        }

        if (((SceneModel)source).CastMembers.Contains(castMember))
        {
            _log.Log(LogLevel.Info, $"AddCastMember completed: cast member {castMember} already exists.");
            return true;
        }

        ((SceneModel)source).CastMembers.Add(castMember);
        _log.Log(LogLevel.Info, $"AddCastMember completed for cast member {castMember}.");
        return true;
    }

    public SceneModel ConvertProblemToScene(StoryModel model, ProblemModel problem)
    {
        _log.Log(LogLevel.Info, $"ConvertProblemToScene called for {problem.Uuid}.");

        var parent = problem.Node.Parent;
        int index = parent.Children.IndexOf(problem.Node);

        // remove the old node from the tree
        parent.Children.RemoveAt(index);

        // create the new scene and insert it in the same slot
        var scene = new SceneModel(model, parent);     // ctor adds its node to parent
        parent.Children.Remove(scene.Node);            // detach the auto-added node
        parent.Children.Insert(index, scene.Node);     // re-insert at original position

        // preserve identifiers
        scene.Name = problem.Name;
        scene.Uuid = problem.Uuid;
        scene.Node.Uuid = problem.Node.Uuid;

        // preserve node state
        scene.Node.IsExpanded = problem.Node.IsExpanded;
        scene.Node.IsSelected = problem.Node.IsSelected;

        // move any children
        foreach (var child in problem.Node.Children.ToList())
        {
            scene.Node.Children.Add(child);
            child.Parent = scene.Node;
        }
        model.StoryElements.StoryElementGuids.Remove(problem.Uuid);


        scene.Protagonist = problem.Protagonist;
        scene.ProtagGoal = problem.ProtGoal;
        scene.Antagonist = problem.Antagonist;
        scene.AntagGoal = problem.AntagGoal;
        scene.Opposition = problem.ProtConflict;
        scene.Outcome = problem.Outcome;
        scene.Notes = problem.Notes;

        // clean up model references
        model.ExplorerView.Remove(problem.Node);   // mirrors the reverse conversion
        // delete every element whose Uuid matches problem.Uuid
        for (int i = model.StoryElements.Count - 1; i >= 0; i--)
        {
            if (model.StoryElements[i].Uuid == problem.Uuid)
                model.StoryElements.RemoveAt(i);

        }
        model.StoryElements.Add(scene);            // register the new element

        model.ExplorerView.Remove(problem.Node);
        scene.Node.Parent.IsExpanded = true; // expand the parent node
        scene.Name = problem.Name;
        scene.Node.Name = problem.Name;
        _log.Log(LogLevel.Info, $"ConvertProblemToScene completed for {scene.Uuid}.");
        return scene;
    }


    /// <summary>
    /// Convert a Scene element to a Problem element.
    /// </summary>
    public ProblemModel ConvertSceneToProblem(StoryModel model, SceneModel scene)
    {
        _log.Log(LogLevel.Info, $"ConvertSceneToProblem called for {scene.Uuid}.");

        var parent = scene.Node.Parent;
        int index = parent.Children.IndexOf(scene.Node);

        // remove the old scene node from the tree
        parent.Children.RemoveAt(index);

        // create the replacement problem and put it back in the same slot
        var problem = new ProblemModel(model, parent);   // ctor appends its node
        parent.Children.Remove(problem.Node);            // detach
        parent.Children.Insert(index, problem.Node);     // re-insert at original pos

        // preserve identifiers
        problem.Uuid = scene.Uuid;
        problem.Node.Uuid = scene.Node.Uuid;

        // preserve node state
        problem.Node.IsExpanded = scene.Node.IsExpanded;
        problem.Node.IsSelected = scene.Node.IsSelected;

        // move children
        foreach (var child in scene.Node.Children.ToList())
        {
            problem.Node.Children.Add(child);
            child.Parent = problem.Node;
        }
        model.StoryElements.StoryElementGuids.Remove(scene.Uuid);

        // copy basic fields
        problem.Protagonist = scene.Protagonist;
        problem.ProtGoal = scene.ProtagGoal;
        problem.Antagonist = scene.Antagonist;
        problem.AntagGoal = scene.AntagGoal;
        problem.ProtConflict = scene.Opposition;
        problem.Outcome = scene.Outcome;
        problem.Notes = scene.Notes;

        // clean up model references
        model.StoryElements.Remove(scene);
        for (int i = model.StoryElements.Count - 1; i >= 0; i--)
        {
            if (model.StoryElements[i].Uuid == scene.Uuid)
                model.StoryElements.RemoveAt(i);

        }
        model.ExplorerView.Remove(scene.Node);   // mirrors the reverse conversion
        model.StoryElements.Add(problem);               // register the new element

        model.ExplorerView.Remove(scene.Node);
        problem.Name = scene.Name;
        problem.Node.Name = scene.Name;
        scene.Node.Parent.IsExpanded = true; // expand the parent node
        _log.Log(LogLevel.Info, $"ConvertSceneToProblem completed for {problem.Uuid}.");
        return problem;
    }
    /// <summary>
    /// Assigns an element to a beat in a ProblemModel.
    /// </summary>
    /// <param name="Model">StoryModel</param>
    /// <param name="Parent">Problem Element that contains the beatsheet you wish to bind to</param>
    /// <param name="Index">Index of beat you are binding to</param>
    /// <param name="DesiredBind">Guid of Element you want to bind to, MUST be scene or problem element</param>
    public void AssignElementToBeat(StoryModel Model, ProblemModel Parent, int Index, Guid DesiredBind)
    {
        //Check params
        if (Model == null)
        {
            throw new ArgumentNullException(nameof(Model));
        }
        if (Parent == null)
        {
            throw new ArgumentNullException(nameof(Parent));
        }
        if (DesiredBind == Guid.Empty)
        {
            throw new ArgumentNullException(nameof(DesiredBind));
        }

        //Get desired bind
        StoryElement? DesiredBindElement;
        Model.StoryElements.StoryElementGuids.TryGetValue(DesiredBind, out DesiredBindElement);
        Parent.StructureBeats[Index].Guid = DesiredBind;

        //Check element really exists.
        if (DesiredBindElement == null)
        {
            throw new NullReferenceException($"GUID: {DesiredBind} does not exist within StoryModel");
        }

        //Check we are binding the correct element
        if (DesiredBindElement.ElementType != StoryItemType.Problem &&
            DesiredBindElement.ElementType != StoryItemType.Scene)
        {
            throw new InvalidOperationException("You can only bind Scene or Problem Elements.");
        }

        //Check Index is valid
        if (Index >= 0 && Parent.StructureBeats.Count-1 >= Index)
        {
            //Bind
            Parent.StructureBeats[Index].Guid = DesiredBind;
        }
        else
        {
            // out of bounds
            throw new InvalidOperationException("Index is out of bounds.");
        }
    }

    /// <summary>
    /// Removes bound element.
    /// </summary>
    /// <param name="Model">Story model</param>
    /// <param name="Parent">Problem Element with beatsheet</param>
    /// <param name="Index">Index you want to unbind from</param>
    public void UnasignBeat(StoryModel Model, ProblemModel Parent, int Index)
    {
        //Check params
        if (Model == null)
        {
            throw new ArgumentNullException(nameof(Model));
        }
        if (Parent == null)
        {
            throw new ArgumentNullException(nameof(Parent));
        }


        //Check Index is valid
        if (Index >= 0 && Parent.StructureBeats.Count - 1 >= Index)
        {
            //unbind
            Parent.StructureBeats[Index].Guid = Guid.Empty;
        }
        else
        {
            // out of bounds
            throw new InvalidOperationException("Index is out of bounds.");
        }
    }

    /// <summary>
    /// Add beat to a ProblemModel.
    /// </summary>
    /// <param name="Parent"></param>
    /// <param name="Title"></param>
    /// <param name="Description"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public void CreateBeat(ProblemModel Parent, string Title, string Description)
    {
        if (Parent == null)
        {
            throw new ArgumentNullException(nameof(Parent));
        }

        //Create and add beat.
        StructureBeatViewModel NewBeat = new()
        {
            Title = Title,
            Description = Description
        };
        Parent.StructureBeats.Add(NewBeat);
    }

    /// <summary>
    /// Deletes a beat and unbinds elements if nesscessary.
    /// </summary>
    /// <param name="Model"></param>
    /// <param name="Index"></param>
    public void DeleteBeat(StoryModel Model, ProblemModel Parent, int Index)
    {
        if (Parent == null)
        {
            throw new ArgumentNullException(nameof(Parent));
        }

        if (Index < 0 || Index >= Parent.StructureBeats.Count)
        {
            // No bound beat
            if (Parent.StructureBeats[Index].Guid == Guid.Empty)
            {
                Parent.StructureBeats.RemoveAt(Index);
                return;
            }

            StoryElement? BoundElement;
            Model.StoryElements.StoryElementGuids.TryGetValue(Parent.StructureBeats[Index].Guid, out BoundElement);
            
            //An element is bound that doesn't exist
            if (BoundElement == null)
            {
                Parent.StructureBeats.RemoveAt(Index);
                return;
            }
            
            //For Problem elements we MUST unassign first or StoryCAD will have issues
            //when trying to assign that element to a beat in the future.
            //Scenes are not limited and can be assigned to multiple
            if (BoundElement.ElementType == StoryItemType.Problem)
            {
                UnasignBeat(Model, Parent, Index);
            }
            Parent.StructureBeats.RemoveAt(Index);
        }
        else
        {
            throw new ArgumentOutOfRangeException("Index is invalid");
        }
    }

    /// <summary>
    /// Sets basic infomation about the beat sheet/creates a new one
    /// If one exists, it will be overwritten and any beats in it will be unbound.
    /// </summary>
    /// <param name="Description">Description of you beatsheet, i.e. what is its structure?</param>
    /// <param name="Model">Problem element you are trying to add the beatsheet to</param>
    /// <param name="Title">This is the title of your beat sheet,
    /// if it is not Custom Beat Sheet it will not be editable within the StoryCAD app.
    /// </param>
    /// <param name="Beats">Beats that this sheet will contain, can be added later</param>
    public void SetBeatSheet(StoryModel Model, ProblemModel Parent, string Description, 
        string Title = "Custom Beat Sheet", ObservableCollection<StructureBeatViewModel> Beats = null)
    {
        Parent.StructureTitle = Title;
        Parent.StructureDescription = Description;

        //Unbind/Delete Beats first.
        for (int i = Parent.StructureBeats.Count - 1; i >= 0; i--) {
            DeleteBeat(Model, Parent, i);
        }

        //Create/Add beats.
        Parent.StructureBeats = Beats ?? new();
    }

    /// <summary>
    /// Saves a beatsheet to a file
    /// </summary>
    /// <param name="Path">File path to save to</param>
    /// <param name="Description"> Beatsheet Description</param>
    /// <param name="Beats">Beats</param>
    public void SaveBeatsheet(string Path, string Description, List<StructureBeatViewModel> Beats)
    {
        SavedBeatsheet Model = new();
        Model.Beats = new();
        foreach (var Beat in Beats) 
        {
            Beat.Guid = Guid.Empty;
            Model.Beats.Add(Beat);
        }

        Model.Description = Description;
        string data = JsonSerializer.Serialize(Model);
        File.WriteAllText(Path, data);
    }

    /// <summary>
    /// Loads a beatsheet
    /// </summary>
    /// <param name="path">File path to load</param>
    /// <returns>Model</returns>
    public SavedBeatsheet LoadBeatsheet(string path)
    {
        string data = File.ReadAllText(path);
        var model = JsonSerializer.Deserialize<SavedBeatsheet>(data);
        foreach (var Beat in model.Beats) { Beat.Guid = Guid.Empty; }

        return model;
    }
}
