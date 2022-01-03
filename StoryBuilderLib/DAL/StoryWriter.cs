using StoryBuilder.Models;
using StoryBuilder.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Windows.Storage;

namespace StoryBuilder.DAL
{
    /// <summary>
    /// StoryWriter parses StoryBuilder's model and writes it to its backing store
    /// (the .stbx file), which is an Xml Document. 
    /// </summary>
    public class StoryWriter
    {
        //TODO: Move System.IO stuff to a DAL microservice?

        /// StoryBuilder's model is found in the StoryBuilder.Models namespace and consists
        /// of various POCO (Plain Old CLR) objects.
        ///
        private StoryModel _model;

        /// The in-memory representation of the .stbx file is an XmlDocument
        /// and its various components are all XmlNodes.
        /// p
        private XmlDocument _xml;

        private StorageFile _outFile;

        /// Story contains a collection of StoryElements, a representation of the StoryExplorer tree,
        /// a representation of the StoryNarrator tree, and a collection of story-specific settings.
        private XmlNode _elements;    // The collection of StoryElements
        private XmlNode _explorer;    // StoryExplorer
        private XmlNode _narrator;    // StoryNarrator
        private XmlNode _relationships; // Character Relationships
        private XmlNode _stbSettings; // Settings

        public StoryWriter()
        {
        }

        

        internal async Task WriteFile(StorageFile output, StoryModel model)
        {
            _outFile = output;
            _model = model;
              _xml = new XmlDocument();
            CreateStoryDocument();
            //      write RTF if converting. 
            ParseStoryElementsAsync();
            ParseExplorerView();
            ParseNarratorView();

            await using (Stream fileStream = await _outFile.OpenStreamForWriteAsync())
            {
                XmlWriterSettings settings = new();
                settings.Async = true;
                settings.Encoding = Encoding.UTF8;
                settings.Indent = true;
                XmlWriter writer = XmlWriter.Create(fileStream, settings);

                _xml.Save(writer);
            }

            _model.Changed = false;
            GlobalData.StoryModel.ProjectFolder = await output.GetParentAsync();
        }

        private void CreateStoryDocument()
        {
            XmlNode docNode = _xml.CreateXmlDeclaration("1.0", "UTF-8", null);
            _xml.AppendChild(docNode);
            // Create StoryBuilder node with version
            XmlNode stb = _xml.CreateElement("StoryBuilder");
            //Create an attribute.
            XmlAttribute attr = _xml.CreateAttribute("Version");
            attr.Value = "2.0";
            stb.Attributes.Append(attr);
            _xml.AppendChild(stb);
            _elements = _xml.CreateElement("StoryElements");
            stb.AppendChild(_elements);
            _explorer = _xml.CreateElement("Explorer");
            stb.AppendChild(_explorer);
            _narrator = _xml.CreateElement("Narrator");
            stb.AppendChild(_narrator);
            _relationships = _xml.CreateElement("Relationships");
            stb.AppendChild(_relationships);

            _stbSettings = _xml.CreateElement("Settings");
            stb.AppendChild(_stbSettings);
        }

        private void ParseStoryElementsAsync()
        {
            foreach (StoryElement element in _model.StoryElements)
            {
                switch (element.Type)
                {
                    case StoryItemType.StoryOverview:
                        ParseOverViewElement(element);
                        break;
                    case StoryItemType.Problem:
                        ParseProblemElement(element);
                        break;
                    case StoryItemType.Character:
                        ParseCharacterElement(element);
                        break;
                    case StoryItemType.Setting:
                        ParseSettingElement(element);
                        break;
                    case StoryItemType.Scene:
                        ParseSceneElement(element);
                        break;
                    case StoryItemType.Folder:
                        ParseFolderElement(element);
                        break;
                    case StoryItemType.Section:
                        ParseSectionElement(element);
                        break;
                    case StoryItemType.TrashCan:
                        ParseTrashCanElement(element);
                        break;
                }
            }
        }

