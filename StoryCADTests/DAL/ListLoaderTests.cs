using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;

#nullable disable

namespace StoryCADTests.DAL;

[TestClass]
public class ListLoaderTests
{
    private readonly Dictionary<string, ObservableCollection<string>> lists = Ioc.Default.GetService<ListData>()
        .ListControlSource;

    [TestMethod]
    public void TestListLoaderLists()
    {
        Assert.AreEqual(66, lists.Count);
        // OverViewModel lists
        Assert.IsTrue(lists.ContainsKey("StoryType"));
        Assert.IsTrue(lists.ContainsKey("Voice"));
        Assert.IsTrue(lists.ContainsKey("Tense"));
        Assert.IsTrue(lists.ContainsKey("Genre"));
        Assert.IsTrue(lists.ContainsKey("Viewpoint"));
        Assert.IsTrue(lists.ContainsKey("LanguageStyle"));
        Assert.IsTrue(lists.ContainsKey("LiteraryTechnique"));
        Assert.IsTrue(lists.ContainsKey("LiteraryStyle"));
        // ProblemViewModel lists
        Assert.IsTrue(lists.ContainsKey("ProblemType"));
        Assert.IsTrue(lists.ContainsKey("ConflictType"));
        Assert.IsTrue(lists.ContainsKey("ProblemSubject"));
        Assert.IsTrue(lists.ContainsKey("ProblemSource"));
        Assert.IsTrue(lists.ContainsKey("Motive"));
        Assert.IsTrue(lists.ContainsKey("Goal"));
        Assert.IsTrue(lists.ContainsKey("Outcome"));
        Assert.IsTrue(lists.ContainsKey("Method"));
        Assert.IsTrue(lists.ContainsKey("Theme"));
        // CharacterViewModel lists
        Assert.IsTrue(lists.ContainsKey("Role"));
        Assert.IsTrue(lists.ContainsKey("StoryRole"));
        Assert.IsTrue(lists.ContainsKey("Archetype"));
        Assert.IsTrue(lists.ContainsKey("Build"));
        Assert.IsTrue(lists.ContainsKey("Nationality"));
        Assert.IsTrue(lists.ContainsKey("Eyes"));
        Assert.IsTrue(lists.ContainsKey("Hair"));
        Assert.IsTrue(lists.ContainsKey("Complexion"));
        Assert.IsTrue(lists.ContainsKey("Race"));
        Assert.IsTrue(lists.ContainsKey("Enneagram"));
        Assert.IsTrue(lists.ContainsKey("Intelligence"));
        Assert.IsTrue(lists.ContainsKey("Values"));
        Assert.IsTrue(lists.ContainsKey("Abnormality"));
        Assert.IsTrue(lists.ContainsKey("Focus"));
        Assert.IsTrue(lists.ContainsKey("Adventureousness"));
        Assert.IsTrue(lists.ContainsKey("Aggression"));
        Assert.IsTrue(lists.ContainsKey("Confidence"));
        Assert.IsTrue(lists.ContainsKey("Conscientiousness"));
        Assert.IsTrue(lists.ContainsKey("Creativity"));
        Assert.IsTrue(lists.ContainsKey("Dominance"));
        Assert.IsTrue(lists.ContainsKey("Enthusiasm"));
        Assert.IsTrue(lists.ContainsKey("Assurance"));
        Assert.IsTrue(lists.ContainsKey("Sensitivity"));
        Assert.IsTrue(lists.ContainsKey("Shrewdness"));
        Assert.IsTrue(lists.ContainsKey("Sociability"));
        Assert.IsTrue(lists.ContainsKey("Stability"));
        Assert.IsTrue(lists.ContainsKey("WoundCategory"));
        Assert.IsTrue(lists.ContainsKey("Wound"));
        // SettingViewModel lists
        Assert.IsTrue(lists.ContainsKey("Locale"));
        Assert.IsTrue(lists.ContainsKey("Season"));
        // PlotPointViewModel lists
        Assert.IsTrue(lists.ContainsKey("Viewpoint"));
        Assert.IsTrue(lists.ContainsKey("SceneType"));
        Assert.IsTrue(lists.ContainsKey("ScenePurpose"));
        Assert.IsTrue(lists.ContainsKey("StoryRole"));
        Assert.IsTrue(lists.ContainsKey("Emotion"));
        Assert.IsTrue(lists.ContainsKey("Goal"));
        Assert.IsTrue(lists.ContainsKey("Opposition"));
        Assert.IsTrue(lists.ContainsKey("Outcome"));
        Assert.IsTrue(lists.ContainsKey("Viewpoint"));
        Assert.IsTrue(lists.ContainsKey("ValueExchange"));
        Assert.AreEqual(7, lists["Season"].Count);
        Assert.AreEqual(5, lists["Viewpoint"].Count);
        Assert.AreEqual(5, lists["Tense"].Count);
    }

    [TestMethod]
    public void CheckForDuplicates()
    {
        var FailedLists = "";
        foreach (var list in lists.Values)
        {
            if (list.Distinct().Count() != list.Count)
            {
                //names list error and thows exception to mark test as failed.
                FailedLists +=
                    "\n -" + lists.Keys.ToList()[
                        lists.Values.ToList().IndexOf(list)]; //gets key (name) of invalid list.
            }
        }

        if (FailedLists != "")
        {
            throw new AssertFailedException("Lists.ini contains duplicate values in the following lists" + FailedLists);
        }
    }
}
