using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using StoryCADLib.Controls;
using StoryCADLib.Services.Messages;

namespace StoryCADLib.Services.BeatTree;

/// <summary>
///     Opens and closes the beat tree window (issue #1546): a second window that shows one beat
///     column for the Story Problem and one for each Problem below it.
/// </summary>
public class BeatTreeWindowService
{
    private readonly AppState _appState;
    private readonly EditFlushService _editFlushService;
    private readonly Windowing _windowing;
    private readonly ILogService _logger;

    private BeatTreeViewModel _viewModel;

    public BeatTreeWindowService(AppState appState, EditFlushService editFlushService, Windowing windowing,
        ILogService logger)
    {
        _appState = appState;
        _editFlushService = editFlushService;
        _windowing = windowing;
        _logger = logger;

        // The window shows one outline. When the outline closes or changes, the window closes.
        _appState.CurrentDocumentChanged += (_, _) => Close();
    }

    /// <summary>The beat tree window, or null when it is not open.</summary>
    public Window Window { get; private set; }

    /// <summary>
    ///     Opens the beat tree window for the open outline. If the window is open, brings it to the front.
    /// </summary>
    public void Open()
    {
        if (Window != null)
        {
            Window.Activate();
            return;
        }

        var model = _appState.CurrentDocument?.Model;
        if (model == null || model.StoryElements.Count == 0)
        {
            WeakReferenceMessenger.Default.Send(new StatusChangedMessage(
                new StatusMessage("Open or create an outline to see the beat tree", LogLevel.Warn)));
            return;
        }

        // The form on the main window can hold edits that are not in the model yet.
        _editFlushService.FlushCurrentEdits();

        _logger.Log(LogLevel.Info, "Opening the beat tree window");
        _viewModel = new BeatTreeViewModel();
        var view = new BeatTreeView { ViewModel = _viewModel, RequestedTheme = _windowing.RequestedTheme };
        _viewModel.DialogRoot = () => view.XamlRoot;
        _viewModel.Load(model);

        Window = new Window { Content = view, Title = "Beat Tree" };
        Window.Activated += OnActivated;
        Window.Closed += OnClosed;
        Window.Activate();
    }

    /// <summary>Closes the beat tree window if it is open.</summary>
    public void Close()
    {
        var window = Window;
        if (window == null)
        {
            return;
        }

        Release();
        try
        {
            window.Close();
        }
        catch (Exception ex)
        {
            _logger.LogException(LogLevel.Warn, ex, "The beat tree window did not close cleanly");
        }
    }

    /// <summary>
    ///     When the writer comes back to the window, read the outline again: an element can be new
    ///     or have a new name, and the API or Collaborator can have changed the structure.
    /// </summary>
    private void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        var model = _appState.CurrentDocument?.Model;
        if (args.WindowActivationState != WindowActivationState.Deactivated && _viewModel != null && model != null)
        {
            _viewModel.Load(model);
        }
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        Release();
    }

    private void Release()
    {
        if (Window != null)
        {
            Window.Activated -= OnActivated;
            Window.Closed -= OnClosed;
        }

        Window = null;
        _viewModel = null;
    }
}
