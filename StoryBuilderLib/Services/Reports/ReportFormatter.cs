using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using CommunityToolkit.Mvvm.DependencyInjection;
using NRtfTree.Util;
using StoryBuilder.DAL;
using StoryBuilder.Models;
using StoryBuilder.Services.Logging;
using StoryBuilder.ViewModels;

namespace StoryBuilder.Services.Reports;

public class ReportFormatter
{
    private Dictionary<string, string[]> _templates = new();
    private StoryReader _rdr;
    private StoryModel _model;

    #region Public methods

    public string FormatStoryOverviewReport(StoryElement element)
    {
        OverviewModel _Overview = (OverviewModel)element;
        string[] _Lines = _templates["Story Overview"];  
        RtfDocument _Doc = new(string.Empty);

        StoryElement _VpChar = StringToStoryElement(_Overview.ViewpointCharacter);
        string _VpName = _VpChar?.Name ?? string.Empty;
        StoryElement _SeProblem = StringToStoryElement(_Overview.StoryProblem);
        string _ProblemName = _SeProblem?.Name ?? string.Empty;
        ProblemModel _Problem = (ProblemModel)_SeProblem;
        string _Premise = _Problem?.Premise ?? string.Empty;

        // Parse and write the report
        foreach (string _Line in _Lines)
        {
            // Parse the report
            StringBuilder _Sb = new(_Line);
            _Sb.Replace("@Title", _Overview.Name);
            _Sb.Replace("@CreateDate", _Overview.DateCreated);
            _Sb.Replace("@ModifiedDate", _Overview.DateModified);
            _Sb.Replace("@Author", _Overview.Author);
            _Sb.Replace("@StoryType", _Overview.StoryType);
            _Sb.Replace("@Genre", _Overview.StoryGenre);
            _Sb.Replace("@Viewpoint", _Overview.Viewpoint);
            _Sb.Replace("@StoryIdea", GetText(_Overview.StoryIdea));
            _Sb.Replace("@Concept", GetText(_Overview.Concept));
            _Sb.Replace("@StoryProblem", _ProblemName);
            _Sb.Replace("@Premise", GetText(_Premise));
            _Sb.Replace("@StoryType", _Overview.StoryType);
            _Sb.Replace("@StoryGenre", _Overview.StoryGenre);
            _Sb.Replace("@LiteraryDevice", _Overview.LiteraryDevice);
            _Sb.Replace("@viewpointCharacter", _VpName);
            _Sb.Replace("@Voice", _Overview.Voice);
            _Sb.Replace("@Tense", _Overview.Tense);
            _Sb.Replace("@Style", _Overview.Style);
            _Sb.Replace("@StructureNotes", GetText(_Overview.StructureNotes));
            _Sb.Replace("@Tone", _Overview.Tone);
            _Sb.Replace("@Notes", GetText(_Overview.Notes));
            _Doc.AddText(_Sb.ToString());
            _Doc.AddNewLine();
        }
        return _Doc.GetRtf();
    }

    public string FormatProblemListReport()
    {
        string[] _Lines = _templates["List of Problems"];
        RtfDocument _Doc = new(string.Empty);

        // Parse and write the report
        foreach (string _Line in _Lines)
        {
            if (_Line.Contains("@Description"))
            {
                foreach (StoryElement _Element in _model.StoryElements)
                    if (_Element.Type == StoryItemType.Problem)
                    {
                        ProblemModel _Chr = (ProblemModel)_Element;
                        StringBuilder _Sb = new(_Line);
                        _Sb.Replace("@Description", _Chr.Name);
                        _Doc.AddText(_Sb.ToString());
                        _Doc.AddNewLine();
                    }
            }
            else
            {
                _Doc.AddText(_Line);
                _Doc.AddNewLine();
            }
        }
        return _Doc.GetRtf();
    }

