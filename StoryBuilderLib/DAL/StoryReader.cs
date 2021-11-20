using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using StoryBuilder.Controllers;
using StoryBuilder.Models;
using StoryBuilder.Services.Logging;
using StoryBuilder.Services.Messages;
using StoryBuilder.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.Storage;
using static Windows.Data.Xml.Dom.XmlDocument;
using XmlDocument = Windows.Data.Xml.Dom.XmlDocument;
using XmlElement = Windows.Data.Xml.Dom.XmlElement;
using XmlNodeList = Windows.Data.Xml.Dom.XmlNodeList;


namespace StoryBuilder.DAL
{
    /// <summary>
    /// StoryWriter parses StoryBuilder's model and writes it to its backing store
    /// (the .stbx file), which is an Xml Document. 
    /// </summary>
    public class StoryReader : ObservableRecipient
    {
        /// StoryBuilder's model is found in the StoryBuilder.Models namespace and consists
        /// of various POCO (Plain Old CLR) objects.
        ///
        private readonly StoryController _story;
        public readonly LogService Logger;

        /// The in-memory representation of the .stbx file is an XmlDocument
        /// and its various components are all XmlNodes.
        private static XmlDocument _xml;

        /// Story contains a collection of StoryElements, a representation of the StoryExplorer tree,
        /// a representation of the Narrator tree, and a collection of story-specific settings.
        private StoryModel _model;

        // There's one OverviewModel per story; it's also the Treeview root
        private OverviewModel _overview;

        public async Task<StoryModel> ReadFile(StorageFile file)
        {
            try
            {
                string msg = $"Reading file {file.Path}.";
                Logger.Log(LogLevel.Info, msg);
                _xml = await LoadFromFileAsync(file);
                LoadStoryModel();
                // Early story outlines may have been built or converted
                // without a TrashCan node added. If this model is one of 
                // those, add the node to both the Explorer and Narrator views.
                if (_model.ExplorerView.Count == 1)
                {
                    TrashCanModel trash = new TrashCanModel(_model);
                    StoryNodeItem trashNode = new StoryNodeItem(trash, null);
                    _model.ExplorerView.Add(trashNode);     // The trashcan is the second root
                }
                if (_model.NarratorView.Count == 1)
                {
                    TrashCanModel trash = new TrashCanModel(_model);
                    StoryNodeItem trashNode = new StoryNodeItem(trash, null);
                    _model.NarratorView.Add(trashNode);     // The trashcan is the second root
                }
                msg = $"File load successful.";
                Logger.Log(LogLevel.Info, msg);
                var smsg = new StatusMessage(msg, 200);
                Messenger.Send(new StatusChangedMessage(smsg));
                return _model;
            }
            catch (Exception ex)
            {
                Logger.LogException(LogLevel.Error, ex, "Error reading story");
                var smsg = new StatusMessage("Error reading story", 200);
                Messenger.Send(new StatusChangedMessage(smsg));
                return new StoryModel();  // return an empty story model
            }
        }

        private void LoadStoryModel()
        {
            _model = new StoryModel();

            XmlElement root = _xml.DocumentElement;

            // This is the node we are looking for in the XML string
            XmlNodeList nodes = root.ChildNodes;

            if (nodes != null)
            {
                foreach (var xn in nodes)
                {
                    switch (xn.NodeName)
                    {
                        case "StoryBuilder":
                            break;
                        case "StoryElements":
                            ParseStoryElements(xn);
                            break;
                        case "Explorer":
                            ParseStoryExplorer(xn);
                            break;
                        case "Narrator":
                            ParseStoryNarrator(xn);
                            break;
                        case "Settings":  // story settings
                            break;
                    }
                }
            }
        }

        private void ParseStoryElements(IXmlNode node)
        {
            foreach (var child in node.ChildNodes)
                RecurseStoryElement(child);
        }

