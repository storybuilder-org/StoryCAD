using System;
using CommunityToolkit.Mvvm.DependencyInjection;
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
            ElementCollection.StoryElementGuids.TryGetValue(Guid.Parse(member), out StoryElement Model);
            Model = Model as CharacterModel;
            if (Comparator(Model.Name)) { return true; }
        }

        if (Comparator(element.Name)) { return true; }  //Searches node name

        if (!string.IsNullOrEmpty(scene.ViewpointCharacter)) //Searches VP characters
        {
            ElementCollection.StoryElementGuids.TryGetValue(Guid.Parse(scene.ViewpointCharacter), out StoryElement vpChar);
            if (Comparator(vpChar.Name)) { return true; }
        }
        if (!string.IsNullOrEmpty(scene.Protagonist)) //Searches protagonist
        {
            ElementCollection.StoryElementGuids.TryGetValue(Guid.Parse(scene.Protagonist), out StoryElement protag);
            if (Comparator(protag.Name)) { return true; }
        }
        if (!string.IsNullOrEmpty(scene.Antagonist)) //Searches Antagonist
        {
            ElementCollection.StoryElementGuids.TryGetValue(Guid.Parse(scene.Antagonist), out StoryElement antag);
            if (Comparator(antag.Name)) { return true; }
        }
        if (!string.IsNullOrEmpty(scene.Setting))
        {
            ElementCollection.StoryElementGuids.TryGetValue(Guid.Parse(scene.Setting), out StoryElement setting);
            if (Comparator(setting.Name)) { return true; }
        }
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

        foreach (RelationshipModel partner in characterModel.RelationshipList) //Checks each character in relationship
        {
            ElementCollection.StoryElementGuids.TryGetValue(Guid.Parse(partner.PartnerUuid), out StoryElement Model);
            if (Comparator(Model.Name)) { return true; }
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

        if (!string.IsNullOrEmpty(problem.Protagonist))//Checks protags name
        {
            ElementCollection.StoryElementGuids.TryGetValue(Guid.Parse(problem.Protagonist), out StoryElement protag);
            if (Comparator(protag.Name)) { return true; } 

        }
        if (!string.IsNullOrEmpty(problem.Antagonist))//Checks antags name
        {
            ElementCollection.StoryElementGuids.TryGetValue(Guid.Parse(problem.Antagonist), out StoryElement antag);
            if (Comparator(antag.Name)) { return true; } 
        }

        if (Comparator(element.Name)) { return true; } //Checks name of node
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
        if (!string.IsNullOrEmpty(overview.StoryProblem))
        {
            ElementCollection.StoryElementGuids.TryGetValue(Guid.Parse(overview.StoryProblem), out StoryElement problem);
            if (Comparator(problem.Name)) { return true; } //Checks problem name
        }

        if (Comparator(element.Name)) { return true; } //checks node name

        return false;
    }
}