    public string FormatProblemReport(StoryElement element)
    {
        ProblemModel _Problem = (ProblemModel)element;
        string[] _Lines = _templates["Problem Description"];
        RtfDocument _Doc = new(string.Empty);

        StoryElement _VpProtagonist = StringToStoryElement(_Problem.Protagonist);
        StoryElement _VpAntagonist = StringToStoryElement(_Problem.Antagonist);

        // Parse and write the report
        foreach (string _Line in _Lines)
        {
            StringBuilder _Sb = new(_Line);
            if (string.IsNullOrEmpty(_Problem.Name)) { _Sb.Replace("@Title", ""); }
            else { _Sb.Replace("@Title", _Problem.Name); }

            _Sb.Replace("@ProblemType", _Problem.ProblemType);

            if (string.IsNullOrEmpty(_Problem.ConflictType)) { _Sb.Replace("@ConflictType", ""); }
            else { _Sb.Replace("@ConflictType", _Problem.ConflictType); }
            
            if (string.IsNullOrEmpty(_Problem.Subject)) { _Sb.Replace("@Subject", ""); }
            else { _Sb.Replace("@Subject", _Problem.Subject); }

            if (string.IsNullOrEmpty(_Problem.StoryQuestion)) { _Sb.Replace("@StoryQuestion", ""); } 
            else { _Sb.Replace("@StoryQuestion", GetText(_Problem.StoryQuestion)); }
            
            if (string.IsNullOrEmpty(_Problem.ProblemSource)) { _Sb.Replace("@ProblemSource", ""); }
            else { _Sb.Replace("@ProblemSource", _Problem.ProblemSource); }

            if (string.IsNullOrEmpty(_Problem.ProtMotive)) { _Sb.Replace("@ProtagMotive", ""); }
            else { _Sb.Replace("@ProtagMotive", _Problem.ProtMotive); }

            if (string.IsNullOrEmpty(_Problem.ProtGoal)) { _Sb.Replace("@ProtagGoal", ""); }
            else { _Sb.Replace("@ProtagGoal", _Problem.ProtGoal); }
            
            if (_VpProtagonist != null)
            {
                if (string.IsNullOrEmpty(_VpProtagonist.Name)) { _Sb.Replace("@ProtagName", ""); }
                else { _Sb.Replace("@ProtagName", _VpProtagonist.Name); }
            }
            else { _Sb.Replace("@ProtagName", ""); }

            if (string.IsNullOrEmpty(_Problem.ProtConflict)) { _Sb.Replace("@ProtagConflict", ""); }
            else { _Sb.Replace("@ProtagConflict", _Problem.ProtConflict); }

            if (_VpAntagonist != null)
            {
                if (string.IsNullOrEmpty(_VpAntagonist.Name)) { _Sb.Replace("@AntagName", ""); }
                else { _Sb.Replace("@AntagName", _VpAntagonist.Name); }
            }
            else { _Sb.Replace("@AntagName", ""); }
            
            if (string.IsNullOrEmpty(_Problem.AntagMotive)) { _Sb.Replace("@AntagMotive", ""); }
            else { _Sb.Replace("@AntagMotive", _Problem.AntagMotive); }

            if (string.IsNullOrEmpty(_Problem.AntagGoal)) { _Sb.Replace("@AntagGoal", ""); }
            else { _Sb.Replace("@AntagGoal", _Problem.AntagGoal); }

            if (string.IsNullOrEmpty(_Problem.AntagConflict)) { _Sb.Replace("@AntagConflict", ""); }
            else { _Sb.Replace("@AntagConflict", _Problem.AntagConflict); }

            if (string.IsNullOrEmpty(_Problem.Outcome)) { _Sb.Replace("@Outcome", ""); }
            else { _Sb.Replace("@Outcome", _Problem.Outcome); }

            if (string.IsNullOrEmpty(_Problem.Method)) { _Sb.Replace("@Method", ""); }
            else { _Sb.Replace("@Method", _Problem.Method); }
            
            if (string.IsNullOrEmpty(_Problem.Theme)) { _Sb.Replace("@Theme", ""); }
            else { _Sb.Replace("@Theme", _Problem.Theme); }

            if (string.IsNullOrEmpty(_Problem.Premise)) { _Sb.Replace("@Premise", ""); }
            else { _Sb.Replace("@Premise", GetText(_Problem.Premise)); }
            
            if (string.IsNullOrEmpty(_Problem.Notes)) { _Sb.Replace("@Notes", ""); }
            else { _Sb.Replace("@Notes", GetText(_Problem.Notes)); }

            _Doc.AddText(_Sb.ToString());
            _Doc.AddNewLine();
        }

        return _Doc.GetRtf();
    }

