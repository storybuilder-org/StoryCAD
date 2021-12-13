using Windows.Data.Xml.Dom;

namespace StoryBuilder.Models
{
    /// <summary>
    /// OverviewModel contains overview information for the entire story, such as title, author, and so on.
    /// It's a good place to capture the original idea which prompted the story.
    ///
    /// There is only one OverviewModel instance for each story. It's also the root of the Shell Page's
    /// StoryExplorer TreeView.
    /// </summary>
    public class OverviewModel : StoryElement
    {
        /* Handing date fields and author:
         * System.DateTime wrkDate = DateTime.FromOADate(0);
           wrkDate = DateTime.Parse(DateTime.Parse(frmStory.DefInstance.mskDateCreated.Text).ToString("MM/dd/yy"));
         * StoryRec.DateCreated.Value = wrkDate.ToString("MM-dd-yy");
         */

        #region Properties

        // Because Overview is always the root of the StoryExplorer, there is only one instance of it.
        // Consequently it has no Id property (although it does have a UUID.)

        // Overview data

        private string _dateCreated;
        public string DateCreated
        {
            get => _dateCreated;
            set => _dateCreated = value;
        }

        private string _author;
        public string Author
        {
            get => _author;
            set => _author = value;

        }

        private string _dateModified;
        public string DateModified
        {
            get => _dateModified;
            set => _dateModified = value;
        }

        private string _storyIdea;
        public string StoryIdea
        {
            get => _storyIdea;
            set => _storyIdea = value;
        }


        private string _concept;
        public string Concept
        {
            get => _concept;
            set => _concept = value;
        }

        // Premise data

        private string _storyProblem;  // The Guid of a Problem StoryElement
        public string StoryProblem
        {
            get => _storyProblem;
            set => _storyProblem = value;
        }

        // Note: the Premise is a read-only view of the story Problem Premise
        //       and is not persisted in the model or on disk. Instead, when
        //       this model is to the ViewModel, StoryProblem's Premise is
        //       copied as read-only text to the OverViewViewModel.

        // Structure data

        private string _storyType;
        public string StoryType
        {
            get => _storyType;
            set => _storyType = value;
        }

        private string _storyGenre;
        public string StoryGenre
        {
            get => _storyGenre;
            set => _storyGenre = value;
        }

        private string _viewpoint;
        public string Viewpoint
        {
            get => _viewpoint;
            set => _viewpoint = value;
        }

        private string _viewpointCharacter;
        public string ViewpointCharacter
        {
            get => _viewpointCharacter;
            set => _viewpointCharacter = value;
        }

        private string _voice;
        public string Voice
        {
            get => _voice;
            set => _voice = value;
        }

        private string _literaryDevice;
        public string LiteraryDevice
        {
            get => _literaryDevice;
            set => _literaryDevice = value;
        }

        private string _tense;
        public string Tense
        {
            get => _tense;
            set => _tense = value;
        }

        private string _style;
        public string Style
        {
            get => _style;
            set => _style = value;
        }

        private string _styleNotes;
        public string StyleNotes
        {
            get => _styleNotes;
            set => _styleNotes = value;
        }
        private string _tone;
        public string Tone
        {
            get => _tone;
            set => _tone = value;
        }

        private string _toneNotes;
        public string ToneNotes
        {
            get => _toneNotes;
            set => _toneNotes = value;
        }

        // Notes data

        private string _notes;
        public string Notes
        {
            get => _notes;
            set => _notes = value;
        }



        #endregion

        #region Constructor
        public OverviewModel(StoryModel model) : base("Story Overview", StoryItemType.StoryOverview, model)
        {
            DateCreated = string.Empty;
            Author = string.Empty;
            DateModified = string.Empty;
            StoryType = string.Empty;
            StoryGenre = string.Empty;
            Viewpoint = string.Empty;
            ViewpointCharacter = string.Empty;
            Voice = string.Empty;
            LiteraryDevice = string.Empty;
            Tense = string.Empty;
            Style = string.Empty;
            Tone = string.Empty;
            StoryIdea = string.Empty;
            Concept = string.Empty;
            StyleNotes = string.Empty;
            ToneNotes = string.Empty;
            Notes = string.Empty;
            StoryProblem = string.Empty;

            // TODO: Set good defaults for these
            //System.DateTime wrkDate = DateTime.FromOADate(0);
            //wrkDate = DateTime.Parse(Convert.ToDateTime(StoryRec.DateCreated.Value).ToString("MM/dd/yy"));
            //frmStory.DefInstance.mskDateCreated.Text = StringsHelper.Format(wrkDate, "Medium Date");
        }
        public OverviewModel(string name, StoryModel model) : base(name, StoryItemType.StoryOverview, model)
        {
            DateCreated = string.Empty;
            Author = string.Empty;
            DateModified = string.Empty;
            StoryType = string.Empty;
            StoryGenre = string.Empty;
            Viewpoint = string.Empty;
            ViewpointCharacter = string.Empty;
            Voice = string.Empty;
            LiteraryDevice = string.Empty;
            Tense = string.Empty;
            Style = string.Empty;
            Tone = string.Empty;
            StoryIdea = string.Empty;
            Concept = string.Empty;
            StyleNotes = string.Empty;
            ToneNotes = string.Empty;
            Notes = string.Empty;
            StoryProblem = string.Empty;

            // TODO: Set good defaults for these
            //System.DateTime wrkDate = DateTime.FromOADate(0);
            //wrkDate = DateTime.Parse(Convert.ToDateTime(StoryRec.DateCreated.Value).ToString("MM/dd/yy"));
            //frmStory.DefInstance.mskDateCreated.Text = StringsHelper.Format(wrkDate, "Medium Date");
        }
        public OverviewModel(IXmlNode xn, StoryModel model) : base(xn, model)
        {
            DateCreated = string.Empty;
            Author = string.Empty;
            DateModified = string.Empty;
            StoryType = string.Empty;
            StoryGenre = string.Empty;
            Viewpoint = string.Empty;
            ViewpointCharacter = string.Empty;
            Voice = string.Empty;
            LiteraryDevice = string.Empty;
            Style = string.Empty;
            Tense = string.Empty;
            Style = string.Empty;
            Tone = string.Empty;
            StoryIdea = string.Empty;
            Concept = string.Empty;
            StyleNotes = string.Empty;
            ToneNotes = string.Empty;
            Notes = string.Empty;
            StoryProblem = string.Empty;

            // TODO: Set good defaults for these
            //System.DateTime wrkDate = DateTime.FromOADate(0);
            //wrkDate = DateTime.Parse(Convert.ToDateTime(StoryRec.DateCreated.Value).ToString("MM/dd/yy"));
            //frmStory.DefInstance.mskDateCreated.Text = StringsHelper.Format(wrkDate, "Medium Date");
        }

        #endregion
    }
}
