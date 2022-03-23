using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.Models;
using StoryBuilder.ViewModels;
using System;
using System.Collections.Generic;

namespace StoryBuilder.Services.Search;

/// <summary>
/// This is a copy of the search service however 
/// It does not search the name of the node and 
/// each search has an optional argument to delete references.
/// </summary>
public class DeletionService
{
    private Guid arg;
    StoryElementCollection ElementCollection;
    /// <summary>
    /// Search a StoryElement for a given string search argument
    /// </summary>
    /// <param name="node">StoryNodeItem whose StoryElement to search</param>
    /// <param name="searchArg">string to search for</param>
    /// <param name="model">model to search in</param>
    /// <returns>true if StoryyElement contains search argument</returns>
    public bool SearchStoryElement(StoryNodeItem node, Guid searchArg, StoryModel model, bool Delete = false)
    {
        bool result = false;
        arg = searchArg;
        StoryElement element = null;
        ElementCollection = Ioc.Default.GetService<ShellViewModel>().StoryModel.StoryElements;

        if (model.StoryElements.StoryElementGuids.ContainsKey(node.Uuid)) { element = model.StoryElements.StoryElementGuids[node.Uuid]; }
        if (element == null) { return false; } 
        switch (element.Type)
        {
            case StoryItemType.StoryOverview:
                result = SearchStoryOverview(node, element, Delete);
                break;
            case StoryItemType.Problem:
                result = SearchProblem(node, element, Delete);
                break;
            case StoryItemType.Character:
                result = SearchCharacter(node, element, Delete);
                break;
            case StoryItemType.Scene:
                result = SearchScene(node, element, Delete);
                break;
        }
        return result;
    }

    /// <summary>
    /// Searches Cast members, viewpoint character, protagonist name, antagonist name and the name of the scene and the selected setting in a scene node
    /// </summary>
    /// <param name="node"></param>
    /// <param name="element"></param>
    /// <returns></returns>
    private bool SearchScene(StoryNodeItem node, StoryElement element, bool Delete)
    {
        SceneModel scene = (SceneModel)element;

        List<string> NewCast = new();
        foreach (string member in scene.CastMembers) //Searches character in scene
        {
            ElementCollection.StoryElementGuids.TryGetValue(Guid.Parse(member), out StoryElement Model);
            Model = Model as CharacterModel;
            if (Model.Uuid == arg) 
            {
                if (!Delete) { return true; }
            }
            else { NewCast.Add(member); }
        }
        if (Delete) { scene.CastMembers = NewCast; }

        if (!string.IsNullOrEmpty(scene.ViewpointCharacter)) //Searches protagonist
        {
            ElementCollection.StoryElementGuids.TryGetValue(Guid.Parse(scene.ViewpointCharacter), out StoryElement vpChar);
            if (vpChar.Uuid == arg)
            {
                if (Delete) { scene.ViewpointCharacter = null; }
                else { return true; }
            }
        }

        if (!string.IsNullOrEmpty(scene.Protagonist)) //Searches protagonist
        {
            ElementCollection.StoryElementGuids.TryGetValue(Guid.Parse(scene.Protagonist), out StoryElement protag);
            if (protag.Uuid == arg) 
            {
                if (Delete) { scene.Protagonist = null; }
                else { return true; }
            }
        }
        
        if (!string.IsNullOrEmpty(scene.Antagonist)) //Searches Antagonist
        {
            ElementCollection.StoryElementGuids.TryGetValue(Guid.Parse(scene.Antagonist), out StoryElement antag);
            if (antag.Uuid == arg) 
            {
                if (Delete) { scene.Antagonist = null; }
                else { return true; } 
            }
        
        }

        if (!string.IsNullOrEmpty(scene.Setting))
        {
            ElementCollection.StoryElementGuids.TryGetValue(Guid.Parse(scene.Setting), out StoryElement setting);
            if (setting.Uuid == arg) 
            {
                if (Delete) { scene.Setting = null; }
                else { return true; }
            }
        }

        return false;
    }

    /// <summary>
    /// Searches the name of each character in a relationship and the name of the character
    /// </summary>
    /// <param name="node"></param>
    /// <param name="element"></param>
    /// <returns></returns>
    private bool SearchCharacter(StoryNodeItem node, StoryElement element, bool Delete)
    {
        CharacterModel characterModel = (CharacterModel)element;

        List<RelationshipModel> NewReleationships = new();
        foreach (RelationshipModel partner in characterModel.RelationshipList) //Checks each character in relationship
        {
            ElementCollection.StoryElementGuids.TryGetValue(Guid.Parse(partner.PartnerUuid), out StoryElement Model);
            if (Model.Uuid == arg) 
            {
                if (!Delete) { return true; }
            }
            else { NewReleationships.Add(partner); }
        }
        if (Delete) { characterModel.RelationshipList = NewReleationships; }
        return false;
    }

    /// <summary>
    /// Searches a problem for the element name, Antag name, protag name,
    /// </summary>
    /// <param name="node"></param>
    /// <param name="element"></param>
    /// <returns></returns>
    private bool SearchProblem(StoryNodeItem node, StoryElement element, bool Delete)
    {
        ProblemModel problem = (ProblemModel)element;

        if (!string.IsNullOrEmpty(problem.Protagonist))//Checks protags name
        {
            ElementCollection.StoryElementGuids.TryGetValue(Guid.Parse(problem.Protagonist), out StoryElement protag);
            if (protag.Uuid == arg) 
            {
                if (Delete) { problem.Protagonist = null; }
                else { return true; }
            } 

        }

        if (!string.IsNullOrEmpty(problem.Antagonist))//Checks antags name
        {
            ElementCollection.StoryElementGuids.TryGetValue(Guid.Parse(problem.Antagonist), out StoryElement antag);
            if (antag.Uuid == arg) 
            {
                if (Delete) { problem.Antagonist = null; }
                else { return true;}
            } 
        }

        return false;
    }

    /// <summary>
    /// Searches the overview node for the name and main story problem
    /// </summary>
    /// <param name="node"></param>
    /// <param name="element"></param>
    /// <returns></returns>
    private bool SearchStoryOverview(StoryNodeItem node, StoryElement element, bool Delete)
    {
        OverviewModel overview = (OverviewModel)element;
        if (!string.IsNullOrEmpty(overview.StoryProblem))
        {
            ElementCollection.StoryElementGuids.TryGetValue(Guid.Parse(overview.StoryProblem), out StoryElement problem);
            if (problem.Uuid == arg) 
            {
                if (Delete) { overview.StoryProblem = null; }
                else { return true; }  
            } //Checks problem name
        }
         return false;
    }
}