    public string FormatCharacterRelationshipReport(StoryElement element)
    {
        CharacterModel _Character = (CharacterModel)element;
        RtfDocument _Doc = new(string.Empty);
        foreach (RelationshipModel _Rel in _Character.RelationshipList)
        {
            foreach (string _Line in _templates["Character Relationship Description"])
            {
                StringBuilder _Sb = new(_Line);
                if (_Rel.Partner == null)
                {
                    foreach (StoryElement _Variable in Ioc.Default.GetRequiredService<ShellViewModel>().StoryModel.StoryElements.Characters)
                    {
                        if (_Variable.Uuid.Equals(Guid.Parse(_Rel.PartnerUuid)))
                        {
                            _Sb.Replace("@Relationship", _Variable.Name);
                            break;
                        }
                    }
                }
                else
                {
                    _Sb.Replace("@Relationship", _Rel.Partner.Name);

                }

                _Sb.Replace("@relationType", _Rel.RelationType);
                _Sb.Replace("@relationTrait", _Rel.Trait);
                _Sb.Replace("@Attitude", _Rel.Attitude);
                _Sb.Replace("@Notes", GetText(_Rel.Notes));

                _Doc.AddText(_Sb.ToString());
                _Doc.AddNewLine();
            }
            _Doc.AddNewLine();
            _Doc.AddNewLine();
        }

        return _Doc.GetRtf();
    }

    public string FormatCharacterListReport()
    {
        string[] _Lines = _templates["List of Characters"];
        RtfDocument _Doc = new(string.Empty);

        // Parse and write the report
        foreach (string _Line in _Lines)
        {
            if (_Line.Contains("@Description"))
            {
                foreach (StoryElement _Element in _model.StoryElements)
                    if (_Element.Type == StoryItemType.Character)
                    {
                        CharacterModel _Chr = (CharacterModel)_Element;
                        StringBuilder _Sb = new(_Line);
                        _Sb.Replace("@Description", _Chr.Name);
                        _Doc.AddText(_Sb.ToString());
                        _Doc.AddNewLine();
                    }
            }
            else
            {
                _Doc.AddText(_Line);
                _Doc.AddNewLine();
            }
        }
        return _Doc.GetRtf();
    }

    public string FormatCharacterReport(StoryElement element)
    {
        CharacterModel _Character = (CharacterModel)element;
        string[] _Lines = _templates["Character Description"];
        RtfDocument _Doc = new(string.Empty);


        // Parse and write the report
        foreach (string _Line in _Lines)
        {
            StringBuilder _Sb = new(_Line);
            //Story Role section
            _Sb.Replace("@Id", _Character.Id.ToString());
            _Sb.Replace("@Title", _Character.Name);
            _Sb.Replace("@Role", _Character.Role);
            _Sb.Replace("@StoryRole", _Character.StoryRole);
            _Sb.Replace("@Archetype", _Character.Archetype);
            _Sb.Replace("@CharacterSketch", GetText(_Character.CharacterSketch));
            //Physical section
            _Sb.Replace("@Age", _Character.Age);
            _Sb.Replace("@Sex", _Character.Sex);
            _Sb.Replace("@Height", _Character.CharHeight);
            _Sb.Replace("@Weight", _Character.Weight);
            _Sb.Replace("@Eyes", _Character.Eyes);
            _Sb.Replace("@Hair", _Character.Hair);
            _Sb.Replace("@Build", _Character.Build);
            _Sb.Replace("@Skin", _Character.Complexion);
            _Sb.Replace("@Race", _Character.Race);
            _Sb.Replace("@Nationality", _Character.Nationality);
            _Sb.Replace("@Health", _Character.Health);
            _Sb.Replace("@PhysNotes", GetText(_Character.PhysNotes));
            //Appearance section
            _Sb.Replace("@Appearance", GetText(_Character.Appearance));
            //Relationships section
            if (_Sb.ToString() == "@Relationships" && _Character.RelationshipList.Count > 0)
            {
                _Sb.Replace("@Relationships", FormatCharacterRelationshipReport(element));
            }

            //Flaw section
            _Sb.Replace("@Flaw", GetText(_Character.Flaw));
            //Backstory section
            _Sb.Replace("@Notes", GetText(_Character.BackStory));
            //Social Traits section
            _Sb.Replace("@Economic", GetText(_Character.Economic));
            _Sb.Replace("@Education", GetText(_Character.Education));
            _Sb.Replace("@Ethnic", GetText(_Character.Ethnic));
            _Sb.Replace("@Religion", GetText(_Character.Religion));
            //Psychological Traits section
            _Sb.Replace("@Personality", _Character.Enneagram);
            _Sb.Replace("@Intelligence", _Character.Intelligence);
            _Sb.Replace("@Values", _Character.Values);
            _Sb.Replace("@Focus", _Character.Focus);
            _Sb.Replace("@Abnormality", _Character.Abnormality);
            _Sb.Replace("@PsychNotes", GetText(_Character.PsychNotes));
            //Inner Traits section
            _Sb.Replace("@Adventure", _Character.Adventureousness);
            _Sb.Replace("@Aggression", _Character.Aggression);
            _Sb.Replace("@Confidence", _Character.Confidence);
            _Sb.Replace("@Conscientious", _Character.Conscientiousness);
            _Sb.Replace("@Creative", _Character.Creativity);
            _Sb.Replace("@Dominance", _Character.Dominance);
            _Sb.Replace("@Enthusiasm", _Character.Enthusiasm);
            _Sb.Replace("@Assurance", _Character.Assurance);
            _Sb.Replace("@Sensitivity", _Character.Sensitivity);
            _Sb.Replace("@Shrewdness", _Character.Shrewdness);
            _Sb.Replace("@Sociability", _Character.Sociability);
            _Sb.Replace("@Stability", _Character.Stability);
            //Outer Traits section
            string _Traits = "";
            foreach (string _Trait in _Character.TraitList)
            {
                _Traits += _Trait + "\n";
            }
            _Sb.Replace("@Traits", _Traits);
            // Notes section
            _Sb.Replace("@Notes", GetText(_Character.Notes));

            _Doc.AddText(_Sb.ToString());
            _Doc.AddNewLine();
        }
         
        return _Doc.GetRtf();
    }

