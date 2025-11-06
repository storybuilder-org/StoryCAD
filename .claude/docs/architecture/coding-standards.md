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

## Code Quality Analyzers

StoryCAD uses Roslyn analyzers configured in `.editorconfig` to enforce code quality standards and detect potential issues.

### Configured Analyzer Rules

**Dead Code Detection**
- `CS0169` - Field is never used
- `CS0414` - Field is assigned but its value is never used
- `CS0168` - Variable is declared but never used
- `IDE0051` - Private member is unused
- `IDE0052` - Private member is unread
- `IDE0060` - Remove unused parameter

**Code Cleanliness**
- `CS0105` - Using directive appeared previously (duplicate usings)

**API Modernization**
- `CS0618` - Member is obsolete (use newer APIs)

All rules are set to `severity = warning` in `.editorconfig` to ensure they appear in build output and IDE error lists.

### Using Analyzers in Development

**Visual Studio / Rider**
- Warnings appear with green squiggles in the editor
- Quick fixes (light bulb icon) available for most issues
- View all warnings: Build → Error List → Warnings tab
- Fix all in scope: Right-click → Quick Actions → Fix all in document/project/solution

**Command Line Build**
```bash
msbuild StoryCAD.sln /t:Build /p:Configuration=Debug
# Analyzer warnings appear in build output
```

**Suppressing Warnings (Use Sparingly)**
```csharp
#pragma warning disable CS0618 // Obsolete API
var result = LegacyMethod();
#pragma warning restore CS0618
```
Only suppress warnings when:
- False positive confirmed
- Temporary workaround documented with TODO
- Platform-specific code requires it

### ReSharper Code Cleanup

For comprehensive code cleanup beyond basic analyzers, use ReSharper/Rider:

**Quick Cleanup** (current file):
- Visual Studio: `Ctrl+E, C`
- Rider: `Ctrl+Alt+Enter` → "Code Cleanup"

**Bulk Cleanup** (entire solution):
- Right-click solution → "Code Cleanup"
- Profile: "Built-in: Full Cleanup"
- Review git diff before committing

**Command Line** (CI/CD or scripting):
```bash
jb inspectcode StoryCAD.sln --output=inspection.xml
jb cleanupcode StoryCAD.sln --profile="Built-in: Full Cleanup"
```

For detailed ReSharper workflows, see `/devdocs/issue_1134_resharper_guide.md`.

### Analyzer Best Practices

1. **Fix warnings immediately** - Don't let them accumulate
2. **Run builds locally** - Catch issues before committing
3. **Review analyzer suggestions** - They often reveal design issues
4. **Update .editorconfig** - Add new rules as patterns emerge
5. **Document suppressions** - Explain why warning is suppressed

Remember: Consistency is more important than personal preference. When modifying existing code, follow its conventions.