using System;
using System.ComponentModel;
using Microsoft.UI.Dispatching;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI;
using NLog;
using StoryBuilder.Models;
using StoryBuilder.Services.Logging;
using StoryBuilder.ViewModels;
using LogLevel = StoryBuilder.Services.Logging.LogLevel;

namespace StoryBuilder.Services
{
    public class AutoSaveService
    {
        private LogService _logger = Ioc.Default.GetRequiredService<LogService>();
        ShellViewModel _shellVM;
        private BackgroundWorker Thread = new() { WorkerSupportsCancellation = true };
        public DispatcherQueue Dispatcher;
        private bool IsRunning = false;

        public void StopService()
        {
            _logger.Log(LogLevel.Info, "Trying to stop AutoSave Service.");
            try
            {
                if (!Thread.CancellationPending && IsRunning)
                {
                    Thread.CancelAsync();
                    _logger.Log(LogLevel.Info, "AutoSave thread requested to stop.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, ex, "Error stopping AutoSave service StopService()");
            }
            IsRunning = false;
        }

        public void StartService()
        {
            _shellVM = Ioc.Default.GetRequiredService<ShellViewModel>();
            if (GlobalData.Preferences.AutoSave && !IsRunning)
            {
                if (GlobalData.Preferences.AutoSaveInterval is > 61 or < 14) { GlobalData.Preferences.AutoSaveInterval = 30; }
                else { GlobalData.Preferences.AutoSaveInterval = GlobalData.Preferences.AutoSaveInterval; }
                Thread.DoWork += SaveFileTask;
                Thread.RunWorkerAsync();
            }
        }

        /// <summary>
        /// This is ran if the user has enabled Autosave,
        /// it runs every x seconds, and simply saves the file
        /// </summary>  
        private async void SaveFileTask(object sender, object e)
        {
            while (true)
            { 
                try
                {
                    IsRunning = true;

                    if (Thread.CancellationPending || !GlobalData.Preferences.AutoSave || 
                        _shellVM.StoryModel.StoryElements.Count == 0)
                    {
                        IsRunning = false;
                        return;
                    }
                    if (_shellVM.StoryModel.Changed)
                    {
                        // Save and write the model on the UI Thread,
                        Dispatcher.TryEnqueue(async () =>  await _shellVM.SaveFile(true) ); 
                    }
                }
                catch (Exception _ex)
                {
                    _logger.LogException(LogLevel.Error, _ex, 
                        "Error AutoSaving file in AutoSaveService.SaveFileTask()");
                }
                //Sleep Users Interval (in seconds)
                System.Threading.Thread.Sleep(GlobalData.Preferences.AutoSaveInterval * 1000);
            }
        }
    }
}
