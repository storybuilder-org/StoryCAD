using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Windows.Storage;
using CommunityToolkit.Mvvm.DependencyInjection;
using NLog;
using StoryCAD.Models;
using StoryCAD.Services.Logging;
using StoryCAD.ViewModels;
using LogLevel = NLog.LogLevel;

namespace StoryCAD.DAL;

/// <summary>
/// StoryWriter parses StoryCAD's model and writes it to its backing store
/// (the .stbx file), which is an Xml Document. 
/// </summary>
public class StoryWriter
{
    //TODO: Move System.IO stuff to a DAL micro service?

    /// StoryCAD's model is found in the StoryCAD.Models namespace and consists
    /// of various Plain Old CLR objects.
    ///
    private StoryModel _model;

    /// The in-memory representation of the .stbx file is an XmlDocument
    /// and its various components are all XmlNodes.
    /// p
    private XmlDocument _xml;

    private StorageFile _outFile;

    /// Story contains a collection of StoryElements, a representation of the StoryExplorer tree,
    /// a representation of the StoryNarrator tree, and a collection of story-specific settings.
    private XmlNode _elements;    // The collection of StoryElements
    private XmlNode _explorer;    // StoryExplorer
    private XmlNode _narrator;    // StoryNarrator
    private XmlNode _stbSettings; // Settings

    private readonly LogService _logger = Ioc.Default.GetRequiredService<LogService>();

    public async Task WriteFile(StorageFile output, StoryModel model)
    {
        _model = model;
        _outFile = output;
        _xml = new XmlDocument();
        CreateStoryDocument();
        //write RTF if converting. 
        ParseStoryElementsAsync();
        ParseExplorerView();
        ParseNarratorView();

        await using (Stream _fileStream = await _outFile.OpenStreamForWriteAsync())
        {
            XmlWriterSettings _settings = new()
            {
                Async = true,
                Encoding = Encoding.UTF8,
                Indent = true,
                CheckCharacters = true
            };
            XmlWriter _writer = XmlWriter.Create(_fileStream, _settings);
            try
            {
                _xml.Save(_writer);
                await _writer.FlushAsync();
            }
            catch (Exception _ex) { _logger.LogException(Services.Logging.LogLevel.Error, _ex, "Error in write file"); }
        }

        model.Changed = false;
        model.ProjectFolder = await output.GetParentAsync();  // is this needed?
    }


    private void CreateStoryDocument()
    {
        XmlNode _docNode = _xml.CreateXmlDeclaration("1.0", "UTF-8", null);
        _xml.AppendChild(_docNode);
        // Create StoryCAD node with version
        XmlNode _stb = _xml.CreateElement("StoryCAD");
        //Create an attribute.
        XmlAttribute _attr = _xml.CreateAttribute("Version");
        _attr.Value = Ioc.Default.GetRequiredService<AppState>().Version; //Write Version data
        Debug.Assert(_stb.Attributes != null, "_stb.Attributes != null");
        _stb.Attributes.Append(_attr);
        _xml.AppendChild(_stb);
        _elements = _xml.CreateElement("StoryElements");
        _stb.AppendChild(_elements);
        _explorer = _xml.CreateElement("Explorer");
        _stb.AppendChild(_explorer);
        _narrator = _xml.CreateElement("Narrator");
        _stb.AppendChild(_narrator);
  
        _stbSettings = _xml.CreateElement("Settings");
        _stb.AppendChild(_stbSettings);
    }

