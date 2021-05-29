using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Storage;
using StoryBuilder.Controllers;
using StoryBuilder.Models;
using StoryBuilder.ViewModels;
using StoryBuilder.Models.Tools;
using Microsoft.UI.Xaml.Controls;

namespace StoryBuilder.DAL
{
    public class LegacyLoader
    {
        // ReSharper disable InconsistentNaming
        // Constants
        const string VersionRecType = "VR";
        const string StoryRecType = "S0";
        const string StoryNoteType = "SN";
        const string ProblemRecType = "D0";
        const string ProbProblemRecType = "D1";
        const string ProbProtagRecType = "D2";
        const string ProbAntagRecType = "D3";
        const string ProbResolutionRecType = "D4";
        const string ProbNotesRecType = "D5";
        const string ProbPremiseType = "DP";
        const string ProbRemarksRecType = "DR";
        const string StoryQuestionType = "DQ";
        const string CharRecType = "C0";
        const string CharRoleRecType = "C1";
        const string CharRoleNoteType = "CR";
        const string CharPhysicalRecType = "C2";
        const string CharPhysicalNoteType = "CP";
        const string CharSocialRecType = "C3";
        const string CharSocialEconomicType = "C$";
        const string CharSocialEducationType = "CE";
        const string CharSocialEthnicType = "CN";
        const string CharSocialReligionType = "CG";
        const string CharPsychRecType = "C4";
        const string CharAppearanceType = "CU";
        const string CharPsychNoteType = "CS";
        const string CharTraitRecType = "C5";
        const string CharWorkType = "CW";
        const string CharLikesType = "CL";
        const string CharHabitsType = "CH";
        const string CharAbilitiesType = "CA";
        const string CharNoteRecType = "C6";
        const string LocRecType = "L0";
        const string LocSettingRecType = "L1";
        const string LocNoteRecType = "L2";
        const string LocSenseRecType = "L3";
        const string LocSenseSightType = "LS";
        const string LocSenseHearingType = "LH";
        const string LocSenseTouchType = "LT";
        const string LocSenseSmellType = "LN";
        const string PlotRecType = "P0";
        const string PlotGoalRecType = "P1";
        const string PlotSceneRecType = "P2";
        const string PlotSceneDescType = "PD";
        const string PlotSequelRecType = "P4";
        const string PlotSequelReviewType = "PR";
        const string PlotNoteRecType = "P3";
        const string QuestionType = "QN";
        const string QuestionAnswerType = "QA";
        const string RelationRecType = "RR";
        const string RelationNoteType = "RN";
        //   Application holds the data for the story being built
        // in memory in records and arrays of records. The
        // following declarations define each record type as an
        // aggregate, and then allocate the records.
        //   The arrays are declared as unsized and are resized
        // whenver a record is added.
        //

        // ReSharper disable UnassignedField.Compiler
        // ReSharper disable FieldCanBeMadeReadOnly.Local

        // Record Type Definitions:
        //
        // VersionData contains the version number of this
        // file.
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public struct VersionData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            private char[] _recordType;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            private char[] _version;
            public string RecordType { get { return new string(_recordType); } }
            public string Version { get { return new string(_version); } }
        }


        // FileData doesn't correspond to any form or picture
        // box definition.  It's used when reading and writing
        // Application files to show record type and, for variable
        // length records, length.
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public struct FileData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            private char[] _recordType;
            public short RecordLength;

