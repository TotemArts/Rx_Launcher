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