        private void ParseOverViewElement(StoryElement element)
        {
            OverviewModel rec = (OverviewModel)element;
            XmlNode overview = _xml.CreateElement("Overview");

            XmlAttribute attr = _xml.CreateAttribute("UUID");
            attr.Value = UuidString(rec.Uuid);
            overview.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Name");
            attr.Value = rec.Name;
            overview.Attributes.Append(attr);
            attr = _xml.CreateAttribute("DateCreated");
            attr.Value = rec.DateCreated;
            overview.Attributes.Append(attr);
            attr = _xml.CreateAttribute("DateModified");
            attr.Value = rec.DateModified;
            overview.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Author");
            attr.Value = rec.Author;
            overview.Attributes.Append(attr);
            attr = _xml.CreateAttribute("StoryType");
            attr.Value = rec.StoryType;
            overview.Attributes.Append(attr);
            attr = _xml.CreateAttribute("StoryGenre");
            attr.Value = rec.StoryGenre;
            overview.Attributes.Append(attr);
            attr = _xml.CreateAttribute("ViewPoint");
            attr.Value = rec.Viewpoint;
            overview.Attributes.Append(attr);
            attr = _xml.CreateAttribute("ViewpointCharacter");
            attr.Value = rec.ViewpointCharacter;
            overview.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Voice");
            attr.Value = rec.Voice;
            overview.Attributes.Append(attr);
            attr = _xml.CreateAttribute("LiteraryDevice");
            attr.Value = rec.LiteraryDevice;
            overview.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Tense");
            attr.Value = rec.Tense;
            overview.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Style");
            attr.Value = rec.Style;
            overview.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Tone");
            attr.Value = rec.Tone;
            overview.Attributes.Append(attr);
            attr = _xml.CreateAttribute("StoryIdea");
            attr.Value = rec.StoryIdea;
            overview.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Concept");
            attr.Value = rec.Concept;
            overview.Attributes.Append(attr);
            attr = _xml.CreateAttribute("StoryProblem");
            attr.Value = rec.StoryProblem;
            overview.Attributes.Append(attr);
            attr = _xml.CreateAttribute("StyleNotes");
            attr.Value = rec.StyleNotes;
            overview.Attributes.Append(attr);
            attr = _xml.CreateAttribute("ToneNotes");
            attr.Value = rec.ToneNotes;
            overview.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Notes");
            attr.Value = rec.Notes;
            overview.Attributes.Append(attr);

            _elements.AppendChild(overview);
        }

        private void ParseProblemElement(StoryElement element)
        {
            ProblemModel rec = (ProblemModel)element;
            XmlNode prob = _xml.CreateElement("Problem");

            XmlAttribute attr = _xml.CreateAttribute("UUID");
            attr.Value = UuidString(rec.Uuid);
            prob.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Name");
            attr.Value = rec.Name;
            prob.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Id");
            attr.Value = rec.Id.ToString();
            prob.Attributes.Append(attr);
            attr = _xml.CreateAttribute("ProblemType");
            attr.Value = rec.ProblemType;
            prob.Attributes.Append(attr);
            attr = _xml.CreateAttribute("ConflictType");
            attr.Value = rec.ConflictType;
            prob.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Subject");
            attr.Value = rec.Subject;
            prob.Attributes.Append(attr);
            attr = _xml.CreateAttribute("ProblemSource");
            attr.Value = rec.ProblemSource;
            prob.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Protagonist");
            attr.Value = rec.Protagonist;
            prob.Attributes.Append(attr);
            attr = _xml.CreateAttribute("ProtGoal");
            attr.Value = rec.ProtGoal;
            prob.Attributes.Append(attr);
            attr = _xml.CreateAttribute("ProtMotive");
            attr.Value = rec.ProtMotive;
            prob.Attributes.Append(attr);
            attr = _xml.CreateAttribute("ProtConflict");
            attr.Value = rec.ProtConflict;
            prob.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Antagonist");
            attr.Value = rec.Antagonist;
            prob.Attributes.Append(attr);
            attr = _xml.CreateAttribute("AntagGoal");
            attr.Value = rec.AntagGoal;
            prob.Attributes.Append(attr);
            attr = _xml.CreateAttribute("AntagMotive");
            attr.Value = rec.AntagMotive;
            prob.Attributes.Append(attr);
            attr = _xml.CreateAttribute("AntagConflict");
            attr.Value = rec.AntagConflict;
            prob.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Outcome");
            attr.Value = rec.Outcome;
            prob.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Method");
            attr.Value = rec.Method;
            prob.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Theme");
            attr.Value = rec.Theme;
            prob.Attributes.Append(attr);

            attr = _xml.CreateAttribute("StoryQuestion");
            attr.Value = rec.StoryQuestion;
            prob.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Premise");
            attr.Value = rec.Premise;
            prob.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Notes");
            attr.Value = rec.Notes;
            prob.Attributes.Append(attr);

            _elements.AppendChild(prob);
        }

