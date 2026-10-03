using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Services.Reports;

namespace StoryCADTests.Services.Reports;

[TestClass]
public class RichTextStripperTests
{
    [TestMethod]
    public void StripRichTextFormat_EmojiAsSurrogatePair_KeepsTheCharacter()
    {
        // RichEdit writes U+1F600 as two signed \u codes: \u-10179 (0xD83D) and \u-8704 (0xDE00).
        const string rtf = @"{\rtf1\ansi\uc1 Smile \u-10179?\u-8704? done\par}";

        var text = new RichTextStripper().StripRichTextFormat(rtf);

        Assert.AreEqual("Smile \U0001F600 done", text);
    }

    [TestMethod]
    public void StripRichTextFormat_CurlyQuote_KeepsTheCharacter()
    {
        // A normal string with doubled backslashes: the C# compiler turns 舗 into a
        // character even inside a verbatim string.
        const string rtf = "{\\rtf1\\ansi\\uc1 it\\u8217?s\\par}";

        var text = new RichTextStripper().StripRichTextFormat(rtf);

        Assert.AreEqual("it’s", text);
    }
}