        private void RecurseStoryElement(IXmlNode node)
        {
            if (node.NodeType == NodeType.ElementNode)
            {
                switch (node.NodeName)
                {
                    case "Overview":
                        ParseOverView(node);
                        break;
                    case "Problem":
                        ParseProblem(node);
                        break;
                    case "Character":
                        ParseCharacter(node);
                        break;
                    case "Setting":
                        ParseSetting(node);
                        break;
                    case "PlotPoint":
                        ParsePlotPoint(node);
                        break;
                    case "Separator":       // legacy; Separator was renamed to Folder
                        ParseFolder(node);
                        break;
                    case "Folder":
                        ParseFolder(node);
                        break;
                    case "Section":
                        ParseSection(node);
                        break;
                    case "TrashCan":
                        ParseTrashCan(node);
                        break;
                }
                foreach (var child in node.ChildNodes)
                    RecurseStoryElement(child);
            }
        }

        private void ParseOverView(IXmlNode xn)
        {
            // There's one OverviewModel per story. Its corresponding
            //StoryNode is the root of the Explorer TreeView.
            _overview = new OverviewModel(xn, _model);
            foreach (var attr in xn.Attributes)
            {
                switch (attr.NodeName)
                {
                    case "UUID":
                        break;
                    case "Name":
                        _overview.Name = attr.InnerText;
                        break;
                    case "DateCreated":
                        _overview.DateCreated = attr.InnerText;
                        break;
                    case "DateModified":
                        _overview.DateModified = attr.InnerText;
                        break;
                    case "Author":
                        _overview.Author = attr.InnerText;
                        break;
                    case "StoryType":
                        _overview.StoryType = attr.InnerText;
                        break;
                    case "StoryGenre":
                        _overview.StoryGenre = attr.InnerText;
                        break;
                    case "ViewPoint":
                        _overview.Viewpoint = attr.InnerText;
                        break;
                    case "Form":
                        _overview.LiteraryDevice = attr.InnerText;
                        break;
                    case "LiteraryDevice":
                        _overview.LiteraryDevice = attr.InnerText;
                        break;
                    case "ViewpointCharacter":
                        _overview.ViewpointCharacter = attr.InnerText;
                        break;
                    case "Voice":
                        _overview.Voice = attr.InnerText;
                        break;
                    case "Style":
                        _overview.Style = attr.InnerText;
                        break;
                    case "Tense":
                        _overview.Tense = attr.InnerText;
                        break;
                    case "Tone":
                        _overview.Tone = attr.InnerText;
                        break;
                    case "StoryIdea":
                        _overview.StoryIdea = attr.InnerText;
                        break;
                    case "Concept":
                        _overview.Concept = attr.InnerText;
                        break;
                    case "StyleNotes":
                        _overview.StyleNotes = attr.InnerText;
                        break;
                    case "ToneNotes":
                        _overview.ToneNotes = attr.InnerText;
                        break;
                    case "Notes":
                        _overview.Notes = attr.InnerText;
                        break;
                }
            }
        }

        private void ParseProblem(IXmlNode xn)
        {
            var prb = new ProblemModel(xn, _model);
            foreach (var attr in xn.Attributes)
            {
                switch (attr.NodeName)
                {
                    case ("UUID"):
                        break;
                    case ("Name"):
                        prb.Name = attr.InnerText;
                        break;
                    case ("ProblemType"):
                        prb.ProblemType = attr.InnerText;
                        break;
                    case ("ConflictType"):
                        prb.ConflictType = attr.InnerText;
                        break;
                    case ("Subject"):
                        prb.Subject = attr.InnerText;
                        break;
                    case ("ProblemSource"):
                        prb.ProblemSource = attr.InnerText;
                        break;
                    case ("Protagonist"):
                        prb.Protagonist = attr.InnerText;
                        break;
                    case ("ProtGoal"):
                        prb.ProtGoal = attr.InnerText;
                        break;
                    case ("ProtMotive"):
                        prb.ProtMotive = attr.InnerText;
                        break;
                    case ("ProtConflict"):
                        prb.ProtConflict = attr.InnerText;
                        break;
                    case ("Antagonist"):
                        prb.Antagonist = attr.InnerText;
                        break;
                    case ("AntagGoal"):
                        prb.AntagGoal = attr.InnerText;
                        break;
                    case ("AntagMotive"):
                        prb.AntagMotive = attr.InnerText;
                        break;
                    case ("AntagConflict"):
                        prb.AntagConflict = attr.InnerText;
                        break;
                    case ("Outcome"):
                        prb.Outcome = attr.InnerText;
                        break;
                    case ("Method"):
                        prb.Method = attr.InnerText;
                        break;
                    case ("Theme"):
                        prb.Theme = attr.InnerText;
                        break;
                    case "StoryQuestion":
                        prb.StoryQuestion = attr.InnerText;
                        break;
                    case "Premise":
                        prb.Premise = attr.InnerText;
                        break;
                    case "Notes":
                        prb.Notes = attr.InnerText;
                        break;
                }
            }
        }

