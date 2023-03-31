using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.Models;
using StoryCAD.Services.Logging;
using StoryCAD.ViewModels;
using LogLevel = StoryCAD.Services.Logging.LogLevel;

namespace StoryCAD.Services.Search;

/// <summary>
/// This is a copy of the search service however 
/// It does not search the name of the node and 
/// each search has an optional argument to delete references.
/// </summary>
public class DeletionService
{
    private Guid _arg;
    private StoryElementCollection _elementCollection;
    private LogService _logger = Ioc.Default.GetRequiredService<LogService>();

    /// <summary>
    /// Search a StoryElement for a given string search argument
    /// </summary>
    /// <param name="node">StoryNodeItem whose StoryElement to search</param>
    /// <param name="searchArg">string to search for</param>
    /// <param name="model">model to search in</param>
    /// <returns>true if StoryElement contains search argument</returns>
    public bool SearchStoryElement(StoryNodeItem node, Guid searchArg, StoryModel model, bool delete = false)
    {
        bool result = false;
        _arg = searchArg;
        StoryElement element = null;
        _elementCollection = Ioc.Default.GetRequiredService<ShellViewModel>().StoryModel.StoryElements;

        if (model.StoryElements.StoryElementGuids.ContainsKey(node.Uuid)) { element = model.StoryElements.StoryElementGuids[node.Uuid]; }
        if (element == null) { return false; } 
        switch (element.Type)
        {
            case StoryItemType.StoryOverview:
                result = SearchStoryOverview(element, delete);
                break;
            case StoryItemType.Problem:
                result = SearchProblem(element, delete);
                break;
            case StoryItemType.Character:
                result = SearchCharacter(element, delete);
                break;
            case StoryItemType.Scene:
                result = SearchScene(element, delete);
                break;
        }
        return result;
    }

    /// <summary>
    /// Searches Cast members, viewpoint character, protagonist name, antagonist name and the name of the scene and the selected setting in a scene node
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    private bool SearchScene(StoryElement element, bool delete)
    {
        SceneModel scene = (SceneModel)element;

        List<string> newCast = new();
        
        foreach (string member in scene.CastMembers) //Searches character in scene
        {
            try
            {
                _elementCollection.StoryElementGuids.TryGetValue(Guid.Parse(member), out StoryElement model);
                model = model as CharacterModel;
                if (model.Uuid == _arg)
                {
                    if (!delete) { return true; }
                }
                else { newCast.Add(member); }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Warn, $"Error checking scene cast member ({ex.Message})");
            }

        }
        if (delete) { scene.CastMembers = newCast; }

        try
        {
            if (!string.IsNullOrEmpty(scene.Protagonist)) //Searches protagonist
            {
                _elementCollection.StoryElementGuids.TryGetValue(Guid.Parse(scene.Protagonist), out StoryElement protag);
                if (protag.Uuid == _arg)
                {
                    if (delete)
                    {
                        scene.Protagonist = null;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warn, $"Error checking scene protagonist ({ex.Message})");
        }


        try
        {
            if (!string.IsNullOrEmpty(scene.Antagonist)) //Searches Antagonist
            {
                _elementCollection.StoryElementGuids.TryGetValue(Guid.Parse(scene.Antagonist), out StoryElement antag);
                if (antag.Uuid == _arg)
                {
                    if (delete) { scene.Antagonist = null; }
                    else { return true; }
                }

            }
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warn, $"Error checking scene antagonist ({ex.Message})");
        }


        try
        {
            if (!string.IsNullOrEmpty(scene.Setting))
            {
                _elementCollection.StoryElementGuids.TryGetValue(Guid.Parse(scene.Setting), out StoryElement setting);
                if (setting.Uuid == _arg)
                {
                    if (delete) { scene.Setting = null; }
                    else { return true; }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warn, $"Error checking scene setting ({ex.Message})");
        }


        return false;
    }

    /// <summary>
    /// Searches the name of each character in a relationship and the name of the character
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    private bool SearchCharacter(StoryElement element, bool delete)
    {
        CharacterModel characterModel = (CharacterModel)element;

        List<RelationshipModel> newRelationships = new();
        foreach (RelationshipModel partner in characterModel.RelationshipList) //Checks each character in relationship
        {
            try
            {
                _elementCollection.StoryElementGuids.TryGetValue(Guid.Parse(partner.PartnerUuid), out StoryElement model);
                if (model.Uuid == _arg)
                {
                    if (!delete) { return true; }
                }
                else { newRelationships.Add(partner); }
            }
            catch (Exception ex) { _logger.Log(LogLevel.Warn,$"Error checking partner in relationship list {ex.Message}"); }

        }
        if (delete) { characterModel.RelationshipList = newRelationships; }
        return false;
    }

    /// <summary>
    /// Searches a problem for the element name, Antag name, protag name,
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    private bool SearchProblem(StoryElement element, bool delete)
    {
        ProblemModel problem = (ProblemModel)element;

        try
        {
            if (!string.IsNullOrEmpty(problem.Protagonist))//Checks protagonist's name
            {
                _elementCollection.StoryElementGuids.TryGetValue(Guid.Parse(problem.Protagonist), out StoryElement protag);
                if (protag.Uuid == _arg)
                {
                    if (delete) { problem.Protagonist = null; }
                    else { return true; }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warn, $"Failed to search scene for protagonist - ({problem.Protagonist}) got error {ex.Message}");
        }

        try
        {
            if (!string.IsNullOrEmpty(problem.Antagonist))//Checks antagonists name
            {
                _elementCollection.StoryElementGuids.TryGetValue(Guid.Parse(problem.Antagonist), out StoryElement antag);
                if (antag.Uuid == _arg)
                {
                    if (delete) { problem.Antagonist = null; }
                    else { return true; }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warn, $"Failed to search scene for antagonist - ({problem.Antagonist}) got error {ex.Message}");
        }


        return false;
    }

    /// <summary>
    /// Searches the overview node for the name and main story problem
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    private bool SearchStoryOverview(StoryElement element, bool delete)
    {
        try
        {
            OverviewModel overview = (OverviewModel)element;
            if (!string.IsNullOrEmpty(overview.StoryProblem))
            {
                _elementCollection.StoryElementGuids.TryGetValue(Guid.Parse(overview.StoryProblem), out StoryElement problem);
                if (problem.Uuid == _arg) 
                {
                    if (delete) { overview.StoryProblem = null; }
                    else { return true; }  
                } //Checks problem name
            }
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warn, $"Failed to search scene for storyoverview got error {ex.Message}");
        }

        return false;
    }
}