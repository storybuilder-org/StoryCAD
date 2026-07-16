using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using CollaboratorLib.Context;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using StoryCADLib.Models;
using StoryCADLib.Services.API;
using StoryCADLib.Services.Collaborator.Contracts;
using StoryCADLib.Services.Reports;
using StoryCollaborator.Models;
using StoryCollaborator.Workflows;

namespace StoryCollaborator
{
    public sealed record WorkflowRunOutcome(WorkflowResult Result, int AppliedCount);

    internal class WorkflowRunner
    {
        private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromMinutes(3) };

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

                var kernelArgs = BuildKernelArguments(gatheredElements);
                result.StatusMessages.Add($"Built arguments from {gatheredElements.Count} elements");

                EnrichWithStoryContext(kernelArgs, gatheredElements, workflowIO);
                ApplySettings(kernelArgs);

                if (workflowIO.ExampleLists.Count > 0)
                    EnrichWithExamples(kernelArgs);

                // Issue #90 step 8 item 5: the direct-to-OpenAI fallback retired along with
                // OPENAI_API_KEY on the client. A proxy failure now propagates to the outer
                // catch clauses below rather than retrying against OpenAI directly.
                result.AssembledPrompt = null;
                var (proxyContent, proxyHash, proxyCost) = await PostToProxyAsync(kernelArgs);
                string planResult = proxyContent;
                result.RemoteTemplateHash = proxyHash;
                result.Cost = proxyCost;

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

