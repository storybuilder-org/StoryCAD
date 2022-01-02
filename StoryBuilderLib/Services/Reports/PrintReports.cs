using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;        
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
        private StringReader fileStream;
        private Font printFont;
        private string documentText;

        //PrintHelper helper = new();

        public async Task Generate()
        {
            string rtf = string.Empty;
            await _formatter.LoadReportTemplates(); // Load text report templates
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
                    documentText = FormatText(rtf);
                    
                    Print(documentText);
                }
            }
        }

        // The PrintPage event is raised for each page to be printed.
        private void pd_PrintPage(object sender, PrintPageEventArgs ev)
        {
            float linesPerPage = 0;
            float yPos = 0;
            int count = 0;
            float leftMargin = ev.MarginBounds.Left;
            float topMargin = ev.MarginBounds.Top;
            String line = null;

            // Calculate the number of lines per page.
            linesPerPage = ev.MarginBounds.Height /
               printFont.GetHeight(ev.Graphics);

            // Iterate over the file, printing each line.
            while (count < linesPerPage &&
               ((line = fileStream.ReadLine()) != null))
            {
                yPos = topMargin + (count * printFont.GetHeight(ev.Graphics));
                ev.Graphics.DrawString(line, printFont, Brushes.Black,
                   leftMargin, yPos, new StringFormat());
                count++;
            }

            // If more lines exist, print another page.
            if (line != null)
                ev.HasMorePages = true;
            else
                ev.HasMorePages = false;
        }

        // Print the file.
        public void Print(string file)
        {

            try
            {
                fileStream = new StringReader(file);
                try
                {
                    printFont = new Font("Arial", 12, FontStyle.Regular, GraphicsUnit.Pixel);
                    PrintDocument pd = new PrintDocument();
                    pd.PrintPage += new PrintPageEventHandler(pd_PrintPage);
                    Margins margins = new Margins(100, 100, 100, 100);
                    pd.DefaultPageSettings.Margins = margins;
                    float pixelsPerChar = printFont.Size;
                    float lineWidth = pd.DefaultPageSettings.PrintableArea.Width;
                    int charsPerLine = Convert.ToInt32(lineWidth / pixelsPerChar);
                    // Print the document.
                    pd.Print();
                }
                catch (Exception ex)
                {
                    //MessageBox.Show(ex.Message);
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
            }
        }

        private string FormatText(string rtfInput) 
        {
            string text = _formatter.GetText(rtfInput, false);
            string[] lines = text.Split('\n');
            StringBuilder sb = new StringBuilder();
            for (int i=0; i<lines.Length; i++)
            {
                string line = lines[i];
                while (line.Length > 72) 
                {
                    string temp = line.Substring(0, 72);
                    int j = temp.LastIndexOf(' ');
                    temp = temp.Substring(0, j);
                    sb.Append(temp);
                    sb.Append('\n');
                    line = line.Substring(j);
                    line = line.TrimStart();
                }
                sb.Append(line.Trim());
                sb.Append('\n');
            }
            return sb.ToString();
        }

        public PrintReports(PrintReportDialogVM vm, StoryModel model)
        {
            _vm = vm;
            _model = model;
            _formatter = new ReportFormatter();
        }
    }
}
