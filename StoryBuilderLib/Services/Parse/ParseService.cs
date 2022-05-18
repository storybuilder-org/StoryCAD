using System;
using System.Threading.Tasks;
using Parse;
using StoryBuilder.Models;
using StoryBuilder.Models.Tools;
using StoryBuilder.Services.Logging;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.DAL;

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
                preferences.LastContact = DateTime.Now;
                await Ioc.Default.GetService<PreferencesIO>().UpdateFile();
                log.Log(LogLevel.Info, "PostPreferences successful");

            }
            catch (ParseException ex)
            {
                //Note: I'm unable to access ErrorCode. The
                //      parse server messages are unique, though,
                //      and can be used to identify specific 
                //      exceptions for corrective action: ex, 
                //"Account already exists for this username"
                //"Invalid session token"
                //"Invalid username/password"
                log.LogException(LogLevel.Warn, ex, ex.Message);
            }
            catch (Exception ex)
            {
                // (InnerException) "Invalid session token
                log.LogException(LogLevel.Warn, ex, ex.Message);
            }
        }

        public async Task PostVersion()
        {
            log.Log(LogLevel.Info, "Posting version data to parse");
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
                var preferences = GlobalData.Preferences;
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
                log.Log(LogLevel.Info, "PostVersion successful");
            }

            catch (ParseException ex)
            {
                //See PostPreferences for notes
                log.LogException(LogLevel.Warn, ex, ex.Message);
            }
            catch (Exception ex)
            {
                // (InnerException) "Invalid session token
                log.LogException(LogLevel.Warn, ex, ex.Message);
            }

        }
    }
}   
    