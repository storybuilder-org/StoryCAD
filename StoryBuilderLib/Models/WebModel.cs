using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.ViewModels;

namespace StoryBuilder.Models;

public class WebModel : StoryElement
{
    
    public WebModel(StoryModel model) : base("New Webpage", StoryItemType.Web, model)
    {
                
    }
}