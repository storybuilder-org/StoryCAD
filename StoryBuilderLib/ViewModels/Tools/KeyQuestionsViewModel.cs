using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using StoryBuilder.Models;
using StoryBuilder.Models.Tools;

namespace StoryBuilder.ViewModels.Tools;

public class KeyQuestionsViewModel : ObservableRecipient
{
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
            _questions = GlobalData.KeyQuestionsSource[_storyElementName];
            _index = _questions.Count - 1;
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

    #region ComboBox and ListBox sources
    public readonly ObservableCollection<string> KeyQuestionElements;
    #endregion

    #region Public Methods
    public void NextQuestion()
    {
        _index++;
        if (_index >= _questions.Count)
            _index = 0;
        _questionModel = _questions[_index];
        Topic = _questionModel.Topic;
        Question = _questionModel.Question;
    }

    public void PreviousQuestion()
    {
        _index--;
        if (_index < 0)
            _index = _questions.Count - 1;
        _questionModel = _questions[_index];
        Topic = _questionModel.Topic;
        Question = _questionModel.Question;
    }

    #endregion

    #region Constructor
    public KeyQuestionsViewModel()
    {
        KeyQuestionElements = new ObservableCollection<string>();

        foreach (string element in GlobalData.KeyQuestionsSource.Keys)
            KeyQuestionElements.Add(element);
        StoryElementName = KeyQuestionElements[0];
    }
    #endregion
}