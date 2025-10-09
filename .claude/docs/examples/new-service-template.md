# New Service Template

## Purpose
Copy-paste template for creating new services in StoryCAD following the Stateless Services pattern.

## When to Use
When adding a new service to handle business logic, file operations, or cross-cutting concerns.

---

## Template

```csharp
using StoryCADLib.Services;
using StoryCADLib.Models;

namespace StoryCADLib.Services;

/// <summary>
/// Service for [service purpose]
/// </summary>
public class [ServiceName]Service
{
    #region Dependencies (Constructor Injection)

    private readonly LogService _logger;
    private readonly AppState _appState;
    // Add other service dependencies as needed

    public [ServiceName]Service(LogService logger, AppState appState)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appState = appState ?? throw new ArgumentNullException(nameof(appState));

        _logger.Log(LogLevel.Info, $"{GetType().Name} initialized");
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// [Method description]
    /// </summary>
    /// <param name="model">Model to operate on (STATELESS - passed as parameter)</param>
    /// <returns>[Return description]</returns>
    public [ReturnType] [MethodName](StoryModel model, [parameters])
    {
        if (model == null) throw new ArgumentNullException(nameof(model));

        _logger.Log(LogLevel.Trace, $"[MethodName] called with {parameters}");

        try
        {
            // Service logic here
            // DO NOT store model in field - keep service stateless

            var result = /* perform operation */;

            _logger.Log(LogLevel.Info, $"[MethodName] completed successfully");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[MethodName] failed");
            throw;
        }
    }

    #endregion
}
```

---

## Service Template with Thread Safety

For services that modify StoryModel, use SerializationLock:

```csharp
using StoryCADLib.Services;
using StoryCADLib.Models;

namespace StoryCADLib.Services;

public class [ServiceName]Service
{
    private readonly LogService _logger;
    private readonly AutoSaveService _autoSave;
    private readonly BackupService _backup;

    public [ServiceName]Service(LogService logger, AutoSaveService autoSave, BackupService backup)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _autoSave = autoSave ?? throw new ArgumentNullException(nameof(autoSave));
        _backup = backup ?? throw new ArgumentNullException(nameof(backup));
    }

    /// <summary>
    /// Modify story data with thread safety
    /// </summary>
    public Guid AddElement(StoryModel model, Guid parentGuid, StoryItemType type, string name)
    {
        if (model == null) throw new ArgumentNullException(nameof(model));

        _logger.Log(LogLevel.Trace, $"Adding {type} element: {name}");

        // IMPORTANT: Wrap state-modifying operations in SerializationLock
        using (var lock = new SerializationLock(_autoSave, _backup, _logger))
        {
            var parent = model.StoryElements.StoryElementGuids[parentGuid];
            var newElement = new StoryElement
            {
                Uuid = Guid.NewGuid(),
                Name = name,
                Type = type,
                ParentGuid = parentGuid
            };

            model.StoryElements.Add(newElement);
            parent.Children.Add(newElement.Uuid);

            _logger.Log(LogLevel.Info, $"Added {type}: {name}");
            return newElement.Uuid;
        }
    }
}
```

---

## Customization Checklist

### 1. Replace Placeholders
- [ ] Replace `[ServiceName]` with your service name (e.g., `Export`, `Import`, `Validation`)
- [ ] Replace `[ReturnType]`, `[MethodName]`, `[parameters]` with actual values
- [ ] Update namespace if needed

### 2. Add Dependencies
- [ ] Add all required services to constructor
- [ ] Store dependencies in private readonly fields
- [ ] Use TRUE constructor injection (not `Ioc.Default.GetService`)

### 3. Implement Methods
- [ ] Keep service STATELESS (no model/document fields)
- [ ] Accept models as method parameters
- [ ] Use SerializationLock for state-modifying operations
- [ ] Add comprehensive logging
- [ ] Handle exceptions appropriately

### 4. Register in BootStrapper.cs
```csharp
services.AddSingleton<[ServiceName]Service>();
```

---

## Example: ValidationService