        private void ParseCharacterElement(StoryElement element)
        {
            CharacterModel rec = (CharacterModel)element;
            XmlNode chr = _xml.CreateElement("Character");

            XmlAttribute attr = _xml.CreateAttribute("UUID");
            attr.Value = UuidString(rec.Uuid);
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Name");
            attr.Value = rec.Name;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Id");
            attr.Value = rec.Id.ToString();
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Role");
            attr.Value = rec.Role;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("StoryRole");
            attr.Value = rec.StoryRole;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Archetype");
            attr.Value = rec.Archetype;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Age");
            attr.Value = rec.Age;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Sex");
            attr.Value = rec.Sex;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Eyes");
            attr.Value = rec.Eyes;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Hair");
            attr.Value = rec.Hair;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Weight");
            attr.Value = rec.Weight;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("CharHeight");
            attr.Value = rec.CharHeight;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Build");
            attr.Value = rec.Build;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Complexion");
            attr.Value = rec.Complexion;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Race");
            attr.Value = rec.Race;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Nationality");
            attr.Value = rec.Nationality;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Health");
            attr.Value = rec.Health;
            chr.Attributes.Append(attr);

            XmlNode relationshipList = _xml.CreateElement("Relationships");
            foreach (RelationshipModel relation in rec.RelationshipList)
            {
                XmlElement relNode = _xml.CreateElement("Relation");
                attr = _xml.CreateAttribute("Partner");
                attr.Value = relation.PartnerUuid;
                relNode.Attributes.Append(attr);
                attr = _xml.CreateAttribute("RelationType");
                attr.Value = relation.RelationType;
                relNode.Attributes.Append(attr);
                attr = _xml.CreateAttribute("Trait");
                attr.Value = relation.Trait;
                relNode.Attributes.Append(attr);
                attr = _xml.CreateAttribute("Attitude");
                attr.Value = relation.Attitude;
                relNode.Attributes.Append(attr);
                attr = _xml.CreateAttribute("Notes");
                attr.Value = relation.Notes;
                relNode.Attributes.Append(attr);
                relationshipList.AppendChild(relNode);
            }
            chr.AppendChild(relationshipList);

            attr = _xml.CreateAttribute("Enneagram");
            attr.Value = rec.Enneagram;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Intelligence");
            attr.Value = rec.Intelligence;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Values");
            attr.Value = rec.Values;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Abnormality");
            attr.Value = rec.Abnormality;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Focus");
            attr.Value = rec.Focus;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Adventureousness");
            attr.Value = rec.Adventureousness;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Aggression");
            attr.Value = rec.Aggression;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Confidence");
            attr.Value = rec.Confidence;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Conscientiousness");
            attr.Value = rec.Conscientiousness;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Creativity");
            attr.Value = rec.Creativity;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Dominance");
            attr.Value = rec.Dominance;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Enthusiasm");
            attr.Value = rec.Enthusiasm;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Assurance");
            attr.Value = rec.Assurance;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Sensitivity");
            attr.Value = rec.Sensitivity;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Shrewdness");
            attr.Value = rec.Shrewdness;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Sociability");
            attr.Value = rec.Sociability;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Stability");
            attr.Value = rec.Stability;
            chr.Attributes.Append(attr);
            XmlNode traitList = _xml.CreateElement("CharacterTraits");
            foreach (string member in rec.TraitList)
            {
                XmlElement trait = _xml.CreateElement("Trait");
                trait.AppendChild(_xml.CreateTextNode(member));
                traitList.AppendChild(trait);
            }
            chr.AppendChild(traitList);
            attr = _xml.CreateAttribute("CharacterSketch");
            attr.Value = rec.CharacterSketch;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("PhysNotes");
            attr.Value = rec.PhysNotes;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Appearance");
            attr.Value = rec.Appearance;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Economic");
            attr.Value = rec.Economic;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Education");
            attr.Value = rec.Education;
            attr = _xml.CreateAttribute("Ethnic");
            attr.Value = rec.Ethnic;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Religion");
            attr.Value = rec.Religion;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("PsychNotes");
            attr.Value = rec.PsychNotes;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Notes");
            attr.Value = rec.Notes;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Flaw");
            attr.Value = rec.Flaw;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("BackStory");
            attr.Value = rec.BackStory;
            chr.Attributes.Append(attr);

            _elements.AppendChild(chr);
        }

