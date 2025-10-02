using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.ViewModels;
using StoryCAD.Models;
using StoryCAD.DAL;
using StoryCAD.Services.Messages;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using StoryCAD.Services.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace StoryCADTests.ViewModels
{
    [TestClass]
    public class SceneViewModelTests
    {
        private FakeLogService _fakeLogger;
        private FakeStoryIO _fakeStoryIO;
        private SceneViewModel _sceneViewModel;
        private StoryModel _storyModel;
        private OverviewModel _overviewModel;

        [TestInitialize]
        public void Setup()
        {
            // Initialize fake services
            _fakeLogger = new FakeLogService();
            _fakeStoryIO = new FakeStoryIO();

            // Setup the IoC container with fake services
            Ioc.Default.ConfigureServices(
                new ServiceCollection()
                .AddSingleton<ILogService>(_fakeLogger)
                .AddSingleton<StoryIO>(_fakeStoryIO)
                .BuildServiceProvider()
            );

            // Initialize StoryModel with necessary data
            _storyModel = new StoryModel
            {
                ProjectFilename = "TestProject.stbx",
                ProjectPath = Path.Combine(Environment.CurrentDirectory, "TestProjects")
            };

            _overviewModel = new OverviewModel("TestProject", _storyModel)
            {
                Viewpoint = "First Person",
                ViewpointCharacter = Guid.NewGuid(),
                DateCreated = DateTime.Today.ToString("yyyy-MM-dd"),
                Author = "Test Author"
            };

            var overviewNode = new StoryNodeItem(_overviewModel, null, StoryItemType.StoryOverview)
            {
                IsRoot = true
            };

            _storyModel.ExplorerView.Add(overviewNode);

            // Initialize SceneModel and assign to SceneViewModel
            var sceneModel = new SceneModel(_storyModel)
            {
                ViewpointCharacter = _overviewModel.ViewpointCharacter
            };

            _sceneViewModel = new SceneViewModel
            {
                Model = sceneModel
            };

            // Register StoryModel in IoC if ShellViewModel.GetModel() retrieves from IoC
            // Assumption: ShellViewModel.GetModel() retrieves StoryModel via Ioc.GetService<StoryModel>()
            Ioc.Default.ConfigureServices(
                new ServiceCollection()
                .AddSingleton(_storyModel)
                .AddSingleton<ILogService>(_fakeLogger)
                .AddSingleton<StoryIO>(_fakeStoryIO)
                .BuildServiceProvider()
            );

            // Note: If ShellViewModel.GetModel() is static and does not utilize IoC,
            // additional refactoring is required for better testability.
        }

        [TestMethod]
        public void UpdateViewpointCharacterTip_WithValidViewpointAndCharacter_ShouldUpdateTipAndNotOpenTeachingTip()
        {
            // Arrange
            // Ensure Viewpoint and ViewpointCharacter are set
            _overviewModel.Viewpoint = "First Person";
            _overviewModel.ViewpointCharacter = Guid.NewGuid(); // Valid UUID
            _sceneViewModel.Model.ViewpointCharacter = _overviewModel.ViewpointCharacter;

            // Act
            _sceneViewModel.UpdateViewpointCharacterTip();

            // Assert
            string expectedTip = $"{Environment.NewLine}Story viewpoint = First Person\nNo story viewpoint character selected";
            Assert.AreEqual(expectedTip, _sceneViewModel.VpCharTip, "VpCharTip content mismatch.");
            Assert.IsFalse(_sceneViewModel.VpCharTipIsOpen, "VpCharTip should not be open when ViewpointCharacter is set.");

            // Verify that the logger was not called since the TeachingTip should not open
            Assert.IsFalse(_fakeLogger.HasLogged(LogLevel.Warn, "ViewpointCharacterTip displayed"), "Logger should not have logged a warning when TeachingTip is not opened.");
        }

        [TestMethod]
        public void UpdateViewpointCharacterTip_WithEmptyViewpointCharacter_ShouldUpdateTipAndOpenTeachingTip()
        {
            // Arrange
            // Set ViewpointCharacter to Guid.Empty to simulate no character selected
            _overviewModel.ViewpointCharacter = Guid.Empty;
            _sceneViewModel.Model.ViewpointCharacter = _overviewModel.ViewpointCharacter;

            // Act
            _sceneViewModel.UpdateViewpointCharacterTip();

            // Assert
            string expectedTip = $"{Environment.NewLine}Story viewpoint = First Person\nNo story viewpoint character selected";
            Assert.AreEqual(expectedTip, _sceneViewModel.VpCharTip, "VpCharTip content mismatch when ViewpointCharacter is empty.");
            Assert.IsTrue(_sceneViewModel.VpCharTipIsOpen, "VpCharTip should be open when ViewpointCharacter is empty.");

            // Verify that the logger was called since the TeachingTip should open
            Assert.IsTrue(_fakeLogger.HasLogged(LogLevel.Warn, "ViewpointCharacterTip displayed"), "Logger should have logged a warning when TeachingTip is opened.");
        }

        [TestMethod]
        public void UpdateViewpointCharacterTip_WithEmptyViewpoint_ShouldSetDefaultTipAndOpenTeachingTip()
        {
            // Arrange
            _overviewModel.Viewpoint = string.Empty;
            _overviewModel.ViewpointCharacter = Guid.Empty; // Ensure TeachingTip is to be opened
            _sceneViewModel.Model.ViewpointCharacter = _overviewModel.ViewpointCharacter;

            // Act
            _sceneViewModel.UpdateViewpointCharacterTip();

            // Assert
            string expectedTip = $"{Environment.NewLine}No story viewpoint selected\nNo story viewpoint character selected";
            Assert.AreEqual(expectedTip, _sceneViewModel.VpCharTip, "VpCharTip should display default message when viewpoint is empty.");
            Assert.IsTrue(_sceneViewModel.VpCharTipIsOpen, "VpCharTip should be open when ViewpointCharacter is empty.");

            // Verify that the logger was called
            Assert.IsTrue(_fakeLogger.HasLogged(LogLevel.Warn, "ViewpointCharacterTip displayed"), "Logger should have logged a warning when TeachingTip is opened.");
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Clean up any resources or reset services if necessary
            _sceneViewModel = null;
            _storyModel = null;
            _overviewModel = null;
            Ioc.Default.Reset();
        }

        /// <summary>
        /// A fake implementation of ILogService for testing purposes.
        /// It records log entries for verification in tests.
        /// </summary>
        private class FakeLogService : ILogService
        {
            private readonly List<LogEntry> _logEntries = new List<LogEntry>();

            public IReadOnlyList<LogEntry> LogEntries => _logEntries.AsReadOnly();

            public void Log(LogLevel level, string message)
            {
                _logEntries.Add(new LogEntry { Level = level, Message = message });
            }

            public void LogException(LogLevel level, Exception exception, string message)
            {
                _logEntries.Add(new LogEntry { Level = level, Message = $"{message}. Exception: {exception.Message}" });
            }

            /// <summary>
            /// Helper method to verify if a specific log entry exists.
            /// </summary>
            public bool HasLogged(LogLevel level, string message)
            {
                foreach (var entry in _logEntries)
                {
                    if (entry.Level == level && entry.Message == message)
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Represents a single log entry.
        /// </summary>
        private class LogEntry
        {
            public LogLevel Level { get; set; }
            public string Message { get; set; }
        }

        /// <summary>
        /// A fake implementation of StoryIO for testing purposes.
        /// If methods are needed, implement them accordingly.
        /// </summary>
        private class FakeStoryIO : StoryIO
        {
            public override Task<StoryModel> ReadStory(StorageFile storyFile)
            {
                // Return a default or test-specific StoryModel if needed
                return Task.FromResult(new StoryModel());
            }

            public override Task WriteStory(StorageFile output, StoryModel model)
            {
                // Simulate writing by doing nothing or recording that the method was called
                return Task.CompletedTask;
            }

            protected override Task<StoryModel> MigrateModel(StorageFile file)
            {
                // Implement migration if needed, otherwise return default
                return Task.FromResult(new StoryModel());
            }
        }
    }
}

