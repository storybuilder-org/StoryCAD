using StoryCADLib.Services.Rtf;

#nullable disable

namespace StoryCADTests.Services.Rtf;

[TestClass]
public class RtfThemeColorTests
{
    // RTF from a dark theme, as recorded on issue #370.
    internal const string Issue370Rtf =
        @"{\rtf1\fbidis\ansi\ansicpg1252\deff0\nouicompat\deflang1033{\fonttbl{\f0\fnil\fcharset0 Segoe UI;}}" + "\r\n" +
        @"{\colortbl ;\red255\green255\blue255;}" + "\r\n" +
        @"{\*\generator Riched20 3.1.0003}\viewkind4\uc1 " + "\r\n" +
        @"\pard\cf1\f0\fs21 How do you deal with your own immanent death?\par" + "\r\n" +
        "}";

    [TestMethod]
    public void RemoveThemeColors_WhenWhite_ReplacesCf1WithCf0()
    {
        var expected = Issue370Rtf.Replace(@"\pard\cf1\f0", @"\pard\cf0\f0");

        var result = RtfThemeColor.RemoveThemeColors(Issue370Rtf);

        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void RemoveThemeColors_WhenLightGrey_ReplacesCf1WithCf0()
    {
        var input = Issue370Rtf.Replace(@"\red255\green255\blue255", @"\red211\green211\blue211");
        var expected = input.Replace(@"\cf1", @"\cf0");

        var result = RtfThemeColor.RemoveThemeColors(input);

        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void RemoveThemeColors_WhenBlack_ReplacesCf1WithCf0()
    {
        var input = @"{\rtf1{\colortbl ;\red0\green0\blue0;}\cf1 a\par}";

        var result = RtfThemeColor.RemoveThemeColors(input);

        Assert.AreEqual(@"{\rtf1{\colortbl ;\red0\green0\blue0;}\cf0 a\par}", result);
    }

    [TestMethod]
    public void RemoveThemeColors_WhenOtherColor_KeepsText()
    {
        var input = @"{\rtf1{\colortbl ;\red255\green0\blue0;}\cf1 a\par}";

        var result = RtfThemeColor.RemoveThemeColors(input);

        Assert.AreEqual(input, result);
    }

    [TestMethod]
    public void RemoveThemeColors_WhenMixedRuns_ReplacesOnlyThemeEntry()
    {
        var input = @"{\rtf1{\colortbl ;\red255\green255\blue255;\red255\green0\blue0;}\cf1 a \cf2 red\cf1  b\par}";

        var result = RtfThemeColor.RemoveThemeColors(input);

        Assert.AreEqual(@"{\rtf1{\colortbl ;\red255\green255\blue255;\red255\green0\blue0;}\cf0 a \cf2 red\cf0  b\par}", result);
    }

    [TestMethod]
    public void RemoveThemeColors_WhenCf0_KeepsText()
    {
        var input = @"{\rtf1{\colortbl ;\red255\green255\blue255;}\cf0 a\par}";

        var result = RtfThemeColor.RemoveThemeColors(input);

        Assert.AreEqual(input, result);
    }

    [TestMethod]
    public void RemoveThemeColors_WhenEntryZeroIsBlack_KeepsCf0()
    {
        var input = @"{\rtf1{\colortbl\red0\green0\blue0;\red255\green0\blue0;}\cf0 a\cf1 b\par}";

        var result = RtfThemeColor.RemoveThemeColors(input);

        Assert.AreEqual(input, result);
    }

    [TestMethod]
    public void RemoveThemeColors_WhenPartialEntry_KeepsText()
    {
        var input = @"{\rtf1{\colortbl ;\red255\green255;}\cf1 a\par}";

        var result = RtfThemeColor.RemoveThemeColors(input);

        Assert.AreEqual(input, result);
    }

    [TestMethod]
    public void RemoveThemeColors_WhenCf10_KeepsText()
    {
        var input = @"{\rtf1{\colortbl ;\red255\green255\blue255;}\cf10 a\par}";

        var result = RtfThemeColor.RemoveThemeColors(input);

        Assert.AreEqual(input, result);
    }

    [TestMethod]
    public void RemoveThemeColors_WhenBoldItalicUnderline_KeepsFormat()
    {
        var input = @"{\rtf1{\colortbl ;\red255\green255\blue255;}\cf1\b a\b0\i b\i0\ul c\ulnone\par}";

        var result = RtfThemeColor.RemoveThemeColors(input);

        Assert.AreEqual(@"{\rtf1{\colortbl ;\red255\green255\blue255;}\cf0\b a\b0\i b\i0\ul c\ulnone\par}", result);
    }

    [TestMethod]
    public void RemoveThemeColors_WhenEscapedBackslash_KeepsText()
    {
        var input = @"{\rtf1{\colortbl ;\red255\green255\blue255;}a \\cf1 b\par}";

        var result = RtfThemeColor.RemoveThemeColors(input);

        Assert.AreEqual(input, result);
    }

    [TestMethod]
    public void RemoveThemeColors_WhenControlSymbols_ReplacesCf1()
    {
        var input = @"{\rtf1{\colortbl ;\red255\green255\blue255;}{\*\generator x}\cf1 a\~b\-c\par}";

        var result = RtfThemeColor.RemoveThemeColors(input);

        Assert.AreEqual(@"{\rtf1{\colortbl ;\red255\green255\blue255;}{\*\generator x}\cf0 a\~b\-c\par}", result);
    }

    [TestMethod]
    public void RemoveThemeColors_WhenBinaryData_ReturnsInput()
    {
        var input = @"{\rtf1{\colortbl ;\red255\green255\blue255;}\cf1 a\bin4 ABCD\par}";

        var result = RtfThemeColor.RemoveThemeColors(input);

        Assert.AreEqual(input, result);
    }

    [TestMethod]
    public void RemoveThemeColors_WhenNoColorTable_ReturnsInput()
    {
        var input = @"{\rtf1 a\par}";

        var result = RtfThemeColor.RemoveThemeColors(input);

        Assert.AreEqual(input, result);
    }

    [TestMethod]
    public void RemoveThemeColors_WhenColorTableNotClosed_ReturnsInput()
    {
        var input = @"{\rtf1{\colortbl ;\red255";

        var result = RtfThemeColor.RemoveThemeColors(input);

        Assert.AreEqual(input, result);
    }

    [TestMethod]
    public void RemoveThemeColors_WhenPlainText_ReturnsInput()
    {
        var input = "plain text";

        var result = RtfThemeColor.RemoveThemeColors(input);

        Assert.AreEqual(input, result);
    }

    [TestMethod]
    public void RemoveThemeColors_WhenNull_ReturnsNull()
    {
        var result = RtfThemeColor.RemoveThemeColors(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void RemoveThemeColors_WhenEmpty_ReturnsEmpty()
    {
        var result = RtfThemeColor.RemoveThemeColors(string.Empty);

        Assert.AreEqual(string.Empty, result);
    }
}
