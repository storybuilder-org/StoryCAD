﻿using Windows.Data.Xml.Dom;
using Windows.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using StoryCAD.Services.Messages;
using StoryCAD.ViewModels.SubViewModels;
using static Windows.Data.Xml.Dom.XmlDocument;
using StoryCAD.ViewModels.Tools;

namespace StoryCAD.DAL;

/// <summary>
/// StoryWriter parses StoryCAD's model and writes it to its backing store
/// (the .stbx file), which is an Xml Document. 
/// </summary>
public class LegacyXMLReader : ObservableRecipient
{


    /// StoryCAD's model is found in the StoryCAD.Models namespace and consists
    /// of various Plain Old CLR objects.
    ///
    private readonly ILogService _logger;

    /// The in-memory representation of the .stbx file is an XmlDocument
    /// and its various components are all XmlNodes.
    private static XmlDocument _xml;

    /// Story contains a collection of StoryElements, a representation of the StoryExplorer tree,
    /// a representation of the NarratorView tree, and a collection of story-specific settings.
    private StoryModel _model;

    // There's one OverviewModel per story; it's also the Treeview root
    private OverviewModel _overview;

    public async Task<StoryModel> ReadFile(StorageFile file)
    {
        try
        {
            string _msg = $"Reading file {file.Path}.";
            _logger.Log(LogLevel.Info, _msg);
            _xml = await LoadFromFileAsync(file);
            if (_xml == null || _xml.DocumentElement == null)
            {
	            // Handle error: the XML is invalid or empty.
	            throw new InvalidDataException("Error reading document");
            }

			LoadStoryModel();
            //Set Story Model File
            Ioc.Default.GetRequiredService<OutlineViewModel>().StoryModelFile = file.Path;
            
            // Early story outlines may have been built or converted
            // without a TrashCan node added. If this model is one of 
            // those, add the node to both the ExplorerView and NarratorView views.
            if (_model.ExplorerView.Count == 1)
            {
                TrashCanModel _trash = new(_model, null);
                StoryNodeItem _trashNode = new(_trash, null);
                _model.ExplorerView.Add(_trashNode);     // The trashcan is the second root
            }
            if (_model.NarratorView.Count == 1)
            {
                //TrashCanModel trash = new(_model);
                //StoryNodeItem trashNode = new(trash, null);
                //_model.NarratorView.Add(trashNode);     // The trashcan is the second root
            }

            Messenger.Send(new StatusChangedMessage(new("File load successful.", LogLevel.Info, true)));

            return _model;
        }
        catch (Exception _ex)
        {
            _logger.LogException(LogLevel.Error, _ex, "Error reading story");
            Messenger.Send(new StatusChangedMessage(new("Error reading story", LogLevel.Error)));
            return new StoryModel();  // return an empty story model
        }
    }

    private void LoadStoryModel()
    {
        _model = new StoryModel();

        XmlElement _root = _xml.DocumentElement;

        // This is the node we are looking for in the XML string
        XmlNodeList _nodes = _root.ChildNodes;

        if (_nodes != null)
        {
            foreach (IXmlNode _xn in _nodes)
            {
                switch (_xn.NodeName)
                {
                    case "StoryCAD":
                        break;
                    case "StoryElements":
                        ParseStoryElements(_xn);
                        break;
                    case "Explorer":
                        ParseStoryExplorer(_xn);
                        break;
                    case "Narrator":
                        ParseStoryNarrator(_xn);
                        break;
                    case "Settings":  // story settings
                        break;
                }
            }
        }
    }

    private void ParseStoryElements(IXmlNode node)
    {
        foreach (IXmlNode _child in node.ChildNodes)
            RecurseStoryElement(_child);
    }