        /// <summary>
        /// Builds KernelArguments from gathered elements and injects candidate lists
        /// for GUID-based mechanisms (CastMembers, Relationships, BeatSheet).
        /// </summary>
        internal KernelArguments BuildKernelArguments(Dictionary<string, StoryElement> elements)
        {
            var kernelArgs = new KernelArguments();
            var rtfStripper = new RichTextStripper();

            foreach (var (label, element) in elements)
            {
                if (element == null) continue;

                kernelArgs[label] = JsonSerializer.Serialize(element, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                var elementType = element.GetType();
                foreach (var prop in elementType.GetProperties())
                {
                    if (prop.CanRead)
                    {
                        var value = prop.GetValue(element);
                        var stringValue = value?.ToString() ?? string.Empty;

                        if (stringValue.StartsWith(@"{\rtf"))
                        {
                            stringValue = rtfStripper.StripRichTextFormat(stringValue);
                        }

                        kernelArgs[$"{label}_{prop.Name}"] = stringValue;
                    }
                }
            }

            // Inject candidate lists for GUID-based mechanisms
            var allSpecs = workflowModel.GetIO().Outputs
                .SelectMany(o => o.PropertiesToUpdate)
                .ToList();

            bool needsCharacters = allSpecs.Any(s =>
                s.WriteVia == WriteVia.CastMembers || s.WriteVia == WriteVia.Relationships);
            bool needsBeatElements = allSpecs.Any(s => s.WriteVia == WriteVia.BeatSheet);

            var serOpts = new JsonSerializerOptions { WriteIndented = false };

            if (needsCharacters)
            {
                var chars = _storyApi.GetElementsByType(StoryItemType.Character);
                if (chars.IsSuccess)
                {
                    kernelArgs["CharacterChoices"] = JsonSerializer.Serialize(
                        chars.Payload?.Select(e => new { GUID = e.Uuid, Name = e.Name }) ?? Enumerable.Empty<object>(),
                        serOpts);
                }
            }

            if (needsBeatElements)
            {
                var problems = _storyApi.GetElementsByType(StoryItemType.Problem);
                if (problems.IsSuccess)
                {
                    kernelArgs["ProblemChoices"] = JsonSerializer.Serialize(
                        problems.Payload?.Select(e => new { GUID = e.Uuid, Name = e.Name }) ?? Enumerable.Empty<object>(),
                        serOpts);
                }

                var scenes = _storyApi.GetElementsByType(StoryItemType.Scene);
                if (scenes.IsSuccess)
                {
                    kernelArgs["SceneChoices"] = JsonSerializer.Serialize(
                        scenes.Payload?.Select(e => new { GUID = e.Uuid, Name = e.Name }) ?? Enumerable.Empty<object>(),
                        serOpts);
                }
            }

            return kernelArgs;
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
                        if (update.Value is not List<BeatInfo> beats) break;

                        // Clear existing beats
                        var structure = _storyApi.GetProblemStructure(uuid);
                        if (structure.IsSuccess)
                        {
                            int beatCount = structure.Payload.Beats.Count();
                            for (int i = 0; i < beatCount; i++)
                                _storyApi.DeleteBeat(uuid, 0);
                        }

                        // Build candidate set for assigned_element validation
                        var problemGuids = GetCandidateGuids(StoryItemType.Problem);
                        var sceneGuids = GetCandidateGuids(StoryItemType.Scene);
                        var validAssignGuids = new System.Collections.Generic.HashSet<Guid>(
                            problemGuids.Concat(sceneGuids));

                        // Create new beats
                        for (int i = 0; i < beats.Count; i++)
                        {
                            var beat = beats[i];
                            _storyApi.CreateBeat(uuid, beat.Title, beat.Description);
                            if (beat.AssignedElement.HasValue)
                            {
                                if (validAssignGuids.Contains(beat.AssignedElement.Value))
                                    _storyApi.AssignElementToBeat(uuid, i, beat.AssignedElement.Value);
                                else
                                    result.StatusMessages.Add(
                                        $"Beat {i} assigned_element {beat.AssignedElement} not in candidate set; left unassigned");
                            }
                        }
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
                    if (Guid.TryParse(ae.GetString(), out var g))
                        assigned = g;
                }
                beats.Add(new BeatInfo(title, desc, assigned));
            }
            return beats;
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
        /// Fetches examples for each name declared in the workflow's ExampleLists and adds them to kernel args.
        /// </summary>
        internal void EnrichWithExamples(KernelArguments kernelArgs)
        {
            foreach (var propertyName in workflowModel.GetIO().ExampleLists)
            {
                var result = _storyApi.GetExamples(propertyName);
                if (result.IsSuccess && result.Payload != null && result.Payload.Any())
                {
                    var formatted = string.Join(", ", result.Payload);
                    kernelArgs[$"{propertyName}_examples"] = formatted;
                    _logger?.LogInformation($"Injected examples for {propertyName}: {result.Payload.Count()} items");
                }
                else
                {
                    _logger?.LogWarning($"No examples found for property: {propertyName}");
                }
            }
        }

        /// <summary>
        /// Enriches kernel arguments with story context.
        /// </summary>
        internal void EnrichWithStoryContext(KernelArguments kernelArgs, Dictionary<string, StoryElement> gatheredElements, WorkflowIO workflowIO)
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

                kernelArgs["StoryContext"] = !string.IsNullOrWhiteSpace(context) ? context : string.Empty;