    public string FormatSettingListReport()
    {
        string[] _Lines = _templates["List of Settings"];
        RtfDocument _Doc = new(string.Empty);

        // Parse and write the report
        foreach (string _Line in _Lines)
        {
            if (_Line.Contains("@Description"))
            {
                foreach (StoryElement _Element in _model.StoryElements)
                    if (_Element.Type == StoryItemType.Setting)
                    {
                        SettingModel _Setting = (SettingModel)_Element;
                        StringBuilder _Sb = new(_Line);
                        _Sb.Replace("@Description", _Setting.Name);
                        _Doc.AddText(_Sb.ToString());
                        _Doc.AddNewLine();
                    }
            }
            else
            {
                _Doc.AddText(_Line);
                _Doc.AddNewLine();
            }
        }
        return _Doc.GetRtf();
    }

    public string FormatSettingReport(StoryElement element)
    {
        SettingModel _Setting = (SettingModel)element;
        string[] _Lines = _templates["Setting Description"];
        RtfDocument _Doc = new(string.Empty);

        // Parse and write the report
        foreach (string _Line in _Lines)
        {
            StringBuilder _Sb = new(_Line);
            _Sb.Replace("@Id", _Setting.Id.ToString());
            _Sb.Replace("@Title", _Setting.Name);
            _Sb.Replace("@Locale", _Setting.Locale);
            _Sb.Replace("@Season", _Setting.Season);
            _Sb.Replace("@Period", _Setting.Period);
            _Sb.Replace("@Lighting", _Setting.Lighting);
            _Sb.Replace("@Weather", _Setting.Weather);
            _Sb.Replace("@Temperature", _Setting.Temperature);
            _Sb.Replace("@Props", _Setting.Props);
            _Sb.Replace("@Summary", GetText(_Setting.Summary));
            _Sb.Replace("@Sights", GetText(_Setting.Sights));
            _Sb.Replace("@Sounds", GetText(_Setting.Sounds));
            _Sb.Replace("@Touch", GetText(_Setting.Touch));
            _Sb.Replace("@SmellTaste", GetText(_Setting.SmellTaste));
            _Sb.Replace("@Notes", GetText(_Setting.Notes));
            _Doc.AddText(_Sb.ToString());
            _Doc.AddNewLine();
        }
        return _Doc.GetRtf();
    }

