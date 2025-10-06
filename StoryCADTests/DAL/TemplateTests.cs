using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels;

#nullable disable

namespace StoryCADTests.DAL;

[TestClass]
public class TemplateTests
{
    /// <summary>
    ///     This tests if all the samples child nodes have parents
    /// </summary>
    [TestMethod]
    public async Task TestSamplesAsync()
    {
        var outline = Ioc.Default.GetService<OutlineService>();
        var fileVm = Ioc.Default.GetRequiredService<FileOpenVM>();

        // Validate templates
        for (var index = 0; index <= 5; index++)
        {
            var model = await outline!.CreateModel($"Test{index}", "Sample Test", index);
            Assert.IsNotNull(model);
            foreach (var item in model.ExplorerView[0].Children)
            {
                Assert.IsTrue(model.StoryElements.Count > 2, "Template missing data.");
            }
        }

        // Validate bundled sample projects
        for (var i = 0; i < fileVm.SampleNames.Count; i++)
        {
            fileVm.SelectedSampleIndex = i;
            await fileVm.OpenSample();
            var model = Ioc.Default.GetRequiredService<AppState>().CurrentDocument.Model;
            Assert.IsNotNull(model);
            Assert.IsTrue(model.StoryElements.Count > 2, $"Sample {fileVm.SampleNames[i]} missing data.");
        }
    }

    [TestMethod]
    public async Task Templates_DoNotCreateDuplicates()
    {
        var outline = Ioc.Default.GetRequiredService<OutlineService>();
        for (var i = 0; i <= 5; i++)
        {
            var model = await outline.CreateModel($"T{i}", "Author", i);
            var seen = new HashSet<Guid>();

            void Walk(StoryNodeItem n)
            {
                Assert.IsTrue(seen.Add(n.Uuid), $"Duplicate node in template {i}");
                if (n.Children != null)
                {
                    foreach (var c in n.Children)
                    {
                        Walk(c);
                    }
                }
            }

            Walk(model.ExplorerView[0]);
        }
    }
}