            public string RecordType
            {
                get { return new string(_recordType); }
                set { _recordType = value.ToCharArray(0, 2); }
            }
        }

        // StoryData corresponds to frmStory.  There is only
        // one StoryData record per story.
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public struct V0006StoryData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
            private char[] _title;
            public string Title { get { return new string(_title); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            private char[] _dateCreated;
            public string DateCreated { get { return new string(_dateCreated); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            private char[] _author;
            public string Author { get { return new string(_author); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            private char[] _dateModified;
            public string DateModified { get { return new string(_dateModified); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            private char[] _storyType;
            public string StoryType { get { return new string(_storyType); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            private char[] _storyGenre;
            public string StoryGenre { get { return new string(_storyGenre); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
            private char[] _targetMarket1;
            public string TargetMarket1 { get { return new string(_targetMarket1); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
            private char[] _targetMarket2;
            public string TargetMarket2 { get { return new string(_targetMarket2); } }
            //public string StoryNotes;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public class StoryData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
            private char[] _title;
            public string Title { get { return new string(_title); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            private char[] _dateCreated;
            public string DateCreated { get { return new string(_dateCreated); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            private char[] _author;
            public string Author { get { return new string(_author); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            private char[] _dateModified;
            public string DateModified { get { return new string(_dateModified); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            private char[] _storyType;
            public string StoryType { get { return new string(_storyType); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            private char[] _storyGenre;
            public string StoryGenre { get { return new string(_storyGenre); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            private char[] _viewPoint;
            public string ViewPoint { get { return new string(_viewPoint); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
            private char[] _targetMarket1;
            public string TargetMarket1 { get { return new string(_targetMarket1); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
            private char[] _targetMarket2;
            public string TargetMarket2 { get { return new string(_targetMarket2); } }
            //public string StoryNotes;
        }

        // ProblemData corresponds to frmProblem.
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public class V0006ProblemData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
            private char[] _description;
            public string Description { get { return new string(_description); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private char[] _problemType;
            public string ProblemType { get { return new string(_problemType); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private char[] _conflictType;
            public string ConflictType { get { return new string(_conflictType); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
            private char[] _subject;
            public string Subject { get { return new string(_subject); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
            private char[] _problemSource;
            public string ProblemSource { get { return new string(_problemSource); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            private char[] _protagonist;
            public string Protagonist { get { return new string(_protagonist); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            private char[] _protMotive;
            public string ProtMotive { get { return new string(_protMotive); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
            private char[] _protGoal;
            public string ProtGoal { get { return new string(_protGoal); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            private char[] _antagonist;
            public string Antagonist { get { return new string(_antagonist); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private char[] _antagMotive;
            public string AntagMotive { get { return new string(_antagMotive); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
            private char[] _antagGoal;
            public string AntagGoal { get { return new string(_antagGoal); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
            private char[] _storyQuestion;
            public string StoryQuestion { get { return new string(_storyQuestion); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
            private char[] _outcome;
            public string Outcome { get { return new string(_outcome); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
            private char[] _theme;
            public string Theme { get { return new string(_theme); } }
            public string Remarks;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public class V0012ProblemData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
            private char[] _description;
            public string Description { get { return new string(_description); } }
            public short ProbProblemNum;
            public short ProbProtagNum;
            public short ProbAntagNum;
            public short ProbResolutionNum;
            public short ProbNotesNum;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public class ProblemData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
            private char[] _description;
            public string Description { get { return new string(_description); } }
            public short ProbProblemNum;
            public short ProbProtagNum;
            public short ProbAntagNum;
            public short ProbResolutionNum;
            public short ProbNotesNum;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public class ProbProblemData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private char[] _problemType;
            public string ProblemType { get { return new string(_problemType); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private char[] _conflictType;
            public string ConflictType { get { return new string(_conflictType); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
            private char[] _subject;
            public string Subject { get { return new string(_subject); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
            private char[] _problemSource;
            public string ProblemSource { get { return new string(_problemSource); } }
            //public string StoryQuestion;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public class ProbProtagData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            private char[] _protagonist;
            public string Protagonist { get { return new string(_protagonist); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            private char[] _protMotive;
            public string ProtMotive { get { return new string(_protMotive); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
            private char[] _protGoal;
            public string ProtGoal { get { return new string(_protGoal); } }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public class ProbAntagData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            private char[] _antagonist;
            public string Antagonist { get { return new string(_antagonist); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private char[] _antagMotive;
            public string AntagMotive { get { return new string(_antagMotive); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
            private char[] _antagGoal;
            public string AntagGoal { get { return new string(_antagGoal); } }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public class V0013ProbResolutionData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
            private char[] _outcome;
            public string Outcome { get { return new string(_outcome); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
            private char[] _method;
            public string Method { get { return new string(_method); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
            private char[] _theme;
            public string Theme { get { return new string(_theme); } }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public class ProbResolutionData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
            private char[] _outcome;
            public string Outcome { get { return new string(_outcome); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
            private char[] _method;
            public string Method { get { return new string(_method); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
            private char[] _theme;
            public string Theme { get { return new string(_theme); } }
            //public string Premise;
        }

        public class ProbNotesData
        {
            public string Notes;
        }

        // CharData Corresponds to FrmChar.  A new occurance of
        // this record is built whenever 'Add Character' is
        // clicked.  The record contains a pointer (subscript) of
        // each char subrecord build for this character.  These
        // correspond to the picture box types which overlay each
        // other on frmChar and are made visible when lblRole,
        // lblNotes, etc, are entered.  The record subtypes are
        // built whenever data on them is entered and no record
        // for this subtype is pointed to by the CharData record
        // for this character.  A subscript of -1 indicates no
        // record exists.
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public class V0008CharData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private char[] _lname;
            public string Lname { get { return new string(_lname); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private char[] _fname;
            public string Fname { get { return new string(_fname); } }
            public short CharRoleNum;
            public short CharPhysicalNum;
            public short CharSocialNum;
            public short CharPsychNum;
            public short CharTraitNum;
            public short CharWorkNum;
            public short CharLikesNum;
            public short CharHabitsNum;
            public short CharAbilitiesNum;
            public short CharNoteNum;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public class CharData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private char[] _lname;
            public string Lname { get { return new string(_lname); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private char[] _fname;
            public string Fname { get { return new string(_fname); } }
            public short CharRoleNum;
            public short CharPhysicalNum;
            public short CharAppearanceNum;
            public short CharSocialNum;
            public short CharPsychNum;
            public short CharTraitNum;
            public short CharWorkNum;
            public short CharLikesNum;
            public short CharHabitsNum;
            public short CharAbilitiesNum;
            public short CharNoteNum;
        }

        // CharRoleData corresponds to boxRole's fields.
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public class CharRoleData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private char[] _role;
            public string Role { get { return new string(_role); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private char[] _storyRole;
            public string StoryRole { get { return new string(_storyRole); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private char[] _archetype;
            public string Archetype { get { return new string(_archetype); } }
            //public string Notes;
        }

        // The previous physical recs had integers for age and
        // weight.
        // CharPhysicalData corresponds to boxPhysical's fields.
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public class V0009CharPhysicalData
        {
            public short Age;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            private char[] _sex;
            public string Sex { get { return new string(_sex); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
            private char[] _eyes;
            public string Eyes { get { return new string(_eyes); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
            private char[] _hair;
            public string Hair { get { return new string(_hair); } }
            public short Weight;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            private char[] _height;
            public string Height { get { return new string(_height); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private char[] _build;
            public string Build { get { return new string(_build); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
            private char[] _complexion;
            public string Complexion { get { return new string(_complexion); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private char[] _race;
            public string Race { get { return new string(_race); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private char[] _nationality;
            public string Nationality { get { return new string(_nationality); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private char[] _health;
            public string Health { get { return new string(_health); } }
            public string Physnotes;
        }
        // CharPhysicalData corresponds to boxPhysical's fields.
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public class CharPhysicalData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            private char[] _age;

            public string Age
            {
                get { return new string(_age); }
                set { _age = value.ToCharArray(); }
            }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            private char[] _sex;
            public string Sex { get { return new string(_sex); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
            private char[] _eyes;
            public string Eyes { get { return new string(_eyes); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
            private char[] _hair;
            public string Hair { get { return new string(_hair); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            private char[] _weight;
            public string Weight { get { return new string(_weight); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            private char[] _height;
            public string Height { get { return new string(_height); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private char[] _build;
            public string Build { get { return new string(_build); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
            private char[] _complexion;
            public string Complexion { get { return new string(_complexion); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private char[] _race;
            public string Race { get { return new string(_race); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private char[] _nationality;
            public string Nationality { get { return new string(_nationality); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private char[] _health;
            public string Health { get { return new string(_health); } }
            //public string Physnotes;
        }
        // CharAppearanceData corresponds to tabAppearance's fields
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public class CharAppearanceData
        {
            public string Appearance;
        }
        // CharSocialData corresponds to boxSocial's fields.
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public class CharSocialData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            private char[] _socialData;
            public string SocialData { get { return new string(_socialData); } }
        }
        // CharPsychData corresponds to boxPsych's fields.
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public class CharPsychData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private char[] _enneagram;
            public string Enneagram { get { return new string(_enneagram); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private char[] _intelligence;
            public string Intelligence { get { return new string(_intelligence); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private char[] _values;
            public string Values { get { return new string(_values); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private char[] _abnormality;
            public string Abnormality { get { return new string(_abnormality); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private char[] _focus;
            public string Focus { get { return new string(_focus); } }
            //public string PsychNotes;
        }

        // CharTraitData corresponds to boxTraits's fields.
        // unlike other record types, this record uses a different
        // format in storage (the CharTraitData array of traits)
        // than is reads or written (the FileTraitData record.)
        // Only the traits actually occupied are written to the
        // outline file.
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public class CharTraitData
        {
            // Although unused, the trait names occupy space in the structure
            // ReSharper disable UnusedField.Compiler
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
            private char[] _adventureousnessName;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
            private char[] _agressionName;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
            private char[] _confidenceName;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
            private char[] _conscientiousnessName;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
            private char[] _creativityName;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
            private char[] _dominanceName;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
            private char[] _enthusiasmName;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
            private char[] _assuranceName;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
            private char[] _sensitivityName;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
            private char[] _shrewdnessName;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
            private char[] _sociabilityName;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
            private char[] _stabilityName;
            // ReSharper restore UnusedField.Compiler
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
            private char[] _adventureousness;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
            private char[] _agression;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
            private char[] _confidence;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
            private char[] _conscientiousness;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
            private char[] _creativity;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
            private char[] _dominance;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
            private char[] _enthusiasm;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
            private char[] _assurance;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
            private char[] _sensitivity;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
            private char[] _shrewdness;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
            private char[] _sociability;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
            private char[] _stability;

            public string Adventureousness { get { return new string(_adventureousness); } }
            public string Agression { get { return new string(_agression); } }
            public string Confidence { get { return new string(_confidence); } }
            public string Conscientiousness { get { return new string(_conscientiousness); } }
            public string Creativity { get { return new string(_creativity); } }
            public string Dominance { get { return new string(_dominance); } }
            public string Enthusiasm { get { return new string(_enthusiasm); } }
            public string Assurance { get { return new string(_assurance); } }
            public string Sensitivity { get { return new string(_sensitivity); } }
            public string Shrewdness { get { return new string(_shrewdness); } }
            public string Sociability { get { return new string(_sociability); } }
            public string Stability { get { return new string(_stability); } }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public class FileTraitData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
            private char[] _traitName;
            public string TraitName { get { return new string(_traitName); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
            private char[] _traitValue;
            public string TraitValue { get { return new string(_traitValue); } }
        }

        // CharWorkData corresponds to tabWork's fields.
        public class CharWorkData
        {
            public string Work;
        }

        // CharLikesData corresponds to tabLikes' fields
        public class CharLikesData
        {
            public string Likes;
        }

        // CharHabitsData corresponds to tabHabits' fields
        public class CharHabitsData
        {
            public string Habits;
        }

        // CharAbilitiesData corresponds to tabAbilities' fields
        public class CharAbilitiesData
        {
            public string Abilities;
        }

        // CharNoteData corresponds to tabNotes' fields.
        public class CharNoteData
        {
            public string Charnote;
        }

        /// <summary>
        /// LocData corresponds to frmSetting.  It contains
        /// the summary description of a setting, and the indices
        /// of the records corresponding to the tabs on
        /// that setting's form.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public class LocData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
            private char[] _locSummary;
            public string LocSummary { get { return new string(_locSummary); } }
            public short LocSettingNum;
            public short LocNoteNum;
            public short LocSenseNum;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public class LocSettingData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private char[] _locale;
            public string Locale { get { return new string(_locale); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private char[] _season;
            public string Season { get { return new string(_season); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private char[] _period;
            public string Period { get { return new string(_period); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private char[] _filler1;
            public string Filler1 { get { return new string(_filler1); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private char[] _lighting;
            public string Lighting { get { return new string(_lighting); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private char[] _weather;
            public string Weather { get { return new string(_weather); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private char[] _temperature;
            public string Temperature { get { return new string(_temperature); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
            private char[] _prop1;
            public string Prop1 { get { return new string(_prop1); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
            private char[] _prop2;
            public string Prop2 { get { return new string(_prop2); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
            private char[] _prop3;
            public string Prop3 { get { return new string(_prop3); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
            private char[] _prop4;
            public string Prop4 { get { return new string(_prop4); } }
        }
        public class LocNoteData
        {
            public string LocNote;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public class V0006LocSenseData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 160)]
            private char[] _sights;
            public string Sights { get { return new string(_sights); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 160)]
            private char[] _sounds;
            public string Sounds { get { return new string(_sounds); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 160)]
            private char[] _touch;
            public string Touch { get { return new string(_touch); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 160)]
            private char[] _smellTaste;
            public string SmellTaste { get { return new string(_smellTaste); } }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public class LocSenseData
        {
            [MarshalAs(UnmanagedType.AnsiBStr)]
            public string Sights;
            [MarshalAs(UnmanagedType.AnsiBStr)]
            public string Sounds;
            [MarshalAs(UnmanagedType.AnsiBStr)]
            public string Touch;
            [MarshalAs(UnmanagedType.AnsiBStr)]
            public string SmellTaste;
        }

        // PlotData corresponds to FrmPlot.  A new occurance of
        // this record is built whenever 'Add Plot Point' is
        // clicked.  The record contains a pointer (subscript) to
        // each plot subrecord build for this character.  These
        // correspond to the picture box types which overlay each
        // other on frmPlot and are made visible when lblNotes,
        // lblGoal, etc, are entered.  The record subtypes are
        // built whenever data on them is entered and no record
        // for this subtype is pointed to by the PlotData record
        // for this character.  A subscript of -1 indicates no
        // record exists.
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public class V0009PlotData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
            private char[] _plotSummary;
            public string PlotSummary { get { return new string(_plotSummary); } }
            public short PlotSceneNum;
            public short PlotGoalNum;
            public short PlotSequelNum;
            public short PlotNoteNum;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public class V0012PlotData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 45)]
            private char[] _plotSummary;
            public string PlotSummary { get { return new string(_plotSummary); } }
            public short PlotSceneNum;
            public short PlotGoalNum;
            public short PlotSequelNum;
            public short PlotNoteNum;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public class PlotData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
            private char[] _plotSummary;
            public string PlotSummary { get { return new string(_plotSummary); } }
            public short PlotSceneNum;
            public short PlotGoalNum;
            public short PlotSequelNum;
            public short PlotNoteNum;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public class V0013PlotSceneData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            private char[] _viewPoint;
            public string ViewPoint { get { return new string(_viewPoint); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            private char[] _date;
            public string Date { get { return new string(_date); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            private char[] _time;
            public string Time { get { return new string(_time); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
            private char[] _setting;
            public string Setting { get { return new string(_setting); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            private char[] _char1;
            public string Char1 { get { return new string(_char1); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            private char[] _char2;
            public string Char2 { get { return new string(_char2); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            private char[] _char3;
            public string Char3 { get { return new string(_char3); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            private char[] _char4;
            public string Char4 { get { return new string(_char4); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            private char[] _role1;
            public string Role1 { get { return new string(_role1); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            private char[] _role2;
            public string Role2 { get { return new string(_role2); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            private char[] _role3;
            public string Role3 { get { return new string(_role3); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            private char[] _role4;
            public string Role4 { get { return new string(_role4); } }
            //public string Remarks;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public class PlotSceneData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            private char[] _viewPoint;
            public string ViewPoint { get { return new string(_viewPoint); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            private char[] _date;
            public string Date { get { return new string(_date); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            private char[] _time;
            public string Time { get { return new string(_time); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
            private char[] _setting;
            public string Setting { get { return new string(_setting); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            private char[] _char1;
            public string Char1 { get { return new string(_char1); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            private char[] _char2;
            public string Char2 { get { return new string(_char2); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            private char[] _char3;
            public string Char3 { get { return new string(_char3); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            private char[] _role1;
            public string Role1 { get { return new string(_role1); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            private char[] _role2;
            public string Role2 { get { return new string(_role2); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            private char[] _role3;
            public string Role3 { get { return new string(_role3); } }
            // public string Remarks;
        }

        // Record type for plot conflict
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public class PlotGoalData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            private char[] _protagonist;
            public string Protagonist { get { return new string(_protagonist); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private char[] _protagMotive;
            public string ProtagMotive { get { return new string(_protagMotive); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
            private char[] _protagGoal;
            public string ProtagGoal { get { return new string(_protagGoal); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            private char[] _antagonist;
            public string Antagonist { get { return new string(_antagonist); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private char[] _antagMotive;
            public string AntagMotive { get { return new string(_antagMotive); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
            private char[] _antagGoal;
            public string AntagGoal { get { return new string(_antagGoal); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
            private char[] _opposition;
            public string Opposition { get { return new string(_opposition); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
            private char[] _outcome;
            public string Outcome { get { return new string(_outcome); } }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public class V0006PlotSequelData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private char[] _emotion;
            public string Emotion { get { return new string(_emotion); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
            private char[] _newGoal;
            public string NewGoal { get { return new string(_newGoal); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 160)]
            private char[] _review;
            public string Review { get { return new string(_review); } }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public class PlotSequelData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private char[] _emotion;
            public string Emotion { get { return new string(_emotion); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
            private char[] _newGoal;
            public string NewGoal { get { return new string(_newGoal); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            private char[] _filler1;
            public string Filler1 { get { return new string(_filler1); } }
            //public string Review;
        }

        // Record for plot notes
        public class PlotNoteData
        {
            public string PlotNote;
        }

        // Record for character to character relationships
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public class V0006CharRelationData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
            private char[] _firstChar;
            public string FirstChar { get { return new string(_firstChar); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
            private char[] _secondChar;
            public string SecondChar { get { return new string(_secondChar); } }
            public string Remarks;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public class V0008CharRelationData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
            private char[] _firstChar;
            public string FirstChar { get { return new string(_firstChar); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
            private char[] _secondChar;
            public string SecondChar { get { return new string(_secondChar); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
            private char[] _filler1;
            public string Filler1 { get { return new string(_filler1); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
            private char[] _trait1;
            public string Trait1 { get { return new string(_trait1); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
            private char[] _filler2;
            public string Filler2 { get { return new string(_filler2); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
            private char[] _trait2;
            public string Trait2 { get { return new string(_trait2); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
            private char[] _relationship;
            public string Relationship { get { return new string(_relationship); } }
            public string Remarks;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public class CharRelationData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
            private char[] _firstChar;
            public string FirstChar { get { return new string(_firstChar); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
            private char[] _secondChar;
            public string SecondChar { get { return new string(_secondChar); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
            private char[] _trait1;
            public string Trait1 { get { return new string(_trait1); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
            private char[] _trait2;
            public string Trait2 { get { return new string(_trait2); } }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
            private char[] _relationship;
            public string Relationship { get { return new string(_relationship); } }
            public string Remarks;
        }

        // ReSharper restore UnassignedField.Compiler
        // ReSharper restore FieldCanBeMadeReadOnly.Local

        //Record areas for file I/O
        private FileData FileRecHeader;
        VersionData VersionRec;
        private V0006StoryData V0006FileStoryRec;
        private StoryData FileStoryRec;
        private V0006ProblemData V0006FileProbRec;
        private V0012ProblemData V0012FileProbRec;
        private ProblemData FileProbRec;
        private ProbProblemData FileProbProblemRec;
        private ProbProtagData FileProbProtagRec;
        private ProbAntagData FileProbAntagRec;
        private V0013ProbResolutionData V0013ProbResolutionRec;
        private ProbResolutionData ProbResolutionRec;
        private V0008CharData V0008FileCharRec;
        private CharData FileCharRec;
        private CharRoleData FileCharRoleRec;
        private V0009CharPhysicalData V0009CharPhysicalRec;
        private CharPhysicalData FileCharPhysicalRec;
        // ReSharper disable once UnusedField.Compiler
        //private CharAppearanceData FileCharAppearanceRec;
        // ReSharper disable once UnusedField.CompilerPlotSequelRec
        //private CharSocialData FileCharSocialRec;
        private CharPsychData FileCharPsychRec;
        // TODO: possible bug? (note next two lines)
        private CharTraitData V0006FileCharTraitRec;
        private FileTraitData FileCharTraitRec;
        private LocData FileLocRec;
        private LocSettingData FileLocSettingRec;
        private V0006LocSenseData V0006FileLocSenseRec;
        // ReSharper disable once UnusedField.Compiler
        //private LocSenseData FileLocSenseRec;
        private V0009PlotData V0009PlotRec;
        private V0012PlotData V0012PlotRec;
        private PlotData FilePlotRec;
        private PlotGoalData FilePlotGoalRec;
        private V0013PlotSceneData V0013PlotSceneRec;
        private PlotSceneData FilePlotSceneRec;
        private PlotSequelData FilePlotSequelRec;
        private V0006CharRelationData V0006FileRelationRec;
        private V0008CharRelationData V0008FileRelationRec;
        private CharRelationData FileRelationRec;

        private StoryController _story;
        public StoryModel StoryModel;

        public LegacyLoader(StoryController controller)
        {
            _story = controller;
        }

        /// <summary>
        /// Loads the currently selected file for Application to update.
        ///
        /// NOTE: Any changes to reads, copybooks, etc. should be mirrored
        /// in LoadCopy and ProcessCopy.
        /// </summary>
        public async Task<StoryModel> LoadFile(StorageFile file)
        {
            StoryModel = new StoryModel();
            OverviewModel overview;
            CharacterModel character = null;
            ProblemModel problem = null;
            SettingModel setting = null;
            PlotPointModel plotpoint = null;
            CharacterRelationshipsModel relationship = null;

            // TODO: Note to test StoryModel for null after call
            // Locally defined variables
            int recNumber = 0;
            //TreeNode locNode = null;
            //TreeNode sceneNode = null;
            // Stop the backup timer, if it's active
            //Controller.StopCLock();
            // There's one OverviewModel per story; it's also the Treeview root
            ///BUG: Title is wrong
            overview = new OverviewModel("Working Title");
            StoryNodeItem overviewNode = new StoryNodeItem(overview, null);
            overviewNode.IsRoot = true;

            // Create new nodes to hold the StoryElement type collections
            StoryElement problems = new FolderModel("Problems");
            StoryNodeItem problemsNode = new StoryNodeItem(problems, overviewNode);
            StoryElement characters = new FolderModel("Characters");
            StoryNodeItem charactersNode = new StoryNodeItem(characters, overviewNode);
            StoryElement settings = new FolderModel("Settings");
            StoryNodeItem settingsNode = new StoryNodeItem(settings, overviewNode);
            StoryElement plotpoints = new FolderModel("Plot Points");
            StoryNodeItem plotpointsNode = new StoryNodeItem(plotpoints, overviewNode);

            // Read the legacy StoryBuilder file.  Each record is
            // really two records: a 'FileRecHeader' record,
            // which contains record type and (for variable strings)
            // length, and then, following, the header record,
            // the actual record itself.
            Stream stream = (await file.OpenReadAsync()).AsStreamForRead();
            using (BinaryReader rdr = new BinaryReader(stream))
            {
                while (rdr.BaseStream.Position != rdr.BaseStream.Length)
                {
                    recNumber++;
                    FileRecHeader = ReadStruct<FileData>(rdr, 4); // Get the next header
                    switch (FileRecHeader.RecordType)
                    {
                        case VersionRecType: //File version record
                            VersionRec = ReadStruct<VersionData>(rdr, 7);
                            break;
                        case StoryRecType: //StoryRec record
                            //switch (VersionRec.Version)
                            switch (VersionRec.Version)
                            {
                                case "00.06":
                                    V0006FileStoryRec = ReadStruct<V0006StoryData>(rdr, 218);
                                    overview.Name = V0006FileStoryRec.Title.TrimEnd();
                                    overview.DateCreated = V0006FileStoryRec.DateCreated.TrimEnd();
                                    overview.Author = V0006FileStoryRec.Author.TrimEnd();
                                    overview.DateModified = V0006FileStoryRec.DateModified.TrimEnd();
                                    overview.StoryType = V0006FileStoryRec.StoryType.TrimEnd();
                                    overview.StoryGenre = V0006FileStoryRec.StoryGenre.TrimEnd();
                                    overview.Viewpoint = string.Empty;
                                    overview.TargetMarket1 = V0006FileStoryRec.TargetMarket1.TrimEnd();
                                    overview.TargetMarket2 = V0006FileStoryRec.TargetMarket2.TrimEnd();
                                    break;
                                default:
                                    FileStoryRec = ReadStruct<StoryData>(rdr, 218);
                                    overview.Name = FileStoryRec.Title;
                                    overview.DateCreated = FileStoryRec.DateCreated.TrimEnd();
                                    overview.Author = FileStoryRec.Author.TrimEnd();
                                    overview.DateModified = FileStoryRec.DateModified.TrimEnd();
                                    overview.StoryType = FileStoryRec.StoryType.TrimEnd();
                                    overview.StoryGenre = FileStoryRec.StoryGenre.TrimEnd();
                                    overview.Viewpoint = FileStoryRec.ViewPoint.TrimEnd();
                                    overview.TargetMarket1 = FileStoryRec.TargetMarket1.TrimEnd();
                                    overview.TargetMarket2 = FileStoryRec.TargetMarket2.TrimEnd();
                                    //FileSystem.FileGet(FileNum, ref StoryRec, -1);
                                    break;
                            }

                            // TODO: deal with default dates in Legacy Loader   
                            //if (StoryRec.DateCreated.Value == "        ") {
                            //    StoryRec.DateCreated.Value = StoryRecMask.DateCreated;
                            //}
                            //if (StoryRec.DateModified.Value == "        ") {
                            //    StoryRec.DateModified.Value = StoryRecMask.DateModified;
                            //}
                            //ShowStory();
                            break;
                        case StoryNoteType:
                            overview.StoryIdea = new string(rdr.ReadChars(FileRecHeader.RecordLength));
                            break;
                        case ProblemRecType:
                            problem = new ProblemModel();
                            //switch (VersionRec.Version)
                            switch (VersionRec.Version)
                            {
                                case "00.06":
                                    V0006FileProbRec = ReadStruct<V0006ProblemData>(rdr);
                                    problem.Name = V0006FileProbRec.Description.TrimEnd();
                                    problem.ProblemType = V0006FileProbRec.ProblemType.TrimEnd();
                                    problem.ConflictType = V0006FileProbRec.ConflictType.TrimEnd();
                                    problem.Subject = V0006FileProbRec.Subject.TrimEnd();
                                    problem.ProblemSource = V0006FileProbRec.ProblemSource.TrimEnd();
                                    problem.StoryQuestion = V0006FileProbRec.StoryQuestion.TrimEnd();
                                    problem.Protagonist = V0006FileProbRec.Protagonist.TrimEnd();
                                    problem.ProtMotive = V0006FileProbRec.ProtMotive.TrimEnd();
                                    problem.ProtGoal = V0006FileProbRec.ProtGoal.TrimEnd();
                                    problem.Antagonist = V0006FileProbRec.Antagonist.TrimEnd();
                                    problem.AntagMotive = V0006FileProbRec.AntagMotive.TrimEnd();
                                    problem.AntagGoal = V0006FileProbRec.AntagGoal.TrimEnd();
                                    problem.Outcome = V0006FileProbRec.Outcome.TrimEnd();
                                    problem.Theme = V0006FileProbRec.Theme.TrimEnd();
                                    problem.Notes = V0006FileProbRec.Remarks.TrimEnd();
                                    break;
                                case "00.07":
                                case "00.08":
                                case "00.09":
                                case "00.10":
                                case "00.11":
                                case "00.12":
                                    V0012FileProbRec = ReadStruct<V0012ProblemData>(rdr);
                                    problem.Name = V0012FileProbRec.Description.TrimEnd();
                                    break;
                                default:
                                    FileProbRec = ReadStruct<ProblemData>(rdr);
                                    problem.Name = FileProbRec.Description.TrimEnd();
                                    break;
                            }
                            StoryNodeItem problemNode = new StoryNodeItem(problem, problemsNode);
                            break;
                        case ProbProblemRecType:
                            FileProbProblemRec = ReadStruct<ProbProblemData>(rdr, 142);
                            problem.ProblemType = FileProbProblemRec.ProblemType.TrimEnd();
                            problem.ConflictType = FileProbProblemRec.ConflictType.TrimEnd();
                            problem.Subject = FileProbProblemRec.Subject.TrimEnd();
                            problem.ProblemSource = FileProbProblemRec.ProblemSource.TrimEnd();
                            //problem.StoryQuestion = FileProbProblemRec.StoryQuestion;
                            break;
                        case ProbProtagRecType:
                            FileProbProtagRec = ReadStruct<ProbProtagData>(rdr);
                            problem.Protagonist = FileProbProtagRec.Protagonist.TrimEnd();
                            problem.ProtMotive = FileProbProtagRec.ProtMotive.TrimEnd();
                            problem.ProtGoal = FileProbProtagRec.ProtGoal.TrimEnd();
                            break;
                        case ProbAntagRecType:
                            FileProbAntagRec = ReadStruct<ProbAntagData>(rdr);
                            problem.Antagonist = FileProbAntagRec.Antagonist.TrimEnd();
                            problem.AntagMotive = FileProbAntagRec.AntagMotive.TrimEnd();
                            problem.AntagGoal = FileProbAntagRec.AntagGoal.TrimEnd();
                            break;
                        case ProbResolutionRecType:
                            switch (VersionRec.Version)
                            {
                                case "00.06":
                                case "00.07":
                                case "00.08":
                                case "00.09":
                                case "00.10":
                                case "00.11":
                                case "00.12":
                                case "00.13":
                                    V0013ProbResolutionRec = ReadStruct<V0013ProbResolutionData>(rdr);
                                    problem.Outcome = V0013ProbResolutionRec.Outcome.TrimEnd();
                                    problem.Method = V0013ProbResolutionRec.Method.TrimEnd();
                                    problem.Theme = V0013ProbResolutionRec.Theme.TrimEnd();
                                    problem.Premise = "";
                                    break;
                                default:
                                    ProbResolutionRec = ReadStruct<ProbResolutionData>(rdr, 152);
                                    problem.Outcome = ProbResolutionRec.Outcome.TrimEnd();
                                    problem.Method = ProbResolutionRec.Method.TrimEnd();
                                    problem.Theme = ProbResolutionRec.Theme.TrimEnd();
                                    //problem.Premise = ProbResolutionRec.Premise;
                                    break;
                            }

                            break;
                        case ProbPremiseType:
                            problem.Premise = new string(rdr.ReadChars(FileRecHeader.RecordLength));
                            break;
                        case ProbNotesRecType:
                            problem.Notes = new string(rdr.ReadChars(FileRecHeader.RecordLength));
                            break;
                        case StoryQuestionType:
                            problem.StoryQuestion = new string(rdr.ReadChars(FileRecHeader.RecordLength));
                            break;
                        case ProbRemarksRecType: //Problem form remarks record
                            problem.Notes = new string(rdr.ReadChars(FileRecHeader.RecordLength));
                            break;
                        case CharRecType:
                            character = new CharacterModel();
                            switch (VersionRec.Version)
                            {
                                case "00.06":
                                case "00.07":
                                case "00.08":
                                    V0008FileCharRec = ReadStruct<V0008CharData>(rdr);
                                    character.Name = V0008FileCharRec.Fname.TrimEnd() + " " +
                                                     V0008FileCharRec.Lname.TrimEnd();
                                    break;
                                default:
                                    FileCharRec = ReadStruct<CharData>(rdr);
                                    character.Name = FileCharRec.Fname.TrimEnd() + " " +
                                                     FileCharRec.Lname.TrimEnd();
                                    break;
                            }
                            StoryNodeItem characterNode = new StoryNodeItem(character, charactersNode);
                            break;
                        case CharRoleRecType:
                            FileCharRoleRec = ReadStruct<CharRoleData>(rdr, 62);
                            character.Role = FileCharRoleRec.Role.TrimEnd();
                            character.StoryRole = FileCharRoleRec.StoryRole.TrimEnd();
                            character.Archetype = FileCharRoleRec.Archetype.TrimEnd();
                            character.CharacterSketch = string.Empty;
                            //character.CharacterSketch = FileCharRoleRec.Notes;
                            break;
                        case CharRoleNoteType: //CharRoleRec notes
                            character.CharacterSketch = new string(rdr.ReadChars(FileRecHeader.RecordLength));
                            break;
                        case CharPhysicalRecType: //CharPhysicalRec record
                            switch (VersionRec.Version)
                            {
                                case "00.03":
                                case "00.04":
                                case "00.05":
                                case "00.06":
                                case "00.07":
                                case "00.08":
                                case "00.09":
                                case "00.10":
                                    V0009CharPhysicalRec = ReadStruct<V0009CharPhysicalData>(rdr);
                                    if (V0009CharPhysicalRec.Age != 0)
                                        character.Age = V0009CharPhysicalRec.Age.ToString(CultureInfo.InvariantCulture);
                                    else
                                        FileCharPhysicalRec.Age = "";
                                    character.Sex = V0009CharPhysicalRec.Sex.TrimEnd();
                                    character.Eyes = V0009CharPhysicalRec.Eyes.TrimEnd();
                                    character.Hair = V0009CharPhysicalRec.Hair.TrimEnd();
                                    if (V0009CharPhysicalRec.Weight != 0)
                                        character.Weight =
                                            V0009CharPhysicalRec.Weight.ToString(CultureInfo.InvariantCulture);
                                    else
                                        character.Weight = "";
                                    character.CharHeight = V0009CharPhysicalRec.Height.TrimEnd();
                                    character.Build = V0009CharPhysicalRec.Build.TrimEnd();
                                    character.Complexion = V0009CharPhysicalRec.Complexion.TrimEnd();
                                    character.Race = V0009CharPhysicalRec.Race.TrimEnd();
                                    character.Nationality = V0009CharPhysicalRec.Nationality.TrimEnd();
                                    character.Health = V0009CharPhysicalRec.Health;
                                    character.PhysNotes = string.Empty;
                                    break;
                                default:
                                    FileCharPhysicalRec = ReadStruct<CharPhysicalData>(rdr, 146);
                                    character.Age = FileCharPhysicalRec.Age.TrimEnd();
                                    character.Sex = FileCharPhysicalRec.Sex.TrimEnd();
                                    character.Eyes = FileCharPhysicalRec.Eyes.TrimEnd();
                                    character.Hair = FileCharPhysicalRec.Hair.TrimEnd();
                                    character.Weight = FileCharPhysicalRec.Weight.TrimEnd();
                                    character.CharHeight = FileCharPhysicalRec.Height.TrimEnd();
                                    character.Build = FileCharPhysicalRec.Build.TrimEnd();
                                    character.Complexion = FileCharPhysicalRec.Complexion.TrimEnd();
                                    character.Race = FileCharPhysicalRec.Race.TrimEnd();
                                    character.Nationality = FileCharPhysicalRec.Nationality.TrimEnd();
                                    character.Health = FileCharPhysicalRec.Health.TrimEnd();
                                    character.PhysNotes = string.Empty;
                                    break;
                            }
                            break;
                        case CharPhysicalNoteType:
                            character.PhysNotes = new string(rdr.ReadChars(FileRecHeader.RecordLength));
                            break;
                        case CharAppearanceType:
                            character.Appearance = new string(rdr.ReadChars(FileRecHeader.RecordLength));
                            break;
                        case CharSocialRecType:
                            string[] social = ReadStrings(rdr, 4);
                            character.Economic = social[0];
                            character.Education = social[1];
                            character.Ethnic = social[2];
                            character.Religion = social[3];
                            break;
                        case CharSocialEconomicType:
                            character.Economic = new string(rdr.ReadChars(FileRecHeader.RecordLength));
                            break;
                        case CharSocialEducationType:
                            character.Education = new string(rdr.ReadChars(FileRecHeader.RecordLength));
                            break;
                        case CharSocialEthnicType:
                            character.Ethnic = new string(rdr.ReadChars(FileRecHeader.RecordLength));
                            break;
                        case CharSocialReligionType:
                            character.Religion = new string(rdr.ReadChars(FileRecHeader.RecordLength));
                            break;
                        case CharPsychRecType:
                            FileCharPsychRec = ReadStruct<CharPsychData>(rdr, 102);
                            character.Enneagram = FileCharPsychRec.Enneagram.TrimEnd();
                            character.Intelligence = FileCharPsychRec.Intelligence.TrimEnd();
                            character.Values = FileCharPsychRec.Values.TrimEnd();
                            character.Abnormality = FileCharPsychRec.Abnormality.TrimEnd();
                            character.Focus = FileCharPsychRec.Focus.TrimEnd();
                            character.PsychNotes = string.Empty; //FileCharPsychRec.PsychNotes;
                            break;
                        case CharPsychNoteType:
                            character.PsychNotes = new string(rdr.ReadChars(FileRecHeader.RecordLength));
                            break;
                        case CharTraitRecType:
                            switch (VersionRec.Version)
                            {
                                case "00.06":
                                case "00.07":
                                    V0006FileCharTraitRec = ReadStruct<CharTraitData>(rdr);
                                    character.Adventureousness = V0006FileCharTraitRec.Adventureousness.TrimEnd();
                                    character.Aggression = V0006FileCharTraitRec.Agression.TrimEnd();
                                    character.Confidence = V0006FileCharTraitRec.Confidence.TrimEnd();
                                    character.Conscientiousness = V0006FileCharTraitRec.Conscientiousness.TrimEnd();
                                    character.Creativity = V0006FileCharTraitRec.Creativity.TrimEnd();
                                    character.Dominance = V0006FileCharTraitRec.Dominance.TrimEnd();
                                    character.Enthusiasm = V0006FileCharTraitRec.Enthusiasm.TrimEnd();
                                    character.Assurance = V0006FileCharTraitRec.Assurance.TrimEnd();
                                    character.Sensitivity = V0006FileCharTraitRec.Sensitivity.TrimEnd();
                                    character.Shrewdness = V0006FileCharTraitRec.Shrewdness.TrimEnd();
                                    character.Sociability = V0006FileCharTraitRec.Sociability.TrimEnd();
                                    character.Stability = V0006FileCharTraitRec.Stability.TrimEnd();
                                    break;
                                default:
                                    FileCharTraitRec = ReadStruct<FileTraitData>(rdr);
                                    switch (FileCharTraitRec.TraitName)
                                    {
                                        case "Adventureousness":
                                            character.Adventureousness = FileCharTraitRec.TraitValue.TrimEnd();
                                            break;
                                        case "Aggression":
                                            character.Aggression = FileCharTraitRec.TraitValue.TrimEnd();
                                            break;
                                        case "Confidence":
                                            character.Confidence = FileCharTraitRec.TraitValue.TrimEnd();
                                            break;
                                        case "Conscientiousness":
                                            character.Conscientiousness = FileCharTraitRec.TraitValue.TrimEnd();
                                            break;
                                        case "Creativity":
                                            character.Creativity = FileCharTraitRec.TraitValue.TrimEnd();
                                            break;
                                        case "Dominance":
                                            character.Dominance = FileCharTraitRec.TraitValue.TrimEnd();
                                            break;
                                        case "Enthusiasm":
                                            character.Enthusiasm = FileCharTraitRec.TraitValue.TrimEnd();
                                            break;
                                        case "Assurance":
                                            character.Assurance = FileCharTraitRec.TraitValue.TrimEnd();
                                            break;
                                        case "Sensitivity":
                                            character.Sensitivity = FileCharTraitRec.TraitValue.TrimEnd();
                                            break;
                                        case "Shrewdness":
                                            character.Shrewdness = FileCharTraitRec.TraitValue.TrimEnd();
                                            break;
                                        case "Sociability":
                                            character.Sociability = FileCharTraitRec.TraitValue.TrimEnd();
                                            break;
                                        case "Stability":
                                            character.Stability = FileCharTraitRec.TraitValue.TrimEnd();
                                            break;
                                    }

                                    break;
                            }

                            break;
                        case CharWorkType:
                            character.Work = new string(rdr.ReadChars(FileRecHeader.RecordLength));
                            break;
                        case CharLikesType:
                            character.Likes = new string(rdr.ReadChars(FileRecHeader.RecordLength));
                            break;
                        case CharHabitsType:
                            character.Habits = new string(rdr.ReadChars(FileRecHeader.RecordLength));
                            break;
                        case CharAbilitiesType:
                            character.Abilities = new string(rdr.ReadChars(FileRecHeader.RecordLength));
                            break;
                        case CharNoteRecType:
                            character.BackStory = new string(rdr.ReadChars(FileRecHeader.RecordLength));
                            break;
                        case LocRecType:
                            setting = new SettingModel();
                            FileLocRec = ReadStruct<LocData>(rdr);
                            setting.Name = FileLocRec.LocSummary.TrimEnd();
                            StoryNodeItem settingNode = new StoryNodeItem(setting, settingsNode);
                            break;
                        case LocSettingRecType:
                            FileLocSettingRec = ReadStruct<LocSettingData>(rdr);
                            setting.Locale = FileLocSettingRec.Locale.TrimEnd();
                            setting.Season = FileLocSettingRec.Season.TrimEnd();
                            setting.Period = FileLocSettingRec.Period.TrimEnd();
                            //setting.Filler1 = FileLocSettingRec.Filler1.TrimEnd();
                            setting.Lighting = FileLocSettingRec.Lighting.TrimEnd();
                            setting.Weather = FileLocSettingRec.Weather.TrimEnd();
                            setting.Temperature = FileLocSettingRec.Temperature.TrimEnd();
                            setting.Prop1 = FileLocSettingRec.Prop1.TrimEnd();
                            setting.Prop2 = FileLocSettingRec.Prop2.TrimEnd();
                            setting.Prop3 = FileLocSettingRec.Prop3.TrimEnd();
                            setting.Prop4 = FileLocSettingRec.Prop4.TrimEnd();
                            break;
                        case LocSenseRecType:
                            switch (VersionRec.Version)
                            {
                                case "00.06":
                                    V0006FileLocSenseRec = ReadStruct<V0006LocSenseData>(rdr);
                                    setting.Sights = V0006FileLocSenseRec.Sights.TrimEnd();
                                    setting.Sounds = V0006FileLocSenseRec.Sounds.TrimEnd();
                                    setting.Touch = V0006FileLocSenseRec.Touch.TrimEnd();
                                    setting.SmellTaste = V0006FileLocSenseRec.SmellTaste.TrimEnd();
                                    break;
                                default:
                                    //FileLocSenseRec = ReadStruct<LocSenseData>(rdr);
                                    // contains four strings, not fixed length. Each has a read below.
                                    string[] results = ReadStrings(rdr, 4);
                                    setting.Sights = results[0];
                                    setting.Sounds = results[1];
                                    setting.Touch = results[2];
                                    setting.SmellTaste = results[3];
                                    break;
                            }

                            break;
                        case LocSenseSightType:
                            setting.Sights = new string(rdr.ReadChars(FileRecHeader.RecordLength));
                            break;
                        case LocSenseHearingType:
                            setting.Sounds = new string(rdr.ReadChars(FileRecHeader.RecordLength));
                            break;
                        case LocSenseTouchType:
                            setting.Touch = new string(rdr.ReadChars(FileRecHeader.RecordLength));
                            break;
                        case LocSenseSmellType:
                            setting.SmellTaste = new string(rdr.ReadChars(FileRecHeader.RecordLength));
                            break;
                        case LocNoteRecType:
                            setting.Notes = new string(rdr.ReadChars(FileRecHeader.RecordLength));
                            break;
                        case PlotRecType:
                            plotpoint = new PlotPointModel();
                            switch (VersionRec.Version)
                            {
                                case "00.03":
                                case "00.04":
                                case "00.05":
                                case "00.06":
                                case "00.07":
                                case "00.08":
                                case "00.09":
                                case "00.10":
                                case "00.11":
                                    V0009PlotRec = ReadStruct<V0009PlotData>(rdr);
                                    plotpoint.Name = V0009PlotRec.PlotSummary.TrimEnd();
                                    break;
                                case "00.12":
                                    V0012PlotRec = ReadStruct<V0012PlotData>(rdr);
                                    plotpoint.Name = V0012PlotRec.PlotSummary.TrimEnd();
                                    break;
                                default:
                                    FilePlotRec = ReadStruct<PlotData>(rdr);
                                    plotpoint.Name = FilePlotRec.PlotSummary.TrimEnd();
                                    break;
                            }
                            StoryNodeItem plotpointNode = new StoryNodeItem(plotpoint, plotpointsNode);
                            break;
                        case PlotGoalRecType:
                            FilePlotGoalRec = ReadStruct<PlotGoalData>(rdr);
                            plotpoint.Protagonist = FilePlotGoalRec.Protagonist.TrimEnd();
                            plotpoint.ProtagEmotion = FilePlotGoalRec.ProtagMotive.TrimEnd();
                            plotpoint.ProtagGoal = FilePlotGoalRec.ProtagGoal.TrimEnd();
                            plotpoint.Antagonist = FilePlotGoalRec.Antagonist.TrimEnd();
                            plotpoint.AntagEmotion = FilePlotGoalRec.AntagMotive.TrimEnd();
                            plotpoint.AntagGoal = FilePlotGoalRec.AntagGoal.TrimEnd();
                            plotpoint.Opposition = FilePlotGoalRec.Opposition.TrimEnd();
                            plotpoint.Outcome = FilePlotGoalRec.Outcome.TrimEnd();
                            break;
                        case PlotSceneRecType:
                            switch (VersionRec.Version)
                            {
                                case "00.03":
                                case "00.04":
                                case "00.05":
                                case "00.06":
                                case "00.07":
                                case "00.08":
                                case "00.09":
                                case "00.10":
                                case "00.11":
                                case "00.12":
                                case "00.13":
                                    V0013PlotSceneRec = ReadStruct<V0013PlotSceneData>(rdr);
                                    plotpoint.Viewpoint = V0013PlotSceneRec.ViewPoint.TrimEnd();
                                    plotpoint.Date = V0013PlotSceneRec.Date.TrimEnd();
                                    plotpoint.Time = V0013PlotSceneRec.Time.TrimEnd();
                                    plotpoint.Setting = V0013PlotSceneRec.Setting.TrimEnd();
                                    plotpoint.Char1 = V0013PlotSceneRec.Char1.TrimEnd();
                                    plotpoint.Char2 = V0013PlotSceneRec.Char2.TrimEnd();
                                    plotpoint.Char3 = V0013PlotSceneRec.Char3.TrimEnd();
                                    plotpoint.Role1 = V0013PlotSceneRec.Role1.TrimEnd();
                                    plotpoint.Role2 = V0013PlotSceneRec.Role2.TrimEnd();
                                    plotpoint.Role3 = V0013PlotSceneRec.Role3.TrimEnd();
                                    plotpoint.Remarks = "";
                                    break;
                                default:
                                    FilePlotSceneRec = ReadStruct<PlotSceneData>(rdr, 257);
                                    plotpoint.Viewpoint = FilePlotSceneRec.ViewPoint.TrimEnd();
                                    plotpoint.Date = FilePlotSceneRec.Date.TrimEnd();
                                    plotpoint.Time = FilePlotSceneRec.Time.TrimEnd();
                                    plotpoint.Setting = FilePlotSceneRec.Setting.TrimEnd();
                                    plotpoint.Char1 = FilePlotSceneRec.Char1.TrimEnd();
                                    plotpoint.Char2 = FilePlotSceneRec.Char2.TrimEnd();
                                    plotpoint.Char3 = FilePlotSceneRec.Char3.TrimEnd();
                                    plotpoint.Role1 = FilePlotSceneRec.Role1.TrimEnd();
                                    plotpoint.Role2 = FilePlotSceneRec.Role2.TrimEnd();
                                    plotpoint.Role3 = FilePlotSceneRec.Role3.TrimEnd();
                                    plotpoint.Remarks = string.Empty;
                                    break;
                            }

                            break;
                        case PlotSceneDescType: //PlotDramaRec notes
                            plotpoint.Remarks = new string(rdr.ReadChars(FileRecHeader.RecordLength));
                            break;
                        case PlotSequelRecType:
                            FilePlotSequelRec = ReadStruct<PlotSequelData>(rdr);
                            plotpoint.Emotion = FilePlotSequelRec.Emotion.TrimEnd();
                            plotpoint.NewGoal = FilePlotSequelRec.NewGoal.TrimEnd();
                            plotpoint.Review = string.Empty;
                            break;
                        case PlotSequelReviewType:
                            plotpoint.Review = new string(rdr.ReadChars(FileRecHeader.RecordLength));
                            break;
                        case PlotNoteRecType:
                            plotpoint.Notes = new string(rdr.ReadChars(FileRecHeader.RecordLength));
                            break;
                        case QuestionType:
                            //question = new KeyQuestionModel();
                            //question.Question = new string(rdr.ReadChars(FileRecHeader.RecordLength));
                            //StoryModel.QuestionList.Add(question);
                            break;
                        case QuestionAnswerType:
                            //question.Answer = new string(rdr.ReadChars(FileRecHeader.RecordLength));
                            break;
                        case RelationRecType:
                            relationship = new CharacterRelationshipsModel();
                            StoryModel.RelationList.Add(relationship);
                            switch (VersionRec.Version)
                            {
                                case "00.06":
                                    V0006FileRelationRec = ReadStruct<V0006CharRelationData>(rdr);
                                    relationship.FirstChar = V0006FileRelationRec.FirstChar.TrimEnd();
                                    relationship.SecondChar = V0006FileRelationRec.SecondChar.TrimEnd();
                                    relationship.FirstTrait = "";
                                    relationship.SecondTrait = "";
                                    relationship.Relationship = "";
                                    relationship.Remarks = "";
                                    break;
                                case "00.08":
                                    V0008FileRelationRec = ReadStruct<V0008CharRelationData>(rdr);
                                    relationship.FirstChar = V0008FileRelationRec.FirstChar.TrimEnd();
                                    relationship.SecondChar = V0008FileRelationRec.SecondChar.TrimEnd();
                                    relationship.FirstTrait = V0008FileRelationRec.Trait1.TrimEnd();
                                    relationship.SecondTrait = V0008FileRelationRec.Trait2.TrimEnd();
                                    relationship.Relationship = V0008FileRelationRec.Relationship.TrimEnd();
                                    relationship.Remarks = V0008FileRelationRec.Remarks.TrimEnd();
                                    break;
                                default:
                                    FileRelationRec = ReadStruct<CharRelationData>(rdr);
                                    relationship.FirstChar = V0008FileRelationRec.FirstChar.TrimEnd();
                                    relationship.SecondChar = V0008FileRelationRec.SecondChar.TrimEnd();
                                    relationship.FirstTrait = V0008FileRelationRec.Trait1.TrimEnd();
                                    relationship.SecondTrait = V0008FileRelationRec.Trait2.TrimEnd();
                                    relationship.Relationship = V0008FileRelationRec.Relationship.TrimEnd();
                                    relationship.Remarks = V0008FileRelationRec.Remarks.TrimEnd();
                                    FileRelationRec.Remarks = "";
                                    break;
                            }

                            break;
                        case RelationNoteType:
                            relationship.Remarks = new string(rdr.ReadChars(FileRecHeader.RecordLength));
                            break;
                        default:
                            string workstring1 = "Error: Unexpected data on file load ";
                            workstring1 = workstring1 + Environment.NewLine;
                            workstring1 = workstring1 + "Record number ";
                            workstring1 = workstring1 + recNumber.ToString();
                            workstring1 = workstring1 + Environment.NewLine;
                            workstring1 = workstring1 + "Position = " + rdr.BaseStream.Position;
                            ContentDialog loadErrorDialog = new ContentDialog()
                            {
                                Title = "LegacyLoader Error",
                                Content = "Unexpected data on file load",
                                CloseButtonText = "Ok"
                            };

                            loadErrorDialog.XamlRoot = _story.XamlRoot;
                            await loadErrorDialog.ShowAsync();
                            break;
                    }
                } // end of while (rdr.BaseStream.Position != rdr.BaseStream.Length)
            }  // end of using 

            // Activate StoryExplorer viewmodel we've been creating
            overviewNode.IsRoot = true;
            StoryModel.ExplorerView.Add(overviewNode);  // The OverviewModel node is the only overview
            TrashCanModel trash = new TrashCanModel();
            StoryNodeItem trashNode = new StoryNodeItem(trash, null);
            trashNode.IsRoot = true;
            StoryModel.ExplorerView.Add(trashNode);     // The trashcan is the second root
            // Create Narrator viewmodel
            SectionModel narrative = new SectionModel("Narrative View");
            StoryNodeItem narrativeNode = new StoryNodeItem(narrative, null);
            narrativeNode.IsRoot = true;
            foreach (StoryNodeItem child in plotpointsNode.Children)
            {
                new StoryNodeItem(child, narrativeNode);
            }
            StoryModel.NarratorView.Add(narrativeNode);
            trash = new TrashCanModel();
            trashNode = new StoryNodeItem(trash, null);
            trashNode.IsRoot = true;
            StoryModel.NarratorView.Add(trashNode);     // The trashcan is the second root

            StoryModel.Changed = false;
            return StoryModel;
        }
        /// <summary>
        /// This generic method reads a struct of type T from a
        /// binary stream.
        /// </summary>
        /// <typeparam name="T">The type of the struct to read</typeparam>
        /// <param name="reader">BinaryReader mapped to a stream</param>
        /// <param name="length">Number of bytes to read</param>
        /// <returns>struct of type T</returns>
        public static T ReadStruct<T>(BinaryReader reader, int length)
        {
            byte[] bytes = reader.ReadBytes(length);
            //byte[] bytes = reader.ReadBytes(Marshal.SizeOf(typeof(T)));

            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            T structure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();

            return structure;
        }

        public static T ReadStruct<T>(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(Marshal.SizeOf(typeof(T)));
            //byte[] bytes = reader.ReadBytes(Marshal.SizeOf(typeof(T)));

            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            T structure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();

            return structure;
        }

        public static string[] ReadStrings(BinaryReader reader, int count)
        {
            List<string> strings = new List<string>();
            for (int i = 0; i < count; i++)
            {
                short length = reader.ReadInt16();
                strings.Add(new string(reader.ReadChars(length)));
            }
            return strings.ToArray();
        }

        // ReSharper restore InconsistentNaming
    }
}