    public string FormatSceneListReport()
    {
        string[] _Lines = _templates["List of Scenes"];
        RtfDocument _Doc = new(string.Empty);

        // Parse and write the report
        foreach (string _Line in _Lines)
        {
            if (_Line.Contains("@Description"))
            {
                foreach (StoryElement _Element in _model.StoryElements)
                    if (_Element.Type == StoryItemType.Scene)
                    {
                        SceneModel _Scene = (SceneModel)_Element;
                        StringBuilder _Sb = new(_Line);
                        _Sb.Replace("@Description", _Scene.Name);
                        _Doc.AddText(_Sb.ToString());
                        _Doc.AddNewLine();
                    }
            }
            else
            {
                _Doc.AddText(_Line);
                _Doc.AddNewLine();
            }
        }
        return _Doc.GetRtf(); 
    }

    public string FormatSceneReport(StoryElement element)
    {
        SceneModel _Scene = (SceneModel)element;
        string[] _Lines = _templates["Scene Description"];
        RtfDocument _Doc = new(string.Empty);

        StoryElement _VpCharacter = StringToStoryElement(_Scene.ViewpointCharacter);
        string _VpCharacterName = _VpCharacter?.Name ?? string.Empty;
        StoryElement _Antagonist = StringToStoryElement(_Scene.Antagonist);
        string _AntagonistName = _Antagonist?.Name ?? string.Empty;
        StoryElement _Protagonist = StringToStoryElement(_Scene.Protagonist);
        string _ProtagonistName = _Protagonist?.Name ?? string.Empty;
        StoryElement _Setting = StringToStoryElement(_Scene.Setting);
        string _SettingName = _Setting?.Name ?? string.Empty;

        // Parse and write the report
        foreach (string _Line in _Lines)
        {
            StringBuilder _Sb = new(_Line);
            //SCENE OVERVIEW SECTION
            _Sb.Replace("@Title", _Scene.Name);
            _Sb.Replace("@Date", _Scene.Date);
            _Sb.Replace("@Time", _Scene.Time);
            if (_Line.Contains("@ViewpointCharacter")) 
                _Sb.Replace("@ViewpointCharacter", _VpCharacterName);
            _Sb.Replace("@Setting", _SettingName);
            _Sb.Replace("@SceneType", _Scene.SceneType);
            
            if (_Line.Contains("@CastMember"))
            {
                foreach (string _SeCastMember in _Scene.CastMembers)
                {
                    StoryElement _CastMember = StringToStoryElement(_SeCastMember);
                    string _CastMemberName = _CastMember?.Name ?? string.Empty;
                    StringBuilder _SbCast = new(_Line);
                            
                    _SbCast.Replace("@CastMember", _CastMemberName);
                    _Doc.AddText(_SbCast.ToString());
                    _Doc.AddNewLine();
                }
                _Sb.Clear();
            }

            _Sb.Replace("@Remarks", GetText(_Scene.Remarks));
            //DEVELOPMENT SECTION
            if (_Line.Contains("@PurposeOfScene"))
            {
                string _PurposeString = "";
                foreach (string _Purpose in _Scene.ScenePurpose) { _PurposeString += _Purpose + ", "; }
                _Sb.Replace("@PurposeOfScene", _PurposeString);
                _Doc.AddText(_Sb.ToString());
                _Sb.Clear();
                /*              
                                foreach (string sePurpose in scene.ScenePurpose)
                                {
                                    StoryElement Purpose = StringToStoryElement(sePurpose);
                                    string PurposeName = Purpose?.Name ?? string.Empty;
                                    StringBuilder sbCast = new(line);

                                    sbCast.Replace("@CastMember", PurposeName);
                                    doc.AddText(sbCast.ToString());
                                    doc.AddNewLine();
                                }*/
            }

            _Sb.Replace("@ValueExchange", _Scene.ValueExchange);
            _Sb.Replace("@Events", GetText(_Scene.Events));
            _Sb.Replace("@Consequence", GetText(_Scene.Consequences));
            _Sb.Replace("@Significance", GetText(_Scene.Significance));
            _Sb.Replace("@Realization", GetText(_Scene.Realization));
            //SCENE CONFLICT SECTION
            _Sb.Replace("@ProtagName", _ProtagonistName);
            _Sb.Replace("@ProtagEmotion", _Scene.ProtagEmotion);
            _Sb.Replace("@ProtagGoal", _Scene.ProtagGoal);
            _Sb.Replace("@Opposition", _Scene.Opposition);
            _Sb.Replace("@AntagName", _AntagonistName);
            _Sb.Replace("@AntagEmotion", _Scene.AntagEmotion);
            _Sb.Replace("@AntagGoal", _Scene.AntagGoal);
            _Sb.Replace("@Outcome", _Scene.Outcome);
            //SEQUEL SECTION
            _Sb.Replace("@Emotion", _Scene.Emotion);
            _Sb.Replace("@Review", GetText(_Scene.Review));
            _Sb.Replace("@NewGoal", _Scene.NewGoal);
            //SCENE NOTES SECTION
            _Sb.Replace("@Notes", GetText(_Scene.Notes));

            _Doc.AddText(_Sb.ToString());
            _Doc.AddNewLine();
        }

        return _Doc.GetRtf();
    }

