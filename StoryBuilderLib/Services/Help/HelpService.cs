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
        public void LaunchHelp()
        {
            Process ShowHelp = new(); //Creates new process
            ShowHelp.StartInfo.UseShellExecute = true; //System will decide best app to use, should open the CHM viewer
            //ShowHelp.StartInfo.FileName = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\StoryBuilder\\Help\\StoryBuilder.chm";
            ShowHelp.StartInfo.FileName = ApplicationData.Current.RoamingFolder.Path.ToString() + "\\StoryBuilder\\Help\\StoryBuilder.chm";
            ShowHelp.Start(); //This actually launches the help program
        }
    }
}
