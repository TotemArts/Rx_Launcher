using Microsoft.VisualStudio.TestTools.UnitTesting;
using RXPatchLib;
using System.IO;
using System.Linq;

namespace RXPatchLibTest
{
    [TestClass]
    public class DirectoryPathIteratorTest
    {
        [TestMethod]
        public void TestEmpty()
        {
            using (var directory = new TemporaryDirectory())
            {
                var filePaths = DirectoryPathIterator.GetChildPathsRecursive(directory.Path);
                CollectionAssert.AreEquivalent(new string[] { }, filePaths.ToArray());
            }
        }

        [TestMethod]
        public void TestFlat()
        {
            using (var directory = new TemporaryDirectory())
            {
                File.WriteAllText(Path.Combine(directory.Path, "a"), "");
                File.WriteAllText(Path.Combine(directory.Path, "b"), "");
                File.WriteAllText(Path.Combine(directory.Path, "c"), "");
                var filePaths = DirectoryPathIterator.GetChildPathsRecursive(directory.Path);
                CollectionAssert.AreEquivalent(new string[] { "a", "b", "c" }, filePaths.ToArray());
            }
        }

        [TestMethod]
        public void TestRecursive()
        {
            using (var directory = new TemporaryDirectory())
            {
                Directory.CreateDirectory(Path.Combine(directory.Path, "sub"));
                Directory.CreateDirectory(Path.Combine(directory.Path, "sub", "sub2"));
                File.WriteAllText(Path.Combine(directory.Path, "a"), "");
                File.WriteAllText(Path.Combine(directory.Path, "sub", "b"), "");
                File.WriteAllText(Path.Combine(directory.Path, "sub", "sub2", "c"), "");
                var filePaths = DirectoryPathIterator.GetChildPathsRecursive(directory.Path);
                CollectionAssert.AreEquivalent(new string[] { 
                    Path.Combine("a"),
                    Path.Combine("sub", "b"),
                    Path.Combine("sub", "sub2", "c"),
                }, filePaths.ToArray());
            }
        }
    }
}
