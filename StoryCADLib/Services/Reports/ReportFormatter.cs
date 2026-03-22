using System.Reflection;
using System.Text;
using NRtfTree.Util;
using StoryCADLib.Models.StoryWorld;
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

    public async Task<string> FormatStoryWorldReport()
    {
        await EnsureTemplatesLoadedAsync();
        var world = (StoryWorldModel)_storyModel.StoryElements.FirstOrDefault(element =>
            element.ElementType == StoryItemType.StoryWorld);

        if (world == null)
        {
            // No StoryWorld element exists, return empty RTF
            RtfDocument emptyDoc = new(string.Empty);
            emptyDoc.AddText("No Story World defined.");
            return emptyDoc.GetRtf();
        }

        var lines = _templates["Story World"];
        RtfDocument doc = new(string.Empty);

        // Parse and write the report
        foreach (var line in lines)
        {
            StringBuilder sb = new(line);

            // World Structure section
            if (line.Contains("@WorldStructure"))
            {
                sb.Clear();
                if (!string.IsNullOrEmpty(world.WorldType))
                {
                    sb.AppendLine($"    World Type: {world.WorldType}");
                    sb.AppendLine();
                    var (description, examples) = GetWorldTypeInfo(world.WorldType);
                    if (!string.IsNullOrEmpty(description))
                    {
                        sb.AppendLine($"    {description}");
                        sb.AppendLine();
                    }
                    if (!string.IsNullOrEmpty(examples))
                    {
                        sb.AppendLine($"    Works in this category: {examples}");
                    }
                }
            }

            // Physical Worlds section
            else if (line.Contains("@PhysicalWorlds"))
            {
                sb.Clear();
                foreach (var entry in world.PhysicalWorlds)
                {
                    sb.AppendLine($"    --- {entry.Name ?? "Unnamed"} ---");
                    if (!string.IsNullOrEmpty(entry.Geography)) sb.AppendLine($"    Geography: {GetText(entry.Geography)}");
                    if (!string.IsNullOrEmpty(entry.Climate)) sb.AppendLine($"    Climate: {GetText(entry.Climate)}");
                    if (!string.IsNullOrEmpty(entry.NaturalResources)) sb.AppendLine($"    Natural Resources: {GetText(entry.NaturalResources)}");
                    if (!string.IsNullOrEmpty(entry.Flora)) sb.AppendLine($"    Flora: {GetText(entry.Flora)}");
                    if (!string.IsNullOrEmpty(entry.Fauna)) sb.AppendLine($"    Fauna: {GetText(entry.Fauna)}");
                    if (!string.IsNullOrEmpty(entry.Astronomy)) sb.AppendLine($"    Astronomy: {GetText(entry.Astronomy)}");
                    sb.AppendLine();
                }
            }

            // Species section
            else if (line.Contains("@Species"))
            {
                sb.Clear();
                foreach (var entry in world.Species)
                {
                    sb.AppendLine($"    --- {entry.Name ?? "Unnamed"} ---");
                    if (!string.IsNullOrEmpty(entry.PhysicalTraits)) sb.AppendLine($"    Physical Traits: {GetText(entry.PhysicalTraits)}");
                    if (!string.IsNullOrEmpty(entry.Lifespan)) sb.AppendLine($"    Lifespan: {GetText(entry.Lifespan)}");
                    if (!string.IsNullOrEmpty(entry.Origins)) sb.AppendLine($"    Origins: {GetText(entry.Origins)}");
                    if (!string.IsNullOrEmpty(entry.SocialStructure)) sb.AppendLine($"    Social Structure: {GetText(entry.SocialStructure)}");
                    if (!string.IsNullOrEmpty(entry.Diversity)) sb.AppendLine($"    Diversity: {GetText(entry.Diversity)}");
                    sb.AppendLine();
                }
            }

            // Cultures section
            else if (line.Contains("@Cultures"))
            {
                sb.Clear();
                foreach (var entry in world.Cultures)
                {
                    sb.AppendLine($"    --- {entry.Name ?? "Unnamed"} ---");
                    if (!string.IsNullOrEmpty(entry.Values)) sb.AppendLine($"    Values: {GetText(entry.Values)}");
                    if (!string.IsNullOrEmpty(entry.Customs)) sb.AppendLine($"    Customs: {GetText(entry.Customs)}");
                    if (!string.IsNullOrEmpty(entry.Taboos)) sb.AppendLine($"    Taboos: {GetText(entry.Taboos)}");
                    if (!string.IsNullOrEmpty(entry.Art)) sb.AppendLine($"    Art: {GetText(entry.Art)}");
                    if (!string.IsNullOrEmpty(entry.DailyLife)) sb.AppendLine($"    Daily Life: {GetText(entry.DailyLife)}");
                    if (!string.IsNullOrEmpty(entry.Entertainment)) sb.AppendLine($"    Entertainment: {GetText(entry.Entertainment)}");
                    sb.AppendLine();
                }
            }

            // Governments section
            else if (line.Contains("@Governments"))
            {
                sb.Clear();
                foreach (var entry in world.Governments)
                {
                    sb.AppendLine($"    --- {entry.Name ?? "Unnamed"} ---");
                    if (!string.IsNullOrEmpty(entry.Type)) sb.AppendLine($"    Type: {GetText(entry.Type)}");
                    if (!string.IsNullOrEmpty(entry.PowerStructures)) sb.AppendLine($"    Power Structures: {GetText(entry.PowerStructures)}");
                    if (!string.IsNullOrEmpty(entry.Laws)) sb.AppendLine($"    Laws: {GetText(entry.Laws)}");
                    if (!string.IsNullOrEmpty(entry.ClassStructure)) sb.AppendLine($"    Class Structure: {GetText(entry.ClassStructure)}");
                    if (!string.IsNullOrEmpty(entry.ForeignRelations)) sb.AppendLine($"    Foreign Relations: {GetText(entry.ForeignRelations)}");
                    sb.AppendLine();
                }
            }

            // Religions section
            else if (line.Contains("@Religions"))
            {
                sb.Clear();
                foreach (var entry in world.Religions)
                {
                    sb.AppendLine($"    --- {entry.Name ?? "Unnamed"} ---");
                    if (!string.IsNullOrEmpty(entry.Deities)) sb.AppendLine($"    Deities: {GetText(entry.Deities)}");
                    if (!string.IsNullOrEmpty(entry.Beliefs)) sb.AppendLine($"    Beliefs: {GetText(entry.Beliefs)}");
                    if (!string.IsNullOrEmpty(entry.Practices)) sb.AppendLine($"    Practices: {GetText(entry.Practices)}");
                    if (!string.IsNullOrEmpty(entry.Organizations)) sb.AppendLine($"    Organizations: {GetText(entry.Organizations)}");
                    if (!string.IsNullOrEmpty(entry.CreationMyths)) sb.AppendLine($"    Creation Myths: {GetText(entry.CreationMyths)}");
                    sb.AppendLine();
                }
            }

            // History section
            else if (line.Contains("@History"))
            {
                sb.Clear();
                if (!string.IsNullOrEmpty(world.FoundingEvents)) sb.AppendLine($"    Founding Events: {GetText(world.FoundingEvents)}");
                if (!string.IsNullOrEmpty(world.MajorConflicts)) sb.AppendLine($"    Major Conflicts: {GetText(world.MajorConflicts)}");
                if (!string.IsNullOrEmpty(world.Eras)) sb.AppendLine($"    Eras: {GetText(world.Eras)}");
                if (!string.IsNullOrEmpty(world.TechnologicalShifts)) sb.AppendLine($"    Technological Shifts: {GetText(world.TechnologicalShifts)}");
                if (!string.IsNullOrEmpty(world.LostKnowledge)) sb.AppendLine($"    Lost Knowledge: {GetText(world.LostKnowledge)}");
            }

            // Economy section
            else if (line.Contains("@Economy"))
            {
                sb.Clear();
                if (!string.IsNullOrEmpty(world.EconomicSystem)) sb.AppendLine($"    Economic System: {GetText(world.EconomicSystem)}");
                if (!string.IsNullOrEmpty(world.Currency)) sb.AppendLine($"    Currency: {GetText(world.Currency)}");
                if (!string.IsNullOrEmpty(world.TradeRoutes)) sb.AppendLine($"    Trade Routes: {GetText(world.TradeRoutes)}");
                if (!string.IsNullOrEmpty(world.Professions)) sb.AppendLine($"    Professions: {GetText(world.Professions)}");
                if (!string.IsNullOrEmpty(world.WealthDistribution)) sb.AppendLine($"    Wealth Distribution: {GetText(world.WealthDistribution)}");
            }

            // Magic/Technology section
            else if (line.Contains("@MagicTechnology"))
            {
                sb.Clear();
                if (!string.IsNullOrEmpty(world.SystemType)) sb.AppendLine($"    System Type: {world.SystemType}");
                if (!string.IsNullOrEmpty(world.Source)) sb.AppendLine($"    Source: {GetText(world.Source)}");
                if (!string.IsNullOrEmpty(world.Rules)) sb.AppendLine($"    Rules: {GetText(world.Rules)}");
                if (!string.IsNullOrEmpty(world.Limitations)) sb.AppendLine($"    Limitations: {GetText(world.Limitations)}");
                if (!string.IsNullOrEmpty(world.Cost)) sb.AppendLine($"    Cost: {GetText(world.Cost)}");
                if (!string.IsNullOrEmpty(world.Practitioners)) sb.AppendLine($"    Practitioners: {GetText(world.Practitioners)}");
                if (!string.IsNullOrEmpty(world.SocialImpact)) sb.AppendLine($"    Social Impact: {GetText(world.SocialImpact)}");
            }

            doc.AddText(sb.ToString());
            doc.AddNewLine();
        }

        return doc.GetRtf();
    }

    /// <summary>
    /// Returns description and examples for a World Type.
    /// </summary>
    private static (string Description, string Examples) GetWorldTypeInfo(string worldType)
    {
        return worldType switch
        {
            "Consensus Reality" => (
                "The world operates exactly as expected - no magic, no hidden layers, no alternate physics. " +
                "\"Consensus\" refers to a specific group or subculture whose reality you're depicting.",
                "87th Precinct • Harry Bosch • Rabbit series • Grisham novels • Big Little Lies"),

            "Enchanted Reality" => (
                "Our world, but reality is porous. The impossible happens and is accepted rather than analyzed. " +
                "Magic or the supernatural exists but isn't systematized - it simply is.",
                "One Hundred Years of Solitude • Like Water for Chocolate • Beloved • Pan's Labyrinth"),

            "Hidden World" => (
                "Our world, but with concealed magical or supernatural layers beneath or alongside normal reality. " +
                "The \"mundane\" world operates normally, but a secret realm exists that most people don't know about.",
                "Harry Potter • Dresden Files • American Gods • The Matrix • Men in Black • Percy Jackson"),

            "Divergent World" => (
                "Our world, but history or conditions diverged at some point. " +
                "The rules of reality remain rational and logical, but society, technology, or events developed differently.",
                "The Man in the High Castle • 11/22/63 • Neuromancer • The Handmaid's Tale"),

            "Constructed World" => (
                "A fully invented reality with its own geography, history, peoples, and rules. " +
                "The world doesn't derive from Earth at all - it's built from scratch.",
                "A Song of Ice and Fire • Discworld • Dune • Star Wars • The Stormlight Archive"),

            "Mythic World" => (
                "A world where narrative meaning matters more than physical causality. " +
                "Fate, prophecy, and archetypes drive events. Things happen because they're meaningful, not because of cause and effect.",
                "The Lord of the Rings • Earthsea • The Chronicles of Narnia • Circe • The Once and Future King"),

            "Estranged World" => (
                "A world that feels fundamentally alien or wrong. Rules may exist, but they resist human intuition. " +
                "The familiar becomes strange.",
                "Solaris • Annihilation • Perdido Street Station • Blindsight • 2001: A Space Odyssey"),

            "Broken World" => (
                "A world where civilization, environment, or social order has collapsed or been corrupted. " +
                "Survival replaces progress. Resources are scarce, institutions have failed.",
                "The Road • Mad Max • 1984 • The Walking Dead • Station Eleven • A Canticle for Leibowitz"),

            _ => ("", "")
        };
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

    private void ProcessBeat(StructureBeat beat, StringBuilder output, StoryModel storyModel, int indentLevel,
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

    internal string FormatStructureBeatsElements(ProblemModel problem)
    {
        StringBuilder beats = new();

        // Build set of template beat titles for custom beat detection
        var beatSheets = Ioc.Default.GetService<BeatSheetsViewModel>();
        HashSet<string> templateTitles = new();
        if (beatSheets != null &&
            !string.IsNullOrEmpty(problem.StructureTitle) &&
            beatSheets.BeatSheets.TryGetValue(problem.StructureTitle, out var template))
        {
            templateTitles = template.PlotPatternScenes.Select(s => s.SceneTitle).ToHashSet();
        }

        bool hasCustomBeats = false;
        int beatIndex = 0;
        foreach (var beat in problem.StructureBeats)
        {
            beatIndex++;
            bool isCustom = !templateTitles.Contains(beat.Title);
            if (isCustom) hasCustomBeats = true;

            // Numbered title with custom marker
            var marker = isCustom ? " *" : "";
            beats.AppendLine($"{beatIndex}. {beat.Title}{marker}");
            beats.AppendLine(beat.Description);

            // Show element assignment or "Unassigned"
            if (beat.Guid != Guid.Empty)
            {
                beats.AppendLine(beat.ElementName);
                beats.AppendLine(beat.ElementDescription);
            }
            else
            {
                beats.AppendLine("Unassigned");
            }

            beats.AppendLine("\t\t-------------");
        }

        // Legend
        if (hasCustomBeats)
        {
            beats.AppendLine("* = custom beat (not from template)");
        }

        return beats.ToString();
    }

    /// <summary>
    ///     Hierarchical view of how problems connect through beat sheets.
    ///     Shows only problems (no scenes, no descriptions) — the structural skeleton.
    /// </summary>
    public string FormatPlotStructureDiagram()
    {
        if (_storyModel?.StoryElements == null)
            return string.Empty;

        var overview = _storyModel.StoryElements
            .OfType<OverviewModel>()
            .FirstOrDefault(e => e.ElementType == StoryItemType.StoryOverview);

        if (overview == null || overview.StoryProblem == Guid.Empty)
            return string.Empty;

        if (!_storyModel.StoryElements.StoryElementGuids.TryGetValue(overview.StoryProblem, out var rootElement) ||
            rootElement is not ProblemModel storyProblem)
            return string.Empty;

        var output = new StringBuilder();
        output.AppendLine("Plot Structure Diagram");
        output.AppendLine("======================");
        output.AppendLine();

        var processedElements = new HashSet<Guid>();
        BuildPlotDiagram(storyProblem, output, 0, processedElements);

        return output.ToString();
    }

    private void BuildPlotDiagram(ProblemModel problem, StringBuilder output, int indentLevel,
        HashSet<Guid> processedElements)
    {
        if (processedElements.Contains(problem.Uuid))
            return;
        processedElements.Add(problem.Uuid);

        var indent = new string('\t', indentLevel);
        output.AppendLine($"{indent}{problem.Name}");

        if (problem.StructureBeats == null || !problem.StructureBeats.Any())
            return;

        foreach (var beat in problem.StructureBeats)
        {
            if (beat.Guid == Guid.Empty)
                continue;

            // Only show beats that link to problems (skip scenes)
            if (_storyModel.StoryElements.StoryElementGuids.TryGetValue(beat.Guid, out var element) &&
                element is ProblemModel subProblem)
            {
                output.AppendLine($"{indent}\t{beat.Title} \u2192 {subProblem.Name}");
                BuildPlotDiagram(subProblem, output, indentLevel + 2, processedElements);
            }
        }
    }

    /// <summary>
    ///     Lists problems and scenes not assigned to any beat sheet.
    /// </summary>
    public string FormatUnassignedElementsReport()
    {
        var output = new StringBuilder();
        output.AppendLine("StoryCAD - Unassigned Elements Report");
        output.AppendLine();

        // Collect all scene GUIDs that are referenced in any beat sheet
        var assignedSceneGuids = new HashSet<Guid>();
        foreach (var element in _storyModel.StoryElements)
        {
            if (element is ProblemModel problem)
            {
                foreach (var beat in problem.StructureBeats)
                {
                    if (beat.Guid != Guid.Empty)
                    {
                        assignedSceneGuids.Add(beat.Guid);
                    }
                }
            }
        }

        // Unassigned Problems (not bound as a beat in any other problem's beat sheet)
        output.AppendLine("============== Unassigned Problems =============");
        var unassignedProblems = _storyModel.StoryElements
            .OfType<ProblemModel>()
            .Where(p => string.IsNullOrEmpty(p.BoundStructure))
            .ToList();

        // Exclude the Story Problem (it's the root, not an orphan)
        var overview = _storyModel.StoryElements
            .OfType<OverviewModel>()
            .FirstOrDefault();
        if (overview != null && overview.StoryProblem != Guid.Empty)
        {
            unassignedProblems.RemoveAll(p => p.Uuid == overview.StoryProblem);
        }

        if (unassignedProblems.Any())
        {
            foreach (var problem in unassignedProblems)
            {
                output.AppendLine($"  {problem.Name}");
            }
        }
        else
        {
            output.AppendLine("  None");
        }

        output.AppendLine();

        // Unassigned Scenes (not referenced by any beat in any problem)
        output.AppendLine("============== Unassigned Scenes =============");
        var unassignedScenes = _storyModel.StoryElements
            .OfType<SceneModel>()
            .Where(s => !assignedSceneGuids.Contains(s.Uuid))
            .ToList();

        if (unassignedScenes.Any())
        {
            foreach (var scene in unassignedScenes)
            {
                output.AppendLine($"  {scene.Name}");
            }
        }
        else
        {
            output.AppendLine("  None");
        }

        return output.ToString();
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
