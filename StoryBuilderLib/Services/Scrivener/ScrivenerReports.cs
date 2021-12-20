using CommunityToolkit.Mvvm.DependencyInjection;
using Net.Sgoliver.NRtfTree.Util;
using StoryBuilder.Controllers;
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

namespace StoryBuilder.Services.Scrivener
{
    public class ScrivenerReports
    {
        private readonly Dictionary<string, string> _templates = new Dictionary<string, string>();
        private StoryModel _model;
        private ScrivenerIo _scrivener;
        private StoryReader _rdr;

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
            Ioc.Default.GetService<StoryController>();
            _scrivener = Ioc.Default.GetService<ScrivenerIo>();
            _scrivener.ScrivenerFile = file;
            _model = model;
            _rdr = Ioc.Default.GetService<StoryReader>();
            //_root = root;
            //_misc = miscFolder;
        }


        public StoryElement StringToStoryElement(string value)
        {
            if (value == null)
                return null;
            if (value.Equals(string.Empty))
                return null;
            // Get the current StoryModel's StoryElementsCollection
            ShellViewModel shell = Ioc.Default.GetService<ShellViewModel>();
            StoryElementCollection elements = shell.StoryModel.StoryElements;
            // legacy: locate the StoryElement from its Name
            foreach (StoryElement element in elements)  // Character or Setting??? Search both?
            {
                if (element.Type == StoryItemType.Character | element.Type == StoryItemType.Setting)
                {
                    if (value.Equals(element.Name))
                        return element;
                }
            }
            // Look for the StoryElement corresponding to the passed guid
            // (This is the normal approach)
            if (Guid.TryParse(value.ToString(), out var guid))
                if (elements.StoryElementGuids.ContainsKey(guid))
                    return elements.StoryElementGuids[guid];
            return null;  // Not found
        }

        #endregion

        #region public methods

        public async Task GenerateReports()
        {
            await _scrivener.LoadScrivenerProject();  // Load the Scrivener project
            _binderNode = _scrivener.BuildBinderItemTree(); // Build a BinderItem model
            UpdateStoryBuilderOutline();  // Replace or add StoryBuilder BinderItems to model
            await LoadReportTemplates(); // Load text report templates
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
            // Read report template
            if (!_templates.ContainsKey("Story Overview"))
                return;
            string template = _templates["Story Overview"]; ;
            string[] lines = template.Split(
                new[] { Environment.NewLine },
                StringSplitOptions.None
            );
            // Locate and open the output content.rtf report
            StorageFolder di = await _scrivener.GetSubFolder(node.Uuid); // Get subfolder path
            StorageFile contents = await di.CreateFileAsync("content.rtf", CreationCollisionOption.ReplaceExisting);
            // Create the document itself
            //RtfDocument doc = new RtfDocument(Path.Combine(di.Path, "content.rtf"));
            RtfDocument doc = new RtfDocument(string.Empty);
            // Add formatting for it
            RtfTextFormat format = new RtfTextFormat();
            format.font = "Calibri";
            format.size = 12;
            format.bold = false;
            format.underline = false;
            format.italic = false;
            // Pre-process RTF properties to preserve [FILE:x.rtf] tag for long fields
            // and then load long fields from their corresponding file in its subfolder
            string saveStoryIdea = overview.StoryIdea;
            string saveConcept = overview.Concept;
            //string savePremise = overview.Premise;
            string saveNotes = overview.Notes;
            overview.StoryIdea = await _rdr.GetRtfText(overview.StoryIdea, overview.Uuid);
            overview.Concept = await _rdr.GetRtfText(overview.Concept, overview.Uuid);
            //overview.Premise = await _rdr.GetRtfText(overview.Premise, overview.Uuid);
            overview.Notes = await _rdr.GetRtfText(overview.Notes, overview.Uuid);
            StoryElement vpChar = StringToStoryElement(overview.ViewpointCharacter);
            StoryElement vpStoryProblem = StringToStoryElement(overview.StoryProblem);

            //CharacterModel vpChar = overview.String
            // Parse and write the report
            foreach (string line in lines)
            {
                // Parse the report
                StringBuilder sb = new StringBuilder(line);
                sb.Replace("@Title", overview.Name);
                sb.Replace("@CreateDate", overview.DateCreated);
                sb.Replace("@ModifiedDate", overview.DateModified);
                sb.Replace("@Author", overview.Author);
                sb.Replace("@StoryType", overview.StoryType);
                sb.Replace("@Genre", overview.StoryGenre);
                sb.Replace("@Viewpoint", overview.Viewpoint);
                sb.Replace("@StoryIdea", overview.StoryIdea);
                sb.Replace("@Concept", overview.Concept);
                sb.Replace("@StoryProblem", vpStoryProblem.Name);
                if (vpStoryProblem != null)
                    sb.Replace("@StoryProblem", vpStoryProblem.Name);
                else
                    sb.Replace("@StoryProblem", string.Empty);

                sb.Replace("@Premise", overview.Premise);
                sb.Replace("@StoryType", overview.StoryType);
                sb.Replace("@StoryGenre", overview.StoryGenre);
                sb.Replace("@LiteraryDevice", overview.LiteraryDevice);
                sb.Replace("@viewpointCharacter", vpChar.Name);
                if (vpChar != null)
                    sb.Replace("@viewpointCharacter", vpChar.Name);
                else
                    sb.Replace("@viewpointCharacter", string.Empty);
                sb.Replace("@Voice", overview.Voice);
                sb.Replace("@Tense", overview.Tense);
                sb.Replace("@Style", overview.Style);
                sb.Replace("@styleNotes", overview.StyleNotes);
                sb.Replace("@Tone", overview.Tone);
                sb.Replace("@toneNotes", overview.ToneNotes);
                sb.Replace("@Notes", overview.Notes);
                doc.AddText(sb.ToString(), format);
                doc.AddNewLine();

            }
            // Write the report
            string rtf = doc.GetRtf();
            await FileIO.WriteTextAsync(contents, rtf);
            // Post-process RTF properties
            overview.StoryIdea = saveStoryIdea;
            overview.Concept = saveConcept;
            //overview.Premise = savePremise;
            overview.Notes = saveNotes;
        }

