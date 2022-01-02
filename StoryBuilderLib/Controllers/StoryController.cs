using StoryBuilder.Models;
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
       /* Much of this method (Add, Update, Show methods, etc) has been moved
       to the ViewModels. Others of this have been moved to the ShellViewModel.
       The mediation between the StoryModel and ViewModels mentioned above should be
       eliminated. What's left is some miscellaneous stuff. If there's a need,
       keep what's left as a service. Otherwise, eliminate.
     */
    public class StoryController
    {
        #region public Fields and Properties

        // The currently open and active StoryModel instance. Null if no 
        // StoryModel is open.
        public StoryModel StoryModel;

        // LoadStatus decides where LoadModel() and SaveModel() get
        // and put RTF text data.
        public LoadStatus LoadStatus;

        #endregion
 
        #region Constructor

        #endregion

    }
}
