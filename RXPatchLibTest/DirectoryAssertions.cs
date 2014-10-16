using RXPatchLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RXPatchLibTest
{
    class DirectoryAssertions
    {
        public static void AreEquivalent(string expectedPath, string actualPath)
        {
            var expectedFiles = DirectoryPathIterator.GetChildPathsRecursive(expectedPath);
            var actualFiles = DirectoryPathIterator.GetChildPathsRecursive(actualPath);
            CollectionAssert.AreEquivalent(expectedFiles.ToArray(), actualFiles.ToArray());
            foreach (var file in expectedFiles)
            {
                var expectedFileContents = File.ReadAllBytes(Path.Combine(expectedPath, file));
                var actualFileContents = File.ReadAllBytes(Path.Combine(actualPath, file));
                CollectionAssert.AreEqual(expectedFileContents, actualFileContents, "file " + file + " is different");
            }
        }
    }
}
