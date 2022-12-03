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
                    ShellViewModel _ShellVM = Ioc.Default.GetRequiredService<ShellViewModel>();
                    if (Thread.CancellationPending || !GlobalData.Preferences.AutoSave || _ShellVM.StoryModel.StoryElements.Count == 0)
                    {
                        IsRunning = false;
                        return;
                    }
                    if (_ShellVM.StoryModel.Changed)
                    {
                        _logger.Log(LogLevel.Info, "Starting SaveFileTask (AutoSave)");
                        try //Updating the lost modified timer
                        {
                            OverviewModel _overview = (_ShellVM.StoryModel.StoryElements.StoryElementGuids[_ShellVM.StoryModel.ExplorerView[0].Uuid]) as OverviewModel;
                            _overview.DateModified = DateTime.Now.ToString("d");
                        }
                        catch (Exception ex) { _logger.Log(LogLevel.Warn, "Failed to update last modified date/time"); }
                        
                        //Save and write.
                        Dispatcher.TryEnqueue(() => { _ShellVM.SaveModel(); }); //Runs on UI Thread, so we can figure out what page is open and save the correct VM.
                        await _ShellVM.WriteModel(); //Write file to disk
                        _ShellVM.StoryModel.Changed = false;
                        _logger.Log(LogLevel.Info, "Wrote autosave file");

                        //Change pen icon back to green so user can see all is good
                        Dispatcher.TryEnqueue(() => { _ShellVM.ChangeStatusColor = Colors.Green; });
                        _logger.Log(LogLevel.Info, "changed pen back to green.");
                    }
                }
                catch (Exception _ex)
                {
                    _logger.LogException(LogLevel.Error, _ex, "Error AutoSaving file in AutoSaveService.SaveFileTask()");
                }
                //Sleep Users Interval (in seconds)
                System.Threading.Thread.Sleep(GlobalData.Preferences.AutoSaveInterval * 1000);
            }
        }
    }
}
