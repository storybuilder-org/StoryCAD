using CommunityToolkit.Mvvm.DependencyInjection;
using NRtfTree.Util;
using StoryBuilder.DAL;
using StoryBuilder.Models;
using StoryBuilder.Models.Scrivener;    
using StoryBuilder.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.Storage;

namespace StoryBuilder.Services.Reports
{
    public class ScrivenerReports
    {

        private StoryModel _model;
        private ScrivenerIo _scrivener;
        private StoryReader _rdr;
        private ReportFormatter _formatter;

        private BinderItem _binderNode;         // Binder root node (BinderItems' parent)
        private BinderItem _storyBuilderNode;   //   StoryBuilder folder node
        private BinderItem _explorerNode;       //     StoryExplorer subfolder node
        private BinderItem _narratorNode;       //     StoryNarrator subfolder node
        private BinderItem _miscNode;           //     Miscellaneous subfolder node
        private BinderItem _problemListNode;    //       List of Problems report
        private BinderItem _characterListNode;  //       List of Characters report
        private BinderItem _settingListNode;    //       List of Settings report
        private BinderItem _sceneListNode;      //       List of Scenes report
        private BinderItem _synopsisNode;       //       Concatenated Scene synopses (a poor man's story synopsis)
        private StringBuilder _stbNotes;
        private XmlElement _newStbRoot;
        private List<BinderItem> _draftFolderItems;
        //private List<BinderItem> _narratorViewItems;

        #region Constructor

        public ScrivenerReports(StorageFile file, StoryModel model)
        {
            _scrivener = Ioc.Default.GetService<ScrivenerIo>();
            _scrivener.ScrivenerFile = file;
            _model = model;
            _rdr = Ioc.Default.GetService<StoryReader>();
            _formatter = new ReportFormatter();
            //_root = root;
            //_misc = miscFolder;
        }

        #endregion

        #region public methods

        public async Task GenerateReports()
        {
            await _scrivener.LoadScrivenerProject();  // Load the Scrivener project
            await _formatter.LoadReportTemplates(); // Load text report templates
            //TODO: load templates from within ReportFOrmatter
            _binderNode = _scrivener.BuildBinderItemTree(); // Build a BinderItem model
            UpdateStoryBuilderOutline();  // Replace or add StoryBuilder BinderItems to model

            await RecurseStoryElementReports(_explorerNode);
            await RecurseStoryElementReports(_narratorNode);
            await GenerateProblemListReport(_problemListNode);
            await GenerateCharacterListReport(_characterListNode);
            await GenerateSettingListReport(_settingListNode);
            await GenerateSceneListReport(_sceneListNode);
            await GenerateSynopsisReport(_synopsisNode);
            //await ProcessPreviousNotes();
            // Narrative view processing (into manuscript)

            AddCustomMetaDataSettings();  // Add new metadata tag if needed
            MatchDraftFolderToNarrator();

            SetLabelSettings();     // Add or replace my binder Label settings

            _newStbRoot = _scrivener.CreateFromBinder(_storyBuilderNode);
            await _scrivener.WriteTestFile("newstb.xml", _newStbRoot); // Debugging
            UpdateStoryBuilder();
            await _scrivener.SaveScrivenerProject(_scrivener.ScrivenerFile);
        }

        private void UpdateStoryBuilder()
        {
            if (_scrivener.StoryBuilder != null)
            {
                IXmlNode parent = _scrivener.StoryBuilder.ParentNode;
                parent.ReplaceChild(_newStbRoot, _scrivener.StoryBuilder);
            }
            else
                _scrivener.Binder.InsertBefore(_newStbRoot, _scrivener.Research);
        }

        private void MatchDraftFolderToNarrator()
        {
            _draftFolderItems = ListDraftFolderContents();
            // _narratorViewItems = ListNarratorViewContents();
        }

