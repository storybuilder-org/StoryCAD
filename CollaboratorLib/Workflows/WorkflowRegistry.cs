using System;
using System.Collections.Generic;
using System.Linq;
using StoryCADLib.Models;
using StoryCADLib.Models.StoryWorld;
using StoryCollaborator.Models;

namespace StoryCollaborator.Workflows
{
    /// <summary>
    /// Static registry of all workflow definitions.
    /// Separates workflow data from workflow structure.
    /// </summary>
    public static class WorkflowRegistry
    {
        /// <summary>
        /// All available workflows.
        /// </summary>
        public static readonly List<Workflow> All = CreateWorkflows();

        /// <summary>
        /// Workflows starred for a user who has never curated the set. One per stage of the
        /// outlining arc — idea to premise, premise to problem and cast, problem is well formed,
        /// cast has function, scene is written — so the top band reads as a next action rather
        /// than a catalog. Seeded once by WorkflowStarService; after that the user's choices win.
        /// #77 took GMC and Structure off the band when ProblemBuilder landed, and #211 has now
        /// deleted them along with the Scene micro-workflows the band held for A:B. One workflow
        /// per stage is the point: ProblemBuilder carries the problem, SceneBuilder the scene.
        /// </summary>
        public static readonly IReadOnlyList<string> DefaultStarredLabels = new List<string>
        {
            "Premise",
            "StoryProblem",
            "ProblemBuilder",
            "StoryFunction",
            "SceneBuilder"
        };

        /// <summary>
        /// How far stored stars have been carried forward. Raise this, and add to
        /// <see cref="RetiredWorkflowReplacements"/>, whenever a consolidation deletes a workflow
        /// a user could have starred. WorkflowStarService compares it against the number in the
        /// user's preferences and migrates once.
        /// #224 raised this to 2 for the two Setting labels SettingBuilder absorbed.
        /// </summary>
        public const int StarMigrationVersion = 2;

        /// <summary>
        /// Deleted workflow label to the workflow that absorbed its job (#211). A user who
        /// starred GMC starred the goal-motive-conflict step, and ProblemBuilder is what does
        /// that step now, so the star moves rather than disappearing. Only used to rewrite
        /// stored stars; nothing here is registered, and every value must be.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> RetiredWorkflowReplacements =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                // #77: ProblemBuilder is the one Problem surface.
                ["ConflictBuilder"] = "ProblemBuilder",
                ["GMC"] = "ProblemBuilder",
                ["Structure"] = "ProblemBuilder",
                ["BeatScenes"] = "ProblemBuilder",
                // #208: SceneBuilder is the one Scene surface.
                ["SceneSummary"] = "SceneBuilder",
                ["CastSceneRoles"] = "SceneBuilder",
                ["SceneDevelopment"] = "SceneBuilder",
                ["SceneConflict"] = "SceneBuilder",
                ["Sequel"] = "SceneBuilder",
                // #224: SettingBuilder is the one Setting surface.
                ["SettingTimeSpace"] = "SettingBuilder",
                ["Sensations"] = "SettingBuilder"
            };

        /// <summary>
        /// Gets a workflow by its label.
        /// </summary>
        public static Workflow? Get(string label) =>
            All.FirstOrDefault(w => w.Label == label);

