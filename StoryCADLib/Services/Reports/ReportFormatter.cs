using System.Reflection;
using System.Text;
using NRtfTree.Util;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels.SubViewModels;
using StoryCADLib.ViewModels.Tools;

namespace StoryCADLib.Services.Reports;

public class ReportFormatter
{
    private readonly StoryModel _storyModel;
    private readonly Dictionary<string, string[]> _templates = new();
    private bool _templatesLoaded;

    /// <summary>
    /// Ensures report templates are loaded before use.
    /// This method is idempotent - calling it multiple times has no effect after first load.
    /// </summary>
    private async Task EnsureTemplatesLoadedAsync()
    {
        if (!_templatesLoaded)
        {
            await LoadReportTemplates();
            _templatesLoaded = true;
        }
    }

    public string FormatListReport(StoryItemType elementType)
    {
        //Get element (override web for websites.)
        var name = elementType == StoryItemType.Web ? "Websites" : elementType + "s";

        string[] lines =
        [
            $"                        StoryCAD - List of {name}",
            "",
            "                        @Description"
        ];
        RtfDocument doc = new(string.Empty);

        // Parse and write the report
        foreach (var line in lines)
        {
            if (line.Contains("@Description"))
            {
                var elements = _storyModel.StoryElements.Where(e => e.ElementType == elementType);
                foreach (var element in elements)
                {
                    StringBuilder sb = new(line);
                    sb.Replace("@Description", element.Name);
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

    #region Public methods

    public async Task<string> FormatStoryOverviewReport()
    {
        await EnsureTemplatesLoadedAsync();
        var overview =
            (OverviewModel)_storyModel.StoryElements.FirstOrDefault(element =>
                element.ElementType == StoryItemType.StoryOverview);
        var lines = _templates["Story Overview"];
        RtfDocument doc = new(string.Empty);

        var vpName = StoryElement.GetByGuid(overview.ViewpointCharacter).Name;
        var problemName = string.Empty;
        var premise = string.Empty;
        if (overview.StoryProblem != Guid.Empty)
        {
            var outlineService = Ioc.Default.GetRequiredService<OutlineService>();
            var problem = (ProblemModel)outlineService.GetStoryElementByGuid(_storyModel, overview.StoryProblem);
            problemName = problem.Name;
            premise = problem.Premise;
        }

        // Parse and write the report
        foreach (var line in lines)
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
            sb.Replace("@StoryIdea", GetText(overview.Description));
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

    public async Task<string> FormatProblemReport(StoryElement element)
    {
        await EnsureTemplatesLoadedAsync();
        var problem = (ProblemModel)element;
        var lines = _templates["Problem Description"];
        RtfDocument doc = new(string.Empty);

        // Parse and write the report
        foreach (var line in lines)
        {
            StringBuilder sb = new(line);
            if (string.IsNullOrEmpty(problem.Name))
            {
                sb.Replace("@Title", "");
            }
            else
            {
                sb.Replace("@Title", problem.Name);
            }

            if (string.IsNullOrEmpty(problem.ProblemType))
            {
                sb.Replace("@ProblemType", "");
            }
            else
            {
                sb.Replace("@ProblemType", problem.ProblemType);
            }

            if (string.IsNullOrEmpty(problem.ConflictType))
            {
                sb.Replace("@ConflictType", "");
            }
            else
            {
                sb.Replace("@ConflictType", problem.ConflictType);
            }

            if (string.IsNullOrEmpty(problem.ProblemCategory))
            {
                sb.Replace("@ProblemCategory", "");
            }
            else
            {
                sb.Replace("@ProblemCategory", problem.ProblemCategory);
            }

            if (string.IsNullOrEmpty(problem.Subject))
            {
                sb.Replace("@Subject", "");
            }
            else
            {
                sb.Replace("@Subject", problem.Subject);
            }

            if (string.IsNullOrEmpty(problem.Description))
            {
                sb.Replace("@StoryQuestion", "");
            }
            else
            {
                sb.Replace("@StoryQuestion", GetText(problem.Description));
            }

            if (string.IsNullOrEmpty(problem.ProblemSource))
            {
                sb.Replace("@ProblemSource", "");
            }
            else
            {
                sb.Replace("@ProblemSource", problem.ProblemSource);
            }

            if (string.IsNullOrEmpty(problem.ProtMotive))
            {
                sb.Replace("@ProtagMotive", "");
            }
            else
            {
                sb.Replace("@ProtagMotive", problem.ProtMotive);
            }

            if (string.IsNullOrEmpty(problem.ProtGoal))
            {
                sb.Replace("@ProtagGoal", "");
            }
            else
            {
                sb.Replace("@ProtagGoal", problem.ProtGoal);
            }

            if (problem.Protagonist != Guid.Empty)
            {
                StoryElement vpProtagonist = (CharacterModel)StoryElement.GetByGuid(problem.Protagonist);
                if (string.IsNullOrEmpty(vpProtagonist.Name))
                {
                    sb.Replace("@ProtagName", "");
                }
                else
                {
                    sb.Replace("@ProtagName", vpProtagonist.Name);
                }
            }
            else
            {
                sb.Replace("@ProtagName", "");
            }

            if (string.IsNullOrEmpty(problem.ProtConflict))
            {
                sb.Replace("@ProtagConflict", "");
            }
            else
            {
                sb.Replace("@ProtagConflict", problem.ProtConflict);
            }

            if (problem.Antagonist != Guid.Empty)
            {
                StoryElement vpAntagonist = (CharacterModel)StoryElement.GetByGuid(problem.Antagonist);

                if (string.IsNullOrEmpty(vpAntagonist.Name))
                {
                    sb.Replace("@AntagName", "");
                }
                else
                {
                    sb.Replace("@AntagName", vpAntagonist.Name);
                }
            }
            else
            {
                sb.Replace("@AntagName", "");
            }

            if (string.IsNullOrEmpty(problem.AntagMotive))
            {
                sb.Replace("@AntagMotive", "");
            }
            else
            {
                sb.Replace("@AntagMotive", problem.AntagMotive);
            }

            if (string.IsNullOrEmpty(problem.AntagGoal))
            {
                sb.Replace("@AntagGoal", "");
            }
            else
            {
                sb.Replace("@AntagGoal", problem.AntagGoal);
            }

            if (string.IsNullOrEmpty(problem.AntagConflict))
            {
                sb.Replace("@AntagConflict", "");
            }
            else
            {
                sb.Replace("@AntagConflict", problem.AntagConflict);
            }

            if (string.IsNullOrEmpty(problem.Outcome))
            {
                sb.Replace("@Outcome", "");
            }
            else
            {
                sb.Replace("@Outcome", problem.Outcome);
            }

            if (string.IsNullOrEmpty(problem.Method))
            {
                sb.Replace("@Method", "");
            }
            else
            {
                sb.Replace("@Method", problem.Method);
            }

            if (string.IsNullOrEmpty(problem.Theme))
            {
                sb.Replace("@Theme", "");
            }
            else
            {
                sb.Replace("@Theme", problem.Theme);
            }

            if (string.IsNullOrEmpty(problem.Premise))
            {
                sb.Replace("@Premise", "");
            }
            else
            {
                sb.Replace("@Premise", GetText(problem.Premise));
            }

            if (string.IsNullOrEmpty(problem.Notes))
            {
                sb.Replace("@Notes", "");
            }
            else
            {
                sb.Replace("@Notes", GetText(problem.Notes));
            }

            //Structure Tab
            if (string.IsNullOrEmpty(problem.StructureTitle))
            {
                sb.Replace("@StructTitle", "");
            }
            else
            {
                sb.Replace("@StructTitle", GetText(problem.StructureTitle));
            }

            if (string.IsNullOrEmpty(problem.StructureDescription))
            {
                sb.Replace("@StructDescription", "");
            }
            else
            {
                sb.Replace("@StructDescription", GetText(problem.StructureDescription));
            }

            if (problem.StructureBeats.Count == 0)
            {
                sb.Replace("@StructBeats", "");
            }
            else
            {
                var beats = FormatStructureBeatsElements(problem);
                sb.Replace("@StructBeats", beats);
            }

            doc.AddText(sb.ToString());
            doc.AddNewLine();
        }

        return doc.GetRtf();
    }

    public string FormatStoryProblemStructureReport()
    {
        var output = new StringBuilder();

        // Retrieve the ShellViewModel once
        var outlineViewModel = Ioc.Default.GetRequiredService<OutlineViewModel>();
        if (_storyModel?.StoryElements == null)
        {
            // StoryElements not available
            return string.Empty;
        }

        // Retrieve the Overview element
        var overview = _storyModel.StoryElements
            .OfType<OverviewModel>()
            .FirstOrDefault(e => e.ElementType == StoryItemType.StoryOverview);

        if (overview == null)
        {
            // Overview element not found
            return string.Empty;
        }

        // Retrieve the StoryProblem GUID from Overview
        if (overview.StoryProblem == Guid.Empty)
        {
            // StoryProblem is null or empty
            return string.Empty;
        }

        var storyProblem = (ProblemModel)_storyModel.StoryElements
            .StoryElementGuids[overview.StoryProblem];

        // Start building the tree with a separate root heading
        output.AppendLine("StoryCAD - Story Problem Structure Report");

        // Process each beat in the StoryProblem
        var processedElements = new HashSet<Guid>(); // Keep track of processed elements
        foreach (var beat in storyProblem.StructureBeats)
        {
            ProcessBeat(beat, output, _storyModel, 1, processedElements);
        }

        return output.ToString();
    }

    private void ProcessBeat(StructureBeatViewModel beat, StringBuilder output, StoryModel storyModel, int indentLevel,
        HashSet<Guid> processedElements)
    {
        var indent = new string('\t', indentLevel);
        var prefix = "- "; // Prefix to denote a beat

        // If GUID is assigned, resolve and append Element details
        if (beat.Guid != Guid.Empty)
        {
            // Check if the element has already been processed
            if (processedElements.Contains(beat.Guid))
            {
                return; // Exit to prevent infinite recursion
            }

            // Mark the element as processed
            processedElements.Add(beat.Guid);

            var element = storyModel.StoryElements
                .FirstOrDefault(e => e.Uuid.Equals(beat.Guid));

            if (element != null)
            {
                // Append Beat Title with prefix
                output.AppendLine($"{indent}{prefix}{beat.Title}");

                // Append Element Name and Description with additional indentation
                output.AppendLine($"{indent}   {beat.ElementName}");

                // If the element is a Problem, process its beats recursively
                if (element.ElementType == StoryItemType.Problem && element is ProblemModel problemElement)
                {
                    if (problemElement.StructureBeats != null && problemElement.StructureBeats.Any())
                    {
                        foreach (var subBeat in problemElement.StructureBeats)
                        {
                            ProcessBeat(subBeat, output, storyModel, indentLevel + 1, processedElements);
                        }
                    }
                }

                // Append separator with proper indentation
                output.AppendLine($"{indent}\t-------------");
            }
        }
    }

    private string FormatStructureBeatsElements(ProblemModel problem)
    {
        StringBuilder beats = new();
        foreach (var beat in problem.StructureBeats)
        {
            beats.AppendLine(beat.Title);
            beats.AppendLine(beat.Description);

            //Don't print element stuff if one is unassigned.
            // Don't print element stuff if one is unassigned.
            if (beat.Guid != Guid.Empty)
            {
                beats.AppendLine(beat.ElementName);
                beats.AppendLine(beat.ElementDescription);
            }

            beats.AppendLine("\t\t-------------");
        }

        return beats.ToString();
    }

    public async Task<string> FormatCharacterRelationshipReport(StoryElement element)
    {
        await EnsureTemplatesLoadedAsync();
        var character = (CharacterModel)element;
        RtfDocument doc = new(string.Empty);
        foreach (var rel in character.RelationshipList)
        {
            foreach (var line in _templates["Character Relationship Description"])
            {
                StringBuilder sb = new(line);
                if (rel.Partner == null)
                {
                    foreach (var otherCharacter in _storyModel.StoryElements.Characters)
                    {
                        if (otherCharacter.Uuid == rel.PartnerUuid)
                        {
                            sb.Replace("@Relationship", character.Name);
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

    public async Task<string> FormatCharacterReport(StoryElement element)
    {
        await EnsureTemplatesLoadedAsync();
        var character = (CharacterModel)element;
        var lines = _templates["Character Description"];
        RtfDocument doc = new(string.Empty);


        // Parse and write the report
        foreach (var line in lines)
        {
            StringBuilder sb = new(line);
            //Story Role section
            sb.Replace("@Title", character.Name);
            sb.Replace("@Role", character.Role);
            sb.Replace("@StoryRole", character.StoryRole);
            sb.Replace("@Archetype", character.Archetype);
            sb.Replace("@CharacterSketch", GetText(character.Description));
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
                sb.Replace("@Relationships", await FormatCharacterRelationshipReport(element));
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
            var traits = "";
            foreach (var trait in character.TraitList)
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

    public async Task<string> FormatSettingReport(StoryElement element)
    {
        await EnsureTemplatesLoadedAsync();
        var setting = (SettingModel)element;
        var lines = _templates["Setting Description"];
        RtfDocument doc = new(string.Empty);

        // Parse and write the report
        foreach (var line in lines)
        {
            StringBuilder sb = new(line);
            sb.Replace("@Title", setting.Name);
            sb.Replace("@Locale", setting.Locale);
            sb.Replace("@Season", setting.Season);
            sb.Replace("@Period", setting.Period);
            sb.Replace("@Lighting", setting.Lighting);
            sb.Replace("@Weather", setting.Weather);
            sb.Replace("@Temperature", setting.Temperature);
            sb.Replace("@Props", setting.Props);
            sb.Replace("@Summary", GetText(setting.Description));
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

    public async Task<string> FormatSceneReport(StoryElement element)
    {
        await EnsureTemplatesLoadedAsync();
        var scene = (SceneModel)element;
        var lines = _templates["Scene Description"];
        RtfDocument doc = new(string.Empty);

        var vpCharacter = StoryElement.GetByGuid(scene.ViewpointCharacter);
        var vpCharacterName = vpCharacter?.Name ?? string.Empty;
        var antagonist = StoryElement.GetByGuid(scene.Antagonist);
        var antagonistName = antagonist?.Name ?? string.Empty;
        var protagonist = StoryElement.GetByGuid(scene.Protagonist);
        var protagonistName = protagonist?.Name ?? string.Empty;
        var setting = StoryElement.GetByGuid(scene.Setting);
        var settingName = setting?.Name ?? string.Empty;

        // Parse and write the report
        foreach (var line in lines)
        {
            StringBuilder sb = new(line);
            //SCENE OVERVIEW SECTION
            sb.Replace("@Title", scene.Name);
            sb.Replace("@Date", scene.Date);
            sb.Replace("@Time", scene.Time);
            if (line.Contains("@ViewpointCharacter"))
            {
                sb.Replace("@ViewpointCharacter", vpCharacterName);
            }

            sb.Replace("@Setting", settingName);
            sb.Replace("@SceneType", scene.SceneType);

            if (line.Contains("@CastMember"))
            {
                foreach (var seCastMember in scene.CastMembers)
                {
                    var castMember = StoryElement.GetByGuid(seCastMember);
                    var castMemberName = castMember?.Name ?? "Unknown Character";
                    StringBuilder sbCast = new(line);

                    sbCast.Replace("@CastMember", castMemberName);
                    doc.AddText(sbCast.ToString());
                    doc.AddNewLine();
                }

                sb.Clear();
            }

            sb.Replace("@Remarks", GetText(scene.Description));
            //DEVELOPMENT SECTION
            if (line.Contains("@PurposeOfScene"))
            {
                var PurposeString = "";
                foreach (var Purpose in scene.ScenePurpose)
                {
                    PurposeString += Purpose + ", ";
                }

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

    public string FormatWebReport(StoryElement element)
    {
        RtfDocument doc = new(string.Empty);
        StringBuilder sb = new();
        var model = element as WebModel;
        sb.AppendLine(model!.Name);
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

    public async Task<string> FormatFolderReport(StoryElement element)
    {
        await EnsureTemplatesLoadedAsync();
        var folder = (FolderModel)element;
        var lines = _templates["Folder Description"];
        RtfDocument doc = new(string.Empty);
        // Parse and write the report
        foreach (var line in lines)
        {
            StringBuilder sb = new(line);
            sb.Replace("@Name", folder.Name);
            sb.Replace("@Notes", GetText(folder.Description));
            doc.AddText(sb.ToString()); //,format);
            doc.AddNewLine();
        }

        return doc.GetRtf();
    }

    public async Task<string> FormatSectionReport(StoryElement element)
    {
        await EnsureTemplatesLoadedAsync();
        var section = (FolderModel)element;
        var lines = _templates["Section Description"];
        RtfDocument doc = new(string.Empty);

        // Parse and write the report
        foreach (var line in lines)
        {
            StringBuilder sb = new(line);
            sb.Replace("@Name", section.Name);
            sb.Replace("@Notes", GetText(section.Description));
            doc.AddText(sb.ToString()); // , format);
            doc.AddNewLine();
        }

        return doc.GetRtf();
    }

    public async Task<string> FormatSynopsisReport()
    {
        await EnsureTemplatesLoadedAsync();
        var lines = _templates["Story Synopsis"];
        RtfDocument doc = new(string.Empty);

        // Pre-process RTF properties to preserve [FILE:x.rtf] tag for long fields
        // and then load long fields from their corresponding file in its subfolder

        // Parse and write the report
        foreach (var line in lines)
        {
            if (line.Contains("@Synopsis"))
            {
                // Find StoryNarrator' Scenes
                foreach (var child in _storyModel.NarratorView[0].Children)
                {
                    var outlineService = Ioc.Default.GetRequiredService<OutlineService>();
                    var scn = outlineService.GetStoryElementByGuid(_storyModel, child.Uuid);
                    if (scn.ElementType != StoryItemType.Scene)
                    {
                        continue;
                    }

                    var scene = (SceneModel)scn;
                    StringBuilder sb = new(line);
                    sb.Replace("@Synopsis", $"[{scene.Name}] {scene.Description}");
                    doc.AddText(sb.ToString());
                    doc.AddNewLine();
                    doc.AddText(scene.Description);
                    doc.AddNewLine();
                }

                if (_storyModel.NarratorView[0].Children.Count == 0)
                {
                    doc.AddText("You currently have no scenes within your narrative view, add some to see them here.");
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
            var tex = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            var Files = Assembly.GetExecutingAssembly().GetManifestResourceNames()
                .Where(name => name.Contains("StoryCADLib.Assets.Install.reports"));
            foreach (var FileName in Files.ToList())
            {
                //Read from manifest stream
                await using var streams = Assembly.GetExecutingAssembly().GetManifestResourceStream(FileName);
                using StreamReader reader = new(streams!);

                //Gets the stream, then formats it into line by line.
                var lines = (await reader.ReadToEndAsync())
                    .Split(new[] { Environment.NewLine }, StringSplitOptions.None);


                _templates.Add(FileName.Split(".")[4], lines);
            }
        }
        catch (Exception ex)
        {
            Ioc.Default.GetService<ILogService>()?.LogException(LogLevel.Error, ex, "Error loading report templates.");
        }
    }

    #endregion

    #region Private methods

    /// <summary>
    ///     A RichEditBox property is a wrapper for an RTF
    ///     document, with its header, font table, color table, etc.,
    ///     and which can be read or written. This causes format problems
    ///     when it's a cell on a StoryCAD report.  This function
    ///     returns only the text, but does preserve newlines as
    ///     paragraph breaks.
    /// </summary>
    public string GetText(string rtfInput, bool formatNewLines = true)
    {
        var text = rtfInput ?? string.Empty;
        if (text.Equals(string.Empty))
        {
            return string.Empty;
        }

        RichTextStripper rts = new();
        text = rts.StripRichTextFormat(text);
        text = text.Replace("\'0d", "");
        text = text.Replace("\'0a", "");
        text = text.Replace("\\", "");
        return text;
    }

    /// <summary>
    ///     Generate a UUID in the Scrivener format (i.e., without curly braces)
    /// </summary>
    /// <returns>string UUID representation</returns>

    #endregion

    #region Constructor

    public ReportFormatter(AppState appState)
    {
        _storyModel = appState.CurrentDocument!.Model;
    }

    #endregion
}
