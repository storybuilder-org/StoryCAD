using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoryCADLib.Services.API;
using StoryCADLib.Services.Dialogs;
using StoryCADLib.Services.Dialogs.Tools;
using StoryCADLib.Services.Outline;

namespace StoryCADLib.ViewModels.Tools;

/// <summary>
/// ViewModel for the Copy Elements dialog (Issue #482).
/// Allows copying story elements (Character, Setting, StoryWorld, Problem, Notes, Web)
/// from the current outline to another outline file.
/// </summary>
[Microsoft.UI.Xaml.Data.Bindable]
public class CopyElementsDialogVM : ObservableRecipient
{
    private readonly AppState _appState;
    private readonly OutlineService _outlineService;
    private readonly StoryCADApi _api;
    private readonly Windowing _windowing;
    private readonly ILogService _logger;

    private StoryModel _targetModel;
    private string _targetFilePath;
    private StoryItemType _selectedFilterType;
    private StoryElement _selectedSourceElement;
    private StoryElement _selectedTargetElement;
    private string _statusMessage;
    private int _copiedCount;

    /// <summary>
    /// Tracks elements copied this session for safe removal.
    /// Only elements in this set can be removed via the Remove button.
    /// </summary>
    private readonly HashSet<Guid> _copiedElementIds = new();

    public CopyElementsDialogVM(
        AppState appState,
        OutlineService outlineService,
        StoryCADApi api,
        Windowing windowing,
        ILogService logger)
    {
        _appState = appState;
        _outlineService = outlineService;
        _api = api;
        _windowing = windowing;
        _logger = logger;

        // Initialize collections
        SourceElements = new ObservableCollection<StoryElement>();
        TargetElements = new ObservableCollection<StoryElement>();

        // Initialize commands
        BrowseTargetCommand = new AsyncRelayCommand(BrowseTargetAsync);
        CopyElementCommand = new RelayCommand(CopyElement);
        RemoveElementCommand = new RelayCommand(RemoveElement);
        MoveUpCommand = new RelayCommand(MoveUp);
        MoveDownCommand = new RelayCommand(MoveDown);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(Cancel);

        // Set initial status message
        StatusMessage = "Select a filter type and browse for a target outline.";
    }

    #region Properties

    /// <summary>
    /// List of element types that can be copied between outlines.
    /// </summary>
    public List<StoryItemType> CopyableTypes { get; } = new()
    {
        StoryItemType.Character,
        StoryItemType.Setting,
        StoryItemType.StoryWorld,
        StoryItemType.Problem,
        StoryItemType.Notes,
        StoryItemType.Web
    };

    /// <summary>
    /// The target StoryModel loaded from the selected file.
    /// </summary>
    public StoryModel TargetModel
    {
        get => _targetModel;
        set => SetProperty(ref _targetModel, value);
    }

    /// <summary>
    /// Path to the target outline file.
    /// </summary>
    public string TargetFilePath
    {
        get => _targetFilePath;
        set => SetProperty(ref _targetFilePath, value);
    }

    /// <summary>
    /// Currently selected filter type for the element lists.
    /// </summary>
    public StoryItemType SelectedFilterType
    {
        get => _selectedFilterType;
        set
        {
            if (SetProperty(ref _selectedFilterType, value))
            {
                RefreshSourceElements();
                // Don't refresh target - it accumulates copied elements regardless of filter
            }
        }
    }

    /// <summary>
    /// Elements from the current outline filtered by SelectedFilterType.
    /// </summary>
    public ObservableCollection<StoryElement> SourceElements { get; }

    /// <summary>
    /// Elements from the target outline filtered by SelectedFilterType.
    /// </summary>
    public ObservableCollection<StoryElement> TargetElements { get; }

    /// <summary>
    /// Currently selected element in the source list.
    /// </summary>
    public StoryElement SelectedSourceElement
    {
        get => _selectedSourceElement;
        set => SetProperty(ref _selectedSourceElement, value);
    }

    /// <summary>
    /// Currently selected element in the target list.
    /// </summary>
    public StoryElement SelectedTargetElement
    {
        get => _selectedTargetElement;
        set => SetProperty(ref _selectedTargetElement, value);
    }

