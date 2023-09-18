using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using Google.Protobuf.WellKnownTypes;
using StoryCAD.Models;
using StoryCAD.Services.Logging;
using StoryCAD.ViewModels;

namespace StoryCAD.Services.Search;

public class SearchService
{
    private string arg;
    StoryElementCollection ElementCollection;
    /// <summary>
    /// Search a StoryElement for a given string search argument
    /// </summary>
    /// <param name="node">StoryNodeItem whose StoryElement to search</param>
    /// <param name="searchArg">string to search for</param>
    /// <param name="model">model to search in</param>
    /// <returns>true if StoryyElement contains search argument</returns>
    public bool SearchStoryElement(StoryNodeItem node, string searchArg, StoryModel model)
    {
        if (searchArg == null)
        {
            Ioc.Default.GetRequiredService<ShellViewModel>().Logger.Log(LogLevel.Warn, "Search argument is null, returning false.");
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

        foreach (string member in scene.CastMembers) //Searches character in scene
        {
            if (CompareStoryElement(member)) { return true; }
        }
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
            string partner = relation.PartnerUuid;
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

        if (CompareStoryElement(overview.StoryProblem)) { return true; }
        if (Comparator(element.Name)) { return true; } //checks node name

        return false;
    }

    /// <summary>
    /// Validate that the passed value is a valid StoryElement.ToString,
    /// and if so, compare the StoryElement.Name to the Search argument
    /// </summary>
    /// <param name="value">a StoryElement guid as a string (possibly)</param>
    /// <returns>true if the Search argument matches the StoryElement.Name, false othewise</returns>
    private bool CompareStoryElement(string value)
    {
        if (value == null)
            return false;
        if (value.Equals(string.Empty))
            return false;
        // Get the current StoryModel's StoryElementsCollection
        ShellViewModel shell = Ioc.Default.GetService<ShellViewModel>();
        StoryElementCollection elements = shell.StoryModel.StoryElements;
        // legacy: locate the StoryElement from its Name
        // Look for the StoryElement corresponding to the passed guid
        if (Guid.TryParse(value, out Guid guid))
        {
            StoryElement element = elements.StoryElementGuids[guid];
            return Comparator(element.Name);
        }
        else
            return false;
    }
}
