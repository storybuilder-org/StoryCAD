using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using StoryBuilder.Models;
using StoryBuilder.Models.Tools;

namespace StoryBuilder.ViewModels.Tools;

public class TopicsViewModel : ObservableRecipient
{
    #region Fields

    private TopicModel _topic;
    private int _index;

    #endregion

    #region Properties

    private string _topicName;
    public string TopicName
    {
        get => _topicName;
        set
        {
            SetProperty(ref _topicName, value);
            LoadTopic(_topicName);
        }
    }

    private string _subTopicName;
    public string SubTopicName
    {
        get => _subTopicName;
        set => SetProperty(ref _subTopicName, value);
    }

    private string _subTopicNote;
    public string SubTopicNote
    {
        get => _subTopicNote;
        set => SetProperty(ref _subTopicNote, value);
    }

    #endregion

    #region ComboBox and ListBox sources
    public readonly ObservableCollection<string> TopicNames;
    public ObservableCollection<string> SubTopicNames;
    public ObservableCollection<string> SubTopicNotes;
    #endregion

    #region Public Methods
    public void LoadTopic(string topicName)
    {
        if (topicName == null || topicName.Equals(string.Empty)) { return; } //Can't load topics that are null or empty.

        _topic = GlobalData.TopicsSource[TopicName];
        switch (_topic.TopicType)
        {
            case TopicTypeEnum.Notepad:
                Process.Start("notepad.exe", _topic.Filename);
                break;
            case TopicTypeEnum.Inline:
                SubTopicNames.Clear();
                SubTopicNotes.Clear();
                foreach (SubTopicModel _model in _topic.SubTopics)
                {
                    SubTopicNames.Add(_model.SubTopicName);
                    SubTopicNotes.Add(_model.SubTopicNotes);
                }
                _index = 0;
                SubTopicName = SubTopicNames[0];
                SubTopicNote = SubTopicNotes[0];
                break;
        }
    }

    public void NextSubTopic()
    {
        _index++;
        if (_index >= SubTopicNames.Count) { _index = 0; }
        SubTopicName = SubTopicNames[_index];
        SubTopicNote = SubTopicNotes[_index];
    }

    public void PreviousSubTopic()
    {
        _index--;
        if (_index < 0) { _index = SubTopicNames.Count - 1; }
        SubTopicName = SubTopicNames[_index];
        SubTopicNote = SubTopicNotes[_index];
    }

    #endregion

    #region Constructor
    public TopicsViewModel()
    {
        TopicNames = new ObservableCollection<string>(GlobalData.TopicsSource.Keys);
        SubTopicNames = new ObservableCollection<string>();
        SubTopicNotes = new ObservableCollection<string>();

        TopicName = TopicNames[0];
        _topic = GlobalData.TopicsSource[TopicName];
        TopicName = _topic.TopicName;
    }
    #endregion
}