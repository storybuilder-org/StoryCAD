using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryBuilder.Controllers;
using StoryBuilder.DAL;
using StoryBuilder.Models;

namespace StoryBuilderTests
{
    [TestClass]
    public class ControlLoaderTests
    {
        [TestMethod]
        public async Task TestControlLoader() 
        {
            StoryController story = new StoryController();
            Assert.IsNotNull(story);
            string localPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}";
            localPath = System.IO.Path.Combine(localPath, "StoryBuilder");
            //StorageFolder localFolder = await StorageFolder.GetFolderFromPathAsync(localPath);
            //Assert.IsNotNull(localFolder);
            ControlLoader loader = new ControlLoader();
            await loader.Init(localPath, story);
            Assert.IsNotNull(GlobalData.ConflictTypes);
            return;
        }
    }
}