    /// <summary>
    /// Status message displayed to the user.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>
    /// Count of elements copied during this session.
    /// </summary>
    public int CopiedCount
    {
        get => _copiedCount;
        set => SetProperty(ref _copiedCount, value);
    }

    /// <summary>
    /// Dialog title including the current outline name.
    /// </summary>
    public string DialogTitle
    {
        get
        {
            var storyName = GetCurrentStoryName();
            return $"Copy Elements from \"{storyName}\" to Another Outline";
        }
    }

    #endregion

    #region Commands

    public IAsyncRelayCommand BrowseTargetCommand { get; }
    public IRelayCommand CopyElementCommand { get; }
    public IRelayCommand RemoveElementCommand { get; }
    public IRelayCommand MoveUpCommand { get; }
    public IRelayCommand MoveDownCommand { get; }
    public IAsyncRelayCommand SaveCommand { get; }
    public IRelayCommand CancelCommand { get; }

    #endregion

    #region Public Methods

    /// <summary>
    /// Opens the Copy Elements dialog.
    /// </summary>
    public async Task OpenCopyElementsDialog()
    {
        // Reset state for new dialog session
        ResetState();

        try
        {
            _logger.Log(LogLevel.Info, "Opening Copy Elements dialog");

            var dialog = new ContentDialog
            {
                Title = DialogTitle,
                PrimaryButtonText = "Save",
                SecondaryButtonText = "Cancel",
                Content = new CopyElementsDialog()
            };

            var result = await _windowing.ShowContentDialog(dialog);

            // If user clicked Save, save the target file
            if (result == ContentDialogResult.Primary && TargetModel != null)
            {
                await SaveAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogException(LogLevel.Error, ex, "Error in OpenCopyElementsDialog");
        }
    }

    /// <summary>
    /// Refreshes the source elements list based on the selected filter type.
    /// </summary>
    public void RefreshSourceElements()
    {
        SourceElements.Clear();

        var model = _appState.CurrentDocument?.Model;
        if (model == null)
        {
            return;
        }

        var elements = model.StoryElements
            .Where(e => e.ElementType == SelectedFilterType);

        foreach (var element in elements)
        {
            SourceElements.Add(element);
        }

        if (SourceElements.Count == 0)
        {
            StatusMessage = $"No {SelectedFilterType} elements in current outline.";
        }
    }

    /// <summary>
    /// Refreshes the target elements list based on the selected filter type.
    /// </summary>
    public void RefreshTargetElements()
    {
        TargetElements.Clear();

        if (TargetModel == null)
        {
            return;
        }

        var elements = TargetModel.StoryElements
            .Where(e => e.ElementType == SelectedFilterType);

        foreach (var element in elements)
        {
            TargetElements.Add(element);
        }
    }

    /// <summary>
    /// Checks if an element was copied during this session.
    /// Used to determine if an element can be safely removed.
    /// </summary>
    /// <param name="uuid">The UUID of the element to check</param>
    /// <returns>True if the element was copied this session</returns>
    public bool IsSessionCopied(Guid uuid)
    {
        return _copiedElementIds.Contains(uuid);
    }

    /// <summary>
    /// Loads a target outline file for copying elements to.
    /// Uses API.OpenOutline to set the API's CurrentModel state for subsequent operations.
    /// </summary>
    /// <param name="path">Path to the target .stbx file</param>
    public async Task LoadTargetFileAsync(string path)
    {
        try
        {
            // Validate path exists
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                StatusMessage = "Error: File not found.";
                TargetModel = null;
                TargetFilePath = string.Empty;
                return;
            }

            // Check if trying to load current file as target
            var currentPath = _appState.CurrentDocument?.FilePath;
            if (!string.IsNullOrEmpty(currentPath) &&
                string.Equals(Path.GetFullPath(path), Path.GetFullPath(currentPath), StringComparison.OrdinalIgnoreCase))
            {
                StatusMessage = "Cannot select current file as target.";
                TargetModel = null;
                TargetFilePath = string.Empty;
                return;
            }

            // Use API to open the target file - this sets API.CurrentModel as state
            // for all subsequent API calls (AddElement, WriteOutline)
            _logger.Log(LogLevel.Info, $"Loading target file via API: {path}");
            var result = await _api.OpenOutline(path);

            if (!result.IsSuccess)
            {
                StatusMessage = $"Error loading file: {result.ErrorMessage}";
                TargetModel = null;
                TargetFilePath = string.Empty;
                return;
            }

            // Store reference to the API's CurrentModel for UI binding
            TargetModel = _api.CurrentModel;
            TargetFilePath = path;

            // Get target story name for status
            var targetName = Path.GetFileNameWithoutExtension(path);
            var overview = TargetModel?.StoryElements
                .FirstOrDefault(e => e.ElementType == StoryItemType.StoryOverview);
            if (overview != null && !string.IsNullOrEmpty(overview.Name))
            {
                targetName = overview.Name;
            }

            StatusMessage = $"Target loaded: {targetName}";
            _logger.Log(LogLevel.Info, $"Target file loaded successfully: {targetName}");

            // Clear copied elements when target changes - starting fresh with new target
            TargetElements.Clear();
            _copiedElementIds.Clear();
            CopiedCount = 0;
        }
        catch (Exception ex)
        {
            _logger.LogException(LogLevel.Error, ex, $"Error loading target file: {path}");
            StatusMessage = $"Error loading file: {ex.Message}";
            TargetModel = null;
            TargetFilePath = string.Empty;
        }
    }

