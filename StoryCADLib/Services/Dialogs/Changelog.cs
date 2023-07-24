using System.Threading.Tasks;
using ABI.Windows.UI.Notifications;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Octokit;
using StoryCAD.Models;
using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.Services.Logging;

namespace StoryCAD.Services.Dialogs
{
    /// <summary>
    /// This class handles showing Change logs for the app
    /// </summary>
    public class Changelog
    {
        private GitHubClient _client = new(new ProductHeaderValue("STB"));
        private LogService Logger = Ioc.Default.GetRequiredService<LogService>();

        /// <summary>
        /// This access the changelog for the latest version
        /// </summary>
        /// <returns>The changelog text if successful</returns>
        public async Task<string> GetChangelogText()
        {
            try
            {
                //Returns body of release
                return (await _client.Repository.Release.Get("StoryBuilder-org", "StoryCAD",
                    GlobalData.Version.Replace("Version: ", ""))).Body;
            }
            catch (Exception _e)
            {
                if (_e.Source!.Contains("Net"))
                {
                    Logger.Log(LogLevel.Info, "Error with network, user probably isn't connected to wifi or is using an auto build");
                }

                if (_e.Source!.Contains("Octokit.NotFoundException"))
                {
                    Logger.Log(LogLevel.Info, "Error finding changelog for this version");
                }

                //Return error message
                return
                """
                Failed to get changelog for this version, this because either:
                 - You are using a development version of StoryCAD
                 - An issue was encountered connecting to GitHub to get the release information
                """;
            }
        }

        public async Task ShowChangeLog()
        {
            //Don't Show changelog on dev build's since its pointless.
            if (GlobalData.DeveloperBuild) { return; }

            try
            {
                ContentDialog _changelogUI = new()
                {
                    Width = 800,
                    Content = new ScrollViewer
                    {
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        Content = new TextBlock
                        {
                            TextWrapping = TextWrapping.Wrap,
                            Text = (await GetChangelogText())
                        }
                    },
                    Title = "What's new in StoryCAD " + GlobalData.Version,
                    PrimaryButtonText = "Okay",
                    XamlRoot = GlobalData.XamlRoot
                };
                await _changelogUI.ShowAsync();
            }
            catch (Exception _e)
            {
                Logger.LogException(LogLevel.Error,_e, "Error in ShowChangeLog()");
            }
        }

    }
}
