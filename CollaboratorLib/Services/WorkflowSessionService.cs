using System.Linq;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using NLog.Extensions.Logging;
using System.Net;
using StoryCADLib.Models;
using StoryCADLib.Services.Store;
using StoryCADLib.Services.Collaborator.Contracts;
using StoryCollaborator.Models;
using StoryCollaborator.Workflows;

namespace StoryCollaborator.Services;

/// <summary>An input the service cannot find on its own. The caller may ask the writer for it.</summary>
public sealed class InputPrompt
{
    internal InputPrompt(ElementRequirement requirement, Guid? preselect, bool isRequired)
    {
        Requirement = requirement;
        Preselect = preselect;
        IsRequired = isRequired;
    }

    /// <summary>The input.</summary>
    public ElementRequirement Requirement { get; }

    /// <summary>The element that the input's reference points to, if the reference is set.</summary>
    public Guid? Preselect { get; }

    /// <summary>True for a required input.</summary>
    public bool IsRequired { get; }

    /// <summary>The writer's choice. Null means they cancelled.</summary>
    internal Guid? Answer { get; set; }
}

/// <summary>What <see cref="WorkflowSessionService.ResolveInputs"/> found.</summary>
public sealed class InputResolution
{
    public Dictionary<string, StoryElement> Elements { get; } = new();
    public List<string> StatusMessages { get; } = new();

    /// <summary>The labels of the required inputs that could not be found.</summary>
    public List<string> Missing { get; } = new();

    /// <summary>A required input could not be found and the workflow cannot create it.</summary>
    public bool Cancelled { get; set; }

    public bool Failed { get; set; }
    public string? BailReason { get; set; }
}

/// <summary>
/// Collaborator #291: the session logic of a Collaborator run. It keeps no state; every call
/// gets the <see cref="WorkflowSession"/> as its first argument. The Collaborator window
/// reaches it through <see cref="WorkflowViewModelWiring"/>, and the MCP server calls it
/// directly. This is the Stateless Services pattern, as <c>OutlineService</c> is for the
/// outline.
/// </summary>
public class WorkflowSessionService
{
    private readonly IStoryCADAPI _api;
    private readonly ILogger<WorkflowSessionService>? _logger;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly StoryCADLib.Services.Logging.ILogService? _auditLogger;

    /// <summary>
    /// The settings that the next run sends. The window changes them while a session is open,
    /// so the owner of the service sets this when they change.
    /// </summary>
    public CollaboratorSettings Settings { get; set; }

