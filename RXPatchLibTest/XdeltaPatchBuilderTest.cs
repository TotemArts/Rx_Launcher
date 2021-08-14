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
            var patchSystem = XdeltaPatchSystemFactory.Preferred;
            var patchBuilder = new XdeltaPatchBuilder(patchSystem);

            using (var oldFile = new TemporaryFile())
            using (var newFile = new TemporaryFile())
            using (var patchFile = new TemporaryFile())
            {
                await patchBuilder.CreatePatchAsync(oldFile.Path, newFile.Path, patchFile.Path);
                Assert.AreNotEqual(0, new FileInfo(patchFile.Path).Length);
            }
        }

        [TestMethod]
        public async Task PatchContentsDoNotDependOnInputPath()
        {
            var patchBuilder = new XdeltaPatchBuilder(XdeltaPatchSystemFactory.Preferred);

            using (var oldFile1 = new TemporaryFile())
            using (var newFile1 = new TemporaryFile())
            using (var oldFile2 = new TemporaryFile())
            using (var newFile2 = new TemporaryFile())
            using (var patchFile1 = new TemporaryFile())
            using (var patchFile2 = new TemporaryFile())
            {
                File.WriteAllText(oldFile1.Path, "a");
                File.WriteAllText(oldFile2.Path, "a");
                File.WriteAllText(newFile1.Path, "b");
                File.WriteAllText(newFile2.Path, "b");
                await patchBuilder.CreatePatchAsync(oldFile1.Path, newFile1.Path, patchFile1.Path);
                await patchBuilder.CreatePatchAsync(oldFile2.Path, newFile2.Path, patchFile2.Path);
                CollectionAssert.AreEquivalent(File.ReadAllBytes(patchFile1.Path), File.ReadAllBytes(patchFile2.Path));
            }
        }

        [TestMethod]
        public async Task CompressedContentsDoNotDependOnInputPath()
        {
            var patchBuilder = new XdeltaPatchBuilder(XdeltaPatchSystemFactory.Preferred);

            using (var newFile1 = new TemporaryFile())
            using (var newFile2 = new TemporaryFile())
            using (var patchFile1 = new TemporaryFile())
            using (var patchFile2 = new TemporaryFile())
            {
                File.WriteAllText(newFile1.Path, "b");
                File.WriteAllText(newFile2.Path, "b");
                await patchBuilder.CompressAsync(newFile1.Path, patchFile1.Path);
                await patchBuilder.CompressAsync(newFile2.Path, patchFile2.Path);
                CollectionAssert.AreEquivalent(File.ReadAllBytes(patchFile1.Path), File.ReadAllBytes(patchFile2.Path));
            }
        }
    }
}
