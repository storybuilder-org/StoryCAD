using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using CollaboratorLib.Context;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;
using StoryCADLib.DAL;
using StoryCADLib.Models;
using StoryCADLib.Services.API;
using StoryCADLib.Services.Collaborator.Contracts;
using StoryCADLib.Services.Reports;
using StoryCollaborator.Models;
using StoryCollaborator.Workflows;

namespace StoryCollaborator
{
    public sealed record WorkflowRunOutcome(WorkflowResult Result, int AppliedCount);

    /// <summary>
    /// Client request body for POST /v1/workflow after issue #106.
    /// </summary>
    internal sealed class WorkflowProxyBody
    {
        public Dictionary<string, string> Args { get; init; } = new();
        public Dictionary<string, JsonObject> Elements { get; init; } = new();
    }

    internal class WorkflowRunner
    {
        private static HttpClient _httpClient = new() { Timeout = TimeSpan.FromMinutes(3) };

        // Test-only accessor (issue #94 design section 5 item 2): lets
        // WorkflowRunnerTruncationTests.cs observe ResetHttpClient's swap without any network activity.
        internal static HttpClient CurrentHttpClient => _httpClient;

        /// <summary>
        /// Swaps in a fresh HttpClient, disposing the old one (issue #94 design section 5 item 2):
        /// the truncation retry calls this to abandon the pooled connection, which server evidence
        /// pins to a flagged Cloudflare isolate after a mid-stream kill (design doc section 2, phase
        /// C). Safe because workflow calls are sequential -- the UI runs one workflow at a time,
        /// PromptTestRunner and the tests included -- and the chat sidebar uses Semantic Kernel's own
        /// HttpClient, untouched here.
        /// </summary>
        internal static void ResetHttpClient()
        {
            var fresh = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
            var old = Interlocked.Exchange(ref _httpClient, fresh);
            old.Dispose();
        }

        private IStoryCADAPI _storyApi;
        private StoryModel storyModel;
        private Workflow workflowModel;
        private readonly ILogger<WorkflowRunner>? _logger;
        private CollaboratorSettings _settings;
        private readonly StoryCADLib.Services.Logging.ILogService? _auditLogger;

        // No Kernel field: issue #90 step 8 item 5 retired the direct-OpenAI Semantic Kernel
        // invocation path (InvokeDirectAsync), which was the only reason this class held one.
        // PostToProxyAsync talks to the Worker over plain HttpClient, never through SK.
        internal WorkflowRunner(StoryModel model, Workflow workflow, IStoryCADAPI api, ILogger<WorkflowRunner>? logger = null, CollaboratorSettings? settings = null, StoryCADLib.Services.Logging.ILogService? auditLogger = null)
        {
            storyModel = model;
            workflowModel = workflow;
            _logger = logger;
            _settings = settings ?? CollaboratorSettings.Default;
            _storyApi = api;
            _auditLogger = auditLogger;
        }

        /// <summary>
        /// Executes the workflow and applies outputs if autoApply is true.
        /// Shared entry point for the gated integration test and for PromptTestRunner.
        /// </summary>
        internal static async Task<WorkflowRunOutcome> RunAndApplyAsync(
            StoryModel model,
            Workflow workflow,
            IStoryCADAPI api,
            Dictionary<string, StoryElement> gatheredElements,
            CollaboratorSettings settings,
            bool autoApply,
            ILogger<WorkflowRunner>? logger = null)
        {
            var runner = new WorkflowRunner(model, workflow, api, logger, settings);
            var result = await runner.RunAsync(gatheredElements);
            var applied = autoApply && result.Success ? runner.ApplyUpdates(result, gatheredElements) : 0;
            return new WorkflowRunOutcome(result, applied);
        }

        /// <summary>
        /// Executes the workflow with pre-gathered elements.
        /// </summary>
        internal async Task<WorkflowResult> RunAsync(Dictionary<string, StoryElement> gatheredElements)
        {
            var workflowIO = workflowModel.GetIO();

            // Validate required inputs before any template or proxy check.
            foreach (var requirement in workflowIO.RequiredInputs)
            {
                if (!gatheredElements.ContainsKey(requirement.ElementLabel))
                    return WorkflowResult.Failed($"Missing required element: '{requirement.ElementLabel}'");
            }

            // Collaborator #150: abort BeatScenes when ProblemCategory is empty or Story Problem.
            if (string.Equals(workflowModel.Label, "BeatScenes", StringComparison.Ordinal))
            {
                var category = string.Empty;
                if (gatheredElements.TryGetValue("Problem", out var problemEl)
                    && problemEl is ProblemModel pm)
                {
                    category = pm.ProblemCategory ?? string.Empty;
                }
                var gateMessage = ValidateBeatScenesCategory(category);
                if (gateMessage != null)
                    return WorkflowResult.Failed(gateMessage);
            }

            // Without a subscriber's (or allowlisted dev/tester's) activation JWT, the workflow
            // degrades to the stub rather than calling out bare (issue #90 step 8 item 5: the
            // OPENAI_API_KEY direct-to-OpenAI path retired, so a held JWT is the only credential).
            if (string.IsNullOrWhiteSpace(KernelFactory.ResolveWorkflowCredential()))
            {
                return BuildStubResponse();
            }

            var result = WorkflowResult.Succeeded();

            try
            {
                result.StatusMessages.Add($"Starting workflow: {workflowModel.Title}");

                var body = BuildWorkflowRequestBody(gatheredElements);
                result.StatusMessages.Add($"Built request body from {gatheredElements.Count} elements");

                EnrichWithStoryContext(body.Args, gatheredElements, workflowIO);
                ApplySettings(body.Args);

                if (workflowIO.ExampleLists.Count > 0)
                    EnrichWithExamples(body.Args);

                if (string.Equals(workflowModel.Label, "BeatScenes", StringComparison.Ordinal))
                    EnrichWithStockScenes(body.Args);

                // Issue #90 step 8 item 5: the direct-to-OpenAI fallback retired along with
                // OPENAI_API_KEY on the client. A proxy failure now propagates to the outer
                // catch clauses below rather than retrying against OpenAI directly.
                result.AssembledPrompt = null;
                var (proxyContent, proxyHash, proxyCost, proxyComplete) = await PostToProxyAsync(body);
                result.RemoteTemplateHash = proxyHash;
                result.Cost = proxyCost;

                if (!proxyComplete)
                {
                    // Issue #94 design section 5 item 3 ("surface, never mask"): the one truncation
                    // retry (PostToProxyAsync -> ExecuteWithTruncationRetryAsync) still came back
                    // incomplete. The partial text is preserved for diagnostics but must never reach
                    // ExtractOutputs or a Success result.
                    return BuildTruncationFailureResult(proxyContent);
                }

                string planResult = proxyContent;
                result.RawResponse = planResult;

                if (string.IsNullOrEmpty(planResult))
                {
                    return WorkflowResult.Failed("Workflow returned empty response");
                }

                result.StatusMessages.Add("Received AI response");

                var outputResult = ExtractOutputs(planResult, gatheredElements, workflowIO.Outputs);

                foreach (var msg in outputResult.StatusMessages)
                    result.StatusMessages.Add(msg);
                foreach (var kvp in outputResult.UpdatedProperties)
                    result.UpdatedProperties[kvp.Key] = kvp.Value;
                foreach (var pending in outputResult.PendingUpdates)
                    result.PendingUpdates.Add(pending);

                // #120: structural fields the model must not invent (list value + GUIDs).
                // Proposes into pending only; #116 classify decides Fill vs Protect (never silent force).
                if (workflowModel.Label == "InnerOuterProblems")
                    EnrichInnerOuterStructuralFields(result, gatheredElements);

                if (!outputResult.Success)
                {
                    result.Success = false;
                    result.ErrorMessage = outputResult.ErrorMessage;
                }

                return result;
            }
            catch (StoryCADLib.Services.Store.OutOfCreditsException ex)
            {
                // Issue #90 design section 10 (step 10): shown as-is, not wrapped in
                // "Workflow execution failed: ..." -- this is a recognized, actionable state
                // (buy more credits or wait for renewal), not an unexpected failure.
                _logger?.LogWarning("Workflow call refused: out of credits ({Workflow})", workflowModel.Title);
                return WorkflowResult.Failed(ex.Message);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "WorkflowRunner.RunAsync error");
                _auditLogger?.LogException(StoryCADLib.Services.Logging.LogLevel.Error, ex,
                    $"Workflow failed: {workflowModel.Title}");
                return WorkflowResult.Failed($"Workflow execution failed: {ex.Message}");
            }
        }

