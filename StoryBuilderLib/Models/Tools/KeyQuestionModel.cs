using CommunityToolkit.Mvvm.ComponentModel;

namespace StoryBuilder.Models.Tools
{
    public class KeyQuestionModel : ObservableObject
    {
        #region Properties

        public string Key { get; set; }
        public string Element { get; set; }
        public string Topic { get; set; }
        public string Question { get; set; }

        #endregion

        #region Constructor

        public KeyQuestionModel()
        {
            Question = string.Empty;
        }

        #endregion
    }
}
