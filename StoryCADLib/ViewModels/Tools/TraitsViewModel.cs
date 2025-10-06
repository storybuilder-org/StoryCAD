using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StoryCAD.ViewModels.Tools;

public class TraitsViewModel : ObservableRecipient
{
    #region Public Methods

    public void ViewChanged(object sender, SelectionChangedEventArgs args)
    {
        ExampleList.Clear();
        switch (Category)
        {
            case "Behaviors":
                foreach (var _item in BehaviorList)
                {
                    ExampleList.Add("(Behavior): " + _item);
                }

                break;
            case "Habits":
                foreach (var _item in HabitList)
                {
                    ExampleList.Add("(Habit): " + _item);
                }

                break;
            case "Skills and Abilities":
                foreach (var _item in SkillList)
                {
                    ExampleList.Add("(Skill): " + _item);
                }

                break;
        }
    }

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


    #region ComboBox sources

    public ObservableCollection<string> CategoryList;
    public ObservableCollection<string> ExampleList;

    public ObservableCollection<string> BehaviorList;
    public ObservableCollection<string> HabitList;
    public ObservableCollection<string> SkillList;

    #endregion

    #region Constructor

    private readonly ListData _listData;

    // Constructor for XAML compatibility - will be removed later
    public TraitsViewModel() : this(Ioc.Default.GetRequiredService<ListData>())
    {
    }

    public TraitsViewModel(ListData listData)
    {
        _listData = listData;
        var _lists = _listData.ListControlSource;
        CategoryList = new ObservableCollection<string> { "Behaviors", "Habits", "Skills and Abilities" };
        ExampleList = new ObservableCollection<string>();

        BehaviorList = _lists["Behavior"];
        HabitList = _lists["Habit"];
        SkillList = _lists["Skill"];
    }

    #endregion
}
