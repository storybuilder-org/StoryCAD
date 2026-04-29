# Microsoft Semantic Kernel Guide for StoryCAD

## Package Information

- **Package**: Microsoft.SemanticKernel
- **Version**: 1.41.0
- **Additional Packages**:
  - Microsoft.SemanticKernel.Abstractions (1.41.0)
  - Microsoft.SemanticKernel.Planners.OpenAI (1.16.0-preview)
- **Purpose**: AI/LLM integration for StoryCAD Collaborator feature
- **Documentation**: Use Context7 MCP for latest docs: `@context7 Microsoft.SemanticKernel` or visit [https://learn.microsoft.com/en-us/semantic-kernel/](https://learn.microsoft.com/en-us/semantic-kernel/)

---

## StoryCAD's Use of Semantic Kernel

### Architecture

Semantic Kernel is used **exclusively in the CollaboratorLib plugin**, not in the main StoryCAD application. This separation keeps AI features modular and optional.

```
StoryCAD (Main App)
   ↓
CollaboratorService (plugin loader/proxy)
   ↓
CollaboratorLib.dll (separate assembly)
   ↓
Semantic Kernel (AI/LLM integration)
```

**Key Components**:
1. **CollaboratorService** - Plugin loader and proxy to CollaboratorLib
2. **ICollaborator Interface** - Contract between StoryCAD and plugin
3. **CollaboratorLib** - Separate assembly containing Semantic Kernel code
4. **Workflows** - AI-assisted writing workflows using Semantic Kernel

---

## CollaboratorService Integration

### Service Responsibilities

**CollaboratorService** acts as a proxy:
- Loads CollaboratorLib.dll dynamically
- Provides interface between StoryCAD and Collaborator
- Manages Collaborator window lifecycle
- Handles plugin discovery (dev, env var, or MSIX package)

**Example Usage**:
```csharp
public class SomeViewModel
{
    private readonly CollaboratorService _collaborator;

    public SomeViewModel()
    {
        _collaborator = Ioc.Default.GetService<CollaboratorService>();
    }

    public async Task OpenCollaborator()
    {
        if (await _collaborator.CollaboratorEnabled())
        {
            _collaborator.LoadWizardViewModel();
            // Collaborator window opens
        }
    }
}
```

### ICollaborator Interface

**Contract between StoryCAD and plugin** (defined in `StoryCADLib/Services/Collaborator/Contracts/ICollaborator.cs`, simplified by issue #1088):

```csharp
public interface ICollaborator
{
    Task<Window> OpenAsync(IStoryCADAPI api, StoryModel model,
                           Window hostWindow, Frame hostFrame,
                           string filePath, ILogService? logger = null);

    CollaboratorResult Close();

    void SetSettings(CollaboratorSettings settings);
    CollaboratorSettings GetSettings();

    void Dispose();
}
```

`OpenAsync` is the single handoff point — the host passes the current `StoryModel`, the API, host-supplied `Window`/`Frame` for the plugin's UI, the file path (for save-via-API), and an optional logger; the call returns the `Window` hosting Collaborator's UI. `Close` retrieves a session summary when the host dismisses the plugin. `SetSettings`/`GetSettings` can run before or after `OpenAsync`.

**Implementation in CollaboratorLib** (`CollaboratorLib/Collaborator.cs`):

```csharp
public class Collaborator : ICollaborator
{
    private bool _kernelInitialized;
    private readonly object _kernelLock = new();
    private Kernel _kernel; // built lazily

    public Collaborator()
    {
        // Empty by design: Semantic Kernel build takes 7+ minutes.
        // Init is deferred to first use to keep construction cheap
        // (matters for tests and startup time).
    }

    private void EnsureKernelInitialized()
    {
        if (_kernelInitialized) return;
        lock (_kernelLock)
        {
            if (_kernelInitialized) return;
            // Build Kernel here (see "Building the Kernel" below).
            _kernelInitialized = true;
        }
    }

    public async Task<Window> OpenAsync(IStoryCADAPI api, StoryModel model,
                                        Window hostWindow, Frame hostFrame,
                                        string filePath, ILogService? logger = null)
    {
        EnsureKernelInitialized();
        // Use api/model/hostWindow/hostFrame to set up the Collaborator session.
        // Return the hosting window.
    }
}
```

---

## Typical Semantic Kernel Patterns (in CollaboratorLib)

### Pattern 1: Basic Kernel Configuration

```csharp
using Microsoft.SemanticKernel;

public class Collaborator
{
    private readonly Kernel _kernel;

    public Collaborator()
    {
        var builder = Kernel.CreateBuilder();

        // Option 1: OpenAI
        builder.AddOpenAIChatCompletion(
            modelId: "gpt-4",
            apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY")
        );

        // Option 2: Azure OpenAI
        builder.AddAzureOpenAIChatCompletion(
            deploymentName: "gpt-4",
            endpoint: "https://your-resource.openai.azure.com/",
            apiKey: Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY")
        );

        _kernel = builder.Build();
    }
}
```

### Pattern 2: Invoking Prompts

```csharp
public async Task<string> GenerateCharacterBackstory(string characterName, string role)
{
    var prompt = $"""
        Generate a detailed backstory for a character named {characterName}
        who has the role of {role} in a story. Include:
        - Their motivations
        - Key life events
        - Personality traits
        - Conflicts or challenges
        """;

    var result = await _kernel.InvokePromptAsync(prompt);
    return result.ToString();
}
```

### Pattern 3: Using Plugins/Functions

```csharp
using Microsoft.SemanticKernel.Plugins.Core;

public void ConfigurePlugins()
{
    // Add built-in plugins
    _kernel.Plugins.AddFromType<TimePlugin>();
    _kernel.Plugins.AddFromType<TextPlugin>();

    // Add custom plugin
    _kernel.Plugins.AddFromObject(new StoryCADPlugin());
}

public class StoryCADPlugin
{
    [KernelFunction]
    [Description("Gets character details from StoryCAD")]
    public string GetCharacter(string characterName)
    {
        // Access StoryCAD data
        return $"Character: {characterName}";
    }
}
```

### Pattern 4: Streaming Responses

```csharp
public async Task StreamResponse(string prompt, Action<string> onChunk)
{
    var settings = new OpenAIPromptExecutionSettings
    {
        MaxTokens = 1000,
        Temperature = 0.7
    };

    await foreach (var chunk in _kernel.InvokePromptStreamingAsync(prompt, settings))
    {
        onChunk(chunk.ToString());
    }
}
```

### Pattern 5: Chat History

```csharp
using Microsoft.SemanticKernel.ChatCompletion;

private readonly ChatHistory _chatHistory = new();

public async Task<string> SendChatMessage(string userMessage)
{
    // Add user message
    _chatHistory.AddUserMessage(userMessage);

    // Get AI response
    var chatService = _kernel.GetRequiredService<IChatCompletionService>();
    var response = await chatService.GetChatMessageContentAsync(_chatHistory);

    // Add assistant message to history
    _chatHistory.AddAssistantMessage(response.Content);

    return response.Content;
}
```

---

## Data passed from StoryCAD to Collaborator

There is no `CollaboratorArgs` bag class. Everything the plugin needs flows through `ICollaborator.OpenAsync(...)` parameters:

| Parameter | Role |
|---|---|
| `IStoryCADAPI api` | API surface the plugin uses to read/write outline data — the only data path; no direct file access |
| `StoryModel model` | The story the Collaborator session operates on |
| `Window hostWindow` | Host-created window for Collaborator's UI |
| `Frame hostFrame` | Host-created frame (with region) for the plugin's internal navigation |
| `string filePath` | Path to the story file, used by API save operations |
| `ILogService? logger` | StoryCAD's logger for high-level audit events; may be null |

The single 6-parameter call replaced an earlier args-bag pattern under the issue #1088 interface simplification. Caller-side example:

```csharp
var hostingWindow = await collaborator.OpenAsync(
    api: _storyCadApi,
    model: AppState.CurrentDocument.Model,
    hostWindow: collaboratorWindow,
    hostFrame: collaboratorFrame,
    filePath: AppState.CurrentDocument.FilePath,
    logger: _logService);
```

---

## Environment Configuration

### API Keys

Semantic Kernel requires API keys for LLM providers. Store these securely:

**Development** (via Doppler or .env):
```bash
# .env file (never commit!)
OPENAI_API_KEY=sk-...
AZURE_OPENAI_ENDPOINT=https://...
AZURE_OPENAI_KEY=...
```

**Loading in code**:
```csharp
using dotenv.net;

public class Collaborator
{
    public Collaborator()
    {
        // Load .env if exists
        DotEnv.Load(options: new DotEnvOptions(probeForEnv: true));

        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("OPENAI_API_KEY not configured");
        }

        // Use in Kernel builder
        var builder = Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion("gpt-4", apiKey);
        _kernel = builder.Build();
    }
}
```

---

## Common Patterns for StoryCAD Workflows

### Workflow 1: Character Development

```csharp
public async Task<CharacterWorkflowResult> DevelopCharacter(CharacterModel character)
{
    var prompt = $"""
        You are a creative writing assistant helping develop a character.

        Current character information:
        - Name: {character.Name}
        - Role: {character.Role}
        - Age: {character.Age}

        Generate:
        1. A detailed backstory
        2. Three key personality traits
        3. Primary motivation
        4. Main conflict
        """;

    var result = await _kernel.InvokePromptAsync<string>(prompt);

    return ParseCharacterResult(result);
}
```

### Workflow 2: Plot Suggestions

```csharp
public async Task<List<PlotIdea>> GeneratePlotTwists(PlotModel plot)
{
    var prompt = $"""
        Given this plot setup:
        {plot.Description}

        Generate 5 unexpected plot twists that would:
        - Surprise the reader
        - Stay consistent with the existing setup
        - Raise the stakes
        """;

    var result = await _kernel.InvokePromptAsync<string>(prompt);

    return ParsePlotIdeas(result);
}
```

### Workflow 3: Scene Expansion

```csharp
public async Task<string> ExpandScene(SceneModel scene)
{
    var prompt = $"""
        Expand this scene outline into a detailed description:

        Scene: {scene.Name}
        Setting: {scene.Setting}
        Characters: {string.Join(", ", scene.Characters)}
        Goal: {scene.Goal}

        Write a 2-3 paragraph detailed scene description focusing on:
        - Vivid sensory details
        - Character interactions
        - Building tension toward the goal
        """;

    return await _kernel.InvokePromptAsync<string>(prompt);
}
```

---

## Error Handling

### Pattern: Safe AI Invocation

```csharp
public async Task<OperationResult<string>> SafeInvokePrompt(string prompt)
{
    try
    {
        var result = await _kernel.InvokePromptAsync<string>(prompt);

        return new OperationResult<string>
        {
            IsSuccess = true,
            Payload = result
        };
    }
    catch (HttpOperationException ex)
    {
        _logger.LogError(ex, "AI service error");
        return new OperationResult<string>
        {
            IsSuccess = false,
            ErrorMessage = "AI service temporarily unavailable"
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error invoking AI");
        return new OperationResult<string>
        {
            IsSuccess = false,
            ErrorMessage = ex.Message
        };
    }
}
```

---

## Testing Semantic Kernel Code

### Collaborator's actual pattern: real implementations, no mocks

`CollaboratorTests` does not mock `Kernel`. Per `CollaboratorTests/TestDataSetup.cs` doc-comment: *"Follows StoryCADTests pattern - uses real implementations, no mocks."* The shared assembly setup runs the real `BootStrapper.Initialise()` IoC, exposes a real `StoryCADApi` and a `Dictionary<string, StoryModel>` loaded from on-disk `TestInputs/` fixtures, and instantiates the production `Collaborator` class directly. `CollaboratorHost.ShutdownAsync()` runs in `[TestCleanup]` between tests.

```csharp
[AssemblyInitialize]
public static async Task AssemblyInitAsync(TestContext context)
{
    BootStrapper.Initialise();                 // real IoC
    InputDir   = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestInputs");
    ResultsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestResults");
    Directory.CreateDirectory(ResultsDir);
    // ... real StoryCADApi resolved via Ioc.Default; SharedStoryModels loaded from .stbx
}
```

Reasoning: mocks would mask integration issues. The test composition root is the right place to use service locators (consistent with the StoryCAD test-DI rule).

### If you do need to substitute the Kernel

`Kernel` itself is constructed via `Kernel.CreateBuilder()` (see "Building the Kernel" above) — substitution at the test level is achievable via standard DI swap (a fake `IChatCompletionService` registered before `builder.Build()`, or a captured-output fake provider). No project-level mocking framework is required; Collaborator does not ship one. If you introduce a substitute, document it as a deliberate departure from the no-mocks pattern.

---

## Common Issues and Solutions

### Issue: "API key not found"

**Problem**: Semantic Kernel can't find API credentials

**Solution**:
```csharp
// Check environment variables
var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (string.IsNullOrEmpty(apiKey))
{
    _logger.LogError("OPENAI_API_KEY not configured");
    throw new InvalidOperationException(
        "Please set OPENAI_API_KEY environment variable or configure Doppler");
}
```

### Issue: Rate limiting errors

**Problem**: Too many requests to AI service

**Solution**: Implement retry with exponential backoff
```csharp
public async Task<string> InvokeWithRetry(string prompt, int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            return await _kernel.InvokePromptAsync<string>(prompt);
        }
        catch (HttpOperationException ex) when (ex.StatusCode == 429)
        {
            if (i == maxRetries - 1) throw;

            var delay = TimeSpan.FromSeconds(Math.Pow(2, i));
            await Task.Delay(delay);
        }
    }

    throw new InvalidOperationException("Max retries exceeded");
}
```

### Issue: Responses too long/expensive

**Problem**: AI generating more than needed

**Solution**: Set token limits
```csharp
var settings = new OpenAIPromptExecutionSettings
{
    MaxTokens = 500,      // Limit response length
    Temperature = 0.7,     // Control randomness
    TopP = 0.9
};

var result = await _kernel.InvokePromptAsync(prompt, settings);
```

---

## Best Practices for StoryCAD

### ✅ Do:
1. **Keep Semantic Kernel in CollaboratorLib** (plugin isolation)
2. **Use OperationResult pattern** for AI calls
3. **Implement error handling** for network/API issues
4. **Store API keys securely** (Doppler, env vars, never hardcode)
5. **Set token limits** to control costs
6. **Test with mocks** to avoid API calls in unit tests
7. **Log AI interactions** for debugging (without sensitive data)

### ❌ Don't:
1. **Don't use Semantic Kernel in main StoryCAD** (keep in plugin)
2. **Don't commit API keys** to version control
3. **Don't skip error handling** (AI services can fail)
4. **Don't send sensitive user data** without consent
5. **Don't assume responses are perfect** (validate AI output)

---

## Plugin Loading (CollaboratorService)

### How StoryCAD Finds CollaboratorLib

1. **STORYCAD_PLUGIN_DIR environment variable** (highest priority)
2. **Sibling repository** (dev builds only)
3. **MSIX AppExtension** (production)

**Enabling Collaborator**:
```bash
# Set environment variable to plugin directory
export STORYCAD_PLUGIN_DIR=/path/to/CollaboratorLib/bin/Debug/net9.0-windows

# Or disable collaborator for debugging
export COLLAB_DEBUG=0
```

---

## See Also

- **Context7 for API docs**: `@context7 Microsoft.SemanticKernel`
- **Official Docs**: [https://learn.microsoft.com/en-us/semantic-kernel/](https://learn.microsoft.com/en-us/semantic-kernel/)
- **CollaboratorService**: `StoryCADLib/Services/Collaborator/CollaboratorService.cs`
- **ICollaborator Interface**: `StoryCADLib/Services/Collaborator/Contracts/ICollaborator.cs`
- **Environment Variables**: `.claude/docs/dependencies/dotenv-doppler-guide.md` (if created)
- **Architecture Notes**: `/devdocs/StoryCAD_architecture_notes.md`

---

## Version Notes

**Current Version**: 1.41.0

**Major Changes from 1.x**:
- Kernel built via `Kernel.CreateBuilder()` instead of `new KernelBuilder()`
- Services added via `builder.AddOpenAIChatCompletion()` instead of `WithOpenAIChatCompletionService()`
- Plugins use `[KernelFunction]` attribute instead of `[SKFunction]`

For version-specific API details, always use Context7: `@context7 Microsoft.SemanticKernel version:1.41.0`
