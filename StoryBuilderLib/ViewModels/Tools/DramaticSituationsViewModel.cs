using CommunityToolkit.Mvvm.ComponentModel;
using StoryBuilder.Models;
using StoryBuilder.Models.Tools;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StoryBuilder.ViewModels.Tools
{
    public class DramaticSituationsViewModel : ObservableRecipient
    {
        #region Fields

        private bool _changed;
        private string _description1;
        private string _description2;
        private string _description3;
        private string _description4;

        #endregion

        #region Properties
        public bool Changed
        {
            get => _changed;
            // ReSharper disable once ValueParameterNotUsed
            set => _changed = false;
        }

        private DramaticSituationModel _situation;

        public DramaticSituationModel Situation
        {
            get => _situation;
            set => SetProperty(ref _situation, value);
        }

        private string _situationName;
        public string SituationName
        {
            get => _situationName;
            set
            {
                SetProperty(ref _situationName, value);
                Situation = situations[value];
            }
        }

        private string _role1;
        public string Role1
        {
            get => _role1;
            set => SetProperty(ref _role1, value);
        }

        private string _role2;
        public string Role2
        {
            get => _role2;
            set => SetProperty(ref _role2, value);
        }

        private string _role3;
        public string Role3
        {
            get => _role3;
            set => SetProperty(ref _role3, value);
        }

        private string _role4;
        public string Role4
        {
            get => _role4;
            set => SetProperty(ref _role4, value);
        }

        public string Description1
        {
            get => _description1;
            set => SetProperty(ref _description1, value);
        }

        public string Description2
        {
            get => _description2;
            set => SetProperty(ref _description2, value);
        }

        public string Description3
        {
            get => _description3;
            set => SetProperty(ref _description3, value);
        }

        public string Description4
        {
            get => _description4;
            set => SetProperty(ref _description4, value);
        }

        private string _notes;
        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        #endregion

        #region Combobox and ListBox sources

        public ObservableCollection<string> SituationsSource;

        private SortedDictionary<string, DramaticSituationModel> situations;

        #endregion

        #region Constructor

        public DramaticSituationsViewModel()
        {
            situations = GlobalData.DramaticSituationsSource;

            SituationsSource = new ObservableCollection<string>();
            foreach (string situation in situations.Keys)
                SituationsSource.Add(situation);
        }
        #endregion
    }
}