    private void RecurseStoryElement(IXmlNode node)
    {
        if (node.NodeType == NodeType.ElementNode)
        {
            switch (node.NodeName)
            {
                case "Overview":
                    ParseOverView(node);
                    break;
                case "Problem":
                    ParseProblem(node);
                    break;
                case "Character":
                    ParseCharacter(node);
                    break;
                case "Setting":
                    ParseSetting(node);
                    break;
                case "PlotPoint":
                    ParseScene(node);   // legacy; PlotPoint was renamed to Scene
                    break;
                case "Scene":
                    ParseScene(node);
                    break;
                case "Separator":       // legacy; Separator was renamed to Folder
                    ParseFolder(node);
                    break;
                case "Folder":
                    ParseFolder(node);
                    break;
                case "Notes": //Notes are folders with different icons.
                    ParseFolder(node);
                    break;
                case "Web":
                    ParseWeb(node);
                    break;
                case "Section":
                    ParseFolder(node);
                    break;
                case "TrashCan":
                    ParseTrashCan(node);
                    break;
            }
            foreach (IXmlNode _child in node.ChildNodes)
                RecurseStoryElement(_child);
        }
    }

    private void ParseWeb(IXmlNode xn)
    {
        WebModel _web = new(xn, _model);
        foreach (IXmlNode 
                     
                     _attr in xn.Attributes)
        {
            switch (_attr.NodeName)
            {
                case "UUID":
                    break;
                case "Name":
                    _web.Name = _attr.InnerText;
                    break;
                case "Timestamp":
                    _web.Timestamp = Convert.ToDateTime(_attr.InnerText);
                    break;
                case "URL":
                    _web.URL = new Uri(_attr.InnerText);
                    break;
            }
        }
    }

    private void ParseOverView(IXmlNode xn)
    {
        // There's one OverviewModel per story. Its corresponding
        //StoryNode is the root of the ExplorerView TreeView.
        _overview = new OverviewModel(xn, _model);
        foreach (IXmlNode _attr in xn.Attributes)
        {
            switch (_attr.NodeName)
            {
                case "UUID":
                    break;
                case "Name":
                    _overview.Name = _attr.InnerText;
                    break;
                case "DateCreated":
                    _overview.DateCreated = _attr.InnerText;
                    break;
                case "DateModified":
                    _overview.DateModified = _attr.InnerText;
                    break;
                case "Author":
                    _overview.Author = _attr.InnerText;
                    break;
                case "StoryType":
                    _overview.StoryType = _attr.InnerText;
                    break;
                case "StoryGenre":
                    _overview.StoryGenre = _attr.InnerText;
                    break;
                case "ViewPoint":
                    _overview.Viewpoint = _attr.InnerText;
                    break;
                case "LiteraryDevice":
                    _overview.LiteraryDevice = _attr.InnerText;
                    break;
                case "ViewpointCharacter":
                    _overview.ViewpointCharacter = Guid.TryParse(_attr.InnerText, out Guid viewpointCharacter) 
                        ? viewpointCharacter 
                        : Guid.Empty;
                    break;
                case "Voice":
                    _overview.Voice = _attr.InnerText;
                    break;
                case "Style":
                    _overview.Style = _attr.InnerText;
                    break;
                case "Tense":
                    _overview.Tense = _attr.InnerText;
                    break;
                case "Tone":
                    _overview.Tone = _attr.InnerText;
                    break;
                case "StoryIdea":
                    _overview.StoryIdea = _attr.InnerText;
                    break;
                case "Concept":
                    _overview.Concept = _attr.InnerText;
                    break;
                case "StoryProblem":
                    _overview.StoryProblem = Guid.TryParse(_attr.InnerText, out Guid storyProblemGuid) 
                        ? storyProblemGuid 
                        : Guid.Empty;
                    break;
                case "Premise":
                    _overview.Premise = _attr.InnerText;
                    break;
                case "StructureNotes":
                    _overview.StructureNotes = _attr.InnerText;
                    break;
                case "Notes":
                    _overview.Notes = _attr.InnerText;
                    break;
            }
        }
    }

