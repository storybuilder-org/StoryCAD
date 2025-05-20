using StoryCAD.Services.Logging;
using StoryCAD.ViewModels.SubViewModels;
using System.Reflection;

//using LogLevel = StoryCAD.Services.Logging.LogLevel;

namespace StoryCAD.Services.Search;

/// <summary>
/// Service responsible for searching StoryElements within the StoryModel based on a given search argument.
/// It supports searching various types of StoryElements, including Scenes, Characters, Problems, Settings, etc.
/// </summary>
public class SearchService
{
    private readonly LogService logger;
    private string arg;
    private Guid guidArg;
    StoryElementCollection ElementCollection;
    
    /// <summary>
    /// Searches a <see cref="StoryElement"/> for a given string search argument.
    /// </summary>
    /// <param name="node">The <see cref="StoryNodeItem"/> whose <see cref="StoryElement"/> will be searched.</param>
    /// <param name="searchArg">The string to search for within the StoryElement.</param>
    /// <param name="model">The <see cref="StoryModel"/> containing the StoryElements.</param>
    /// <returns><c>true</c> if the StoryElement contains the search argument; otherwise, <c>false</c>.</returns>
    public bool SearchStoryElement(StoryNodeItem node, string searchArg, StoryModel model)
    {
        if (searchArg == null)
        {
            logger.Log(LogLevel.Warn, "Search argument is null, returning false.");
            return false;
        } // Fixes blank search

        bool result = false;
        arg = searchArg.ToLower();
        StoryElement element = null;
        ElementCollection = model.StoryElements;

        if (model.StoryElements.StoryElementGuids.ContainsKey(node.Uuid)) { element = model.StoryElements.StoryElementGuids[node.Uuid]; }
        if (element == null) { return false; }
        switch (element.ElementType)
        {
            case StoryItemType.StoryOverview:
                result = SearchStoryOverview(node, element);
                break;
            case StoryItemType.Problem:
                result = SearchProblem(node, element);
                break;
            case StoryItemType.Character:
                result = SearchCharacter(node, element);
                break;
            case StoryItemType.Setting:
                result = SearchSetting(node, element);
                break;
            case StoryItemType.Scene:
                result = SearchScene(node, element);
                break;
            case StoryItemType.Folder:
                result = SearchFolder(node, element);
                break;
            case StoryItemType.Section:
                result = SearchSection(node, element);
                break;
        }
        return result;
    }
    
    /// <summary>
    /// Compares the provided text with the search argument.
    /// </summary>
    /// <param name="text">The text to compare.</param>
    /// <returns><c>true</c> if the text contains the search argument; otherwise, <c>false</c>.</returns>
    private bool Comparator(string text)
    {
        return text.ToLower().Contains(arg);
    }

