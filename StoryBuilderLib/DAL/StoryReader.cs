using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using StoryBuilder.Models;
using StoryBuilder.Services.Logging;
using StoryBuilder.Services.Messages;
using StoryBuilder.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
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
                _model.ProjectFile = file;
                _model.ProjectFilename = file.Name;
                _model.ProjectFolder = await file.GetParentAsync();
                _model.ProjectPath = _model.ProjectFolder.Path;
                _model.ProjectFilename = Path.GetFileName(file.Path);
                _model.FilesFolder = await _model.ProjectFolder.CreateFolderAsync("files", CreationCollisionOption.OpenIfExists);
                // Early story outlines may have been built or converted
                // without a TrashCan node added. If this model is one of 
                // those, add the node to both the Explorer and Narrator views.
                if (_model.ExplorerView.Count == 1)
                {
                    TrashCanModel trash = new(_model);
                    StoryNodeItem trashNode = new(trash, null);
                    _model.ExplorerView.Add(trashNode);     // The trashcan is the second root
                }
                if (_model.NarratorView.Count == 1)
                {
                    TrashCanModel trash = new(_model);
                    StoryNodeItem trashNode = new(trash, null);
                    _model.NarratorView.Add(trashNode);     // The trashcan is the second root
                }

                Messenger.Send(new StatusChangedMessage(new($"File load successful.", LogLevel.Info, true)));

                return _model;
            }
            catch (Exception ex)
            {
                Logger.LogException(LogLevel.Error, ex, "Error reading story");
                Messenger.Send(new StatusChangedMessage(new("Error reading story", LogLevel.Error)));
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
                foreach (IXmlNode xn in nodes)
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
            foreach (IXmlNode child in node.ChildNodes)
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
                        ParseScene(node);   // legacy; PlotPoint was renamed to Scene
                        break;
                    case "Scene":
                        ParseScene(node);
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
                foreach (IXmlNode child in node.ChildNodes)
                    RecurseStoryElement(child);
            }
        }

        private void ParseOverView(IXmlNode xn)
        {
            // There's one OverviewModel per story. Its corresponding
            //StoryNode is the root of the Explorer TreeView.
            _overview = new OverviewModel(xn, _model);
            foreach (IXmlNode attr in xn.Attributes)
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
                    case "StoryProblem":
                        _overview.StoryProblem = attr.InnerText;
                        break;
                    case "StructureNotes":
                        _overview.StructureNotes = attr.InnerText;
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
            ProblemModel prb = new(xn, _model);
            foreach (IXmlNode attr in xn.Attributes)
            {
                switch (attr.NodeName)
                {
                    case "UUID":
                        break;
                    case "Name":
                        prb.Name = attr.InnerText;
                        break;
                    case "ProblemType":
                        prb.ProblemType = attr.InnerText;
                        break;
                    case "ConflictType":
                        prb.ConflictType = attr.InnerText;
                        break;
                    case "Subject":
                        prb.Subject = attr.InnerText;
                        break;
                    case "ProblemSource":
                        prb.ProblemSource = attr.InnerText;
                        break;
                    case "Protagonist":
                        prb.Protagonist = attr.InnerText;
                        break;
                    case "ProtGoal":
                        prb.ProtGoal = attr.InnerText;
                        break;
                    case "ProtMotive":
                        prb.ProtMotive = attr.InnerText;
                        break;
                    case "ProtConflict":
                        prb.ProtConflict = attr.InnerText;
                        break;
                    case "Antagonist":
                        prb.Antagonist = attr.InnerText;
                        break;
                    case "AntagGoal":
                        prb.AntagGoal = attr.InnerText;
                        break;
                    case "AntagMotive":
                        prb.AntagMotive = attr.InnerText;
                        break;
                    case "AntagConflict":
                        prb.AntagConflict = attr.InnerText;
                        break;
                    case "Outcome":
                        prb.Outcome = attr.InnerText;
                        break;
                    case "Method":
                        prb.Method = attr.InnerText;
                        break;
                    case "Theme":
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
            CharacterModel chr = new(xn, _model);
            IXmlNode traitList = xn.SelectSingleNode("./CharacterTraits");
            if (traitList != null)
            {
                foreach (IXmlNode child in traitList.ChildNodes)
                    if (child.NodeName.Equals("Trait"))
                        chr.TraitList.Add(child.InnerText);
            }
            IXmlNode relationList = xn.SelectSingleNode("./Relationships");
            if (relationList != null)
            {
                foreach (IXmlNode child in relationList.ChildNodes)
                    if (child.NodeName.Equals("Relation"))
                        chr.RelationshipList.Add(new RelationshipModel(child));
            }
            foreach (IXmlNode attr in xn.Attributes)
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
                    case "Education":
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
                    case "Notes":
                        chr.Notes = attr.InnerText;
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
            SettingModel loc = new(xn, _model);
            foreach (IXmlNode attr in xn.Attributes)
            {
                switch (attr.NodeName)
                {
                    case "UUID":
                        break;
                    case "Name":
                        loc.Name = attr.InnerText;
                        break;
                    case "Id":
                        loc.Id = Convert.ToInt32(attr.InnerText);
                        break;
                    case "Locale":
                        loc.Locale = attr.InnerText;
                        break;
                    case "Season":
                        loc.Season = attr.InnerText;
                        break;
                    case "Period":
                        loc.Period = attr.InnerText;
                        break;
                    case "Lighting":
                        loc.Lighting = attr.InnerText;
                        break;
                    case "Weather":
                        loc.Weather = attr.InnerText;
                        break;
                    case "Temperature":
                        loc.Temperature = attr.InnerText;
                        break;
                    case "Prop1":
                        loc.Prop1 = attr.InnerText;
                        break;
                    case "Prop2":
                        loc.Prop2 = attr.InnerText;
                        break;
                    case "Prop3":
                        loc.Prop3 = attr.InnerText;
                        break;
                    case "Prop4":
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

        private void ParseScene(IXmlNode xn)
        {
            SceneModel scene = new(xn, _model);
            IXmlNode castMembers = xn.SelectSingleNode("./CastMembers");
            if (castMembers != null)
                foreach (IXmlNode child in castMembers.ChildNodes)
                    if (child.NodeName.Equals("Member"))
                        scene.CastMembers.Add(child.InnerText);
            foreach (IXmlNode attr in xn.Attributes)
            {
                switch (attr.NodeName)
                {
                    case "UUID":
                        break;
                    case "Name":
                        scene.Name = attr.InnerText;
                        break;
                    case "Id":
                        scene.Id = Convert.ToInt32(attr.InnerText);
                        break;
                    case "ViewpointCharacter":
                        scene.ViewpointCharacter = attr.InnerText;
                        break;
                    case "Date":
                        scene.Date = attr.InnerText;
                        break;
                    case "Time":
                        scene.Time = attr.InnerText;
                        break;
                    case "Setting":
                        scene.Setting = attr.InnerText;
                        break;
                    case "SceneType":
                        scene.SceneType = attr.InnerText;
                        break;
                    case "Protagonist":
                        scene.Protagonist = attr.InnerText;
                        break;
                    case "ProtagEmotion":
                        scene.ProtagEmotion = attr.InnerText;
                        break;
                    case "ProtagGoal":
                        scene.ProtagGoal = attr.InnerText;
                        break;
                    case "Antagonist":
                        scene.Antagonist = attr.InnerText;
                        break;
                    case "AntagEmotion":
                        scene.AntagEmotion = attr.InnerText;
                        break;
                    case "AntagGoal":
                        scene.AntagGoal = attr.InnerText;
                        break;
                    case "Opposition":
                        scene.Opposition = attr.InnerText;
                        break;
                    case "Outcome":
                        scene.Outcome = attr.InnerText;
                        break;
                    case "Emotion":
                        scene.Emotion = attr.InnerText;
                        break;
                    case "NewGoal":
                        scene.NewGoal = attr.InnerText;
                        break;
                    case "ScenePurpose":
                        scene.ScenePurpose = attr.InnerText;
                        break;
                    case "ValueExchange":
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
            FolderModel folder = new(xn, _model);
            foreach (IXmlNode attr in xn.Attributes)
            {
                switch (attr.NodeName)
                {
                    case "UUID":
                        break;
                    case "Name":
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
            SectionModel section = new(xn, _model);
            foreach (IXmlNode attr in xn.Attributes)
            {
                switch (attr.NodeName)
                {
                    case "UUID":
                        break;
                    case "Name":
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
            TrashCanModel trash = new(xn, _model);
            foreach (IXmlNode attr in xn.Attributes)
            {
                switch (attr.NodeName)
                {
                    case "UUID":
                        break;
                    case "Name":
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
            StoryNodeItem node = new(parent, xn);
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
            StoryNodeItem node = new(parent, xn);
            if (root) _model.NarratorView.Add(node);

            XmlNodeList children = xn.SelectNodes("StoryNode");
            foreach (IXmlNode child in children)
                RecurseNarratorNode(node, child, false);
        }

        #region Constructor
        public StoryReader()
        {
            Logger = Ioc.Default.GetService<LogService>();

            XmlDocument doc = new();
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

