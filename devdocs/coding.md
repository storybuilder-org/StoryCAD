# Coding Standards

## Naming Conventions

### General Rules
- Use descriptive names that express intent
- Avoid abbreviations except well-known ones (URL, API, etc.)
- No underscores except for backing fields
- Acronyms: Two letters = uppercase (IO), three+ letters = Pascal/camelCase (Xml, Json)

### Specific Conventions
- **Classes/Interfaces**: PascalCase
  - Interfaces start with 'I': `INavigationService`
  - Classes named for what they are: `StoryModel`, `CharacterViewModel`
- **Methods**: PascalCase
  - Verbs or verb phrases: `SaveDocument()`, `CanExecute()`
- **Properties**: PascalCase
  - Nouns or noun phrases: `CurrentView`, `IsChanged`
- **Fields**: 
  - Private: _camelCase with underscore: `private string _name;`
  - Public: PascalCase (but prefer properties)
- **Parameters/Variables**: camelCase
  - `string fileName`, `int itemCount`
- **Constants**: UPPER_CASE with underscores
  - `public const int MAX_RETRY_COUNT = 3;`

### File Names
- Match the primary class name: `StoryModel.cs` contains `class StoryModel`
- One class per file (except small related types)
- Test files: `[ClassName]Tests.cs`

## Commenting Standards

### When to Comment
- **DO** comment complex algorithms or business logic
- **DO** comment "why" not "what" 
- **DO** use XML doc comments for public APIs
- **DON'T** comment obvious code
- **DON'T** leave commented-out code - use version control

### Comment Formats
```csharp
/// <summary>
/// Saves the story model to the specified file path.
/// </summary>
/// <param name="path">The output file path</param>
/// <returns>True if successful</returns>
public async Task<bool> SaveStory(string path)

// Complex business rule explanation
if (node.Parent?.Type == StoryItemType.TrashCan)
{
    // Can't restore children directly - only top-level items
    return false;
}

// TODO: Implement retry logic for cloud storage
// HACK: Temporary workaround for issue #123
// NOTE: This must happen before view initialization
```

## Code Layout

### File Organization
```csharp
using System;
using System.Collections.Generic;
// System usings first, alphabetically

using CommunityToolkit.Mvvm;
using StoryCAD.Models;
// Third-party and local usings next

namespace StoryCAD.Services
{
    public class MyClass : ObservableObject
    {
        #region Fields
        private string _name;
        private readonly ILogger _logger;
        #endregion

        #region Properties
        public string Name 
        { 
            get => _name;
            set => SetProperty(ref _name, value);
        }
        #endregion

        #region Constructors
        public MyClass(ILogger logger)
        {
            _logger = logger;
        }
        #endregion

        #region Public Methods
        public void DoSomething()
        {
            // Implementation
        }
        #endregion

        #region Private Methods
        private void Helper()
        {
            // Implementation
        }
        #endregion
    }
}
```

### Indentation and Spacing
- Use 4 spaces (not tabs)
- Opening braces on new line (Allman style)
- Single blank line between methods
- No trailing whitespace
- Max line length: 120 characters (break at operators/commas)

### Method Layout
```csharp
public async Task<bool> ProcessItem(Item item, CancellationToken token)
{
    // 1. Guard clauses first
    if (item == null)
        throw new ArgumentNullException(nameof(item));

    // 2. Variable declarations
    var result = false;
    var retryCount = 0;

    // 3. Main logic
    try
    {
        while (retryCount < MAX_RETRIES)
        {
            result = await ProcessCore(item, token);
            if (result) break;
            retryCount++;
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to process item {ItemId}", item.Id);
        throw;
    }

    // 4. Return
    return result;
}
```

## SOLID Principles

### Single Responsibility Principle (SRP)
- Each class should have one reason to change
- Services do one thing well
- Example: `FileIO` handles file operations, not validation

### Open/Closed Principle (OCP)
- Open for extension, closed for modification
- Use interfaces and abstract classes
- Example: `IStoryElement` allows new element types without changing existing code

### Liskov Substitution Principle (LSP)
- Derived classes must be substitutable for base classes
- Don't throw exceptions in overrides that base doesn't throw
- Maintain base class contracts

### Interface Segregation Principle (ISP)
- Many specific interfaces better than one general interface
- Don't force classes to implement methods they don't use
- Example: `INavigable`, `ISaveable`, `IValidatable` vs one large `IStoryElement`

### Dependency Inversion Principle (DIP)
- Depend on abstractions, not concretions
- Constructor injection for dependencies
- Example:
```csharp
// Good - depends on interface
public class StoryService
{
    private readonly IFileService _fileService;
    
    public StoryService(IFileService fileService)
    {
        _fileService = fileService;
    }
}

// Bad - depends on concrete class
public class StoryService
{
    private readonly FileService _fileService = new FileService();
}
```

## Additional Guidelines

### Async/Await
- Async methods return `Task` or `Task<T>`
- Suffix async methods with `Async`: `SaveAsync()`
- Use `ConfigureAwait(false)` for non-UI code
- Don't use `async void` except for event handlers

### Exception Handling
- Catch specific exceptions, not base `Exception`
- Log exceptions before re-throwing
- Use custom exceptions for business logic errors
- Always include inner exception when wrapping

### LINQ Usage
- Prefer method syntax for simple queries
- Use query syntax for complex joins
- Be mindful of performance with large collections
- Materialize queries early with `ToList()` or `ToArray()` when needed

### Properties vs Methods
- Properties: Fast, no side effects, return consistent results
- Methods: May be slow, have side effects, or throw exceptions
- When in doubt, use a method

Remember: Consistency is more important than personal preference. When modifying existing code, follow its conventions.