        private void ParseCharacter(IXmlNode xn)
        {
            var chr = new CharacterModel(xn, _model);
            var traitList = xn.SelectSingleNode("./CharacterTraits");
            if (traitList != null)
            {
                foreach (IXmlNode child in traitList.ChildNodes)
                    if (child.NodeName.Equals("Trait"))
                        chr.TraitList.Add(child.InnerText);
            }
            var relationList = xn.SelectSingleNode("./Relationships");
            if (relationList != null)
            {
                foreach (IXmlNode child in relationList.ChildNodes)
                    if (child.NodeName.Equals("Relation"))
                        chr.RelationshipList.Add(new RelationshipModel(child));
            }
            foreach (var attr in xn.Attributes)
            {
                switch (attr.NodeName)
                {
                    case "UUID":
                        break;
                    case "Name":
                        chr.Name = attr.InnerText;
                        break;
                    case "Id":
                        chr.Id = Convert.ToInt32(attr.InnerText);
                        break;
                    case "Role":
                        chr.Role = attr.InnerText;
                        break;
                    case "StoryRole":
                        chr.StoryRole = attr.InnerText;
                        break;
                    case "Archetype":
                        chr.Archetype = attr.InnerText;
                        break;
                    case "Age":
                        chr.Age = attr.InnerText;
                        break;
                    case "Sex":
                        chr.Sex = attr.InnerText;
                        break;
                    case "Eyes":
                        chr.Eyes = attr.InnerText;
                        break;
                    case "Hair":
                        chr.Hair = attr.InnerText;
                        break;
                    case "Weight":
                        chr.Weight = attr.InnerText;
                        break;
                    case "CharHeight":
                        chr.CharHeight = attr.InnerText;
                        break;
                    case "Build":
                        chr.Build = attr.InnerText;
                        break;
                    case "Complexion":
                        chr.Complexion = attr.InnerText;
                        break;
                    case "Race":
                        chr.Race = attr.InnerText;
                        break;
                    case "Nationality":
                        chr.Nationality = attr.InnerText;
                        break;
                    case "Health":
                        chr.Health = attr.InnerText;
                        break;
                    case "Enneagram":
                        chr.Enneagram = attr.InnerText;
                        break;
                    case "Intelligence":
                        chr.Intelligence = attr.InnerText;
                        break;
                    case "Values":
                        chr.Values = attr.InnerText;
                        break;
                    case "Abnormality":
                        chr.Abnormality = attr.InnerText;
                        break;
                    case "Focus":
                        chr.Focus = attr.InnerText;
                        break;
                    case "Adventureousness":
                        chr.Adventureousness = attr.InnerText;
                        break;
                    case "Aggression":
                        chr.Aggression = attr.InnerText;
                        break;
                    case "Confidence":
                        chr.Confidence = attr.InnerText;
                        break;
                    case "Conscientiousness":
                        chr.Conscientiousness = attr.InnerText;
                        break;
                    case "Creativity":
                        chr.Creativity = attr.InnerText;
                        break;
                    case "Dominance":
                        chr.Dominance = attr.InnerText;
                        break;
                    case "Enthusiasm":
                        chr.Enthusiasm = attr.InnerText;
                        break;
                    case "Assurance":
                        chr.Assurance = attr.InnerText;
                        break;
                    case "Sensitivity":
                        chr.Sensitivity = attr.InnerText;
                        break;
                    case "Shrewdness":
                        chr.Shrewdness = attr.InnerText;
                        break;
                    case "Sociability":
                        chr.Sociability = attr.InnerText;
                        break;
                    case "Stability":
                        chr.Stability = attr.InnerText;
                        break;
                    case "CharacterSketch":
                        chr.CharacterSketch = attr.InnerText;
                        break;
                    case "PhysNotes":
                        chr.PhysNotes = attr.InnerText;
                        break;
                    case "Appearance":
                        chr.Appearance = attr.InnerText;
                        break;
                    case "Economic":
                        chr.Economic = attr.InnerText;
                        break;
                    case "Education,":
                        chr.Education = attr.InnerText;
                        break;
                    case "Ethnic":
                        chr.Ethnic = attr.InnerText;
                        break;
                    case "Religion":
                        chr.Religion = attr.InnerText;
                        break;
                    case "PsychNotes":
                        chr.PsychNotes = attr.InnerText;
                        break;
                    case "Work":
                        chr.Work = attr.InnerText;
                        break;
                    case "Likes":
                        chr.Likes = attr.InnerText;
                        break;
                    case "Habits":
                        chr.Habits = attr.InnerText;
                        break;
                    case "Abilities":
                        chr.Abilities = attr.InnerText;
                        break;
                    case "Flaw":
                        chr.Flaw = attr.InnerText;
                        break;
                    case "BackStory":
                        chr.BackStory = attr.InnerText;
                        break;
                }
            }
        }

