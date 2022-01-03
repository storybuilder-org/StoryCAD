using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            string localPath = GlobalData.RootDirectory;

            ControlLoader loader = new();
            await loader.Init(localPath);
            Assert.IsNotNull(GlobalData.ConflictTypes);
            return;
        }
    }
}