        private static readonly JsonSerializerOptions ElementSerializeOptions = new()
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new EmptyGuidConverter(),
                new StoryElementConverter(),
                new JsonStringEnumConverter()
            }
        };

        /// <summary>
        /// Builds the #106 request body: full gathered elements (RTF stripped on outbound copy only)
        /// plus pass-through args and declared collection lists. Does not flatten Label_Property keys
        /// and does not invent lists from WriteVia.
        /// Issue #107: character craft workflows also get RelatedProblems (full Problem models).
        /// </summary>
        internal WorkflowProxyBody BuildWorkflowRequestBody(Dictionary<string, StoryElement> gatheredElements)
        {
            var body = new WorkflowProxyBody();

            foreach (var (label, element) in gatheredElements)
            {
                if (element == null) continue;
                body.Elements[label] = SerializeElementOutbound(element);
            }

            var serOpts = new JsonSerializerOptions { WriteIndented = false };
            foreach (var collection in workflowModel.GetIO().CollectionInputs)
            {
                var listResult = _storyApi.GetElementsByType(collection.ElementType);
                if (!listResult.IsSuccess) continue;

                var projected = (listResult.Payload ?? Enumerable.Empty<StoryElement>())
                    .Select(e => ProjectCollectionElement(e, collection.Projection))
                    .ToList();
                body.Args[collection.RequestName] = JsonSerializer.Serialize(projected, serOpts);
            }

            AttachRelatedProblemsCollection(body, gatheredElements, serOpts);

            return body;
        }

        /// <summary>
        /// Index → all problems where Character is prot and/or antag → full models in args.
        /// Empty array when no links or no Character. Relationship deferred.
        /// </summary>
        private void AttachRelatedProblemsCollection(
            WorkflowProxyBody body,
            Dictionary<string, StoryElement> gatheredElements,
            JsonSerializerOptions serOpts)
        {
            _ = serOpts;
            if (!ContextResolver.RelatedProblemsWorkflows.Contains(workflowModel.Label))
                return;

            var array = new JsonArray();
            if (gatheredElements.TryGetValue("Character", out var characterElement) &&
                characterElement is CharacterModel character)
            {
                var index = ProblemCharacterIndex.Build(_storyApi, storyModel);
                foreach (var problemGuid in index.RelatedProblemGuids(character.Uuid))
                {
                    var result = _storyApi.GetStoryElement(problemGuid);
                    if (result.IsSuccess && result.Payload is ProblemModel problem)
                        array.Add(SerializeElementOutbound(problem));
                }
            }

            body.Args[ContextResolver.RelatedProblemsRequestName] = array.ToJsonString();
        }

        /// <summary>
        /// Serialize runtime type to a JSON object, then strip RTF on that copy only.
        /// Never mutates the live outline element.
        /// </summary>
        internal JsonObject SerializeElementOutbound(StoryElement element)
        {
            // StoryElementConverter writes Type discriminator using runtime type.
            var json = JsonSerializer.Serialize<StoryElement>(element, ElementSerializeOptions);
            var node = JsonNode.Parse(json)!.AsObject();
            StripRtfOnTopLevelStrings(node);
            return node;
        }

        private static void StripRtfOnTopLevelStrings(JsonObject obj)
        {
            var stripper = new RichTextStripper();
            foreach (var key in obj.Select(p => p.Key).ToList())
            {
                if (obj[key] is JsonValue jv && jv.TryGetValue<string>(out var s)
                    && s != null && s.StartsWith(@"{\rtf", StringComparison.Ordinal))
                {
                    obj[key] = stripper.StripRichTextFormat(s);
                }
            }
        }

        private object ProjectCollectionElement(StoryElement element, ElementProjection projection)
        {
            return projection switch
            {
                ElementProjection.IdAndName => new { GUID = element.Uuid, Name = element.Name },
                ElementProjection.BaseStoryElement => BuildBaseProjection(element),
                ElementProjection.FullModel => SerializeElementOutbound(element),
                _ => new { GUID = element.Uuid, Name = element.Name }
            };
        }

        private object BuildBaseProjection(StoryElement element)
        {
            var description = element.Description ?? string.Empty;
            if (description.StartsWith(@"{\rtf", StringComparison.Ordinal))
                description = new RichTextStripper().StripRichTextFormat(description);

            return new
            {
                GUID = element.Uuid,
                Name = element.Name,
                ElementDescription = description,
                Type = element.ElementType.ToString()
            };
        }

        /// <summary>
        /// Issue #120 / #116: after JSON extract, propose Inner Problem structure the model must not invent.
        /// ConflictType is proposed as Person vs. Self (Lists.json craft default). Protagonist and
        /// Antagonist GUIDs both equal the gathered Protagonist (self vs self). These land in pending
        /// only; <see cref="ClassifyScalarUpdates"/> decides Fill vs Protect — Accept never silent-forces
        /// a user-owned different value. ProblemType Decision|Discover comes from the model.
        /// </summary>
        internal void EnrichInnerOuterStructuralFields(
            WorkflowResult result,
            Dictionary<string, StoryElement> gatheredElements)
        {
            if (!gatheredElements.TryGetValue("InnerProblem", out var inner))
            {
                const string skip = "InnerOuter structural enrich skipped: InnerProblem not gathered";
                result.StatusMessages.Add(skip);
                _logger?.LogInformation("{Message}", skip);
                return;
            }

            // Craft default list value — do not trust free model text for ConflictType.
            const string personVsSelf = "Person vs. Self";
            AddOrReplaceScalarUpdate(result, "InnerProblem", inner.Uuid, "ConflictType", personVsSelf);

            if (!gatheredElements.TryGetValue("Protagonist", out var protagonist))
            {
                var partial =
                    $"InnerOuter structural enrich: ConflictType proposed={personVsSelf}; Protagonist not gathered; links not set";
                result.StatusMessages.Add(partial);
                _logger?.LogInformation("{Message}", partial);
                return;
            }

            var protGuid = protagonist.Uuid.ToString();
            AddOrReplaceScalarUpdate(result, "InnerProblem", inner.Uuid, "Protagonist", protGuid);
            AddOrReplaceScalarUpdate(result, "InnerProblem", inner.Uuid, "Antagonist", protGuid);
            var enrichMsg =
                $"InnerOuter structural enrich: ConflictType proposed={personVsSelf}; Protagonist=Antagonist={protGuid}";
            result.StatusMessages.Add(enrichMsg);
            // Preferences log (not chat): durable record for #116 craft/overwrite diagnosis.
            _logger?.LogInformation("{Message}", enrichMsg);
        }

        /// <summary>
        /// Issue #116: classify scalar pending updates against live outline values and session-touch set.
        /// Drops NoOps. Attaches craft explanation on Protect when a <see cref="CraftFieldHints"/> entry exists.
        /// Non-scalar updates stay <see cref="UpdateKind.Unclassified"/>.
        /// </summary>
        /// <param name="sessionTouched">Keys from <see cref="PendingUpdate.SessionTouchKey"/> applied this Collaborator session.</param>
        internal void ClassifyScalarUpdates(
            WorkflowResult result,
            ISet<string>? sessionTouched,
            string? workflowId = null)
        {
            workflowId ??= workflowModel.Label;
            sessionTouched ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var kept = new List<PendingUpdate>();
            var display = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var noOpCount = 0;
            var protectCount = 0;
            var fillCount = 0;
            var refreshCount = 0;

            _logger?.LogInformation(
                "ClassifyScalarUpdates start: workflow={Workflow} pending={Count} sessionTouched={Touched}",
                workflowId, result.PendingUpdates.Count, sessionTouched.Count);

            foreach (var update in result.PendingUpdates)
            {
                if (update.Spec.WriteVia != WriteVia.Scalar)
                {
                    kept.Add(update);
                    display[update.Key] = FormatDisplayValue(update);
                    _logger?.LogInformation(
                        "Classify {Key} kind=Unclassified (non-scalar WriteVia={WriteVia})",
                        update.Key, update.Spec.WriteVia);
                    continue;
                }

                var currentRaw = ReadCurrentScalarDisplay(update.ElementUuid, update.Spec.Property);
                var current = NormalizeCompareText(currentRaw);
                var proposed = NormalizeCompareText(FormatDisplayValue(update));

                UpdateKind kind;
                if (string.Equals(current, proposed, StringComparison.OrdinalIgnoreCase))
                    kind = UpdateKind.NoOp;
                else if (string.IsNullOrEmpty(current))
                    kind = UpdateKind.Fill;
                else if (sessionTouched.Contains(update.SessionTouchKey))
                    kind = UpdateKind.Refresh;
                else
                    kind = UpdateKind.Protect;

                // Preferences log: kind + values so NoOp/Protect is diagnosable without chat.
                _logger?.LogInformation(
                    "Classify {Key} kind={Kind} current=\"{Current}\" proposed=\"{Proposed}\"",
                    update.Key,
                    kind,
                    TruncateForLog(current),
                    TruncateForLog(proposed));

                if (kind == UpdateKind.NoOp)
                {
                    noOpCount++;
                    result.StatusMessages.Add($"No-op (unchanged): {update.Key}");
                    continue;
                }

                switch (kind)
                {
                    case UpdateKind.Fill: fillCount++; break;
                    case UpdateKind.Refresh: refreshCount++; break;
                    case UpdateKind.Protect: protectCount++; break;
                }

                string? craft = null;
                var hint = CraftFieldHints.Find(workflowId, update.ElementLabel, update.Spec.Property);
                if (hint != null && kind == UpdateKind.Protect)
                    craft = hint.Explanation;

                var classified = update with
                {
                    Kind = kind,
                    CurrentDisplay = string.IsNullOrEmpty(currentRaw) ? string.Empty : currentRaw,
                    CraftExplanation = craft
                };
                kept.Add(classified);
                display[classified.Key] = FormatDisplayValue(classified);
                result.StatusMessages.Add($"Classified {classified.Key} as {kind}");
            }

            result.PendingUpdates.Clear();
            result.PendingUpdates.AddRange(kept);
            result.UpdatedProperties.Clear();
            foreach (var kvp in display)
                result.UpdatedProperties[kvp.Key] = kvp.Value;

            _logger?.LogInformation(
                "ClassifyScalarUpdates done: kept={Kept} fill={Fill} refresh={Refresh} protect={Protect} noop={NoOp}",
                kept.Count, fillCount, refreshCount, protectCount, noOpCount);
        }

        /// <summary>Shorten long prose for the Preferences log; keep list values intact when short.</summary>
        private static string TruncateForLog(string? text, int max = 120)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            var t = text.Replace('\r', ' ').Replace('\n', ' ');
            return t.Length <= max ? t : t.Substring(0, max) + "...";
        }

        /// <summary>
        /// True when Accept All may apply this update without an explicit per-field accept.
        /// </summary>
        internal static bool AcceptAllMayApply(PendingUpdate update) => update.AcceptAllMayApply;

        private string ReadCurrentScalarDisplay(Guid elementUuid, string propertyName)
        {
            var got = _storyApi.GetStoryElement(elementUuid);
            if (!got.IsSuccess || got.Payload == null)
                return string.Empty;

            var element = got.Payload;
            var property = element.GetType().GetProperty(propertyName);
            if (property == null || !property.CanRead)
                return string.Empty;

            var value = property.GetValue(element);
            if (value == null)
                return string.Empty;

            if (value is Guid g)
                return g == Guid.Empty ? string.Empty : g.ToString();

            var text = value.ToString() ?? string.Empty;
            if (text.StartsWith(@"{\rtf", StringComparison.Ordinal))
                text = new RichTextStripper().StripRichTextFormat(text) ?? string.Empty;

            return text.Trim();
        }

        private static string NormalizeCompareText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;
            return text.Trim();
        }

        private static string FormatDisplayValue(PendingUpdate update)
        {
            if (update.Value == null)
                return string.Empty;

            return update.Spec.WriteVia switch
            {
                WriteVia.Scalar => update.Value.ToString() ?? string.Empty,
                WriteVia.SimpleList when update.Value is System.Collections.ICollection c => $"{c.Count} items",
                WriteVia.BeatSheet when update.Value is List<BeatInfo> beatList =>
                    FormatBeatSheetDisplay(beatList),
                WriteVia.BeatSheet when update.Value is System.Collections.ICollection c => $"{c.Count} beats",
                WriteVia.CastMembers when update.Value is System.Collections.ICollection c => $"{c.Count} cast members",
                WriteVia.Relationships when update.Value is System.Collections.ICollection c => $"{c.Count} relationships",
                WriteVia.TypedList when update.Value is System.Collections.ICollection c => $"{c.Count} entries",
                _ => update.Value.ToString() ?? string.Empty
            };
        }

        /// <summary>
        /// Injects or replaces a scalar PendingUpdate and its UpdatedProperties display entry.
        /// </summary>
        private static void AddOrReplaceScalarUpdate(
            WorkflowResult result,
            string elementLabel,
            Guid elementUuid,
            string property,
            string value)
        {
            var key = $"{elementLabel}.{property}";
            result.PendingUpdates.RemoveAll(u =>
                u.ElementLabel == elementLabel && u.Spec.Property == property);
            result.PendingUpdates.Add(new PendingUpdate(
                elementLabel,
                elementUuid,
                new PropertySpec(property),
                value));
            result.UpdatedProperties[key] = value;
        }

        /// <summary>
        /// Extracts output values from the AI response without applying them.
        /// Builds PendingUpdates from the JSON using each PropertySpec's JsonKey and WriteVia.
        /// UpdatedProperties is populated as a display-only projection.
        /// </summary>
        internal WorkflowResult ExtractOutputs(
            string aiResponse,
            Dictionary<string, StoryElement> elements,
            List<ElementOutput> outputs)
        {
            var result = WorkflowResult.Succeeded();

            var jsonText = ExtractJson(aiResponse);

            _logger?.LogInformation("=== EXTRACTED JSON ===");
            _logger?.LogInformation(jsonText ?? "(null - extraction failed)");
            _logger?.LogInformation("=== END EXTRACTED JSON ===");

            if (string.IsNullOrEmpty(jsonText))
            {
                result.Success = false;
                result.ErrorMessage = "Could not parse JSON from AI response";
                result.StatusMessages.Add("Failed to extract JSON from response");
                return result;
            }

            try
            {
                using var doc = JsonDocument.Parse(jsonText);
                var root = doc.RootElement;

                foreach (var output in outputs)
                {
                    if (!elements.TryGetValue(output.ElementLabel, out var element))
                    {
                        result.StatusMessages.Add($"Element not found for output: {output.ElementLabel}");
                        continue;
                    }

                    foreach (var spec in output.PropertiesToUpdate)
                    {
                        var jsonKey = spec.JsonKey ?? spec.Property;

                        if (!root.TryGetProperty(jsonKey, out var jsonProp))
                        {
                            result.StatusMessages.Add($"JSON property not found: {jsonKey}");
                            continue;
                        }

                        _logger?.LogInformation($"=== EXTRACTED PROPERTY: {output.ElementLabel}.{spec.Property} (key: {jsonKey}) ===");

                        object? value;
                        string displayValue;

                        switch (spec.WriteVia)
                        {
                            case WriteVia.Scalar:
                                value = jsonProp.ValueKind == JsonValueKind.String
                                    ? jsonProp.GetString()
                                    : jsonProp.ToString();
                                displayValue = value?.ToString() ?? string.Empty;
                                break;

                            case WriteVia.SimpleList:
                                var strList = ExtractStringList(jsonProp, result.StatusMessages, $"{output.ElementLabel}.{spec.Property}");
                                value = strList;
                                displayValue = $"{strList.Count} items";
                                break;

                            case WriteVia.BeatSheet:
                                var beats = ExtractBeatList(jsonProp);
                                value = beats;
                                displayValue = $"{beats.Count} beats";
                                break;

                            case WriteVia.CastMembers:
                                var castGuids = ExtractGuidList(jsonProp);
                                value = castGuids;
                                displayValue = $"{castGuids.Count} cast members";
                                break;

                            case WriteVia.Relationships:
                                var rels = ExtractRelationshipList(jsonProp);
                                value = rels;
                                displayValue = $"{rels.Count} relationships";
                                break;

                            case WriteVia.TypedList:
                                // Extract a JSON array of objects; each object is deserialized
                                // into the collection's typed entry by the API at apply time.
                                // Clone() detaches each element so it survives doc disposal.
                                var typedEntries = ExtractTypedEntries(jsonProp);
                                value = typedEntries;
                                displayValue = $"{typedEntries.Count} entries";
                                break;

                            default:
                                throw new InvalidOperationException($"Unhandled WriteVia value: {spec.WriteVia}");
                        }

                        result.UpdatedProperties[$"{output.ElementLabel}.{spec.Property}"] = displayValue;
                        result.PendingUpdates.Add(new PendingUpdate(output.ElementLabel, element.Uuid, spec, value));
                        result.StatusMessages.Add($"Extracted {output.ElementLabel}.{spec.Property}");
                    }
                }
            }
            catch (JsonException ex)
            {
                result.Success = false;
                result.ErrorMessage = $"JSON parse error: {ex.Message}";
                result.StatusMessages.Add($"JSON parsing failed: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Applies pending updates to story elements via the API.
        /// Dispatches each PendingUpdate by its WriteVia mechanism.
        /// The explicit default: arm throws on any unhandled WriteVia value.
        /// </summary>
        internal int ApplyUpdates(WorkflowResult result, Dictionary<string, StoryElement> gatheredElements)
        {
            int appliedCount = 0;

            foreach (var update in result.PendingUpdates)
            {
                var spec = update.Spec;
                var uuid = update.ElementUuid;

                switch (spec.WriteVia)
                {
                    case WriteVia.Scalar:
                    {
                        var applyResult = _storyApi.UpdateElementProperty(uuid, spec.Property, update.Value ?? string.Empty);
                        if (applyResult.IsSuccess)
                            appliedCount++;
                        else
                            _logger?.LogWarning($"Failed to apply {update.ElementLabel}.{spec.Property}: {applyResult.ErrorMessage}");
                        break;
                    }

                    case WriteVia.SimpleList:
                    {
                        if (update.Value is not List<string> entries)
                        {
                            _logger?.LogWarning($"{update.ElementLabel}.{spec.Property}: expected List<string> for SimpleList");
                            break;
                        }
                        // Clear existing entries by repeatedly removing at index 0
                        var existing = _storyApi.GetStoryElement(uuid);
                        if (existing.IsSuccess && existing.Payload != null)
                        {
                            var prop = existing.Payload.GetType().GetProperty(spec.Property);
                            var currentList = prop?.GetValue(existing.Payload) as System.Collections.IList;
                            int count = currentList?.Count ?? 0;
                            for (int i = 0; i < count; i++)
                                _storyApi.RemoveCollectionEntry(uuid, spec.Property, 0);
                        }
                        foreach (var entry in entries)
                            _storyApi.AddCollectionEntry(uuid, spec.Property, entry);
                        appliedCount++;
                        break;
                    }

                    case WriteVia.BeatSheet:
                    {
                        // #167: merge proposed beats; never wipe filled assignments.
                        if (update.Value is not List<BeatInfo> beats) break;
                        ApplyBeatSheetMerge(uuid, beats, result);
                        appliedCount++;
                        break;
                    }

                    case WriteVia.CastMembers:
                    {
                        if (update.Value is not List<Guid> charGuids) break;
                        var validChars = new System.Collections.Generic.HashSet<Guid>(
                            GetCandidateGuids(StoryItemType.Character));
                        foreach (var charGuid in charGuids)
                        {
                            if (validChars.Contains(charGuid))
                                _storyApi.AddCastMember(uuid, charGuid);
                            else
                                result.StatusMessages.Add(
                                    $"CastMembers: GUID {charGuid} not in character candidate set; skipped");
                        }
                        appliedCount++;
                        break;
                    }

                    case WriteVia.Relationships:
                    {
                        if (update.Value is not List<RelationshipInfo> relationships) break;
                        var validChars = new System.Collections.Generic.HashSet<Guid>(
                            GetCandidateGuids(StoryItemType.Character));
                        foreach (var rel in relationships)
                        {
                            if (validChars.Contains(rel.RecipientGuid))
                                _storyApi.AddRelationship(uuid, rel.RecipientGuid, rel.Description, rel.Mirror);
                            else
                                result.StatusMessages.Add(
                                    $"Relationships: recipient GUID {rel.RecipientGuid} not in character candidate set; skipped");
                        }
                        appliedCount++;
                        break;
                    }

                    case WriteVia.TypedList:
                    {
                        if (update.Value is not List<JsonElement> typedEntries)
                        {
                            _logger?.LogWarning($"{update.ElementLabel}.{spec.Property}: expected List<JsonElement> for TypedList");
                            break;
                        }
                        // Clear existing entries by repeatedly removing at index 0,
                        // then add each entry; the API deserializes the JSON object
                        // into the collection's typed element (e.g. CultureEntry).
                        var existing = _storyApi.GetStoryElement(uuid);
                        if (existing.IsSuccess && existing.Payload != null)
                        {
                            var prop = existing.Payload.GetType().GetProperty(spec.Property);
                            var currentList = prop?.GetValue(existing.Payload) as System.Collections.IList;
                            int count = currentList?.Count ?? 0;
                            for (int i = 0; i < count; i++)
                                _storyApi.RemoveCollectionEntry(uuid, spec.Property, 0);
                        }
                        foreach (var entry in typedEntries)
                        {
                            var addResult = _storyApi.AddCollectionEntry(uuid, spec.Property, entry);
                            if (!addResult.IsSuccess)
                                result.StatusMessages.Add(
                                    $"{spec.Property}: entry rejected: {addResult.ErrorMessage}");
                        }
                        appliedCount++;
                        break;
                    }

                    default:
                        throw new InvalidOperationException($"Unhandled WriteVia value: {spec.WriteVia}");
                }
            }

            _logger?.LogInformation($"Applied {appliedCount} pending updates");
            return appliedCount;
        }

        private IEnumerable<Guid> GetCandidateGuids(StoryItemType type)
        {
            var result = _storyApi.GetElementsByType(type);
            return result.IsSuccess && result.Payload != null
                ? result.Payload.Select(e => e.Uuid)
                : Enumerable.Empty<Guid>();
        }

        private static List<string> ExtractStringList(JsonElement elem, List<string> statusMessages, string context)
        {
            if (elem.ValueKind == JsonValueKind.Array)
            {
                return elem.EnumerateArray()
                    .Select(e => e.GetString() ?? e.ToString())
                    .ToList();
            }
            // Model returned a bare string; treat as single entry, add a drift note.
            statusMessages.Add($"{context}: expected JSON array for SimpleList, got {elem.ValueKind}; treating as single entry");
            return new List<string> { elem.GetString() ?? elem.ToString() };
        }

        private static List<BeatInfo> ExtractBeatList(JsonElement elem)
        {
            if (elem.ValueKind != JsonValueKind.Array) return new List<BeatInfo>();
            var beats = new List<BeatInfo>();
            foreach (var beatElem in elem.EnumerateArray())
            {
                var title = beatElem.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                var desc = beatElem.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";
                Guid? assigned = null;
                if (beatElem.TryGetProperty("assigned_element", out var ae))
                {
                    if (ae.ValueKind == JsonValueKind.String
                        && Guid.TryParse(ae.GetString(), out var g))
                        assigned = g;
                }
                string? sceneName = null;
                if (beatElem.TryGetProperty("scene_name", out var sn)
                    && sn.ValueKind == JsonValueKind.String)
                {
                    sceneName = sn.GetString();
                }
                beats.Add(new BeatInfo(title, desc, assigned, sceneName));
            }
            return beats;
        }

        /// <summary>
        /// Collaborator #150: abort when ProblemCategory is empty or Story Problem.
        /// Returns null when the run may continue.
        /// </summary>
        internal static string? ValidateBeatScenesCategory(string? problemCategory)
        {
            if (string.IsNullOrWhiteSpace(problemCategory))
                return "Set Problem Category before Scenes from Beats.";
            if (string.Equals(problemCategory.Trim(), "Story Problem", StringComparison.Ordinal))
                return "Use Structure on the story problem; run Scenes from Beats on a complication or other category.";
            return null;
        }

        private static string FormatBeatSheetDisplay(List<BeatInfo> beats)
        {
            var createCount = beats.Count(b => !string.IsNullOrWhiteSpace(b.SceneName));
            if (createCount > 0)
                return $"{beats.Count} beats ({createCount} new scenes for empty beats)";
            return $"{beats.Count} beats";
        }

        /// <summary>
        /// Collaborator #150: inject Stock Scenes catalog into args for BeatScenes.
        /// </summary>
        internal void EnrichWithStockScenes(Dictionary<string, string> args)
        {
            var catsResult = _storyApi.GetStockSceneCategories();
            if (!catsResult.IsSuccess || catsResult.Payload == null)
            {
                _logger?.LogWarning("BeatScenes: no stock scene categories from API");
                args["StockScenes"] = string.Empty;
                return;
            }

            var sb = new System.Text.StringBuilder();
            foreach (var category in catsResult.Payload)
            {
                sb.AppendLine($"### {category}");
                var scenesResult = _storyApi.GetStockScenes(category);
                if (scenesResult.IsSuccess && scenesResult.Payload != null)
                {
                    foreach (var scene in scenesResult.Payload)
                        sb.AppendLine($"- {scene}");
                }
                sb.AppendLine();
            }
            args["StockScenes"] = sb.ToString();
            _logger?.LogInformation("Injected StockScenes for BeatScenes ({Length} chars)", args["StockScenes"].Length);
        }

        /// <summary>
        /// Collaborator #167: install or merge beat proposals without clearing filled assignments.
        /// Empty sheet: create proposed beats and assign valid candidates.
        /// Non-empty: fill blank descriptions; assign only empty slots; never reassign.
        /// Collaborator #150 (BeatScenes only): empty slots with SceneName create a Scene under
        /// the problem and assign it; Structure never creates.
        /// </summary>
        internal void ApplyBeatSheetMerge(Guid problemUuid, List<BeatInfo> proposed, WorkflowResult result)
        {
            if (proposed == null || proposed.Count == 0)
            {
                result.StatusMessages.Add("BeatSheet: empty proposal; no changes");
                return;
            }

            var problemGuids = new HashSet<Guid>(GetCandidateGuids(StoryItemType.Problem));
            var sceneGuids = new HashSet<Guid>(GetCandidateGuids(StoryItemType.Scene));
            var validAssignGuids = new HashSet<Guid>(problemGuids.Concat(sceneGuids));
            bool allowSceneCreate = string.Equals(workflowModel.Label, "BeatScenes", StringComparison.Ordinal);

            var existingResult = _storyApi.GetProblemStructure(problemUuid);
            if (!existingResult.IsSuccess)
            {
                result.StatusMessages.Add($"BeatSheet: cannot load structure for {problemUuid}");
                return;
            }

            var existingBeats = existingResult.Payload.Beats?.ToList()
                ?? new List<(string BeatTitle, string BeatDescription, Guid? LinkedElement)>();

            // GUIDs already assigned on this problem (preserve; block multi-problem on same sheet).
            var usedOnSheet = new HashSet<Guid>(
                existingBeats
                    .Where(b => b.LinkedElement.HasValue && b.LinkedElement.Value != Guid.Empty)
                    .Select(b => b.LinkedElement!.Value));

            if (existingBeats.Count == 0)
            {
                for (int i = 0; i < proposed.Count; i++)
                {
                    var beat = proposed[i];
                    var title = string.IsNullOrWhiteSpace(beat.Title) ? $"Beat {i + 1}" : beat.Title.Trim();
                    var desc = beat.Description?.Trim() ?? string.Empty;
                    _storyApi.CreateBeat(problemUuid, title, desc);
                    TryFillEmptyBeat(problemUuid, i, beat, allowSceneCreate, validAssignGuids, problemGuids, usedOnSheet, result);
                }
                return;
            }

            // Append extra proposed rows when the model grows the sheet (do not delete extras).
            for (int i = existingBeats.Count; i < proposed.Count; i++)
            {
                var beat = proposed[i];
                var title = string.IsNullOrWhiteSpace(beat.Title) ? $"Beat {i + 1}" : beat.Title.Trim();
                var desc = beat.Description?.Trim() ?? string.Empty;
                _storyApi.CreateBeat(problemUuid, title, desc);
            }

            // Re-read after possible creates.
            existingResult = _storyApi.GetProblemStructure(problemUuid);
            existingBeats = existingResult.IsSuccess
                ? existingResult.Payload.Beats?.ToList()
                    ?? new List<(string, string, Guid?)>()
                : existingBeats;

            int pairCount = Math.Min(existingBeats.Count, proposed.Count);
            for (int i = 0; i < pairCount; i++)
            {
                var current = existingBeats[i];
                var beat = proposed[i];

                var newTitle = string.IsNullOrWhiteSpace(current.BeatTitle) && !string.IsNullOrWhiteSpace(beat.Title)
                    ? beat.Title.Trim()
                    : current.BeatTitle;
                // Prefer keep filled description; fill blank only.
                var newDesc = string.IsNullOrWhiteSpace(current.BeatDescription) && !string.IsNullOrWhiteSpace(beat.Description)
                    ? beat.Description.Trim()
                    : current.BeatDescription;

                if (!string.Equals(newTitle, current.BeatTitle, StringComparison.Ordinal)
                    || !string.Equals(newDesc, current.BeatDescription, StringComparison.Ordinal))
                {
                    _storyApi.UpdateBeat(problemUuid, i, newTitle ?? string.Empty, newDesc ?? string.Empty);
                }

                var alreadyFilled = current.LinkedElement.HasValue && current.LinkedElement.Value != Guid.Empty;
                if (alreadyFilled)
                    continue;

                TryFillEmptyBeat(problemUuid, i, beat, allowSceneCreate, validAssignGuids, problemGuids, usedOnSheet, result);
            }
        }

        /// <summary>
        /// Empty beat only: BeatScenes may create a Scene from SceneName; otherwise assign GUID.
        /// </summary>
        private void TryFillEmptyBeat(
            Guid problemUuid,
            int beatIndex,
            BeatInfo beat,
            bool allowSceneCreate,
            HashSet<Guid> validAssignGuids,
            HashSet<Guid> problemGuids,
            HashSet<Guid> usedOnSheet,
            WorkflowResult result)
        {
            if (allowSceneCreate && !string.IsNullOrWhiteSpace(beat.SceneName))
            {
                var name = beat.SceneName.Trim();
                var addResult = _storyApi.AddElement(StoryItemType.Scene, problemUuid.ToString(), name);
                if (!addResult.IsSuccess)
                {
                    result.StatusMessages.Add(
                        $"Beat {beatIndex} scene create failed: {addResult.ErrorMessage}");
                    return;
                }

                var newGuid = addResult.Payload;
                validAssignGuids.Add(newGuid);
                TryAssignBeat(problemUuid, beatIndex, newGuid, validAssignGuids, problemGuids, usedOnSheet, result);
                result.StatusMessages.Add($"Beat {beatIndex}: created Scene '{name}' ({newGuid})");
                return;
            }

            TryAssignBeat(problemUuid, beatIndex, beat.AssignedElement, validAssignGuids, problemGuids, usedOnSheet, result);
        }

        private void TryAssignBeat(
            Guid problemUuid,
            int beatIndex,
            Guid? assigned,
            HashSet<Guid> validAssignGuids,
            HashSet<Guid> problemGuids,
            HashSet<Guid> usedOnSheet,
            WorkflowResult result)
        {
            if (!assigned.HasValue || assigned.Value == Guid.Empty)
                return;

            if (!validAssignGuids.Contains(assigned.Value))
            {
                result.StatusMessages.Add(
                    $"Beat {beatIndex} assigned_element {assigned.Value} not in candidate set; left unassigned");
                return;
            }

            // One problem per beat sheet (and prefer one scene): skip if already used on this sheet.
            if (usedOnSheet.Contains(assigned.Value))
            {
                result.StatusMessages.Add(
                    $"Beat {beatIndex} assigned_element {assigned.Value} already used on this sheet; left unassigned");
                return;
            }

            var assignResult = _storyApi.AssignElementToBeat(problemUuid, beatIndex, assigned.Value);
            if (assignResult.IsSuccess)
                usedOnSheet.Add(assigned.Value);
            else
                result.StatusMessages.Add(
                    $"Beat {beatIndex} assign failed: {assignResult.ErrorMessage}");
        }

        private static List<JsonElement> ExtractTypedEntries(JsonElement elem)
        {
            var entries = new List<JsonElement>();
            if (elem.ValueKind != JsonValueKind.Array) return entries;
            foreach (var entry in elem.EnumerateArray())
            {
                if (entry.ValueKind == JsonValueKind.Object)
                    entries.Add(entry.Clone());
            }
            return entries;
        }

        private static List<Guid> ExtractGuidList(JsonElement elem)
        {
            if (elem.ValueKind != JsonValueKind.Array) return new List<Guid>();
            var guids = new List<Guid>();
            foreach (var entry in elem.EnumerateArray())
            {
                string? guidStr = null;
                if (entry.ValueKind == JsonValueKind.String)
                    guidStr = entry.GetString();
                else if (entry.ValueKind == JsonValueKind.Object)
                {
                    if (entry.TryGetProperty("guid", out var g) || entry.TryGetProperty("GUID", out g))
                        guidStr = g.GetString();
                }
                if (Guid.TryParse(guidStr, out var parsed))
                    guids.Add(parsed);
            }
            return guids;
        }

        private static List<RelationshipInfo> ExtractRelationshipList(JsonElement elem)
        {
            var results = new List<RelationshipInfo>();
            if (elem.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in elem.EnumerateArray())
                    ParseRelationshipEntry(entry, results);
            }
            else if (elem.ValueKind == JsonValueKind.Object)
            {
                ParseRelationshipEntry(elem, results);
            }
            return results;
        }

        private static void ParseRelationshipEntry(JsonElement entry, List<RelationshipInfo> results)
        {
            string? guidStr = null;
            if (entry.TryGetProperty("recipient_guid", out var rg) || entry.TryGetProperty("GUID", out rg))
                guidStr = rg.GetString();
            if (!Guid.TryParse(guidStr, out var recipientGuid)) return;
            var desc = entry.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";
            var mirror = entry.TryGetProperty("mirror", out var m) && m.GetBoolean();
            results.Add(new RelationshipInfo(recipientGuid, desc, mirror));
        }

        /// <summary>
        /// Extracts JSON object from a string that may contain surrounding text.
        /// </summary>
        internal static string? ExtractJson(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;

            var jsonStart = text.IndexOf("{");
            var jsonEnd = text.LastIndexOf("}");

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                return text.Substring(jsonStart, jsonEnd - jsonStart + 1);
            }

            return null;
        }

        /// <summary>
        /// Fetches examples for each name declared in the workflow's ExampleLists and adds them to args.
        /// </summary>
        internal void EnrichWithExamples(Dictionary<string, string> args)
        {
            foreach (var propertyName in workflowModel.GetIO().ExampleLists)
            {
                var result = _storyApi.GetExamples(propertyName);
                if (result.IsSuccess && result.Payload != null && result.Payload.Any())
                {
                    var formatted = string.Join(", ", result.Payload);
                    args[$"{propertyName}_examples"] = formatted;
                    _logger?.LogInformation($"Injected examples for {propertyName}: {result.Payload.Count()} items");
                }
                else
                {
                    _logger?.LogWarning($"No examples found for property: {propertyName}");
                }
            }
        }

        /// <summary>
        /// Enriches args with story context (still client-built for #106).
        /// </summary>
        internal void EnrichWithStoryContext(Dictionary<string, string> args, Dictionary<string, StoryElement> gatheredElements, WorkflowIO workflowIO)
        {
            try
            {
                StoryElement? targetElement = null;
                StoryItemType targetType = StoryItemType.StoryOverview;

                foreach (var output in workflowIO.Outputs)
                {
                    if (gatheredElements.TryGetValue(output.ElementLabel, out var element))
                    {
                        targetElement = element;
                        targetType = output.ElementType;
                        break;
                    }
                }

                if (targetElement == null)
                {
                    foreach (var input in workflowIO.RequiredInputs)
                    {
                        if (gatheredElements.TryGetValue(input.ElementLabel, out var element))
                        {
                            targetElement = element;
                            targetType = input.ElementType;
                            break;
                        }
                    }
                }

                var resolver = new ContextResolver();
                var spec = resolver.GetContextFor(workflowModel.Label, targetType);

                var builder = new StoryContextBuilder(_storyApi);
                var context = builder.BuildContext(targetElement, spec, storyModel);

                args["StoryContext"] = !string.IsNullOrWhiteSpace(context) ? context : string.Empty;

                if (!string.IsNullOrWhiteSpace(context))
                    _logger?.LogInformation($"Enriched with story context ({context.Length} chars) for {workflowModel.Label} workflow");
                else
                    _logger?.LogInformation($"No story context generated for {workflowModel.Label} workflow");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"Failed to enrich story context: {ex.Message}");
                args["StoryContext"] = string.Empty;
            }
        }

        /// <summary>
        /// Applies user settings to the prompt as instructions.
        /// </summary>
        internal void ApplySettings(Dictionary<string, string> args)
        {
            var instructions = new List<string>();

            instructions.Add(_settings.Terseness switch
            {
                TersenessLevel.Concise => "Be concise. Brief responses only.",
                TersenessLevel.Detailed => "Provide detailed explanations with examples.",
                _ => ""
            });

            instructions.Add(_settings.ContentPreservation switch
            {
                ContentPreservationLevel.Strict => "Preserve the user's exact wording. Only fill gaps.",
                // The coach system message on the Worker forbids revising text that is
                // already sound, and it outranks this string. Flexible widens what counts
                // as worth proposing; it does not license the rewrite it used to promise.
                ContentPreservationLevel.Flexible => "The writer welcomes suggestions on filled fields as well as blank ones.",
                _ => ""
            });

            if (!string.IsNullOrWhiteSpace(_settings.GenrePreferences))
                instructions.Add($"The user prefers these genres: {_settings.GenrePreferences}");

            if (!string.IsNullOrWhiteSpace(_settings.StoryFormLikes))
                instructions.Add($"The user likes these story forms: {_settings.StoryFormLikes}");
            if (!string.IsNullOrWhiteSpace(_settings.StoryFormDislikes))
                instructions.Add($"Avoid suggesting these story forms: {_settings.StoryFormDislikes}");

            var result = string.Join(" ", instructions.Where(s => !string.IsNullOrEmpty(s)));
            args["UserSettings"] = result;

            if (!string.IsNullOrEmpty(result))
                _logger?.LogInformation("Applied settings: {Settings}", result);
        }

        /// <summary>
        /// Posts the workflow request to the proxy's /v1/workflow endpoint, retrying once on a
        /// fresh connection if the stream comes back truncated (issue #94 design section 5 item 2).
        /// Returns the SSE response text, the X-Template-Hash header value (null on fallback path),
        /// the cost reported by the proxy's collab_cost event (null when absent), and whether the
        /// returned stream (after the retry, if one occurred) is complete.
        /// </summary>
        private async Task<(string Content, string? TemplateHash, ProxyCostInfo? Cost, bool Complete)> PostToProxyAsync(WorkflowProxyBody body)
        {
            var proxyBaseUrl = Environment.GetEnvironmentVariable("COLLAB_PROXY_URL")
                ?? KernelFactory.DefaultProxyBaseUrl;

            var payload = JsonSerializer.Serialize(new
            {
                workflowId = workflowModel.Label,
                args = body.Args,
                elements = body.Elements
            });

            return await ExecuteWithTruncationRetryAsync(
                () => PostToProxyOnceAsync(proxyBaseUrl, payload), ResetHttpClient);
        }

        /// <summary>
        /// One attempt of the full send-plus-read sequence PostToProxyAsync's truncation retry
        /// wraps: credential resolution via SendWithReactivationRetryAsync, the 401 guard,
        /// EnsureNotOutOfCredits, EnsureSuccessStatusCode, the X-Template-Hash header read, then
        /// ReadSseStreamAsync (issue #94 design section 5 item 2).
        /// </summary>
        private async Task<(string Content, string? TemplateHash, ProxyCostInfo? Cost, bool Complete)> PostToProxyOnceAsync(string proxyBaseUrl, string payload)
        {
            var response = await SendWithReactivationRetryAsync(
                () => SendWorkflowRequestAsync(proxyBaseUrl, payload), KernelFactory.ReactivateAsync);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                response.Dispose();
                // No activation token held (never activated, or one reactivation attempt above
                // still didn't produce one): refuse client-side rather than let
                // EnsureSuccessStatusCode()'s generic "does not indicate success: 401" surface.
                throw new InvalidOperationException(
                    "No activation token held; refusing to call the proxy workflow endpoint without " +
                    "a credential. Subscribe to Collaborator, or (for dev builds) enroll on the allowlist.");
            }

            EnsureNotOutOfCredits(response);
            response.EnsureSuccessStatusCode();

            string? templateHash = null;
            if (response.Headers.TryGetValues("X-Template-Hash", out var hashValues))
                templateHash = hashValues.FirstOrDefault();

            var (content, cost, complete) = await ReadSseStreamAsync(response);
            return (content, templateHash, cost, complete);
        }

        /// <summary>
        /// Sends one attempt of the workflow POST. Resolved per call: a subscriber's (or
        /// allowlisted dev/tester's) activation JWT is the sole credential (issue #90 step 8 item
        /// 6 retires the COLLAB_PROXY_TOKEN shared-secret fallback), and the JWT rotates
        /// ~12-hourly so it must never be cached across calls. No credential available returns a
        /// synthetic 401 rather than throwing, so <see cref="SendWithReactivationRetryAsync" />
        /// handles a missing credential the same way it handles the Worker's own 401 (design
        /// section 11's failure-mode row groups "JWT missing, invalid, or expired" together).
        /// </summary>
        private async Task<HttpResponseMessage> SendWorkflowRequestAsync(string proxyBaseUrl, string payload)
        {
            var credential = KernelFactory.ResolveWorkflowCredential();
            if (string.IsNullOrWhiteSpace(credential))
                return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);

            using var request = CreateWorkflowRequest(proxyBaseUrl, credential, payload);
            return await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        }

        /// <summary>
        /// 401-refresh-once-and-retry (issue #90 step 8 item 7 / design section 11's expired-token
        /// row): a workflow call that comes back 401 refreshes the activation once through
        /// re-activation and retries with the refreshed credential before failing. Exactly one
        /// retry -- a second 401 is returned as-is so the caller's handling surfaces it rather than
        /// looping. Reactivation is <see cref="KernelFactory.ReactivateAsync"/> (shared with chat,
        /// Collaborator #95). internal static and testable without a live HTTP transport: sendRequest
        /// and reactivate are injected, matching CreateWorkflowRequest/
        /// EnsureNotOutOfCredits's existing testable-without-network pattern in this class.
        /// </summary>
        internal static async Task<HttpResponseMessage> SendWithReactivationRetryAsync(
            Func<Task<HttpResponseMessage>> sendRequest, Func<Task> reactivate)
        {
            var response = await sendRequest();
            if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
                return response;

            response.Dispose();
            await reactivate();
            return await sendRequest();
        }

        /// <summary>
        /// Issue #90 design section 10 "The cutoff" (ruling of 2026-07-15, step 10): the Worker
        /// refuses a workflow call with 429 before any upstream dispatch when the caller's balance
        /// is at or below zero. Checked before EnsureSuccessStatusCode() so the thrown exception is
        /// StoryCADLib.Services.Store.OutOfCreditsException, not the generic HttpRequestException
        /// EnsureSuccessStatusCode() would otherwise produce (message: "Response status code does
        /// not indicate success: 429"). Deliberately not an HttpRequestException itself, so it can
        /// never be mistaken for a transport failure and swallowed by ordinary HTTP error handling:
        /// an out-of-credits refusal must always reach the caller as its own recognizable state
        /// (step 8 item 5 retired the direct-to-OpenAI fallback that used to make this distinction
        /// load-bearing for a different reason -- routing a capped user's personal API key around
        /// the balance cutoff -- but the exception stays distinct regardless). internal static and
        /// side-effect-free on success, matching CreateWorkflowRequest/ReadSseStreamAsync's existing
        /// testable-without-HTTP-transport pattern in this class.
        /// </summary>
        internal static void EnsureNotOutOfCredits(HttpResponseMessage response)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                throw new StoryCADLib.Services.Store.OutOfCreditsException();
        }

        /// <summary>
        /// Builds the /workflow POST with the resolved Bearer credential attached. The
        /// activation contract requires the credential on every Collaborator call.
        /// </summary>
        internal static HttpRequestMessage CreateWorkflowRequest(string proxyBaseUrl, string credential, string payload)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{proxyBaseUrl}/workflow");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credential);
            request.Content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
            return request;
        }

        /// <summary>
        /// Reads the /workflow endpoint's SSE stream to completion. Complete is true iff a
        /// <c>data: [DONE]</c> line was actually read -- the Worker appends it on every complete
        /// stream, including the cost-missing and unpriced-model paths, so its absence is a
        /// truncation with no false-positive source (issue #94 design section 5 item 1). collab_cost
        /// stays optional (ADR-002 fail-open): [DONE] without a cost event is still Complete=true.
        /// </summary>
        internal static async Task<(string Content, ProxyCostInfo? Cost, bool Complete)> ReadSseStreamAsync(HttpResponseMessage response)
        {
            var sb = new System.Text.StringBuilder();
            ProxyCostInfo? cost = null;
            bool complete = false;
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (!line.StartsWith("data: ")) continue;
                var data = line.Substring(6).Trim();
                if (data == "[DONE]") { complete = true; break; }
                try
                {
                    using var doc = JsonDocument.Parse(data);
                    if (doc.RootElement.TryGetProperty("choices", out var choices) &&
                        choices.GetArrayLength() > 0 &&
                        choices[0].TryGetProperty("delta", out var delta) &&
                        delta.TryGetProperty("content", out var content) &&
                        content.ValueKind == JsonValueKind.String)
                    {
                        sb.Append(content.GetString());
                    }
                    else if (doc.RootElement.TryGetProperty("collab_cost", out var collabCost))
                    {
                        // Shared with the X-Collab-Cost header path: the Worker emits an
                        // identical payload on both. Note the parser requires only `model`,
                        // not `workflow` — the chat route's streaming branch sends
                        // workflow: null, and the previous non-null guard here silently
                        // dropped every one of those events.
                        cost = ProxyCostParser.TryParse(collabCost);
                    }
                }
                catch (JsonException) { /* malformed chunk — skip */ }
            }
            return (sb.ToString(), cost, complete);
        }

        /// <summary>
        /// Truncation-retry-once policy (issue #94 design section 5 items 2-3): a stream that comes
        /// back incomplete (no <c>data: [DONE]</c> read -- <see cref="ReadSseStreamAsync"/>) resets
        /// the shared HttpClient (abandoning the pooled connection -- server evidence pins it to a
        /// flagged Cloudflare isolate after a kill, so a same-connection retry would die again) and
        /// re-sends once end-to-end through the same credential path before returning the (possibly
        /// still incomplete) result to the caller. Exactly one retry, mirroring
        /// <see cref="SendWithReactivationRetryAsync"/>'s refresh-once-and-retry shape. internal
        /// static and testable without a live HTTP transport: attempt and reset are injected,
        /// matching this class's existing testable-without-network pattern.
        /// </summary>
        internal static async Task<(string Content, string? TemplateHash, ProxyCostInfo? Cost, bool Complete)> ExecuteWithTruncationRetryAsync(
            Func<Task<(string Content, string? TemplateHash, ProxyCostInfo? Cost, bool Complete)>> attempt,
            Action reset)
        {
            var result = await attempt();
            if (result.Complete)
                return result;

            reset();
            return await attempt();
        }

        /// <summary>
        /// Issue #94 design section 5 item 3 ("surface, never mask"): builds the failed result for a
        /// stream that came back incomplete even after PostToProxyAsync's one truncation retry. The
        /// partial text is preserved in RawResponse for diagnostics; it never reaches ExtractOutputs
        /// or a Success result. internal static and pinned directly by its own test (driving RunAsync
        /// end-to-end here would require faking HTTP), per the NO MOCKS rule in
        /// Collaborator/CLAUDE.md.
        /// </summary>
        internal static WorkflowResult BuildTruncationFailureResult(string partialContent)
        {
            return new WorkflowResult
            {
                Success = false,
                ErrorMessage = "The AI response stream ended before completion; the answer may be incomplete. Please try again.",
                RawResponse = partialContent
            };
        }


        /// <summary>
        /// Builds a response for stub workflows (those without prompts yet).
        /// </summary>
        internal WorkflowResult BuildStubResponse()
        {
            var result = WorkflowResult.Succeeded();
            var workflowIO = workflowModel.GetIO();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Workflow: {workflowModel.Title}");
            sb.AppendLine();
            sb.AppendLine("This workflow is planned but not yet implemented.");
            sb.AppendLine();
            sb.AppendLine($"Description: {workflowModel.Description}");

            if (!string.IsNullOrEmpty(workflowModel.Explanation))
            {
                sb.AppendLine();
                sb.AppendLine($"Details: {workflowModel.Explanation}");
            }

            if (workflowIO.RequiredInputs.Count > 0 || workflowIO.OptionalInputs.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Inputs:");
                foreach (var input in workflowIO.RequiredInputs)
                    sb.AppendLine($"  • {input.ElementLabel} ({input.ElementType}) - required");
                foreach (var input in workflowIO.OptionalInputs)
                    sb.AppendLine($"  • {input.ElementLabel} ({input.ElementType}) - optional");
            }

            if (workflowIO.Outputs.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Outputs:");
                foreach (var output in workflowIO.Outputs)
                {
                    var props = string.Join(", ", output.PropertiesToUpdate.Select(p => p.Property));
                    var action = "updates";
                    sb.AppendLine($"  • {action} {output.ElementLabel}: {props}");
                }
            }

            sb.AppendLine();
            sb.AppendLine("Check back soon - this workflow is in development!");

            result.AssembledPrompt = sb.ToString();
            result.RawResponse = "(stub workflow - no AI call made)";

            return result;
        }
    }
}
