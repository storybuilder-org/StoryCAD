using CommunityToolkit.Mvvm.ComponentModel;

namespace StoryCADLib.Models.Tools;

public class KeyQuestionModel : ObservableObject
{
    #region Constructor

    public KeyQuestionModel()
    {
        Question = string.Empty;
    }

    #endregion

    #region Properties

    public string Key { get; set; }
    public string Element { get; set; }
    public string Topic { get; set; }
    public string Question { get; set; }

    #endregion
}
