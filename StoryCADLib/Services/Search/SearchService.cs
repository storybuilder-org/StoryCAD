using ABI.Windows.Media.Audio;
//using NLog;
using StoryCAD.Services.Logging;
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
        ElementCollection = Ioc.Default.GetService<ShellViewModel>().StoryModel.StoryElements;

        if (model.StoryElements.StoryElementGuids.ContainsKey(node.Uuid)) { element = model.StoryElements.StoryElementGuids[node.Uuid]; }
        if (element == null) { return false; }
        switch (element.Type)
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

    private bool SearchSection(StoryNodeItem node, StoryElement element)
    {
        return Comparator(element.Name);
    }

    private bool SearchFolder(StoryNodeItem node, StoryElement element)
    {
        return Comparator(element.Name);
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
        if (Comparator(element.Name)) { return true; }  //Searches node name
        if (CompareStoryElement(scene.ViewpointCharacter)) { return true; }
        if (CompareStoryElement(scene.Protagonist)) { return true; }
        if (CompareStoryElement(scene.Antagonist)) { return true; }
        if (CompareStoryElement(scene.Setting)) { return true; }

        return false; //No match, return false
    }

    private bool SearchSetting(StoryNodeItem node, StoryElement element)
    {
        return Comparator(element.Name);
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
        return Comparator(element.Name); //Checks element name
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
        if (Comparator(element.Name)) { return true; }

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
            string problemName = problem.Name;
            if (Comparator(problemName)) { return true; }
        }

        if (Comparator(element.Name)) { return true; } //checks node name

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
        ShellViewModel shell = Ioc.Default.GetService<ShellViewModel>();
        StoryElementCollection elements = shell!.StoryModel.StoryElements;

        if (elements.StoryElementGuids.TryGetValue(guid, out StoryElement element))
        {
            return Comparator(element.Name);
        }

        return false;
    }

    public SearchService()
    {
        logger = Ioc.Default.GetService<LogService>();
    }
}
