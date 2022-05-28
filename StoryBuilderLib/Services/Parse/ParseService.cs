using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Parse;
using StoryBuilder.Models;
using StoryBuilder.Models.Tools;
using StoryBuilder.Services.Logging;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.DAL;
using Windows.Storage;
using ABI.Windows.Devices.Printers.Extensions;

namespace StoryBuilder.Services.Parse
{

    /// <summary>
    /// ParseService is StoryBuilder's interface to our backend server,
    /// aristotle.storybuiler.com. The server is hosted by AARCH64.com
    /// in partnership with FOSSHost.com and hosts the open source 
    /// parse server (see https://parseplatform.com
    /// 
    /// Docs: https://docs.parseplatform.org/dotnet/guide/
    /// </summary>
    public class ParseService
    {
        private LogService Log = Ioc.Default.GetService<LogService>();
        public async void Begin()
        {
            BackgroundWorker Worker = new();
            Worker.DoWork += async (sender, e) =>
            {
                try
                {
                    // If the previous attempt to communicate to the back-end server failed, retry
                    if (!GlobalData.Preferences.ParsePreferencesStatus)
                        await PostPreferences(GlobalData.Preferences);
                    if (!GlobalData.Preferences.ParseVersionStatus)
                        await PostVersion();

                    if (!GlobalData.Version.Equals(GlobalData.Preferences.Version))
                    {
                        // Process a version change (usually a new release)
                        Log.Log(LogLevel.Info, "Version mismatch: " + GlobalData.Version + " != " + GlobalData.Preferences.Version);
                        var preferences = GlobalData.Preferences;
                        // Update Preferences
                        preferences.Version = GlobalData.Version;
                        PreferencesIO prefIO = new(preferences, GlobalData.RootDirectory);
                        await prefIO.UpdateFile();
                        // Post deployment to backend server
                        await PostVersion();
                    }
                }
                catch (Exception ex)
                {
                    Log.LogException(LogLevel.Warn,ex, "Error in parse service worker");
                }
            };
            Worker.RunWorkerAsync();
        }

        private LogService log = Ioc.Default.GetService<LogService>();
        public async Task PostPreferences(PreferencesModel preferences)
        {
            log.Log(LogLevel.Info, "Posting preferences data to parse");
            try
            {
                log.Log(LogLevel.Info, "Register ParsePreferences subclass");
                ParseObject.RegisterSubclass<ParsePreferences>();

                log.Log(LogLevel.Info, "Initialize ParseClient");
                ParseClient.Initialize(new ParseClient.Configuration
                {
                    ApplicationId = "StoryBuilder",
                    Server = "http://localhost:1337/parse/"
                });

                if (ParseUser.CurrentUser != null)
                {
                    ParseUser.LogOut();
                }

                log.Log(LogLevel.Info, "Create ParsePrefences ParseObject");
                var pref = new ParsePreferences
                {
                    Email = preferences.Email,
                    Name = preferences.Name,
                    ErrorCollectionConsent = preferences.ErrorCollectionConsent,
                    Newsletter = preferences.Newsletter,
                    Version = "1.0.0",
                    UpdateDate = DateTime.Now.ToShortDateString()
                };

                log.Log(LogLevel.Info, "Save ParsePreferences data");
                await pref.SaveAsync();
                await Ioc.Default.GetService<PreferencesIO>().UpdateFile();
                log.Log(LogLevel.Info, "PostPreferences successful");
                await SavePreferencesStatus(true); 

            }
            catch (ParseException ex)
            {
                //Note: I'm unable to access ErrorCode. The
                //      parse server messages are unique, though,
                //      and can be used to identify specific 
                //      exceptions for corrective action: ex, 
                //"Account already exists for this username"
                //"Invalid session token"
                await SavePreferencesStatus(false);
                //"Invalid username/password"
                log.LogException(LogLevel.Warn, ex, ex.Message);
            }
            catch (Exception ex)
            {
                // (InnerException) "Invalid session token
                await SavePreferencesStatus(false);
                log.LogException(LogLevel.Warn, ex, ex.Message);
            }
        }

        public async Task PostVersion()
        {
            log.Log(LogLevel.Info, "Posting version data to parse");
            var preferences = GlobalData.Preferences;
            try
            {
                log.Log(LogLevel.Info, "Register ParseVersion subclass");
                ParseObject.RegisterSubclass<ParseVersion>();

                log.Log(LogLevel.Info, "Initialize ParseClient");
                ParseClient.Initialize(new ParseClient.Configuration
                {
                    ApplicationId = "StoryBuilder",
                    Server = "http://localhost:1337/parse/"
                });

                if (ParseUser.CurrentUser != null)
                {
                    ParseUser.LogOut();
                }

                log.Log(LogLevel.Info, "Create ParseVersion ParseObject");
                var vers = new ParseVersion
                {

                    Email = preferences.Email,
                    Name = preferences.Name,
                    PreviousVersion = preferences.Version ?? "",
                    CurrentVersion = GlobalData.Version,
                    RunDate = DateTime.Now.ToShortDateString()
                };
                log.Log(LogLevel.Info, "Save Version data to parse-server");
                await vers.SaveAsync();
                await SaveVersionStatus(true);
                log.Log(LogLevel.Info, "PostVersion successful");
            }
            catch (ParseException ex)
            {
                //See PostPreferences for notes
                await SaveVersionStatus(false);
                log.LogException(LogLevel.Warn, ex, ex.Message);
            }
            catch (Exception ex)
            {
                // (InnerException) "Invalid session token
                await SaveVersionStatus(false);
                log.LogException(LogLevel.Warn, ex, ex.Message);
            }
        }
        
        private async Task SavePreferencesStatus(bool preferencesStatus) 
        {
            PreferencesIO prfIO = new(GlobalData.Preferences, System.IO.Path.Combine(ApplicationData.Current.RoamingFolder.Path, "Storybuilder"));
            GlobalData.Preferences.ParsePreferencesStatus = preferencesStatus;
            await prfIO.UpdateFile();
        }

        private async Task SaveVersionStatus(bool versionStatus)
        {
            PreferencesIO prfIO = new(GlobalData.Preferences, System.IO.Path.Combine(ApplicationData.Current.RoamingFolder.Path, "Storybuilder"));
            GlobalData.Preferences.ParseVersionStatus = versionStatus;
            await prfIO.UpdateFile();

        }
    }
}   
    