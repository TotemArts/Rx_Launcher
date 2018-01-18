using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RXPatchLib;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Threading;

namespace RXPatchLibTest
{
    [TestClass]
    public class RxPatcherTest
    {
        [TestMethod]
        public async Task EmptyRoundtrip()
        {
            await RoundtripTest((oldDir, newDir, targetDir, patchDir, applicationDir) =>
            {

            });
        }

        [TestMethod]
        public async Task SimpleRoundtrip()
        {
            await RoundtripTest((oldDir, newDir, targetDir, patchDir, applicationDir) =>
            {
                Directory.CreateDirectory(Path.Combine(newDir, "TestDir"));
                File.WriteAllText(Path.Combine(newDir, "TestDir", "TestFile"), "TestData");
            });
        }

        [TestMethod]
        [Ignore]
        public async Task Beta2To3Roundtrip()
        {
            var oldDir = "C:\\games\\Renegade X Beta 2";
            var newDir = "C:\\games\\Renegade X Beta 3";
            var targetDir = "C:\\games\\Renegade X patchtest";
            var applicationDir = "C:\\games\\Renegade X patchtest\\patch";
            using (var patchDir = new TemporaryDirectory())
            {
                await RoundtripTest(oldDir, newDir, targetDir, patchDir.Path, applicationDir);
            }
        }

        [TestMethod]
        [Ignore]
        public async Task Beta2To3Patch()
        {
            var newDir = "C:\\games\\Renegade X Beta 3";
            var targetDir = "C:\\games\\Renegade X patchtest";
            var applicationDir = "C:\\games\\Renegade X patchtest\\patch";
            var patchDir = "C:\\games\\Renegade X patchtest source";

            await new RxPatcher().ApplyPatchFromWeb("file:///" + patchDir, targetDir, applicationDir, TestProgressHandlerFactory.Create(), new CancellationToken(), null);

            await DirectoryAssertions.IsSubsetOf(newDir, targetDir);
        }

        public async Task RoundtripTest(Action<string, string, string, string, string> setupFiles)
        {
            using (var oldDir = new TemporaryDirectory())
            using (var newDir = new TemporaryDirectory())
            using (var targetDir = new TemporaryDirectory())
            using (var patchDir = new TemporaryDirectory())
            using (var applicationDir = new TemporaryDirectory())
            {
                setupFiles(oldDir.Path, newDir.Path, targetDir.Path, patchDir.Path, applicationDir.Path);

                await RoundtripTest(oldDir.Path, newDir.Path, targetDir.Path, patchDir.Path, applicationDir.Path);
            }
        }

        private async Task RoundtripTest(string oldDir, string newDir, string targetDir, string patchDir, string applicationDir)
        {
            var patchInfo = new PatchInfo
            {
                OldPath = oldDir,
                NewPath = newDir,
                PatchPath = patchDir,
            };

            var builder = new RxPatchBuilder();
            await builder.CreatePatchAsync(patchInfo);

            await new RxPatcher().ApplyPatchFromWeb("file:///" + patchDir, targetDir, applicationDir, TestProgressHandlerFactory.Create(), new CancellationToken(), null);

            await DirectoryAssertions.IsSubsetOf(newDir, targetDir);
        }
    }
}
