using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.Controllers;
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
        private readonly StoryController _story;
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
            _story = Ioc.Default.GetService<StoryController>();
        }

        //internal async Task WriteFile(StorageFile output, StoryModel model)
        //{
        //    _outFile = output;
        //    _model = model;

        //    _xml = new XmlDocument();
        //    CreateStoryDocument();
        //    //      write RTF if converting. 
        //    await ParseStoryElementsAsync();
        //    ParseExplorerView();
        //    ParseNarratorView();
        //    ParseRelationships();
        //    using (Stream fileStream = await _outFile.OpenStreamForWriteAsync())
        //    {
        //        _xml.Save(fileStream);
        //    }
        //    _model.Changed = false;
        //    _story.ProjectFolder = await output.GetParentAsync();
        //}

        internal async Task WriteFile(StorageFile output, StoryModel model)
        {
            _outFile = output;
            _model = model;
                        _xml = new XmlDocument();
            CreateStoryDocument();
            //      write RTF if converting. 
            await ParseStoryElementsAsync();
            ParseExplorerView();
            ParseNarratorView();

            using (Stream fileStream = await _outFile.OpenStreamForWriteAsync())
            {
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Async = true;
                settings.Encoding = Encoding.UTF8;
                settings.Indent = true;
                XmlWriter writer = XmlWriter.Create(fileStream, settings);

                _xml.Save(writer);
            }

            _model.Changed = false;
            _story.ProjectFolder = await output.GetParentAsync();
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

        private async Task ParseStoryElementsAsync()
        {
            foreach (StoryElement element in _model.StoryElements)
            {
                switch (element.Type)
                {
                    case StoryItemType.StoryOverview:
                        await ParseOverViewElement(element);
                        break;
                    case StoryItemType.Problem:
                        await ParseProblemElement(element);
                        break;
                    case StoryItemType.Character:
                        await ParseCharacterElement(element);
                        break;
                    case StoryItemType.Setting:
                        await ParseSettingElement(element);
                        break;
                    case StoryItemType.PlotPoint:
                        await ParsePlotPointElement(element);
                        break;
                    case StoryItemType.Folder:
                        await ParseFolderElement(element);
                        break;
                    case StoryItemType.Section:
                        await ParseSectionElement(element);
                        break;
                    case StoryItemType.TrashCan:
                        ParseTrashCanElement(element);
                        break;
                }
            }
        }

        private async Task ParseOverViewElement(StoryElement element)
        {
            OverviewModel rec = (OverviewModel)element;
            XmlNode overview = _xml.CreateElement("Overview");
            XmlAttribute attr;

            attr = _xml.CreateAttribute("UUID");
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
            attr = _xml.CreateAttribute("TargetMarket1");
            attr.Value = rec.TargetMarket1;
            overview.Attributes.Append(attr);
            attr = _xml.CreateAttribute("TargetMarket2");
            attr.Value = rec.TargetMarket2;
            overview.Attributes.Append(attr);

            attr = _xml.CreateAttribute("StoryIdea");
            rec.StoryIdea = await PutRtfText(rec.StoryIdea, rec.Uuid, "storyidea.rtf");
            attr.Value = rec.StoryIdea;
            overview.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Concept");
            rec.Concept = await PutRtfText(rec.Concept, rec.Uuid, "concept.rtf");
            attr.Value = rec.Concept;
            overview.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Premise");
            rec.Premise = await PutRtfText(rec.Premise, rec.Uuid, "premise.rtf");
            attr.Value = rec.Premise;
            overview.Attributes.Append(attr);
            attr = _xml.CreateAttribute("StyleNotes");
            rec.StyleNotes = await PutRtfText(rec.StyleNotes, rec.Uuid, "stylenotes.rtf");
            attr.Value = rec.StyleNotes;
            overview.Attributes.Append(attr);
            attr = _xml.CreateAttribute("ToneNotes");
            rec.ToneNotes = await PutRtfText(rec.ToneNotes, rec.Uuid, "tonenotes.rtf");
            attr.Value = rec.ToneNotes;
            overview.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Notes");
            rec.Premise = await PutRtfText(rec.Concept, rec.Uuid, "notes.rtf");
            attr.Value = rec.Notes;
            overview.Attributes.Append(attr);

            _elements.AppendChild(overview);
        }

        private async Task ParseProblemElement(StoryElement element)
        {
            ProblemModel rec = (ProblemModel)element;
            XmlNode prob = _xml.CreateElement("Problem");
            XmlAttribute attr;

            attr = _xml.CreateAttribute("UUID");
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
            rec.StoryQuestion = await PutRtfText(rec.StoryQuestion, rec.Uuid, "storyquestion.rtf");
            attr.Value = rec.StoryQuestion;
            prob.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Premise");
            rec.Premise = await PutRtfText(rec.Premise, rec.Uuid, "premise.rtf");
            attr.Value = rec.Premise;
            prob.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Notes");
            rec.Notes = await PutRtfText(rec.Notes, rec.Uuid, "notes.rtf");
            attr.Value = rec.Notes;
            prob.Attributes.Append(attr);

            _elements.AppendChild(prob);
        }

        private async Task ParseCharacterElement(StoryElement element)
        {
            CharacterModel rec = (CharacterModel)element;
            XmlNode chr = _xml.CreateElement("Character");
            XmlAttribute attr;

            attr = _xml.CreateAttribute("UUID");
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
                relation.Notes = await PutRtfText(relation.Notes, rec.Uuid, relation.PartnerUuid + "_notes.rtf");
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
            attr = _xml.CreateAttribute("WoundCategory");
            attr.Value = rec.WoundCategory;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("WoundSummary");
            attr.Value = rec.WoundSummary;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("CharacterSketch");
            rec.CharacterSketch = await PutRtfText(rec.CharacterSketch, rec.Uuid, "charactersketch.rtf");
            attr.Value = rec.CharacterSketch;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("PhysNotes");
            rec.PhysNotes = await PutRtfText(rec.PhysNotes, rec.Uuid, "physnotes.rtf");
            attr.Value = rec.PhysNotes;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Appearance");
            rec.Appearance = await PutRtfText(rec.Appearance, rec.Uuid, "appearance.rtf");
            attr.Value = rec.Appearance;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Economic");
            rec.Economic = await PutRtfText(rec.Economic, rec.Uuid, "economic.rtf");
            attr.Value = rec.Economic;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Education");
            rec.Education = await PutRtfText(rec.Education, rec.Uuid, "education.rtf");
            attr.Value = rec.Education;
            attr = _xml.CreateAttribute("Ethnic");
            rec.Ethnic = await PutRtfText(rec.Ethnic, rec.Uuid, "ethnic.rtf");
            attr.Value = rec.Ethnic;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Religion");
            rec.Religion = await PutRtfText(rec.Religion, rec.Uuid, "religion.rtf");
            attr.Value = rec.Religion;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("PsychNotes");
            rec.PsychNotes = await PutRtfText(rec.PsychNotes, rec.Uuid, "psychnotes.rtf");
            attr.Value = rec.PsychNotes;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Work");
            rec.Work = await PutRtfText(rec.Work, rec.Uuid, "work.rtf");
            attr.Value = rec.Work;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Likes");
            rec.Likes = await PutRtfText(rec.Likes, rec.Uuid, "likes.rtf");
            attr.Value = rec.Likes;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Habits");
            rec.Habits = await PutRtfText(rec.Habits, rec.Uuid, "habits.rtf");
            attr.Value = rec.Habits;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Abilities");
            rec.Abilities = await PutRtfText(rec.Abilities, rec.Uuid, "abilities.rtf");
            attr.Value = rec.Abilities;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Wound");
            rec.Wound = await PutRtfText(rec.Wound, rec.Uuid, "wound.rtf");
            attr.Value = rec.Wound;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Fears");
            rec.Fears = await PutRtfText(rec.Fears, rec.Uuid, "fears.rtf");
            attr.Value = rec.Fears;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Lies");
            rec.Lies = await PutRtfText(rec.Lies, rec.Uuid, "lies.rtf");
            attr.Value = rec.Lies;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Secrets");
            rec.Secrets = await PutRtfText(rec.Secrets, rec.Uuid, "secrets.rtf");
            attr.Value = rec.Secrets;
            chr.Attributes.Append(attr);
            attr = _xml.CreateAttribute("BackStory");
            rec.Secrets = await PutRtfText(rec.BackStory, rec.Uuid, "backstory.rtf");
            attr.Value = rec.Secrets;
            chr.Attributes.Append(attr);

            _elements.AppendChild(chr);
        }

        private async Task ParseSettingElement(StoryElement element)
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
            rec.Summary = await PutRtfText(rec.Summary, rec.Uuid, "summary.rtf");
            attr.Value = rec.Summary;
            loc.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Sights");
            rec.Sights = await PutRtfText(rec.Sights, rec.Uuid, "sights.rtf");
            attr.Value = rec.Sights;
            loc.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Sounds");
            rec.Sounds = await PutRtfText(rec.Sounds, rec.Uuid, "sounds.rtf");
            attr.Value = rec.Sounds;
            loc.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Touch");
            rec.Touch = await PutRtfText(rec.Touch, rec.Uuid, "touch.rtf");
            attr.Value = rec.Touch;
            loc.Attributes.Append(attr);
            attr = _xml.CreateAttribute("SmellTaste");
            rec.SmellTaste = await PutRtfText(rec.SmellTaste, rec.Uuid, "smelltaste.rtf");
            attr.Value = rec.SmellTaste;
            loc.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Notes");
            rec.Notes = await PutRtfText(rec.Notes, rec.Uuid, "notes.rtf");
            attr.Value = rec.Notes;
            loc.Attributes.Append(attr);

            _elements.AppendChild(loc);
        }

        private async Task ParsePlotPointElement(StoryElement element)
        {
            PlotPointModel rec = (PlotPointModel)element;
            XmlNode plotPoint = _xml.CreateElement("PlotPoint");
            XmlAttribute attr;

            attr = _xml.CreateAttribute("UUID");
            attr.Value = UuidString(rec.Uuid);
            plotPoint.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Name");
            attr.Value = rec.Name;
            plotPoint.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Id");
            attr.Value = rec.Id.ToString();
            plotPoint.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Viewpoint");
            attr.Value = rec.Viewpoint;
            plotPoint.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Date");
            attr.Value = rec.Date;
            plotPoint.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Time");
            attr.Value = rec.Time;
            plotPoint.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Setting");
            attr.Value = rec.Setting;
            plotPoint.Attributes.Append(attr);
            attr = _xml.CreateAttribute("SceneType");
            attr.Value = rec.SceneType;
            plotPoint.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Char1");
            attr.Value = rec.Char1;
            plotPoint.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Char2");
            attr.Value = rec.Char2;
            plotPoint.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Char3");
            attr.Value = rec.Char3;
            plotPoint.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Role1");
            attr.Value = rec.Role1;
            plotPoint.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Role2");
            attr.Value = rec.Role2;
            plotPoint.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Role3");
            attr.Value = rec.Role3;
            plotPoint.Attributes.Append(attr);
            XmlNode castList = _xml.CreateElement("CastMembers");
            foreach (string member in rec.CastMembers)
            {
                XmlElement castMember = _xml.CreateElement("Member");
                castMember.AppendChild(_xml.CreateTextNode(member));
                castList.AppendChild(castMember);
            }
            plotPoint.AppendChild(castList);
            attr = _xml.CreateAttribute("Remarks");
            attr.Value = rec.Remarks;
            plotPoint.Attributes.Append(attr);
            attr = _xml.CreateAttribute("ScenePurpose");
            attr.Value = rec.ScenePurpose;
            plotPoint.Attributes.Append(attr);
            attr = _xml.CreateAttribute("ValueExchange");
            attr.Value = rec.ValueExchange;
            plotPoint.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Protagonist");
            attr.Value = rec.Protagonist;
            plotPoint.Attributes.Append(attr);
            attr = _xml.CreateAttribute("ProtagEmotion");
            attr.Value = rec.ProtagEmotion;
            plotPoint.Attributes.Append(attr);
            attr = _xml.CreateAttribute("ProtagGoal");
            attr.Value = rec.ProtagGoal;
            plotPoint.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Antagonist");
            attr.Value = rec.Antagonist;
            plotPoint.Attributes.Append(attr);
            attr = _xml.CreateAttribute("AntagEmotion");
            attr.Value = rec.AntagEmotion;
            plotPoint.Attributes.Append(attr);
            attr = _xml.CreateAttribute("AntagGoal");
            attr.Value = rec.AntagGoal;
            plotPoint.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Opposition");
            attr.Value = rec.Opposition;
            plotPoint.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Outcome");
            attr.Value = rec.Outcome;
            plotPoint.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Emotion");
            attr.Value = rec.Emotion;
            plotPoint.Attributes.Append(attr);
            attr = _xml.CreateAttribute("NewGoal");
            attr.Value = rec.NewGoal;
            plotPoint.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Remarks");
            rec.Remarks = await PutRtfText(rec.Remarks, rec.Uuid, "remarks.rtf");
            attr.Value = rec.Remarks;
            plotPoint.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Events");
            rec.Events = await PutRtfText(rec.Events, rec.Uuid, "events.rtf");
            attr.Value = rec.Events;
            plotPoint.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Consequences");
            rec.Consequences = await PutRtfText(rec.Consequences, rec.Uuid, "consequences.rtf");
            attr.Value = rec.Consequences;
            plotPoint.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Significance");
            rec.Significance = await PutRtfText(rec.Significance, rec.Uuid, "significance.rtf");
            attr.Value = rec.Significance;
            plotPoint.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Realization");
            rec.Realization = await PutRtfText(rec.Realization, rec.Uuid, "realization.rtf");
            attr.Value = rec.Realization;
            plotPoint.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Review");
            rec.Review = await PutRtfText(rec.Review, rec.Uuid, "review.rtf");
            attr.Value = rec.Review;
            plotPoint.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Notes");
            rec.Notes = await PutRtfText(rec.Notes, rec.Uuid, "notes.rtf");
            attr.Value = rec.Notes;
            plotPoint.Attributes.Append(attr);

            _elements.AppendChild(plotPoint);
        }

        private async Task ParseFolderElement(StoryElement element)
        {
            FolderModel rec = (FolderModel)element;
            XmlNode node = _xml.CreateElement("Folder");
            XmlAttribute attr;

            attr = _xml.CreateAttribute("UUID");
            attr.Value = UuidString(rec.Uuid);
            node.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Name");
            attr.Value = rec.Name;
            node.Attributes.Append(attr);

            attr = _xml.CreateAttribute("Notes");
            rec.Notes = await PutRtfText(rec.Notes, rec.Uuid, "notes.rtf");
            attr.Value = rec.Notes;
            node.Attributes.Append(attr);

            _elements.AppendChild(node);
        }

        private async Task ParseSectionElement(StoryElement element)
        {
            SectionModel rec = (SectionModel)element;
            XmlNode node = _xml.CreateElement("Section");
            XmlAttribute attr;

            attr = _xml.CreateAttribute("UUID");
            attr.Value = UuidString(rec.Uuid);
            node.Attributes.Append(attr);
            attr = _xml.CreateAttribute("Name");
            attr.Value = rec.Name;
            node.Attributes.Append(attr);

            attr = _xml.CreateAttribute("Notes");
            rec.Notes = await PutRtfText(rec.Notes, rec.Uuid, "notes.rtf");
            attr.Value = rec.Notes;
            node.Attributes.Append(attr);

            _elements.AppendChild(node);
        }

        private void ParseTrashCanElement(StoryElement element)
        {
            TrashCanModel rec = (TrashCanModel)element;
            XmlNode node = _xml.CreateElement("TrashCan");
            XmlAttribute attr;

            attr = _xml.CreateAttribute("UUID");
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
                using (var stream = await rtfFile.OpenStreamForWriteAsync())
                {
                    using (var writer = new StreamWriter(stream))
                        writer.Write(work);
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
            using (var stream = await rtfFile.OpenStreamForWriteAsync())
            {
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(notes);
                }
            }
        }

        /// <summary>
        /// Locate or create a Directory for a StoryElement based on its GUID
        /// </summary>
        /// <param name="uuid">The GUID of a text node</param>
        /// <returns>StorageFolder instance for the StoryElement's folder</returns>
        private async Task<StorageFolder> FindSubFolder(Guid uuid)
        {
            // Search the ProjectFolder's subfolders for the desired one (folder name == uuid as string)
            IReadOnlyList<StorageFolder> folders = await _story.FilesFolder.GetFoldersAsync();
            foreach (StorageFolder folder in folders)
                if (folder.Name.Equals(UuidString(uuid)))
                    return folder;
            // If the SubFolder doesn't exist, create it.
            StorageFolder newFolder = await _story.FilesFolder.CreateFolderAsync(UuidString(uuid));
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
