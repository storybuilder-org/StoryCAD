using CommunityToolkit.Mvvm.ComponentModel;

namespace StoryBuilder.ViewModels
{    
    /// <summary>
    /// this is a VM for the MainWindow, its pretty useless
    /// as its only purpose is to change the window title
    /// it was the only way I could think of to change it quickly.
    /// </summary>
    public class MainWindowVM : ObservableRecipient
    {
        private string _title;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }
    }
}