    private void ParseStoryElementsAsync()
    {
        StoryElementCollection tempCollection = _model.StoryElements; //Prevents rare error of collection was modified.
        foreach (StoryElement _element in tempCollection)
        {
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (_element.Type)
            {
                case StoryItemType.StoryOverview:
                    ParseOverViewElement(_element);
                    break;
                case StoryItemType.Problem:
                    ParseProblemElement(_element);
                    break;
                case StoryItemType.Character:
                    ParseCharacterElement(_element);
                    break;
                case StoryItemType.Setting:
                    ParseSettingElement(_element);
                    break;
                case StoryItemType.Scene:
                    ParseSceneElement(_element);
                    break;
                case StoryItemType.Folder:
                    ParseFolderElement(_element);
                    break;
                case StoryItemType.Notes: //Notes are just folders with a different icon
                    ParseFolderElement(_element);
                    break;
                case StoryItemType.Section:
                    ParseFolderElement(_element);
                    break;
                case StoryItemType.TrashCan:
                    ParseTrashCanElement(_element);
                    break;
                case StoryItemType.Web:
                    ParseWebElement(_element);
                    break;
                default:
                    Ioc.Default.GetRequiredService<Logger>().Log(LogLevel.Warn, "Unknown Element found in StoryWriter: " + _element.Type);
                    break;
            }
        }
    }

    private void ParseWebElement(StoryElement element)
    {
        WebModel _rec = (WebModel)element;
        XmlNode _web = _xml.CreateElement("Web");
        Debug.Assert(_web != null, nameof(_web) + " != null");

        XmlAttribute _attr = _xml.CreateAttribute("UUID");
        _attr.Value = UuidString(_rec.Uuid);
        Debug.Assert(_web.Attributes != null, "_web.Attributes != null");
        _web.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Name");
        _attr.Value = _rec.Name;
        _web.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("URL");
        _attr.Value = _rec.URL.ToString();
        _web.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Timestamp");
        // ReSharper disable SpecifyACultureInStringConversionExplicitly
        _attr.Value = _rec.Timestamp.ToString("o");
        // ReSharper restore SpecifyACultureInStringConversionExplicitly
        _web.Attributes.Append(_attr);

        _elements.AppendChild(_web);
    }

    private void ParseOverViewElement(StoryElement element)
    {
        OverviewModel _rec = (OverviewModel)element;
        XmlNode _overview = _xml.CreateElement("Overview");

        XmlAttribute _attr = _xml.CreateAttribute("UUID");
        _attr.Value = UuidString(_rec.Uuid);
        Debug.Assert(_overview.Attributes != null, "_overview.Attributes != null");
        _overview.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Name");
        _attr.Value = _rec.Name;
        _overview.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("DateCreated");
        _attr.Value = _rec.DateCreated;
        _overview.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("DateModified");
        _attr.Value = _rec.DateModified;
        _overview.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Author");
        _attr.Value = _rec.Author;
        _overview.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("StoryType");
        _attr.Value = _rec.StoryType;
        _overview.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("StoryGenre");
        _attr.Value = _rec.StoryGenre;
        _overview.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("ViewPoint");
        _attr.Value = _rec.Viewpoint;
        _overview.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("ViewpointCharacter");
        _attr.Value = _rec.ViewpointCharacter;
        _overview.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Voice");
        _attr.Value = _rec.Voice;
        _overview.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("LiteraryDevice");
        _attr.Value = _rec.LiteraryDevice;
        _overview.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Tense");
        _attr.Value = _rec.Tense;
        _overview.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Style");
        _attr.Value = _rec.Style;
        _overview.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Tone");
        _attr.Value = _rec.Tone;
        _overview.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("StoryIdea");
        _attr.Value = _rec.StoryIdea;
        _overview.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Concept");
        _attr.Value = _rec.Concept;
        _overview.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("StoryProblem");
        _attr.Value = _rec.StoryProblem;
        _overview.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Premise");
        _attr.Value = _rec.Premise;
        _overview.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("StructureNotes");
        _attr.Value = _rec.StructureNotes;
        _overview.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Notes");
        _attr.Value = _rec.Notes;
        _overview.Attributes.Append(_attr);

        _elements.AppendChild(_overview);
    }