        private void ParseSetting(IXmlNode xn)
        {
            var loc = new SettingModel(xn, _model);
            foreach (var attr in xn.Attributes)
            {
                switch (attr.NodeName)
                {
                    case ("UUID"):
                        break;
                    case ("Name"):
                        loc.Name = attr.InnerText;
                        break;
                    case ("Id"):
                        loc.Id = Convert.ToInt32(attr.InnerText);
                        break;
                    case ("Locale"):
                        loc.Locale = attr.InnerText;
                        break;
                    case ("Season"):
                        loc.Season = attr.InnerText;
                        break;
                    case ("Period"):
                        loc.Period = attr.InnerText;
                        break;
                    case ("Lighting"):
                        loc.Lighting = attr.InnerText;
                        break;
                    case ("Weather"):
                        loc.Weather = attr.InnerText;
                        break;
                    case ("Temperature"):
                        loc.Temperature = attr.InnerText;
                        break;
                    case ("Prop1"):
                        loc.Prop1 = attr.InnerText;
                        break;
                    case ("Prop2"):
                        loc.Prop2 = attr.InnerText;
                        break;
                    case ("Prop3"):
                        loc.Prop3 = attr.InnerText;
                        break;
                    case ("Prop4"):
                        loc.Prop4 = attr.InnerText;
                        break;
                    case "Summary":
                        loc.Summary = attr.InnerText;
                        break;
                    case "Sights":
                        loc.Sights = attr.InnerText;
                        break;
                    case "Sounds":
                        loc.Sounds = attr.InnerText;
                        break;
                    case "Touch":
                        loc.Touch = attr.InnerText;
                        break;
                    case "SmellTaste":
                        loc.SmellTaste = attr.InnerText;
                        break;
                    case "Notes":
                        loc.Notes = attr.InnerText;
                        break;
                }
            }

        }

