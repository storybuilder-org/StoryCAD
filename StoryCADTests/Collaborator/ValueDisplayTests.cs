using System;
using System.Collections.Generic;
using System.Text.Json;
using StoryCollaborator.Models;

#nullable disable

namespace StoryCADTests.Collaborator;

/// <summary>
/// Unit tests for ValueDisplay (#129 display formatting).
/// </summary>
[TestClass]
public class ValueDisplayTests
{
    #region SplitPascalCase Tests

    [TestMethod]
    public void SplitPascalCase_WithPascalCase_InsertsSpaces()
    {
        Assert.AreEqual("Structure Title", ValueDisplay.SplitPascalCase("StructureTitle"));
    }

    [TestMethod]
    public void SplitPascalCase_WithAcronym_KeepsAcronymTogether()
    {
        Assert.AreEqual("GMC Notes", ValueDisplay.SplitPascalCase("GMCNotes"));
    }

    [TestMethod]
    public void SplitPascalCase_WithSingleWord_ReturnsUnchanged()
    {
        Assert.AreEqual("Premise", ValueDisplay.SplitPascalCase("Premise"));
    }

    [TestMethod]
    public void SplitPascalCase_WithNullOrEmpty_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, ValueDisplay.SplitPascalCase(null));
        Assert.AreEqual(string.Empty, ValueDisplay.SplitPascalCase("  "));
    }

    #endregion

    #region Format Tests

    [TestMethod]
    public void Format_WithNull_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, ValueDisplay.Format(null));
    }

    [TestMethod]
    public void Format_WithString_ReturnsSameString()
    {
        Assert.AreEqual("hello", ValueDisplay.Format("hello"));
    }

    [TestMethod]
    public void Format_WithStringList_ReturnsBulletedLines()
    {
        var result = ValueDisplay.Format(new List<string> { "one", "two" });

        Assert.AreEqual("• one\n• two", result);
    }

    [TestMethod]
    public void Format_WithBeatList_ReturnsNumberedTitlesAndDescriptions()
    {
        var beats = new List<BeatInfo>
        {
            new("Act I", "Setup"),
            new("Act II", "")
        };

        var result = ValueDisplay.Format(beats);

        Assert.AreEqual("1. Act I — Setup\n2. Act II", result);
    }

    [TestMethod]
    public void Format_WithGuidList_ResolvesElementNames()
    {
        var guid = Guid.NewGuid();

        var result = ValueDisplay.Format(new List<Guid> { guid }, g => g == guid ? "Greta" : null);

        Assert.AreEqual("• Greta", result);
    }

    [TestMethod]
    public void Format_WithUnresolvedGuid_FallsBackToGuidString()
    {
        var guid = Guid.NewGuid();

        var result = ValueDisplay.Format(new List<Guid> { guid });

        Assert.AreEqual($"• {guid:D}", result);
    }

    [TestMethod]
    public void Format_WithRelationshipList_ResolvesNameAndDescription()
    {
        var guid = Guid.NewGuid();
        var relationships = new List<RelationshipInfo> { new(guid, "rival") };

        var result = ValueDisplay.Format(relationships, _ => "Herold");

        Assert.AreEqual("• Herold — rival", result);
    }

    [TestMethod]
    public void Format_WithJsonElementList_ReturnsReadablePairs()
    {
        using var doc = JsonDocument.Parse("""{"TraitName":"Brave","TraitNotes":"Under fire"}""");
        var entries = new List<JsonElement> { doc.RootElement.Clone() };

        var result = ValueDisplay.Format(entries);

        Assert.AreEqual("• Trait Name: Brave; Trait Notes: Under fire", result);
    }

    [TestMethod]
    public void Format_WithList_NeverReturnsClrTypeName()
    {
        var result = ValueDisplay.Format(new List<string> { "x" });

        Assert.IsFalse(result.Contains("System.Collections"));
    }

    #endregion
}