        private async Task GenerateProblemListReport(BinderItem node)
        {
            // Read report template
            if (!_templates.ContainsKey("List of Problems"))
                return;
            string template = _templates["List of Problems"];
            // Read report template
            string[] lines = template.Split(
                new[] { Environment.NewLine },
                StringSplitOptions.None
            );

            // Locate and open the output content.rtf report
            StorageFolder di = await _scrivener.GetSubFolder(node.Uuid); // Get subfolder path
            StorageFile contents = await di.CreateFileAsync("content.rtf", CreationCollisionOption.ReplaceExisting);
            // Create the document itself
            //RtfDocument doc = new RtfDocument(Path.Combine(di.Path, "content.rtf"));
            RtfDocument doc = new RtfDocument(string.Empty);
            // Add formatting for it
            RtfTextFormat format = new RtfTextFormat();
            format.font = "Calibri";
            format.size = 12;
            format.bold = false;
            format.underline = false;
            format.italic = false;

            // Parse and write the report
            foreach (string line in lines)
            {
                if (line.Contains("@Description"))
                {
                    foreach (StoryElement element in _model.StoryElements)
                        if (element.Type == StoryItemType.Problem)
                        {
                            ProblemModel chr = (ProblemModel)element;
                            StringBuilder sb = new StringBuilder(line);
                            sb.Replace("@Description", chr.Name);
                            doc.AddText(sb.ToString(), format);
                            doc.AddNewLine();
                        }
                }
                else
                {
                    doc.AddText(line, format);
                    doc.AddNewLine();
                }
            }
            // Write the report
            string rtf = doc.GetRtf();
            await FileIO.WriteTextAsync(contents, rtf);
        }