        private void ParseSettingElement(StoryElement element)
        {
            SettingModel rec = (SettingModel)element;
            XmlNode loc = _xml.CreateElement("Setting");
            XmlAttribute attr;

            attr = _xml.CreateAttribute("UUID");
            attr.Value = UuidString(rec.Uuid);
            loc.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Name");
            attr.Value = rec.Name;
            loc.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Id");
            attr.Value = rec.Id.ToString();
            loc.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Locale");
            attr.Value = rec.Locale;
            loc.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Season");
            attr.Value = rec.Season;
            loc.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Period");
            attr.Value = rec.Period;
            loc.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Lighting");
            attr.Value = rec.Lighting;
            loc.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Weather");
            attr.Value = rec.Weather;
            loc.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Temperature");
            attr.Value = rec.Temperature;
            loc.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Prop1");
            attr.Value = rec.Prop1;
            loc.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Prop2");
            attr.Value = rec.Prop2;
            loc.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Prop3");
            attr.Value = rec.Prop3;
            loc.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Prop4");
            attr.Value = rec.Prop4;
            loc.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Summary");
            attr.Value = rec.Summary;
            loc.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Sights");
            attr.Value = rec.Sights;
            loc.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Sounds");
            attr.Value = rec.Sounds;
            loc.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Touch");
            attr.Value = rec.Touch;
            loc.Attributes.Append(attr);
            attr = _xml.CreateAttribute("SmellTaste");
            attr.Value = rec.SmellTaste;
            loc.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Notes");
            attr.Value = rec.Notes;
            loc.Attributes.Append(attr);

            _elements.AppendChild(loc);
        }

