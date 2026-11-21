using System.Text.Json;
using StoryCADLib.DAL;
using StoryCADTests.Services.Rtf;

#nullable disable

namespace StoryCADTests.DAL;

[TestClass]
public class RtfThemeColorConverterTests
{
    private JsonSerializerOptions _options;

    [TestInitialize]
    public void Initialize()
    {
        _options = new JsonSerializerOptions { Converters = { new RtfThemeColorConverter() } };
    }

    [TestMethod]
    public void Write_WhenThemeColor_WritesCf0()
    {
        var json = JsonSerializer.Serialize(RtfThemeColorTests.Issue370Rtf, _options);

        StringAssert.Contains(json, @"\\cf0");
        Assert.IsFalse(json.Contains(@"\\cf1"));
    }

    [TestMethod]
    public void Read_WhenThemeColor_ReturnsCf0()
    {
        var json = JsonSerializer.Serialize(RtfThemeColorTests.Issue370Rtf);

        var result = JsonSerializer.Deserialize<string>(json, _options);

        StringAssert.Contains(result, @"\cf0");
        Assert.IsFalse(result.Contains(@"\cf1"));
    }

    [TestMethod]
    public void Write_WhenPlainText_WritesSameText()
    {
        var json = JsonSerializer.Serialize("plain text", _options);

        Assert.AreEqual("\"plain text\"", json);
    }
}