    public string FormatWebReport (StoryElement element)
    {
        RtfDocument _Doc = new(string.Empty);
        StringBuilder _Sb = new();
        WebModel _Model = element as WebModel;
        _Sb.AppendLine(_Model!.Name);
        _Sb.AppendLine(_Model.URL.ToString());
        _Doc.AddText(_Sb.ToString());
        _Doc.AddNewLine();
        return _Doc.GetRtf();

        /*
         STUB: Re-implement this code at a later date, because
        var _webview = new WebView2();
        _webview.Source = ((WebModel)element).URL;
        _webview.Height = 1080;
        _webview.Width = 1920;
        await _webview.EnsureCoreWebView2Async();

        //Create file
        StorageFolder ParentFolder = ApplicationData.Current.RoamingFolder;
        ParentFolder = await ParentFolder.CreateFolderAsync("Cache",CreationCollisionOption.OpenIfExists);
        StorageFile imgfile = await ParentFolder.CreateFileAsync(Guid.NewGuid().ToString() + ".png");
        
        //Screenshot site
        MemoryStream _buffer = new();
        var x = _buffer.AsRandomAccessStream();

        await _webview.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, (IRandomAccessStream)_buffer);
        //await _webview.CoreWebView2.PrintToPdfAsync("C:\\test.pdf", null);
        //Write to disk
        Stream WriteStream = await imgfile.OpenStreamForWriteAsync();
        WriteStream.Write(_buffer.ToArray());

        //Add image
        doc.AddImage(imgfile.Path, 1920, 1080);
        */

    }

    public string FormatWebListReport()
    {
        string[] _Lines = _templates["List of Websites"];
        RtfDocument _Doc = new(string.Empty);

        // Parse and write the report
        foreach (string _Line in _Lines)
        {
            if (_Line.Contains("@Description"))
            {
                foreach (StoryElement _Element in _model.StoryElements)
                {
                    if (_Element.Type == StoryItemType.Web)
                    {
                        WebModel _Scene = (WebModel)_Element;
                        StringBuilder _Sb = new(_Line);
                        _Sb.Replace("@Description", _Scene.Name);
                        _Doc.AddText(_Sb.ToString());
                        _Doc.AddNewLine();
                    }
                }

            }
            else
            {
                _Doc.AddText(_Line);
                _Doc.AddNewLine();
            }
        }
        return _Doc.GetRtf();
    }

    public string FormatFolderReport(StoryElement element)
    {
        FolderModel _Folder = (FolderModel)element;
        string[] _Lines = _templates["Folder Description"];
        RtfDocument _Doc = new(string.Empty);
        // Parse and write the report
        foreach (string _Line in _Lines)
        {
            StringBuilder _Sb = new(_Line);
            _Sb.Replace("@Name", _Folder.Name);
            _Sb.Replace("@Notes", GetText(_Folder.Notes));
            _Doc.AddText(_Sb.ToString());  //,format);
            _Doc.AddNewLine();
        }
        return _Doc.GetRtf();
    }

    public string FormatSectionReport(StoryElement element)
    {
        SectionModel _Section = (SectionModel)element;
        string[] _Lines = _templates["Section Description"];
        RtfDocument _Doc = new(string.Empty);

        // Parse and write the report
        foreach (string _Line in _Lines)
        {
            StringBuilder _Sb = new(_Line);
            _Sb.Replace("@Name", _Section.Name);
            _Sb.Replace("@Notes", GetText(_Section.Notes));
            _Doc.AddText(_Sb.ToString()); // , format);
            _Doc.AddNewLine();
        }
        return _Doc.GetRtf();
    }