        private async Task GenerateProblemReport(BinderItem node, StoryElement element)
        {
            ProblemModel problem = (ProblemModel)element;
            if (node == null) throw new ArgumentNullException(nameof(node));
            // Read report template
            if (!_templates.ContainsKey("Problem Description"))
                return;
            string template = _templates["Problem Description"];
            string[] lines = template.Split(
                new[] { Environment.NewLine },
                StringSplitOptions.None
            );
            // Locate and open the output content.rtf report
            StorageFolder di = await _scrivener.GetSubFolder(node.Uuid); // Get subfolder path
            StorageFile contents = await di.CreateFileAsync("content.rtf", CreationCollisionOption.ReplaceExisting);
            // Create the document itself
            //RtfDocument doc = new RtfDocument(Path.Combine(di.Path, "content.rtf"));
            RtfDocument doc = new RtfDocument(string.Empty);
            // Add formatting for it
            RtfTextFormat format = new RtfTextFormat();
            format.font = "Calibri";
            format.size = 12;
            format.bold = false;
            format.underline = false;
            format.italic = false;
            // Pre-process RTF properties to preserve [FILE:x.rtf] tag for long fields
            // and then load long fields from their corresponding file in its subfolder
            string saveStoryQuestion = problem.StoryQuestion;
            problem.StoryQuestion = await _rdr.GetRtfText(problem.StoryQuestion, problem.Uuid);
            string savePremise = problem.Premise;
            problem.Premise = await _rdr.GetRtfText(problem.Premise, problem.Uuid);
            string saveNotes = problem.Notes;
            problem.Notes = await _rdr.GetRtfText(problem.Notes, problem.Uuid);
            // Parse and write the report
            foreach (string line in lines)
            {
                StringBuilder sb = new StringBuilder(line);
                sb.Replace("@Id", node.Id.ToString());
                sb.Replace("@Title", problem.Name);
                sb.Replace("@ProblemType", problem.ProblemType);
                sb.Replace("@ConflictType", problem.ConflictType);
                sb.Replace("@Subject", problem.Subject);
                sb.Replace("@StoryQuestion", problem.StoryQuestion);
                sb.Replace("@ProblemSource", problem.ProblemSource);
                sb.Replace("@ProtagName", problem.Protagonist);
                sb.Replace("@ProtagMotive", problem.ProtMotive);
                sb.Replace("@ProtagGoal", problem.ProtGoal);
                sb.Replace("@AntagName", problem.Antagonist);
                sb.Replace("@AntagMotive", problem.AntagMotive);
                sb.Replace("@AntagGoal", problem.AntagGoal);
                sb.Replace("@Outcome", problem.Outcome);
                sb.Replace("@Method", problem.Method);
                sb.Replace("@Theme", problem.Theme);
                sb.Replace("@Notes", problem.Notes);
                doc.AddText(sb.ToString(), format);
                doc.AddNewLine();
            }
            // Write the report
            string rtf = doc.GetRtf();
            await FileIO.WriteTextAsync(contents, rtf);
            // Post-process RTF properties
            problem.StoryQuestion = saveStoryQuestion;
            problem.Premise = savePremise;
            problem.Notes = saveNotes;
        }

        private async Task GenerateCharacterListReport(BinderItem node)
        {
            // Read report template
            if (!_templates.ContainsKey("List of Characters"))
                return;
            string template = _templates["List of Characters"];
            string[] lines = template.Split(
                new[] { Environment.NewLine },
                StringSplitOptions.None
            );

            // Locate and open the output content.rtf report
            StorageFolder di = await _scrivener.GetSubFolder(node.Uuid); // Get subfolder path
            StorageFile contents = await di.CreateFileAsync("content.rtf", CreationCollisionOption.ReplaceExisting);
            // Create the document itself
            //RtfDocument doc = new RtfDocument(Path.Combine(di.Path, "content.rtf"));
            RtfDocument doc = new RtfDocument(string.Empty);
            // Add formatting for it
            RtfTextFormat format = new RtfTextFormat();
            format.font = "Calibri";
            format.size = 12;
            format.bold = false;
            format.underline = false;
            format.italic = false;

            // Parse and write the report
            foreach (string line in lines)
            {
                if (line.Contains("@Description"))
                {
                    foreach (StoryElement element in _model.StoryElements)
                        if (element.Type == StoryItemType.Character)
                        {
                            CharacterModel chr = (CharacterModel)element;
                            StringBuilder sb = new StringBuilder(line);
                            sb.Replace("@Description", chr.Name);
                            doc.AddText(sb.ToString(), format);
                            doc.AddNewLine();
                        }
                }
                else
                {
                    doc.AddText(line, format);
                    doc.AddNewLine();
                }
            }
            // Write the report
            string rtf = doc.GetRtf();
            await FileIO.WriteTextAsync(contents, rtf);
        }

