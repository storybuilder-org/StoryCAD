using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services.API;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels;

#nullable disable

namespace StoryCADTests.Services.API;

/// <summary>
/// TDD test for issue #1395: AssignElementToBeat must return OperationResult.Failure
/// instead of letting OutlineService.AssignElementToBeat throw an unhandled exception
/// when the elementGuid equals the problemGuid (self-assignment).
/// </summary>
[TestClass]
public class StoryCADApiBeatExceptionHandlingTests
{
    private readonly StoryCADApi _api = new(
        Ioc.Default.GetRequiredService<OutlineService>(),
        Ioc.Default.GetRequiredService<ListData>(),
        Ioc.Default.GetRequiredService<ControlData>(),
        Ioc.Default.GetRequiredService<ToolsData>());

    /// <summary>
    /// Seeds an outline, adds a Problem element under the StoryOverview, and
    /// adds one beat to that Problem. Returns the Problem's GUID.
    /// </summary>
    private async Task<Guid> CreateProblemWithOneBeat()
    {
        var createResult = await _api.CreateEmptyOutline("Beat Test Outline", "Test Author", "0");
        Assert.IsTrue(createResult.IsSuccess, $"Outline creation must succeed: {createResult.ErrorMessage}");

        var overview = _api.CurrentModel.StoryElements
            .First(e => e.ElementType == StoryItemType.StoryOverview);

        var addResult = _api.AddElement(StoryItemType.Problem, overview.Uuid.ToString(), "Test Problem");
        Assert.IsTrue(addResult.IsSuccess, $"Problem add must succeed: {addResult.ErrorMessage}");
        var problemGuid = addResult.Payload;

        // CreateBeat is safe (no OutlineService call that throws for this input).
        var beatResult = _api.CreateBeat(problemGuid, "Act 1", "Opening beat");
        Assert.IsTrue(beatResult.IsSuccess, $"CreateBeat must succeed: {beatResult.ErrorMessage}");

        return problemGuid;
    }

    /// <summary>
    /// AssignElementToBeat: when the elementGuid equals the problemGuid, OutlineService
    /// throws InvalidOperationException("Cannot assign a Problem as a beat on itself.").
    /// The API does not guard against self-assignment, so the exception currently escapes.
    /// After the green phase wraps the call in try/catch, this must return Failure.
    /// </summary>
    [TestMethod]
    public async Task AssignElementToBeat_WhenProblemAssignedToItself_ReturnsFailureWithoutThrowing()
    {
        // Arrange
        var problemGuid = await CreateProblemWithOneBeat();

        // Act: assign the Problem to beat index 0 of itself -- self-assignment.
        // OutlineService.AssignElementToBeat throws InvalidOperationException for this.
        var result = _api.AssignElementToBeat(problemGuid, 0, problemGuid);

        // Assert: must be a clean Failure, not an unhandled exception.
        Assert.IsFalse(result.IsSuccess,
            "AssignElementToBeat with self-assignment must return IsSuccess=false.");
        Assert.IsNotNull(result.ErrorMessage,
            "AssignElementToBeat with self-assignment must provide an ErrorMessage.");
    }

}
