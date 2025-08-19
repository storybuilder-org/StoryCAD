using StoryCAD.DAL;
using StoryCAD.Services.Search;
using StoryCAD.ViewModels.Tools;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;
using Windows.Storage;
using StoryCAD.Services.Backend;
using StoryCAD.Services.Locking;
using StoryCAD.Services.Backup;
using StoryCAD.ViewModels.SubViewModels;

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
internal Task<StoryModel> CreateModel(string name, string author, int selectedTemplateIndex)
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
    model.TrashView.Add(trash.Node);                             // Add to TrashView instead of ExplorerView

    FolderModel narrative = new("Narrative View", model,
                                StoryItemType.Folder, null);
    model.NarratorView.Add(narrative.Node);

    // branch on template type
    switch (selectedTemplateIndex)
    {
        case 0: // Blank Outline
            break;

        case 1: // Overview and Story Problem
        {
            StoryElement storyProblem = new ProblemModel("Story Problem", model, overview.Node);
            StoryElement storyProtag  = new CharacterModel("Protagonist",  model, storyProblem.Node);
            StoryElement storyAntag   = new CharacterModel("Antagonist",   model, storyProblem.Node);

            overview.StoryProblem = storyProblem.Uuid;
            var problem = (ProblemModel)storyProblem;
            problem.Protagonist = storyProtag.Uuid;
            problem.Antagonist  = storyAntag.Uuid;
            problem.Premise =
                """
                Your [protagonist] in a situation [genre, setting] wants something [goal], which brings him
                into [conflict] with a second character [antagonist]. After a series of conflicts [additional
                problems], the final battle [climax scene] erupts, and the [protagonist] finally resolves the
                conflict [outcome].
                """;
            storyProblem.Node.IsExpanded = true;
            break;
        }

        case 2: // Folders
        {
            StoryElement problems   = new FolderModel("Problems",   model, StoryItemType.Folder, overview.Node);
            StoryElement characters = new FolderModel("Characters", model, StoryItemType.Folder, overview.Node);
            StoryElement settings   = new FolderModel("Settings",   model, StoryItemType.Folder, overview.Node);
            StoryElement scenes     = new FolderModel("Scenes",     model, StoryItemType.Folder, overview.Node);

            problems.Node.IsExpanded   = true;
            characters.Node.IsExpanded = true;
            settings.Node.IsExpanded = true;
            scenes.Node.IsExpanded = true;
            break;
        }

        case 3: // External and Internal Problems
        {
            StoryElement external = new ProblemModel("External Problem", model, overview.Node);
            StoryElement protag   = new CharacterModel("Protagonist",     model, external.Node);
            StoryElement antag    = new CharacterModel("Antagonist",      model, external.Node);

            overview.StoryProblem = external.Uuid;
            var extProb = (ProblemModel)external;
            extProb.Name        = "External Problem";
            extProb.Protagonist = protag.Uuid;
            extProb.Antagonist  = antag.Uuid;
            extProb.Premise =
                """
                Your [protagonist] in a situation [genre, setting] wants something [goal], which brings him
                into [conflict] with a second character [antagonist]. After a series of conflicts [additional
                problems], the final battle [climax scene] erupts, and the [protagonist] finally resolves the
                conflict [outcome].
                """;
            external.Node.IsExpanded = true;

            StoryElement internalProb = new ProblemModel("Internal Problem", model, overview.Node);
            var intProb = (ProblemModel)internalProb;
            intProb.Protagonist  = protag.Uuid;
            intProb.Antagonist   = protag.Uuid;
            intProb.ConflictType = "Person vs. Self";
            intProb.Premise =
                """
                Your [protagonist] grapples with an [internal conflict] and is their own antagonist,
                marred by self-doubt and fears or having a [goal] that masks this conflict rather than
                a real need. The [climax scene] is often a moment of introspection in which he or she
                makes a decision or discovery that resolves the internal conflict [outcome]. Resolving
                this problem may enable your [protagonist] to resolve another (external) problem.
                """;
            break;
        }

        case 4: // Protagonist and Antagonist
        {
            StoryElement protag = new CharacterModel("Protagonist", model, overview.Node);
            StoryElement antag  = new CharacterModel("Antagonist",  model, overview.Node);
            protag.Node.IsExpanded = true;

            StoryElement storyProblem = new ProblemModel("Story Problem", model, protag.Node);
            overview.StoryProblem = storyProblem.Uuid;
            var problem = (ProblemModel)storyProblem;
            problem.Protagonist = protag.Uuid;
            problem.Antagonist  = antag.Uuid;
            problem.Premise =
                """
                Your [protagonist] in a situation [genre, setting] wants something [goal], which brings him
                into [conflict] with a second character [antagonist]. After a series of conflicts [additional
                problems], the final battle [climax scene] erupts, and the [protagonist] finally resolves the
                conflict [outcome].
                """;
            break;
        }

        case 5: // Problems and Characters
        {
            StoryElement problemsFolder   = new FolderModel("Problems",   model, StoryItemType.Folder, overview.Node);
            StoryElement charactersFolder = new FolderModel("Characters", model, StoryItemType.Folder, overview.Node);
            StoryElement settingsFolder   = new FolderModel("Settings",   model, StoryItemType.Folder, overview.Node);
            StoryElement scenesFolder     = new FolderModel("Scenes",     model, StoryItemType.Folder, overview.Node);

            StoryElement external = new ProblemModel("External Problem", model, problemsFolder.Node);
            StoryElement protag   = new CharacterModel("Protagonist",     model, charactersFolder.Node);
            StoryElement antag    = new CharacterModel("Antagonist",      model, charactersFolder.Node);

            overview.StoryProblem = external.Uuid;
            var extProb = (ProblemModel)external;
            extProb.Name        = "External Problem";
            extProb.Protagonist = protag.Uuid;
            extProb.Antagonist  = antag.Uuid;
            extProb.Premise =
                """
                Your [protagonist] in a situation [genre, setting] wants something [goal], which brings him
                into [conflict] with a second character [antagonist]. After a series of conflicts [additional
                problems], the final battle [climax scene] erupts, and the [protagonist] finally resolves the
                conflict [outcome].
                """;
            external.Node.IsExpanded = true;
            settingsFolder.Node.IsExpanded = true;
            scenesFolder.Node.IsExpanded = true;

            StoryElement internalProb = new ProblemModel("Internal Problem", model, problemsFolder.Node);
            var intProb = (ProblemModel)internalProb;
            intProb.Protagonist  = protag.Uuid;
            intProb.Antagonist   = protag.Uuid;
            intProb.ConflictType = "Person vs. Self";
            intProb.Premise =
                """
                Your [protagonist] grapples with an [internal conflict] and is their own antagonist,
                marred by self-doubt and fears or having a [goal] that masks this conflict rather than
                a real need. The [climax scene] is often a moment of introspection in which he or she
                makes a decision or discovery that resolves the internal conflict [outcome]. Resolving
                this problem may enable your [protagonist] to resolve another (external) problem.
                """;
            break;
        }

        default:
            throw new ArgumentOutOfRangeException(nameof(selectedTemplateIndex));
    }

    // Reset Changed flag after template creation
    // Template creation adds nodes which trigger change events, but a newly created model shouldn't be marked as changed
    model.Changed = false;
    
    _log.Log(LogLevel.Info, $"Model created, element count {model.StoryElements.Count}");
    return Task.FromResult(model);
}


    /// <summary>
    /// Writes the StoryModel JSON file to disk.
    /// Returns true if write is successful or throws an exception.
    /// </summary>
    internal async Task<bool> WriteModel(StoryModel model, string file)
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
    internal async Task<StoryModel> OpenFile(string path)
    {
        _log.Log(LogLevel.Info, $"Opening file {path}");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Cannot file Outline File: " + path);
        }

        var file = await StorageFile.GetFileFromPathAsync(path);
        var outlineViewModel = Ioc.Default.GetRequiredService<OutlineViewModel>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        StoryModel model = await new StoryIO(_log, outlineViewModel, appState).ReadStory(file);
        _log.Log(LogLevel.Info, $"Opened model contains {model.StoryElements.Count} elements.");
        return model;
    }

    /// <summary>
    /// Sets the current view type for the story model and updates the CurrentView accordingly.
    /// </summary>
    /// <param name="model">The StoryModel to update</param>
    /// <param name="viewType">The desired view type (Explorer or Narrator)</param>
    /// <exception cref="ArgumentNullException">Thrown when model is null</exception>
    /// <exception cref="ArgumentException">Thrown when viewType is invalid</exception>
    internal void SetCurrentView(StoryModel model, StoryViewType viewType)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        var autoSaveService = Ioc.Default.GetRequiredService<AutoSaveService>();
        var backupService = Ioc.Default.GetRequiredService<BackupService>();
        
        using (var serializationLock = new SerializationLock(autoSaveService, backupService, _log))
        {
            switch (viewType)
            {
                case StoryViewType.ExplorerView:
                    model.CurrentView = model.ExplorerView;
                    model.CurrentViewType = StoryViewType.ExplorerView;
                    break;
                case StoryViewType.NarratorView:
                    model.CurrentView = model.NarratorView;
                    model.CurrentViewType = StoryViewType.NarratorView;
                    break;
                default:
                    throw new ArgumentException($"Unsupported view type: {viewType}", nameof(viewType));
            }
            
            _log.Log(LogLevel.Info, $"View switched to {viewType}");
        }
    }

    /// <summary>
    /// Adds a new StoryElement to the StoryModel.
    /// </summary>
    /// <param name="model">StoryModel we are using</param>
    /// <param name="typeToAdd">Type of StoryElement that should be created</param>
    /// <param name="parent">Parent of the node we are creating</param>
    /// <returns>Newly created StoryElement</returns>
    internal StoryElement AddStoryElement(StoryModel model, StoryItemType typeToAdd, StoryNodeItem parent)
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
    internal List<StoryElement> FindElementReferences(StoryModel model, Guid elementGuid)
    {
        _log.Log(LogLevel.Info, $"FindElementReferences called for element {elementGuid}.");
        
        // Use GetByGuid with the model parameter for stateless operation
        StoryElement elementToDelete = StoryElement.GetByGuid(elementGuid, model);
        
        if (elementToDelete == null || elementToDelete.Node == null)
        {
            _log.Log(LogLevel.Warn, $"Element {elementGuid} not found or has no associated Node.");
            return new List<StoryElement>();
        }
        
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
            // Skip checking the element against itself
            if (element.Uuid == elementGuid)
                continue;
                
            if (Ioc.Default.GetRequiredService<SearchService>().SearchUuid(element.Node, elementGuid, model))
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
    private bool RemoveReferenceToElement(Guid elementToRemove, StoryModel model)
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
            Ioc.Default.GetRequiredService<SearchService>()
                .SearchUuid(element.Node, elementToRemove, model, true);
        }
        _log.Log(LogLevel.Info, "RemoveReferenceToElement completed.");
        return true;
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
    internal bool AddRelationship(StoryModel Model, Guid source, Guid recipient, string desc, bool mirror = false)
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

        // Check for duplicate relationship before adding
        CharacterModel sourceCharacter = (CharacterModel)sourceElement;
        foreach (var existingRel in sourceCharacter.RelationshipList)
        {
            if (existingRel.PartnerUuid == recipient &&
                existingRel.RelationType == desc &&
                existingRel.Trait == string.Empty &&
                existingRel.Attitude == string.Empty)
            {
                _log.Log(LogLevel.Info, $"Duplicate relationship from {source} to {recipient} not added.");
                return true; // Return true but don't add duplicate
            }
        }

        RelationshipModel relationship = new(recipient, desc);
        sourceCharacter.RelationshipList.Add(relationship);

        if (mirror)
        {
            // Check for duplicate in mirror relationship too
            CharacterModel recipientCharacter = (CharacterModel)recipientElement;
            bool mirrorExists = false;
            foreach (var existingRel in recipientCharacter.RelationshipList)
            {
                if (existingRel.PartnerUuid == source &&
                    existingRel.RelationType == desc &&
                    existingRel.Trait == string.Empty &&
                    existingRel.Attitude == string.Empty)
                {
                    mirrorExists = true;
                    break;
                }
            }
            
            if (!mirrorExists)
            {
                RelationshipModel mirrorRelationship = new(source, desc);
                recipientCharacter.RelationshipList.Add(mirrorRelationship);
            }
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
    internal bool AddCastMember(StoryModel Model, StoryElement source, Guid castMember)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }
        
        _log.Log(LogLevel.Info, $"AddCastMember called for cast member {castMember} on source {source.Uuid}.");

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

    internal SceneModel ConvertProblemToScene(StoryModel model, ProblemModel problem)
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
    internal ProblemModel ConvertSceneToProblem(StoryModel model, SceneModel scene)
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
    internal void AssignElementToBeat(StoryModel Model, ProblemModel Parent, int Index, Guid DesiredBind)
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
        StoryElement DesiredBindElement;
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
    internal void UnasignBeat(StoryModel Model, ProblemModel Parent, int Index)
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
    internal void CreateBeat(ProblemModel Parent, string Title, string Description)
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
    internal void DeleteBeat(StoryModel Model, ProblemModel Parent, int Index)
    {
        if (Model == null)  {  throw new ArgumentNullException(nameof(Model)); }
        if (Parent == null)
        {
            throw new ArgumentNullException(nameof(Parent));
        }

        if (Index < 0 || Index >= Parent.StructureBeats.Count)
        {
            throw new ArgumentOutOfRangeException("Index is invalid");
        }

        // Index is valid, proceed with deletion
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
    internal void SaveBeatsheet(string Path, string Description, List<StructureBeatViewModel> Beats)
    {
        SavedBeatsheet Model = new();
        Model.Beats = Beats = Beats
                .Select(b =>
                {
                    var copy = new StructureBeatViewModel();
                    copy.Title = b.Title;
                    copy.Description = b.Description;
                    return copy;
                })
                .ToList();
        Model.Description = Description;
        string data = JsonSerializer.Serialize(Model);
        File.WriteAllText(Path, data);
    }

    /// <summary>
    /// Loads a beatsheet
    /// </summary>
    /// <param name="path">File path to load</param>
    /// <returns>Model</returns>
    internal SavedBeatsheet LoadBeatsheet(string path)
    {
        string data = File.ReadAllText(path);
        var model = JsonSerializer.Deserialize<SavedBeatsheet>(data);
        foreach (var Beat in model.Beats) { Beat.Guid = Guid.Empty; }

        return model;
    }

    /// <summary>
    /// Sets the Changed status of a StoryModel with proper SerializationLock handling
    /// </summary>
    /// <param name="model">The StoryModel to update</param>
    /// <param name="changed">The desired Changed status</param>
    /// <exception cref="ArgumentNullException">Thrown when model is null</exception>
    internal void SetChanged(StoryModel model, bool changed)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        var autoSaveService = Ioc.Default.GetRequiredService<AutoSaveService>();
        var backupService = Ioc.Default.GetRequiredService<BackupService>();
        
        using (var serializationLock = new SerializationLock(autoSaveService, backupService, _log))
        {
            model.Changed = changed;
            _log.Log(LogLevel.Info, $"Model Changed status set to {changed}");
        }
    }

    /// <summary>
    /// Gets a StoryElement by its GUID from the StoryModel
    /// </summary>
    /// <param name="model">The StoryModel to search in</param>
    /// <param name="guid">The GUID of the element to find</param>
    /// <returns>The StoryElement with the matching GUID</returns>
    /// <exception cref="ArgumentNullException">Thrown when model is null</exception>
    /// <exception cref="ArgumentException">Thrown when guid is empty</exception>
    /// <exception cref="InvalidOperationException">Thrown when element with guid is not found</exception>
    internal StoryElement GetStoryElementByGuid(StoryModel model, Guid guid)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));
            
        if (guid == Guid.Empty)
            throw new ArgumentException("GUID cannot be empty", nameof(guid));

        var autoSaveService = Ioc.Default.GetRequiredService<AutoSaveService>();
        var backupService = Ioc.Default.GetRequiredService<BackupService>();
        
        using (var serializationLock = new SerializationLock(autoSaveService, backupService, _log))
        {
            if (!model.StoryElements.StoryElementGuids.TryGetValue(guid, out var element))
            {
                throw new InvalidOperationException($"StoryElement with GUID {guid} not found");
            }
            
            _log.Log(LogLevel.Info, $"Retrieved StoryElement {element.Name} with GUID {guid}");
            return element;
        }
    }

    /// <summary>
    /// Updates a StoryElement in the StoryModel with proper SerializationLock handling
    /// </summary>
    /// <param name="model">The StoryModel containing the element</param>
    /// <param name="element">The StoryElement to update</param>
    /// <exception cref="ArgumentNullException">Thrown when model or element is null</exception>
    internal void UpdateStoryElement(StoryModel model, StoryElement element)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));
            
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        var autoSaveService = Ioc.Default.GetRequiredService<AutoSaveService>();
        var backupService = Ioc.Default.GetRequiredService<BackupService>();
        
        using (var serializationLock = new SerializationLock(autoSaveService, backupService, _log))
        {
            // Update the element in the dictionary
            model.StoryElements.StoryElementGuids[element.Uuid] = element;
            
            // Mark the model as changed directly (already within a lock)
            model.Changed = true;
            
            _log.Log(LogLevel.Info, $"Updated StoryElement {element.Name} with GUID {element.Uuid}");
        }
    }

    /// <summary>
    /// Gets a list of all character elements from the story model.
    /// </summary>
    /// <param name="model">The story model to retrieve characters from</param>
    /// <returns>A list of CharacterModel elements</returns>
    internal List<CharacterModel> GetCharacterList(StoryModel model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        var autoSaveService = Ioc.Default.GetRequiredService<AutoSaveService>();
        var backupService = Ioc.Default.GetRequiredService<BackupService>();
        
        using (var serializationLock = new SerializationLock(autoSaveService, backupService, _log))
        {
            var characters = model.StoryElements
                .Where(e => e.ElementType == StoryItemType.Character)
                .Cast<CharacterModel>()
                .ToList();
                
            _log.Log(LogLevel.Info, $"Retrieved {characters.Count} characters from model");
            return characters;
        }
    }

    /// <summary>
    /// Updates a story element by its GUID.
    /// </summary>
    /// <param name="model">The story model containing the element</param>
    /// <param name="guid">The GUID of the element to update</param>
    /// <param name="updatedElement">The updated element data</param>
    internal void UpdateStoryElementByGuid(StoryModel model, Guid guid, StoryElement updatedElement)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));
            
        if (guid == Guid.Empty)
            throw new ArgumentException("GUID cannot be empty", nameof(guid));
            
        if (updatedElement == null)
            throw new ArgumentNullException(nameof(updatedElement));

        // First verify the element exists
        var existingElement = GetStoryElementByGuid(model, guid);
        
        // Set the GUID to ensure consistency
        updatedElement.Uuid = guid;
        
        // Update using the existing UpdateStoryElement method
        UpdateStoryElement(model, updatedElement);
    }

    /// <summary>
    /// Gets a list of all setting elements from the story model.
    /// </summary>
    /// <param name="model">The story model to retrieve settings from</param>
    /// <returns>A list of SettingModel elements</returns>
    internal List<SettingModel> GetSettingsList(StoryModel model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        var autoSaveService = Ioc.Default.GetRequiredService<AutoSaveService>();
        var backupService = Ioc.Default.GetRequiredService<BackupService>();
        
        using (var serializationLock = new SerializationLock(autoSaveService, backupService, _log))
        {
            var settings = model.StoryElements
                .Where(e => e.ElementType == StoryItemType.Setting)
                .Cast<SettingModel>()
                .ToList();
                
            _log.Log(LogLevel.Info, $"Retrieved {settings.Count} settings from model");
            return settings;
        }
    }

    /// <summary>
    /// Gets all story elements from the model.
    /// </summary>
    /// <param name="model">The story model to retrieve elements from</param>
    /// <returns>A list of all story elements</returns>
    internal List<StoryElement> GetAllStoryElements(StoryModel model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        var autoSaveService = Ioc.Default.GetRequiredService<AutoSaveService>();
        var backupService = Ioc.Default.GetRequiredService<BackupService>();
        
        using (var serializationLock = new SerializationLock(autoSaveService, backupService, _log))
        {
            var elements = model.StoryElements.ToList();
            _log.Log(LogLevel.Info, $"Retrieved {elements.Count} story elements from model");
            return elements;
        }
    }

    /// <summary>
    /// Searches for story elements containing the specified text.
    /// </summary>
    /// <param name="model">The story model to search in</param>
    /// <param name="searchText">The text to search for (case-insensitive)</param>
    /// <returns>A list of story elements that contain the search text</returns>
    internal List<StoryElement> SearchForText(StoryModel model, string searchText)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));
            
        if (string.IsNullOrWhiteSpace(searchText))
            return new List<StoryElement>();

        _log.Log(LogLevel.Info, $"Searching for text: {searchText}");
        
        var searchService = new SearchService();
        var results = new List<StoryElement>();
        
        var autoSaveService = Ioc.Default.GetRequiredService<AutoSaveService>();
        var backupService = Ioc.Default.GetRequiredService<BackupService>();
        
        using (var serializationLock = new SerializationLock(autoSaveService, backupService, _log))
        {
            foreach (var element in model.StoryElements)
            {
                if (searchService.SearchString(element.Node, searchText, model))
                {
                    results.Add(element);
                }
            }
        }
        
        _log.Log(LogLevel.Info, $"Found {results.Count} elements containing '{searchText}'");
        return results;
    }

    /// <summary>
    /// Searches for story elements that reference the specified UUID.
    /// </summary>
    /// <param name="model">The story model to search in</param>
    /// <param name="targetUuid">The UUID to search for</param>
    /// <returns>A list of story elements that reference the specified UUID</returns>
    internal List<StoryElement> SearchForUuidReferences(StoryModel model, Guid targetUuid)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));
            
        if (targetUuid == Guid.Empty)
            return new List<StoryElement>();

        _log.Log(LogLevel.Info, $"Searching for UUID references: {targetUuid}");
        
        var searchService = new SearchService();
        var results = new List<StoryElement>();
        
        var autoSaveService = Ioc.Default.GetRequiredService<AutoSaveService>();
        var backupService = Ioc.Default.GetRequiredService<BackupService>();
        
        using (var serializationLock = new SerializationLock(autoSaveService, backupService, _log))
        {
            foreach (var element in model.StoryElements)
            {
                if (searchService.SearchUuid(element.Node, targetUuid, model, false))
                {
                    results.Add(element);
                }
            }
        }
        
        _log.Log(LogLevel.Info, $"Found {results.Count} elements referencing UUID {targetUuid}");
        return results;
    }

    /// <summary>
    /// Removes all references to a specified UUID from the story model.
    /// </summary>
    /// <param name="model">The story model to clean</param>
    /// <param name="targetUuid">The UUID to remove references to</param>
    /// <returns>The number of elements that had references removed</returns>
    internal int RemoveUuidReferences(StoryModel model, Guid targetUuid)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));
            
        if (targetUuid == Guid.Empty)
            return 0;

        _log.Log(LogLevel.Info, $"Removing references to UUID: {targetUuid}");
        
        var searchService = new SearchService();
        int affectedCount = 0;
        
        var autoSaveService = Ioc.Default.GetRequiredService<AutoSaveService>();
        var backupService = Ioc.Default.GetRequiredService<BackupService>();
        
        using (var serializationLock = new SerializationLock(autoSaveService, backupService, _log))
        {
            foreach (var element in model.StoryElements)
            {
                if (searchService.SearchUuid(element.Node, targetUuid, model, true))
                {
                    affectedCount++;
                }
            }
            
            if (affectedCount > 0)
            {
                model.Changed = true;
            }
        }
        
        _log.Log(LogLevel.Info, $"Removed references from {affectedCount} elements");
        return affectedCount;
    }

    /// <summary>
    /// Searches for story elements within a specific subtree.
    /// </summary>
    /// <param name="model">The story model to search in</param>
    /// <param name="rootNode">The root node of the subtree to search</param>
    /// <param name="searchText">The text to search for (case-insensitive)</param>
    /// <returns>A list of story elements in the subtree that contain the search text</returns>
    internal List<StoryElement> SearchInSubtree(StoryModel model, StoryNodeItem rootNode, string searchText)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));
            
        if (rootNode == null)
            throw new ArgumentNullException(nameof(rootNode));
            
        if (string.IsNullOrWhiteSpace(searchText))
            return new List<StoryElement>();

        _log.Log(LogLevel.Info, $"Searching in subtree rooted at {rootNode.Name} for text: {searchText}");
        
        var searchService = new SearchService();
        var results = new List<StoryElement>();
        
        var autoSaveService = Ioc.Default.GetRequiredService<AutoSaveService>();
        var backupService = Ioc.Default.GetRequiredService<BackupService>();
        
        using (var serializationLock = new SerializationLock(autoSaveService, backupService, _log))
        {
            // Search the root node
            if (searchService.SearchString(rootNode, searchText, model))
            {
                if (model.StoryElements.StoryElementGuids.TryGetValue(rootNode.Uuid, out var element))
                {
                    results.Add(element);
                }
            }
            
            // Recursively search children
            SearchChildrenRecursive(rootNode, searchText, model, searchService, results);
        }
        
        _log.Log(LogLevel.Info, $"Found {results.Count} elements in subtree containing '{searchText}'");
        return results;
    }

    /// <summary>
    /// Helper method to recursively search children nodes.
    /// </summary>
    private void SearchChildrenRecursive(StoryNodeItem parentNode, string searchText, 
        StoryModel model, SearchService searchService, List<StoryElement> results)
    {
        foreach (var childNode in parentNode.Children)
        {
            if (searchService.SearchString(childNode, searchText, model))
            {
                if (model.StoryElements.StoryElementGuids.TryGetValue(childNode.Uuid, out var element))
                {
                    results.Add(element);
                }
            }
            
            // Recursively search this child's children
            SearchChildrenRecursive(childNode, searchText, model, searchService, results);
        }
    }

    #region Trash Operations

    /// <summary>
    /// Moves a story element to the trash.
    /// </summary>
    /// <param name="element">The element to move to trash</param>
    /// <param name="model">The story model</param>
    /// <exception cref="ArgumentNullException">Thrown when element or model is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when element cannot be moved to trash</exception>
    internal void MoveToTrash(StoryElement element, StoryModel model)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));
        
        if (model == null)
            throw new ArgumentNullException(nameof(model));
        
        // Validate element can be moved to trash
        if (element.Node.IsRoot)
            throw new InvalidOperationException("Cannot move root nodes to trash");
        
        if (element.ElementType == StoryItemType.TrashCan)
            throw new InvalidOperationException("Cannot move TrashCan to trash");
        
        if (element.ElementType == StoryItemType.StoryOverview)
            throw new InvalidOperationException("Cannot move StoryOverview to trash");

        _log.Log(LogLevel.Info, $"Moving element {element.Uuid} to trash");

        var autoSaveService = Ioc.Default.GetRequiredService<AutoSaveService>();
        var backupService = Ioc.Default.GetRequiredService<BackupService>();
        
        using (var serializationLock = new SerializationLock(autoSaveService, backupService, _log))
        {
            // Remove references to this element from other elements
            RemoveReferenceToElement(element.Uuid, model);
            
            // Get the trash node
            var trashNode = model.TrashView.FirstOrDefault(n => n.Type == StoryItemType.TrashCan);
            if (trashNode == null)
            {
                _log.Log(LogLevel.Error, "TrashCan node not found");
                throw new InvalidOperationException("TrashCan node not found in model");
            }
            
            // Remove from current parent
            var parent = element.Node.Parent;
            if (parent != null)
            {
                parent.Children.Remove(element.Node);
            }
            
            // Add to trash
            element.Node.Parent = trashNode;
            trashNode.Children.Add(element.Node);
            
            // Mark model as changed
            model.Changed = true;
            
            _log.Log(LogLevel.Info, $"Successfully moved element {element.Uuid} to trash");
        }
    }

    /// <summary>
    /// Restores an element from trash back to the explorer view.
    /// </summary>
    /// <param name="trashNode">The node in trash to restore</param>
    /// <param name="model">The story model</param>
    /// <exception cref="ArgumentNullException">Thrown when trashNode or model is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when node cannot be restored</exception>
    internal void RestoreFromTrash(StoryNodeItem trashNode, StoryModel model)
    {
        if (trashNode == null)
            throw new ArgumentNullException(nameof(trashNode));
        
        if (model == null)
            throw new ArgumentNullException(nameof(model));
        
        // Validate node is in trash
        if (StoryNodeItem.RootNodeType(trashNode) != StoryItemType.TrashCan)
            throw new InvalidOperationException("Can only restore items from trash");
        
        // Only allow restoring top-level items from trash
        if (trashNode.Parent?.Type != StoryItemType.TrashCan)
            throw new InvalidOperationException("Can only restore top-level items from trash. Restore the parent item instead.");

        _log.Log(LogLevel.Info, $"Restoring element {trashNode.Uuid} from trash");

        var autoSaveService = Ioc.Default.GetRequiredService<AutoSaveService>();
        var backupService = Ioc.Default.GetRequiredService<BackupService>();
        
        using (var serializationLock = new SerializationLock(autoSaveService, backupService, _log))
        {
            // Get the overview node (root of explorer view)
            var overviewNode = model.ExplorerView.FirstOrDefault();
            if (overviewNode == null)
            {
                _log.Log(LogLevel.Error, "Explorer view root not found");
                throw new InvalidOperationException("Explorer view root not found in model");
            }
            
            // Remove from trash
            var trashCanNode = trashNode.Parent;
            trashCanNode.Children.Remove(trashNode);
            
            // Add to explorer view (at the end)
            trashNode.Parent = overviewNode;
            overviewNode.Children.Add(trashNode);
            
            // Mark model as changed
            model.Changed = true;
            
            _log.Log(LogLevel.Info, $"Successfully restored element {trashNode.Uuid} from trash");
        }
    }

    /// <summary>
    /// Empties the trash, permanently removing all items.
    /// </summary>
    /// <param name="model">The story model</param>
    /// <exception cref="ArgumentNullException">Thrown when model is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when TrashCan node is not found</exception>
    internal void EmptyTrash(StoryModel model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        _log.Log(LogLevel.Info, "Emptying trash");

        var autoSaveService = Ioc.Default.GetRequiredService<AutoSaveService>();
        var backupService = Ioc.Default.GetRequiredService<BackupService>();
        
        using (var serializationLock = new SerializationLock(autoSaveService, backupService, _log))
        {
            // Get the trash node
            var trashNode = model.TrashView.FirstOrDefault(n => n.Type == StoryItemType.TrashCan);
            if (trashNode == null)
            {
                _log.Log(LogLevel.Error, "TrashCan node not found");
                throw new InvalidOperationException("TrashCan node not found");
            }
            
            // Clear all children
            int itemCount = trashNode.Children.Count;
            trashNode.Children.Clear();
            
            // Mark model as changed if items were removed
            if (itemCount > 0)
            {
                model.Changed = true;
            }
            
            _log.Log(LogLevel.Info, $"Successfully emptied trash, removed {itemCount} items");
        }
    }

    #endregion
}