        private async Task GenerateCharacterReport(BinderItem node, StoryElement element)
        {
            CharacterModel character = (CharacterModel)element;
            // Read report template
            if (!_templates.ContainsKey("Character Description"))
                return;
            string template = _templates["Character Description"];
            string[] lines = template.Split(
                new[] { Environment.NewLine },
                StringSplitOptions.None
            );
            // Locate and open the output content.rtf report
            StorageFolder di = await _scrivener.GetSubFolder(node.Uuid); // Get subfolder path
            StorageFile contents = await di.CreateFileAsync("content.rtf", CreationCollisionOption.ReplaceExisting);
            // Create the document itself
            //RtfDocument doc = new RtfDocument(Path.Combine(di.Path, "content.rtf"));
            RtfDocument doc = new RtfDocument(string.Empty);
            // Add formatting for it
            RtfTextFormat format = new RtfTextFormat();
            format.font = "Calibri";
            format.size = 12;
            format.bold = false;
            format.underline = false;
            format.italic = false;

            // Pre-process RTF properties to preserve [FILE:x.rtf] tag for long fields
            // and then load long fields from their corresponding file in its subfolder
            string saveCharacterSketch = character.CharacterSketch;
            character.CharacterSketch = await _rdr.GetRtfText(character.CharacterSketch, character.Uuid);
            string savePhysNotes = character.PhysNotes;
            character.PhysNotes = await _rdr.GetRtfText(character.PhysNotes, character.Uuid);
            string saveAppearance = character.Appearance;
            character.Appearance = await _rdr.GetRtfText(character.Appearance, character.Uuid);
            string saveEconomic = character.Economic;
            character.Economic = await _rdr.GetRtfText(character.Economic, character.Uuid);
            string saveEducation = character.Education;
            character.Education = await _rdr.GetRtfText(character.Education, character.Uuid);
            string saveEthnic = character.Ethnic;
            character.Ethnic = await _rdr.GetRtfText(character.Ethnic, character.Uuid);
            string saveReligion = character.Religion;
            character.Religion = await _rdr.GetRtfText(character.Religion, character.Uuid);
            string savePsychNotes = character.PsychNotes;
            character.PsychNotes = await _rdr.GetRtfText(character.PsychNotes, character.Uuid);
            string saveLikes = character.Notes;
            character.Notes = await _rdr.GetRtfText(character.Notes, character.Uuid);
            // Parse and write the report
            foreach (string line in lines)
            {
                StringBuilder sb = new StringBuilder(line);
                //Story Role section
                sb.Replace("@Id", node.Id.ToString());
                sb.Replace("@Title", character.Name);
                sb.Replace("@Role", character.Role);
                sb.Replace("@StoryRole", character.StoryRole);
                sb.Replace("@Archetype", character.Archetype);
                sb.Replace("@CharacterSketch", character.CharacterSketch);
                //Physical section
                sb.Replace("@Age", character.Age);
                sb.Replace("@Sex", character.Sex);
                sb.Replace("@Height", character.CharHeight);
                sb.Replace("@Weight", character.Weight);
                sb.Replace("@Eyes", character.Eyes);
                sb.Replace("@Hair", character.Hair);
                sb.Replace("@Build", character.Build);
                sb.Replace("@Skin", character.Complexion);
                sb.Replace("@Race", character.Race);
                sb.Replace("@Nationality", character.Nationality);
                sb.Replace("@Health", character.Health);
                sb.Replace("@PhysNotes", character.PhysNotes);
                //Appearance section
                sb.Replace("@Appearance", character.Appearance);
                //Relationships section
                sb.Replace("@Relationship", character.Relationship);
                sb.Replace("@relationType", character.RelationType);
                sb.Replace("@relationTrait", character.RelationTrait);
                sb.Replace("@Attitude", character.Attitude);
                sb.Replace("@RelationshipNotes", character.RelationshipNotes);
                //Flaw section
                sb.Replace("@Flaw", character.Flaw);
                

                //Backstory section
                sb.Replace("@Notes", character.BackStory);

                //Social Traits section
                sb.Replace("@Economic", character.Economic);
                sb.Replace("@Education", character.Education);
                sb.Replace("@Ethnic", character.Ethnic);
                sb.Replace("@Religion", character.Religion);
                //Psychological Traits section
                sb.Replace("@Personality", character.Enneagram);
                sb.Replace("@Intelligence", character.Intelligence);
                sb.Replace("@Values", character.Values);
                sb.Replace("@Focus", character.Focus);
                sb.Replace("@Abnormality", character.Abnormality);
                sb.Replace("@PsychNotes", character.PsychNotes);
                //Inner Traits section
                sb.Replace("@Adventure", character.Adventureousness);
                sb.Replace("@Aggression", character.Aggression);
                sb.Replace("@Confidence", character.Confidence);
                sb.Replace("@Conscientious", character.Conscientiousness);
                sb.Replace("@Creative", character.Creativity);
                sb.Replace("@Dominance", character.Dominance);
                sb.Replace("@Enthusiasm", character.Enthusiasm);
                sb.Replace("@Assurance", character.Assurance);
                sb.Replace("@Sensitivity", character.Sensitivity);
                sb.Replace("@Shrewdness", character.Shrewdness);
                sb.Replace("@Sociability", character.Sociability);
                sb.Replace("@Stability", character.Stability);
                //Outer Traits section
                sb.Replace("@Traits", character.outerTrait);
                doc.AddText(sb.ToString(), format);
                doc.AddNewLine();
            }

            // Write the report
            string rtf = doc.GetRtf();
            await FileIO.WriteTextAsync(contents, rtf);
            // Post-produce RTF files
            character.CharacterSketch = saveCharacterSketch;
            character.PhysNotes = savePhysNotes;
            character.Appearance = saveAppearance;
            character.Economic = saveEconomic;
            character.Education = saveEducation;
            character.Ethnic = saveEthnic;
            character.Religion = saveReligion;
            character.PsychNotes = savePsychNotes;
            character.Notes = saveLikes;
        }