```csharp
using StoryCADLib.Services;
using StoryCADLib.Models;

namespace StoryCADLib.Services;

/// <summary>
/// Service for validating story structure and data
/// </summary>
public class ValidationService
{
    private readonly LogService _logger;

    public ValidationService(LogService logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validate story structure for integrity
    /// </summary>
    public ValidationResult ValidateStory(StoryModel model)
    {
        if (model == null) throw new ArgumentNullException(nameof(model));

        _logger.Log(LogLevel.Trace, "Validating story structure");

        var result = new ValidationResult { IsValid = true };

        // Check for orphaned elements
        foreach (var element in model.StoryElements)
        {
            if (element.ParentGuid != Guid.Empty)
            {
                if (!model.StoryElements.StoryElementGuids.ContainsKey(element.ParentGuid))
                {
                    result.IsValid = false;
                    result.Errors.Add($"Element {element.Name} has invalid parent GUID");
                }
            }
        }

        // Check for duplicate names
        var nameGroups = model.StoryElements.GroupBy(e => e.Name);
        foreach (var group in nameGroups.Where(g => g.Count() > 1))
        {
            result.Warnings.Add($"Duplicate element name: {group.Key}");
        }

        _logger.Log(LogLevel.Info,
            $"Validation complete: {(result.IsValid ? "Valid" : "Invalid")}, {result.Errors.Count} errors, {result.Warnings.Count} warnings");

        return result;
    }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
```

---

## Common Patterns

### Pattern 1: Query Service (Read-only)

```csharp
public class QueryService
{
    public List<StoryElement> GetElementsByType(StoryModel model, StoryItemType type)
    {
        return model.StoryElements.Where(e => e.Type == type).ToList();
    }

    public StoryElement FindElementByName(StoryModel model, string name)
    {
        return model.StoryElements.FirstOrDefault(e => e.Name == name);
    }
}
```

### Pattern 2: Command Service (State-modifying)

```csharp
public class CommandService
{
    private readonly LogService _logger;
    private readonly AutoSaveService _autoSave;
    private readonly BackupService _backup;

    public Guid AddElement(StoryModel model, ...)
    {
        using (var lock = new SerializationLock(_autoSave, _backup, _logger))
        {
            // Modify model
            return newGuid;
        }
    }

    public void DeleteElement(StoryModel model, Guid elementGuid)
    {
        using (var lock = new SerializationLock(_autoSave, _backup, _logger))
        {
            // Modify model
        }
    }
}
```

### Pattern 3: File Operation Service

```csharp
public class ExportService
{
    private readonly LogService _logger;
    private readonly AutoSaveService _autoSave;
    private readonly BackupService _backup;

    public async Task<string> ExportToMarkdown(StoryModel model, string outputPath)
    {
        using (var lock = new SerializationLock(_autoSave, _backup, _logger))
        {
            var markdown = GenerateMarkdown(model);
            await File.WriteAllTextAsync(outputPath, markdown);
            return outputPath;
        }
    }

    private string GenerateMarkdown(StoryModel model)
    {
        // Generate markdown from model
        return markdown;
    }
}
```

---

## Common Mistakes to Avoid

### ❌ Storing state in service
```csharp
public class MyService
{
    private StoryModel _currentModel; // WRONG! Services must be stateless

    public void DoSomething()
    {
        _currentModel.Elements.Add(...); // Using stored state
    }
}
```

### ❌ Not using SerializationLock for state changes
```csharp
public void AddElement(StoryModel model, ...)
{
    model.Elements.Add(newElement); // WRONG! No SerializationLock
}
```

### ❌ Using Ioc.Default in services
```csharp
public class MyService
{
    private LogService _logger;

    public MyService()
    {
        _logger = Ioc.Default.GetService<LogService>(); // WRONG! Use constructor injection
    }
}
```

### ❌ Not validating parameters
```csharp
public void DoSomething(StoryModel model)
{
    model.Elements.Add(...); // WRONG! Could be null
}
```

---

## See Also

- **Patterns Quick Reference**: `.claude/docs/architecture/patterns-quick-ref.md`
- **Stateless Services Pattern**: `/devdocs/StoryCAD_architecture_notes.md#7-stateless-services-pattern`
- **SerializationLock Pattern**: `/devdocs/StoryCAD_architecture_notes.md#3-thread-safety-pattern-serializationlock`
- **Existing Services**: `/StoryCADLib/Services/` (for real examples)
