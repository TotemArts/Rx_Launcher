using RXPatchLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RXPatchLibTest
{
    class DirectoryAssertions
    {
        public static async Task IsSubsetOf(string expectedPath, string actualPath)
        {
            var expectedFiles = DirectoryPathIterator.GetChildPathsRecursive(expectedPath);
            var actualFiles = DirectoryPathIterator.GetChildPathsRecursive(actualPath);
            CollectionAssert.IsSubsetOf(expectedFiles.ToArray(), actualFiles.ToArray());
            foreach (var file in expectedFiles)
            {
                var expectedFileContents = await SHA256.GetFileHashAsync(Path.Combine(expectedPath, file));
                var actualFileContents = await SHA256.GetFileHashAsync(Path.Combine(actualPath, file));
                Assert.AreEqual(expectedFileContents, actualFileContents, "file " + file + " is different");
            }
        }
    }
}
