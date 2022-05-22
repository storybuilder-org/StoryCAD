using Parse;

namespace StoryBuilder.Services.Parse
{
    [ParseClassName("Preferences")]
    public class ParsePreferences : ParseObject
    {
        //TODO: Tie this to ParseUser

        [ParseFieldName("email")]
        public string Email 
        { 
            get { return GetProperty<string>(); } 
            set { SetProperty(value); } 
        }

        [ParseFieldName("name")]
        public string Name 
        { 
            get { return GetProperty<string>(); } 
            set { SetProperty(value); } 
        }

        [ParseFieldName("elmahconsent")]
        public bool ErrorCollectionConsent 
        { 
            get { return GetProperty<bool>(); } 
            set { SetProperty(value); } 
        }

        [ParseFieldName("newsletterconsent")]
        public bool Newsletter 
        { 
            get { return GetProperty<bool>(); } 
            set { SetProperty(value); } 
        }

        [ParseFieldName("version")]
        public string Version 
        { 
            get { return GetProperty<string>(); } 
            set { SetProperty(value); } 
        }

        [ParseFieldName("updatedate")]
        public string UpdateDate 
        { 
            get { return GetProperty<string>(); } 
            set { SetProperty(value); } 
        }
    }
}
