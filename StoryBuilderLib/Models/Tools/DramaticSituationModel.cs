namespace StoryBuilder.Models.Tools
{
    public class DramaticSituationModel
    {
        #region Properties

        public string SituationName { get; set; }
        public string Role1 { get; set; }
        public string Role2 { get; set; }
        public string Role3 { get; set; }
        public string Role4 { get; set; }
        public string Description1 { get; set; }
        public string Description2 { get; set; }
        public string Description3 { get; set; }
        public string Description4 { get; set; }
        public string Notes { get; set; }

        #endregion

        #region Constructor

        public DramaticSituationModel(string situationName)
        {
            SituationName = situationName;
            Role1 = string.Empty;
            Role2 = string.Empty;
            Role3 = string.Empty;
            Role4 = string.Empty;
            Description1 = string.Empty;
            Description2 = string.Empty;
            Description3 = string.Empty;
            Description4 = string.Empty;
            Notes = string.Empty;
        }

        #endregion
    }
}
