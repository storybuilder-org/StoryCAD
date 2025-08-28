using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using StoryCAD.Models.Tools;

namespace StoryCAD.ViewModels.Tools;

public class TopicsViewModel : ObservableRecipient
{
    private readonly ToolsData _toolsData;

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
    public async void LoadTopic(string topicName)
    {
        if (topicName == null || topicName.Equals(string.Empty)) { return; } //Can't load topics that are null or empty.

        _topic = _toolsData.TopicsSource[TopicName];
        switch (_topic.TopicType)
        {
            case TopicTypeEnum.Notepad:
                if (_topic.Filename.Contains("Symbols.txt") || _topic.Filename.Contains("Bibliog.txt"))
                {
                    //Gets content of Symbols.txt or Bibliog.txt 
                    await using Stream internalResourceStream = Assembly.GetExecutingAssembly()
                        .GetManifestResourceStream("StoryCAD.Assets.Install." + Path.GetFileName(_topic.Filename));
                    using StreamReader reader = new(internalResourceStream);

                    //Writes file to temp.
                    File.WriteAllText(_topic.Filename, await reader.ReadToEndAsync());
                }

                Process.Start(new ProcessStartInfo() { UseShellExecute = true, FileName= _topic.Filename });
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

    // Constructor for XAML compatibility - will be removed later
    public TopicsViewModel() : this(Ioc.Default.GetRequiredService<ToolsData>())
    {
    }

    public TopicsViewModel(ToolsData toolsData)
    {
        _toolsData = toolsData;
        TopicNames = new ObservableCollection<string>(_toolsData.TopicsSource.Keys);
        SubTopicNames = new ObservableCollection<string>();
        SubTopicNotes = new ObservableCollection<string>();

        TopicName = TopicNames[0];
        _topic = _toolsData.TopicsSource[TopicName];
        TopicName = _topic.TopicName;
    }
    #endregion
}