using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using StoryCADLib.Models;
using StoryCADLib.Services.Dialogs;
using StoryCADLib.Services.Logging;
using StoryCADLib.Services.Store;
using Windows.System;

namespace StoryCADLib.ViewModels.Store;

/// <summary>
///     Join / Not now dialog. Sibling of <see cref="SubscribeDialogViewModel" />.
///     Toolbar click is not enrollment; only Join posts enroll true.
/// </summary>
public sealed class BetaEnrollmentDialogViewModel
{
    private readonly IStoreActivationService _activation;
    private readonly ILogService _logService;

    public string FailureReason { get; private set; }

    public RelayCommand OpenGettingStartedCommand { get; }

    public BetaEnrollmentDialogViewModel(IStoreActivationService activation, ILogService logService)
    {
        _activation = activation;
        _logService = logService;
        OpenGettingStartedCommand = new RelayCommand(OpenGettingStarted);
    }

    public async Task<bool> ShowAsync(Windowing windowing)
    {
        FailureReason = null;
        var dialog = new ContentDialog
        {
            Title = "StoryCAD Collaborator",
            Content = new BetaEnrollmentDialog(this),
            PrimaryButtonText = "Join",
            CloseButtonText = "Not now",
            DefaultButton = ContentDialogButton.Primary
        };

        dialog.PrimaryButtonClick += async (_, args) =>
        {
            var deferral = args.GetDeferral();
            try
            {
                args.Cancel = !await JoinAsync();
            }
            finally
            {
                deferral.Complete();
            }
        };

        await windowing.ShowContentDialog(dialog);
        return _activation.State == ActivationState.Active;
    }

    internal async Task<bool> JoinAsync()
    {
        try
        {
            var response = await _activation.EnrollBetaAsync();
            if (response.Ok && !string.IsNullOrEmpty(response.Jwt))
            {
                return true;
            }

            FailureReason = response.Reason;
            return false;
        }
        catch (Exception ex)
        {
            _logService.Log(LogLevel.Warn, $"Join failed: {ex.Message}");
            FailureReason = "unreachable";
            return false;
        }
    }

    private void OpenGettingStarted()
    {
        var baseUrl = Ioc.Default.GetService<AppState>()?.ManualBaseUrl
                      ?? "https://beta.manual.storybuilder.org/";
        var url = new Uri(new Uri(baseUrl), StoreConfig.GettingStartedManualRelativeUrl).ToString();
        try
        {
            _ = Launcher.LaunchUriAsync(new Uri(url));
        }
        catch (Exception ex)
        {
            _logService.Log(LogLevel.Warn, $"Failed to open {url}: {ex.Message}");
        }
    }
}