        private List<BinderItem> ListDraftFolderContents()
        {
            BinderItem draftFolder = null;
            // Find the root DraftFolder BinderItem
            foreach (BinderItem child in _binderNode.Children)
                if (child.Type == BinderItemType.DraftFolder)
                {
                    draftFolder = child;
                    break;
                }
            List<BinderItem> draftFolderItems = new List<BinderItem>();
            foreach (BinderItem node in draftFolder)
            {
                draftFolderItems.Add(node);
            }
            return draftFolderItems;
        }

        private List<BinderItem> ListNarratorViewContents()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Private Methods

        #region Generate StoryBuilder model in BinderItem format

        public void UpdateStoryBuilderOutline()
        {
            // Locate StoryBuilder BinderItem or insert just before Research folder
            _storyBuilderNode = LocateFolder(_binderNode, "StoryBuilder");
            if (_storyBuilderNode == null)
                _storyBuilderNode = InsertFolderBefore(_binderNode, "Research", "StoryBuilder");
            // Locate or add StoryBuilder's three child folders
            _explorerNode = LocateFolder(_storyBuilderNode, "Explorer");
            if (_explorerNode == null)
                _explorerNode = AddFolder(_storyBuilderNode, "Explorer");
            _explorerNode.Children.Clear();
            _narratorNode = LocateFolder(_storyBuilderNode, "Narrator");
            if (_narratorNode == null)
                _narratorNode = AddFolder(_storyBuilderNode, "Narrator");
            _narratorNode.Children.Clear();
            _miscNode = LocateFolder(_storyBuilderNode, "Miscellaneous");
            if (_miscNode == null)
                _miscNode = AddFolder(_storyBuilderNode, "Miscellaneous");
            // Locate or add the Miscellaneous folder's child reports
            _problemListNode = LocateText(_miscNode, "List of Problems");
            if (_problemListNode == null)
                _problemListNode = AddText(_miscNode, "List of Problems");
            _characterListNode = LocateText(_miscNode, "List of Characters");
            if (_characterListNode == null)
                _characterListNode = AddText(_miscNode, "List of Characters");
            _settingListNode = LocateText(_miscNode, "List of Settings");
            if (_settingListNode == null)
                _settingListNode = AddText(_miscNode, "List of Settings");
            _sceneListNode = LocateText(_miscNode, "List of Scenes");
            if (_sceneListNode == null)
                _sceneListNode = AddText(_miscNode, "List of Scenes");
            _synopsisNode = LocateText(_miscNode, "Story Synopsis");
            if (_synopsisNode == null)
                _synopsisNode = AddText(_miscNode, "Story Synopsis");
            AddStoryExplorerNodes();
            AddStoryNarratorNodes();
        }

        private void AddStoryExplorerNodes()
        {
            RecurseStoryModelNode(_model.ExplorerView[0], _explorerNode);
        }

        private void AddStoryNarratorNodes()
        {
            RecurseStoryModelNode(_model.NarratorView[0], _narratorNode);
        }

        public BinderItem LocateFolder(BinderItem parent, string title)
        {
            // See if there if the desired folder exists under parent
            foreach (BinderItem child in parent.Children)
                if (child.Title == title)
                    return child;

            return null;
        }

        public BinderItem AddFolder(BinderItem parent, string title)
        {
            BinderItem item = new BinderItem(0, NewUuid(), BinderItemType.Folder, title);
            parent.Children.Add(item);
            return item;
        }

        public BinderItem InsertFolderBefore(BinderItem parent, string after, string title)
        {
            BinderItem item = new BinderItem(0, NewUuid(), BinderItemType.Folder, title);
            parent.Children.Insert(FolderIndex(_binderNode, after), item);
            return item;
        }

        private int FolderIndex(BinderItem parent, string title)
        {
            for (int i = 0; i < _binderNode.Children.Count; i++)
                if (_binderNode.Children[i].Title.Equals(title))
                    return i;
            return -1;
        }

        public BinderItem LocateText(BinderItem parent, string title)
        {
            // See if there if the desired file exists under parent
            foreach (BinderItem child in parent.Children)
                if (child.Title == title)
                    return child;

            return null;
        }

