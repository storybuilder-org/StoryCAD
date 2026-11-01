using StoryCADLib.Converters;

namespace StoryCADTests.Converters;

[TestClass]
public class EmptyStringToNullConverterTests
{
    private readonly EmptyStringToNullConverter _converter = new();

    [TestMethod]
    public void Convert_EmptyString_ReturnsNull()
    {
        // Null clears a ComboBox selection; an empty string not in the list is ignored (#1422).
        Assert.IsNull(_converter.Convert(string.Empty, typeof(object), null, string.Empty));
    }

    [TestMethod]
    public void Convert_ListValue_ReturnsSameValue()
    {
        Assert.AreEqual("Antagonist", _converter.Convert("Antagonist", typeof(object), null, string.Empty));
    }

    [TestMethod]
    public void ConvertBack_Null_ReturnsEmptyString()
    {
        Assert.AreEqual(string.Empty, _converter.ConvertBack(null, typeof(string), null, string.Empty));
    }

    [TestMethod]
    public void ConvertBack_ListValue_ReturnsSameValue()
    {
        Assert.AreEqual("Protagonist", _converter.ConvertBack("Protagonist", typeof(string), null, string.Empty));
    }
}
