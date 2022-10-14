using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.Models;
using StoryBuilder.ViewModels;

namespace StoryBuilder.Services.Search;

/// <summary>
/// This is a copy of the search service however 
/// It does not search the name of the node and 
/// each search has an optional argument to delete references.
/// </summary>
public class DeletionService
{
    private Guid _arg;
    private StoryElementCollection _elementCollection;
    /// <summary>
    /// Search a StoryElement for a given string search argument
    /// </summary>
    /// <param name="node">StoryNodeItem whose StoryElement to search</param>
    /// <param name="searchArg">string to search for</param>
    /// <param name="model">model to search in</param>
    /// <param name="delete">Should this node be deleted</param>
    /// <returns>true if StoryElement contains search argument</returns>
    public bool SearchStoryElement(StoryNodeItem node, Guid searchArg, StoryModel model, bool delete = false)
    {
        bool _Result = false;
        _arg = searchArg;
        StoryElement _Element = null;
        _elementCollection = Ioc.Default.GetRequiredService<ShellViewModel>().StoryModel.StoryElements;

        if (model.StoryElements.StoryElementGuids.ContainsKey(node.Uuid)) { _Element = model.StoryElements.StoryElementGuids[node.Uuid]; }
        if (_Element == null) { return false; } 
        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (_Element.Type)
        {
            case StoryItemType.StoryOverview:
                _Result = SearchStoryOverview(_Element, delete);
                break;
            case StoryItemType.Problem:
                _Result = SearchProblem(_Element, delete);
                break;
            case StoryItemType.Character:
                _Result = SearchCharacter(_Element, delete);
                break;
            case StoryItemType.Scene:
                _Result = SearchScene(_Element, delete);
                break;
        }
        return _Result;
    }

    /// <summary>
    /// Searches Cast members, viewpoint character, protagonist name, antagonist name and the name of the scene and the selected setting in a scene node
    /// </summary>
    /// <param name="element"></param>
    /// <param name="delete">Should this node be deleted</param>
    /// <returns></returns>
    private bool SearchScene(StoryElement element, bool delete)
    {
        SceneModel _Scene = (SceneModel)element;

        List<string> _NewCast = new();
        foreach (string _Member in _Scene.CastMembers) //Searches character in scene
        {
            _elementCollection.StoryElementGuids.TryGetValue(Guid.Parse(_Member), out StoryElement _Model);
            _Model = _Model as CharacterModel;
            if (_Model!.Uuid == _arg) 
            {
                if (!delete) { return true; }
            }
            else { _NewCast.Add(_Member); }
        }
        if (delete) { _Scene.CastMembers = _NewCast; }

        if (!string.IsNullOrEmpty(_Scene.ViewpointCharacter)) //Searches protagonist
        {
            _elementCollection.StoryElementGuids.TryGetValue(Guid.Parse(_Scene.ViewpointCharacter), out StoryElement _VpChar);
            if (_VpChar!.Uuid == _arg)
            {
                if (delete) { _Scene.ViewpointCharacter = null; }
                else { return true; }
            }
        }

        if (!string.IsNullOrEmpty(_Scene.Protagonist)) //Searches protagonist
        {
            _elementCollection.StoryElementGuids.TryGetValue(Guid.Parse(_Scene.Protagonist), out StoryElement _Protag);
            if (_Protag!.Uuid == _arg) 
            {
                if (delete) { _Scene.Protagonist = null; }
                else { return true; }
            }
        }
        
        if (!string.IsNullOrEmpty(_Scene.Antagonist)) //Searches Antagonist
        {
            _elementCollection.StoryElementGuids.TryGetValue(Guid.Parse(_Scene.Antagonist), out StoryElement _Antag);
            if (_Antag!.Uuid == _arg) 
            {
                if (delete) { _Scene.Antagonist = null; }
                else { return true; } 
            }
        
        }

        if (!string.IsNullOrEmpty(_Scene.Setting))
        {
            _elementCollection.StoryElementGuids.TryGetValue(Guid.Parse(_Scene.Setting), out StoryElement _Setting);
            if (_Setting!.Uuid == _arg) 
            {
                if (delete) { _Scene.Setting = null; }
                else { return true; }
            }
        }

        return false;
    }

    /// <summary>
    /// Searches the name of each character in a relationship and the name of the character
    /// </summary>
    /// <param name="element"></param>
    /// <param name="delete">Should this node be deleted</param>
    /// <returns></returns>
    private bool SearchCharacter(StoryElement element, bool delete)
    {
        CharacterModel _CharacterModel = (CharacterModel)element;

        List<RelationshipModel> _NewRelationships = new();
        foreach (RelationshipModel _Partner in _CharacterModel.RelationshipList) //Checks each character in relationship
        {
            _elementCollection.StoryElementGuids.TryGetValue(Guid.Parse(_Partner.PartnerUuid), out StoryElement _Model);
            if (_Model!.Uuid == _arg) 
            {
                if (!delete) { return true; }
            }
            else { _NewRelationships.Add(_Partner); }
        }
        if (delete) { _CharacterModel.RelationshipList = _NewRelationships; }
        return false;
    }

    /// <summary>
    /// Searches a problem for the element name, Antag name, protag name,
    /// </summary>
    /// <param name="element"></param>
    /// <param name="delete">Should this node be deleted</param>
    /// <returns></returns>
    private bool SearchProblem(StoryElement element, bool delete)
    {
        ProblemModel _Problem = (ProblemModel)element;

        if (!string.IsNullOrEmpty(_Problem.Protagonist))//Checks protagonists name
        {
            _elementCollection.StoryElementGuids.TryGetValue(Guid.Parse(_Problem.Protagonist), out StoryElement _Protag);
            if (_Protag!.Uuid == _arg) 
            {
                if (delete) { _Problem.Protagonist = null; }
                else { return true; }
            } 

        }

        if (!string.IsNullOrEmpty(_Problem.Antagonist))//Checks antagonists name
        {
            _elementCollection.StoryElementGuids.TryGetValue(Guid.Parse(_Problem.Antagonist), out StoryElement _Antag);
            if (_Antag!.Uuid == _arg) 
            {
                if (delete) { _Problem.Antagonist = null; }
                else { return true;}
            } 
        }

        return false;
    }

    /// <summary>
    /// Searches the overview node for the name and main story problem
    /// </summary>
    /// <param name="element"></param>
    /// <param name="delete">Should this node be deleted</param>
    /// <returns></returns>
    private bool SearchStoryOverview(StoryElement element, bool delete)
    {
        OverviewModel _Overview = (OverviewModel)element;
        if (!string.IsNullOrEmpty(_Overview.StoryProblem))
        {
            _elementCollection.StoryElementGuids.TryGetValue(Guid.Parse(_Overview.StoryProblem), out StoryElement _Problem);
            if (_Problem!.Uuid == _arg) 
            {
                if (delete) { _Overview.StoryProblem = null; }
                else { return true; }  
            } //Checks problem name
        }
        return false;
    }
}