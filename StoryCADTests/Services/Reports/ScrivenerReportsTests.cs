using System.Xml.Linq;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.DAL;
using StoryCADLib.Models;
using StoryCADLib.Services;
using StoryCADLib.Services.Outline;
using StoryCADLib.Services.Reports;

#nullable disable

namespace StoryCADTests.Services.Reports;

[TestClass]
public class ScrivenerReportsTests
{
    private AppState _appState;
    private OutlineService _outlineService;
    private string _testRoot;

    [TestInitialize]
    public void TestInitialize()
    {
        _appState = Ioc.Default.GetRequiredService<AppState>();
        _outlineService = Ioc.Default.GetRequiredService<OutlineService>();
        _testRoot = Path.Combine(Path.GetTempPath(), "StoryCAD-1568", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testRoot);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        try
        {
            if (Directory.Exists(_testRoot))
            {
                Directory.Delete(_testRoot, true);
            }
        }
        catch
        {
            // Temp files may still be held by StorageFile
        }
    }

    [TestMethod]
    public async Task GenerateReports_FirstExport_WritesOneStoryCADFolderWithUniqueUuids()
    {
        var model = await CreateOutline();
        var (projectDir, file) = await CreateScrivenerProject("FirstExport");

        await Export(file, projectDir, model);

        var xml = XDocument.Load(file.Path);
        Assert.AreEqual(1, CountStoryCADFolders(xml));
        AssertUniqueBinderUuids(xml);
        AssertStoryCADImmediatelyBeforeResearch(xml);
        Assert.AreEqual(1, xml.Descendants("BinderItem")
            .Count(e => (string)e.Attribute("Type") == "DraftFolder"));
        Assert.AreEqual(1, xml.Descendants("BinderItem")
            .Count(e => (string)e.Attribute("Type") == "ResearchFolder"));
        Assert.AreEqual(1, xml.Descendants("BinderItem")
            .Count(e => (string)e.Attribute("Type") == "TrashFolder"));
    }

    [TestMethod]
    public async Task GenerateReports_SecondExport_ReplacesStoryCADFolderWithoutDuplicateUuids()
    {
        var model = await CreateOutline();
        var (projectDir, file) = await CreateScrivenerProject("Reexport");

        await Export(file, projectDir, model);
        await Export(file, projectDir, model);

        var xml = XDocument.Load(file.Path);
        Assert.AreEqual(1, CountStoryCADFolders(xml));
        AssertUniqueBinderUuids(xml);
        AssertStoryCADImmediatelyBeforeResearch(xml);
    }

    [TestMethod]
    public async Task GenerateReports_SceneInExplorerAndNarrator_UsesDistinctUuidsAndStbUuid()
    {
        var model = await CreateOutline();
        var scene = _outlineService.AddStoryElement(model, StoryItemType.Scene, model.ExplorerView[0]);
        Assert.IsTrue(scene.Node.CopyToNarratorView(model));
        var (projectDir, file) = await CreateScrivenerProject("NarratorUuid");

        await Export(file, projectDir, model);

        var xml = XDocument.Load(file.Path);
        AssertUniqueBinderUuids(xml);
        var scenes = xml.Descendants("BinderItem")
            .Where(e => (string)e.Element("Title") == scene.Name)
            .ToList();
        Assert.AreEqual(2, scenes.Count, "Explorer and Narrator should each have the scene");
        var explorer = scenes.Single(e =>
            Guid.Parse((string)e.Attribute("UUID")!) == scene.Uuid);
        var narrator = scenes.Single(e =>
            Guid.Parse((string)e.Attribute("UUID")!) != scene.Uuid);
        var stb = narrator.Element("MetaData")
            ?.Element("CustomMetaData")
            ?.Elements("MetaDataItem")
            .FirstOrDefault(i => (string)i.Element("FieldID") == "stbuuid")
            ?.Element("Value")?.Value;
        Assert.IsFalse(string.IsNullOrEmpty(stb), "Narrator scene should have stbuuid metadata");
        Assert.AreEqual(scene.Uuid, Guid.Parse(stb!));
        Assert.AreEqual(scene.Uuid, Guid.Parse((string)explorer.Attribute("UUID")!));
    }

    [TestMethod]
    public async Task GenerateReports_StoryCADFolderInTrash_RemovesTrashCopyAndInsertsBeforeResearch()
    {
        var model = await CreateOutline();
        var (projectDir, file) = await CreateScrivenerProject("TrashCopy", storyCadInTrash: true);

        await Export(file, projectDir, model);

        var xml = XDocument.Load(file.Path);
        Assert.AreEqual(1, CountStoryCADFolders(xml));
        AssertUniqueBinderUuids(xml);
        AssertStoryCADImmediatelyBeforeResearch(xml);
        var trash = xml.Descendants("BinderItem")
            .First(e => (string)e.Attribute("Type") == "TrashFolder");
        Assert.IsFalse(trash.Descendants("BinderItem")
            .Any(e => (string)e.Element("Title") == "StoryCAD"));
    }

