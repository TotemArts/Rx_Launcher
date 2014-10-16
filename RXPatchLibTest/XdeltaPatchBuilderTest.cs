using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RXPatchLib;
using System.Threading.Tasks;
using System.IO;

namespace RXPatchLibTest
{
    [TestClass]
    public class XdeltaPatchBuilderTest
    {
        [TestMethod]
        public async Task TestWithEmptyFiles()
        {
            var patchSystem = new XdeltaPatchSystem();
            var patchBuilder = new XdeltaPatchBuilder(patchSystem);

            using (var oldFile = new TemporaryFile())
            using (var newFile = new TemporaryFile())
            using (var patchFile = new TemporaryFile())
            {
                await patchBuilder.CreatePatchAsync(oldFile.Path, newFile.Path, patchFile.Path);
                Assert.AreNotEqual(0, new FileInfo(patchFile.Path).Length);
            }
        }
    }
}
