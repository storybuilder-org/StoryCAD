using StoryBuilder.Models;
using StoryBuilder.Models.Tools;
using StoryBuilder.ViewModels.Tools;
using Windows.Storage;

namespace StoryBuilder.Controllers
{
    //TODO: Chang description to describe its role as Application Automation
    /// <summary>
    /// Copyright 2019 Seven Valleys Software
    /// All Rights Reserved
    ///
    /// The StoryController class mediates between StoryModel and the various
    /// ViewModels.
    /// </summary>
       /* Many of this class's methods (Add, Update, Show methods, etc) has been moved
       to the ViewModels. Others of this have been moved to the ShellViewModel.
       The mediation between the StoryModel and viewmodels mentioned above should be
       eliminated. What's left is some miscellaneous stuff. If there's a need,
       keep what's left as a service. Otherwise, eliminate.
     */
    public class StoryController
    {
        #region public Fields and Properties

        // The currently open and active StoryModel instance. Null if no 
        // StoryModel is open.
        public StoryModel StoryModel;

        // File references. These are used to coordinate LoadModel()
        // and SaveModel() behavior in Story Element ViewModels and 
        // Open, Save, and SaveAs behavior in the ShellViewModel.
        // File and folder data
        //TODO: Move into StoryModel?
        public StorageFolder ProjectFolder;
        public StorageFolder FilesFolder;
        public StorageFile ProjectFile;
        public string ProjectFilename;
        public string ProjectPath;

        // LoadStatus decides where LoadModel() and SaveModel() get
        // and put RTF text data.
        public LoadStatus LoadStatus;

        #endregion
 
        #region Constructor

        public StoryController()
        {
        }

        #endregion

    }
}