    private void ParseProblem(IXmlNode xn)
    {
        ProblemModel _prb = new(xn, _model);
        foreach (IXmlNode _attr in xn.Attributes)
        {
            switch (_attr.NodeName)
            {
                case "UUID":
                    break;
                case "Name":
                    _prb.Name = _attr.InnerText;
                    break;
                case "ProblemType":
                    _prb.ProblemType = _attr.InnerText;
                    break;
                case "ConflictType":
                    _prb.ConflictType = _attr.InnerText;
                    break;
                case "ProblemCategory":
                    _prb.ProblemCategory = _attr.InnerText;
                    break;
                case "Subject":
                    _prb.Subject = _attr.InnerText;
                    break;
                case "ProblemSource":
                    _prb.ProblemSource = _attr.InnerText;
                    break;
                case "Protagonist":
                    _prb.Protagonist = Guid.TryParse(_attr.InnerText, out Guid protagonist) 
                        ? protagonist
                        : Guid.Empty;
                    break;
                case "ProtGoal":
                    _prb.ProtGoal = _attr.InnerText;
                    break;
                case "ProtMotive":
                    _prb.ProtMotive = _attr.InnerText;
                    break;
                case "ProtConflict":
                    _prb.ProtConflict = _attr.InnerText;
                    break;
                case "Antagonist":
                    _prb.Antagonist = Guid.TryParse(_attr.InnerText, out Guid antagonist) 
                        ? antagonist
                        : Guid.Empty;
                    break;
                case "AntagGoal":
                    _prb.AntagGoal = _attr.InnerText;
                    break;
                case "AntagMotive":
                    _prb.AntagMotive = _attr.InnerText;
                    break;
                case "AntagConflict":
                    _prb.AntagConflict = _attr.InnerText;
                    break;
                case "Outcome":
                    _prb.Outcome = _attr.InnerText;
                    break;
                case "Method":
                    _prb.Method = _attr.InnerText;
                    break;
                case "Theme":
                    _prb.Theme = _attr.InnerText;
                    break;
                case "StoryQuestion":
                    _prb.StoryQuestion = _attr.InnerText;
                    break;
                case "Premise":
                    _prb.Premise = _attr.InnerText;
                    break;
                case "Notes":
                    _prb.Notes = _attr.InnerText;
                    break;
				case "StructureModelTitle":
					_prb.StructureTitle = _attr.InnerText;
					break;
				case "StructureDescription":
					_prb.StructureDescription = _attr.InnerText;
					break;
				case "BoundStructure":
					_prb.BoundStructure = _attr.InnerText;
					break;
			}
		}

        XmlNodeList Beats = xn.SelectNodes("./StructureBeats/Beat");
        if (Beats != null)
        {
	        foreach (IXmlNode beatNode in Beats)
	        {
		        StructureBeatViewModel beat = new StructureBeatViewModel();

		        IXmlNode titleNode = beatNode.SelectSingleNode("./Title");
		        if (titleNode != null)
			        beat.Title = titleNode.InnerText;

		        IXmlNode descriptionNode = beatNode.SelectSingleNode("./Description");
		        if (descriptionNode != null)
			        beat.Description = descriptionNode.InnerText;

		        IXmlNode guidNode = beatNode.SelectSingleNode("./Guid");
		        if (guidNode != null)
                    beat.Guid = Guid.TryParse(guidNode.InnerText, out Guid guid) ? guid : Guid.Empty;

		        _prb.StructureBeats.Add(beat);
			}
        }
	}

