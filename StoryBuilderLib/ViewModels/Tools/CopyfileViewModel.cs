using CommunityToolkit.Mvvm.ComponentModel;
using StoryBuilder.Models;
using System.Collections.ObjectModel;

namespace StoryBuilder.ViewModels.Tools
{
    public class CopyfileViewModel : ObservableRecipient
    {
        #region Fields

        private bool _changed;

        private string _title;

        private ObservableCollection<SettingModel> _settings;
        private ObservableCollection<CharacterModel> _characters;

        #endregion

        #region Properties

        public bool Changed
        {
            get { return _changed; }
            // ReSharper disable once ValueParameterNotUsed
            set { _changed = false; }
        }

        #endregion

        public ObservableCollection<CharacterModel> Characters
        {
            get => _characters;
            set => SetProperty(ref _characters, value);
        }
    }
}
