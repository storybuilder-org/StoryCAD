using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.Models;
using StoryBuilder.Services.Logging;
using StoryBuilder.ViewModels;
// ReSharper disable PossibleNullReferenceException
// ReSharper disable SwitchStatementMissingSomeEnumCasesNoDefault

namespace StoryBuilder.Services.Search;

public class SearchService
{
    private string _arg;
    private StoryElementCollection _elementCollection;
    /// <summary>
    /// Search a StoryElement for a given string search argument
    /// </summary>
    /// <param name="node">StoryNodeItem whose StoryElement to search</param>
    /// <param name="searchArg">string to search for</param>
    /// <param name="model">model to search in</param>
    /// <returns>true if StoryElement contains search argument</returns>
    public bool SearchStoryElement(StoryNodeItem node, string searchArg, StoryModel model)
    {
        if (searchArg == null)
        {
            Ioc.Default.GetRequiredService<ShellViewModel>().Logger.Log(LogLevel.Warn, "Search argument is null, returning false.");
            return false;
        } // Fixes blank search
        
        bool _Result = false;
        _arg = searchArg.ToLower();
        StoryElement _Element = null;
        _elementCollection = Ioc.Default.GetRequiredService<ShellViewModel>().StoryModel.StoryElements;

        if (model.StoryElements.StoryElementGuids.ContainsKey(node.Uuid)) { _Element = model.StoryElements.StoryElementGuids[node.Uuid]; }
        if (_Element == null) { return false; } 
        switch (_Element.Type)
        {
            case StoryItemType.StoryOverview:
                _Result = SearchStoryOverview(_Element);
                break;
            case StoryItemType.Problem:
                _Result = SearchProblem(_Element);
                break;
            case StoryItemType.Character:
                _Result = SearchCharacter(_Element);
                break;
            case StoryItemType.Setting:
                _Result = SearchSetting(_Element);
                break;
            case StoryItemType.Scene:
                _Result = SearchScene(_Element);
                break;
            case StoryItemType.Folder:
                _Result = SearchFolder(_Element);
                break;
            case StoryItemType.Section:
                _Result = SearchSection(_Element);
                break;
        }
        return _Result;
    }
    private bool Comparator(string text)
    {
        return text.ToLower().Contains(_arg);
    }

    private bool SearchSection(StoryElement element)
    {
        return Comparator(element.Name);
    }

    private bool SearchFolder(StoryElement element)
    {
        return Comparator(element.Name);
    }

    /// <summary>
    /// Searches Cast members, protagonist name, antagonist name and the name of the scene and the selected setting in a scene node
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    private bool SearchScene(StoryElement element)
    {
        SceneModel _Scene = (SceneModel)element;

        foreach (string _Member in _Scene.CastMembers) //Searches character in scene
        {
            _elementCollection.StoryElementGuids.TryGetValue(Guid.Parse(_Member), out StoryElement _Model);
            _Model = _Model as CharacterModel;
            if (Comparator(_Model.Name)) { return true; }
        }

        if (Comparator(element.Name)) { return true; }  //Searches node name

        if (!string.IsNullOrEmpty(_Scene.ViewpointCharacter)) //Searches VP characters
        {
            _elementCollection.StoryElementGuids.TryGetValue(Guid.Parse(_Scene.ViewpointCharacter), out StoryElement _VpChar);
            if (Comparator(_VpChar.Name)) { return true; }
        }
        if (!string.IsNullOrEmpty(_Scene.Protagonist)) //Searches protagonist
        {
            _elementCollection.StoryElementGuids.TryGetValue(Guid.Parse(_Scene.Protagonist), out StoryElement _Protag);
            if (Comparator(_Protag.Name)) { return true; }
        }
        if (!string.IsNullOrEmpty(_Scene.Antagonist)) //Searches Antagonist
        {
            _elementCollection.StoryElementGuids.TryGetValue(Guid.Parse(_Scene.Antagonist), out StoryElement _Antag);
            if (Comparator(_Antag.Name)) { return true; }
        }
        if (!string.IsNullOrEmpty(_Scene.Setting))
        {
            _elementCollection.StoryElementGuids.TryGetValue(Guid.Parse(_Scene.Setting), out StoryElement _Setting);
            if (Comparator(_Setting.Name)) { return true; }
        }
        return false; //No match, return false
    }

    private bool SearchSetting(StoryElement element)
    {
        return Comparator(element.Name);
    }

    /// <summary>
    /// Searches the name of each character in a relationship and the name of the character
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    private bool SearchCharacter(StoryElement element)
    {
        CharacterModel _CharacterModel = (CharacterModel)element;

        foreach (RelationshipModel _Partner in _CharacterModel.RelationshipList) //Checks each character in relationship
        {
            _elementCollection.StoryElementGuids.TryGetValue(Guid.Parse(_Partner.PartnerUuid), out StoryElement _Model);
            if (Comparator(_Model.Name)) { return true; }
        }

        return Comparator(element.Name); //Checks element name
    }

    /// <summary>
    /// Searches a problem for the element name, Antag name, protag name,
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    private bool SearchProblem(StoryElement element)
    {
        ProblemModel _Problem = (ProblemModel)element;

        if (!string.IsNullOrEmpty(_Problem.Protagonist))//Checks protagonists name
        {
            _elementCollection.StoryElementGuids.TryGetValue(Guid.Parse(_Problem.Protagonist), out StoryElement _Protag);
            if (Comparator(_Protag.Name)) { return true; } 

        }
        if (!string.IsNullOrEmpty(_Problem.Antagonist))//Checks antagonists name
        {
            _elementCollection.StoryElementGuids.TryGetValue(Guid.Parse(_Problem.Antagonist), out StoryElement _Antag);
            if (Comparator(_Antag.Name)) { return true; } 
        }

        return Comparator(element.Name); //Checks name of node
    }

    /// <summary>
    /// Searches the overview node for the name and main story problem
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    private bool SearchStoryOverview(StoryElement element)
    {
        OverviewModel _Overview = (OverviewModel)element;
        if (!string.IsNullOrEmpty(_Overview.StoryProblem))
        {
            _elementCollection.StoryElementGuids.TryGetValue(Guid.Parse(_Overview.StoryProblem), out StoryElement _Problem);
            if (Comparator(_Problem.Name)) { return true; } //Checks problem name
        }

        return Comparator(element.Name); //checks node name
    }
}