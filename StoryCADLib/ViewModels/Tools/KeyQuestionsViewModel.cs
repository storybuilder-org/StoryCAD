using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using StoryCADLib.Models.Tools;

namespace StoryCADLib.ViewModels.Tools;

public class KeyQuestionsViewModel : ObservableRecipient
{
    #region ComboBox and ListBox sources

    public readonly ObservableCollection<string> KeyQuestionElements;

    #endregion

    #region Fields

    private List<KeyQuestionModel> _questions;
    private KeyQuestionModel _questionModel;
    private int _index;

    #endregion

    #region Properties

    private string _storyElementName;

    public string StoryElementName
    {
        get => _storyElementName;
        set
        {
            SetProperty(ref _storyElementName, value);
            _questions = _toolsData.KeyQuestionsSource[_storyElementName];
            _index = _questions.Count - 1;
            NextQuestion();
        }
    }

    private string _topic;

    public string Topic
    {
        get => _topic;
        set => SetProperty(ref _topic, value);
    }

    private string _question;

    public string Question
    {
        get => _question;
        set => SetProperty(ref _question, value);
    }

    #endregion

    #region Public Methods

    public void NextQuestion()
    {
        _index++;
        if (_index >= _questions.Count)
        {
            _index = 0;
        }

        _questionModel = _questions[_index];
        Topic = _questionModel.Topic;
        Question = _questionModel.Question;
    }

    public void PreviousQuestion()
    {
        _index--;
        if (_index < 0)
        {
            _index = _questions.Count - 1;
        }

        _questionModel = _questions[_index];
        Topic = _questionModel.Topic;
        Question = _questionModel.Question;
    }

    #endregion

    #region Constructor

    private readonly ToolsData _toolsData;

    // Constructor for XAML compatibility - will be removed later
    public KeyQuestionsViewModel() : this(Ioc.Default.GetRequiredService<ToolsData>())
    {
    }

    public KeyQuestionsViewModel(ToolsData toolsData)
    {
        _toolsData = toolsData;
        KeyQuestionElements = new ObservableCollection<string>();

        foreach (var _element in _toolsData.KeyQuestionsSource.Keys)
        {
            KeyQuestionElements.Add(_element);
        }

        StoryElementName = KeyQuestionElements[0];
    }

    #endregion
}
