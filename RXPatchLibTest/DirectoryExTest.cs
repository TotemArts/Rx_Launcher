using RXPatchLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace RXPatchLibTest
{
    [TestClass]
    public class DirectoryExTest
    {
        [TestMethod]
        public void TestDeleteContents()
        {
            using (var dir = new TemporaryDirectory())
            {
                var subDirPath = Path.Combine(dir.Path, "subDir");
                var subDirFilePath = Path.Combine(subDirPath, "file");
                var filePath = Path.Combine(dir.Path, "file");
                Directory.CreateDirectory(subDirPath);
                File.WriteAllText(subDirFilePath, "");
                File.WriteAllText(filePath, "");
                DirectoryEx.DeleteContents(dir.Path);
                Assert.IsFalse(File.Exists(filePath));
                Assert.IsFalse(Directory.Exists(subDirPath));
                Assert.IsTrue(Directory.Exists(dir.Path));
            }
        }
    }
}