    private void ParseCharacter(IXmlNode xn)
    {
        CharacterModel _chr = new(xn, _model);
        IXmlNode _traitList = xn.SelectSingleNode("./CharacterTraits");
        if (_traitList != null)
        {
            foreach (IXmlNode _child in _traitList.ChildNodes)
                if (_child.NodeName.Equals("Trait"))
                    _chr.TraitList.Add(_child.InnerText);
        }
        IXmlNode _relationList = xn.SelectSingleNode("./Relationships");
        if (_relationList != null)
        {
            foreach (IXmlNode _child in _relationList.ChildNodes)
                if (_child.NodeName.Equals("Relation"))
                    _chr.RelationshipList.Add(new RelationshipModel(_child));
        }
        foreach (IXmlNode _attr in xn.Attributes)
        {
            switch (_attr.NodeName)
            {
                case "UUID":
                    break;
                case "Name":
                    _chr.Name = _attr.InnerText;
                    break;
                case "Role":
                    _chr.Role = _attr.InnerText;
                    break;
                case "StoryRole":
                    _chr.StoryRole = _attr.InnerText;
                    break;
                case "Archetype":
                    _chr.Archetype = _attr.InnerText;
                    break;
                case "Age":
                    _chr.Age = _attr.InnerText;
                    break;
                case "Sex":
                    _chr.Sex = _attr.InnerText;
                    break;
                case "Eyes":
                    _chr.Eyes = _attr.InnerText;
                    break;
                case "Hair":
                    _chr.Hair = _attr.InnerText;
                    break;
                case "Weight":
                    _chr.Weight = _attr.InnerText;
                    break;
                case "CharHeight":
                    _chr.CharHeight = _attr.InnerText;
                    break;
                case "Build":
                    _chr.Build = _attr.InnerText;
                    break;
                case "Complexion":
                    _chr.Complexion = _attr.InnerText;
                    break;
                case "Race":
                    _chr.Race = _attr.InnerText;
                    break;
                case "Nationality":
                    _chr.Nationality = _attr.InnerText;
                    break;
                case "Health":
                    _chr.Health = _attr.InnerText;
                    break;
                case "Enneagram":
                    _chr.Enneagram = _attr.InnerText;
                    break;
                case "Intelligence":
                    _chr.Intelligence = _attr.InnerText;
                    break;
                case "Values":
                    _chr.Values = _attr.InnerText;
                    break;
                case "Abnormality":
                    _chr.Abnormality = _attr.InnerText;
                    break;
                case "Focus":
                    _chr.Focus = _attr.InnerText;
                    break;
                case "Adventureousness": //TODO: fix spelling issue fully.
                    _chr.Adventureousness = _attr.InnerText;
                    break;
                case "Aggression":
                    _chr.Aggression = _attr.InnerText;
                    break;
                case "Confidence":
                    _chr.Confidence = _attr.InnerText;
                    break;
                case "Conscientiousness":
                    _chr.Conscientiousness = _attr.InnerText;
                    break;
                case "Creativity":
                    _chr.Creativity = _attr.InnerText;
                    break;
                case "Dominance":
                    _chr.Dominance = _attr.InnerText;
                    break;
                case "Enthusiasm":
                    _chr.Enthusiasm = _attr.InnerText;
                    break;
                case "Assurance":
                    _chr.Assurance = _attr.InnerText;
                    break;
                case "Sensitivity":
                    _chr.Sensitivity = _attr.InnerText;
                    break;
                case "Shrewdness":
                    _chr.Shrewdness = _attr.InnerText;
                    break;
                case "Sociability":
                    _chr.Sociability = _attr.InnerText;
                    break;
                case "Stability":
                    _chr.Stability = _attr.InnerText;
                    break;
                case "CharacterSketch":
                    _chr.CharacterSketch = _attr.InnerText;
                    break;
                case "PhysNotes":
                    _chr.PhysNotes = _attr.InnerText;
                    break;
                case "Appearance":
                    _chr.Appearance = _attr.InnerText;
                    break;
                case "Economic":
                    _chr.Economic = _attr.InnerText;
                    break;
                case "Education":
                    _chr.Education = _attr.InnerText;
                    break;
                case "Ethnic":
                    _chr.Ethnic = _attr.InnerText;
                    break;
                case "Religion":
                    _chr.Religion = _attr.InnerText;
                    break;
                case "PsychNotes":
                    _chr.PsychNotes = _attr.InnerText;
                    break;
                case "Notes":
                    _chr.Notes = _attr.InnerText;
                    break;
                case "Flaw":
                    _chr.Flaw = _attr.InnerText;
                    break;
                case "BackStory":
                    _chr.BackStory = _attr.InnerText;
                    break;
            }
        }
    }