        private void ParseSceneElement(StoryElement element)
        {
            SceneModel rec = (SceneModel)element;
            XmlNode scene = _xml.CreateElement("Scene");
            XmlAttribute attr;

            attr = _xml.CreateAttribute("UUID");
            attr.Value = UuidString(rec.Uuid);
            scene.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Name");
            attr.Value = rec.Name;
            scene.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Id");
            attr.Value = rec.Id.ToString();
            scene.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Viewpoint");
            attr.Value = rec.Viewpoint;
            scene.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Date");
            attr.Value = rec.Date;
            scene.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Time");
            attr.Value = rec.Time;
            scene.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Setting");
            attr.Value = rec.Setting;
            scene.Attributes.Append(attr);
            attr = _xml.CreateAttribute("SceneType");
            attr.Value = rec.SceneType;
            scene.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Char1");
            attr.Value = rec.Char1;
            scene.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Char2");
            attr.Value = rec.Char2;
            scene.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Char3");
            attr.Value = rec.Char3;
            scene.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Role1");
            attr.Value = rec.Role1;
            scene.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Role2");
            attr.Value = rec.Role2;
            scene.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Role3");
            attr.Value = rec.Role3;
            scene.Attributes.Append(attr);
            XmlNode castList = _xml.CreateElement("CastMembers");
            foreach (string member in rec.CastMembers)
            {
                XmlElement castMember = _xml.CreateElement("Member");
                castMember.AppendChild(_xml.CreateTextNode(member));
                castList.AppendChild(castMember);
            }
            scene.AppendChild(castList);
            attr = _xml.CreateAttribute("Remarks");
            attr.Value = rec.Remarks;
            scene.Attributes.Append(attr);
            attr = _xml.CreateAttribute("ScenePurpose");
            attr.Value = rec.ScenePurpose;
            scene.Attributes.Append(attr);
            attr = _xml.CreateAttribute("ValueExchange");
            attr.Value = rec.ValueExchange;
            scene.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Protagonist");
            attr.Value = rec.Protagonist;
            scene.Attributes.Append(attr);
            attr = _xml.CreateAttribute("ProtagEmotion");
            attr.Value = rec.ProtagEmotion;
            scene.Attributes.Append(attr);
            attr = _xml.CreateAttribute("ProtagGoal");
            attr.Value = rec.ProtagGoal;
            scene.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Antagonist");
            attr.Value = rec.Antagonist;
            scene.Attributes.Append(attr);
            attr = _xml.CreateAttribute("AntagEmotion");
            attr.Value = rec.AntagEmotion;
            scene.Attributes.Append(attr);
            attr = _xml.CreateAttribute("AntagGoal");
            attr.Value = rec.AntagGoal;
            scene.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Opposition");
            attr.Value = rec.Opposition;
            scene.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Outcome");
            attr.Value = rec.Outcome;
            scene.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Emotion");
            attr.Value = rec.Emotion;
            scene.Attributes.Append(attr);
            attr = _xml.CreateAttribute("NewGoal");
            attr.Value = rec.NewGoal;
            scene.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Remarks");
            attr.Value = rec.Remarks;
            scene.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Events");
            attr.Value = rec.Events;
            scene.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Consequences");
            attr.Value = rec.Consequences;
            scene.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Significance");
            attr.Value = rec.Significance;
            scene.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Realization");
            attr.Value = rec.Realization;
            scene.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Review");
            attr.Value = rec.Review;
            scene.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Notes");
            attr.Value = rec.Notes;
            scene.Attributes.Append(attr);

            _elements.AppendChild(scene);
        }

        private void ParseFolderElement(StoryElement element)
        {
            FolderModel rec = (FolderModel)element;
            XmlNode node = _xml.CreateElement("Folder");

            XmlAttribute attr = _xml.CreateAttribute("UUID");
            attr.Value = UuidString(rec.Uuid);
            node.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Name");
            attr.Value = rec.Name;
            node.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Notes");
            attr.Value = rec.Notes;
            node.Attributes.Append(attr);

            _elements.AppendChild(node);
        }

        private void ParseSectionElement(StoryElement element)
        {
            SectionModel rec = (SectionModel)element;
            XmlNode node = _xml.CreateElement("Section");

            XmlAttribute attr = _xml.CreateAttribute("UUID");
            attr.Value = UuidString(rec.Uuid);
            node.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Name");
            attr.Value = rec.Name;
            node.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Notes");
            attr.Value = rec.Notes;
            node.Attributes.Append(attr);

            _elements.AppendChild(node);
        }

        private void ParseTrashCanElement(StoryElement element)
        {
            TrashCanModel rec = (TrashCanModel)element;
            XmlNode node = _xml.CreateElement("TrashCan");

            XmlAttribute attr = _xml.CreateAttribute("UUID");
            attr.Value = UuidString(rec.Uuid);
            node.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Name");
            attr.Value = rec.Name;
            node.Attributes.Append(attr);

            _elements.AppendChild(node);
        }

        private void ParseExplorerView()
        {
            foreach (StoryNodeItem root in _model.ExplorerView)
            {
                root.IsRoot = true;
                XmlElement rootElement = RecurseCreateXmlElement(null, root);
                _explorer.AppendChild(rootElement);
            }
        }