    /// <param name="api">Every write to the outline goes through it.</param>
    /// <param name="logger">The service log.</param>
    /// <param name="settings">The settings that a run sends.</param>
    /// <param name="loggerFactory">
    /// Makes the logger of each <see cref="WorkflowRunner"/> and <see cref="ElementResolver"/>
    /// the service creates. Null logs nothing from them.
    /// </param>
    /// <param name="auditLogger">StoryCAD's log for the audit events of the session.</param>
    public WorkflowSessionService(
        IStoryCADAPI api,
        ILogger<WorkflowSessionService>? logger,
        CollaboratorSettings settings,
        ILoggerFactory? loggerFactory = null,
        StoryCADLib.Services.Logging.ILogService? auditLogger = null)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _logger = logger;
        Settings = settings ?? CollaboratorSettings.Default;
        _loggerFactory = loggerFactory;
        _auditLogger = auditLogger;
    }

    #region Messages

    private static void Status(WorkflowSession session, string text) =>
        session.Messages.Add(new SessionMessage(SessionMessageKind.Status, text));

    private static void Say(WorkflowSession session, string text) =>
        session.Messages.Add(new SessionMessage(SessionMessageKind.Collaborator, text));

    private static void Fail(WorkflowSession session, string text)
    {
        session.SetLastError(text);
        session.Messages.Add(new SessionMessage(SessionMessageKind.Error, text));
    }

    private static void Changed(WorkflowSession session) => session.Changed?.Invoke();

    #endregion

    #region Inputs

    /// <summary>
    /// Finds each input element of the workflow without asking the writer. The elements go in
    /// <see cref="WorkflowSession.Elements"/>; the labels of the required inputs that cannot be
    /// found are in <see cref="InputResolution.Missing"/>.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="element">
    /// The element the run is about. It fills the first input of its type that <paramref name="given"/>
    /// does not name.
    /// </param>
    /// <param name="given">Input label to element GUID.</param>
    public InputResolution ResolveInputs(
        WorkflowSession session,
        StoryElement? element,
        IReadOnlyDictionary<string, Guid>? given)
    {
        var result = new InputResolution();
        session.Elements = result.Elements;

        // Without a picker the steps never stop to ask, so running them to the end is the whole gather.
        foreach (var _ in GatherSteps(session, element, given, result, interactive: false))
        {
        }

        return result;
    }

    /// <summary>
    /// As <see cref="ResolveInputs"/>, but asks <paramref name="pick"/> for an input that it cannot
    /// find. The window gives the element picker. A null result from <paramref name="pick"/> means
    /// the writer cancelled. With a picker, the first required input that stays empty ends the
    /// gather, because the writer is not asked for the rest.
    /// </summary>
    public async Task<InputResolution> ResolveInputsAsync(
        WorkflowSession session,
        StoryElement? element,
        IReadOnlyDictionary<string, Guid>? given,
        Func<InputPrompt, Task<Guid?>>? pick)
    {
        var result = new InputResolution();
        session.Elements = result.Elements;

        foreach (var prompt in GatherSteps(session, element, given, result, interactive: pick != null))
            prompt.Answer = await pick!(prompt);

        return result;
    }

    /// <summary>
    /// The one gather loop. It runs to the end by itself, and stops at each input it cannot find
    /// when <paramref name="interactive"/> is true: the caller sets <see cref="InputPrompt.Answer"/>
    /// and asks for the next step. The next input may refer to the element that was just picked,
    /// so the steps stay in order.
    /// </summary>
    private IEnumerable<InputPrompt> GatherSteps(
        WorkflowSession session,
        StoryElement? element,
        IReadOnlyDictionary<string, Guid>? given,
        InputResolution result,
        bool interactive)
    {
        var workflowIO = session.Workflow.GetIO();
        var state = new GatherState(element, given);

        // Add section header
        result.StatusMessages.Add("--- Gathering input elements ---");

        // Phase 1: Gather required inputs
        foreach (var requirement in workflowIO.RequiredInputs)
        {
            var outcome = new InputOutcome();
            foreach (var prompt in ResolveInput(session, requirement, state, result, true, interactive, outcome))
                yield return prompt;

            if (outcome.Element == null)
            {
                result.Missing.Add(requirement.ElementLabel);
                if (!requirement.CreateIfMissing)
                {
                    result.Cancelled = true;
                    result.StatusMessages.Add($"Cancelled: {requirement.ElementLabel} is required.");
                    if (interactive)
                        yield break;
                }
            }
        }

        // Phase 2: Gather optional inputs - try reference first, then picker fallback
        foreach (var requirement in workflowIO.OptionalInputs)
        {
            // Skip if already gathered (e.g., duplicate label in required)
            if (result.Elements.ContainsKey(requirement.ElementLabel))
                continue;

            foreach (var prompt in ResolveInput(session, requirement, state, result, false, interactive, new InputOutcome()))
                yield return prompt;
        }

        if (result.Cancelled)
            yield break;

        if (string.Equals(session.Workflow.Label, "SceneBuilder", StringComparison.Ordinal)
            && result.Elements.TryGetValue("Scene", out var sceneBuilderScene))
        {
            InjectSceneBuilder(session, sceneBuilderScene, result);
        }

        // #201: Story Problem + seats when Overview.StoryProblem resolves (thin gather).
        if (string.Equals(session.Workflow.Label, "DefineStoryWorld", StringComparison.Ordinal))
        {
            InjectStoryProblemSeatsForWorld(result);
        }
    }

    /// <summary>What the caller gave: the element the run is about and the named inputs.</summary>
    private sealed class GatherState
    {
        public StoryElement? Primary;
        public readonly IReadOnlyDictionary<string, Guid> Given;

        public GatherState(StoryElement? primary, IReadOnlyDictionary<string, Guid>? given)
        {
            Primary = primary;
            Given = given ?? new Dictionary<string, Guid>();
        }
    }

    /// <summary>The element that one input step found, or null.</summary>
    private sealed class InputOutcome
    {
        public StoryElement? Element;
    }

    /// <summary>
    /// Gathers a single element based on its requirement.
    /// Uses ElementResolver for auto-resolution (StoryOverview, referenced elements).
    /// For required inputs: uses auto-resolved element if available, otherwise asks the writer.
    /// For optional inputs: always asks the writer with the current selection pre-selected.
    /// </summary>
    private IEnumerable<InputPrompt> ResolveInput(
        WorkflowSession session,
        ElementRequirement requirement,
        GatherState state,
        InputResolution result,
        bool isRequired,
        bool interactive,
        InputOutcome outcome)
    {
        var gatheredElements = result.Elements;
        var statusMessages = result.StatusMessages;

        // What the caller named comes first.
        StoryElement? supplied = null;
        if (state.Given.TryGetValue(requirement.ElementLabel, out var givenGuid))
        {
            var got = _api.GetStoryElement(givenGuid);
            if (got.IsSuccess && got.Payload != null)
                supplied = got.Payload;
            else
                statusMessages.Add($"{requirement.ElementLabel}: no element has the GUID {givenGuid}");
        }
        else if (state.Primary != null && state.Primary.ElementType == requirement.ElementType)
        {
            supplied = state.Primary;
            state.Primary = null;
        }

        if (supplied != null)
        {
            gatheredElements[requirement.ElementLabel] = supplied;
            statusMessages.Add($"Using {requirement.ElementLabel}: {supplied.Name}");
            outcome.Element = supplied;
            yield break;
        }

        var resolver = new ElementResolver(_api, _loggerFactory?.CreateLogger<ElementResolver>());

        // Try auto-resolution via ElementResolver (handles StoryOverview and other singletons)
        var autoResolved = resolver.ResolveRequirement(requirement, gatheredElements);
        if (autoResolved != null)
        {
            gatheredElements[requirement.ElementLabel] = autoResolved;
            statusMessages.Add($"Using {requirement.ElementLabel}: {autoResolved.Name}");
            _logger?.LogDebug("Auto-resolved {Label} to '{Name}'",
                requirement.ElementLabel, autoResolved.Name);
            outcome.Element = autoResolved;
            yield break;
        }

        // #201: StoryWorld is a singleton. Create when missing; never ElementPicker.
        if (requirement.ElementType == StoryItemType.StoryWorld)
        {
            if (requirement.CreateIfMissing)
            {
                var created = TryCreateStoryWorldSingleton(session, statusMessages);
                if (created != null)
                {
                    gatheredElements[requirement.ElementLabel] = created;
                    outcome.Element = created;
                    yield break;
                }
            }

            statusMessages.Add($"{requirement.ElementLabel}: StoryWorld missing and could not be created");
            _logger?.LogWarning("StoryWorld gather failed (create={Create})", requirement.CreateIfMissing);
            yield break;
        }

        // Try to get referenced element for pre-selection (e.g., "Problem.Protagonist")
        StoryElement? currentElement = null;
        Guid? currentGuid = null;
        if (!string.IsNullOrEmpty(requirement.ReferencedElementLabel))
        {
            currentElement = resolver.GetReferencedElement(requirement, gatheredElements);
            if (currentElement != null)
            {
                currentGuid = currentElement.Uuid;

                // For REQUIRED inputs, use existing reference without prompting
                if (isRequired)
                {
                    gatheredElements[requirement.ElementLabel] = currentElement;
                    statusMessages.Add(
                        $"Found {requirement.ElementLabel}: {currentElement.Name} (from {requirement.ReferencedElementLabel})");
                    _logger?.LogDebug("Resolved {Label} via {Ref} to '{Name}'",
                        requirement.ElementLabel, requirement.ReferencedElementLabel, currentElement.Name);
                    if (string.Equals(
                            requirement.ReferencedElementLabel, "Overview.StoryProblem",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        ApplyStoryProblemCategory(currentElement.Uuid, statusMessages);
                    }
                    outcome.Element = currentElement;
                    yield break;
                }

                // For OPTIONAL inputs, the picker shows with pre-selection
                _logger?.LogDebug("Found current {Label}: '{Name}', asking for a choice",
                    requirement.ElementLabel, currentElement.Name);
            }
            else
            {
                // Reference empty/missing — user-visible (#123); then picker for required
                // (or optional) so we never run with a silent empty character.
                statusMessages.Add(
                    $"{requirement.ReferencedElementLabel} is not set — select {requirement.ElementLabel}.");
                _logger?.LogDebug("Reference {Ref} empty, will prompt for {Label}",
                    requirement.ReferencedElementLabel, requirement.ElementLabel);
            }
        }

        // Nobody to ask: an optional input takes the element its reference points to.
        if (!interactive)
        {
            if (!isRequired && currentElement != null)
            {
                gatheredElements[requirement.ElementLabel] = currentElement;
                statusMessages.Add($"Using {requirement.ElementLabel}: {currentElement.Name}");
                outcome.Element = currentElement;
            }

            yield break;
        }

        var prompt = new InputPrompt(requirement, currentGuid, isRequired);
        yield return prompt;
        var selectedGuid = prompt.Answer;

        if (selectedGuid == null)
        {
            // User cancelled
            if (isRequired)
            {
                statusMessages.Add($"{requirement.ElementLabel}: selection cancelled");
            }
            else
            {
                statusMessages.Add($"{requirement.ElementLabel}: not selected (optional)");
            }
            _logger?.LogDebug("User cancelled selection for {Label}", requirement.ElementLabel);
            yield break;
        }

        // Look up the selected element
        var lookup = _api.GetStoryElement(selectedGuid.Value);
        if (lookup.IsSuccess && lookup.Payload != null)
        {
            var picked = lookup.Payload;
            gatheredElements[requirement.ElementLabel] = picked;
            statusMessages.Add($"Selected {requirement.ElementLabel}: {picked.Name}");
            _logger?.LogDebug("User selected {Type} '{Name}' for {Label}",
                picked.ElementType, picked.Name, requirement.ElementLabel);

            // If this was a reference fallback, update the source element's Guid property
            if (!string.IsNullOrEmpty(requirement.ReferencedElementLabel))
            {
                UpdateReferenceProperty(requirement.ReferencedElementLabel, gatheredElements, picked.Uuid, statusMessages);
            }

            outcome.Element = picked;
            yield break;
        }

        _logger?.LogWarning("Could not find element with GUID {Guid}", selectedGuid);
    }

    /// <summary>
    /// Updates a Guid reference property on a source element.
    /// For example, "Problem.Protagonist" sets Problem.Protagonist = pickedElementGuid.
    /// </summary>
    private void UpdateReferenceProperty(
        string referencedElementLabel,
        Dictionary<string, StoryElement> gatheredElements,
        Guid pickedElementGuid,
        List<string> statusMessages)
    {
        var parts = referencedElementLabel.Split('.');
        if (parts.Length != 2)
        {
            _logger?.LogWarning("Invalid reference format for update: {Reference}", referencedElementLabel);
            return;
        }

        var sourceLabel = parts[0];
        var propertyName = parts[1];

        if (!gatheredElements.TryGetValue(sourceLabel, out var sourceElement))
        {
            _logger?.LogWarning("Source element '{Label}' not found for property update", sourceLabel);
            return;
        }

        var result = _api.UpdateElementProperty(sourceElement.Uuid, propertyName, pickedElementGuid);
        if (result.IsSuccess)
        {
            statusMessages.Add($"  (Updated {sourceLabel}.{propertyName})");
            _logger?.LogDebug("Updated {Source}.{Property} = {Guid}", sourceLabel, propertyName, pickedElementGuid);

            // Linking Overview.StoryProblem means this Problem is the main story problem —
            // ProblemCategory is that structural fact (Lists.json), not an Accept/Protect field.
            if (string.Equals(sourceLabel, "Overview", StringComparison.OrdinalIgnoreCase)
                && string.Equals(propertyName, "StoryProblem", StringComparison.OrdinalIgnoreCase))
            {
                ApplyStoryProblemCategory(pickedElementGuid, statusMessages);
            }
        }
        else
        {
            _logger?.LogWarning("Failed to update {Source}.{Property}: {Error}",
                sourceLabel, propertyName, result.ErrorMessage);
        }
    }

    /// <summary>
    /// Writes Problem.ProblemCategory = "Story problem" immediately (not pending / not Accept).
    /// Call whenever Overview.StoryProblem is linked to this Problem.
    /// </summary>
    internal void ApplyStoryProblemCategory(Guid problemGuid, List<string>? statusMessages = null)
    {
        var catResult = _api.UpdateElementProperty(
            problemGuid, "ProblemCategory", StoryProblemCategoryListValue);
        if (catResult.IsSuccess)
        {
            statusMessages?.Add($"  (Set Problem.ProblemCategory = {StoryProblemCategoryListValue})");
            _logger?.LogDebug(
                "Set Problem {Guid} ProblemCategory = {Category}",
                problemGuid, StoryProblemCategoryListValue);
        }
        else
        {
            _logger?.LogWarning(
                "Failed to set ProblemCategory on {Guid}: {Error}",
                problemGuid, catResult.ErrorMessage);
        }
    }

    /// <summary>
    /// Collaborator #201: inject Problem / Protagonist / Antagonist when Overview links them.
    /// Does not create elements. Does not open pickers.
    /// </summary>
    private void InjectStoryProblemSeatsForWorld(InputResolution result)
    {
        if (!result.Elements.TryGetValue("Overview", out var overviewElement)
            || overviewElement is not OverviewModel overview)
            return;

        if (overview.StoryProblem == Guid.Empty)
            return;

        var problemResult = _api.GetStoryElement(overview.StoryProblem);
        if (!problemResult.IsSuccess || problemResult.Payload is not ProblemModel problem)
            return;

        result.Elements["Problem"] = problem;
        result.StatusMessages.Add($"Using Problem: {problem.Name}");

        if (problem.Protagonist != Guid.Empty)
        {
            var prot = _api.GetStoryElement(problem.Protagonist);
            if (prot.IsSuccess && prot.Payload != null)
            {
                result.Elements["Protagonist"] = prot.Payload;
                result.StatusMessages.Add($"Using Protagonist: {prot.Payload.Name}");
            }
        }

        if (problem.Antagonist != Guid.Empty)
        {
            var antag = _api.GetStoryElement(problem.Antagonist);
            if (antag.IsSuccess && antag.Payload != null)
            {
                result.Elements["Antagonist"] = antag.Payload;
                result.StatusMessages.Add($"Using Antagonist: {antag.Payload.Name}");
            }
        }
    }

    /// <summary>
    /// Collaborator #201: create the StoryWorld singleton under Overview when missing.
    /// </summary>
    private StoryElement? TryCreateStoryWorldSingleton(WorkflowSession session, List<string> statusMessages)
    {
        var existing = _api.GetStoryWorld();
        if (existing.IsSuccess && existing.Payload != null)
            return existing.Payload;

        // Parent: Overview (explorer root).
        Guid overviewGuid = Guid.Empty;
        if (session.Model.ExplorerView.Count > 0)
            overviewGuid = session.Model.ExplorerView[0].Uuid;
        if (overviewGuid == Guid.Empty)
        {
            var overviewList = _api.GetElementsByType(StoryItemType.StoryOverview);
            if (overviewList.IsSuccess && overviewList.Payload is { Count: > 0 })
                overviewGuid = overviewList.Payload[0].Uuid;
        }

        if (overviewGuid == Guid.Empty)
        {
            statusMessages.Add("Cannot create StoryWorld: Overview not found");
            return null;
        }

        var add = _api.AddElement(StoryItemType.StoryWorld, overviewGuid.ToString(), "Story World");
        if (!add.IsSuccess)
        {
            statusMessages.Add($"Cannot create StoryWorld: {add.ErrorMessage}");
            _logger?.LogWarning("AddElement StoryWorld failed: {Error}", add.ErrorMessage);
            return null;
        }

        var created = _api.GetStoryElement(add.Payload);
        if (!created.IsSuccess || created.Payload == null)
        {
            statusMessages.Add("StoryWorld was created but could not be reloaded");
            return null;
        }

        statusMessages.Add($"Created StoryWorld: {created.Payload.Name}");
        _logger?.LogInformation("Created StoryWorld {Guid}", created.Payload.Uuid);
        return created.Payload;
    }

    /// <summary>
    /// Collaborator #208 / #246: inject Overview, owner, contributing Problems, neighbors, seats, Setting.
    /// Sets Failed on empty-category bail. Does not open pickers.
    /// </summary>
    private void InjectSceneBuilder(WorkflowSession session, StoryElement sceneElement, InputResolution result)
    {
        InjectOverviewIfMissing(result);

        var resolver = new SceneStructureNeighborResolver(_api);
        var resolved = resolver.ResolveForSceneBuilder(sceneElement);

        foreach (var line in resolved.StatusLines)
            result.StatusMessages.Add(line);

        if (resolved.OwnerState is SceneStructureNeighborResolver.SceneBuilderOwnerState.EmptyCategoryBail)
        {
            result.Failed = true;
            result.BailReason = resolved.BailReason;
            return;
        }

        if (resolved.OwnerProblem != null)
            result.Elements["Problem"] = resolved.OwnerProblem;
        if (resolved.PrecedingScene != null)
            result.Elements["PrecedingScene"] = resolved.PrecedingScene;
        if (resolved.NextScene != null)
            result.Elements["NextScene"] = resolved.NextScene;

        InjectSceneBuilderSeatsAndCast(sceneElement, result);
        InjectSceneBuilderContributingLabels(sceneElement, resolver, resolved, result);
        InjectStoryProblemAncestor(result);
    }

    private void InjectOverviewIfMissing(InputResolution result)
    {
        if (result.Elements.ContainsKey("Overview"))
            return;

        var overviews = _api.GetElementsByType(StoryItemType.StoryOverview);
        if (!overviews.IsSuccess || overviews.Payload == null || overviews.Payload.Count == 0)
            return;

        result.Elements["Overview"] = overviews.Payload[0];
        result.StatusMessages.Add($"Using Overview: {overviews.Payload[0].Name}");
    }

    private void InjectStoryProblemAncestor(InputResolution result)
    {
        if (!result.Elements.TryGetValue("Overview", out var overviewEl)
            || overviewEl is not OverviewModel overview
            || overview.StoryProblem == Guid.Empty)
            return;
        if (result.Elements.TryGetValue("Problem", out var owner)
            && owner.Uuid == overview.StoryProblem)
            return;

        var sp = _api.GetStoryElement(overview.StoryProblem);
        if (!sp.IsSuccess || sp.Payload is not ProblemModel problem)
            return;

        result.Elements["StoryProblem"] = problem;
        result.StatusMessages.Add($"Using StoryProblem (ancestor): {problem.Name}");
    }

    private void InjectSceneBuilderSeatsAndCast(StoryElement sceneElement, InputResolution result)
    {
        if (sceneElement is not SceneModel scene)
            return;

        if (scene.Setting != Guid.Empty)
        {
            var settingResult = _api.GetStoryElement(scene.Setting);
            if (settingResult.IsSuccess && settingResult.Payload is SettingModel setting)
            {
                result.Elements["Setting"] = setting;
                result.StatusMessages.Add($"Using Setting: {setting.Name}");
            }
        }

        var used = new HashSet<Guid>();
        TryInjectCharacterSeat(scene.Protagonist, "Protagonist", result, used);
        TryInjectCharacterSeat(scene.Antagonist, "Antagonist", result, used);
        TryInjectCharacterSeat(scene.ViewpointCharacter, "ViewpointCharacter", result, used);

        int n = 1;
        foreach (var guid in scene.CastMembers ?? new List<Guid>())
        {
            if (guid == Guid.Empty || used.Contains(guid))
                continue;
            var member = _api.GetStoryElement(guid);
            if (!member.IsSuccess || member.Payload == null)
                continue;
            result.Elements[$"CastMember{n}"] = member.Payload;
            result.StatusMessages.Add($"Using CastMember{n}: {member.Payload.Name}");
            used.Add(guid);
            n++;
        }
    }

    private void TryInjectCharacterSeat(
        Guid guid,
        string label,
        InputResolution result,
        HashSet<Guid> used)
    {
        if (guid == Guid.Empty)
            return;
        var got = _api.GetStoryElement(guid);
        if (!got.IsSuccess || got.Payload is not CharacterModel character)
            return;
        result.Elements[label] = character;
        result.StatusMessages.Add($"Using {label}: {character.Name}");
        used.Add(guid);
    }

    private void InjectSceneBuilderContributingLabels(
        StoryElement sceneElement,
        SceneStructureNeighborResolver resolver,
        SceneStructureNeighborResolver.SceneBuilderResolveResult resolved,
        InputResolution result)
    {
        var owners = resolved.ContributingProblems.ToList();
        if (owners.Count == 0)
            return;

        var ordered = new List<ProblemModel>();
        void AddIfOwner(ProblemModel? problem)
        {
            if (problem == null)
                return;
            var match = owners.FirstOrDefault(o => o.Uuid == problem.Uuid);
            if (match != null && ordered.All(o => o.Uuid != match.Uuid))
                ordered.Add(match);
        }

        AddIfOwner(resolved.OwnerProblem);
        AddIfOwner(resolver.GetExplorerParentProblem(sceneElement));
        foreach (var owner in owners)
            AddIfOwner(owner);

        const int cap = 8;
        int k = Math.Min(cap, ordered.Count);
        for (int i = 0; i < k; i++)
        {
            result.Elements[$"ContributingProblem{i + 1}"] = ordered[i];
            result.StatusMessages.Add($"Using ContributingProblem{i + 1}: {ordered[i].Name}");
        }

        if (ordered.Count > cap)
        {
            result.StatusMessages.Add(
                $"Scene Builder: {ordered.Count} contributing Problems; labeled first {cap}.");
        }
    }

    #endregion

    #region Run

    private WorkflowRunner NewRunner(WorkflowSession session) =>
        new(session.Model, session.Workflow, _api, _loggerFactory?.CreateLogger<WorkflowRunner>(),
            Settings, _auditLogger)
        {
            // Collaborator #237 item 11: a Try Again carries what the writer rejected.
            RejectedProposals = session.RejectedProposals
        };

    private WorkflowRunner EnsureRunner(WorkflowSession session) =>
        session.Runner ??= NewRunner(session);

    /// <summary>
    /// Collaborator #286: counts whole seconds while a run is in flight and tells the screen each
    /// second, so a long run (Problem Builder took 750 s on 2026-10-23) is seen to be alive.
    /// Ends by itself when <see cref="WorkflowSession.Busy"/> goes false.
    /// </summary>
    private static async Task TickWhileBusyAsync(WorkflowSession session)
    {
        while (session.Busy)
        {
            await Task.Delay(1000);
            if (!session.Busy) break;
            session.RunSeconds++;
            Changed(session);
        }
    }

    /// <summary>
    /// Runs the workflow and puts the result in the session. A failure becomes an error line;
    /// nothing is thrown.
    /// </summary>
    public async Task RunAsync(WorkflowSession session)
    {
        session.SetLastError(null);
        try
        {
            Status(session, $"Running {session.Workflow.Title}...");
            session.Busy = true;
            session.RunSeconds = 0;

            // Cost line describes the run in flight, not the previous one.
            session.PendingCostSummary = string.Empty;
            Changed(session);
            _ = TickWhileBusyAsync(session);

            // Execute via WorkflowRunner
            session.Runner = NewRunner(session);
            _auditLogger?.Log(StoryCADLib.Services.Logging.LogLevel.Info,
                $"Workflow started: {session.Workflow.Title} with {session.Elements.Count} elements");
            var result = await session.Runner.RunAsync(session.Elements, session.ExtraArgs);

            SetResult(session, result);
        }
        catch (Exception ex)
        {
            session.Busy = false;
            Fail(session, $"Error executing workflow: {ex.Message}");
            _logger?.LogError(ex, "Error executing workflow {Workflow}", session.Workflow.Title);
        }

        Changed(session);
    }

    /// <summary>
    /// Takes the result of a run: records the cost, classifies each proposal against the live
    /// outline, and opens the proposal set and the chat. <see cref="RunAsync"/> calls this once
    /// the Worker has answered.
    /// </summary>
    internal void SetResult(WorkflowSession session, WorkflowResult result)
    {
        var runner = EnsureRunner(session);

        // A failed run leaves the last good run's proposals in place (design section 8).
        if (result.Success)
            session.Result = result;

        // Cost line (devdocs/collaborator_workflow_cost_display_design.md).
        // Recorded for every run, priced or not: a null Cost still advances the display
        // to "cost unavailable" rather than leaving the previous run's figure showing.
        session.PendingCostSummary = session.CostTracker.Record(result.Cost);

        // #116: classify scalars against live outline + session-touch (after extract/enrich).
        if (result.Success)
            runner.ClassifyScalarUpdates(result, session.TouchedFields, session.Workflow.Label);

        session.Busy = false;

        if (result.Success)
        {
            // Show status messages rolled up (omit noisy per-field classify lines)
            foreach (var msg in result.StatusMessages)
            {
                if (msg.StartsWith("Classified ", StringComparison.Ordinal)
                    || msg.StartsWith("No-op ", StringComparison.Ordinal))
                    continue;
                Status(session, msg);
            }
            Status(session, "Workflow completed successfully.");

            // Add AI explanation if available in raw response
            if (!string.IsNullOrEmpty(result.RawResponse))
            {
                var explanation = ExtractExplanationFromResponse(result.RawResponse);
                if (!string.IsNullOrEmpty(explanation))
                    Say(session, explanation);
            }

            // If there are property updates, open the proposals
            if (result.PendingUpdates.Count > 0)
            {
                // #140 / #116 rev: Protect accepts are staged until end-of-queue confirm.
                session.Stage = new OverwriteAcceptanceSession();
                session.OrphanBindDone = false;
                session.LastAppliedKeys.Clear();

                var protectCount = result.PendingUpdates.Count(u => u.Kind == UpdateKind.Protect);
                // #145: clear chat, seed proposal set, unlock Send
                BeginProposalChatSession(session, result);
                Say(session, BuildSummaryMessage(
                    result.PendingUpdates.Count, protectCount, result.KeptProperties));
            }
            else
            {
                session.ChatEnabled = false;
                session.ChatPlaceholder = "No proposals to edit in chat";
                Say(session, BuildSummaryMessage(0, 0, result.KeptProperties));
            }
        }
        else
        {
            Fail(session, result.ErrorMessage ?? "Unknown error");
            foreach (var msg in result.StatusMessages)
                Status(session, msg);
        }

        _logger?.LogInformation("Workflow {Workflow} completed. Success: {Success}",
            session.Workflow.Title, result.Success);
    }

    /// <summary>
    /// Collaborator #237 item 11: Try Again means "give me something different". The rows the
    /// writer has not accepted (open and skipped) go to the Worker as what to depart from,
    /// then the workflow runs again. The rows come from the session, not from the screen, so
    /// the top bar and the panel send the same rows.
    /// </summary>
    public async Task TryAgainAsync(WorkflowSession session)
    {
        session.SetLastError(null);
        try
        {
            var rejected = session.Proposals == null
                ? string.Empty
                : WorkflowRunner.FormatRejectedProposals(
                    session.Proposals.All
                        .Where(e => e.Status != ProposalSessionStatus.Accepted)
                        .OrderBy(e => e.Update.Key, StringComparer.OrdinalIgnoreCase)
                        .Select(e => ToSessionPendingUpdateItem(e)));
            session.RejectedProposals = rejected;
            session.Stage.ClearStage();
            Status(session, "Re-running workflow for a different result...");
            // ExtraArgs travels with the re-run: without it the interview
            // summary would run again with an empty transcript and propose
            // the character's history from nothing.
            await RunAsync(session);
        }
        catch (Exception ex)
        {
            Fail(session, $"Error re-running workflow: {ex.Message}");
            _logger?.LogError(ex, "Error in TryAgain handler");
        }
    }

    /// <summary>
    /// #145: clear chat, seed system + proposal snapshot, unlock Send.
    /// Call after the workflow one-shot has classified pending updates.
    /// </summary>
    private void BeginProposalChatSession(WorkflowSession session, WorkflowResult result)
    {
        if (result.PendingUpdates.Count == 0)
        {
            session.ChatEnabled = false;
            session.ChatPlaceholder = "No proposals to edit in chat";
            session.Proposals = null;
            return;
        }

        session.Proposals = new SessionProposalSet();
        session.Proposals.ReplaceFromPending(result.PendingUpdates, guid => ResolveElementName(session.Model, guid));

        session.ChatHistory = new ChatHistory();
        // Collaborator #237 item 3: the chat knows the run's order, so a change to one
        // proposal re-derives the ones written after it.
        session.ChatHistory.AddSystemMessage(SessionProposalSet.BuildSystemInstructions(
            session.Workflow.Title, session.Proposals.All.Select(e => e.DisplayName).ToList()));
        session.ChatHistory.AddSystemMessage(session.Proposals.BuildSnapshotText());

        if (!session.PreserveConversation)
            session.Messages.Add(new SessionMessage(SessionMessageKind.ClearConversation));
        Say(session,
            "Proposals are ready. Ask about them or request changes (for example, rename a field). " +
            "Accept still writes the outline. This chat is only for these proposals.");

        session.ChatEnabled = true;
        session.ChatPlaceholder = "Ask about or change proposals…";
        _logger?.LogInformation(
            "Proposal chat seeded for {Workflow} with {Count} keys",
            session.Workflow.Title, session.Proposals.Count);
    }

    /// <summary>
    /// Mid-chat: append an updated proposal snapshot (keeps user/assistant turns).
    /// </summary>
    private static void RefreshProposalSnapshotInHistory(WorkflowSession session)
    {
        if (session.ChatHistory == null || session.Proposals == null)
            return;
        session.ChatHistory.AddSystemMessage(
            "Updated property proposals:\n" + session.Proposals.BuildSnapshotText());
    }

    /// <summary>
    /// Extracts the explanation field from a JSON AI response.
    /// </summary>
    private static string? ExtractExplanationFromResponse(string response)
    {
        try
        {
            var jsonStart = response.IndexOf("{");
            var jsonEnd = response.LastIndexOf("}");
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonText = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                using var doc = System.Text.Json.JsonDocument.Parse(jsonText);

                if (doc.RootElement.TryGetProperty("explanation", out var explanation))
                {
                    return explanation.GetString();
                }
            }
        }
        catch
        {
            // Ignore parse errors - explanation is optional
        }
        return null;
    }

    #endregion

    #region Accept and skip

    /// <summary>
    /// Applies one Fill or Refresh proposal to the outline. A Protect proposal goes on the stage
    /// instead and is marked accepted; <see cref="ApplyStagedAsync"/> writes it once the
    /// overwrite is confirmed. Returns false when the key is unknown or the write failed; the
    /// reason is in the messages.
    /// </summary>
    public Task<bool> AcceptAsync(WorkflowSession session, string propertyKey)
    {
        if (string.IsNullOrEmpty(propertyKey))
        {
            _logger?.LogWarning("AcceptProperty called with empty key");
            return Task.FromResult(false);
        }

        try
        {
            var pending = session.Result?.PendingUpdates
                .FirstOrDefault(u => string.Equals(u.Key, propertyKey, StringComparison.OrdinalIgnoreCase));
            if (pending == null)
            {
                _logger?.LogWarning("Property key not found in pending updates: {Key}", propertyKey);
                return Task.FromResult(false);
            }

            if (pending.Kind == UpdateKind.Protect)
            {
                // Stage overwrite; confirm once when the queue is fully decided (#140).
                // Mark Accepted in the session set now so Review Each can leave the row
                // (IsSettled). Without this, KindLabel stays "Has your text",
                // AdvancePastSettledRows never moves, and Accept looks stuck.
                session.Stage.StageProtect(pending);
                RemovePendingKeys(session, new[] { propertyKey });
                session.Proposals?.MarkAccepted(propertyKey);
                Status(session, $"Queued overwrite: {propertyKey}");
                _logger?.LogInformation("AcceptProperty: Staged Protect {Key}", propertyKey);
                return Task.FromResult(true);
            }

            var applied = ApplyPendingList(session, new[] { pending });
            if (applied > 0)
            {
                Status(session, $"Applied {propertyKey}");
                _logger?.LogInformation("AcceptProperty: Applied {Key}", propertyKey);
                _auditLogger?.Log(StoryCADLib.Services.Logging.LogLevel.Info,
                    $"Applied update: {propertyKey}");
                session.Proposals?.MarkAccepted(propertyKey);
                RemovePendingKeys(session, new[] { propertyKey });
                RefreshProposalSnapshotInHistory(session);
                // Save on each confirmed update, not just the last one in the
                // queue: there is no manual Save button to fall back on.
                AutoSave(session, "AcceptProperty");
                return Task.FromResult(true);
            }

            Fail(session, $"Failed to apply {propertyKey}");
            _logger?.LogWarning("Failed to apply {Key}", propertyKey);
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            Fail(session, $"Error applying {propertyKey}: {ex.Message}");
            _logger?.LogError(ex, "Error in AcceptProperty handler for {Key}", propertyKey);
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Skips one proposal. Returns false when the key is not an open proposal.
    /// </summary>
    public bool Skip(WorkflowSession session, string propertyKey)
    {
        if (string.IsNullOrEmpty(propertyKey))
        {
            _logger?.LogWarning("SkipProperty called with empty key");
            return false;
        }

        try
        {
            var removed = session.Result?.PendingUpdates.RemoveAll(u =>
                string.Equals(u.Key, propertyKey, StringComparison.OrdinalIgnoreCase)) ?? 0;
            session.Result?.UpdatedProperties.Remove(propertyKey);
            if (removed > 0)
            {
                session.Proposals?.MarkSkipped(propertyKey);
                Status(session, $"Skipped {propertyKey}");
                _logger?.LogInformation("SkipProperty: Skipped {Key}", propertyKey);
                RefreshProposalSnapshotInHistory(session);
                return true;
            }

            _logger?.LogWarning("Property key not found for skip: {Key}", propertyKey);
            return false;
        }
        catch (Exception ex)
        {
            Fail(session, $"Error skipping {propertyKey}: {ex.Message}");
            _logger?.LogError(ex, "Error in SkipProperty handler for {Key}", propertyKey);
            return false;
        }
    }

    /// <summary>
    /// True when every proposal is decided and Protect rows wait on the stage. The caller then
    /// asks the writer once and gives the answer to <see cref="ApplyStagedAsync"/>.
    /// </summary>
    internal bool ShouldConfirmStaged(WorkflowSession session) =>
        session.Stage.ShouldConfirmStaged(session.Result?.PendingUpdates.Count ?? 0);

    /// <summary>
    /// Applies the staged Protect rows when <paramref name="overwrite"/> is true. When it is
    /// false, the rows stay as they are in the outline and are marked skipped.
    /// </summary>
    public Task ApplyStagedAsync(WorkflowSession session, bool overwrite)
    {
        var staged = session.Stage.StagedProtect.ToList();
        if (staged.Count == 0)
            return Task.CompletedTask;

        if (overwrite)
        {
            var n = ApplyPendingList(session, staged);
            foreach (var u in staged)
                session.Proposals?.MarkAccepted(u.Key);
            Say(session,
                n > 0
                    ? $"Applied {n} overwrite(s) after confirmation."
                    : "No overwrites were applied.");
            _logger?.LogInformation("StagedProtect confirmed: Applied {Count}", n);
            _auditLogger?.Log(StoryCADLib.Services.Logging.LogLevel.Info,
                $"Applied {n} confirmed overwrites from workflow: {session.Workflow.Title}");
            session.Stage.ClearStage();
            RefreshProposalSnapshotInHistory(session);
            AutoSave(session, "confirmed overwrites");
        }
        else
        {
            // Writer refused overwrites: session was already MarkAccepted for Review
            // Each advance — flip to Skipped so chat/snapshot match the outline.
            foreach (var u in staged)
                session.Proposals?.MarkSkipped(u.Key);
            Say(session, $"Cancelled: left {staged.Count} existing field(s) unchanged.");
            _logger?.LogInformation("StagedProtect cancelled: {Count}", staged.Count);
            session.Stage.ClearStage();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Applies each open proposal. Protect proposals (text the writer wrote) are applied only
    /// when <paramref name="overwrite"/> is true; otherwise they stay open.
    /// </summary>
    public Task AcceptAllAsync(WorkflowSession session, bool overwrite)
    {
        try
        {
            var result = session.Result;
            if (result == null)
                return Task.CompletedTask;

            var (free, protect) = OverwriteAcceptanceSession.Partition(result.PendingUpdates);
            session.Stage.ClearStage();

            var applyList = new List<PendingUpdate>(free);
            if (protect.Count > 0)
            {
                if (!overwrite)
                {
                    // Cancel overwrites: still apply free rows; leave Protect pending.
                    var freeOnly = ApplyPendingList(session, free);
                    foreach (var u in free)
                        session.Proposals?.MarkAccepted(u.Key);
                    RemovePendingKeys(session, free.Select(u => u.Key));
                    Say(session,
                        freeOnly > 0
                            ? $"Applied {freeOnly} free update(s). Left {protect.Count} field(s) with existing text unchanged."
                            : $"Left {protect.Count} field(s) with existing text unchanged.");
                    _logger?.LogInformation(
                        "AcceptAll cancelled overwrites: free={Free} protect={Protect}",
                        freeOnly, protect.Count);
                    RefreshProposalSnapshotInHistory(session);
                    if (freeOnly > 0)
                        AutoSave(session, "Accept All free-only");
                    return Task.CompletedTask;
                }

                applyList.AddRange(protect);
            }

            var count = ApplyPendingList(session, applyList);
            var appliedKeys = new HashSet<string>(session.LastAppliedKeys, StringComparer.OrdinalIgnoreCase);
            foreach (var u in applyList)
                session.Proposals?.MarkAccepted(u.Key);
            result.PendingUpdates.Clear();
            result.UpdatedProperties.Clear();

            var sb = new System.Text.StringBuilder();
            if (appliedKeys.Count > 0)
            {
                // Only what applied: a beat row can return 0 (#217 5.7).
                sb.AppendLine($"Applied {appliedKeys.Count} update(s) to your outline:");
                sb.AppendLine();
                foreach (var u in applyList.Where(u => appliedKeys.Contains(u.Key)))
                {
                    var valuePreview = TruncateForChat(FormatValueForDisplay(session.Model, u.Value));
                    sb.AppendLine($"**{u.Key}**: {valuePreview}");
                    sb.AppendLine();
                }
            }
            else
                sb.AppendLine("No updates to apply.");

            Say(session, sb.ToString().TrimEnd());
            _logger?.LogInformation(
                "AcceptAll: Applied {Count} (free={Free} protect={Protect})",
                count, free.Count, protect.Count);
            _auditLogger?.Log(StoryCADLib.Services.Logging.LogLevel.Info,
                $"Applied {count} updates from workflow: {session.Workflow.Title}");

            RefreshProposalSnapshotInHistory(session);
            if (count > 0)
                AutoSave(session, "Accept All");
        }
        catch (Exception ex)
        {
            Fail(session, $"Error applying updates: {ex.Message}");
            _logger?.LogError(ex, "Error in AcceptAll handler");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Accept Remaining: applies the free proposals now and stages the remaining Protect
    /// proposals. The caller asks about the stage once the queue is empty (#140).
    /// </summary>
    internal Task AcceptRemainingFreeAsync(WorkflowSession session)
    {
        try
        {
            var result = session.Result;
            if (result == null)
                return Task.CompletedTask;

            var (free, protect) = OverwriteAcceptanceSession.Partition(result.PendingUpdates);
            var freeCount = ApplyPendingList(session, free);
            RemovePendingKeys(session, free.Select(u => u.Key));

            foreach (var u in protect)
            {
                session.Stage.StageProtect(u);
                session.Proposals?.MarkAccepted(u.Key);
            }
            RemovePendingKeys(session, protect.Select(u => u.Key));

            if (freeCount > 0)
                Say(session, $"Applied {freeCount} free update(s).");

            if (freeCount > 0)
                AutoSave(session, "Accept Remaining free");
        }
        catch (Exception ex)
        {
            Fail(session, $"Error applying remaining: {ex.Message}");
            _logger?.LogError(ex, "Error in AcceptRemainingFree handler");
        }

        return Task.CompletedTask;
    }

    private int ApplyPendingList(WorkflowSession session, IReadOnlyList<PendingUpdate> list)
    {
        var runner = EnsureRunner(session);
        int applied = 0;
        session.LastAppliedKeys.Clear();
        foreach (var u in list)
        {
            // One update per slice: the applied keys are then known, and a row
            // that returns 0 (#217 5.7: its beat was filled meanwhile) says why
            // in the status list instead of a bare "Failed to apply".
            var slice = WorkflowResult.Succeeded();
            slice.PendingUpdates.Add(u);
            var n = runner.ApplyUpdates(slice, session.Elements);
            foreach (var msg in slice.StatusMessages)
            {
                if (!msg.StartsWith("No-op ", StringComparison.Ordinal))
                    Status(session, msg);
            }
            if (n > 0)
                session.LastAppliedKeys.Add(u.Key);
            applied += n;
            // Collaborator #272: record the touch only when something was written,
            // with the fingerprint of what is now in the outline.
            if (n > 0)
            {
                var fingerprint = runner.ReadTouchFingerprint(u);
                if (fingerprint != null)
                    session.TouchedFields[u.SessionTouchKey] = fingerprint;
            }
        }
        if (!session.OrphanBindDone
            && session.Result != null
            && string.Equals(session.Workflow.Label, "SceneBuilder", StringComparison.Ordinal))
        {
            var bindMsg = runner.TryApplySceneBuilderOrphanBind(session.Result, session.Elements);
            session.OrphanBindDone = true;
            if (!string.IsNullOrEmpty(bindMsg))
                Status(session, bindMsg);
        }
        if (applied > 0)
            session.WriteCount++;
        return applied;
    }

    private static void RemovePendingKeys(WorkflowSession session, IEnumerable<string> keys)
    {
        if (session.Result == null)
            return;
        var set = keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        session.Result.PendingUpdates.RemoveAll(u => set.Contains(u.Key));
        foreach (var key in set)
            session.Result.UpdatedProperties.Remove(key);
    }

    #endregion

    #region Save

    /// <summary>
    /// Saves the story outline via the API.
    /// Bypasses StoryCAD's ViewModel flush to ensure API-applied changes persist.
    /// </summary>
    public async Task<bool> SaveAsync(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            _logger?.LogWarning("Save failed: No file path available");
            return false;
        }

        try
        {
            var result = await _api.WriteOutline(filePath);
            if (result.IsSuccess)
            {
                _logger?.LogInformation("Outline saved successfully to {FilePath}", filePath);
                return true;
            }
            else
            {
                _logger?.LogError("Save failed: {Error}", result.ErrorMessage);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving outline to {FilePath}", filePath);
            return false;
        }
    }

    /// <summary>
    /// Saves the outline file after a write, without making the caller wait: the write
    /// queues behind StoryCAD's serialization lock, which an in-flight autosave can hold for
    /// seconds. The task is in <see cref="WorkflowSession.PendingSave"/>; a new save waits for the
    /// one before it, and a caller that must not outrun the saves awaits it.
    /// </summary>
    private void AutoSave(WorkflowSession session, string reason)
    {
        var path = session.FilePath;
        var previous = session.PendingSave;
        session.PendingSave = Task.Run(async () =>
        {
            // One save at a time, in the order the writes happened.
            if (previous != null)
            {
                try { await previous; } catch { /* that save logged its own failure */ }
            }

            var saved = await SaveAsync(path);
            if (saved)
                _logger?.LogInformation("Auto-save completed after {Reason}", reason);
        });
    }

    #endregion

    #region Display text

    /// <summary>
    /// The chat summary after a run. Collaborator #237 item 9: plain words, no em dash, one count.
    /// Collaborator #272: names the properties the model kept, which the list does not show.
    /// </summary>
    internal static string BuildSummaryMessage(int proposalCount, int protectCount, IReadOnlyList<string> keptProperties)
    {
        var keptNote = keptProperties.Count switch
        {
            0 => string.Empty,
            1 => $"1 field is kept as it is: {keptProperties[0]}.",
            _ => $"{keptProperties.Count} fields are kept as they are: {string.Join(", ", keptProperties)}."
        };

        if (proposalCount == 0)
            return keptProperties.Count == 0
                ? "No property updates were extracted from the response."
                : $"No property updates. {keptNote}";

        var replaceNote = protectCount > 0
            ? $" {protectCount} of them would replace text you wrote; those ask for confirmation."
            : string.Empty;
        var keptSentence = keptNote.Length > 0 ? " " + keptNote : string.Empty;
        return $"Found {proposalCount} property update(s).{replaceNote}{keptSentence} " +
               "Choose Accept All, Review Each, or Try Again. Chat can revise these proposals.";
    }

    /// <summary>
    /// Issue #90 design section 10 "The cutoff" (ruling of 2026-07-15, step 10): the shipped chat
    /// sidebar sends through Semantic Kernel to the Worker's /v1/chat/completions, which refuses
    /// with 429 (before any upstream dispatch) when the caller's balance is at or below zero.
    /// Semantic Kernel wraps a non-success HTTP response in <see cref="HttpOperationException"/>
    /// (its <see cref="HttpOperationException.StatusCode"/> carries the code); this recognizes the
    /// 429 shape and translates it to <see cref="OutOfCreditsException"/> so
    /// WorkflowViewModel.SendButtonClicked's <c>ChatMessage.Error(ex.Message)</c> shows a message
    /// naming the credits screen instead of the raw HTTP exception text. Every other exception
    /// passes through unchanged. internal static and side-effect-free, so it is testable without a
    /// live kernel call: construct an HttpOperationException directly (its public 4-arg
    /// constructor takes the status code) and assert on the returned exception's type/message.
    /// </summary>
    internal static Exception TranslateChatException(Exception ex)
    {
        if (ex is not HttpOperationException { StatusCode: HttpStatusCode.TooManyRequests })
        {
            return ex;
        }

        var allowlist = Ioc.Default.GetService<IStoreActivationService>()?.IsAllowlistActivation == true;
        return allowlist
            ? new OutOfCreditsException(StoreConfig.OutOfCreditsBetaMessage)
            : new OutOfCreditsException();
    }

    /// <summary>
    /// Exact Lists.json ProblemCategory value when a Problem is the Overview StoryProblem.
    /// The input gather writes it when it links the Problem.
    /// </summary>
    internal const string StoryProblemCategoryListValue = "Story problem";


    /// <summary>
    /// Readable text for a typed pending-update value; lists render per entry and
    /// element GUIDs resolve to outline names (#129).
    /// </summary>
    internal static string? ResolveElementName(StoryModel model, Guid guid) =>
        model?.StoryElements?.StoryElementGuids != null
        && model.StoryElements.StoryElementGuids.TryGetValue(guid, out var element)
            ? element?.Name
            : null;

    internal static string FormatValueForDisplay(StoryModel model, object? value) =>
        ValueDisplay.Format(value, guid => ResolveElementName(model, guid));

    internal static string TruncateForChat(string? text, int max = 200)
    {
        var value = text ?? "(empty)";
        if (value.Length > max)
            return value.Substring(0, max) + "...";
        return value;
    }

    /// <summary>
    /// One session proposal as a row of the review pane.
    /// </summary>
    internal static StoryCADLib.Collaborator.Models.PendingUpdateItem ToSessionPendingUpdateItem(
        SessionProposalSet.Entry e)
    {
        // ProposedText is already ValueDisplay-formatted at capture; do not rebind Value to that string
        // (List properties must keep typed Values for Accept / SimpleList).
        var u = e.Update;
        var proposed = TruncateForChat(e.ProposedText, 500);
        var current = TruncateForChat(u.CurrentDisplay ?? string.Empty, 300);
        var kindLabel = e.Status switch
        {
            ProposalSessionStatus.Accepted => "Accepted",
            ProposalSessionStatus.Skipped => "Skipped",
            _ => u.Kind switch
            {
                UpdateKind.Fill => "New",
                UpdateKind.Refresh => "Refresh",
                UpdateKind.Protect => "Has your text",
                _ => "Update"
            }
        };
        return new StoryCADLib.Collaborator.Models.PendingUpdateItem
        {
            Key = u.Key,
            ElementName = u.ElementLabel,
            PropertyDisplayName = u.DisplayNameOverride ?? u.Spec.Property,
            ProposedDisplay = proposed,
            CurrentDisplay = current,
            KindLabel = kindLabel,
            IsProtected = e.Status == ProposalSessionStatus.Open && u.Kind == UpdateKind.Protect,
            CraftExplanation = u.CraftExplanation ?? string.Empty,
            SummaryLine = kindLabel
        };
    }

    #endregion

    #region Chat

    /// <summary>
    /// Sends one chat message. In an interview it is the answer to the last question and the
    /// reply is the next question. Otherwise it is a talk about the proposals, and a patch in
    /// the reply changes them. Returns the text to show the writer.
    /// </summary>
    public Task<string> ChatAsync(WorkflowSession session, string userMessage)
    {
        session.SetLastError(null);
        session.LastChatCost = null;
        if (session.IsInterview)
        {
            if (session.InterviewAwaitingChoice || session.InterviewCursor.NotStarted)
                return ChooseInterviewTargetsAsync(session, userMessage);

            return RunInterviewTurnAsync(session, userMessage);
        }

        return ChatAboutProposalsAsync(session, userMessage);
    }

    private async Task<string> ChatAboutProposalsAsync(WorkflowSession session, string userMessage)
    {
        try
        {
            if (session.Proposals == null || session.Proposals.Count == 0)
            {
                return "Chat unlocks after the workflow produces property proposals.";
            }

            var chatService = session.ChatService ??= EnsureChatService();
            session.ChatHistory?.AddUserMessage(userMessage);
            _logger?.LogDebug("User message added to chat: {Message}", userMessage);

            // Cost line (shown when CollaboratorSettings.ShowCostDetails is on). The proxy's
            // X-Collab-Cost header is read by
            // ActivationJwtHandler inside this scope (Semantic Kernel hides the response
            // from us); Semantic Kernel's own usage metadata is the fallback when the
            // header is absent, giving tokens without dollars.
            Microsoft.SemanticKernel.ChatMessageContent response;
            ProxyCostInfo? chatCost;
            using (var costScope = ChatCostCapture.Begin())
            {
                response = await chatService.GetChatMessageContentAsync(session.ChatHistory!);
                chatCost = costScope.Cost;
            }

            session.LastChatCost = chatCost;

            var responseText = response.Content ?? "No response received.";

            // Nothing to report leaves the previous line alone rather than blanking it:
            // a chat turn that cannot account for itself should not erase the last
            // workflow's figure.
            var usageRead = ChatUsageReader.TryRead(response.Metadata, out var chatIn, out var chatOut);
            if (chatCost != null || usageRead)
                session.PendingCostSummary = session.CostTracker.RecordChat(chatCost, chatIn, chatOut);

            ChatPatchParser.TryParse(responseText, out var display, out var patches);
            var listChanged = false;

            if (patches.Count > 0 && session.Proposals != null)
            {
                // Collaborator #237 item 5: report what was applied, by name, and what was
                // not. The old text said "Updated" for every parsed patch, so a key the
                // session did not know produced a success line over an unchanged list.
                // The Scorecard chat of 2026-09-05 added a second way: the model re-sent
                // the text already in the list and the app said "Changed". Same text is
                // now reported as unchanged.
                var changed = new List<string>();
                var unchanged = new List<string>();
                var unknown = new List<string>();
                foreach (var p in patches)
                {
                    var resolved = session.Proposals.ResolveKey(p.Key);
                    if (resolved != null &&
                        session.Proposals.TryApplyPatch(resolved, p.Value, out var reopened, out var textChanged))
                    {
                        var name = session.Proposals.Get(resolved)?.DisplayName ?? resolved;
                        if (textChanged || reopened)
                            changed.Add(name);
                        else
                            unchanged.Add(name);
                    }
                    else
                    {
                        unknown.Add(p.Key);
                        _logger?.LogWarning("Ignored chat patch for unknown key {Key}", p.Key);
                    }
                }

                var notes = new List<string>();
                if (changed.Count > 0)
                {
                    var synced = SyncWorkflowResultFromSession(session);
                    listChanged = synced;
                    notes.Add(synced
                        ? $"Changed {string.Join(", ", changed)}. The list shows the new text. Accept to write the outline."
                        : $"Changed {string.Join(", ", changed)} in chat only. The list could not be refreshed; run the workflow again.");
                }
                if (unchanged.Count > 0)
                    notes.Add($"{string.Join(", ", unchanged)} already read that way; nothing changed there.");
                if (unknown.Count > 0)
                    notes.Add($"No proposal in this run is named {string.Join(", ", unknown)}; nothing changed for it.");

                var note = string.Join(" ", notes);
                display = string.IsNullOrWhiteSpace(display) ? note : display + "\n\n" + note;
            }
            else if (ChatPatchParser.HasUnreadPatchBlock(responseText, patches))
            {
                _logger?.LogWarning("Chat reply named patches but none could be read");
                display += "\n\nCollaborator wrote a change the app could not read. Ask for it again.";
            }

            // The history holds what the writer saw, note included, so the model knows the
            // app applied its patch. The refreshed snapshot follows the assistant turn.
            session.ChatHistory?.AddAssistantMessage(display);
            if (listChanged)
            {
                session.ProposalsChanged = true;
                RefreshProposalSnapshotInHistory(session);
            }

            _logger?.LogDebug("Assistant response (display): {Response}", display);
            return display;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing chat message");
            var translated = TranslateChatException(ex);
            session.SetLastError(translated.Message);
            throw translated;
        }
    }

    /// <summary>
    /// Push session proposals into WorkflowResult (open only for Accept) — #145.
    /// </summary>
    private bool SyncWorkflowResultFromSession(WorkflowSession session)
    {
        if (session.Proposals == null || session.Result == null)
        {
            // Collaborator #237 item 5: this used to return in silence, and the caller
            // reported success anyway. It cannot happen while a chat session is open, so
            // say so where a log reader will see it.
            _logger?.LogWarning(
                "Proposal sync skipped: session={Session} result={Result}",
                session.Proposals != null, session.Result != null);
            return false;
        }

        var open = session.Proposals.OpenAsPendingUpdates().ToList();
        session.Result.PendingUpdates.Clear();
        session.Result.UpdatedProperties.Clear();
        foreach (var u in open)
        {
            session.Result.PendingUpdates.Add(u);
            session.Result.UpdatedProperties[u.Key] = FormatValueForDisplay(session.Model, u.Value);
        }

        return true;
    }

    private static readonly object KernelLock = new();
    private static IChatCompletionService? _sharedChatService;

    /// <summary>
    /// Ensures Semantic Kernel is initialized. Thread-safe, only initializes once.
    /// Called lazily when kernel is actually needed (not in constructor).
    /// </summary>
    internal IChatCompletionService EnsureChatService()
    {
        lock (KernelLock)
        {
            if (_sharedChatService == null)
            {
                ILoggerFactory? loggerFactory = null;
                try
                {
                    loggerFactory = LoggerFactory.Create(builder =>
                    {
                        builder.SetMinimumLevel(LogLevel.Warning); // SK is verbose, only log warnings+
                        if (NLog.LogManager.Configuration != null)
                        {
                            builder.AddNLog(NLog.LogManager.Configuration);
                        }
                    });
                }
                catch
                {
                    loggerFactory = LoggerFactory.Create(builder =>
                    {
                        builder.SetMinimumLevel(LogLevel.Warning);
                    });
                }

                // Single construction site: delegate to the shared factory, which also
                // logs the active path (direct vs proxy, endpoint host) per D6.
                var kernel = KernelInitializer.EnsureBuilt(loggerFactory);
                _sharedChatService = kernel.GetRequiredService<IChatCompletionService>();
                _logger?.LogInformation("Semantic Kernel initialized");
            }

            return _sharedChatService;
        }
    }

    #endregion

    #region Interview (#119)

    /// <summary>
    /// Opens an interview (#119). First the choice: what to explore, by the form's own
    /// labels, or "you choose" (design section 25.5). Nothing goes to the Worker until
    /// the writer answers that. The earlier build asked its first question at once, from
    /// a stock sentence; Terry read that as the model not knowing how to start. The goal
    /// is what starts it, and the writer or the Worker names the goal.
    /// </summary>
    public void StartInterview(WorkflowSession session)
    {
        session.InterviewTranscript = new InterviewTranscript();
        session.InterviewCursor.Reset();
        session.InterviewPlan = InterviewPlan.Default();
        session.InterviewAwaitingChoice = true;
        session.InterviewModelChooses = false;
        session.InterviewPendingQuestion = string.Empty;
        session.InterviewNoteGuid = Guid.Empty;
        session.InterviewHasUnsavedTurns = false;
        session.InterviewAnswerAwaitingQuestion = false;
        session.CanSaveInterview = false;
        session.InterviewTurnRunning = false;

        session.Elements.TryGetValue("Character", out var character);
        session.InterviewCharacterName = character?.Name ?? "this character";
        session.InterviewCharacterGuid = character?.Uuid ?? Guid.Empty;

        session.ChatPlaceholder = "Numbers, a field name, or: you choose";
        session.ChatEnabled = true;
        Say(session,
            $"You will answer as {session.InterviewCharacterName}, in their voice, in first person. "
            + "There are no wrong answers and nothing here is written to their form until you "
            + "save. If a question assumes something you have not decided yet, decide it now.");
        Say(session,
            InterviewChoice.ListText(session.InterviewCharacterName, BlankInterviewFields(character)));

        _logger?.LogInformation("Interview opened for {Character}", session.InterviewCharacterName);
    }

    /// <summary>
    /// The writer's answer to "what do you want to explore?" (design 25.5 steps 3 and 4).
    /// Sets the plan and runs the opening. Unreadable: the list again, nothing else. An
    /// opening that fails or returns nothing usable re-arms the choice and says so, rather
    /// than swallowing the writer's next message as a retry of a plan they may want to
    /// change (review, 2026-09-07).
    /// </summary>
    private async Task<string> ChooseInterviewTargetsAsync(WorkflowSession session, string text)
    {
        var choice = InterviewChoice.Parse(text);
        if (choice.Kind == InterviewChoiceKind.Unreadable)
        {
            session.Elements.TryGetValue("Character", out var character);
            return "I did not catch that. "
                + InterviewChoice.ListText(session.InterviewCharacterName, BlankInterviewFields(character));
        }

        session.InterviewAwaitingChoice = false;
        session.InterviewModelChooses = choice.Kind == InterviewChoiceKind.ModelChooses;
        session.InterviewPlan = session.InterviewModelChooses
            ? InterviewPlan.Default()
            : InterviewPlan.From(choice.Targets);

        session.ChatPlaceholder = $"Answer as {session.InterviewCharacterName}…";
        _logger?.LogInformation("Interview targets for {Character}: {Targets}",
            session.InterviewCharacterName,
            session.InterviewModelChooses ? "(Worker chooses)" : session.InterviewPlan.ToArg());

        // The opening posts its own question, or its own error.
        await RunInterviewTurnAsync(session, answer: null);

        if (session.InterviewCursor.NotStarted)
        {
            session.InterviewAwaitingChoice = true;
            session.InterviewModelChooses = false;
            session.ChatPlaceholder = "Numbers, a field name, or: you choose";
            return "The interview did not start. Say what to explore again, or: you choose.";
        }

        return string.Empty;
    }

    /// <summary>
    /// The form fields with nothing on them, so the choice list can mark them. Blank is
    /// the case the interview exists for, and the writer should see where the gaps are.
    /// </summary>
    private static IReadOnlySet<string> BlankInterviewFields(StoryElement? element) =>
        InterviewScript.BlankFields(element as CharacterModel);

    /// <summary>
    /// One interview turn (#119): record the answer just given, then ask what the Worker
    /// says comes next.
    ///
    /// Returns the question text so the caller posts it. The opening turn has no
    /// answer and nothing to post it, so that one posts itself.
    /// </summary>
    private async Task<string> RunInterviewTurnAsync(WorkflowSession session, string? answer)
    {
        if (session.InterviewTranscript == null)
            return string.Empty;

        // One turn at a time. Send is async void, so a second press during a slow call
        // would otherwise open a concurrent turn against the same transcript. The input is
        // disabled while a turn runs; if a message gets through anyway, say so. The
        // message is already in the pane, so a silent return would look like an answer
        // that was taken.
        if (session.InterviewTurnRunning)
        {
            Fail(session,
                "A question is still on its way. That reply was not taken; send it again when the question arrives.");
            return string.Empty;
        }

        var transcript = session.InterviewTranscript;
        var opening = answer == null;

        // Recorded before the call, not after: if the Worker fails, the writer's answer is
        // still part of the interview and still gets saved. If the last call failed, this
        // message continues that answer: the question it belongs to was never asked again,
        // so a second turn on the same question would put the question in the notes twice.
        var extended = false;
        if (!opening)
        {
            extended = session.InterviewAnswerAwaitingQuestion && transcript.ExtendLastAnswer(answer!);
            if (!extended)
                transcript.Add(session.InterviewCursor.Field, session.InterviewPendingQuestion, answer!);
            session.InterviewAnswerAwaitingQuestion = false;
            session.InterviewHasUnsavedTurns = !transcript.IsEmpty;
            session.CanSaveInterview = session.InterviewHasUnsavedTurns;
        }

        // What the Worker responds to: the whole answer, when this message extended one.
        var latestAnswer = opening
            ? null
            : extended ? transcript.Turns[^1].Answer : answer;

        session.InterviewTurnRunning = true;
        session.Busy = true;
        Changed(session);
        try
        {
            var runner = NewRunner(session);

            // A "you choose" opening has no field yet: the Worker names the targets and
            // the client takes its plan from the reply (design 25.5 step 4). Every other
            // turn walks the plan the writer or the Worker already set.
            var choosing = opening && session.InterviewModelChooses;
            var field = opening
                ? (choosing ? string.Empty : session.InterviewPlan.First)
                : session.InterviewCursor.Field;
            var nextField = choosing ? null : session.InterviewPlan.Next(field);

            var body = runner.BuildWorkflowRequestBody(session.Elements);
            runner.EnrichWithStoryContext(body.Args, session.Elements, session.Workflow.GetIO());
            runner.ApplySettings(body.Args);
            WorkflowRunner.SetInterviewArgs(
                body.Args,
                field,
                nextField,
                opening ? 0 : session.InterviewCursor.TurnsOnField,
                transcript.ToPromptText(),
                latestAnswer,
                choosing ? string.Empty : session.InterviewPlan.ToArg());

            var result = await runner.RunPreparedAsync(body, session.Elements);

            session.PendingCostSummary = session.CostTracker.Record(result.Cost);
            session.LastChatCost = result.Cost;

            if (!result.Success)
            {
                Fail(session, result.ErrorMessage ?? "The interview stalled.");
                session.InterviewAnswerAwaitingQuestion = !opening;
                return string.Empty;
            }

            var reply = InterviewReply.Parse(result.RawResponse ?? string.Empty);

            if (!InterviewReply.ShouldApply(opening, reply.Question)
                && string.IsNullOrWhiteSpace(reply.Question))
            {
                Fail(session, "The interviewer sent nothing back. Say something to try again.");
                session.InterviewAnswerAwaitingQuestion = !opening;
                return string.Empty;
            }

            // A closing line where a question was due. On the last field the interviewer
            // meant to end, whatever its header said: finish. With fields still to go it
            // left the interview instead of the field (the retest of 2026-09-07 saw it on
            // a second refusal): treated like an empty reply, the cursor holds, the writer's
            // next message extends the answer, and nobody is told to save a half-done
            // interview.
            if (!opening && reply.Verdict != InterviewVerdict.Done
                && InterviewReply.LooksLikeAClose(reply.Question))
            {
                if (nextField == null)
                {
                    FinishInterview(session, reply.Question);
                    return string.Empty;
                }

                Fail(session, "The interviewer tried to stop early. Say something to go on.");
                session.InterviewAnswerAwaitingQuestion = true;
                _logger?.LogWarning("Interview close arrived with {Next} still to go for {Character}",
                    nextField, session.InterviewCharacterName);
                return string.Empty;
            }

            if (opening)
            {
                if (session.InterviewModelChooses)
                {
                    // The Worker's targets, up to three, unknown ids dropped. Nothing usable
                    // is a failed opening, not a silent walk of all thirteen fields under a
                    // first question aimed elsewhere: the cursor stays unstarted and the
                    // caller puts the choice back on screen. Told by page header, never id.
                    var chosen = InterviewPlan.FromKnown(reply.Targets, maxCount: 3);
                    if (chosen == null)
                    {
                        Fail(session, "The interviewer did not say what it chose.");
                        _logger?.LogWarning("Interview opening for {Character} named no usable target: {Raw}",
                            session.InterviewCharacterName, string.Join(",", reply.Targets));
                        return string.Empty;
                    }

                    session.InterviewPlan = chosen;
                    session.InterviewModelChooses = false;
                    Say(session, session.InterviewPlan.Describe());
                    _logger?.LogInformation("Interview targets chosen by the Worker for {Character}: {Targets}",
                        session.InterviewCharacterName, session.InterviewPlan.ToArg());
                }
                session.InterviewCursor.Start(session.InterviewPlan);
            }
            else if (reply.Verdict == InterviewVerdict.Done
                     || ((reply.Verdict == InterviewVerdict.GotIt
                          || reply.Verdict == InterviewVerdict.NotThis)
                         && nextField == null))
            {
                FinishInterview(session, reply.Question);
                return string.Empty;
            }
            else
            {
                session.InterviewCursor.Apply(reply.Verdict);
            }

            session.InterviewPendingQuestion = reply.Question;

            if (opening)
            {
                Say(session, reply.Question);
                return string.Empty;
            }

            return reply.Question;
        }
        finally
        {
            session.InterviewTurnRunning = false;
            session.Busy = false;
            Changed(session);
        }
    }

    /// <summary>
    /// The last target is done (#119). Saves without asking: the writer has just spent
    /// the session on this and the one thing they must not lose is the record.
    /// </summary>
    private void FinishInterview(WorkflowSession session, string closingLine)
    {
        session.InterviewPendingQuestion = string.Empty;
        session.ChatEnabled = false;
        session.ChatPlaceholder = "The interview is finished";

        // The closing line is the model's, when it wrote one. When it wrote another
        // question instead (the sample-outline runs of 2026-09-07 did, twice), the writer
        // must not be shown a question with the chat already closed; the Saved line that
        // follows is the close.
        if (InterviewReply.LooksLikeAQuestion(closingLine))
        {
            _logger?.LogWarning("Interview closing line for {Character} was a question; not shown: {Line}",
                session.InterviewCharacterName, closingLine);
        }
        else if (!string.IsNullOrWhiteSpace(closingLine))
        {
            Say(session, closingLine);
        }

        SaveInterview(session);
    }

    /// <summary>
    /// Writes the interview into the outline (#119) as a Notes element under the character.
    ///
    /// No model call. Terry: "Do not improve the Summarize prose. Save the questions as
    /// asked and the answers as typed." Running the transcript through a model to produce a
    /// digest is what the first build did, and it lost both the questions and the answers.
    /// </summary>
    internal Task SaveInterviewAsync(WorkflowSession session)
    {
        SaveInterview(session);
        return Task.CompletedTask;
    }

    private void SaveInterview(WorkflowSession session)
    {
        var transcript = session.InterviewTranscript;
        if (transcript == null || transcript.IsEmpty)
        {
            Say(session, "There is nothing to save yet. Answer a question first.");
            return;
        }

        if (session.InterviewCharacterGuid == Guid.Empty)
        {
            Fail(session, "The interview cannot be saved: the character is no longer in the outline.");
            return;
        }

        var error = WriteInterviewNote(session);
        if (error != null)
        {
            Fail(session, $"The interview could not be saved: {error}");
            _logger?.LogWarning("Interview save failed for {Character}: {Error}",
                session.InterviewCharacterName, error);
            return;
        }

        session.CanSaveInterview = false;
        AutoSave(session, "interview note");
        Say(session,
            $"Saved under {session.InterviewCharacterName} as \"Interview with {session.InterviewCharacterName}\", "
            + $"{transcript.Turns.Count} questions and answers.");

        _logger?.LogInformation("Interview saved for {Character}: {Turns} turns",
            session.InterviewCharacterName, transcript.Turns.Count);
    }

    /// <summary>
    /// Called when the workflow page goes away (#119). Writes an unsaved interview rather
    /// than dropping it: the first build kept the exchange only in the chat pane, so closing
    /// the panel left the outline with no record that the interview had happened.
    /// </summary>
    internal void EndInterview(WorkflowSession session)
    {
        if (session.InterviewTranscript is { IsEmpty: false } && session.InterviewHasUnsavedTurns)
        {
            var error = WriteInterviewNote(session);
            _logger?.LogInformation(
                "Interview auto-saved on close for {Character}: {Turns} turns, {Outcome}",
                session.InterviewCharacterName, session.InterviewTranscript.Turns.Count, error ?? "ok");
        }

        session.InterviewTranscript = null;
        session.InterviewCursor.Reset();
        session.InterviewPlan = InterviewPlan.Default();
        session.InterviewAwaitingChoice = false;
        session.InterviewModelChooses = false;
        session.InterviewPendingQuestion = string.Empty;
        session.InterviewCharacterGuid = Guid.Empty;
        session.InterviewCharacterName = string.Empty;
        session.InterviewNoteGuid = Guid.Empty;
        session.InterviewHasUnsavedTurns = false;
        session.InterviewAnswerAwaitingQuestion = false;
    }

    /// <summary>
    /// Writes the transcript to this session's Notes element: created on the first write,
    /// updated in place after that, so a writer who saves early and keeps answering ends
    /// the session with one note holding everything. Returns null on success, else the
    /// error text. It writes the model only; the Save path saves the file.
    /// </summary>
    private string? WriteInterviewNote(WorkflowSession session)
    {
        if (session.InterviewTranscript == null || session.InterviewCharacterGuid == Guid.Empty)
            return "the character is no longer in the outline";

        var text = session.InterviewTranscript.ToNotesText(session.InterviewCharacterName);
        if (session.InterviewNoteGuid == Guid.Empty)
        {
            var added = _api.AddElement(
                StoryItemType.Notes,
                session.InterviewCharacterGuid.ToString(),
                $"Interview with {session.InterviewCharacterName}",
                new Dictionary<string, object> { ["Description"] = text });
            if (!added.IsSuccess)
                return added.ErrorMessage;
            session.InterviewNoteGuid = added.Payload;
        }
        else
        {
            var updated = _api.UpdateElementProperty(session.InterviewNoteGuid, "Description", text);
            if (!updated.IsSuccess)
                return updated.ErrorMessage;
        }

        session.InterviewHasUnsavedTurns = false;
        return null;
    }

    #endregion
}