        private void ParsePlotPoint(IXmlNode xn)
        {
            var scene = new PlotPointModel(xn, _model);
            var castMembers = xn.SelectSingleNode("./CastMembers");
            if (castMembers != null)
                foreach (IXmlNode child in castMembers.ChildNodes)
                    if (child.NodeName.Equals("Member"))
                        scene.CastMembers.Add(child.InnerText);
            string member = string.Empty;
            foreach (var attr in xn.Attributes)
            {
                switch (attr.NodeName)
                {
                    case ("UUID"):
                        break;
                    case ("Name"):
                        scene.Name = attr.InnerText;
                        break;
                    case ("Id"):
                        scene.Id = Convert.ToInt32(attr.InnerText);
                        break;
                    case ("Viewpoint"):
                        scene.Viewpoint = attr.InnerText;
                        break;
                    case ("Date"):
                        scene.Date = attr.InnerText;
                        break;
                    case ("Time"):
                        scene.Time = attr.InnerText;
                        break;
                    case ("Setting"):
                        scene.Setting = attr.InnerText;
                        break;
                    case ("SceneType"):
                        scene.SceneType = attr.InnerText;
                        break;
                    case ("Char1"):  // legacy
                        member = attr.InnerText;
                        if (!member.Equals(string.Empty))
                            scene.CastMembers.Add(member);
                        //scene.Char1 = attr.InnerText;
                        break;
                    case ("Char2"): // legacy
                        member = attr.InnerText;
                        if (!member.Equals(string.Empty))
                            scene.CastMembers.Add(member);
                        //scene.Char2 = attr.InnerText;
                        break;
                    case ("Char3"): // legacy
                        member = attr.InnerText;
                        if (!member.Equals(string.Empty))
                            scene.CastMembers.Add(member);
                        //scene.Char3 = attr.InnerText;
                        break;
                    case ("Role1"):  // legacy
                        //scene.Role1 = attr.InnerText;
                        break;
                    case ("Role2"):  // legacy
                        //scene.Role2 = attr.InnerText;
                        break;
                    case ("Role3"):  // legacy
                        //scene.Role3 = attr.InnerText;
                        break;
                    case ("Protagonist"):
                        scene.Protagonist = attr.InnerText;
                        break;
                    case ("ProtagEmotion"):
                        scene.ProtagEmotion = attr.InnerText;
                        break;
                    case ("ProtagGoal"):
                        scene.ProtagGoal = attr.InnerText;
                        break;
                    case ("Antagonist"):
                        scene.Antagonist = attr.InnerText;
                        break;
                    case ("AntagEmotion"):
                        scene.AntagEmotion = attr.InnerText;
                        break;
                    case ("AntagGoal"):
                        scene.AntagGoal = attr.InnerText;
                        break;
                    case ("Opposition"):
                        scene.Opposition = attr.InnerText;
                        break;
                    case ("Outcome"):
                        scene.Outcome = attr.InnerText;
                        break;
                    case ("Emotion"):
                        scene.Emotion = attr.InnerText;
                        break;
                    case ("NewGoal"):
                        scene.NewGoal = attr.InnerText;
                        break;
                    case ("ScenePurpose"):
                        scene.ScenePurpose = attr.InnerText;
                        break;
                    case ("ValueExchange"):
                        scene.ValueExchange = attr.InnerText;
                        break;
                    case "Remarks":
                        scene.Remarks = attr.InnerText;
                        break;
                    case "Events":
                        scene.Events = attr.InnerText;
                        break;
                    case "Consequences":
                        scene.Consequences = attr.InnerText;
                        break;
                    case "Significance":
                        scene.Significance = attr.InnerText;
                        break;
                    case "Realization":
                        scene.Realization = attr.InnerText;
                        break;
                    case "Review":
                        scene.Review = attr.InnerText;
                        break;
                    case "Notes":
                        scene.Notes = attr.InnerText;
                        break;
                }
            }

        }

        private void ParseFolder(IXmlNode xn)
        {
            var folder = new FolderModel(xn, _model);
            foreach (var attr in xn.Attributes)
            {
                switch (attr.NodeName)
                {
                    case ("UUID"):
                        break;
                    case ("Name"):
                        folder.Name = attr.InnerText;
                        break;
                    case "Notes":
                        folder.Notes = attr.InnerText;
                        break;
                }
            }
        }

        private void ParseSection(IXmlNode xn)
        {
            var section = new SectionModel(xn, _model);
            foreach (var attr in xn.Attributes)
            {
                switch (attr.NodeName)
                {
                    case ("UUID"):
                        break;
                    case ("Name"):
                        section.Name = attr.InnerText;
                        break;
                    case "Notes":
                        section.Notes = attr.InnerText;
                        break;
                }
            }
        }

        private void ParseTrashCan(IXmlNode xn)
        {
            var trash = new TrashCanModel(xn, _model);
            foreach (var attr in xn.Attributes)
            {
                switch (attr.NodeName)
                {
                    case ("UUID"):
                        break;
                    case ("Name"):
                        trash.Name = attr.InnerText;
                        break;
                }
            }
        }

