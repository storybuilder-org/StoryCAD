using CommunityToolkit.Mvvm.ComponentModel;

namespace StoryBuilder.ViewModels
{
    public class SaveAsViewModel : ObservableRecipient
    {
        #region Fields
        #endregion

        #region Properties

        private string _projectName;
        public string ProjectName
        {
            get => _projectName;
            set => SetProperty(ref _projectName, value);
        }

        private string _projectPathName;
        public string ProjectPathName
        {
            get => _projectPathName;
            set => SetProperty(ref _projectPathName, value);
        }

        #endregion

        #region Public Methods
        #endregion

        #region Constructor
        public SaveAsViewModel()
        {
        }
        #endregion
    }
}
