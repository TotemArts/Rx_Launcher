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
            await RoundtripTest((OldDir, NewDir, TargetDir, PatchDir, WorkingDir) =>
            {

            });
        }

        [TestMethod]
        public async Task SimpleRoundtrip()
        {
            await RoundtripTest((OldDir, NewDir, TargetDir, PatchDir, WorkingDir) =>
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
            var WorkingDir = "C:\\games\\Renegade X patchtest";
            using (var PatchDir = new TemporaryDirectory())
            {
                await RoundtripTest(OldDir, NewDir, TargetDir, PatchDir.Path, WorkingDir);
            }
        }

        [TestMethod]
        [Ignore]
        public async Task Beta2to3Patch()
        {
            var NewDir = "C:\\games\\Renegade X Beta 3";
            var TargetDir = "C:\\games\\Renegade X patchtest";
            var WorkingDir = "C:\\games\\Renegade X patchtest";
            var PatchDir = "C:\\games\\Renegade X patchtest source";

            var patcher = new RXPatcher();
            await patcher.ApplyPatch("file:///" + PatchDir, TargetDir, WorkingDir);

            await DirectoryAssertions.IsSubsetOf(NewDir, TargetDir);
        }

        public async Task RoundtripTest(Action<string, string, string, string, string> SetupFiles)
        {
            using (var OldDir = new TemporaryDirectory())
            using (var NewDir = new TemporaryDirectory())
            using (var TargetDir = new TemporaryDirectory())
            using (var PatchDir = new TemporaryDirectory())
            using (var WorkingDir = new TemporaryDirectory())
            {
                SetupFiles(OldDir.Path, NewDir.Path, TargetDir.Path, PatchDir.Path, WorkingDir.Path);

                await RoundtripTest(OldDir.Path, NewDir.Path, TargetDir.Path, PatchDir.Path, WorkingDir.Path);
            }
        }

        private async Task RoundtripTest(string OldDir, string NewDir, string TargetDir, string PatchDir, string WorkingDir)
        {
            var patchInfo = new PatchInfo
            {
                OldPath = OldDir,
                NewPath = NewDir,
                PatchPath = PatchDir,
            };

            var builder = new RXPatchBuilder();
            await builder.CreatePatchAsync(patchInfo);

            var patcher = new RXPatcher();
            await patcher.ApplyPatch("file:///" + PatchDir, TargetDir, WorkingDir);

            await DirectoryAssertions.IsSubsetOf(NewDir, TargetDir);
        }
    }
}
