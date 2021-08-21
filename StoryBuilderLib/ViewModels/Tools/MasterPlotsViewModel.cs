using CommunityToolkit.Mvvm.ComponentModel;
using StoryBuilder.Models;
using StoryBuilder.Models.Tools;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StoryBuilder.ViewModels.Tools
{
    public class MasterPlotsViewModel : ObservableRecipient
    {
        #region Properties

        private string _masterPlotName;
        public string MasterPlotName
        {
            get => _masterPlotName;
            set
            {
                SetProperty(ref _masterPlotName, value);
                if (MasterPlots.ContainsKey(value))
                    MasterPlotNotes = MasterPlots[value].MasterPlotNotes;
            }
        }

        private string _masterPlotNotes;
        public string MasterPlotNotes
        {
            get => _masterPlotNotes;
            set => SetProperty(ref _masterPlotNotes, value);
        }

        private IList<MasterPlotScene> _masterPlotScenes;
        public IList<MasterPlotScene> MasterPlotScenes
        {
            get => _masterPlotScenes;
            set => SetProperty(ref _masterPlotScenes, value);
        }

        #endregion

        #region ComboBox and ListBox sources

        public readonly ObservableCollection<string> MasterPlotNames;

        public readonly Dictionary<string, MasterPlotModel> MasterPlots;

        #endregion

        #region Constructor

        public MasterPlotsViewModel()
        {
            MasterPlotNames = new ObservableCollection<string>();
            MasterPlots = new Dictionary<string, MasterPlotModel>();
            foreach (MasterPlotModel plot in GlobalData.MasterPlotsSource)
            {
                MasterPlotNames.Add(plot.MasterPlotName);
                MasterPlots.Add(plot.MasterPlotName, plot);
            }
            MasterPlotName = GlobalData.MasterPlotsSource[0].MasterPlotName;
        }

        #endregion
    }
}