                if (!string.IsNullOrWhiteSpace(context))
                    _logger?.LogInformation($"Enriched with story context ({context.Length} chars) for {workflowModel.Label} workflow");
                else
                    _logger?.LogInformation($"No story context generated for {workflowModel.Label} workflow");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"Failed to enrich story context: {ex.Message}");
                kernelArgs["StoryContext"] = string.Empty;
            }
        }

        /// <summary>
        /// Applies user settings to the prompt as instructions.
        /// </summary>
        internal void ApplySettings(KernelArguments kernelArgs)
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
                ContentPreservationLevel.Flexible => "Feel free to rewrite and improve the content.",
                _ => ""
            });

            if (!string.IsNullOrWhiteSpace(_settings.GenrePreferences))
                instructions.Add($"The user prefers these genres: {_settings.GenrePreferences}");

            if (!string.IsNullOrWhiteSpace(_settings.StoryFormLikes))
                instructions.Add($"The user likes these story forms: {_settings.StoryFormLikes}");
            if (!string.IsNullOrWhiteSpace(_settings.StoryFormDislikes))
                instructions.Add($"Avoid suggesting these story forms: {_settings.StoryFormDislikes}");

            var result = string.Join(" ", instructions.Where(s => !string.IsNullOrEmpty(s)));
            kernelArgs["UserSettings"] = result;

            if (!string.IsNullOrEmpty(result))
                _logger?.LogInformation("Applied settings: {Settings}", result);
        }

        /// <summary>
        /// Posts the workflow request to the proxy's /v1/workflow endpoint.
        /// Returns the SSE response text, the X-Template-Hash header value (null on fallback path),
        /// and the cost reported by the proxy's collab_cost event (null when absent).
        /// </summary>
        private async Task<(string Content, string? TemplateHash, ProxyCostInfo? Cost)> PostToProxyAsync(KernelArguments kernelArgs)
        {
            var proxyBaseUrl = Environment.GetEnvironmentVariable("COLLAB_PROXY_URL")
                ?? KernelFactory.DefaultProxyBaseUrl;

            var args = new Dictionary<string, string>();
            foreach (var kvp in kernelArgs)
                args[kvp.Key] = kvp.Value is string s ? s : kvp.Value?.ToString() ?? string.Empty;

            var payload = JsonSerializer.Serialize(new { workflowId = workflowModel.Label, args });

            var response = await SendWithReactivationRetryAsync(
                () => SendWorkflowRequestAsync(proxyBaseUrl, payload), ReactivateAsync);

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

            var (content, cost) = await ReadSseStreamAsync(response);
            return (content, templateHash, cost);
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

        // Re-activation trigger for the 401-refresh-once-and-retry policy (issue #90 step 8 item
        // 7). No-op if IoC has no activation service configured (PromptTestRunner, tests) --
        // matches KernelFactory.GetActivationJwt's InvalidOperationException guard for the same host.
        private static Task ReactivateAsync()
        {
            try
            {
                var service = Ioc.Default.GetService<StoryCADLib.Services.Store.IStoreActivationService>();
                return service?.ReactivateAsync() ?? Task.CompletedTask;
            }
            catch (InvalidOperationException)
            {
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// 401-refresh-once-and-retry (issue #90 step 8 item 7 / design section 11's expired-token
        /// row): a workflow call that comes back 401 refreshes the activation once through
        /// re-activation and retries with the refreshed credential before failing. Exactly one
        /// retry -- a second 401 is returned as-is so the caller's handling surfaces it rather than
        /// looping. internal static and testable without a live HTTP transport or activation flow:
        /// sendRequest and reactivate are injected, matching CreateWorkflowRequest/
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

        internal static async Task<(string Content, ProxyCostInfo? Cost)> ReadSseStreamAsync(HttpResponseMessage response)
        {
            var sb = new System.Text.StringBuilder();
            ProxyCostInfo? cost = null;
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (!line.StartsWith("data: ")) continue;
                var data = line.Substring(6).Trim();
                if (data == "[DONE]") break;
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
                        try
                        {
                            var workflow = collabCost.GetProperty("workflow").GetString();
                            var model = collabCost.GetProperty("model").GetString();
                            if (workflow is not null && model is not null)
                            {
                                cost = new ProxyCostInfo(
                                    workflow,
                                    model,
                                    collabCost.GetProperty("input_tokens").GetInt32(),
                                    collabCost.GetProperty("output_tokens").GetInt32(),
                                    collabCost.GetProperty("cost_microdollars").GetInt64());
                            }
                        }
                        catch (Exception) { /* malformed collab_cost — skip, Cost stays null */ }
                    }
                }
                catch (JsonException) { /* malformed chunk — skip */ }
            }
            return (sb.ToString(), cost);
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
