using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StoryCAD.ViewModels.Tools;

public class TraitsViewModel: ObservableRecipient
{
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
                foreach (string _item in BehaviorList) { ExampleList.Add("(Behavior): " + _item); }
                break;
            case "Habits":
                foreach (string _item in HabitList) { ExampleList.Add("(Habit): " + _item); }
                break;
            case "Skills and Abilities":
                foreach (string _item in SkillList) { ExampleList.Add("(Skill): " + _item); }
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
        Dictionary<string, ObservableCollection<string>> _lists = Ioc.Default.GetService<ListData>().ListControlSource;
        CategoryList = new ObservableCollection<string> { "Behaviors", "Habits", "Skills and Abilities" };
        ExampleList = new ObservableCollection<string>();

        BehaviorList = _lists["Behavior"];
        HabitList = _lists["Habit"];
        SkillList = _lists["Skill"];
    }

    #endregion
}