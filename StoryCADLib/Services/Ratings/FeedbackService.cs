using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Services.Store.Engagement;
namespace StoryCAD.Services.Ratings;

public class FeedbackService
{
    /// <summary>
    /// Calling this function will open the Feedback hub.
    /// </summary>
    public void OpenFeedbackHub()
    {
        if (Microsoft.Services.Store.Engagement.StoreServicesFeedbackLauncher.IsSupported())
        { 
        }
}
}
