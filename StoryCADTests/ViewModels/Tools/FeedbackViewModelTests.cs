using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using StoryCADLib.ViewModels.Tools;

#nullable disable

namespace StoryCADTests.ViewModels.Tools;

[TestClass]
public class FeedbackViewModelTests
{
    private FeedbackViewModel GetFreshVm()
    {
        var vm = Ioc.Default.GetRequiredService<FeedbackViewModel>();
        vm.Title = string.Empty;
        vm.Body = string.Empty;
        return vm;
    }

    [TestMethod]
    public void IsValid_WhenTitleAndBodyEmpty_ReturnsFalse()
    {
        // Arrange
        var vm = GetFreshVm();

        // Act / Assert
        Assert.IsFalse(vm.IsValid);
    }

    [TestMethod]
    public void IsValid_WhenTitleBelowMinimumAndBodyMeetsMinimum_ReturnsFalse()
    {
        // Arrange
        var vm = GetFreshVm();

        // Act
        vm.Title = "123456789"; // 9 chars
        vm.Body = "12345678901234567890"; // 20 chars

        // Assert
        Assert.IsFalse(vm.IsValid);
    }

    [TestMethod]
    public void IsValid_WhenTitleMeetsMinimumAndBodyBelowMinimum_ReturnsFalse()
    {
        // Arrange
        var vm = GetFreshVm();

        // Act
        vm.Title = "1234567890"; // 10 chars
        vm.Body = "1234567890123456789"; // 19 chars

        // Assert
        Assert.IsFalse(vm.IsValid);
    }

    [TestMethod]
    public void IsValid_WhenTitleAndBodyAtExactMinimums_ReturnsTrue()
    {
        // Arrange
        var vm = GetFreshVm();

        // Act
        vm.Title = "1234567890"; // 10 chars
        vm.Body = "12345678901234567890"; // 20 chars

        // Assert
        Assert.IsTrue(vm.IsValid);
    }

    [TestMethod]
    public void IsValid_WhenTitleAndBodyExceedMinimums_ReturnsTrue()
    {
        // Arrange
        var vm = GetFreshVm();

        // Act
        vm.Title = "This is a bug report title";
        vm.Body = "This is a detailed description of the bug that occurred in the application.";

        // Assert
        Assert.IsTrue(vm.IsValid);
    }

    [TestMethod]
    public void TitleError_WhenTitleBelowMinimum_IsNonEmpty()
    {
        // Arrange
        var vm = GetFreshVm();

        // Act
        vm.Title = "short";

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(vm.TitleError));
    }

    [TestMethod]
    public void TitleError_WhenTitleMeetsMinimum_IsEmpty()
    {
        // Arrange
        var vm = GetFreshVm();

        // Act
        vm.Title = "1234567890"; // 10 chars

        // Assert
        Assert.AreEqual(string.Empty, vm.TitleError);
    }

    [TestMethod]
    public void BodyError_WhenBodyBelowMinimum_IsNonEmpty()
    {
        // Arrange
        var vm = GetFreshVm();

        // Act
        vm.Body = "too short";

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(vm.BodyError));
    }

    [TestMethod]
    public void BodyError_WhenBodyMeetsMinimum_IsEmpty()
    {
        // Arrange
        var vm = GetFreshVm();

        // Act
        vm.Body = "12345678901234567890"; // 20 chars

        // Assert
        Assert.AreEqual(string.Empty, vm.BodyError);
    }

    [TestMethod]
    public void TitleErrorVisibility_WhenTitleBelowMinimum_IsVisible()
    {
        // Arrange
        var vm = GetFreshVm();

        // Act
        vm.Title = "short";

        // Assert
        Assert.AreEqual(Visibility.Visible, vm.TitleErrorVisibility);
    }

    [TestMethod]
    public void TitleErrorVisibility_WhenTitleMeetsMinimum_IsCollapsed()
    {
        // Arrange
        var vm = GetFreshVm();

        // Act
        vm.Title = "1234567890"; // 10 chars

        // Assert
        Assert.AreEqual(Visibility.Collapsed, vm.TitleErrorVisibility);
    }

    [TestMethod]
    public void BodyErrorVisibility_WhenBodyBelowMinimum_IsVisible()
    {
        // Arrange
        var vm = GetFreshVm();

        // Act
        vm.Body = "too short";

        // Assert
        Assert.AreEqual(Visibility.Visible, vm.BodyErrorVisibility);
    }

    [TestMethod]
    public void BodyErrorVisibility_WhenBodyMeetsMinimum_IsCollapsed()
    {
        // Arrange
        var vm = GetFreshVm();

        // Act
        vm.Body = "12345678901234567890"; // 20 chars

        // Assert
        Assert.AreEqual(Visibility.Collapsed, vm.BodyErrorVisibility);
    }

    [TestMethod]
    public void TitleTitle_ByDefault_IsIssueTitle()
    {
        // Arrange
        var vm = GetFreshVm();

        // Act / Assert
        Assert.AreEqual("Issue Title", vm.TitleTitle);
    }
}
