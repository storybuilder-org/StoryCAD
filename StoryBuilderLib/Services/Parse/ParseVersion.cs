using Parse;

namespace StoryBuilder.Services.Parse
{ 
    [ParseClassName("Version")]
    public class ParseVersion : ParseObject
    {
        [ParseFieldName("email")]
        public string Email 
        { get { return GetProperty<string>(); } 
          set { SetProperty(value); }
        }

        [ParseFieldName("name")]
        public string Name 
        { 
            get { return GetProperty<string>(); }  
            set { SetProperty(value); } 
        }

        [ParseFieldName("currentversion")]
        public string CurrentVersion 
        { 
            get { return GetProperty<string>(); }  
            set { SetProperty(value); }
        }

        [ParseFieldName("previousversion")]
        public string PreviousVersion 
        { 
            get { return GetProperty<string>(); }  
            set { SetProperty(value); }
        }

        [ParseFieldName("RunDate")]
        public string RunDate 
        { 
            get { return GetProperty<string>(); } 
            set { SetProperty(value); } 
        }
    }
}
