using Microsoft.VisualStudio.TestTools.UnitTesting;
using RXPatchLib;
using System.Threading.Tasks;
using System.IO;

namespace RXPatchLibTest
{
    [TestClass]
    public class XdeltaPatcherTest
    {
        [TestMethod]
        public async Task TestRoundtrip()
        {
            var patchSystem = XdeltaPatchSystemFactory.Preferred;
            var patchBuilder = new XdeltaPatchBuilder(patchSystem);
            var patcher = new XdeltaPatcher(patchSystem);

            using (var oldFile = new TemporaryFile())
            using (var newFile = new TemporaryFile())
            using (var patchFile = new TemporaryFile())
            using (var patchedFile = new TemporaryFile())
            {
                File.WriteAllText(oldFile.Path, "old");
                File.WriteAllText(newFile.Path, "new");
                await patchBuilder.CreatePatchAsync(oldFile.Path, newFile.Path, patchFile.Path);
                await patcher.ApplyPatchAsync(oldFile.Path, patchedFile.Path, patchFile.Path);
                Assert.AreEqual("new", File.ReadAllText(patchedFile.Path));
            }
        }
    }
}
