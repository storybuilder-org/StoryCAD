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
        /// problem gets a shape, cast has function, scenes happen and scenes have conflict — so
        /// the top band reads as a next action rather than a catalog. Seeded once by
        /// WorkflowStarService; after that the user's choices win.
        /// Both scene workflows are starred because the user manual's A Path to Try tells writers
        /// to prefer Scene Summary when running only one; starring Scene Conflict alone would put
        /// the product at odds with its own craft guidance.
        /// </summary>
        public static readonly IReadOnlyList<string> DefaultStarredLabels = new List<string>
        {
            "Premise",
            "StoryProblem",
            "GMC",
            "Structure",
            "StoryFunction",
            "SceneSummary",
            "SceneConflict"
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

                // GMC workflow - full WorkflowIO
                new Workflow(
                    label: "GMC",
                    title: "Goal / Motivation / Conflict (GMC)",
                    description: "The goal of this workflow is to ensure that a Problem Story Element " +
                                "is a well-formed problem capable of contributing to the story's plot.",
                    explanation: "A story is a narrative that revolves around a character facing " +
                                "a conflict or problem. A problem arises when a character's " +
                                "attempt to achieve their goal, motivated by a need or want, is " +
                                "obstructed by a conflict that prevents its easy achievement.\r\n " +
                                "StoryCAD's Problem form contains a tab which describes your protagonist's " +
                                "Goal, Motivation, and Conflict. Another tab does the same for the antagonist, " +
                                "because the antagonist is often the main source of conflict for the protagonist. " +
                                "Even a non-human conflict can be thought of as an antagonist through personification, " +
                                "by giving it a goal. For example, a storm might 'want' to destroy a town.\r\n " +
                                "Defining your story problems through GMC makes it easier to create the scenes which " +
                                "describe the protagonist's pursuit of their goal, the motives which drive the quest, " +
                                "and the obstacles that challenge the protagonist's progress.",
                    workflowIO: new WorkflowIO
                    {
                        // Full characters so Worker can fill Protagonist_Name / Antagonist_Name (issue #106).
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
                        Outputs = new List<ElementOutput>
                        {
                            new ElementOutput
                            {
                                ElementType = StoryItemType.Problem,
                                ElementLabel = "Problem",
                                PropertiesToUpdate = new List<PropertySpec>
                                {
                                    new PropertySpec("ProtGoal"),
                                    new PropertySpec("ProtMotive"),
                                    new PropertySpec("ProtConflict"),
                                    new PropertySpec("AntagGoal"),
                                    new PropertySpec("AntagMotive"),
                                    new PropertySpec("AntagConflict"),
                                    new PropertySpec("ProblemType"),
                                    new PropertySpec("Premise")
                                },

                            }
                        },
                        ExampleLists = new List<string> { "ConflictType", "Motive" }
                    }) { PrimaryElementType = StoryItemType.Problem },

                new Workflow(
                    "ConflictBuilder", "Conflict Builder",
                    "Use the Conflict Builder tool to develop and intensify the central conflict of a story problem, " +
                    "exploring different conflict categories and escalation patterns.",
                    StoryItemType.Problem,
                    explanation: "Conflict is what prevents your character from achieving their goal. This workflow " +
                                "guides you through the Conflict Builder tool to find conflicts that add complexity " +
                                "and layers of meaning—avoiding both senseless violence and shallow conflicts that " +
                                "resolve too easily.",
                    outputProperties: new List<PropertySpec> { new PropertySpec("ProtConflict"), new PropertySpec("AntagConflict") }),
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
                // Collaborator #167: assign problems/scenes to beats; preserve filled assignments.
                new Workflow(
                    label: "Structure",
                    title: "Problem Structure",
                    description: "Choose a beat sheet and match existing problems and scenes to beats " +
                                "(story problem prefers other problems; other problems prefer scenes).",
                    explanation: "Structure gives your problem shape—a beginning that hooks, a middle that complicates, " +
                                "and an ending that resolves. This workflow chooses a beat sheet (full for the story " +
                                "problem, mini for others) and assigns existing problems and scenes to unfilled beats. " +
                                "It does not create new elements or wipe filled assignments.",
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
                            }
                        },
                        OptionalInputs = new List<ElementRequirement>
                        {
                            // Overview helps the Worker detect the outline story problem.
                            new ElementRequirement
                            {
                                ElementType = StoryItemType.StoryOverview,
                                ElementLabel = "Overview",
                                RequiredProperties = new List<PropertySpec>(),
                                CreateIfMissing = false
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
                                    new PropertySpec("StructureTitle"),
                                    new PropertySpec("StructureDescription"),
                                    new PropertySpec("StructureBeats", WriteVia.BeatSheet, JsonKey: "beats")
                                }
                            }
                        },
                        CollectionInputs = new List<CollectionInput>
                        {
                            new CollectionInput
                            {
                                RequestName = "ProblemChoices",
                                ElementType = StoryItemType.Problem,
                                Projection = ElementProjection.BaseStoryElement
                            },
                            new CollectionInput
                            {
                                RequestName = "SceneChoices",
                                ElementType = StoryItemType.Scene,
                                Projection = ElementProjection.BaseStoryElement
                            }
                        }
                    }) { PrimaryElementType = StoryItemType.Problem },
                // Collaborator #150: invent Scene stubs for empty beats on non–Story Problem categories.
                new Workflow(
                    label: "BeatScenes",
                    title: "Scenes from Beats",
                    description: "Create scene stubs for empty beats on a problem with a beat sheet " +
                                "(complications and other non–story-problem categories).",
                    explanation: "When a problem has a beat sheet with empty slots, this workflow invents " +
                                "one Scene per empty beat: a one-line Name for the central event or conflict, " +
                                "in causal order. It creates each Scene under the problem and assigns it to " +
                                "the beat. It requires a Problem Category and does not run when the category " +
                                "is Story Problem (use Structure instead). Filled beats stay unchanged.",
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
                            }
                        },
                        OptionalInputs = new List<ElementRequirement>
                        {
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
                        Outputs = new List<ElementOutput>
                        {
                            new ElementOutput
                            {
                                ElementType = StoryItemType.Problem,
                                ElementLabel = "Problem",
                                PropertiesToUpdate = new List<PropertySpec>
                                {
                                    new PropertySpec("StructureTitle"),
                                    new PropertySpec("StructureDescription"),
                                    new PropertySpec("StructureBeats", WriteVia.BeatSheet, JsonKey: "beats")
                                }
                            }
                        }
                    }) { PrimaryElementType = StoryItemType.Problem },
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

                // === Setting Workflows (scene-specific) ===
                new Workflow(
                    "SettingTimeSpace", "Setting in Time and Space",
                    "Define a setting's location, time period, season, weather, and atmospheric conditions.",
                    StoryItemType.Setting,
                    explanation: "Setting is more than backdrop—it shapes mood, creates obstacles, and reflects theme. " +
                                "This workflow helps you establish where and when your scene takes place, from broad " +
                                "period and locale down to specific weather and lighting that affect your characters.",
                    outputProperties: new List<PropertySpec>
                    {
                        new PropertySpec("Locale"),
                        new PropertySpec("Season"),
                        new PropertySpec("Period"),
                        new PropertySpec("Lighting"),
                        new PropertySpec("Weather"),
                        new PropertySpec("Temperature")
                    }),
                new Workflow(
                    "Sensations", "Sensory Details",
                    "Develop the sensory details—sights, sounds, touch, smell, and taste—that bring a setting to life.",
                    StoryItemType.Setting,
                    explanation: "Readers experience your setting through character senses. This workflow prompts you " +
                                "to explore each sense, finding specific details that immerse readers in the scene. " +
                                "Smell is particularly powerful—primitive and emotional, it can pull readers deep into " +
                                "your story world.",
                    outputProperties: new List<PropertySpec>
                    {
                        new PropertySpec("Sights"),
                        new PropertySpec("Sounds"),
                        new PropertySpec("Touch"),
                        new PropertySpec("SmellTaste")
                    }),
                // SettingCreateImage removed; preserved on branch issue-76-image-workflows (issue #76).

                // === Scene Workflows ===
                // #174: Scene only on RequiredInputs. Problem / PrecedingScene / NextScene are
                // inject-only after structure resolve (never OptionalInputs).
                new Workflow(
                    label: "SceneSummary",
                    title: "Scene Summary",
                    description: "Create a concise summary of a scene's purpose, content, and role in the larger story.",
                    explanation: "Every scene should earn its place. This workflow helps you articulate what happens " +
                                "in the scene, why it matters, and what would be lost without it—ensuring each scene " +
                                "advances plot, reveals character, or both.",
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
                                    new PropertySpec("Description")
                                }
                            }
                        }
                    }) { PrimaryElementType = StoryItemType.Scene },
                new Workflow(
                    "CastSceneRoles", "Cast and Scene Roles",
                    "Define which characters appear in a scene and what role each plays—protagonist, antagonist, " +
                    "ally, or other function.",
                    StoryItemType.Scene,
                    explanation: "Scenes need characters with purposes. This workflow helps you cast your scene " +
                                "deliberately—who must be there, who serves the scene's goals, and what role each " +
                                "character plays in the scene's conflict.",
                    // Bug 3: CastMembers is List<Guid>; must use CastMembers mechanism, not Scalar.
                    // Runner injects CharacterChoices; model returns chosen GUIDs under key "cast".
                    outputProperties: new List<PropertySpec>
                    {
                        new PropertySpec("CastMembers", WriteVia.CastMembers, JsonKey: "cast")
                    }),
                new Workflow(
                    "SceneDevelopment", "Scene Development",
                    "Develop how a scene advances both the outer plot problem and the protagonist's inner " +
                    "character arc.",
                    StoryItemType.Scene,
                    explanation: "The best scenes work on multiple levels—advancing external plot while developing " +
                                "internal character. This workflow (based on Lisa Cron's Story Genius method) helps " +
                                "you identify what happens, what it means to the protagonist, and how it changes them.",
                    // Bug 4: ScenePurpose is List<string>; must use SimpleList, not Scalar.
                    outputProperties: new List<PropertySpec>
                    {
                        new PropertySpec("ScenePurpose", WriteVia.SimpleList, ListEntryType: typeof(string)),
                        new PropertySpec("ValueExchange"),
                        new PropertySpec("Events"),
                        new PropertySpec("Consequences"),
                        new PropertySpec("Significance"),
                        new PropertySpec("Realization")
                    }),
                new Workflow(
                    label: "SceneConflict",
                    title: "Scene Conflict",
                    description: "Structure the conflict within a scene—the protagonist's goal, the opposition they face, " +
                                 "and the outcome.",
                    explanation: "A scene is a small story with goal, conflict, and outcome. This workflow uses the " +
                                "Actor's Studio method to define what the scene protagonist wants, what opposes them, " +
                                "and how the scene ends—usually in a way that makes things worse.",
                    workflowIO: new WorkflowIO
                    {
                        // Scene + full characters for Protagonist_Name / Antagonist_Name (#106).
                        RequiredInputs = new List<ElementRequirement>
                        {
                            new ElementRequirement
                            {
                                ElementType = StoryItemType.Scene,
                                ElementLabel = "Scene",
                                RequiredProperties = new List<PropertySpec>(),
                                CreateIfMissing = false
                            },
                            new ElementRequirement
                            {
                                ElementType = StoryItemType.Character,
                                ElementLabel = "Protagonist",
                                ReferencedElementLabel = "Scene.Protagonist",
                                RequiredProperties = new List<PropertySpec>(),
                                CreateIfMissing = false
                            },
                            new ElementRequirement
                            {
                                ElementType = StoryItemType.Character,
                                ElementLabel = "Antagonist",
                                ReferencedElementLabel = "Scene.Antagonist",
                                RequiredProperties = new List<PropertySpec>(),
                                CreateIfMissing = false
                            }
                        },
                        Outputs = new List<ElementOutput>
                        {
                            new ElementOutput
                            {
                                ElementType = StoryItemType.Scene,
                                ElementLabel = "Scene",
                                PropertiesToUpdate = new List<PropertySpec>
                                {
                                    new PropertySpec("ProtagGoal"),
                                    new PropertySpec("Opposition"),
                                    new PropertySpec("Outcome")
                                }
                            }
                        }
                    }) { PrimaryElementType = StoryItemType.Scene },
                new Workflow(
                    "Sequel", "Sequel (Reaction)",
                    "Develop the character's emotional reaction, reflection, and decision-making after a " +
                    "scene's conflict.",
                    StoryItemType.Scene,
                    explanation: "After conflict comes reaction. The sequel (or 'reaction beat') shows the protagonist's " +
                                "emotional response to what just happened, their dilemma about what to do next, and " +
                                "their decision that leads to the next scene. This pacing between action and reaction " +
                                "creates story rhythm.",
                    outputProperties: new List<PropertySpec>
                    {
                        new PropertySpec("Emotion"),
                        new PropertySpec("Review"),
                        new PropertySpec("NewGoal")
                    }),
                // SceneCreateImage removed; preserved on branch issue-76-image-workflows (issue #76).
            };

            // Issue #106: declared collection inputs (not inferred from WriteVia).
            var characterChoices = new CollectionInput
            {
                RequestName = "CharacterChoices",
                ElementType = StoryItemType.Character,
                Projection = ElementProjection.IdAndName
            };
            GetFrom(list, "CastSceneRoles")!.GetIO().CollectionInputs.Add(characterChoices);
            // Relationship CollectionInputs are declared on its WorkflowIO above.

            return list;
        }

        private static Workflow? GetFrom(List<Workflow> list, string label) =>
            list.FirstOrDefault(w => w.Label == label);
    }
}