        private async Task GenerateSettingListReport(BinderItem node)
        {
            // Read report template
            if (!_templates.ContainsKey("List of Settings"))
                return;
            string template = _templates["List of Settings"];
            string[] lines = template.Split(
                new[] { Environment.NewLine },
                StringSplitOptions.None
            );

            // Locate and open the output content.rtf report
            StorageFolder di = await _scrivener.GetSubFolder(node.Uuid); // Get subfolder path
            StorageFile contents = await di.CreateFileAsync("content.rtf", CreationCollisionOption.ReplaceExisting);
            // Create the document itself
            //RtfDocument doc = new RtfDocument(Path.Combine(di.Path, "content.rtf"));
            RtfDocument doc = new RtfDocument(string.Empty);
            // Add formatting for it
            RtfTextFormat format = new RtfTextFormat();
            format.font = "Calibri";
            format.size = 12;
            format.bold = false;
            format.underline = false;
            format.italic = false;

            // Parse and write the report
            foreach (string line in lines)
            {
                if (line.Contains("@Description"))
                {
                    foreach (StoryElement element in _model.StoryElements)
                        if (element.Type == StoryItemType.Setting)
                        {
                            SettingModel loc = (SettingModel)element;
                            StringBuilder sb = new StringBuilder(line);
                            sb.Replace("@Description", loc.Name);
                            doc.AddText(sb.ToString(), format);
                            doc.AddNewLine();
                        }
                }
                else
                {
                    doc.AddText(line, format);
                    doc.AddNewLine();
                }
            }
            // Write the report
            string rtf = doc.GetRtf();
            await FileIO.WriteTextAsync(contents, rtf);
        }

