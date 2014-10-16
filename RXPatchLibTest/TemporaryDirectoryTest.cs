using Microsoft.VisualStudio.TestTools.UnitTesting;
using RXPatchLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RXPatchLibTest
{
    [TestClass]
    public class TemporaryDirectoryTest
    {
        [TestMethod]
        public void TestEmpty()
        {
            string path;
            using (var directory = new TemporaryDirectory())
            {
                path = directory.Path;
                Assert.IsTrue(Directory.Exists(path));
            }
            Assert.IsFalse(Directory.Exists(path));
        }
        [TestMethod]
        public void TestNonempty()
        {
            string path;
            using (var directory = new TemporaryDirectory())
            {
                path = directory.Path;
                File.WriteAllText(Path.Combine(directory.Path, "a"), "");
            }
            Assert.IsFalse(Directory.Exists(path));
        }
    }
}
