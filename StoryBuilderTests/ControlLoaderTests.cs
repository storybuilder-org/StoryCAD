using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryBuilder.Controllers;
using StoryBuilder.DAL;
using StoryBuilder.Models;
using System.Threading.Tasks;

namespace StoryBuilderTests
{
    [TestClass]
    public class ControlLoaderTests
    {
        [TestMethod]
        public async Task TestControlLoader()
        {
            StoryController story = new();
            Assert.IsNotNull(story);
            string localPath = GlobalData.RootDirectory;

            ControlLoader loader = new();
            await loader.Init(localPath, story);
            Assert.IsNotNull(GlobalData.ConflictTypes);
            return;
        }
    }
}