    #endregion

    #region Private Methods

    private string GetCurrentStoryName()
    {
        var model = _appState.CurrentDocument?.Model;
        if (model == null)
        {
            return "Untitled";
        }

        // Get name from Overview element
        var overview = model.StoryElements
            .FirstOrDefault(e => e.ElementType == StoryItemType.StoryOverview);

        if (overview != null && !string.IsNullOrEmpty(overview.Name))
        {
            return overview.Name;
        }

        // Fall back to filename
        var path = _appState.CurrentDocument?.FilePath;
        if (!string.IsNullOrEmpty(path))
        {
            return Path.GetFileNameWithoutExtension(path);
        }

        return "Untitled";
    }

    private void ResetState()
    {
        TargetModel = null;
        TargetFilePath = string.Empty;
        SourceElements.Clear();
        TargetElements.Clear();
        SelectedSourceElement = null;
        SelectedTargetElement = null;
        StatusMessage = "Select a filter type and browse for a target outline.";
        CopiedCount = 0;
        _copiedElementIds.Clear();
    }

    private async Task BrowseTargetAsync()
    {
        try
        {
            _logger.Log(LogLevel.Info, "BrowseTargetAsync called");

            // Show file picker for .stbx files
            var file = await _windowing.ShowFilePicker("Open", ".stbx");

            if (file != null)
            {
                await LoadTargetFileAsync(file.Path);
            }
            else
            {
                _logger.Log(LogLevel.Info, "User cancelled file picker");
            }
        }
        catch (Exception ex)
        {
            _logger.LogException(LogLevel.Error, ex, "Error in BrowseTargetAsync");
            StatusMessage = "Error opening file picker.";
        }
    }