        private void ParseNarratorView()
        {
            foreach (StoryNodeItem root in _model.NarratorView)
            {
                root.IsRoot = true;
                XmlElement rootElement = RecurseCreateXmlElement(null, root);
                _narrator.AppendChild(rootElement);
            }
        }

        /// <summary>
        /// Create the TreeView's Xml equivalent of the StoryNodeModel
        /// via recursive descent. This is ran once for the Explorer
        /// view and again for the Narrator view.
        /// 
        /// Since it's a TreeView node model, all the XML version of the
        /// node needs is to point to its StoryElement model and its
        /// children.
        /// </summary>
        /// <param name="parent">An XmlElement to add this under </param>
        /// <param name="node">A StoryNodeModel node</param>
        /// <returns>XnlElement version of the Treeview node</returns>
        public XmlElement RecurseCreateXmlElement(XmlElement parent, StoryNodeItem node)
        {
            XmlElement element = _xml.CreateElement("StoryNode");
            // Set attributes
            element.SetAttribute("UUID", UuidString(node.Uuid));
            element.SetAttribute("Type", node.Type.ToString("g"));
            element.SetAttribute("Name", node.Name);
            // Add MetaData 
            element.SetAttribute("IsExpanded", node.IsExpanded.ToString());
            element.SetAttribute("IsSelected", node.IsSelected.ToString());
            element.SetAttribute("IsRoot", node.IsRoot.ToString());
            parent?.AppendChild(element);
            // Traverse and create XmlNodes for the binderNode's child node subtree
            foreach (StoryNodeItem child in node.Children)
            {
                RecurseCreateXmlElement(element, child);
            }
            return element;
        }

        public async Task<string> PutRtfText(string note, Guid uuid, string filename)
        {
            char[] endchars = { ' ', (char)0 };      // remove trialing zero from RichEditText
            string work = note.TrimEnd(endchars);
            // If the note already contains an imbedded file reference, we're done
            if (note.StartsWith("[FILE:"))
                return work;
            //TODO: Make external RTF file size a Preference
            if (note.Length > 1024)
            {
                StorageFolder folder = await FindSubFolder(uuid);
                StorageFile rtfFile = await folder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
                await using (Stream stream = await rtfFile.OpenStreamForWriteAsync())
                {
                    await using (StreamWriter writer = new(stream)) {await writer.WriteAsync(work);}
                }
                return $"[FILE:{filename}]";
            }
            return work;
        }
        public async Task<string> ReadRtfText(Guid uuid, string rftFilename)
        {
            StorageFolder folder = await FindSubFolder(uuid);
            StorageFile rtfFile =
                await folder.GetFileAsync(rftFilename);
            return await FileIO.ReadTextAsync(rtfFile);
        }
        public async Task WriteRtfText(Guid uuid, string rftFilename, string notes)
        {
            // //https://stackoverflow.com/questions/32948609/how-to-close-the-opened-storagefile
            StorageFolder folder = await FindSubFolder(uuid);
            StorageFile rtfFile = await folder.CreateFileAsync(rftFilename, CreationCollisionOption.ReplaceExisting);
            await using Stream stream = await rtfFile.OpenStreamForWriteAsync();
            await using StreamWriter writer = new(stream);
            await writer.WriteAsync(notes);
        }

        /// <summary>
        /// Locate or create a Directory for a StoryElement based on its GUID
        /// </summary>
        /// <param name="uuid">The GUID of a text node</param>
        /// <returns>StorageFolder instance for the StoryElement's folder</returns>
        private async Task<StorageFolder> FindSubFolder(Guid uuid)
        {
            // Search the ProjectFolder's subfolders for the desired one (folder name == uuid as string)
            IReadOnlyList<StorageFolder> folders = await GlobalData.StoryModel.FilesFolder.GetFoldersAsync();
            foreach (StorageFolder folder in folders)
                if (folder.Name.Equals(UuidString(uuid)))
                    return folder;
            // If the SubFolder doesn't exist, create it.
            StorageFolder newFolder = await GlobalData.StoryModel.FilesFolder.CreateFolderAsync(UuidString(uuid));
            return newFolder;
        }

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
