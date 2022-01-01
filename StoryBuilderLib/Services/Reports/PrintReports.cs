using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StoryBuilder.Models;  

namespace StoryBuilder.Services.Reports
{
    public class PrintReports
    {
        private StoryModel _model; 
        public void Initialize() 
        { 
        }

        public PrintReports(StoryModel model) 
        {
            _model = model;
        }
    }
}