        private async Task GenerateSettingReport(BinderItem node, StoryElement element)
        {
            SettingModel setting = (SettingModel)element;
            // Read report template
            if (!_templates.ContainsKey("Setting Description"))
                return;
            string template = _templates["Setting Description"];
            string[] lines = template.Split(
                new[] { Environment.NewLine },
                StringSplitOptions.None
            );
            // Locate and open the output content.rtf report
            StorageFolder di = await _scrivener.GetSubFolder(node.Uuid); // Get subfolder path
            StorageFile contents = await di.CreateFileAsync("content.rtf", CreationCollisionOption.ReplaceExisting);
            // Create the document itself
            //RtfDocument doc = new RtfDocument(Path.Combine(di.Path, "content.rtf"));
            RtfDocument doc = new RtfDocument(string.Empty);
            // Add formatting for it
            RtfTextFormat format = new RtfTextFormat();
            format.font = "Calibri";
            format.size = 12;
            format.bold = false;
            format.underline = false;
            format.italic = false;
            // Pre-process RTF properties to preserve [FILE:x.rtf] tag for long fields
            // and then load long fields from their corresponding file in its subfolder
            string saveSights = setting.Sights;
            setting.Sights = await _rdr.GetRtfText(setting.Sights, setting.Uuid);
            string saveSounds = setting.Sounds;
            setting.Sounds = await _rdr.GetRtfText(setting.Sounds, setting.Uuid);
            string saveTouch = setting.Touch;
            setting.Touch = await _rdr.GetRtfText(setting.Touch, setting.Uuid);
            string saveSmellTaste = setting.SmellTaste;
            setting.SmellTaste = await _rdr.GetRtfText(setting.SmellTaste, setting.Uuid);
            string saveNotes = setting.Notes;
            setting.Notes = await _rdr.GetRtfText(setting.Notes, setting.Uuid);
            // Parse and write the report
            foreach (string line in lines)
            {
                StringBuilder sb = new StringBuilder(line);
                sb.Replace("@Id", node.Id.ToString());
                sb.Replace("@Title", setting.Name);
                sb.Replace("@Locale", setting.Locale);
                sb.Replace("@Season", setting.Season);
                sb.Replace("@Period", setting.Period);
                sb.Replace("@Lighting", setting.Lighting);
                sb.Replace("@Weather", setting.Weather);
                sb.Replace("@Temperature", setting.Temperature);
                sb.Replace("@Prop1", setting.Prop1);
                sb.Replace("@Prop2", setting.Prop2);
                sb.Replace("@Prop3", setting.Prop3);
                sb.Replace("@Prop4", setting.Prop4);
                sb.Replace("@Sights", setting.Sights);
                sb.Replace("@Sounds", setting.Sounds);
                sb.Replace("@Touch", setting.Touch);
                sb.Replace("@SmellTaste", setting.SmellTaste);
                sb.Replace("@Notes", setting.Notes);
                doc.AddText(sb.ToString(), format);
                doc.AddNewLine();
            }
            // Write the report
            string rtf = doc.GetRtf();
            await FileIO.WriteTextAsync(contents, rtf);
            // Post-process RTF properties
            setting.Sights = saveSights;
            setting.Sounds = saveSounds;
            setting.Touch = saveTouch;
            setting.SmellTaste = saveSmellTaste;
            setting.Notes = saveNotes;
        }

        private async Task GenerateSceneListReport(BinderItem node)
        {
            // Read report template
            if (!_templates.ContainsKey("List of Scenes"))
                return;
            string template = _templates["List of Scenes"];
            string[] lines = template.Split(
                new[] { Environment.NewLine },
                StringSplitOptions.None
            );

            // Locate and open the output content.rtf report
            StorageFolder di = await _scrivener.GetSubFolder(node.Uuid); // Get subfolder path
            StorageFile contents = await di.CreateFileAsync("content.rtf", CreationCollisionOption.ReplaceExisting);
            // Create the document itself
            //RtfDocument doc = new RtfDocument(Path.Combine(di.Path, "content.rtf"));
            RtfDocument doc = new RtfDocument(string.Empty);
            // Add formatting for it
            RtfTextFormat format = new RtfTextFormat();
            format.font = "Calibri";
            format.size = 12;
            format.bold = false;
            format.underline = false;
            format.italic = false;

            // Parse and write the report
            foreach (string line in lines)
            {
                if (line.Contains("@Description"))
                {
                    foreach (StoryElement element in _model.StoryElements)
                        if (element.Type == StoryItemType.Scene)
                        {
                            SceneModel chr = (SceneModel)element;
                            StringBuilder sb = new StringBuilder(line);
                            sb.Replace("@Description", chr.Name);
                            doc.AddText(sb.ToString(), format);
                            doc.AddNewLine();
                        }
                }
                else
                {
                    doc.AddText(line, format);
                    doc.AddNewLine();
                }
            }
            // Write the report
            string rtf = doc.GetRtf();
            await FileIO.WriteTextAsync(contents, rtf);
        }

