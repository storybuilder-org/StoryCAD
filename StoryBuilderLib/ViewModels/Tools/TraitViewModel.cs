using CommunityToolkit.Mvvm.ComponentModel;
using StoryBuilder.Models;
using StoryBuilder.Models.Tools;
using System.Collections.Generic;
using System.Collections.ObjectModel;


namespace StoryBuilder.ViewModels.Tools
{
    public class TraitViewModel: ObservableRecipient
    {
        #region Fields
        #endregion

        #region Properties

        private string _behavior;
        public string Behavior
        {
            get => _behavior;
            set => SetProperty(ref _behavior, value);
        }

        private string _habit;
        public string Habit
        {
            get => _habit;
            set => SetProperty(ref _habit, value);
        }

        private string _skill;
        public string Skill
        {
            get => _skill;
            set => SetProperty(ref _skill, value);
        }

        #endregion

        #region ComboBox sources

        public ObservableCollection<string> BehaviorList;
        public ObservableCollection<string> HabitList;
        public ObservableCollection<string> SkillList;

        #endregion

        #region Constructor

        public TraitViewModel()
        {
            Dictionary<string, ObservableCollection<string>> lists = GlobalData.ListControlSource;

            BehaviorList = lists["Behavior"];
            HabitList = lists["Habit"];
            SkillList = lists["Skill"];
        }

        #endregion
    }
}