    /// <summary>
    /// Searches all public string properties of an object for the current search argument.
    /// </summary>
    /// <param name="element">Object whose properties to inspect.</param>
    /// <returns>true if any string property contains the search text.</returns>
    private bool SearchStringFields(object element)
    {
        foreach (PropertyInfo prop in element.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.PropertyType == typeof(string))
            {
                string? value = prop.GetValue(element) as string;
                if (!string.IsNullOrEmpty(value) && Comparator(value))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool SearchSection(StoryNodeItem node, StoryElement element)
    {
        return SearchStringFields(element);
    }

    private bool SearchFolder(StoryNodeItem node, StoryElement element)
    {
        return SearchStringFields(element);
    }

    /// <summary>
    /// Searches Cast members, protagonist name, antagonist name and the name of the scene and the selected setting in a scene node
    /// </summary>
    /// <param name="node"></param>
    /// <param name="element"></param>
    /// <returns></returns>
    private bool SearchScene(StoryNodeItem node, StoryElement element)
    {
        SceneModel scene = (SceneModel)element;

        // Search through each CastMember represented by GUID
        foreach (Guid memberGuid in scene.CastMembers) // Searches character in scene
        {
            if (CompareStoryElement(memberGuid))
            {
                return true;
            }
        }
        
        // Search the Scene's properties
        if (SearchStringFields(scene)) { return true; }
        if (CompareStoryElement(scene.ViewpointCharacter)) { return true; }
        if (CompareStoryElement(scene.Protagonist)) { return true; }
        if (CompareStoryElement(scene.Antagonist)) { return true; }
        if (CompareStoryElement(scene.Setting)) { return true; }

        return false; //No match, return false
    }

    private bool SearchSetting(StoryNodeItem node, StoryElement element)
    {
        return SearchStringFields(element);
    }

    /// <summary>
    /// Searches the name of each character in a relationship and the name of the character
    /// </summary>
    /// <param name="node"></param>
    /// <param name="element"></param>
    /// <returns></returns>
    private bool SearchCharacter(StoryNodeItem node, StoryElement element)
    {
        CharacterModel characterModel = (CharacterModel)element;


        foreach (RelationshipModel relation in characterModel.RelationshipList) //Checks each character in relationship
        {
            Guid partner = relation.PartnerUuid;
            if (CompareStoryElement(partner)) { return true; }
        }
        if (SearchStringFields(characterModel)) { return true; }
        return false;
    }

    /// <summary>
    /// Searches a problem for the element name, Antag name, protag name,
    /// </summary>
    /// <param name="node"></param>
    /// <param name="element"></param>
    /// <returns></returns>
    private bool SearchProblem(StoryNodeItem node, StoryElement element)
    {
        ProblemModel problem = (ProblemModel)element;

        if (CompareStoryElement(problem.Protagonist)) { return true; }
        if (CompareStoryElement(problem.Antagonist)) { return true; }
        if (SearchStringFields(problem)) { return true; }

        return false;
    }

    /// <summary>
    /// Searches the overview node for the name and main story problem
    /// </summary>
    /// <param name="node"></param>
    /// <param name="element"></param>
    /// <returns></returns>
    private bool SearchStoryOverview(StoryNodeItem node, StoryElement element)
    {
        OverviewModel overview = (OverviewModel)element;

        if (overview.StoryProblem != Guid.Empty)
        {
            ProblemModel problem = (ProblemModel) ElementCollection.StoryElementGuids[overview.StoryProblem];
            if (SearchStringFields(problem)) { return true; }
        }

        if (SearchStringFields(overview)) { return true; }

        return false;
    }

    //TODO: Once all StoryElement references are converted to Guid instead of Guid.ToString(), remove the string version
    /// <summary>
    /// Compares the name of the StoryElement associated with the provided GUID to the search argument.
    /// </summary>
    /// <param name="guid">The GUID of the StoryElement to compare.</param>
    /// <returns><c>true</c> if the StoryElement's name contains the search argument; otherwise, <c>false</c>.</returns>
    private bool CompareStoryElement(Guid guid)
    {
        if (guid == Guid.Empty)
            return false;

        // Retrieve the StoryElement associated with the GUID
        OutlineViewModel shell = Ioc.Default.GetService<OutlineViewModel>();
        StoryElementCollection elements = shell!.StoryModel.StoryElements;

        if (elements.StoryElementGuids.TryGetValue(guid, out StoryElement element))
        {
            return Comparator(element.Name);
        }

        return false;
    }

    /// <summary>
    /// Searches a StoryElement for references to a specific Guid and optionally removes them.
    /// </summary>
    public bool SearchStoryElement(StoryNodeItem node, Guid searchGuid, StoryModel model, bool delete = false)
    {
        if (model?.StoryElements?.StoryElementGuids == null || node == null)
        {
            logger.Log(LogLevel.Info, "StoryElements or node is null");
            return false;
        }

        if (!model.StoryElements.StoryElementGuids.TryGetValue(node.Uuid, out var element) || element == null)
        {
            logger.Log(LogLevel.Info, $"No element found for UUID {node.Uuid}");
            return false;
        }

        guidArg = searchGuid;
        ElementCollection = model.StoryElements;

        return element.ElementType switch
        {
            StoryItemType.StoryOverview => SearchOverviewReferences((OverviewModel)element, delete),
            StoryItemType.Problem => SearchProblemReferences((ProblemModel)element, delete),
            StoryItemType.Character => SearchCharacterReferences((CharacterModel)element, delete),
            StoryItemType.Scene => SearchSceneReferences((SceneModel)element, delete),
            _ => false,
        };
    }

    private bool SearchSceneReferences(SceneModel scene, bool delete)
    {
        List<Guid> newCast = new();
        foreach (Guid memberGuid in scene.CastMembers)
        {
            if (ElementCollection.StoryElementGuids.TryGetValue(memberGuid, out StoryElement model))
            {
                if (model.Uuid == guidArg)
                {
                    if (!delete) return true;
                }
                else
                {
                    newCast.Add(memberGuid);
                }
            }
        }

        if (delete) scene.CastMembers = newCast;

        if (scene.Protagonist == guidArg)
        {
            if (delete) scene.Protagonist = Guid.Empty; else return true;
        }
        if (scene.Antagonist == guidArg)
        {
            if (delete) scene.Antagonist = Guid.Empty; else return true;
        }
        if (scene.ViewpointCharacter == guidArg)
        {
            if (delete) scene.ViewpointCharacter = Guid.Empty; else return true;
        }
        if (scene.Setting == guidArg)
        {
            if (delete) scene.Setting = Guid.Empty; else return true;
        }

        return false;
    }

    private bool SearchCharacterReferences(CharacterModel character, bool delete)
    {
        List<RelationshipModel> newRelationships = new();
        foreach (RelationshipModel partner in character.RelationshipList)
        {
            StoryElement model = StoryElement.GetByGuid(partner.PartnerUuid);
            if (model.Uuid == guidArg)
            {
                if (!delete) return true;
            }
            else
            {
                newRelationships.Add(partner);
            }
        }
        if (delete) character.RelationshipList = newRelationships;
        return false;
    }

    private bool SearchProblemReferences(ProblemModel problem, bool delete)
    {
        if (problem.Protagonist == guidArg)
        {
            if (delete) problem.Protagonist = Guid.Empty; else return true;
        }
        if (problem.Antagonist == guidArg)
        {
            if (delete) problem.Antagonist = Guid.Empty; else return true;
        }
        if (problem.StructureBeats != null)
        {
            foreach (var beat in problem.StructureBeats)
            {
                if (beat.Guid == guidArg)
                {
                    if (delete) beat.Guid = Guid.Empty; else return true;
                }
            }
        }
        return false;
    }

    private bool SearchOverviewReferences(OverviewModel overview, bool delete)
    {
        bool found = false;
        if (overview.StoryProblem == guidArg)
        {
            found = true;
            if (delete) overview.StoryProblem = Guid.Empty;
        }
        if (overview.ViewpointCharacter == guidArg)
        {
            found = true;
            if (delete) overview.ViewpointCharacter = Guid.Empty;
        }
        return found;
    }

    public SearchService()
    {
        logger = Ioc.Default.GetService<LogService>();
    }
}