        private async Task GenerateSceneReport(BinderItem node, StoryElement element)
        {
            SceneModel scene = (SceneModel)element;
            // Read report template
            if (!_templates.ContainsKey("Scene Description"))
                return;
            string template = _templates["Scene Description"];
            string[] lines = template.Split(
                new[] { Environment.NewLine },
                StringSplitOptions.None
            );
            // Locate and open the output content.rtf report
            StorageFolder di = await _scrivener.GetSubFolder(node.Uuid); // Get subfolder path
            StorageFile contents = await di.CreateFileAsync("content.rtf", CreationCollisionOption.ReplaceExisting);
            // Create the document itself
            //RtfDocument doc = new RtfDocument(Path.Combine(di.Path, "content.rtf"));
            RtfDocument doc = new RtfDocument(string.Empty);
            // Add formatting for it
            RtfTextFormat format = new RtfTextFormat();
            format.font = "Calibri";
            format.size = 12;
            format.bold = false;
            format.underline = false;
            format.italic = false;
            // Pre-process RTF properties to preserve [FILE:x.rtf] tag for long fields
            // and then load long fields from their corresponding file in its subfolder
            string saveRemarks = scene.Remarks;
            scene.Remarks = await _rdr.GetRtfText(scene.Remarks, scene.Uuid);
            string saveReview = scene.Review;
            scene.Review = await _rdr.GetRtfText(scene.Review, scene.Uuid);
            string saveNotes = scene.Notes;
            scene.Notes = await _rdr.GetRtfText(scene.Notes, scene.Uuid);
            // Parse and write the report
            foreach (string line in lines)
            {
                StringBuilder sb = new StringBuilder(line);
                sb.Replace("@Id", node.Id.ToString());
                sb.Replace("@Title", scene.Name);
                sb.Replace("@Date", scene.Date);
                sb.Replace("@Time", scene.Time);
                sb.Replace("@Viewpoint", scene.Viewpoint);
                sb.Replace("@Setting", scene.Setting);
                sb.Replace("@Char1", scene.Char1);
                sb.Replace("@Char2", scene.Char2);
                sb.Replace("@Char3", scene.Char3);
                sb.Replace("@Role1", scene.Role1);
                sb.Replace("@Role2", scene.Role2);
                sb.Replace("@Role3", scene.Role3);
                sb.Replace("@Remarks", scene.Remarks);
                sb.Replace("@ProtagName", scene.Protagonist);
                sb.Replace("@ProtagEmotion", scene.ProtagEmotion);
                sb.Replace("@ProtagGoal", scene.ProtagGoal);
                sb.Replace("@AntagName", scene.Antagonist);
                sb.Replace("@AntagEmotion", scene.AntagEmotion);
                sb.Replace("@AntagGoal", scene.AntagGoal);
                sb.Replace("@Opposition", scene.Opposition);
                sb.Replace("@Outcome", scene.Outcome);
                sb.Replace("@Emotion", scene.Emotion);
                sb.Replace("@Review", scene.Review);
                sb.Replace("@NewGoal", scene.NewGoal);
                sb.Replace("@Notes", scene.Notes);
                doc.AddText(sb.ToString(), format);
                doc.AddNewLine();
            }
            // Write the report
            string rtf = doc.GetRtf();
            await FileIO.WriteTextAsync(contents, rtf);
            // Post-process RTF properties
            scene.Remarks = saveRemarks;
            scene.Review = saveReview;
            scene.Notes = saveNotes;
        }

        private async Task GenerateFolderReport(BinderItem node, StoryElement element)
        {
            FolderModel folder = (FolderModel)element;
            // Read report template
            if (!_templates.ContainsKey("Folder Description"))
                return;
            string template = _templates["Folder Description"];
            string[] lines = template.Split(
                new[] { Environment.NewLine },
                StringSplitOptions.None
            );
            // Locate and open the output content.rtf report
            StorageFolder di = await _scrivener.GetSubFolder(node.Uuid); // Get subfolder path
            StorageFile contents = await di.CreateFileAsync("content.rtf", CreationCollisionOption.ReplaceExisting);
            // Create the document itself
            //RtfDocument doc = new RtfDocument(Path.Combine(di.Path, "content.rtf"));
            RtfDocument doc = new RtfDocument(string.Empty);
            // Add formatting for it
            RtfTextFormat format = new RtfTextFormat();
            format.font = "Calibri";
            format.size = 12;
            format.bold = false;
            format.underline = false;
            format.italic = false;
            // Pre-process RTF properties to preserve [FILE:x.rtf] tag for long fields
            // and then load long fields from their corresponding file in its subfolder
            string saveNotes = folder.Notes;
            folder.Notes = await _rdr.GetRtfText(folder.Notes, folder.Uuid);
            // Parse and write the report
            foreach (string line in lines)
            {
                StringBuilder sb = new StringBuilder(line);
                sb.Replace("@Name", folder.Name);
                sb.Replace("@Notes", folder.Notes);
                doc.AddText(sb.ToString(), format);
            }
            // Write the report
            string rtf = doc.GetRtf();
            await FileIO.WriteTextAsync(contents, rtf);
            // Post-process RTF properties
            folder.Notes = saveNotes;
        }

