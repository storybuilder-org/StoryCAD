# New ViewModel Template

## Purpose
Copy-paste template for creating new ViewModels in StoryCAD following all architectural patterns.

## When to Use
When adding a new story element ViewModel (Character, Plot, Scene, etc.) or any ViewModel that needs data binding.

---

## Template

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Services;
using StoryCADLib.Models;

namespace StoryCADLib.ViewModels;

/// <summary>
/// ViewModel for [ElementName] story element
/// </summary>
[ObservableObject]
public partial class [ElementName]ViewModel : ISaveable
{
    #region Fields and Services

    private readonly [ElementName]Model _model;
    private readonly OutlineService _outlineService;
    private readonly LogService _logger;
    private readonly AppState _appState;

    #endregion

    #region Constructor

    public [ElementName]ViewModel()
    {
        // Get services from IoC container
        _outlineService = Ioc.Default.GetService<OutlineService>();
        _logger = Ioc.Default.GetService<LogService>();
        _appState = Ioc.Default.GetService<AppState>();

        _logger.Log(LogLevel.Info, $"{GetType().Name} constructor completed");
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Initialize ViewModel with model data
    /// </summary>
    public void LoadModel([ElementName]Model model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));

        // Load properties from model
        Name = _model.Name;
        // Add other properties...

