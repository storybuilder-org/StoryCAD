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
            return new List<Workflow>
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
                    }),

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
                        OptionalInputs = new List<ElementRequirement>
                        {
                            new ElementRequirement
                            {
                                ElementType = StoryItemType.Problem,
                                ElementLabel = "Problem",
                                RequiredProperties = new List<PropertySpec>(),
                                CreateIfMissing = true
                            },
                            new ElementRequirement
                            {
                                ElementType = StoryItemType.Character,
                                ElementLabel = "Protagonist",
                                RequiredProperties = new List<PropertySpec>(),
                                CreateIfMissing = true
                            },
                            new ElementRequirement
                            {
                                ElementType = StoryItemType.Character,
                                ElementLabel = "Antagonist",
                                RequiredProperties = new List<PropertySpec>(),
                                CreateIfMissing = true
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
                                    new PropertySpec("Premise")
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
                    }),

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
                                ElementType = StoryItemType.Problem,
                                ElementLabel = "Problem",
                                RequiredProperties = new List<PropertySpec> { new PropertySpec("Protagonist"), new PropertySpec("Antagonist") },
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
                    }),

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
                            }
                        },
                        OptionalInputs = new List<ElementRequirement>
                        {
                            new ElementRequirement
                            {
                                ElementType = StoryItemType.Character,
                                ElementLabel = "Protagonist",
                                RequiredProperties = new List<PropertySpec>(),
                                CreateIfMissing = false
                            }
                        },
                        Outputs = new List<ElementOutput>
                        {
                            new ElementOutput
                            {
                                ElementType = StoryItemType.Problem,
                                ElementLabel = "InnerProblem",
                                PropertiesToUpdate = new List<PropertySpec>
                                {
                                    new PropertySpec("Description", JsonKey: "InnerProblemDescription"),
                                    new PropertySpec("Theme", JsonKey: "theme_connection"),
                                    new PropertySpec("Method", JsonKey: "resolution_path"),
                                    new PropertySpec("Notes", JsonKey: "explanation")
                                }
                            }
                        }
                    }),
                new Workflow(
                    "Structure", "Problem Structure",
                    "Define the structural beats and turning points of a story problem by selecting and applying " +
                    "a beat sheet template.",
                    StoryItemType.Problem,
                    explanation: "Structure gives your problem shape—a beginning that hooks, a middle that complicates, " +
                                "and an ending that resolves. This workflow helps you choose a beat sheet (Three Act, " +
                                "Hero's Journey, Save the Cat, etc.) and maps your problem's key moments to its beats.",
                    outputProperties: new List<PropertySpec>
                    {
                        new PropertySpec("StructureTitle"),
                        new PropertySpec("StructureDescription"),
                        // Beat array: model emits "beats" array; runner clears and rebuilds via the beat API.
                        new PropertySpec("StructureBeats", WriteVia.BeatSheet, JsonKey: "beats")
                    }),
                // === Character Workflows ===
                new Workflow(
                    "RoleAndStoryRole", "Role and Story Role",
                    "Define a character's archetypal role and their function in the story.",
                    StoryItemType.Character,
                    explanation: "Every character serves a purpose. Role defines their relationship to the protagonist " +
                                "(ally, mentor, love interest, antagonist). Story Role captures their narrative function " +
                                "(provides comic relief, delivers exposition, represents theme). Archetype connects them " +
                                "to universal patterns readers instinctively recognize—the Hero, Trickster, Shadow, or " +
                                "Shapeshifter. This workflow helps you cast your character deliberately.",
                    outputProperties: new List<PropertySpec> { new PropertySpec("Role"), new PropertySpec("StoryRole"), new PropertySpec("Archetype") },
                    exampleLists: new List<string> { "Role", "StoryRole", "Archetype" }),
                new Workflow(
                    "PhysicalAppearance", "Physical and Appearance",
                    "Develop a character's physical description, distinctive features, and overall appearance " +
                    "that readers will visualize.",
                    StoryItemType.Character,
                    explanation: "Physical details ground your character in reality and can reveal personality, " +
                                "history, and social position. This workflow helps you define age, build, coloring, " +
                                "and distinctive features—focusing on details that matter to your story rather than " +
                                "exhaustive description.",
                    outputProperties: new List<PropertySpec>
                    {
                        new PropertySpec("Age"),
                        new PropertySpec("Sex"),
                        new PropertySpec("Eyes"),
                        new PropertySpec("Hair"),
                        new PropertySpec("Build"),
                        new PropertySpec("Complexion"),
                        new PropertySpec("Appearance")
                    }),
                new Workflow(
                    "SocialFactors", "Social Background",
                    "Explore a character's social background, economic status, education, ethnicity, and " +
                    "relationship to society.",
                    StoryItemType.Character,
                    explanation: "Characters exist within social contexts that shape their worldview, opportunities, " +
                                "and conflicts. This workflow helps you define the social forces that formed your " +
                                "character—class, education, cultural background, and religion—and how these create " +
                                "story possibilities.",
                    outputProperties: new List<PropertySpec>
                    {
                        new PropertySpec("Economic"),
                        new PropertySpec("Education"),
                        new PropertySpec("Ethnic"),
                        new PropertySpec("Religion")
                    }),
                new Workflow(
                    "PsychologicalMakeup", "Psychological Profile",
                    "Develop a character's personality, psychology, values, and mental characteristics using " +
                    "the Enneagram and other frameworks.",
                    StoryItemType.Character,
                    explanation: "Understanding your character's psychology helps you write consistent, believable " +
                                "behavior. This workflow explores personality type (Enneagram), intelligence, core " +
                                "values, psychological focus, and any abnormalities—the internal landscape that " +
                                "drives external action.",
                    outputProperties: new List<PropertySpec>
                    {
                        new PropertySpec("Enneagram"),
                        new PropertySpec("Intelligence"),
                        new PropertySpec("Values"),
                        new PropertySpec("Abnormality"),
                        new PropertySpec("Focus")
                    }),
                new Workflow(
                    "InnerOuterTraits", "Inner and Outer Traits",
                    "Define the visible behaviors others see (outer traits) and the hidden inner qualities " +
                    "that drive them (inner traits).",
                    StoryItemType.Character,
                    explanation: "Outer traits are what characters display to the world—habits, mannerisms, social " +
                                "behaviors. Inner traits are the deeper qualities—courage, insecurity, compassion—that " +
                                "explain why they act as they do. The gap between outer and inner creates character " +
                                "depth and story potential.",
                    // Bug 2: TraitList is List<string>; must use SimpleList, not Scalar.
                    outputProperties: new List<PropertySpec>
                    {
                        new PropertySpec("TraitList", WriteVia.SimpleList, ListEntryType: typeof(string))
                    }),
                new Workflow(
                    "Flaw", "Character Flaw",
                    "Identify and develop a character's central flaw—the weakness or blind spot tied to their " +
                    "inner problem and character arc.",
                    StoryItemType.Character,
                    explanation: "A flaw is often the source of inner conflict and the key to character transformation. " +
                                "This workflow helps you identify a flaw that matters to your story—one that creates " +
                                "problems, blocks goals, and must be overcome (or not) for the character's arc to complete.",
                    outputProperties: new List<PropertySpec> { new PropertySpec("Flaw") }),
                new Workflow(
                    "Backstory", "Backstory",
                    "Develop the character's formative history—the wound, ghost, or defining events that explain " +
                    "who they are now.",
                    StoryItemType.Character,
                    explanation: "Backstory is cause and explanation. Something happened that created your character's " +
                                "flaw, shaped their worldview, and drives their current behavior. This workflow helps " +
                                "you identify that wound or defining moment—what the character may not even consciously " +
                                "remember but which controls them still.",
                    outputProperties: new List<PropertySpec> { new PropertySpec("BackStory") }),
                new Workflow(
                    "Relationship", "Character Relationship",
                    "Develop the dynamics, history, and tension between two characters.",
                    StoryItemType.Character,
                    explanation: "Relationships create conflict, reveal character, and drive plot. This workflow " +
                                "explores how two characters relate—their shared history, what they want from each " +
                                "other, sources of tension, and how the relationship might change through the story.",
                    // RelationshipList is List<RelationshipModel>; recipient GUID injected via CharacterChoices.
                    outputProperties: new List<PropertySpec>
                    {
                        new PropertySpec("RelationshipList", WriteVia.Relationships, JsonKey: "relationship")
                    }),

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
                new Workflow(
                    "SceneSummary", "Scene Summary",
                    "Create a concise summary of a scene's purpose, content, and role in the larger story.",
                    StoryItemType.Scene,
                    explanation: "Every scene should earn its place. This workflow helps you articulate what happens " +
                                "in the scene, why it matters, and what would be lost without it—ensuring each scene " +
                                "advances plot, reveals character, or both.",
                    outputProperties: new List<PropertySpec> { new PropertySpec("Description") }),
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
                    "SceneConflict", "Scene Conflict",
                    "Structure the conflict within a scene—the protagonist's goal, the opposition they face, " +
                    "and the outcome.",
                    StoryItemType.Scene,
                    explanation: "A scene is a small story with goal, conflict, and outcome. This workflow uses the " +
                                "Actor's Studio method to define what the scene protagonist wants, what opposes them, " +
                                "and how the scene ends—usually in a way that makes things worse.",
                    outputProperties: new List<PropertySpec>
                    {
                        new PropertySpec("ProtagGoal"),
                        new PropertySpec("Opposition"),
                        new PropertySpec("Outcome")
                    }),
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
        }
    }
}