    public string FormatSynopsisReport()
    {

        string[] _Lines = _templates["Story Synopsis"];
        RtfDocument _Doc = new(string.Empty);

        // Pre-process RTF properties to preserve [FILE:x.rtf] tag for long fields
        // and then load long fields from their corresponding file in its subfolder

        // Parse and write the report
        foreach (string _Line in _Lines)
        {
            if (_Line.Contains("@Synopsis"))
            {
                // Find StoryNarrator' Scenes
                foreach (StoryNodeItem _Child in _model.NarratorView[0].Children)
                {
                    StoryElement _Scn = _model.StoryElements.StoryElementGuids[_Child.Uuid];
                    if (_Scn.Type != StoryItemType.Scene)
                        continue;
                    SceneModel _Scene = (SceneModel)_Scn;
                    StringBuilder _Sb = new(_Line);
                    _Sb.Replace("@Synopsis", $"[{_Scene.Name}] {_Scene.Description}");
                    _Doc.AddText(_Sb.ToString());
                    _Doc.AddNewLine();
                    _Doc.AddText(_Scene.Remarks);
                    _Doc.AddNewLine();
                }
            }
            else
            {
                _Doc.AddText(_Line);
                _Doc.AddNewLine();
            }
        }
        return _Doc.GetRtf();
    }

    public async Task LoadReportTemplates()
    {
        try
        {
            _templates.Clear();
            StorageFolder _LocalFolder = ApplicationData.Current.RoamingFolder;
            StorageFolder _StbFolder = await _LocalFolder.GetFolderAsync("StoryBuilder");
            StorageFolder _TemplatesFolder = await _StbFolder.GetFolderAsync("reports");
            IReadOnlyList<StorageFile> _Templates = await _TemplatesFolder.GetFilesAsync();
            foreach (StorageFile _Fi in _Templates)
            {
                string _Name = _Fi.DisplayName[..(_Fi.Name.Length - 4)];
                string _Text = await FileIO.ReadTextAsync(_Fi);
                string[] _Lines = _Text.Split(
                    new[] { Environment.NewLine },
                    StringSplitOptions.None
                );
                _templates.Add(_Name, _Lines);
            }
        }
        catch (Exception _Ex)
        {
            Ioc.Default.GetRequiredService<LogService>().LogException(LogLevel.Error, _Ex, "Error loading report templates.");
        }
    }

    #endregion

    #region Private methods
    private static StoryElement StringToStoryElement(string value)
    {
        if (value == null)
            return null;
        if (value.Equals(string.Empty))
            return null;
        // Get the current StoryModel's StoryElementsCollection
        ShellViewModel _Shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        StoryElementCollection _Elements = _Shell.StoryModel.StoryElements;
        // legacy: locate the StoryElement from its Name
        foreach (StoryElement _Element in _Elements)  // Character or Setting??? Search both?
        {
            if (_Element.Type == StoryItemType.Character | _Element.Type == StoryItemType.Setting)
            {
                if (value.Equals(_Element.Name))
                    return _Element;
            }
        }
        // Look for the StoryElement corresponding to the passed guid
        // (This is the normal approach)
        if (Guid.TryParse(value, out Guid _Guid))
            if (_Elements.StoryElementGuids.ContainsKey(_Guid))
                return _Elements.StoryElementGuids[_Guid];
        return null;  // Not found
    }

    /// <summary>
    /// A RichEditBox property is an a wrapper for an RTF 
    /// document, with its header, font table, color table, etc.,
    /// and which can be read or written. This causes format problems
    /// when it's a cell on a StoryBuilder report.  This function
    /// returns only the text, but does preserve newlines as 
    /// paragraph breaks.
    /// </summary>
    public string GetText(string rtfInput, bool formatNewLines = true)
    {
        string _Text = rtfInput ?? string.Empty;
        if (rtfInput.Equals(string.Empty)) {return string.Empty;}
        RichTextStripper _Rts = new();
        _Text =  _Rts.StripRichTextFormat(_Text);
        _Text = _Text.Replace("\'0d", "");
        _Text = _Text.Replace("\'0a", "");
        _Text = _Text.Replace("\\","");
        return _Text;
    }

    /// <summary>
    /// Generate a UUID in the Scrivener format (i.e., without curly braces)
    /// </summary>
    /// <returns>string UUID representation</returns>

    #endregion

    #region Constructor

    public ReportFormatter() 
    {
        _rdr = Ioc.Default.GetService<StoryReader>();
        ShellViewModel _Shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        _model = _Shell.StoryModel;
    }

    #endregion
}