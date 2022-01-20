using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.Models;
using StoryBuilder.ViewModels;
using System;

namespace StoryBuilder.Services.Search;

public class SearchService
{
    private string arg;
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

    private bool SearchScene(StoryNodeItem node, StoryElement element)
    {
        SceneModel scene = (SceneModel)element;
        foreach (string member in scene.CastMembers)
        {
            Ioc.Default.GetService<ShellViewModel>().StoryModel.StoryElements.StoryElementGuids.TryGetValue(Guid.Parse(member), out StoryElement Model);
            Model = Model as CharacterModel;
            if (Comparator(Model.Name)) { return true; }
        }

        Ioc.Default.GetService<ShellViewModel>().StoryModel.StoryElements.StoryElementGuids.TryGetValue(Guid.Parse(scene.Protagonist), out StoryElement protag);
        Ioc.Default.GetService<ShellViewModel>().StoryModel.StoryElements.StoryElementGuids.TryGetValue(Guid.Parse(scene.Antagonist), out StoryElement antag);
        if (Comparator(element.Name)) { return true; }
        else if  (Comparator(protag.Name)) { return true; }
        else if (Comparator(antag.Name)) { return true; }
        return false;
    }

    private bool SearchSetting(StoryNodeItem node, StoryElement element)
    {
        return Comparator(element.Name);
    }

    private bool SearchCharacter(StoryNodeItem node, StoryElement element)
    {
        CharacterModel characterModel = (CharacterModel)element;

        foreach (RelationshipModel partner in characterModel.RelationshipList)
        {
            Ioc.Default.GetService<ShellViewModel>().StoryModel.StoryElements.StoryElementGuids.TryGetValue(Guid.Parse(partner.PartnerUuid), out StoryElement Model);
            if (Comparator(Model.Name)) { return true; }
        }

        return Comparator(element.Name);
    }

    private bool SearchProblem(StoryNodeItem node, StoryElement element)
    {
        ProblemModel problem = (ProblemModel)element;
        StoryElement protag = null;
        StoryElement antag = null;
        Ioc.Default.GetService<ShellViewModel>().StoryModel.StoryElements.StoryElementGuids.TryGetValue(Guid.Parse(problem.Protagonist), out antag);
        Ioc.Default.GetService<ShellViewModel>().StoryModel.StoryElements.StoryElementGuids.TryGetValue(Guid.Parse(problem.Antagonist), out protag);
        protag = (CharacterModel)protag;
        antag = (CharacterModel)antag;

        if (Comparator(element.Name)) { return true; }
        else if (Comparator(protag.Name)) { return true; } //Checks protags name (if UUID)
        else if (Comparator(antag.Name)) { return true; } //Checks antags name (if UUID)
        else { return false; }
    }

    private bool SearchStoryOverview(StoryNodeItem node, StoryElement element)
    {
            OverviewModel overview = (OverviewModel)element;
        StoryElement problem;
        Ioc.Default.GetService<ShellViewModel>().StoryModel.StoryElements.StoryElementGuids.TryGetValue(Guid.Parse(overview.StoryProblem), out problem);
        if (Comparator(element.Name)) { return true; }
        else if (Comparator(problem.Name)) { return true; }
        else { return false; }
    }
}