    private void ParseProblemElement(StoryElement element)
    {
        ProblemModel _rec = (ProblemModel)element;
        XmlNode _prob = _xml.CreateElement("Problem");

        XmlAttribute _attr = _xml.CreateAttribute("UUID");
        _attr.Value = UuidString(_rec.Uuid);
        Debug.Assert(_prob.Attributes != null, "_prob.Attributes != null");
        _prob.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Name");
        _attr.Value = _rec.Name;
        _prob.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("ProblemType");
        _attr.Value = _rec.ProblemType;
        _prob.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("ConflictType");
        _attr.Value = _rec.ConflictType;
        _prob.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("ProblemCategory");
        _attr.Value = _rec.ProblemCategory;
        _prob.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Subject");
        _attr.Value = _rec.Subject;
        _prob.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("ProblemSource");
        _attr.Value = _rec.ProblemSource;
        _prob.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Protagonist");
        _attr.Value = _rec.Protagonist;
        _prob.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("ProtGoal");
        _attr.Value = _rec.ProtGoal;
        _prob.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("ProtMotive");
        _attr.Value = _rec.ProtMotive;
        _prob.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("ProtConflict");
        _attr.Value = _rec.ProtConflict;
        _prob.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Antagonist");
        _attr.Value = _rec.Antagonist;
        _prob.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("AntagGoal");
        _attr.Value = _rec.AntagGoal;
        _prob.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("AntagMotive");
        _attr.Value = _rec.AntagMotive;
        _prob.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("AntagConflict");
        _attr.Value = _rec.AntagConflict;
        _prob.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Outcome");
        _attr.Value = _rec.Outcome;
        _prob.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Method");
        _attr.Value = _rec.Method;
        _prob.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Theme");
        _attr.Value = _rec.Theme;
        _prob.Attributes.Append(_attr);

        _attr = _xml.CreateAttribute("StoryQuestion");
        _attr.Value = _rec.StoryQuestion;
        _prob.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Premise");
        _attr.Value = _rec.Premise;
        _prob.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Notes");
        _attr.Value = _rec.Notes;
        _prob.Attributes.Append(_attr);

        _elements.AppendChild(_prob);
    }