    private void ParseSetting(IXmlNode xn)
    {
        SettingModel _loc = new(xn, _model);
        foreach (IXmlNode _attr in xn.Attributes)
        {
            switch (_attr.NodeName)
            {
                case "UUID":
                    break;
                case "Name":
                    _loc.Name = _attr.InnerText;
                    break;
                case "Locale":
                    _loc.Locale = _attr.InnerText;
                    break;
                case "Season":
                    _loc.Season = _attr.InnerText;
                    break;
                case "Period":
                    _loc.Period = _attr.InnerText;
                    break;
                case "Lighting":
                    _loc.Lighting = _attr.InnerText;
                    break;
                case "Weather":
                    _loc.Weather = _attr.InnerText;
                    break;
                case "Temperature":
                    _loc.Temperature = _attr.InnerText;
                    break;
                case "Props":
                    _loc.Props = _attr.InnerText;
                    break;
                case "Summary":
                    _loc.Summary = _attr.InnerText;
                    break;
                case "Sights":
                    _loc.Sights = _attr.InnerText;
                    break;
                case "Sounds":
                    _loc.Sounds = _attr.InnerText;
                    break;
                case "Touch":
                    _loc.Touch = _attr.InnerText;
                    break;
                case "SmellTaste":
                    _loc.SmellTaste = _attr.InnerText;
                    break;
                case "Notes":
                    _loc.Notes = _attr.InnerText;
                    break;
            }
        }

    }

    private void ParseScene(IXmlNode xn)
    {
        SceneModel _scene = new(xn, _model);
        IXmlNode castMembers = xn.SelectSingleNode("./CastMembers");
        if (castMembers != null)
            foreach (IXmlNode child in castMembers.ChildNodes)
                if (child.NodeName.Equals("Member"))
                {
                    if (Guid.TryParse(child.InnerText, out Guid memberGuid))
                    {
                        _scene.CastMembers.Add(memberGuid);
                    }
                    else
                    {
                        _logger.Log(LogLevel.Warn, $"Invalid GUID '{child.InnerText}' for cast member in scene '{_scene.Name}'.");
                    }
                }
;
        IXmlNode scenePurpose = xn.SelectSingleNode("./ScenePurpose");
        if (scenePurpose != null)
            foreach (IXmlNode child in scenePurpose.ChildNodes)
                if (child.NodeName.Equals("Purpose"))
                    _scene.ScenePurpose.Add(child.InnerText);

        foreach (IXmlNode attr in xn.Attributes)
        {
            switch (attr.NodeName)
            {
                case "UUID":
                    break;
                case "Name":
                    _scene.Name = attr.InnerText;
                    break;
                case "ViewpointCharacter":
                    _scene.ViewpointCharacter = Guid.TryParse(attr.InnerText, out Guid viewpointCharacter) 
                        ? viewpointCharacter
                        : Guid.Empty;
                    break;
                case "Date":
                    _scene.Date = attr.InnerText;
                    break;
                case "Time":
                    _scene.Time = attr.InnerText;
                    break;
                case "Setting":
                    _scene.Setting = Guid.TryParse(attr.InnerText, out Guid setting) 
                        ? setting
                        : Guid.Empty;
                    break;
                case "SceneType":
                    _scene.SceneType = attr.InnerText;
                    break;
                case "Protagonist":
                    _scene.Protagonist = Guid.TryParse(attr.InnerText, out Guid protagonist) 
                        ? protagonist
                        : Guid.Empty;
                    break;
                case "ProtagEmotion":
                    _scene.ProtagEmotion = attr.InnerText;
                    break;
                case "ProtagGoal":
                    _scene.ProtagGoal = attr.InnerText;
                    break;
                case "Antagonist":
                    _scene.Antagonist = Guid.TryParse(attr.InnerText, out Guid antagonist) 
                        ? antagonist 
                        : Guid.Empty;
                    break;
                case "AntagEmotion":
                    _scene.AntagEmotion = attr.InnerText;
                    break;
                case "AntagGoal":
                    _scene.AntagGoal = attr.InnerText;
                    break;
                case "Opposition":
                    _scene.Opposition = attr.InnerText;
                    break;
                case "Outcome":
                    _scene.Outcome = attr.InnerText;
                    break;
                case "Emotion":
                    _scene.Emotion = attr.InnerText;
                    break;
                case "NewGoal":
                    _scene.NewGoal = attr.InnerText;
                    break;
                case "ValueExchange":
                    _scene.ValueExchange = attr.InnerText;
                    break;
                case "Remarks":
                    _scene.Remarks = attr.InnerText;
                    break;
                case "Events":
                    _scene.Events = attr.InnerText;
                    break;
                case "Consequences":
                    _scene.Consequences = attr.InnerText;
                    break;
                case "Significance":
                    _scene.Significance = attr.InnerText;
                    break;
                case "Realization":
                    _scene.Realization = attr.InnerText;
                    break;
                case "Review":
                    _scene.Review = attr.InnerText;
                    break;
                case "Notes":
                    _scene.Notes = attr.InnerText;
                    break;
            }
        }

    }
    private void ParseNotes(IXmlNode xn)
    {
        FolderModel _notes = new(xn, _model);
        foreach (IXmlNode _attr in xn.Attributes)
        {
            switch (_attr.NodeName)
            {
                case "UUID":
                    break;
                case "Name":
                    _notes.Name = _attr.InnerText;
                    break;
                case "Notes":
                    _notes.Notes = _attr.InnerText;
                    break;
            }
        }
    }

