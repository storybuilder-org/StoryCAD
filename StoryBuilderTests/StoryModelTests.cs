using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryBuilder.Models;

namespace StoryBuilderTests
{
    [TestClass]
    public class StoryModelTests
    {
        private StoryModel model = new StoryModel();

        [TestMethod]
        public void TestConstructor()
        {
            Assert.IsNotNull(model);
            Assert.IsNotNull(model.ExplorerView);
            Assert.IsNotNull(model.NarratorView);
            Assert.IsNotNull(model.StoryElements);
            Assert.IsNotNull(model.Relationships);
            Assert.IsFalse(model.Changed);
        }
    }
}