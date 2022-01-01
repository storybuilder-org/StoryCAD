using CommunityToolkit.Mvvm.ComponentModel;
using StoryBuilder.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Controls;


namespace StoryBuilder.ViewModels.Tools
{
    public class TraitsViewModel: ObservableRecipient
    {
        #region Fields
        #endregion

        #region Properties

        private string _category;
        public string Category
        {
            get => _category;
            set => SetProperty(ref _category, value);
        }

        private string _example;
        public string Example   
        {
            get => _example;
            set => SetProperty(ref _example, value);
        }

        #endregion

        #region Public Methods
        public void ViewChanged(object sender, SelectionChangedEventArgs args)
        {
            ExampleList.Clear();
            switch (Category)
            {
                case "Behaviors":
                    foreach (string item in BehaviorList)
                        ExampleList.Add("(Behavior): " + item);
                    break;
                case "Habits":
                    foreach (string item in HabitList)
                        ExampleList.Add("(Habit): " + item);
                    break;
                case "Skills and Abilities":
                    foreach (string item in SkillList)
                        ExampleList.Add("(Skill): " + item);
                    break;
            }
        }

        #endregion


        #region ComboBox sources

         public ObservableCollection<string> CategoryList;
        public ObservableCollection<string> ExampleList;

        public ObservableCollection<string> BehaviorList;
        public ObservableCollection<string> HabitList;
        public ObservableCollection<string> SkillList;

        #endregion

        #region Constructor

        public TraitsViewModel()
        {
            Dictionary<string, ObservableCollection<string>> lists = GlobalData.ListControlSource;
            CategoryList = new ObservableCollection<string>();
            CategoryList.Add("Behaviors");
            CategoryList.Add("Habits");
            CategoryList.Add("Skills and Abilities");
            ExampleList = new ObservableCollection<string>();

            BehaviorList = lists["Behavior"];
            HabitList = lists["Habit"];
            SkillList = lists["Skill"];
        }

        #endregion
    }
}