    /// <summary>
    /// Copies the selected source element to the target model.
    /// API.CurrentModel is already set to the target by LoadTargetFileAsync.
    /// </summary>
    public void CopyElement()
    {
        _logger.Log(LogLevel.Info, "CopyElement called");

        // Validate preconditions
        if (SelectedSourceElement == null)
        {
            _logger.Log(LogLevel.Info, "No source element selected");
            return;
        }

        if (TargetModel == null)
        {
            StatusMessage = "Please select a target outline first.";
            _logger.Log(LogLevel.Warn, "No target model loaded");
            return;
        }

        // Check StoryWorld singleton constraint
        if (SelectedSourceElement.ElementType == StoryItemType.StoryWorld)
        {
            var existingWorld = TargetModel.StoryElements
                .Any(e => e.ElementType == StoryItemType.StoryWorld);
            if (existingWorld)
            {
                StatusMessage = "Target already has a StoryWorld. Only one allowed per story.";
                _logger.Log(LogLevel.Warn, "Target already has StoryWorld - copy blocked");
                return;
            }
        }

        // Get the Overview element as parent for the new element
        var targetOverview = TargetModel.StoryElements
            .FirstOrDefault(e => e.ElementType == StoryItemType.StoryOverview);
        if (targetOverview == null)
        {
            StatusMessage = "Error: Target has no Overview element.";
            _logger.LogException(LogLevel.Error, new InvalidOperationException("No overview"), "Target model invalid");
            return;
        }

        // Verify the Overview has a valid node in the tree structure
        if (targetOverview.Node == null)
        {
            StatusMessage = "Error: Target Overview has no tree node.";
            _logger.Log(LogLevel.Error, "Target Overview.Node is null - tree structure not initialized");
            return;
        }

        // API.CurrentModel was set by LoadTargetFileAsync via OpenOutline
        // Step 1: Create the element - preserving original GUID across the fictional universe
        var result = _api.AddElement(
            SelectedSourceElement.ElementType,
            targetOverview.Uuid.ToString(),
            SelectedSourceElement.Name,
            GUIDOverride: SelectedSourceElement.Uuid.ToString());

        if (!result.IsSuccess)
        {
            StatusMessage = $"Copy failed: {result.ErrorMessage}";
            _logger.Log(LogLevel.Error, $"Copy failed: {result.ErrorMessage}");
            return;
        }

        var copiedUuid = result.Payload;
        _logger.Log(LogLevel.Info, $"Created element {SelectedSourceElement.Name} with GUID {copiedUuid}");

        // Step 2: Copy properties from source to the new element
        try
        {
            // Serialize source element to get all properties
            var sourceJson = SelectedSourceElement.Serialize();
            var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(sourceJson);

            // Get the target element to access property types for deserialization
            var targetElement = TargetModel.StoryElements.FirstOrDefault(e => e.Uuid == copiedUuid);
            if (targetElement == null)
            {
                _logger.Log(LogLevel.Warn, "Could not find copied element for property update");
                return;
            }

            // Convert to Dictionary<string, object> for UpdateElementProperties
            // Skip properties that shouldn't be copied (structural/system properties)
            var propertiesToCopy = new Dictionary<string, object>();
            var skipProperties = new HashSet<string>
            {
                "Uuid", "Type", "Name", "Node", "Children", "Parent",
                "ElementType", "IsSelected", "IsExpanded"
            };

            foreach (var kvp in properties)
            {
                if (skipProperties.Contains(kvp.Key))
                    continue;

                // Convert JsonElement to appropriate type
                object value = null;
                switch (kvp.Value.ValueKind)
                {
                    case JsonValueKind.String:
                        value = kvp.Value.GetString();
                        break;
                    case JsonValueKind.Number:
                        value = kvp.Value.GetInt32();
                        break;
                    case JsonValueKind.True:
                        value = true;
                        break;
                    case JsonValueKind.False:
                        value = false;
                        break;
                    case JsonValueKind.Array:
                    case JsonValueKind.Object:
                        // Deserialize to the actual property type
                        var propInfo = targetElement.GetType().GetProperty(kvp.Key);
                        if (propInfo != null)
                        {
                            var jsonText = kvp.Value.GetRawText();
                            value = JsonSerializer.Deserialize(jsonText, propInfo.PropertyType);
                        }
                        break;
                }

                if (value != null)
                    propertiesToCopy[kvp.Key] = value;
            }

            if (propertiesToCopy.Count > 0)
            {
                var updateResult = _api.UpdateElementProperties(copiedUuid, propertiesToCopy);
                if (!updateResult.IsSuccess)
                {
                    _logger.Log(LogLevel.Warn, $"Failed to copy some properties: {updateResult.ErrorMessage}");
                }
                else
                {
                    _logger.Log(LogLevel.Info, $"Copied {propertiesToCopy.Count} properties to {SelectedSourceElement.Name}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogException(LogLevel.Warn, ex, "Error copying properties - element created but may be incomplete");
        }

        // Track the copied element and add to target list directly
        _copiedElementIds.Add(copiedUuid);
        CopiedCount++;
        var copiedElement = TargetModel.StoryElements.FirstOrDefault(e => e.Uuid == copiedUuid);
        if (copiedElement != null)
        {
            TargetElements.Add(copiedElement);
        }
        StatusMessage = $"Copied: {SelectedSourceElement.Name}";
        _logger.Log(LogLevel.Info, $"Copied {SelectedSourceElement.Name} to target with GUID {copiedUuid}");
    }

    /// <summary>
    /// Removes the selected target element if it was copied this session.
    /// </summary>
    public void RemoveElement()
    {
        _logger.Log(LogLevel.Info, "RemoveElement called");

        // Validate preconditions
        if (SelectedTargetElement == null)
        {
            _logger.Log(LogLevel.Info, "No target element selected");
            return;
        }

        if (TargetModel == null)
        {
            _logger.Log(LogLevel.Warn, "No target model loaded");
            return;
        }

        // Check if element was copied this session
        if (!_copiedElementIds.Contains(SelectedTargetElement.Uuid))
        {
            StatusMessage = "Can only remove elements copied this session.";
            _logger.Log(LogLevel.Warn, "Attempted to remove non-session element");
            return;
        }

        // Remove the element from the target model
        var elementToRemove = SelectedTargetElement;
        var elementName = elementToRemove.Name;

        // Remove from StoryElements collection
        TargetModel.StoryElements.Remove(elementToRemove);

        // Remove from parent's children if has node
        if (elementToRemove.Node?.Parent != null)
        {
            elementToRemove.Node.Parent.Children.Remove(elementToRemove.Node);
        }

        // Update tracking
        _copiedElementIds.Remove(elementToRemove.Uuid);
        CopiedCount--;

        // Remove from target list directly and clear selection
        TargetElements.Remove(elementToRemove);
        SelectedTargetElement = null;

        StatusMessage = $"Removed: {elementName}";
        _logger.Log(LogLevel.Info, $"Removed element: {elementName}");
    }

    /// <summary>
    /// Moves selection up in the source list.
    /// </summary>
    public void MoveUp()
    {
        _logger.Log(LogLevel.Info, "MoveUp called");

        if (SelectedSourceElement == null || SourceElements.Count == 0)
        {
            return;
        }

        var currentIndex = SourceElements.IndexOf(SelectedSourceElement);
        if (currentIndex > 0)
        {
            SelectedSourceElement = SourceElements[currentIndex - 1];
        }
    }

    /// <summary>
    /// Moves selection down in the source list.
    /// </summary>
    public void MoveDown()
    {
        _logger.Log(LogLevel.Info, "MoveDown called");

        if (SelectedSourceElement == null || SourceElements.Count == 0)
        {
            return;
        }

        var currentIndex = SourceElements.IndexOf(SelectedSourceElement);
        if (currentIndex < SourceElements.Count - 1)
        {
            SelectedSourceElement = SourceElements[currentIndex + 1];
        }
    }

    /// <summary>
    /// Saves the target file with any copied elements using the API.
    /// API.CurrentModel is already set to the target by LoadTargetFileAsync.
    /// </summary>
    public async Task SaveAsync()
    {
        _logger.Log(LogLevel.Info, "SaveAsync called");

        if (TargetModel == null || string.IsNullOrEmpty(TargetFilePath))
        {
            _logger.Log(LogLevel.Warn, "No target to save");
            return;
        }

        try
        {
            // API.CurrentModel was set by LoadTargetFileAsync via OpenOutline
            // Just call WriteOutline directly
            var result = await _api.WriteOutline(TargetFilePath);

            if (result.IsSuccess)
            {
                StatusMessage = $"Saved: {Path.GetFileName(TargetFilePath)}";
                _logger.Log(LogLevel.Info, $"Target file saved: {TargetFilePath}");
            }
            else
            {
                StatusMessage = $"Save failed: {result.ErrorMessage}";
                _logger.Log(LogLevel.Error, $"Save failed: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogException(LogLevel.Error, ex, $"Error saving target file: {TargetFilePath}");
            StatusMessage = $"Save failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Cancels the dialog and resets state without saving.
    /// </summary>
    public void Cancel()
    {
        _logger.Log(LogLevel.Info, "Cancel called");
        ResetState();
    }

    #endregion
}