    private void ParseCharacterElement(StoryElement element)
    {
        CharacterModel _rec = (CharacterModel)element;
        XmlNode _chr = _xml.CreateElement("Character");

        XmlAttribute _attr = _xml.CreateAttribute("UUID");
        _attr.Value = UuidString(_rec.Uuid);
        Debug.Assert(_chr.Attributes != null, "_chr.Attributes != null");
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Name");
        _attr.Value = _rec.Name;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Role");
        _attr.Value = _rec.Role;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("StoryRole");
        _attr.Value = _rec.StoryRole;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Archetype");
        _attr.Value = _rec.Archetype;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Age");
        _attr.Value = _rec.Age;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Sex");
        _attr.Value = _rec.Sex;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Eyes");
        _attr.Value = _rec.Eyes;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Hair");
        _attr.Value = _rec.Hair;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Weight");
        _attr.Value = _rec.Weight;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("CharHeight");
        _attr.Value = _rec.CharHeight;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Build");
        _attr.Value = _rec.Build;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Complexion");
        _attr.Value = _rec.Complexion;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Race");
        _attr.Value = _rec.Race;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Nationality");
        _attr.Value = _rec.Nationality;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Health");
        _attr.Value = _rec.Health;
        _chr.Attributes.Append(_attr);

        XmlNode _relationshipList = _xml.CreateElement("Relationships");
        foreach (RelationshipModel _relation in _rec.RelationshipList)
        {
            XmlElement _relNode = _xml.CreateElement("Relation");
            _attr = _xml.CreateAttribute("Partner");
            _attr.Value = _relation.PartnerUuid;
            _relNode.Attributes.Append(_attr);
            _attr = _xml.CreateAttribute("RelationType");
            _attr.Value = _relation.RelationType;
            _relNode.Attributes.Append(_attr);
            _attr = _xml.CreateAttribute("Trait");
            _attr.Value = _relation.Trait;
            _relNode.Attributes.Append(_attr);
            _attr = _xml.CreateAttribute("Attitude");
            _attr.Value = _relation.Attitude;
            _relNode.Attributes.Append(_attr);
            _attr = _xml.CreateAttribute("Notes");
            _attr.Value = _relation.Notes;
            _relNode.Attributes.Append(_attr);
            _relationshipList.AppendChild(_relNode);
        }
        _chr.AppendChild(_relationshipList);

        _attr = _xml.CreateAttribute("Enneagram");
        _attr.Value = _rec.Enneagram;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Intelligence");
        _attr.Value = _rec.Intelligence;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Values");
        _attr.Value = _rec.Values;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Abnormality");
        _attr.Value = _rec.Abnormality;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Focus");
        _attr.Value = _rec.Focus;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Adventureousness");
        _attr.Value = _rec.Adventureousness;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Aggression");
        _attr.Value = _rec.Aggression;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Confidence");
        _attr.Value = _rec.Confidence;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Conscientiousness");
        _attr.Value = _rec.Conscientiousness;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Creativity");
        _attr.Value = _rec.Creativity;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Dominance");
        _attr.Value = _rec.Dominance;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Enthusiasm");
        _attr.Value = _rec.Enthusiasm;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Assurance");
        _attr.Value = _rec.Assurance;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Sensitivity");
        _attr.Value = _rec.Sensitivity;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Shrewdness");
        _attr.Value = _rec.Shrewdness;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Sociability");
        _attr.Value = _rec.Sociability;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Stability");
        _attr.Value = _rec.Stability;
        _chr.Attributes.Append(_attr);
        XmlNode _traitList = _xml.CreateElement("CharacterTraits");
        foreach (string _member in _rec.TraitList)
        {
            XmlElement _trait = _xml.CreateElement("Trait");
            _trait.AppendChild(_xml.CreateTextNode(_member));
            _traitList.AppendChild(_trait);
        }
        _chr.AppendChild(_traitList);
        _attr = _xml.CreateAttribute("CharacterSketch");
        _attr.Value = _rec.CharacterSketch;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("PhysNotes");
        _attr.Value = _rec.PhysNotes;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Appearance");
        _attr.Value = _rec.Appearance;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Economic");
        _attr.Value = _rec.Economic;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Education");
        _attr.Value = _rec.Education;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Ethnic");
        _attr.Value = _rec.Ethnic;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Religion");
        _attr.Value = _rec.Religion;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("PsychNotes");
        _attr.Value = _rec.PsychNotes;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Notes");
        _attr.Value = _rec.Notes;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Flaw");
        _attr.Value = _rec.Flaw;
        _chr.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("BackStory");
        _attr.Value = _rec.BackStory;
        _chr.Attributes.Append(_attr);

        _elements.AppendChild(_chr);
    }

    private void ParseSettingElement(StoryElement element)
    {
        SettingModel _rec = (SettingModel)element;
        XmlNode _loc = _xml.CreateElement("Setting");

        XmlAttribute _attr = _xml.CreateAttribute("UUID");
        _attr.Value = UuidString(_rec.Uuid);
        Debug.Assert(_loc.Attributes != null, "_loc.Attributes != null");
        _loc.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Name");
        _attr.Value = _rec.Name;
        _loc.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Locale");
        _attr.Value = _rec.Locale;
        _loc.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Season");
        _attr.Value = _rec.Season;
        _loc.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Period");
        _attr.Value = _rec.Period;
        _loc.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Lighting");
        _attr.Value = _rec.Lighting;
        _loc.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Weather");
        _attr.Value = _rec.Weather;
        _loc.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Temperature");
        _attr.Value = _rec.Temperature;
        _loc.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Props");
        _attr.Value = _rec.Props;
        _loc.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Summary");
        _attr.Value = _rec.Summary;
        _loc.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Sights");
        _attr.Value = _rec.Sights;
        _loc.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Sounds");
        _attr.Value = _rec.Sounds;
        _loc.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Touch");
        _attr.Value = _rec.Touch;
        _loc.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("SmellTaste");
        _attr.Value = _rec.SmellTaste;
        _loc.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Notes");
        _attr.Value = _rec.Notes;
        _loc.Attributes.Append(_attr);

        _elements.AppendChild(_loc);
    }