        _logger.Log(LogLevel.Trace, $"Loaded {_model.Name} into ViewModel");
    }

    #endregion

    #region Observable Properties

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _description;

    // Add more properties as needed...

    #endregion

    #region Property Change Handlers

    /// <summary>
    /// Called when Name property changes
    /// </summary>
    partial void OnNameChanged(string oldValue, string newValue)
    {
        if (!string.IsNullOrEmpty(oldValue) && !string.IsNullOrEmpty(newValue))
        {
            // Notify name change for TreeView synchronization
            var msg = new NameChangeMessage(oldValue, newValue);
            Messenger.Send(new NameChangedMessage(msg));

            // Mark document as changed
            Messenger.Send(new IsChangedMessage(true));
        }
    }

    #endregion

    #region ISaveable Implementation

    /// <summary>
    /// Flush ViewModel data to Model
    /// </summary>
    public void SaveModel()
    {
        if (_model == null)
        {
            _logger.Log(LogLevel.Warn, "SaveModel called but _model is null");
            return;
        }

        // Transfer all properties to model
        _model.Name = Name;
        _model.Description = Description;
        // Add other properties...

        _logger.Log(LogLevel.Trace, $"Saved ViewModel to {_model.Name}");
    }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task Save()
    {
        try
        {
            SaveModel();
            await _outlineService.SaveStoryAsync(_appState.CurrentDocument);

            Messenger.Send(new StatusChangedMessage(
                new StatusMessage("[ElementName] saved successfully", LogLevel.Info, true)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save [ElementName]");
            Messenger.Send(new StatusChangedMessage(
                new StatusMessage("Failed to save [ElementName]", LogLevel.Error, false)));
        }
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private void Delete()
    {
        // Delete logic here
        Messenger.Send(new StatusChangedMessage(
            new StatusMessage("[ElementName] deleted", LogLevel.Info, true)));
    }

    private bool CanDelete()
    {
        return _model != null && !string.IsNullOrEmpty(_model.Name);
    }

    #endregion
}
```

---

## Customization Checklist

After copying the template, customize:

### 1. Replace Placeholders
- [ ] Replace `[ElementName]` with your element name (e.g., `Character`, `Plot`, `Scene`)
- [ ] Replace `[ElementName]Model` with actual model class
- [ ] Update namespace if needed

### 2. Add Properties
- [ ] Add `[ObservableProperty]` fields for all bindable properties
- [ ] Implement property change handlers if needed (for validation, notifications, etc.)

### 3. Update SaveModel()
- [ ] Flush ALL properties from ViewModel to Model
- [ ] Ensure no properties are forgotten (causes data loss!)

### 4. Add Commands
- [ ] Add element-specific commands (Save, Delete, Add, etc.)
- [ ] Implement CanExecute for commands that need validation
- [ ] Use `[NotifyCanExecuteChangedFor(nameof(CommandName))]` if needed

### 5. Register in BootStrapper.cs
```csharp
services.AddTransient<[ElementName]ViewModel>();
```

### 6. Create/Update Page
Update the corresponding page to register ISaveable:
```csharp
protected override void OnNavigatedTo(NavigationEventArgs e)
{
    base.OnNavigatedTo(e);
    var appState = Ioc.Default.GetService<AppState>();
    appState.CurrentSaveable = ViewModel; // Register ViewModel
}

protected override void OnNavigatedFrom(NavigationEventArgs e)
{
    var appState = Ioc.Default.GetService<AppState>();
    appState.CurrentSaveable = null; // Unregister
    base.OnNavigatedFrom(e);
}
```

---

## Example: CharacterViewModel

```csharp
[ObservableObject]
public partial class CharacterViewModel : ISaveable
{
    private readonly CharacterModel _model;
    private readonly OutlineService _outlineService;
    private readonly LogService _logger;
    private readonly AppState _appState;

    public CharacterViewModel()
    {
        _outlineService = Ioc.Default.GetService<OutlineService>();
        _logger = Ioc.Default.GetService<LogService>();
        _appState = Ioc.Default.GetService<AppState>();
    }

    public void LoadModel(CharacterModel model)
    {
        _model = model;
        Name = _model.Name;
        Role = _model.Role;
        Age = _model.Age;
        Description = _model.Description;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name;

    [ObservableProperty]
    private string _role;

    [ObservableProperty]
    private int _age;

    [ObservableProperty]
    private string _description;

    partial void OnNameChanged(string oldValue, string newValue)
    {
        if (!string.IsNullOrEmpty(oldValue) && !string.IsNullOrEmpty(newValue))
        {
            Messenger.Send(new NameChangedMessage(new NameChangeMessage(oldValue, newValue)));
            Messenger.Send(new IsChangedMessage(true));
        }

        SaveCommand.NotifyCanExecuteChanged();
    }

    public void SaveModel()
    {
        _model.Name = Name;
        _model.Role = Role;
        _model.Age = Age;
        _model.Description = Description;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        SaveModel();
        await _outlineService.SaveStoryAsync(_appState.CurrentDocument);
        Messenger.Send(new StatusChangedMessage(
            new StatusMessage("Character saved", LogLevel.Info, true)));
    }

    private bool CanSave() => !string.IsNullOrEmpty(Name);
}
```

---

## Common Mistakes to Avoid

### ❌ Forgetting to implement ISaveable
```csharp
public partial class MyViewModel // Missing ISaveable!
{
    // SaveModel() won't be called
}
```

### ❌ Not flushing all properties in SaveModel()
```csharp
public void SaveModel()
{
    _model.Name = Name;
    // Forgot Description, Role, etc. - DATA LOSS!
}
```

### ❌ Not registering in page navigation
```csharp
protected override void OnNavigatedTo(NavigationEventArgs e)
{
    base.OnNavigatedTo(e);
    // Forgot: appState.CurrentSaveable = ViewModel;
}
```

### ❌ Manually implementing INotifyPropertyChanged
```csharp
private string _name;
public string Name
{
    get => _name;
    set
    {
        _name = value;
        OnPropertyChanged(); // Don't do this - use [ObservableProperty]!
    }
}
```

---

## See Also

- **MVVM Toolkit Guide**: `.claude/docs/dependencies/mvvm-toolkit-guide.md`
- **Patterns Quick Reference**: `.claude/docs/architecture/patterns-quick-ref.md`
- **ISaveable Pattern**: `/devdocs/StoryCAD_architecture_notes.md#4-isaveable-pattern`
- **Existing ViewModels**: `/StoryCADLib/ViewModels/` (for real examples)
