using System;
using System.Diagnostics;
using Windows.Storage;

namespace StoryBuilder.Services.Help
{
    public class HelpService
    {
        /// <summary>
        /// Display StoryBuilder's CHM help file Contents by 
        /// invoking the HTML Help Executable as an external
        /// process. The help files are contained in the 
        /// 'help' installation subfolder.
        /// </summary>
        public async void LaunchHelp()
        {
            /*var localFolder = ApplicationData.Current.LocalFolder;
            var help = await localFolder.GetFolderAsync("help");
            string helpPath = help.Path;
            Process process = new Process
            {
                StartInfo =
                {FileName = @"hh.exe", Arguments = @"StoryBuilder.chm"}
            };
            process.StartInfo.WorkingDirectory = helpPath;
            process.Start();*/
            Process ShowHelp = new();
            ShowHelp.StartInfo.UseShellExecute = true; //System will decide best app to use
            ShowHelp.StartInfo.FileName = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\StoryBuilder\\Help\\StoryBuilder.chm";
            ShowHelp.Start();

        }
    }
}
