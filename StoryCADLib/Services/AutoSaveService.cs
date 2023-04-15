using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.Models;
using StoryCAD.Services.Logging;
using StoryCAD.ViewModels;
using LogLevel = StoryCAD.Services.Logging.LogLevel;

namespace StoryCAD.Services
{
    public class AutoSaveService
    {
        private LogService _logger = Ioc.Default.GetRequiredService<LogService>();
        ShellViewModel _shellVM;
        private BackgroundWorker autoSaveWorker = new()
        { WorkerSupportsCancellation = true, WorkerReportsProgress = false };

        /// <summary>
        /// Performs an AutoSave every x seconds, x being the value of AutoSaveInterval in user preferences.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void AutoSaveTask(object sender, DoWorkEventArgs e)
        {
            //TODO: Move the following lines to Preferences, add appropriate Status and logging
            if (GlobalData.Preferences.AutoSaveInterval is > 61 or < 14)
                GlobalData.Preferences.AutoSaveInterval = 30;

            try
            {
                _logger.Log(LogLevel.Info, "AutoSave task started.");
                while (!autoSaveWorker.CancellationPending)
                {
                    System.Threading.Thread.Sleep(GlobalData.Preferences.AutoSaveInterval * 1000);
                    _logger.Log(LogLevel.Info, "Initiating AutoSave backup.");
                    await AutoSaveProject();
                }
                e.Cancel = true;
                _logger.Log(LogLevel.Info, "AutoSave task finished.");
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, ex, "Error in AutoSave task.");
            }
        }

        public void StopService()
        {
            _logger.Log(LogLevel.Info, "Trying to stop AutoSave Service.");
            try
            {
                if (!autoSaveWorker.IsBusy)
                {
                    autoSaveWorker.CancelAsync();
                    _logger.Log(LogLevel.Info, "AutoSave thread requested to stop.");
                }
                autoSaveWorker.DoWork -= AutoSaveTask;
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, ex, "Error stopping AutoSave service StopService()");
            }
        }

        public void StartAutoSave()
        {
            _shellVM = Ioc.Default.GetRequiredService<ShellViewModel>();
            if (GlobalData.Preferences.AutoSave)
            {
                autoSaveWorker.DoWork += AutoSaveTask;
                if (!autoSaveWorker.IsBusy)
                    autoSaveWorker.RunWorkerAsync();
            }
        }

        /// <summary>
        /// This is ran if the user has enabled AutoSave,
        /// it runs every x seconds, and simply saves the file
        /// </summary>  
        private async Task AutoSaveProject()
        {
            try
            {
                if (autoSaveWorker.CancellationPending || !GlobalData.Preferences.AutoSave ||
                    _shellVM.StoryModel.StoryElements.Count == 0)
                {
                    return;
                }

                if (_shellVM.StoryModel.Changed)
                {
                    // Save and write the model on the UI autoSaveWorker,
                    GlobalData.GlobalDispatcher.TryEnqueue(async () => await _shellVM.SaveFile(true));
                }
            }
            catch (Exception _ex)
            {
                _logger.LogException(LogLevel.Error, _ex,
                    "Error saving file in AutoSaveService.AutoSaveProject()");
            }
        }
    }
}

