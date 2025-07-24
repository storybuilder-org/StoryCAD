using System;
using System.IO;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.Services;

namespace StoryCADTests;

[TestClass]
public class FileOpenMenuTests
{
    [TestMethod]
    public void FileOpenMenu_CreatesBackupDirectory()
    {
        var prefs = Ioc.Default.GetRequiredService<PreferenceService>();
        string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
        prefs.Model.BackupDirectory = dir;
        new StoryCAD.Services.Dialogs.FileOpenMenuPage();
        Assert.IsTrue(Directory.Exists(dir));
    }
}