    private async Task<StoryModel> CreateOutline()
    {
        var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
        _appState.CurrentDocument = new StoryDocument(model, "test.stbx");
        return model;
    }

    private async Task Export(StorageFile file, string projectDir, StoryModel model)
    {
        _appState.CurrentDocument = new StoryDocument(model, "test.stbx");
        var scrivener = Ioc.Default.GetRequiredService<ScrivenerIo>();
        scrivener.ProjectPath = projectDir;
        var reports = new ScrivenerReports(file, _appState);
        await reports.GenerateReports();
    }

    private async Task<(string projectDir, StorageFile file)> CreateScrivenerProject(
        string name, bool storyCadInTrash = false)
    {
        var projectDir = Path.Combine(_testRoot, name);
        Directory.CreateDirectory(Path.Combine(projectDir, "Files", "Data"));
        var scrivxPath = Path.Combine(projectDir, name + ".scrivx");
        await File.WriteAllTextAsync(scrivxPath, MinimalScrivx(storyCadInTrash));
        var file = await StorageFile.GetFileFromPathAsync(scrivxPath);
        return (projectDir, file);
    }

    private static string MinimalScrivx(bool storyCadInTrash)
    {
        var trashChildren = storyCadInTrash
            ? """
                  <Children>
                    <BinderItem UUID="EEEEEEEE-EEEE-EEEE-EEEE-EEEEEEEEEEEE" Type="Folder" Created="2026-01-01 00:00:00 +0000" Modified="2026-01-01 00:00:00 +0000">
                      <Title>StoryCAD</Title>
                      <MetaData><IncludeInCompile>Yes</IncludeInCompile></MetaData>
                    </BinderItem>
                  </Children>
              """
            : "";

        return $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <ScrivenerProject Version="2.0">
              <Binder>
                <BinderItem UUID="A0000000-0000-0000-0000-000000000001" Type="DraftFolder" Created="2026-01-01 00:00:00 +0000" Modified="2026-01-01 00:00:00 +0000">
                  <Title>Draft</Title>
                  <MetaData><IncludeInCompile>Yes</IncludeInCompile></MetaData>
                </BinderItem>
                <BinderItem UUID="A0000000-0000-0000-0000-000000000002" Type="ResearchFolder" Created="2026-01-01 00:00:00 +0000" Modified="2026-01-01 00:00:00 +0000">
                  <Title>Research</Title>
                  <MetaData><IncludeInCompile>No</IncludeInCompile></MetaData>
                </BinderItem>
                <BinderItem UUID="A0000000-0000-0000-0000-000000000003" Type="TrashFolder" Created="2026-01-01 00:00:00 +0000" Modified="2026-01-01 00:00:00 +0000">
                  <Title>Trash</Title>
                  <MetaData><IncludeInCompile>No</IncludeInCompile></MetaData>
                  {trashChildren}
                </BinderItem>
              </Binder>
              <StatusSettings>
                <Title>Status</Title>
              </StatusSettings>
              <ProjectBookmarks />
            </ScrivenerProject>
            """;
    }

    private static int CountStoryCADFolders(XDocument xml)
    {
        return xml.Descendants("BinderItem").Count(e => (string)e.Element("Title") == "StoryCAD");
    }

    private static void AssertUniqueBinderUuids(XDocument xml)
    {
        var uuids = xml.Descendants("BinderItem")
            .Select(e => (string)e.Attribute("UUID"))
            .Where(u => !string.IsNullOrEmpty(u))
            .ToList();
        Assert.AreEqual(uuids.Count, uuids.Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            "BinderItem UUID values must be unique");
    }

    private static void AssertStoryCADImmediatelyBeforeResearch(XDocument xml)
    {
        var binderKids = xml.Root!.Element("Binder")!.Elements("BinderItem").ToList();
        var storyCadIndex = binderKids.FindIndex(e => (string)e.Element("Title") == "StoryCAD");
        var researchIndex = binderKids.FindIndex(e => (string)e.Attribute("Type") == "ResearchFolder");
        Assert.AreNotEqual(-1, storyCadIndex, "Binder root should contain a StoryCAD folder");
        Assert.AreNotEqual(-1, researchIndex, "Binder root should contain a ResearchFolder");
        Assert.AreEqual(researchIndex - 1, storyCadIndex,
            "StoryCAD folder should sit immediately before Research");
    }
}