    private void ParseFolder(IXmlNode xn)
    {
        FolderModel _folder = new(xn, _model);
        foreach (IXmlNode _attr in xn.Attributes)
        {
            switch (_attr.NodeName)
            {
                case "UUID":
                    break;
                case "Name":
                    _folder.Name = _attr.InnerText;
                    break;
                case "Notes":
                    _folder.Notes = _attr.InnerText;
                    break;
            }
        }
    }

    private void ParseTrashCan(IXmlNode xn)
    {
        TrashCanModel _trash = new(xn, _model);
        foreach (IXmlNode _attr in xn.Attributes)
        {
            switch (_attr.NodeName)
            {
                case "UUID":
                    break;
                case "Name":
                    _trash.Name = _attr.InnerText;
                    break;
            }
        }
    }

    private void ParseStoryExplorer(IXmlNodeSelector explorerNode)
    {
        foreach (IXmlNode _child in explorerNode.SelectNodes("StoryNode"))
            RecurseExplorerNode(null, _child, true);
    }

    private void RecurseExplorerNode(StoryNodeItem parent, IXmlNode xn, bool root)
    {
        StoryNodeItem _node = new(parent, xn);
        if (root) _model.ExplorerView.Add(_node);

        XmlNodeList _children = xn.SelectNodes("StoryNode");
        foreach (IXmlNode _child in _children)
            RecurseExplorerNode(_node, _child, false);
    }

    private void ParseStoryNarrator(IXmlNodeSelector narratorNode)
    {
        foreach (IXmlNode _child in narratorNode.SelectNodes("StoryNode"))
            RecurseNarratorNode(null, _child, true);
    }

    private void RecurseNarratorNode(StoryNodeItem parent, IXmlNode xn, bool root)
    {
        StoryNodeItem _node = new(parent, xn);
        if (_node.Name == "Deleted Story Elements" && root) {return;}
        if (root) _model.NarratorView.Add(_node);

        XmlNodeList _children = xn.SelectNodes("StoryNode");
        foreach (IXmlNode _child in _children)
            RecurseNarratorNode(_node, _child, false);

    }

    #region Constructor
    public LegacyXMLReader(ILogService logger)
    {
        _logger = logger;
        //Logger = Ioc.Default.GetService<LogService>();

        XmlDocument _doc = new();
        _xml = _doc;
        //Model.Changed = false;
    }

    #endregion
}