using Microsoft.VisualStudio.TestTools.UnitTesting;
using RXPatchLib;
using System.IO;

namespace RXPatchLibTest
{
    [TestClass]
    public class TemporaryFileTest
    {
        [TestMethod]
        public void Test()
        {
            string path;
            using (var file = new TemporaryFile())
            {
                path = file.Path;
                Assert.IsTrue(File.Exists(path));
            }
            Assert.IsFalse(File.Exists(path));
        }
    }
}
