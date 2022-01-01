using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StoryBuilder.Models;
using StoryBuilder.ViewModels;
using StoryBuilder.ViewModels.Tools;

namespace StoryBuilder.Services.Reports
{
    public class PrintReports
    {
        private PrintReportDialogVM _vm;
        private StoryModel _model;
        private ReportFormatter _formatter;
        //PrintHelper helper = new();

        public async Task Generate()
        {
            string rtf;

            if (_vm.CreateOverview)
            {
                //rtf = await _formatter.FormatStoryOverviewReport();
            }

            foreach (StoryNodeItem node in _vm.SelectedNodes)
            {
                StoryElement element = null;
                if (_model.StoryElements.StoryElementGuids.ContainsKey(node.Uuid))
                    element = _model.StoryElements.StoryElementGuids[node.Uuid];
                if (element != null)
                {
                    switch (node.Type)
                    {
                        case StoryItemType.Problem:
                            rtf = await _formatter.FormatProblemReport(element);
                            break;
                        case StoryItemType.Character:
                            rtf = await _formatter.FormatCharacterReport(element);
                            break;
                    }
                }
            }
        }

        public PrintReports(PrintReportDialogVM vm, StoryModel model)
        {
            _vm = vm;
            _model = model;
            _formatter = new ReportFormatter();
        }
    }
}
