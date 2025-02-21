using Windows.Storage;
using StoryCAD.DAL;

namespace StoryCAD.Services.Outline 
{
    /// <summary>
    /// OutlineService contains the raw methods which 
    /// </summary>
    public class OutlineService
    {
        #region Public Methods

        /// <summary>
        /// This creates a new StoryModel based on a template
        /// </summary>
        /// <param name="file">The file the outline will be written to</param>
        /// <param name="name">The outline's overview story element name</param>
        /// <param name="author">The outline's overview story element author</param>
        /// <param name="selectedTemplateIndex">The template to use (see NewProject.xaml)</param>
        public async Task<StoryModel> CreateModel(StorageFile file, string name, string author, int selectedTemplateIndex)
        {   
            name = Path.GetFileNameWithoutExtension(file.Path);
            StoryModel model = new()
            {
                ProjectFilename = Path.GetFileName(file.Path),
                ProjectFolder = await StorageFolder.GetFolderFromPathAsync(file.Path),
                ProjectFile = file,
                ProjectPath = Path.GetDirectoryName(file.Path)
            };

            OverviewModel overview = new(name, model)
            {
            DateCreated = DateTime.Today.ToString("yyyy-MM-dd"),
            Author = author
            };
            
        StoryNodeItem overviewNode = new(overview, null) { IsExpanded = true, IsRoot = true };
        model.ExplorerView.Add(overviewNode);
        TrashCanModel trash = new(model);
        StoryNodeItem trashNode = new(trash, null);
        model.ExplorerView.Add(trashNode);     // The trashcan is the second root
        FolderModel narrative = new("Narrative View", model, StoryItemType.Folder);
        StoryNodeItem narrativeNode = new(narrative, null) { IsRoot = true };
        model.NarratorView.Add(narrativeNode);

        // Every new story gets a StoryProblem with a Protagonist and Antagonist
        // Except for Blank Project
        if (selectedTemplateIndex != 0)
        {
            StoryElement storyProblem = new ProblemModel("Story Problem", model);
            StoryNodeItem storyProblemNode = new(storyProblem, null);
            StoryElement storyProtag = new CharacterModel("Protagonist", model);
            StoryNodeItem storyProtagNode = new StoryNodeItem(storyProtag, null);
            StoryElement storyAntag = new CharacterModel("Antagonist", model);
            StoryNodeItem storyAntagNode = new StoryNodeItem(storyAntag, null);
            overview.StoryProblem = storyProblem.Uuid;
            var problem = storyProblem as ProblemModel;
            problem.Protagonist = storyProtag.Uuid;
            problem.Antagonist = storyAntag.Uuid;
            problem.Premise =
                @"Your[protagonist] in a situation[genre, setting] wants something[goal], which brings him" +
                @"into [conflict] with a second character[antagonist]. After a series of conflicts[additional " +
                @"problems], the final battle[climax scene] erupts, and the[protagonist] finally resolves the " +
                @"conflict[outcome].";

            // Use the NewProjectDialog template to complete the model
            switch (selectedTemplateIndex)
            {
                case 1:  // Story problem and characters
                    overviewNode.Children.Add(storyProblemNode);
                    storyProblemNode.Children.Add(storyProtagNode);
                    storyProblemNode.Children.Add(storyAntagNode);

                    //Correctly set parents
                    storyProblemNode.Parent = overviewNode;
                    storyProtagNode.Parent = storyProblemNode;
                    storyAntagNode.Parent = storyProblemNode;
                    storyProblemNode.IsExpanded = true;
                    break;
                case 2:  // Folders for each type- story problem and characters belong in the corresponding folders
                    StoryElement problems = new FolderModel("Problems", model, StoryItemType.Folder);
                    StoryNodeItem problemsNode = new(problems, overviewNode);
                    storyProblemNode.Parent = problemsNode;
                    StoryElement characters = new FolderModel("Characters", model, StoryItemType.Folder);
                    StoryNodeItem charactersNode = new(characters, overviewNode);
                    storyProtagNode.Parent = charactersNode;
                    storyAntagNode.Parent = charactersNode;
                    StoryElement settings = new FolderModel("Settings", model, StoryItemType.Folder);
                    StoryNodeItem settingsNode = new(settings, overviewNode);
                    StoryElement scenes = new FolderModel("Scenes", model, StoryItemType.Folder);
                    StoryNodeItem scenesNode = new(scenes, overviewNode);
                    overview.StoryProblem = storyProblem.Uuid;
                    problemsNode.Children.Add(storyProblemNode);
                    charactersNode.Children.Add(storyProtagNode);
                    charactersNode.Children.Add(storyAntagNode);
                    problemsNode.IsExpanded = true;
                    charactersNode.IsExpanded = true;
                    break;
                case 3:
                    storyProblemNode.Name = "External Problem";
                    storyProblemNode.IsExpanded = true;
                    storyProblemNode.Parent = overviewNode;
                    overviewNode.Children.Add(storyProblemNode);
                    storyProblemNode.Children.Add(storyProtagNode);
                    storyProblemNode.Children.Add(storyAntagNode);
                    problem = storyProblem as ProblemModel;
                    problem.Name = "External Problem";
                    overview.StoryProblem = problem.Uuid;
                    problem.Protagonist = storyProtag.Uuid;
                    problem.Antagonist = storyAntagNode.Uuid;
                    storyProtagNode.Parent = storyProblemNode;
                    storyAntagNode.Parent = storyProblemNode;
                    StoryElement internalProblem = new ProblemModel("Internal Problem", model);
                    StoryNodeItem internalProblemNode = new(internalProblem, overviewNode);
                    problem = internalProblem as ProblemModel;
                    problem.Protagonist = storyProtag.Uuid;
                    problem.Antagonist = storyProtag.Uuid;
                    problem.ConflictType = "Person vs. Self";
                    problem.Premise =
                        @"Your [protagonist] grapples with an [internal conflict] and is their own antagonist, marred by self-doubt and fears " +
                        @"or having a [goal] that masks this conflict rather than a real need. The [climax scene] is often a moment of introspection in which " +
                        @"he or she makes a decision or discovery that resolves the internal conflict [outcome]. Resolving this problem may enable your " +
                        @"[protagonist] to resolve another (external) problem.";
                    break;
                case 4:
                    overviewNode.Children.Add(storyProtagNode);
                    overviewNode.Children.Add(storyAntagNode);
                    storyProtagNode.Children.Add(storyProblemNode);
                    storyProtagNode.IsExpanded = true;
                    storyProblemNode.Parent = storyProtagNode;
                    storyProtagNode.Parent = overviewNode;
                    storyAntagNode.Parent = overviewNode;
                    break;
                case 5:
                    StoryElement problemsFolder = new FolderModel("Problems", model, StoryItemType.Folder);
                    StoryNodeItem problemsFolderNode = new(problemsFolder, overviewNode) { IsExpanded = true };
                    StoryElement charactersFolder = new FolderModel("Characters", model, StoryItemType.Folder);
                    StoryNodeItem charactersFolderNode = new(charactersFolder, overviewNode) { IsExpanded = true };
                    StoryElement settingsFolder = new FolderModel("Settings", model, StoryItemType.Folder);
                    StoryNodeItem settingsFolderNode = new(settingsFolder, overviewNode);
                    StoryElement scenesFolder = new FolderModel("Scenes", model, StoryItemType.Folder);
                    StoryNodeItem scenesFolderNode = new(scenesFolder, overviewNode);
                    storyProblemNode.Name = "External Problem";
                    storyProblemNode.IsExpanded = true;
                    problemsFolderNode.Children.Add(storyProblemNode);
                    storyProblemNode.Parent = problemsFolderNode;
                    problem = storyProblem as ProblemModel;
                    problem.Name = "External Problem";
                    problem.Protagonist = storyProtag.Uuid;
                    problem.Antagonist = storyAntagNode.Uuid;
                    overview.StoryProblem = problem.Uuid;
                    internalProblem = new ProblemModel("Internal Problem", model);
                    internalProblemNode = new(internalProblem, problemsFolderNode);
                    problem = internalProblem as ProblemModel;
                    problem.Protagonist = storyProtag.Uuid;
                    problem.Antagonist = storyProtag.Uuid;
                    problem.ConflictType = "Person vs. Self";
                    problem.Premise =
                        @"Your [protagonist] grapples with an [internal conflict] and is their own antagonist, marred by self-doubt and fears " +
                        @"or having a [goal] that masks this conflict rather than a real need. The [climax scene] is often a moment of introspection in which " +
                        @"he or she makes a decision or discovery that resolves the internal conflict [outcome]. Resolving this problem may enable your " +
                        @"[protagonist] to resolve another (external) problem.";
                    charactersFolderNode.Children.Add(storyProtagNode);
                    charactersFolderNode.Children.Add(storyAntagNode);
                    storyProtagNode.Parent = charactersFolderNode;
                    storyAntagNode.Parent = charactersFolderNode;
                    break;
            }
        }
        return model;
    }

        /// <summary>
        /// This writes the StoryModel JSON file to the backing store (disk)
        /// </summary>
        /// <param name="model">the StoryModel to write</param>
        /// <param name="file">The StorageFile to write it to</param>
        /// <returns></returns>
        public async Task WriteModel(StoryModel model, StorageFile file)
        {
            StoryIO wtr = Ioc.Default.GetRequiredService<StoryIO>();
            await wtr.WriteStory(file, model);
        }

        #endregion
    }
}
