using StoryCADLib.ViewModels;

namespace StoryCADTests.ViewModels;

/// <summary>
///     Backup-name parsing the File Open menu uses to sort and label the Backups tab
///     without a stat per file. BackupService writes "{Outline} as of yyyyMMdd_HHmm.zip".
/// </summary>
public partial class FileOpenVMTests
{
    [TestMethod]
    [TestCategory("CrossPlatform")]
    public void ParseBackupTimestamp_WellFormedName_ReturnsStamp()
    {
        var when = FileOpenVM.ParseBackupTimestamp(@"G:\Backups\Danger Calls as of 20260904_0657.zip");

        Assert.AreEqual(new DateTime(2026, 9, 4, 6, 57, 0), when);
    }

    [TestMethod]
    [TestCategory("CrossPlatform")]
    public void ParseBackupTimestamp_OutlineNameContainsAsOf_UsesLastStamp()
    {
        var when = FileOpenVM.ParseBackupTimestamp(@"C:\b\Notes as of Tuesday as of 20250101_2359.zip");

        Assert.AreEqual(new DateTime(2025, 1, 1, 23, 59, 0), when);
    }

    [TestMethod]
    [TestCategory("CrossPlatform")]
    public void ParseBackupTimestamp_NameWithoutPattern_ReturnsNull()
    {
        Assert.IsNull(FileOpenVM.ParseBackupTimestamp(@"C:\b\Danger Calls.zip"));
        Assert.IsNull(FileOpenVM.ParseBackupTimestamp(@"C:\b\Danger Calls as of yesterday.zip"));
        Assert.IsNull(FileOpenVM.ParseBackupTimestamp(string.Empty));
    }

    [TestMethod]
    [TestCategory("CrossPlatform")]
    public void OrderBackupsNewestFirst_ThreeStampedNames_NewestFirst()
    {
        var oldest = @"C:\b\Story as of 20260101_0900.zip";
        var middle = @"C:\b\Story as of 20260615_1200.zip";
        var newest = @"C:\b\Story as of 20260904_0657.zip";

        var ordered = FileOpenVM.OrderBackupsNewestFirst(new[] { middle, oldest, newest });

        CollectionAssert.AreEqual(new[] { newest, middle, oldest }, ordered);
    }
}