        private async Task GenerateSectionReport(BinderItem node, StoryElement element)
        {
            SectionModel section = (SectionModel)element;
            // Read report template
            if (!_templates.ContainsKey("Section Description"))
                return;
            string template = _templates["Section Description"];
            string[] lines = template.Split(
                new[] { Environment.NewLine },
                StringSplitOptions.None
            );
            // Locate and open the output content.rtf report
            StorageFolder di = await _scrivener.GetSubFolder(node.Uuid); // Get subfolder path
            StorageFile contents = await di.CreateFileAsync("content.rtf", CreationCollisionOption.ReplaceExisting);
            // Create the document itself
            //RtfDocument doc = new RtfDocument(Path.Combine(di.Path, "content.rtf"));
            RtfDocument doc = new RtfDocument(string.Empty);
            // Add formatting for it
            RtfTextFormat format = new RtfTextFormat();
            format.font = "Calibri";
            format.size = 12;
            format.bold = false;
            format.underline = false;
            format.italic = false;
            // Pre-process RTF properties to preserve [FILE:x.rtf] tag for long fields
            // and then load long fields from their corresponding file in its subfolder
            string saveNotes = section.Notes;
            section.Notes = await _rdr.GetRtfText(section.Notes, section.Uuid);
            // Parse and write the report
            foreach (string line in lines)
            {
                StringBuilder sb = new StringBuilder(line);
                sb.Replace("@Id", node.Id.ToString());
                sb.Replace("@Name", section.Name);
                sb.Replace("@Notes", section.Notes);
                doc.AddText(sb.ToString(), format);
            }
            // Write the report
            string rtf = doc.GetRtf();
            await FileIO.WriteTextAsync(contents, rtf);
            // Post-process RTF properties
            section.Notes = saveNotes;
        }

        private async Task GenerateSynopsisReport(BinderItem node)
        {
            // Read report template
            if (!_templates.ContainsKey("Story Synopsis"))
                return;
            string template = _templates["Story Synopsis"];
            string[] lines = template.Split(
                new[] { Environment.NewLine },
                StringSplitOptions.None
            );
            // Create a folder for the document
            StorageFolder di = await _scrivener.GetSubFolder(node.Uuid);
            // Create the document itself
            RtfDocument doc = new RtfDocument(Path.Combine(di.Path, "content.rtf"));
            // Add formatting for it
            RtfTextFormat format = new RtfTextFormat();
            format.font = "Calibri";
            format.size = 12;
            format.bold = false;
            format.underline = false;
            format.italic = false;
            // Parse and write the report
            foreach (string line in lines)
            {
                if (line.Contains("@Synopsis"))
                {
                    // Find StoryNarrator' Scenes
                    foreach (StoryNodeItem child in _model.NarratorView[0].Children)
                    {
                        StoryElement scn = _model.StoryElements.StoryElementGuids[child.Uuid];
                        SceneModel scene = (SceneModel)scn;
                        var sb = new StringBuilder(line);
                        sb.Replace("@Synopsis", $"[{scene.Name}] {scene.Description}");
                        doc.AddText(sb.ToString(), format);
                        doc.AddNewLine();
                        doc.AddText(scene.Remarks, format);
                        doc.AddNewLine();
                        doc.AddNewLine();
                    }
                }
                else
                {
                    doc.AddText(line, format);
                    doc.AddNewLine();
                }
            }

            string rtf = doc.GetRtf();
            // Locate and open the output content.rtf report
            StorageFile contents = await di.CreateFileAsync("content.rtf", CreationCollisionOption.ReplaceExisting);
            // Write the report
            await FileIO.WriteTextAsync(contents, rtf);
        }

        private async Task LoadReportTemplates()
        {
            try
            {
                _templates.Clear();
                StorageFolder localFolder = ApplicationData.Current.RoamingFolder;
                StorageFolder stbFolder = await localFolder.GetFolderAsync("StoryBuilder");
                StorageFolder templatesFolder = await stbFolder.GetFolderAsync("reports");
                var templates = await templatesFolder.GetFilesAsync();
                foreach (var fi in templates)
                {
                    string name = fi.DisplayName.Substring(0, fi.Name.Length - 4);
                    string text = await FileIO.ReadTextAsync(fi);

                    _templates.Add(name, text);
                }
            }
            catch (Exception ex)
            {
                //TODO: Log exception
            }
        }

        /// <summary>
        /// Generate a UUID in the Scrivener format (i.e., without curly braces)
        /// </summary>
        /// <returns>string UUID representation</returns>
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