    private void ParseSceneElement(StoryElement element)
    {
        SceneModel _rec = (SceneModel)element;
        XmlNode _scene = _xml.CreateElement("Scene");

        XmlAttribute _attr = _xml.CreateAttribute("UUID");
        _attr.Value = UuidString(_rec.Uuid);
        Debug.Assert(_scene.Attributes != null, "_scene.Attributes != null");
        _scene.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Name");
        _attr.Value = _rec.Name;
        _scene.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("ViewpointCharacter");
        _attr.Value = _rec.ViewpointCharacter;
        _scene.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Date");
        _attr.Value = _rec.Date;
        _scene.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Time");
        _attr.Value = _rec.Time;
        _scene.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Setting");
        _attr.Value = _rec.Setting;
        _scene.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("SceneType");
        _attr.Value = _rec.SceneType;
        _scene.Attributes.Append(_attr);
         XmlNode _castList = _xml.CreateElement("CastMembers");
        foreach (string _member in _rec.CastMembers)
        {
            XmlElement _castMember = _xml.CreateElement("Member");
            _castMember.AppendChild(_xml.CreateTextNode(_member));
            _castList.AppendChild(_castMember);
        }
        _scene.AppendChild(_castList);
        XmlNode _scenePurposeList = _xml.CreateElement("ScenePurpose");
        foreach (string _item in _rec.ScenePurpose)
        {
            XmlElement _purpose = _xml.CreateElement("Purpose");
            _purpose.AppendChild(_xml.CreateTextNode(_item));
            _scenePurposeList.AppendChild(_purpose);
        }
        _scene.AppendChild(_scenePurposeList);
        _attr = _xml.CreateAttribute("Remarks");
        _attr.Value = _rec.Remarks;
        _scene.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("ValueExchange");
        _attr.Value = _rec.ValueExchange;
        _scene.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Protagonist");
        _attr.Value = _rec.Protagonist;
        _scene.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("ProtagEmotion");
        _attr.Value = _rec.ProtagEmotion;
        _scene.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("ProtagGoal");
        _attr.Value = _rec.ProtagGoal;
        _scene.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Antagonist");
        _attr.Value = _rec.Antagonist;
        _scene.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("AntagEmotion");
        _attr.Value = _rec.AntagEmotion;
        _scene.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("AntagGoal");
        _attr.Value = _rec.AntagGoal;
        _scene.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Opposition");
        _attr.Value = _rec.Opposition;
        _scene.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Outcome");
        _attr.Value = _rec.Outcome;
        _scene.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Emotion");
        _attr.Value = _rec.Emotion;
        _scene.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("NewGoal");
        _attr.Value = _rec.NewGoal;
        _scene.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Remarks");
        _attr.Value = _rec.Remarks;
        _scene.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Events");
        _attr.Value = _rec.Events;
        _scene.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Consequences");
        _attr.Value = _rec.Consequences;
        _scene.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Significance");
        _attr.Value = _rec.Significance;
        _scene.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Realization");
        _attr.Value = _rec.Realization;
        _scene.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Review");
        _attr.Value = _rec.Review;
        _scene.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Notes");
        _attr.Value = _rec.Notes;
        _scene.Attributes.Append(_attr);

        _elements.AppendChild(_scene);
    }

