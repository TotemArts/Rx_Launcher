using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RXPatchLib;
using System.Threading.Tasks;
using MonoTorrent.Tracker;
using MonoTorrent.Tracker.Listeners;
using System.IO;
using System.Collections.Generic;
using MonoTorrent.Client;
using System.Threading;
using MonoTorrent.Common;

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
        public async Task Beta2to3Patch()
        {
            var NewDir = "C:\\games\\Renegade X Beta 3";
            var TargetDir = "C:\\games\\Renegade X patchtest";
            var WorkingDir = "C:\\games\\Renegade X patchtest";
            var PatchDir = "C:\\games\\Renegade X patchtest source";
            var torrentPath = Path.Combine(PatchDir, "unnamed patch.torrent");
            byte[] torrentData = File.ReadAllBytes(torrentPath);

            var patcher = new RXPatcher();
            await patcher.ApplyPatch(torrentData, TargetDir, WorkingDir);

            DirectoryAssertions.AreEquivalent(NewDir, TargetDir);
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
                TorrentInfo = new PatchTorrentInfo
                {
                    AnnounceUrls = new List<List<String>> { new List<String> { "http://192.168.56.101:6969/announce" } }
                }
            };

            var builder = new RXPatchBuilder();
            await builder.CreatePatchAsync(patchInfo);

            var torrentPath = Path.Combine(PatchDir, patchInfo.TorrentInfo.Name + ".torrent");
            byte[] torrentData = File.ReadAllBytes(torrentPath);
            /*
            using (var Tracker = new Tracker())...
            var listener = new HttpListener("http://localhost:6969/announce/");
            Tracker.TimeoutInterval = new TimeSpan(50);
            Tracker.RegisterListener(listener);
            Tracker.Add(new InfoHashTrackable(Torrent.Load(torrentData)));
            listener.Start();*/

            var patcher = new RXPatcher();
            await patcher.ApplyPatch(torrentData, TargetDir, WorkingDir);

            DirectoryAssertions.AreEquivalent(NewDir, TargetDir);
        }
    }
}
