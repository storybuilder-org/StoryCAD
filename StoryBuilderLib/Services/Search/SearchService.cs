﻿using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.Models;
using StoryBuilder.ViewModels;
using System;

namespace StoryBuilder.Services.Search;

public class SearchService
{
    private string arg;
    StoryElementCollection ElementCollection = Ioc.Default.GetService<ShellViewModel>().StoryModel.StoryElements;
    /// <summary>
    /// Search a StoryElement for a given string search argument
    /// </summary>
    /// <param name="node">StoryNodeItem whose StoryElement to search</param>
    /// <param name="searchArg">string to search for</param>
    /// <param name="model">model to search in</param>
    /// <returns>true if StoryyElement contains search argument</returns>
    public bool SearchStoryElement(StoryNodeItem node, string searchArg, StoryModel model)
    {
        bool result = false;
        arg = searchArg.ToLower();
        StoryElement element = null;

        if (model.StoryElements.StoryElementGuids.ContainsKey(node.Uuid))
            element = model.StoryElements.StoryElementGuids[node.Uuid];
        if (element == null)
            return false;
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
    /// Searches Cast members, protagonist name, antagonist name and the name of scene in a scene
    /// </summary>
    /// <param name="node"></param>
    /// <param name="element"></param>
    /// <returns></returns>
    private bool SearchScene(StoryNodeItem node, StoryElement element)
    {
        SceneModel scene = (SceneModel)element;
        foreach (string member in scene.CastMembers)
        {
            ElementCollection.StoryElementGuids.TryGetValue(Guid.Parse(member), out StoryElement Model);
            Model = Model as CharacterModel;
            if (Comparator(Model.Name)) { return true; }
        }

        ElementCollection.StoryElementGuids.TryGetValue(Guid.Parse(scene.Protagonist), out StoryElement protag);
        ElementCollection.StoryElementGuids.TryGetValue(Guid.Parse(scene.Antagonist), out StoryElement antag);
        if (Comparator(element.Name)) { return true; }
        else if  (Comparator(protag.Name)) { return true; }
        else if (Comparator(antag.Name)) { return true; }
        return false;
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

        //Gets the Protagonist/Antagonist from the UUID provided in the problem node, thencast
        ElementCollection.StoryElementGuids.TryGetValue(Guid.Parse(problem.Protagonist), out StoryElement antag);
        ElementCollection.StoryElementGuids.TryGetValue(Guid.Parse(problem.Antagonist), out StoryElement protag);

        //Casts to the character model
        protag = (CharacterModel)protag;
        antag = (CharacterModel)antag;

        if (Comparator(element.Name)) { return true; } //Checks name of node
        else if (Comparator(protag.Name)) { return true; } //Checks protags name
        else if (Comparator(antag.Name)) { return true; } //Checks antags name
        else { return false; }
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
        ElementCollection.StoryElementGuids.TryGetValue(Guid.Parse(overview.StoryProblem), out StoryElement problem);
        if (Comparator(element.Name)) { return true; } //checks node name
        else if (Comparator(problem.Name)) { return true; } //Checks problem name
        else { return false; }
    }
}