        private void ParseStoryExplorer(IXmlNode explorerNode)
        {
            foreach (IXmlNode child in explorerNode.SelectNodes("StoryNode"))
                RecurseExplorerNode(null, child, true);
        }

        private void RecurseExplorerNode(StoryNodeItem parent, IXmlNode xn, bool root)
        {
            StoryNodeItem node = new StoryNodeItem(parent, xn);
            if (root) _model.ExplorerView.Add(node);

            XmlNodeList children = xn.SelectNodes("StoryNode");
            foreach (IXmlNode child in children)
                RecurseExplorerNode(node, child, false);
        }

        private void ParseStoryNarrator(IXmlNode narratorNode)
        {
            foreach (IXmlNode child in narratorNode.SelectNodes("StoryNode"))
                RecurseNarratorNode(null, child, true);
        }

        private void RecurseNarratorNode(StoryNodeItem parent, IXmlNode xn, bool root)
        {
            StoryNodeItem node = new StoryNodeItem(parent, xn);
            if (root) _model.NarratorView.Add(node);

            XmlNodeList children = xn.SelectNodes("StoryNode");
            foreach (IXmlNode child in children)
                RecurseNarratorNode(node, child, false);
        }

        /// <summary>
        /// An RTF text field, if it's longer than 2K, will have been written to a
        /// separate text file in a subfolder for the Story Element it's a part of.
        /// If that's the case, the text field will contain the text file's filename
        /// as an imbedded string in the form [FILE:filename.rtf]
        /// </summary>
        /// <param name="note">the .stbx file's rtf text field or file reference</param>
        /// <param name="uuid">StoryElement uuid (also the subfolder name) </param>
        /// <returns>the rtf text field</returns>
        public async Task<string> GetRtfText(string note, Guid uuid)
        {
            // If it's just text, and not an imbedded file reference,
            // return the text
            if (!note.StartsWith("[FILE:"))
                return note;
            // Otherwise read and return the imbedded file from its subfolder
            char[] separator = { '[', ']', ':' };
            string[] result = note.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            string filename = result[1];
            StorageFolder folder = await FindSubFolder(uuid);
            StorageFile rtfFile =
                await folder.GetFileAsync(filename);
            return await FileIO.ReadTextAsync(rtfFile);
        }
        public async Task<string> ReadRtfText(Guid uuid, string rftFilename)
        {
            StorageFolder folder = await FindSubFolder(uuid);
            StorageFile rtfFile = await folder.GetFileAsync(rftFilename);
            return await FileIO.ReadTextAsync(rtfFile);
        }

        /// <summary>
        /// Locate or create a Directory for a StoryElement based on its GUID
        /// </summary>
        /// <param name="uuid">The GUID of a text node</param>
        /// <returns>StorageFolder instance for the StoryElement's folder</returns>
        private async Task<StorageFolder> FindSubFolder(Guid uuid)
        {
            // Search the ProjectFolder's subfolders for the SubFolder
            IReadOnlyList<StorageFolder> folders = await _story.FilesFolder.GetFoldersAsync();
            foreach (StorageFolder folder in folders)
                if (folder.Name.Equals(UuidString(uuid)))
                    //Story.SubFolder = folder;
                    return folder;

            // If the SubFolder doesn't exist, create it.
            StorageFolder newFolder = await _story.FilesFolder.CreateFolderAsync(UuidString(uuid));
            return newFolder;
        }

        #region Constructor
        public StoryReader()
        {
            _story = Ioc.Default.GetService<StoryController>();
            Logger = Ioc.Default.GetService<LogService>();

            XmlDocument doc = new XmlDocument();
            _xml = doc;
            //Model.Changed = false;
        }

        #endregion

        /// <summary>
        /// Generate a GUID string in the Scrivener format (i.e., without curly braces)
        /// </summary>
        /// <returns>string UUID representation</returns>
        public static string UuidString(Guid uuid)
        {
            string id = uuid.ToString("B").ToUpper();
            id = id.Replace("{", string.Empty);
            id = id.Replace("}", string.Empty);
            return id;
        }
    }
}