    private void ParseNotesElement(StoryElement element)
    {
        FolderModel _rec = (FolderModel)element;
        XmlNode _node = _xml.CreateElement("Notes");

        XmlAttribute _attr = _xml.CreateAttribute("UUID");
        _attr.Value = UuidString(_rec.Uuid);
        Debug.Assert(_node.Attributes != null, "_node.Attributes != null");
        _node.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Name");
        _attr.Value = _rec.Name;
        _node.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Notes");
        _attr.Value = _rec.Notes;
        _node.Attributes.Append(_attr);

        _elements.AppendChild(_node);
    }
    private void ParseFolderElement(StoryElement element)
    {
        FolderModel _rec = (FolderModel)element;
        XmlNode _node = _xml.CreateElement("Folder");

        XmlAttribute _attr = _xml.CreateAttribute("UUID");
        _attr.Value = UuidString(_rec.Uuid);
        Debug.Assert(_node.Attributes != null, "_node.Attributes != null");
        _node.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Name");
        _attr.Value = _rec.Name;
        _node.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Notes");
        _attr.Value = _rec.Notes;
        _node.Attributes.Append(_attr);

        _elements.AppendChild(_node);
    }

    private void ParseTrashCanElement(StoryElement element)
    {
        TrashCanModel _rec = (TrashCanModel)element;
        XmlNode _node = _xml.CreateElement("TrashCan");

        XmlAttribute _attr = _xml.CreateAttribute("UUID");
        _attr.Value = UuidString(_rec.Uuid);
        Debug.Assert(_node.Attributes != null, "_node.Attributes != null");
        _node.Attributes.Append(_attr);
        _attr = _xml.CreateAttribute("Name");
        _attr.Value = _rec.Name;
        _node.Attributes.Append(_attr);

        _elements.AppendChild(_node);
    }

    private void ParseExplorerView()
    {
        foreach (StoryNodeItem _root in _model.ExplorerView)
        {
            _root.IsRoot = true;
            XmlElement _rootElement = RecurseCreateXmlElement(null, _root);
            _explorer.AppendChild(_rootElement);
        }
    }

    private void ParseNarratorView()
    {
        foreach (StoryNodeItem _root in _model.NarratorView)
        {
            _root.IsRoot = true;
            XmlElement _rootElement = RecurseCreateXmlElement(null, _root);
            _narrator.AppendChild(_rootElement);
        }
    }

    /// <summary>
    /// Create the TreeView's Xml equivalent of the StoryNodeModel
    /// via recursive descent. This is ran once for the ExplorerView
    /// view and again for the NarratorView view.
    /// 
    /// Since it's a TreeView node model, all the XML version of the
    /// node needs is to point to its StoryElement model and its
    /// children.
    /// </summary>
    /// <param name="parent">An XmlElement to add this under </param>
    /// <param name="node">A StoryNodeModel node</param>
    /// <returns>XnlElement version of the Treeview node</returns>
    public XmlElement RecurseCreateXmlElement(XmlElement parent, StoryNodeItem node)
    {
        XmlElement _element = _xml.CreateElement("StoryNode");
        // Set attributes
        _element.SetAttribute("UUID", UuidString(node.Uuid));
        _element.SetAttribute("Type", node.Type.ToString("g"));
        _element.SetAttribute("Name", node.Name);
        // Add MetaData 
        _element.SetAttribute("IsExpanded", node.IsExpanded.ToString());
        _element.SetAttribute("IsSelected", node.IsSelected.ToString());
        _element.SetAttribute("IsRoot", node.IsRoot.ToString());
        parent?.AppendChild(_element);
        // Traverse and create XmlNodes for the binderNode's child node subtree
        foreach (StoryNodeItem _child in node.Children)
        {
            RecurseCreateXmlElement(_element, _child);
        }
        return _element;
    }

    /// <summary>
    /// Generate a GUID string in the Scrivener format (i.e., without curly braces)
    /// </summary>
    /// <returns>string UUID representation</returns>
    public static string UuidString(Guid uuid)
    {
        string _id = uuid.ToString("B").ToUpper();
        _id = _id.Replace("{", string.Empty);
        _id = _id.Replace("}", string.Empty);
        return _id;
    }
}