        public BinderItem AddText(BinderItem parent, string title)
        {
            BinderItem item = new BinderItem(0, NewUuid(), BinderItemType.Text, title);
            parent.Children.Add(item);
            return item;
        }

        private void RecurseStoryModelNode(StoryNodeItem node, BinderItem parent)
        {
            BinderItemType type;
            switch (node.Type)
            {
                case StoryItemType.Section:
                    type = BinderItemType.Folder;
                    break;
                case StoryItemType.Folder:
                    type = BinderItemType.Folder;
                    break;
                default:
                    type = BinderItemType.Text;
                    break;
            }
            BinderItem binderItem = new BinderItem(0, StoryWriter.UuidString(node.Uuid), type, node.Name, parent);
            foreach (StoryNodeItem child in node.Children)
                RecurseStoryModelNode(child, binderItem);
        }

        public async Task ProcessPreviousNotes()
        {
            _stbNotes = new StringBuilder(string.Empty);
            ///TODO: Activate notes collecting
            await LoadPreviousNotes();
            await ReadScrivenerNotes();
            await SaveScrivenerNotes();
        }

        /// <summary>
        /// If StoryBridge ran previously for this Scrivener project, there is a 'Miscellaneous' folder
        /// under the old StoryBuilder subfolder. It man contain a 'Previous Scrivener Notes' text file, which  
        /// contains the concatenated notes.rtf text for even earlier StoryBridge runs, if the Scrivener user
        /// has added any Inspector notes to any of the StoryBuilder reports.
        ///
        /// This function loads the 'Previous Scrivener Notes' file into a StringBuilder. We'll then append any
        /// notes.rtf content for the StoryBuilder reports to it in function ReadScrivenerNotes(). If the file
        /// doesn't exist, it creates an empty StringBuilder.
        /// </summary>
        public async Task LoadPreviousNotes()
        {
            _stbNotes = new StringBuilder();  // Initialize the aggregate notes

            if (_miscNode == null)
                return;

            // Look for 'Previous Scrivener Notes' in the old StoryBuilder's Miscellaneous subfolder and read it. (see ScrivenerReports)
            foreach (BinderItem node in (_miscNode))
            {
                if (node.Type == BinderItemType.Text)
                {
                    if (node.Title.Equals("Previous Scrivener Notes") & node.Parent.Title.Equals("Miscellaneous"))
                    {
                        StorageFolder di = await _scrivener.GetSubFolder(node.Uuid);
                        // There should be only one text file in the folder
                        var files = await di.GetFilesAsync();
                        if (files.Count != 1)
                            return;
                        // It should be a content.rtf file
                        if (!files[0].Name.Equals("content.rtf"))
                            return;
                        // Read and return it
                        string text = await _scrivener.ReadRtfText(Path.Combine(di.Path, "content.rtf")); // Read text, create StringBuilder from it
                        _stbNotes.Append(text);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Identify any Scrivener notes added for StoryBuilder content and add them to
        /// the StringBuilder stbNotes. They constitute a cumulative log of such notes
        /// from one StoryBridge execution to another. The log will be written along with
        /// the new StoryBuilder reports.
        /// </summary>
        public async Task ReadScrivenerNotes()
        {
            StorageFolder di;
            if (_miscNode != null)
            {
                foreach (BinderItem node in _miscNode) // For each text BinderItem in the old StoryBuilder's folders
                {
                    if (node.Type == BinderItemType.Text)
                    {
                        di = await _scrivener.GetSubFolder(node.Uuid);
                        IReadOnlyList<StorageFile> files = await di.GetFilesAsync();
                        foreach (StorageFile fi in files)
                            if (fi.Name.Equals("notes.rtf")) // If this content has any notes, collect them
                            {
                                string header = string.Format("SCRIVENER NOTES FOR '{0} {1}' as of {2}",
                                    node.Parent.Title, node.Title, node.Modified);
                                _stbNotes.AppendLine(header);
                                var text = await _scrivener.ReadRtfText(fi.Path);
                                _stbNotes.AppendLine(text);
                            }
                    }
                }
            }
        }

        /// <summary>
        /// After collecting all previous and current Scrivener notes related to StoryBuilder
        /// reports, write them as 'Previous Scrivener Notes' in the new StoryBuilder's
        /// Miscellaneous subfolder.
        /// </summary>
        public async Task SaveScrivenerNotes()
        {
            ///TODO: Complete SaveScrivenerNotes code
            await Task.Run(() =>
            {
                // Add a BinderItem for the report under the Miscellaneous folder
                BinderItem story = new BinderItem(0, NewUuid(), BinderItemType.Text, "Previous Scrivener Notes", _miscNode);
                // Create a folder for the document
                string path = Path.Combine(_scrivener.ProjectPath, "Files", "Data", story.Uuid, "content.rtf");
                // Locate and open the output content.rtf report
                // Locate and open the output content.rtf report
                //StorageFolder di = await GetSubFolder(story.Uuid); // Get subfolder path
                //StorageFile contents = await di.CreateFileAsync("content.rtf", CreationCollisionOption.ReplaceExisting);

                // Create the document itself
                RtfDocument doc = new RtfDocument(path);

                //StorageFile rtfFile = await di.CreateFileAsync("content.rtf", CreationCollisionOption.ReplaceExisting);
                // Add formatting for it
                RtfTextFormat format = new RtfTextFormat();
                format.font = "Calibri";
                format.size = 12;
                format.bold = false;
                format.underline = false;
                format.italic = false;
                //TODO: Is this formatting added?
                // Write the report
                doc.AddText(_stbNotes.ToString(), format);
                doc.AddNewLine();
                string notes = doc.GetRtf();
            });
        }

        #endregion

        #region Generate StoryBuilder reports under StoryBuilder model

        /// <summary>
        /// This method builds the reports under the StoryExplorer
        /// node, which consists of a tree of BinderItem nodes matching
        /// the StoryBuilder's StoryExplorer outline.
        /// 
        /// For each node the corresponding StoryElement is found
        /// (based on the matching Guid) and the report is generated
        /// as a content.rtf file in the BinderItem's subfolder.
        /// </summary>
        /// <param name="node">BinderItem node</param>
        /// <returns></returns>
        private async Task RecurseStoryElementReports(BinderItem node)
        {
            StoryElement element = null;
            Guid uuid = new Guid(node.Uuid);
            if (_model.StoryElements.StoryElementGuids.ContainsKey(uuid))
                element = _model.StoryElements.StoryElementGuids[uuid];
            if (element != null)
            {
                switch (element.Type)
                {
                    case StoryItemType.StoryOverview:
                        await GenerateStoryOverviewReport(node, element);
                        break;
                    case StoryItemType.Problem:
                        await GenerateProblemReport(node, element);
                        break;
                    case StoryItemType.Character:
                        await GenerateCharacterReport(node, element);
                        break;
                    case StoryItemType.Setting:
                        await GenerateSettingReport(node, element);
                        break;
                    case StoryItemType.Scene:
                        await GenerateSceneReport(node, element);
                        break;
                    case StoryItemType.Folder:
                        await GenerateFolderReport(node, element);
                        break;
                    case StoryItemType.Section:
                        await GenerateSectionReport(node, element);
                        break;
                }
            }
            foreach (BinderItem child in node.Children)
                await RecurseStoryElementReports(child);
        }

        private async Task GenerateStoryOverviewReport(BinderItem node, StoryElement element)
        {
            OverviewModel overview = (OverviewModel)element;

            // Locate and open the output content.rtf report
            StorageFolder di = await _scrivener.GetSubFolder(node.Uuid); // Get subfolder path
            StorageFile contents = await di.CreateFileAsync("content.rtf", CreationCollisionOption.ReplaceExisting);
 
            string rtf = _formatter.FormatStoryOverviewReport(overview);
            
            // Write the report
            await FileIO.WriteTextAsync(contents, rtf);
        }

        private async Task GenerateProblemListReport(BinderItem node)
        {
            // Locate and open the output content.rtf report
            StorageFolder di = await _scrivener.GetSubFolder(node.Uuid); // Get subfolder path
            StorageFile contents = await di.CreateFileAsync("content.rtf", CreationCollisionOption.ReplaceExisting);
            string rtf =  _formatter.FormatProblemListReport();
            // Write the report
            await FileIO.WriteTextAsync(contents, rtf);
        }

        private async Task GenerateProblemReport(BinderItem node, StoryElement element)
        {
            // Locate and open the output content.rtf report
            StorageFolder di = await _scrivener.GetSubFolder(node.Uuid); // Get subfolder path
            StorageFile contents = await di.CreateFileAsync("content.rtf", CreationCollisionOption.ReplaceExisting);

            string rtf = _formatter.FormatProblemReport(element);
            
            // Write the report
            await FileIO.WriteTextAsync(contents, rtf);
        }

        private async Task GenerateCharacterListReport(BinderItem node)
        {
            // Locate and open the output content.rtf report
            StorageFolder di = await _scrivener.GetSubFolder(node.Uuid); // Get subfolder path
            StorageFile contents = await di.CreateFileAsync("content.rtf", CreationCollisionOption.ReplaceExisting);
            string rtf = _formatter.FormatCharacterListReport();
            // Write the report
            await FileIO.WriteTextAsync(contents, rtf);
        }

        private async Task GenerateCharacterReport(BinderItem node, StoryElement element)
        {
            // Locate and open the output content.rtf report
            StorageFolder di = await _scrivener.GetSubFolder(node.Uuid); // Get subfolder path
            StorageFile contents = await di.CreateFileAsync("content.rtf", CreationCollisionOption.ReplaceExisting);

            string rtf = _formatter.FormatCharacterReport(element);
            
            // Write the report
            await FileIO.WriteTextAsync(contents, rtf);
        }

        private async Task GenerateSettingListReport(BinderItem node)
        {
            // Locate and open the output content.rtf report
            StorageFolder di = await _scrivener.GetSubFolder(node.Uuid); // Get subfolder path
            StorageFile contents = await di.CreateFileAsync("content.rtf", CreationCollisionOption.ReplaceExisting);

            string rtf = _formatter.FormatSettingListReport();
            
            // Write the report
            await FileIO.WriteTextAsync(contents, rtf);
        }

        private async Task GenerateSettingReport(BinderItem node, StoryElement element)
        {
            // Locate and open the output content.rtf report
            StorageFolder di = await _scrivener.GetSubFolder(node.Uuid); // Get subfolder path
            StorageFile contents = await di.CreateFileAsync("content.rtf", CreationCollisionOption.ReplaceExisting);
 
            string rtf = _formatter.FormatSettingReport(element);
            
            // Write the report
            await FileIO.WriteTextAsync(contents, rtf);
        }

        private async Task GenerateSceneListReport(BinderItem node)
        {
            // Locate and open the output content.rtf report
            StorageFolder di = await _scrivener.GetSubFolder(node.Uuid); // Get subfolder path
            StorageFile contents = await di.CreateFileAsync("content.rtf", CreationCollisionOption.ReplaceExisting);

            string rtf = _formatter.FormatSceneListReport();
            
            // Write the report
            await FileIO.WriteTextAsync(contents, rtf);
        }

        private async Task GenerateSceneReport(BinderItem node, StoryElement element)
        {
            // Locate and open the output content.rtf report
            StorageFolder di = await _scrivener.GetSubFolder(node.Uuid); // Get subfolder path
            StorageFile contents = await di.CreateFileAsync("content.rtf", CreationCollisionOption.ReplaceExisting);

            string rtf = _formatter.FormatSceneReport(element);

            // Write the report
            await FileIO.WriteTextAsync(contents, rtf);
        }

        private async Task GenerateFolderReport(BinderItem node, StoryElement element)
        {
            // Locate and open the output content.rtf report
            StorageFolder di = await _scrivener.GetSubFolder(node.Uuid); // Get subfolder path
            StorageFile contents = await di.CreateFileAsync("content.rtf", CreationCollisionOption.ReplaceExisting);

            string rtf = _formatter.FormatFolderReport(element);

            // Write the report
            await FileIO.WriteTextAsync(contents, rtf);
        }

        private async Task GenerateSectionReport(BinderItem node, StoryElement element)
        {
            // Locate and open the output content.rtf report
            StorageFolder di = await _scrivener.GetSubFolder(node.Uuid); // Get subfolder path
            StorageFile contents = await di.CreateFileAsync("content.rtf", CreationCollisionOption.ReplaceExisting);

            string rtf = _formatter.FormatSectionReport(element);

            // Write the report
            await FileIO.WriteTextAsync(contents, rtf);
        }

        private async Task GenerateSynopsisReport(BinderItem node)
        {
            // Locate and open the output content.rtf report
            StorageFolder di = await _scrivener.GetSubFolder(node.Uuid);
            StorageFile contents = await di.CreateFileAsync("content.rtf", CreationCollisionOption.ReplaceExisting);

            string rtf = _formatter.FormatSynopsisReport();

            // Write the report
            await FileIO.WriteTextAsync(contents, rtf);
        }


        private string NewUuid()
        {
            string id = Guid.NewGuid().ToString("B").ToUpper();
            id = id.Replace("{", string.Empty);
            id = id.Replace("}", string.Empty);
            return id;
        }

        #endregion

        #region Generate StoryBuilder metadata and scene BinderItems under DraftFolder

        private void AddCustomMetaDataSettings()
        {
            XmlElement customMetaData = (XmlElement)_scrivener.CustomMetaDataSettings;
            IXmlNode stbUuid = (XmlElement)_scrivener.StbUuidSetting;
            XmlAttribute attr;

            if (stbUuid != null)        // the setting already exits
                return;

            if (customMetaData == null)
            {
                customMetaData = _scrivener.XmlDocument.CreateElement("CustomMetaDataSettings");
                _scrivener.ScrivenerProject.InsertBefore(customMetaData, _scrivener.ProjectBookMarks);
            }

            stbUuid = _scrivener.XmlDocument.CreateElement("MetaDataField");

            attr = _scrivener.XmlDocument.CreateAttribute("Type");
            attr.Value = "Text";
            stbUuid.Attributes.SetNamedItem(attr);
            attr = _scrivener.XmlDocument.CreateAttribute("ID");
            attr.Value = "stbuuid";
            stbUuid.Attributes.SetNamedItem(attr);
            attr = _scrivener.XmlDocument.CreateAttribute("Wraps");
            attr.Value = "No";
            stbUuid.Attributes.SetNamedItem(attr);
            attr = _scrivener.XmlDocument.CreateAttribute("Color");
            attr.Value = "1.000000 0.666667 0.498039";
            stbUuid.Attributes.SetNamedItem(attr);
            IXmlNode title = _scrivener.XmlDocument.CreateElement("Title");
            title.InnerText = "stbuuid";
            stbUuid.AppendChild(title);

            customMetaData.AppendChild(stbUuid);
        }

        #endregion

        #region Generate my binder LabelSettings

        private void SetLabelSettings()
        {

            XmlAttribute attr;

            // Create a replacement LabelSettings node with my label values
            XmlElement labelSettings = _scrivener.XmlDocument.CreateElement("LabelSettings");

            IXmlNode title = _scrivener.XmlDocument.CreateElement("Title");
            title.InnerText = "Binder Labels";
            labelSettings.AppendChild(title);
            IXmlNode defaultId = _scrivener.XmlDocument.CreateElement("DefaultLabelID");
            title.InnerText = "-1";
            labelSettings.AppendChild(defaultId);
            IXmlNode labels = _scrivener.XmlDocument.CreateElement("Labels");
            labelSettings.AppendChild(labels);
            // Generate each label 
            IXmlNode label = _scrivener.XmlDocument.CreateElement("Label");
            attr = _scrivener.XmlDocument.CreateAttribute("ID");
            attr.Value = "-1";
            label.Attributes.SetNamedItem(attr);
            label.InnerText = "No Label";
            labels.AppendChild(label);
            label = _scrivener.XmlDocument.CreateElement("Label");
            attr = _scrivener.XmlDocument.CreateAttribute("ID");
            attr.Value = "1";
            label.Attributes.SetNamedItem(attr);
            labels.AppendChild(label);
            attr = _scrivener.XmlDocument.CreateAttribute("Color");
            attr.Value = "1.000000 1.000000 1.000000";
            label.Attributes.SetNamedItem(attr);
            label.InnerText = "Not Started";
            labels.AppendChild(label);
            label = _scrivener.XmlDocument.CreateElement("Label");
            attr = _scrivener.XmlDocument.CreateAttribute("ID");
            attr.Value = "2";
            labels.AppendChild(label);
            attr = _scrivener.XmlDocument.CreateAttribute("Color");
            attr.Value = "1.000000 1.000000 0.000000";
            label.Attributes.SetNamedItem(attr);
            labels.AppendChild(label);
            attr = _scrivener.XmlDocument.CreateAttribute("Color");
            attr.Value = "1.000000 1.000000 0.000000";
            label.Attributes.SetNamedItem(attr);
            label.InnerText = "In Progress";
            labels.AppendChild(label);
            label = _scrivener.XmlDocument.CreateElement("Label");
            attr = _scrivener.XmlDocument.CreateAttribute("ID");
            attr.Value = "3";
            label.Attributes.SetNamedItem(attr);
            labels.AppendChild(label);
            attr = _scrivener.XmlDocument.CreateAttribute("Color");
            attr.Value = "1.000000 0.000000 0.000000";
            label.Attributes.SetNamedItem(attr);
            label.InnerText = "First Draft";
            labels.AppendChild(label);
            label = _scrivener.XmlDocument.CreateElement("Label");
            attr = _scrivener.XmlDocument.CreateAttribute("ID");
            attr.Value = "4";
            label.Attributes.SetNamedItem(attr);
            labels.AppendChild(label);
            attr = _scrivener.XmlDocument.CreateAttribute("Color");
            attr.Value = "0.333333 1.000000 0.000000";
            label.Attributes.SetNamedItem(attr);
            label.InnerText = "Reviewed";
            labels.AppendChild(label);
            label = _scrivener.XmlDocument.CreateElement("Label");
            attr = _scrivener.XmlDocument.CreateAttribute("ID");
            attr.Value = "5";
            label.Attributes.SetNamedItem(attr);
            labels.AppendChild(label);
            attr = _scrivener.XmlDocument.CreateAttribute("Color");
            attr.Value = "0.000000 0.666667 0.000000";
            label.Attributes.SetNamedItem(attr);
            label.InnerText = "Revised Draft";
            labels.AppendChild(label);
            label = _scrivener.XmlDocument.CreateElement("Label");
            attr = _scrivener.XmlDocument.CreateAttribute("ID");
            attr.Value = "6";
            label.Attributes.SetNamedItem(attr);
            labels.AppendChild(label);
            attr = _scrivener.XmlDocument.CreateAttribute("Color");
            attr.Value = "0.000000 0.666667 0.000000";
            label.Attributes.SetNamedItem(attr);
            label.InnerText = "Final Draft";
            labels.AppendChild(label);
            label = _scrivener.XmlDocument.CreateElement("Label");
            attr = _scrivener.XmlDocument.CreateAttribute("ID");
            attr.Value = "7";
            label.Attributes.SetNamedItem(attr);
            labels.AppendChild(label);
            attr = _scrivener.XmlDocument.CreateAttribute("Color");
            attr.Value = "0.262745 0.262745 0.396078";
            label.Attributes.SetNamedItem(attr);
            label.InnerText = "Done";
            labels.AppendChild(label);

            if (_scrivener.LabelSettings != null)
            {
                IXmlNode parent = _scrivener.LabelSettings.ParentNode;
                parent.ReplaceChild(labelSettings, _scrivener.LabelSettings);
            }
            else
                _scrivener.ScrivenerProject.InsertBefore(labelSettings, _scrivener.StatusSettings);

        }

        #endregion

        #endregion Private methods
    }
}
