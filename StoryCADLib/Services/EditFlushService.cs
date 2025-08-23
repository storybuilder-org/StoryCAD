using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.Models;
using StoryCAD.Services.Logging;

namespace StoryCAD.Services;

/// <summary>
/// Service responsible for flushing UI edits from ViewModels to the Model.
/// Eliminates circular dependencies by using AppState instead of direct ViewModel references.
/// </summary>
public class EditFlushService
{
    private readonly AppState _appState;
    private readonly ILogService _logger;
    
    public EditFlushService(AppState appState)
    {
        _appState = appState;
        _logger = Ioc.Default.GetRequiredService<ILogService>();
    }
    
    /// <summary>
    /// Flushes edits from the current saveable ViewModel to the model.
    /// Safe to call even when CurrentSaveable is null.
    /// </summary>
    public void FlushCurrentEdits()
    {
        try
        {
            _appState.CurrentSaveable?.SaveModel();
        }
        catch (Exception ex)
        {
            _logger.LogException(LogLevel.Error, ex, "Error flushing edits to model");
        }
    }
}