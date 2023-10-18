using System.Collections.Generic;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.DAL;
using StoryCAD.Models;
using System.Linq;
using StoryCAD.Models.Tools;
using StoryCAD.Services.Logging;
using System.Threading.Tasks;
using System;
using Microsoft.UI.Xaml;

namespace StoryCAD.ViewModels
{
      

    /// <summary>
    /// This contains controls data
    /// previously stored in GlobalData.cs
    /// </summary>
    public class ControlData
    {
        LogService _log = Ioc.Default.GetService<LogService>();

        //Character conflics
        public SortedDictionary<string, ConflictCategoryModel> ConflictTypes;
        
        /// <summary>
        /// Possible relations
        /// </summary>
        public List<string> RelationTypes;

        public ControlData()
        {
            int subTypeCount = 0;
            int exampleCount = 0;
            try
            {
                _log.Log(LogLevel.Info, "Loading Controls.ini data");
                ControlLoader loader = Ioc.Default.GetService<ControlLoader>();
                Task.Run(async () => 
                {
                    List<Object> Controls = await loader.Init();
                    ConflictTypes = (SortedDictionary<string, ConflictCategoryModel>)Controls[0];
                    RelationTypes = (List<string>)Controls[1];
                }).Wait();

                _log.Log(LogLevel.Info, "ConflictType Counts");
                _log.Log(LogLevel.Info,
                    $"{ConflictTypes.Keys.Count} ConflictType keys created");
                foreach (ConflictCategoryModel type in ConflictTypes.Values)
                {
                    subTypeCount += type.SubCategories.Count;
                    exampleCount += type.SubCategories.Sum(subType => type.Examples[subType].Count);
                }
                _log.Log(LogLevel.Info,
                    $"{subTypeCount} Total ConflictSubType keys created");
                _log.Log(LogLevel.Info,
                    $"{exampleCount} Total ConflictSubType keys created");
            }
            catch (Exception ex)
            {
                _log.LogException(LogLevel.Error, ex, "Error loading Controls.ini");
                Application.Current.Exit();
            }
        }
    }
}
