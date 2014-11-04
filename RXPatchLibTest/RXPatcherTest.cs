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
    public class RXPatcherTest
    {
        [TestMethod]
        public async Task EmptyRoundtrip()
        {
            await RoundtripTest((OldDir, NewDir, TargetDir, PatchDir, ApplicationDir) =>
            {

            });
        }

        [TestMethod]
        public async Task SimpleRoundtrip()
        {
            await RoundtripTest((OldDir, NewDir, TargetDir, PatchDir, ApplicationDir) =>
            {
                Directory.CreateDirectory(Path.Combine(NewDir, "TestDir"));
                File.WriteAllText(Path.Combine(NewDir, "TestDir", "TestFile"), "TestData");
            });
        }

        [TestMethod]
        [Ignore]
        public async Task Beta2to3Roundtrip()
        {
            var OldDir = "C:\\games\\Renegade X Beta 2";
            var NewDir = "C:\\games\\Renegade X Beta 3";
            var TargetDir = "C:\\games\\Renegade X patchtest";
            var ApplicationDir = "C:\\games\\Renegade X patchtest\\patch";
            using (var PatchDir = new TemporaryDirectory())
            {
                await RoundtripTest(OldDir, NewDir, TargetDir, PatchDir.Path, ApplicationDir);
            }
        }

        [TestMethod]
        [Ignore]
        public async Task Beta2to3Patch()
        {
            var NewDir = "C:\\games\\Renegade X Beta 3";
            var TargetDir = "C:\\games\\Renegade X patchtest";
            var ApplicationDir = "C:\\games\\Renegade X patchtest\\patch";
            var PatchDir = "C:\\games\\Renegade X patchtest source";

            await new RXPatcher().ApplyPatchFromWeb("file:///" + PatchDir, TargetDir, ApplicationDir, TestProgressHandlerFactory.Create());

            await DirectoryAssertions.IsSubsetOf(NewDir, TargetDir);
        }

        public async Task RoundtripTest(Action<string, string, string, string, string> SetupFiles)
        {
            using (var OldDir = new TemporaryDirectory())
            using (var NewDir = new TemporaryDirectory())
            using (var TargetDir = new TemporaryDirectory())
            using (var PatchDir = new TemporaryDirectory())
            using (var ApplicationDir = new TemporaryDirectory())
            {
                SetupFiles(OldDir.Path, NewDir.Path, TargetDir.Path, PatchDir.Path, ApplicationDir.Path);

                await RoundtripTest(OldDir.Path, NewDir.Path, TargetDir.Path, PatchDir.Path, ApplicationDir.Path);
            }
        }

        private async Task RoundtripTest(string OldDir, string NewDir, string TargetDir, string PatchDir, string ApplicationDir)
        {
            var patchInfo = new PatchInfo
            {
                OldPath = OldDir,
                NewPath = NewDir,
                PatchPath = PatchDir,
            };

            var builder = new RXPatchBuilder();
            await builder.CreatePatchAsync(patchInfo);

            await new RXPatcher().ApplyPatchFromWeb("file:///" + PatchDir, TargetDir, ApplicationDir, TestProgressHandlerFactory.Create());

            await DirectoryAssertions.IsSubsetOf(NewDir, TargetDir);
        }
    }
}
