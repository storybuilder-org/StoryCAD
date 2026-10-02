using StoryCADLib.Collaborator.Models;
using StoryCADLib.Collaborator.ViewModels;

namespace StoryCADTests.Collaborator;

/// <summary>
/// Collaborator #107: Outline gaps page copies the Guess sentence.
/// </summary>
[TestClass]
public class GapWorkflowViewModelTests
{
    [TestMethod]
    public void Load_CopiesGuessSentence_AndRaisesPropertyChanged()
    {
        var vm = new GapWorkflowViewModel();
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != null)
                changed.Add(e.PropertyName);
        };

        vm.Load(new GapWorkflowPayload
        {
            GuessSentence = "Guess: the outline is in Ideation."
        });

        Assert.AreEqual("Guess: the outline is in Ideation.", vm.GuessSentence);
        CollectionAssert.Contains(changed, nameof(GapWorkflowViewModel.GuessSentence));
    }
}