        /// <summary>
        /// Creates all workflow instances.
        /// Workflows with prompts are fully implemented; those without show as stubs.
        /// </summary>
        private static List<Workflow> CreateWorkflows()
        {
            var list = new List<Workflow>
            {
                // === Overview Workflows ===

                // Premise workflow - full WorkflowIO
                new Workflow(
                    label: "Premise",
                    title: "Ideation (Story idea => Concept => Premise)",
                    description: "The goal of this workflow is to ensure that a workable premise has been created, usually " +
                                "from the idea and concept, or from a story prompt.",
                    explanation: "The Story Overview form, the root of the " +
                                "Story Explorer tree, contains tabs with text fields for Story Idea, Concept, and Premise which " +
                                "are usually the starting place for your story.\r\n " +
                                "Premise is unique in that every Problem Story Element (form) contains a Premise of its own " +
                                "in its Resolution tab. This is because a StoryCAD Premise is a condensation or " +
                                "synopsis of the problem, and can be written as a one-sentence 'structured English' fashion " +
                                "with the parts of a problem: a protagonist with a goal, motivation, and conflict in the form " +
                                "of an antagonist (see GMC).\r\n " +
                                "Only one Problem, however, is the main story problem- the problem which, when concluded, resolves " +
                                "the story. Other Problems, and eventually the Scenes that describe their arcs, are complications, " +
                                "subplots, and sequences, and are subordinate the main problem. Together, the problems and their " +
                                "child scenes are the vehicle for the story's plot.",
                    workflowIO: new WorkflowIO
                    {
                        RequiredInputs = new List<ElementRequirement>
                        {
                            new ElementRequirement
                            {
                                ElementType = StoryItemType.StoryOverview,
                                ElementLabel = "Overview",
                                RequiredProperties = new List<PropertySpec>(),
                                CreateIfMissing = false
                            }
                        },
                        OptionalInputs = new List<ElementRequirement>
                        {
                            new ElementRequirement
                            {
                                ElementType = StoryItemType.StoryOverview,
                                ElementLabel = "Overview",
                                RequiredProperties = new List<PropertySpec> { new PropertySpec("Concept"), new PropertySpec("Premise") },
                                CreateIfMissing = false
                            }
                        },
                        Outputs = new List<ElementOutput>
                        {
                            new ElementOutput
                            {
                                ElementType = StoryItemType.StoryOverview,
                                ElementLabel = "Overview",
                                PropertiesToUpdate = new List<PropertySpec>
                                {
                                    new PropertySpec("Description"),
                                    new PropertySpec("Concept"),
                                    new PropertySpec("Premise")
                                },
        
                            }
                        }
                    }) { PrimaryElementType = StoryItemType.StoryOverview },

                // Story Problem workflow - full WorkflowIO
                new Workflow(
                    label: "StoryProblem",
                    title: "Story Problem (Premise => Problem + Characters)",
                    description: "Transform a developed Premise into a complete Story Problem with " +
                                "linked Protagonist and Antagonist characters.",
                    explanation: "The Premise you developed contains the core elements of your story problem: " +
                                "a protagonist with a goal and motivation, an antagonist providing opposition, " +
                                "and the central conflict between them.\r\n\r\n" +
                                "This workflow extracts those elements and structures them into:\r\n" +
                                "• A Problem story element (your main Story Problem)\r\n" +
                                "• A Protagonist character element\r\n" +
                                "• An Antagonist character element\r\n\r\n" +
                                "The Problem will be linked to the Overview as the Story Problem, and the " +
                                "Protagonist and Antagonist characters will be linked to the Problem.\r\n\r\n" +
                                "After this workflow completes, you'll have a structured foundation for your story " +
                                "that can be further developed with scenes and additional problems (subplots).",
                    workflowIO: new WorkflowIO
                    {
                        RequiredInputs = new List<ElementRequirement>
                        {
                            new ElementRequirement
                            {
                                ElementType = StoryItemType.StoryOverview,
                                ElementLabel = "Overview",
                                RequiredProperties = new List<PropertySpec> { new PropertySpec("Premise") },
                                CreateIfMissing = false
                            }
                        },
                        // Gather order: Problem, then cast. ReferencedElementLabel writes structural
                        // GUID links at pick/create time (Collaborator #118): Overview.StoryProblem,
                        // Problem.Protagonist, Problem.Antagonist — same path as GMC's references.
                        OptionalInputs = new List<ElementRequirement>
                        {
                            new ElementRequirement
                            {
                                ElementType = StoryItemType.Problem,
                                ElementLabel = "Problem",
                                RequiredProperties = new List<PropertySpec>(),
                                CreateIfMissing = true,
                                ReferencedElementLabel = "Overview.StoryProblem"
                            },
                            new ElementRequirement
                            {
                                ElementType = StoryItemType.Character,
                                ElementLabel = "Protagonist",
                                RequiredProperties = new List<PropertySpec>(),
                                CreateIfMissing = true,
                                ReferencedElementLabel = "Problem.Protagonist"
                            },
                            new ElementRequirement
                            {
                                ElementType = StoryItemType.Character,
                                ElementLabel = "Antagonist",
                                RequiredProperties = new List<PropertySpec>(),
                                CreateIfMissing = true,
                                ReferencedElementLabel = "Problem.Antagonist"
                            }
                        },
                        Outputs = new List<ElementOutput>
                        {
                            new ElementOutput
                            {
                                ElementType = StoryItemType.Problem,
                                ElementLabel = "Problem",
                                PropertiesToUpdate = new List<PropertySpec>
                                {
                                    new PropertySpec("Name"),
                                    // ProblemCategory is not LLM: set at gather when Overview.StoryProblem is linked.
                                    new PropertySpec("ProblemType"),
                                    new PropertySpec("ConflictType"),
                                    new PropertySpec("Subject"),
                                    // Bug 1: StoryQuestion does not exist on ProblemModel; it was folded
                                    // into StoryElement.Description (StoryCAD issue #1102). The prompt
                                    // emits the key "StoryQuestion"; we write to Description.
                                    new PropertySpec("Description", JsonKey: "StoryQuestion"),
                                    new PropertySpec("ProblemSource"),
                                    new PropertySpec("ProtGoal"),
                                    new PropertySpec("ProtMotive"),
                                    new PropertySpec("ProtConflict"),
                                    new PropertySpec("AntagGoal"),
                                    new PropertySpec("AntagMotive"),
                                    new PropertySpec("AntagConflict"),
                                    new PropertySpec("Premise"),
                                    // Resolution tab (#118 option A)
                                    new PropertySpec("Outcome"),
                                    new PropertySpec("Method"),
                                    new PropertySpec("Theme")
                                },
        
                            },
                            new ElementOutput
                            {
                                ElementType = StoryItemType.Character,
                                ElementLabel = "Protagonist",
                                // Prompt emits protagonist_name; write to Name.
                                PropertiesToUpdate = new List<PropertySpec> { new PropertySpec("Name", JsonKey: "protagonist_name") },
        
                            },
                            new ElementOutput
                            {
                                ElementType = StoryItemType.Character,
                                ElementLabel = "Antagonist",
                                // Prompt emits antagonist_name; write to Name.
                                PropertiesToUpdate = new List<PropertySpec> { new PropertySpec("Name", JsonKey: "antagonist_name") },
        
                            }
                        }
                    }) { PrimaryElementType = StoryItemType.StoryOverview },

                // Story Form - simple workflow
                new Workflow(
                    "StoryForm", "Story Form",
                    "Define the story's genre, length, and structural form.",
                    StoryItemType.StoryOverview,
                    explanation: "Story form decisions shape reader expectations and your writing approach. Genre " +
                                "establishes conventions your audience expects (or that you'll deliberately subvert). " +
                                "Story type—novel, novella, short story, screenplay—determines scope and pacing. " +
                                "This workflow helps you make these foundational choices early, when they can guide " +
                                "rather than constrain your outlining.",
                    outputProperties: new List<PropertySpec> { new PropertySpec("StoryGenre"), new PropertySpec("StoryType") }),

                // === Problem Workflows ===

                new Workflow(
                    label: "InnerOuterProblems",
                    title: "Inner and Outer Problems",
                    description: "Given an outer (external goal) problem, develop a complementary inner problem representing " +
                                 "the protagonist's internal struggle—rooted in a flaw or wound that must be overcome before " +
                                 "the outer problem can truly be resolved.",
                    explanation: "Every compelling protagonist pursues an external goal (the Want) while unknowingly " +
                                 "needing internal growth (the Need). This workflow helps you create that inner problem " +
                                 "as a separate Problem element. If your character already has a Flaw or Backstory " +
                                 "defined, the workflow uses those as the source of the inner struggle. If not, it " +
                                 "will suggest what flaw or wound might explain the inner problem.",
                    workflowIO: new WorkflowIO
                    {
                        RequiredInputs = new List<ElementRequirement>
                        {
                            new ElementRequirement
                            {
                                ElementType = StoryItemType.Problem,
                                ElementLabel = "OuterProblem",
                                RequiredProperties = new List<PropertySpec>(),
                                CreateIfMissing = false
                            },
                            new ElementRequirement
                            {
                                ElementType = StoryItemType.Problem,
                                ElementLabel = "InnerProblem",
                                RequiredProperties = new List<PropertySpec>(),
                                CreateIfMissing = true
                            },
                            // Full protagonist for Protagonist_Name / Flaw / BackStory placeholders (#106).
                            new ElementRequirement
                            {
                                ElementType = StoryItemType.Character,
                                ElementLabel = "Protagonist",
                                ReferencedElementLabel = "OuterProblem.Protagonist",
                                RequiredProperties = new List<PropertySpec>(),
                                CreateIfMissing = false
                            }
                        },
                        Outputs = new List<ElementOutput>
                        {
                            // #120: full Inner Problem form + Protagonist.Flaw.
                            // ConflictType + Protagonist/Antagonist GUIDs are injected after
                            // extract (Person vs. Self; both links = gathered Protagonist).
                            new ElementOutput
                            {
                                ElementType = StoryItemType.Problem,
                                ElementLabel = "InnerProblem",
                                PropertiesToUpdate = new List<PropertySpec>
                                {
                                    new PropertySpec("Description", JsonKey: "InnerProblemDescription"),
                                    new PropertySpec("Theme", JsonKey: "theme_connection"),
                                    new PropertySpec("Method", JsonKey: "resolution_path"),
                                    new PropertySpec("Notes", JsonKey: "explanation"),
                                    // Craft: inner problem is usually something to decide or discover
                                    // (Defining_Problems / Problem and Character Process).
                                    new PropertySpec("ProblemType"),
                                    new PropertySpec("ProtGoal"),
                                    new PropertySpec("ProtMotive"),
                                    new PropertySpec("ProtConflict"),
                                    new PropertySpec("AntagGoal"),
                                    new PropertySpec("AntagMotive"),
                                    new PropertySpec("AntagConflict")
                                }
                            },
                            new ElementOutput
                            {
                                ElementType = StoryItemType.Character,
                                ElementLabel = "Protagonist",
                                PropertiesToUpdate = new List<PropertySpec>
                                {
                                    new PropertySpec("Flaw")
                                }
                            }
                        }
                    }) { PrimaryElementType = StoryItemType.Problem },

                // #77 ProblemBuilder: one Problem surface. Consolidates ConflictBuilder, GMC,
                // Structure, and BeatScenes. Writes the RequiredFieldGapScanner spine, chooses a
                // beat sheet, fills empty beats, and creates Scene stubs for #208.
                // ProblemCategory, Protagonist, and Antagonist are preconditions, not outputs.
                new Workflow(
                    label: "ProblemBuilder",
                    title: "Problem Builder",
                    description: "Fill a Problem: goal, motive, conflict, outcome, and a beat sheet " +
                                 "with scenes for its empty beats.",
                    explanation: "One pass over a Problem you selected. It writes the fields a Problem " +
                                 "needs to be usable, chooses a beat sheet that fits the Problem Category " +
                                 "and Conflict Type, binds free scenes and problems to empty beats, and " +
                                 "creates scene stubs for the rest. It never changes a beat you filled and " +
                                 "never adds beats to a sheet you already chose. Set Problem Category, " +
                                 "Protagonist, and Antagonist before you run it.",
                    workflowIO: new WorkflowIO
                    {
                        RequiredInputs = new List<ElementRequirement>
                        {
                            new ElementRequirement
                            {
                                ElementType = StoryItemType.Problem,
                                ElementLabel = "Problem",
                                RequiredProperties = new List<PropertySpec>(),
                                CreateIfMissing = false
                            },
                            new ElementRequirement
                            {
                                ElementType = StoryItemType.StoryOverview,
                                ElementLabel = "Overview",
                                RequiredProperties = new List<PropertySpec>(),
                                CreateIfMissing = false
                            },
                            new ElementRequirement
                            {
                                ElementType = StoryItemType.Character,
                                ElementLabel = "Protagonist",
                                ReferencedElementLabel = "Problem.Protagonist",
                                RequiredProperties = new List<PropertySpec>(),
                                CreateIfMissing = false
                            },
                            new ElementRequirement
                            {
                                ElementType = StoryItemType.Character,
                                ElementLabel = "Antagonist",
                                ReferencedElementLabel = "Problem.Antagonist",
                                RequiredProperties = new List<PropertySpec>(),
                                CreateIfMissing = false
                            }
                        },
                        OptionalInputs = new List<ElementRequirement>(),
                        Outputs = new List<ElementOutput>
                        {
                            new ElementOutput
                            {
                                ElementType = StoryItemType.Problem,
                                ElementLabel = "Problem",
                                PropertiesToUpdate = new List<PropertySpec>
                                {
                                    new PropertySpec("Name"),
                                    new PropertySpec("Description", JsonKey: "StoryQuestion"),
                                    new PropertySpec("ProblemType"),
                                    new PropertySpec("ConflictType"),
                                    new PropertySpec("Subject"),
                                    new PropertySpec("ProblemSource"),
                                    new PropertySpec("Premise"),
                                    new PropertySpec("ProtGoal"),
                                    new PropertySpec("ProtMotive"),
                                    new PropertySpec("ProtConflict"),
                                    new PropertySpec("AntagGoal"),
                                    new PropertySpec("AntagMotive"),
                                    new PropertySpec("AntagConflict"),
                                    new PropertySpec("Outcome"),
                                    new PropertySpec("Method"),
                                    new PropertySpec("Theme"),
                                    new PropertySpec("Notes", JsonKey: "situation_sheet"),
                                    new PropertySpec("StructureTitle"),
                                    new PropertySpec("StructureDescription"),
                                    new PropertySpec("StructureBeats", WriteVia.BeatSheet, JsonKey: "beats")
                                }
                            }
                        },
                        // Every list-backed output needs its examples, or the model invents a
                        // value the StoryCAD dropdown cannot show (a live run returned
                        // ProblemType "Integrity"; Lists.json allows three values).
                        ExampleLists = new List<string>
                        {
                            "ProblemType", "ProblemSource", "Outcome", "Method", "Theme",
                            "Motive", "ConflictType", "ProblemCategory",
                            // #208 handoff: a created stub carries SceneType.
                            "SceneType"
                        },
                        CollectionInputs = new List<CollectionInput>
                        {
                            // #217 rule 5: free elements for this Problem's sheet only.
                            new CollectionInput
                            {
                                RequestName = "ProblemChoices",
                                ElementType = StoryItemType.Problem,
                                Projection = ElementProjection.BaseStoryElement,
                                FreeElementsFor = "Problem"
                            },
                            new CollectionInput
                            {
                                RequestName = "SceneChoices",
                                ElementType = StoryItemType.Scene,
                                Projection = ElementProjection.BaseStoryElement,
                                FreeElementsFor = "Problem"
                            },
                            // #208 handoff: cast on a created stub, resolved from this set.
                            new CollectionInput
                            {
                                RequestName = "CharacterChoices",
                                ElementType = StoryItemType.Character,
                                Projection = ElementProjection.IdAndName
                            }
                        }
                    })
                {
                    PrimaryElementType = StoryItemType.Problem,
                    CreatesScenesForBeats = true,
                    RequiresProblemCategory = true,
                    InjectsConflictTaxonomy = true,
                    InjectsBeatSheets = true,
                    InjectsStockScenes = true,
                    InjectsCurrentBeats = true
                },
                // === Character Workflows ===
                // #182 DefineCharacter: world identity + personality. Occupation Role lives here.
                new Workflow(
                    "DefineCharacter", "Define Character",
                    "Define who this person is in the world: occupation, body, social background, " +
                    "psychology, personality facets, and traits—kept coherent with each other and with " +
                    "problems that link this character.",
                    StoryItemType.Character,
                    explanation: "Build a coherent person sheet in one pass. Occupation (Role), appearance, " +
                                "class and culture, psych profile, and traits must fit together and fit " +
                                "problems where this character is protagonist or antagonist. " +
                                "Does not set Story Role, Character Sketch, Flaw, or Backstory.",
                    outputProperties: new List<PropertySpec>
                    {
                        new PropertySpec("Role"),
                        new PropertySpec("Age"),
                        new PropertySpec("Sex"),
                        new PropertySpec("Eyes"),
                        new PropertySpec("Hair"),
                        new PropertySpec("Build"),
                        new PropertySpec("Complexion"),
                        new PropertySpec("Appearance"),
                        new PropertySpec("Economic"),
                        new PropertySpec("Education"),
                        new PropertySpec("Ethnic"),
                        new PropertySpec("Religion"),
                        new PropertySpec("Enneagram"),
                        new PropertySpec("Intelligence"),
                        new PropertySpec("Values"),
                        new PropertySpec("Abnormality"),
                        new PropertySpec("Focus"),
                        new PropertySpec("Adventurousness"),
                        new PropertySpec("Aggression"),
                        new PropertySpec("Confidence"),
                        new PropertySpec("Conscientiousness"),
                        new PropertySpec("Creativity"),
                        new PropertySpec("Dominance"),
                        new PropertySpec("Enthusiasm"),
                        new PropertySpec("Assurance"),
                        new PropertySpec("Sensitivity"),
                        new PropertySpec("Shrewdness"),
                        new PropertySpec("Sociability"),
                        new PropertySpec("Stability"),
                        new PropertySpec("TraitList", WriteVia.SimpleList, ListEntryType: typeof(string))
                    },
                    exampleLists: new List<string>
                    {
                        "Role", "Build", "Eyes", "Hair", "Complexion", "Race", "Nationality",
                        "Enneagram", "Intelligence", "Values", "Abnormality", "Focus", "Trait",
                        "Adventurousness", "Aggression", "Confidence", "Conscientiousness",
                        "Creativity", "Dominance", "Enthusiasm", "Assurance", "Sensitivity",
                        "Shrewdness", "Sociability", "Stability"
                    }),
                // #183 StoryFunction: plot function only. Occupation Role is DefineCharacter.
                new Workflow(
                    "StoryFunction", "Character Story Function",
                    "Define the character's plot function: Story Role, Archetype, and Character Sketch.",
                    StoryItemType.Character,
                    explanation: "Story Role is narrative function (Protagonist, Antagonist, Supporting). " +
                                "Archetype is the universal pattern (Hero, Mentor, Shadow). " +
                                "Character Sketch (Description) is short story-function prose from those choices, " +
                                "Related Problems, Flaw when present, and story premise—not a physical biography. " +
                                "Occupation Role is set by Define Character, not this workflow.",
                    outputProperties: new List<PropertySpec>
                    {
                        new PropertySpec("StoryRole"),
                        new PropertySpec("Archetype"),
                        // Character Sketch (gap label); Collaborator #142
                        new PropertySpec("Description")
                    },
                    exampleLists: new List<string> { "StoryRole", "Archetype" }),
                // #184 FlawBackstory: wound + history together. Retires Flaw and Backstory.
                new Workflow(
                    "FlawBackstory", "Flaw and Backstory",
                    "Identify the character's central flaw and the formative history that grounds it.",
                    StoryItemType.Character,
                    explanation: "Flaw is the weakness or blind spot that creates internal cost. BackStory is formative " +
                                "history. When empty, this run fills focused history that grounds the flaw. When already " +
                                "filled, this run keeps existing facts and weaves Ghost and wound into them. Related " +
                                "Problems bound stakes. Prefer rows marked Person vs. Self when present. Problem workflow " +
                                "Inner and Outer Problems may also write Flaw from the problem side; last Accept wins. " +
                                "Does not set Story Role, Character Sketch, bulk sheet fields, or Relationship.",
                    outputProperties: new List<PropertySpec>
                    {
                        new PropertySpec("Flaw"),
                        new PropertySpec("BackStory")
                    },
                    exampleLists: new List<string> { "Wound", "WoundCategory" }),
                new Workflow(
                    label: "Relationship",
                    title: "Character Relationship",
                    description: "Develop the dynamics, history, and tension between two characters.",
                    explanation: "Name both people. Prefer some sheet fill from Define Character, Character Story Function, or Flaw and Backstory on each side. " +
                                "The run still proceeds if sheets are thin. The model uses filled traits when they exist. It does not invent missing bulk fields. " +
                                "Accept writes the short type, Trait, Attitude, and Relationship Notes on both people.",
                    workflowIO: new WorkflowIO
                    {
                        // Primary + Partner full elements for Partner_* placeholders (#106).
                        RequiredInputs = new List<ElementRequirement>
                        {
                            new ElementRequirement
                            {
                                ElementType = StoryItemType.Character,
                                ElementLabel = "Character",
                                RequiredProperties = new List<PropertySpec>(),
                                CreateIfMissing = false
                            },
                            new ElementRequirement
                            {
                                ElementType = StoryItemType.Character,
                                ElementLabel = "Partner",
                                RequiredProperties = new List<PropertySpec>(),
                                CreateIfMissing = false
                            }
                        },
                        Outputs = new List<ElementOutput>
                        {
                            new ElementOutput
                            {
                                ElementType = StoryItemType.Character,
                                ElementLabel = "Character",
                                PropertiesToUpdate = new List<PropertySpec>
                                {
                                    new PropertySpec("RelationshipList", WriteVia.Relationships, JsonKey: "relationship")
                                }
                            }
                        },
                        CollectionInputs = new List<CollectionInput>
                        {
                            new CollectionInput
                            {
                                RequestName = "CharacterChoices",
                                ElementType = StoryItemType.Character,
                                Projection = ElementProjection.IdAndName
                            }
                        },
                        ExampleLists = new List<string> { "Trait", "Attitude" }
                    }) { PrimaryElementType = StoryItemType.Character },

                // === Character Interview (#119) ===
                // Registered inside the Character block, not after it: the nav pane opens a
                // new group whenever PrimaryElementType changes, so a Character entry after
                // the Scene entries would render a second "Character" header (#129 grouping).

                // Conversational: one call per section, prose back, no proposals of its own.
                new Workflow(
                    label: "CharacterInterview",
                    title: "Character Interview",
                    description: "Interview a character in their own voice, a section of their life at a time.",
                    explanation: "Other character workflows fill the form. This one lets you hear the " +
                                 "character: you pick which parts of their life to ask about, they answer " +
                                 "in first person, and you can break in with your own questions at any " +
                                 "point. Nothing is written to the outline until you press Summarize.",
                    workflowIO: new WorkflowIO
                    {
                        RequiredInputs = new List<ElementRequirement>
                        {
                            new ElementRequirement
                            {
                                ElementType = StoryItemType.Character,
                                ElementLabel = "Character",
                                CreateIfMissing = false
                            }
                        },
                        OptionalInputs = new List<ElementRequirement>
                        {
                            new ElementRequirement
                            {
                                ElementType = StoryItemType.StoryOverview,
                                ElementLabel = "Overview"
                            },
                            new ElementRequirement
                            {
                                ElementType = StoryItemType.Problem,
                                ElementLabel = "Problem"
                            }
                        },
                        Outputs = new List<ElementOutput>()
                    })
                {
                    PrimaryElementType = StoryItemType.Character,
                    Mode = WorkflowMode.Conversational
                },

                // One-shot: reads the finished transcript, proposes fields through the normal path.
                // Off the menu (ShowInMenu): its only input that matters is the transcript, which
                // exists solely inside an interview session. Run directly it would propose a
                // character's backstory and flaw from nothing.
                new Workflow(
                    label: "CharacterInterviewSummary",
                    title: "Character Interview Summary",
                    description: "Turn a finished interview into Notes and character fields.",
                    explanation: "Writes the questions and answers into Notes, and proposes the character " +
                                 "fields the answers actually grounded. Anything the interview invented is " +
                                 "marked as invention rather than recall.",
                    workflowIO: new WorkflowIO
                    {
                        RequiredInputs = new List<ElementRequirement>
                        {
                            new ElementRequirement
                            {
                                ElementType = StoryItemType.Character,
                                ElementLabel = "Character",
                                CreateIfMissing = false
                            }
                        },
                        Outputs = new List<ElementOutput>
                        {
                            new ElementOutput
                            {
                                ElementType = StoryItemType.Character,
                                ElementLabel = "Character",
                                PropertiesToUpdate = new List<PropertySpec>
                                {
                                    new PropertySpec("Notes"),
                                    new PropertySpec("BackStory"),
                                    new PropertySpec("Flaw"),
                                    new PropertySpec("Values"),
                                    new PropertySpec("PsychNotes"),
                                    new PropertySpec("Education"),
                                    new PropertySpec("Nationality"),
                                    new PropertySpec("Ethnic")
                                }
                            }
                        }
                    })
                {
                    PrimaryElementType = StoryItemType.Character,
                    Mode = WorkflowMode.OneShot,
                    ShowInMenu = false
                },

                // === StoryWorld Workflows ===
                // #201 DefineStoryWorld: one worldbuilding surface (classifier + cultures + live areas).
                new Workflow(
                    label: "DefineStoryWorld",
                    title: "Define Story World",
                    description: "Classify the story world and fill worldbuilding fields this World Type needs.",
                    explanation: "Sets World Type, a short world tell (Description), cultures, and only the " +
                                "Physical / History / Magic-Technology areas that World Type makes live. " +
                                "May create the StoryWorld element when the outline has none. " +
                                "Does not fill Setting places, Species, Governments, Religions, or Economy. " +
                                "Axis fields come from the host World Type map on Accept, not from the model.",
                    workflowIO: new WorkflowIO
                    {
                        RequiredInputs = new List<ElementRequirement>
                        {
                            new ElementRequirement
                            {
                                ElementType = StoryItemType.StoryWorld,
                                ElementLabel = "StoryWorld",
                                RequiredProperties = new List<PropertySpec>(),
                                CreateIfMissing = true
                            },
                            new ElementRequirement
                            {
                                ElementType = StoryItemType.StoryOverview,
                                ElementLabel = "Overview",
                                RequiredProperties = new List<PropertySpec>(),
                                CreateIfMissing = false
                            }
                        },
                        OptionalInputs = new List<ElementRequirement>(),
                        Outputs = new List<ElementOutput>
                        {
                            new ElementOutput
                            {
                                ElementType = StoryItemType.StoryWorld,
                                ElementLabel = "StoryWorld",
                                PropertiesToUpdate = new List<PropertySpec>
                                {
                                    new PropertySpec("Name"),
                                    new PropertySpec("WorldType"),
                                    new PropertySpec("Description"),
                                    new PropertySpec("FoundingEvents"),
                                    new PropertySpec("MajorConflicts"),
                                    new PropertySpec("Eras"),
                                    new PropertySpec("TechnologicalShifts"),
                                    new PropertySpec("LostKnowledge"),
                                    new PropertySpec("SystemType"),
                                    new PropertySpec("Source"),
                                    new PropertySpec("Rules"),
                                    new PropertySpec("Limitations"),
                                    new PropertySpec("Cost"),
                                    new PropertySpec("Practitioners"),
                                    new PropertySpec("SocialImpact"),
                                    new PropertySpec("Cultures", WriteVia.TypedList,
                                        ListEntryType: typeof(CultureEntry)),
                                    new PropertySpec("PhysicalWorlds", WriteVia.TypedList,
                                        ListEntryType: typeof(PhysicalWorldEntry))
                                }
                            }
                        },
                        ExampleLists = new List<string> { "WorldType", "SystemType" }
                    }) { PrimaryElementType = StoryItemType.StoryWorld },

                // === Setting Workflows ===
                // #224: one Setting-primary surface. SettingTimeSpace and Sensations
                // merged. Sensations read seven properties SettingTimeSpace writes, from
                // the stored element, so the sensory pass only saw values the writer had
                // already accepted from an earlier run. One pass makes that order
                // internal (ADR-008).
                //
                // Props and Notes gain a writer here. Nothing wrote Props while
                // Sensations consumed it, so the objects that make the sounds and the
                // textures were only ever the writer's own.
                //
                // exampleLists carries Locale only. SettingPage.xaml binds Locale and Season
                // to ComboBox IsEditable="True", so both are suggestion lists rather than
                // closed lists, and the prompt Writes both. Season is deliberately NOT
                // declared: three of its seven Lists.json members carry a classifying
                // parenthetical, and Season is a short field where one member is the whole
                // answer, so injecting the list made live runs open the property with the
                // catalog label ("Winter (Bare). The facility leans on closed doors...").
                // An explicit prohibition cut that but did not close it. Neither merged
                // workflow declared a list, and both wrote sound Seasons without one.
                new Workflow(
                    "SettingBuilder", "Setting Builder",
                    "Build a Setting: time, place, conditions, props, and the four senses.",
                    StoryItemType.Setting,
                    explanation: "Setting is more than backdrop—it shapes mood, creates obstacles, and reflects theme. " +
                                "This workflow places your setting in time and space, names the objects a scene can " +
                                "reach for, and then works through each sense in turn. The senses read the place the " +
                                "same run just built, so light, weather and props are already on the page when the " +
                                "sensory detail is written. Smell is particularly powerful—primitive and emotional, " +
                                "it can pull readers deep into your story world.",
                    outputProperties: new List<PropertySpec>
                    {
                        new PropertySpec("Period"),
                        new PropertySpec("Locale"),
                        new PropertySpec("Season"),
                        new PropertySpec("Weather"),
                        new PropertySpec("Lighting"),
                        new PropertySpec("Temperature"),
                        new PropertySpec("Props"),
                        new PropertySpec("Sights"),
                        new PropertySpec("Sounds"),
                        new PropertySpec("Touch"),
                        new PropertySpec("SmellTaste"),
                        new PropertySpec("Notes")
                    },
                    exampleLists: new List<string> { "Locale" }),
                // SettingCreateImage removed; preserved on branch issue-76-image-workflows (issue #76).

                // === Scene Workflows ===
                // #208: one Scene-primary surface. Five micro-workflows stay registered for A:B.
                // Required Scene only. Problem / neighbors / contributing Problems are inject-only.
                new Workflow(
                    label: "SceneBuilder",
                    title: "Scene Builder",
                    description: "Fill an existing Scene: sketch, type, cast, development, conflict, and sequel.",
                    explanation: "Scene Builder does not create a Scene. Select a Scene first. The run reads contributing Problems and writes Scene fields the model can infer. It does not run on a Story Problem.",
                    workflowIO: new WorkflowIO
                    {
                        RequiredInputs = new List<ElementRequirement>
                        {
                            new ElementRequirement
                            {
                                ElementType = StoryItemType.Scene,
                                ElementLabel = "Scene",
                                RequiredProperties = new List<PropertySpec>(),
                                CreateIfMissing = false
                            }
                        },
                        OptionalInputs = new List<ElementRequirement>(),
                        Outputs = new List<ElementOutput>
                        {
                            new ElementOutput
                            {
                                ElementType = StoryItemType.Scene,
                                ElementLabel = "Scene",
                                PropertiesToUpdate = new List<PropertySpec>
                                {
                                    new PropertySpec("Description"),
                                    new PropertySpec("SceneType"),
                                    new PropertySpec("CastMembers", WriteVia.CastMembers, JsonKey: "cast"),
                                    new PropertySpec("Protagonist"),
                                    new PropertySpec("Antagonist"),
                                    new PropertySpec("ScenePurpose", WriteVia.SimpleList, ListEntryType: typeof(string)),
                                    new PropertySpec("ValueExchange"),
                                    new PropertySpec("Events"),
                                    new PropertySpec("Consequences"),
                                    new PropertySpec("Significance"),
                                    new PropertySpec("Realization"),
                                    new PropertySpec("ProtagGoal"),
                                    new PropertySpec("Opposition"),
                                    new PropertySpec("Outcome"),
                                    new PropertySpec("AntagGoal"),
                                    new PropertySpec("ProtagEmotion"),
                                    new PropertySpec("AntagEmotion"),
                                    new PropertySpec("Emotion"),
                                    new PropertySpec("Review"),
                                    new PropertySpec("NewGoal"),
                                    new PropertySpec("ViewpointCharacter"),
                                    new PropertySpec("Setting"),
                                    new PropertySpec("Date"),
                                    new PropertySpec("Time"),
                                    new PropertySpec("Notes")
                                }
                            }
                        },
                        CollectionInputs = new List<CollectionInput>
                        {
                            new CollectionInput
                            {
                                RequestName = "CharacterChoices",
                                ElementType = StoryItemType.Character,
                                Projection = ElementProjection.IdAndName
                            }
                        },
                        ExampleLists = new List<string>
                        {
                            "SceneType", "ScenePurpose", "ValueExchange", "Emotion",
                            "Goal", "Opposition", "Outcome"
                        }
                    }) { PrimaryElementType = StoryItemType.Scene },
                // SceneCreateImage removed; preserved on branch issue-76-image-workflows (issue #76).
            };

            // Issue #106 attached CharacterChoices to CastSceneRoles here; #211 deleted that
            // workflow. Every surviving workflow declares its CollectionInputs on its own
            // WorkflowIO above, so nothing is patched onto the list after it is built.
            return list;
        }
    }
}
