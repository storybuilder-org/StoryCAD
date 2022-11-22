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
        OverviewModel overview = (OverviewModel)element;
        string[] lines = _templates["Story Overview"];  
        RtfDocument doc = new(string.Empty);

        StoryElement vpChar = StringToStoryElement(overview.ViewpointCharacter);
        string vpName = vpChar?.Name ?? string.Empty;
        StoryElement seProblem = StringToStoryElement(overview.StoryProblem);
        string problemName = seProblem?.Name ?? string.Empty;
        ProblemModel problem = (ProblemModel)seProblem;
        string premise = problem?.Premise ?? string.Empty;

        // Parse and write the report
        foreach (string line in lines)
        {
            // Parse the report
            StringBuilder sb = new(line);
            sb.Replace("@Title", overview.Name);
            sb.Replace("@CreateDate", overview.DateCreated);
            sb.Replace("@ModifiedDate", overview.DateModified);
            sb.Replace("@Author", overview.Author);
            sb.Replace("@StoryType", overview.StoryType);
            sb.Replace("@Genre", overview.StoryGenre);
            sb.Replace("@Viewpoint", overview.Viewpoint);
            sb.Replace("@StoryIdea", GetText(overview.StoryIdea));
            sb.Replace("@Concept", GetText(overview.Concept));
            sb.Replace("@StoryProblem", problemName);
            sb.Replace("@Premise", GetText(premise));
            sb.Replace("@StoryType", overview.StoryType);
            sb.Replace("@StoryGenre", overview.StoryGenre);
            sb.Replace("@LiteraryDevice", overview.LiteraryDevice);
            sb.Replace("@viewpointCharacter", vpName);
            sb.Replace("@Voice", overview.Voice);
            sb.Replace("@Tense", overview.Tense);
            sb.Replace("@Style", overview.Style);
            sb.Replace("@StructureNotes", GetText(overview.StructureNotes));
            sb.Replace("@Tone", overview.Tone);
            sb.Replace("@Notes", GetText(overview.Notes));
            doc.AddText(sb.ToString());
            doc.AddNewLine();
        }
        return doc.GetRtf();
    }

    public string FormatProblemListReport()
    {
        string[] lines = _templates["List of Problems"];
        RtfDocument doc = new(string.Empty);

        // Parse and write the report
        foreach (string line in lines)
        {
            if (line.Contains("@Description"))
            {
                foreach (StoryElement element in _model.StoryElements)
                    if (element.Type == StoryItemType.Problem)
                    {
                        ProblemModel chr = (ProblemModel)element;
                        StringBuilder sb = new(line);
                        sb.Replace("@Description", chr.Name);
                        doc.AddText(sb.ToString());
                        doc.AddNewLine();
                    }
            }
            else
            {
                doc.AddText(line);
                doc.AddNewLine();
            }
        }
        return doc.GetRtf();
    }

    public string FormatProblemReport(StoryElement element)
    {
        ProblemModel problem = (ProblemModel)element;
        string[] lines = _templates["Problem Description"];
        RtfDocument doc = new(string.Empty);

        StoryElement vpProtagonist = StringToStoryElement(problem.Protagonist);
        StoryElement vpAntagonist = StringToStoryElement(problem.Antagonist);

        // Parse and write the report
        foreach (string line in lines)
        {
            StringBuilder sb = new(line);
            if (String.IsNullOrEmpty(problem.Name)) { sb.Replace("@Title", ""); }
            else { sb.Replace("@Title", problem.Name); }

            sb.Replace("@ProblemType", problem.ProblemType);

            if (String.IsNullOrEmpty(problem.ConflictType)) { sb.Replace("@ConflictType", ""); }
            else { sb.Replace("@ConflictType", problem.ConflictType); }
            
            if (String.IsNullOrEmpty(problem.Subject)) { sb.Replace("@Subject", ""); }
            else { sb.Replace("@Subject", problem.Subject); }

            if (String.IsNullOrEmpty(problem.StoryQuestion)) { sb.Replace("@StoryQuestion", ""); } 
            else { sb.Replace("@StoryQuestion", GetText(problem.StoryQuestion)); }
            
            if (String.IsNullOrEmpty(problem.ProblemSource)) { sb.Replace("@ProblemSource", ""); }
            else { sb.Replace("@ProblemSource", problem.ProblemSource); }

            if (String.IsNullOrEmpty(problem.ProtMotive)) { sb.Replace("@ProtagMotive", ""); }
            else { sb.Replace("@ProtagMotive", problem.ProtMotive); }

            if (String.IsNullOrEmpty(problem.ProtGoal)) { sb.Replace("@ProtagGoal", ""); }
            else { sb.Replace("@ProtagGoal", problem.ProtGoal); }
            
            if (vpProtagonist != null)
            {
                if (String.IsNullOrEmpty(vpProtagonist.Name)) { sb.Replace("@ProtagName", ""); }
                else { sb.Replace("@ProtagName", vpProtagonist.Name); }
            }
            else { sb.Replace("@ProtagName", ""); }

            if (String.IsNullOrEmpty(problem.ProtConflict)) { sb.Replace("@ProtagConflict", ""); }
            else { sb.Replace("@ProtagConflict", problem.ProtConflict); }

            if (vpAntagonist != null)
            {
                if (String.IsNullOrEmpty(vpAntagonist.Name)) { sb.Replace("@AntagName", ""); }
                else { sb.Replace("@AntagName", vpAntagonist.Name); }
            }
            else { sb.Replace("@AntagName", ""); }
            
            if (String.IsNullOrEmpty(problem.AntagMotive)) { sb.Replace("@AntagMotive", ""); }
            else { sb.Replace("@AntagMotive", problem.AntagMotive); }

            if (String.IsNullOrEmpty(problem.AntagGoal)) { sb.Replace("@AntagGoal", ""); }
            else { sb.Replace("@AntagGoal", problem.AntagGoal); }

            if (String.IsNullOrEmpty(problem.AntagConflict)) { sb.Replace("@AntagConflict", ""); }
            else { sb.Replace("@AntagConflict", problem.AntagConflict); }

            if (String.IsNullOrEmpty(problem.Outcome)) { sb.Replace("@Outcome", ""); }
            else { sb.Replace("@Outcome", problem.Outcome); }

            if (String.IsNullOrEmpty(problem.Method)) { sb.Replace("@Method", ""); }
            else { sb.Replace("@Method", problem.Method); }
            
            if (String.IsNullOrEmpty(problem.Theme)) { sb.Replace("@Theme", ""); }
            else { sb.Replace("@Theme", problem.Theme); }

            if (String.IsNullOrEmpty(problem.Premise)) { sb.Replace("@Premise", ""); }
            else { sb.Replace("@Premise", GetText(problem.Premise)); }
            
            if (String.IsNullOrEmpty(problem.Notes)) { sb.Replace("@Notes", ""); }
            else { sb.Replace("@Notes", GetText(problem.Notes)); }

            doc.AddText(sb.ToString());
            doc.AddNewLine();
        }

        return doc.GetRtf();
    }

    public string FormatCharacterRelationshipReport(StoryElement element)
    {
        CharacterModel character = (CharacterModel)element;
        RtfDocument doc = new(string.Empty);
        foreach (RelationshipModel rel in character.RelationshipList)
        {
            foreach (string line in _templates["Character Relationship Description"])
            {
                StringBuilder sb = new(line);
                if (rel.Partner == null)
                {
                    foreach (StoryElement VARIABLE in Ioc.Default.GetService<ShellViewModel>().StoryModel.StoryElements.Characters)
                    {
                        if (VARIABLE.Uuid.Equals(Guid.Parse(rel.PartnerUuid)))
                        {
                            sb.Replace("@Relationship", VARIABLE.Name);
                            break;
                        }
                    }
                }
                else
                {
                    sb.Replace("@Relationship", rel.Partner.Name);

                }

                sb.Replace("@relationType", rel.RelationType);
                sb.Replace("@relationTrait", rel.Trait);
                sb.Replace("@Attitude", rel.Attitude);
                sb.Replace("@Notes", GetText(rel.Notes));

                doc.AddText(sb.ToString());
                doc.AddNewLine();
            }
            doc.AddNewLine();
            doc.AddNewLine();
        }

        return doc.GetRtf();
    }

    public string FormatCharacterListReport()
    {
        string[] lines = _templates["List of Characters"];
        RtfDocument doc = new(string.Empty);

        // Parse and write the report
        foreach (string line in lines)
        {
            if (line.Contains("@Description"))
            {
                foreach (StoryElement element in _model.StoryElements)
                    if (element.Type == StoryItemType.Character)
                    {
                        CharacterModel chr = (CharacterModel)element;
                        StringBuilder sb = new(line);
                        sb.Replace("@Description", chr.Name);
                        doc.AddText(sb.ToString());
                        doc.AddNewLine();
                    }
            }
            else
            {
                doc.AddText(line);
                doc.AddNewLine();
            }
        }
        return doc.GetRtf();
    }

    public string FormatCharacterReport(StoryElement element)
    {
        CharacterModel character = (CharacterModel)element;
        string[] lines = _templates["Character Description"];
        RtfDocument doc = new(string.Empty);


        // Parse and write the report
        foreach (string line in lines)
        {
            StringBuilder sb = new(line);
            //Story Role section
            sb.Replace("@Id", character.Id.ToString());
            sb.Replace("@Title", character.Name);
            sb.Replace("@Role", character.Role);
            sb.Replace("@StoryRole", character.StoryRole);
            sb.Replace("@Archetype", character.Archetype);
            sb.Replace("@CharacterSketch", GetText(character.CharacterSketch));
            //Physical section
            sb.Replace("@Age", character.Age);
            sb.Replace("@Sex", character.Sex);
            sb.Replace("@Height", character.CharHeight);
            sb.Replace("@Weight", character.Weight);
            sb.Replace("@Eyes", character.Eyes);
            sb.Replace("@Hair", character.Hair);
            sb.Replace("@Build", character.Build);
            sb.Replace("@Skin", character.Complexion);
            sb.Replace("@Race", character.Race);
            sb.Replace("@Nationality", character.Nationality);
            sb.Replace("@Health", character.Health);
            sb.Replace("@PhysNotes", GetText(character.PhysNotes));
            //Appearance section
            sb.Replace("@Appearance", GetText(character.Appearance));
            //Relationships section
            if (sb.ToString() == "@Relationships" && character.RelationshipList.Count > 0)
            {
                sb.Replace("@Relationships", FormatCharacterRelationshipReport(element));
            }

            //Flaw section
            sb.Replace("@Flaw", GetText(character.Flaw));
            //Backstory section
            sb.Replace("@Notes", GetText(character.BackStory));
            //Social Traits section
            sb.Replace("@Economic", GetText(character.Economic));
            sb.Replace("@Education", GetText(character.Education));
            sb.Replace("@Ethnic", GetText(character.Ethnic));
            sb.Replace("@Religion", GetText(character.Religion));
            //Psychological Traits section
            sb.Replace("@Personality", character.Enneagram);
            sb.Replace("@Intelligence", character.Intelligence);
            sb.Replace("@Values", character.Values);
            sb.Replace("@Focus", character.Focus);
            sb.Replace("@Abnormality", character.Abnormality);
            sb.Replace("@PsychNotes", GetText(character.PsychNotes));
            //Inner Traits section
            sb.Replace("@Adventure", character.Adventureousness);
            sb.Replace("@Aggression", character.Aggression);
            sb.Replace("@Confidence", character.Confidence);
            sb.Replace("@Conscientious", character.Conscientiousness);
            sb.Replace("@Creative", character.Creativity);
            sb.Replace("@Dominance", character.Dominance);
            sb.Replace("@Enthusiasm", character.Enthusiasm);
            sb.Replace("@Assurance", character.Assurance);
            sb.Replace("@Sensitivity", character.Sensitivity);
            sb.Replace("@Shrewdness", character.Shrewdness);
            sb.Replace("@Sociability", character.Sociability);
            sb.Replace("@Stability", character.Stability);
            //Outer Traits section
            string traits = "";
            foreach (string trait in character.TraitList)
            {
                traits += trait + "\n";
            }
            sb.Replace("@Traits", traits);
            // Notes section
            sb.Replace("@Notes", GetText(character.Notes));

            doc.AddText(sb.ToString());
            doc.AddNewLine();
        }
         
        return doc.GetRtf();
    }

    public string FormatSettingListReport()
    {
        string[] lines = _templates["List of Settings"];
        RtfDocument doc = new(string.Empty);

        // Parse and write the report
        foreach (string line in lines)
        {
            if (line.Contains("@Description"))
            {
                foreach (StoryElement element in _model.StoryElements)
                    if (element.Type == StoryItemType.Setting)
                    {
                        SettingModel setting = (SettingModel)element;
                        StringBuilder sb = new(line);
                        sb.Replace("@Description", setting.Name);
                        doc.AddText(sb.ToString());
                        doc.AddNewLine();
                    }
            }
            else
            {
                doc.AddText(line);
                doc.AddNewLine();
            }
        }
        return doc.GetRtf();
    }

    public string FormatSettingReport(StoryElement element)
    {
        SettingModel setting = (SettingModel)element;
        string[] lines = _templates["Setting Description"];
        RtfDocument doc = new(string.Empty);

        // Parse and write the report
        foreach (string line in lines)
        {
            StringBuilder sb = new(line);
            sb.Replace("@Id", setting.Id.ToString());
            sb.Replace("@Title", setting.Name);
            sb.Replace("@Locale", setting.Locale);
            sb.Replace("@Season", setting.Season);
            sb.Replace("@Period", setting.Period);
            sb.Replace("@Lighting", setting.Lighting);
            sb.Replace("@Weather", setting.Weather);
            sb.Replace("@Temperature", setting.Temperature);
            sb.Replace("@Props", setting.Props);
            sb.Replace("@Summary", GetText(setting.Summary));
            sb.Replace("@Sights", GetText(setting.Sights));
            sb.Replace("@Sounds", GetText(setting.Sounds));
            sb.Replace("@Touch", GetText(setting.Touch));
            sb.Replace("@SmellTaste", GetText(setting.SmellTaste));
            sb.Replace("@Notes", GetText(setting.Notes));
            doc.AddText(sb.ToString());
            doc.AddNewLine();
        }
        return doc.GetRtf();
    }

    public string FormatSceneListReport()
    {
        string[] lines = _templates["List of Scenes"];
        RtfDocument doc = new(string.Empty);

        // Parse and write the report
        foreach (string line in lines)
        {
            if (line.Contains("@Description"))
            {
                foreach (StoryElement element in _model.StoryElements)
                    if (element.Type == StoryItemType.Scene)
                    {
                        SceneModel scene = (SceneModel)element;
                        StringBuilder sb = new(line);
                        sb.Replace("@Description", scene.Name);
                        doc.AddText(sb.ToString());
                        doc.AddNewLine();
                    }
            }
            else
            {
                doc.AddText(line);
                doc.AddNewLine();
            }
        }
        return doc.GetRtf(); 
    }

    public string FormatSceneReport(StoryElement element)
    {
        SceneModel scene = (SceneModel)element;
        string[] lines = _templates["Scene Description"];
        RtfDocument doc = new(string.Empty);

        StoryElement vpCharacter = StringToStoryElement(scene.ViewpointCharacter);
        string vpCharacterName = vpCharacter?.Name ?? string.Empty;
        StoryElement antagonist = StringToStoryElement(scene.Antagonist);
        string antagonistName = antagonist?.Name ?? string.Empty;
        StoryElement protagonist = StringToStoryElement(scene.Protagonist);
        string protagonistName = protagonist?.Name ?? string.Empty;
        StoryElement setting = StringToStoryElement(scene.Setting);
        string settingName = setting?.Name ?? string.Empty;

        // Parse and write the report
        foreach (string line in lines)
        {
            StringBuilder sb = new(line);
            //SCENE OVERVIEW SECTION
            sb.Replace("@Title", scene.Name);
            sb.Replace("@Date", scene.Date);
            sb.Replace("@Time", scene.Time);
            if (line.Contains("@ViewpointCharacter")) {sb.Replace("@ViewpointCharacter", vpCharacterName);}
            sb.Replace("@Setting", settingName);
            sb.Replace("@SceneType", scene.SceneType);
            
            if (line.Contains("@CastMember"))
            {
                foreach (string seCastMember in scene.CastMembers)
                {
                    StoryElement castMember = StringToStoryElement(seCastMember);
                    string castMemberName = castMember?.Name ?? string.Empty;
                    StringBuilder sbCast = new(line);
                            
                    sbCast.Replace("@CastMember", castMemberName);
                    doc.AddText(sbCast.ToString());
                    doc.AddNewLine();
                }
                sb.Clear();
            }

            sb.Replace("@Remarks", GetText(scene.Remarks));
            //DEVELOPMENT SECTION
            if (line.Contains("@PurposeOfScene"))
            {
                string PurposeString = "";
                foreach (string Purpose in scene.ScenePurpose) { PurposeString += Purpose + ", "; }
                sb.Replace("@PurposeOfScene", PurposeString);
                doc.AddText(sb.ToString());
                sb.Clear();
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

            sb.Replace("@ValueExchange", scene.ValueExchange);
            sb.Replace("@Events", GetText(scene.Events));
            sb.Replace("@Consequence", GetText(scene.Consequences));
            sb.Replace("@Significance", GetText(scene.Significance));
            sb.Replace("@Realization", GetText(scene.Realization));
            //SCENE CONFLICT SECTION
            sb.Replace("@ProtagName", protagonistName);
            sb.Replace("@ProtagEmotion", scene.ProtagEmotion);
            sb.Replace("@ProtagGoal", scene.ProtagGoal);
            sb.Replace("@Opposition", scene.Opposition);
            sb.Replace("@AntagName", antagonistName);
            sb.Replace("@AntagEmotion", scene.AntagEmotion);
            sb.Replace("@AntagGoal", scene.AntagGoal);
            sb.Replace("@Outcome", scene.Outcome);
            //SEQUEL SECTION
            sb.Replace("@Emotion", scene.Emotion);
            sb.Replace("@Review", GetText(scene.Review));
            sb.Replace("@NewGoal", scene.NewGoal);
            //SCENE NOTES SECTION
            sb.Replace("@Notes", GetText(scene.Notes));

            doc.AddText(sb.ToString());
            doc.AddNewLine();
        }

        return doc.GetRtf();
    }

    public string FormatWebReport (StoryElement element)
    {
        RtfDocument doc = new(string.Empty);
        StringBuilder sb = new();
        WebModel model = element as WebModel;
        sb.AppendLine(model.Name);
        sb.AppendLine(model.URL.ToString());
        doc.AddText(sb.ToString());
        doc.AddNewLine();
        return doc.GetRtf();

        // STUB: Reimpliment this code at a later date, because
        /*
        //Create new Webview, then initalise corewebview and configure URL. 
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
        string[] lines = _templates["List of Websites"];
        RtfDocument doc = new(string.Empty);

        // Parse and write the report
        foreach (string line in lines)
        {
            if (line.Contains("@Description"))
            {
                foreach (StoryElement element in _model.StoryElements)
                {
                    if (element.Type == StoryItemType.Web)
                    {
                        WebModel scene = (WebModel)element;
                        StringBuilder sb = new(line);
                        sb.Replace("@Description", scene.Name);
                        doc.AddText(sb.ToString());
                        doc.AddNewLine();
                    }
                }

            }
            else
            {
                doc.AddText(line);
                doc.AddNewLine();
            }
        }
        return doc.GetRtf();
    }

    public string FormatFolderReport(StoryElement element)
    {
        FolderModel folder = (FolderModel)element;
        string[] lines = _templates["Folder Description"];
        RtfDocument doc = new(string.Empty);
        // Parse and write the report
        foreach (string line in lines)
        {
            StringBuilder sb = new(line);
            sb.Replace("@Name", folder.Name);
            sb.Replace("@Notes", GetText(folder.Notes));
            doc.AddText(sb.ToString());  //,format);
            doc.AddNewLine();
        }
        return doc.GetRtf();
    }

    public string FormatSectionReport(StoryElement element)
    {
        FolderModel section = (FolderModel)element;
        string[] lines = _templates["Section Description"];
        RtfDocument doc = new(string.Empty);

        // Parse and write the report
        foreach (string line in lines)
        {
            StringBuilder sb = new(line);
            sb.Replace("@Name", section.Name);
            sb.Replace("@Notes", GetText(section.Notes));
            doc.AddText(sb.ToString()); // , format);
            doc.AddNewLine();
        }
        return doc.GetRtf();
    }

    public string FormatSynopsisReport()
    {

        string[] lines = _templates["Story Synopsis"];
        RtfDocument doc = new(string.Empty);

        // Pre-process RTF properties to preserve [FILE:x.rtf] tag for long fields
        // and then load long fields from their corresponding file in its subfolder

        // Parse and write the report
        foreach (string line in lines)
        {
            if (line.Contains("@Synopsis"))
            {
                // Find StoryNarrator' Scenes
                foreach (StoryNodeItem child in _model.NarratorView[0].Children)
                {
                    StoryElement scn = _model.StoryElements.StoryElementGuids[child.Uuid];
                    if (scn.Type != StoryItemType.Scene)
                        continue;
                    SceneModel scene = (SceneModel)scn;
                    StringBuilder sb = new(line);
                    sb.Replace("@Synopsis", $"[{scene.Name}] {scene.Description}");
                    doc.AddText(sb.ToString());
                    doc.AddNewLine();
                    doc.AddText(scene.Remarks);
                    doc.AddNewLine();
                }
            }
            else
            {
                doc.AddText(line);
                doc.AddNewLine();
            }
        }
        return doc.GetRtf();
    }

    public async Task LoadReportTemplates()
    {
        try
        {
            _templates.Clear();
            StorageFolder localFolder = ApplicationData.Current.RoamingFolder;
            StorageFolder stbFolder = await localFolder.GetFolderAsync("StoryBuilder");
            StorageFolder templatesFolder = await stbFolder.GetFolderAsync("reports");
            IReadOnlyList<StorageFile> templates = await templatesFolder.GetFilesAsync();
            foreach (StorageFile fi in templates)
            {
                string name = fi.DisplayName[..(fi.Name.Length - 4)];
                string text = await FileIO.ReadTextAsync(fi);
                string[] lines = text.Split(
                    new[] { Environment.NewLine },
                    StringSplitOptions.None
                );
                _templates.Add(name, lines);
            }
        }
        catch (Exception ex)
        {
            Ioc.Default.GetService<LogService>().LogException(LogLevel.Error, ex, "Error loading report templates.");
        }
    }

    #endregion

    #region Private methods
    private StoryElement StringToStoryElement(string value)
    {
        if (value == null)
            return null;
        if (value.Equals(string.Empty))
            return null;
        // Get the current StoryModel's StoryElementsCollection
        ShellViewModel shell = Ioc.Default.GetService<ShellViewModel>();
        StoryElementCollection elements = shell.StoryModel.StoryElements;
        // legacy: locate the StoryElement from its Name
        foreach (StoryElement element in elements)  // Character or Setting??? Search both?
        {
            if (element.Type == StoryItemType.Character | element.Type == StoryItemType.Setting)
            {
                if (value.Equals(element.Name))
                    return element;
            }
        }
        // Look for the StoryElement corresponding to the passed guid
        // (This is the normal approach)
        if (Guid.TryParse(value, out Guid guid))
            if (elements.StoryElementGuids.ContainsKey(guid))
                return elements.StoryElementGuids[guid];
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
        string text = rtfInput ?? string.Empty;
        if (rtfInput.Equals(string.Empty))
            return string.Empty;
        RichTextStripper rts = new();
        text =  rts.StripRichTextFormat(text);
        text = text.Replace("\'0d", "");
        text = text.Replace("\'0a", "");
        text = text.Replace("\\","");
        return text;
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
        ShellViewModel shell = Ioc.Default.GetService<ShellViewModel>();
        _model = shell.StoryModel;
    }

    #endregion
}