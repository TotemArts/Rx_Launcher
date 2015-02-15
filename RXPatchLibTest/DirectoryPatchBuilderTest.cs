using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RXPatchLib;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace RXPatchLibTest
{
    [TestClass]
    public class DirectoryPatchBuilderTest
    {
        const string DummyDataA = "dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/dummy data/";
        const string DummyDataB = "more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/more faked/";

        string GetStringHash(string s)
        {
            return BitConverter.ToString(new SHA1CryptoServiceProvider().ComputeHash(Encoding.ASCII.GetBytes(s))).Replace("-", string.Empty);
        }

        [TestMethod]
        public async Task TestEmpty()
        {
            using (var oldDir = new TemporaryDirectory())
            using (var newDir = new TemporaryDirectory())
            using (var patchDir = new TemporaryDirectory())
            using (var builder = new DirectoryPatchBuilder(new XdeltaPatchBuilder(XdeltaPatchSystemFactory.Preferred)))
            {
                await builder.CreatePatchAsync(oldDir.Path, newDir.Path, patchDir.Path);
                string instructionsString = File.ReadAllText(patchDir.Path + Path.DirectorySeparatorChar + "instructions.json");
                var instructions = JsonConvert.DeserializeObject<List<FilePatchInstruction>>(instructionsString);
                Assert.AreEqual(0, instructions.Count);
            }
        }

        [TestMethod]
        public async Task TestRemoved()
        {
            using (var oldDir = new TemporaryDirectory())
            using (var newDir = new TemporaryDirectory())
            using (var patchDir = new TemporaryDirectory())
            using (var builder = new DirectoryPatchBuilder(new XdeltaPatchBuilder(XdeltaPatchSystemFactory.Preferred)))
            {
                File.WriteAllText(Path.Combine(oldDir.Path, "a"), DummyDataA);

                await builder.CreatePatchAsync(oldDir.Path, newDir.Path, patchDir.Path);
                string instructionsString = File.ReadAllText(patchDir.Path + Path.DirectorySeparatorChar + "instructions.json");
                var instructions = JsonConvert.DeserializeObject<List<FilePatchInstruction>>(instructionsString);

                Assert.AreEqual(1, instructions.Count);
                Assert.AreEqual("a", instructions[0].Path);
                Assert.AreEqual(GetStringHash(DummyDataA), instructions[0].OldHash);
                Assert.AreEqual(null, instructions[0].NewHash);
                Assert.AreEqual(false, instructions[0].HasDelta);
            }
        }

        [TestMethod]
        public async Task TestAdded()
        {
            using (var oldDir = new TemporaryDirectory())
            using (var newDir = new TemporaryDirectory())
            using (var patchDir = new TemporaryDirectory())
            using (var builder = new DirectoryPatchBuilder(new XdeltaPatchBuilder(XdeltaPatchSystemFactory.Preferred)))
            {
                File.WriteAllText(Path.Combine(newDir.Path, "a"), DummyDataA);

                await builder.CreatePatchAsync(oldDir.Path, newDir.Path, patchDir.Path);
                string instructionsString = File.ReadAllText(patchDir.Path + Path.DirectorySeparatorChar + "instructions.json");
                var instructions = JsonConvert.DeserializeObject<List<FilePatchInstruction>>(instructionsString);

                Assert.AreEqual(1, instructions.Count);
                Assert.AreEqual("a", instructions[0].Path);
                Assert.AreEqual(null, instructions[0].OldHash);
                Assert.AreEqual(GetStringHash(DummyDataA), instructions[0].NewHash);
                Assert.AreEqual(false, instructions[0].HasDelta);
                Assert.IsTrue(File.Exists(patchDir.Path + Path.DirectorySeparatorChar + "full" + Path.DirectorySeparatorChar + instructions[0].NewHash));
            }
        }

        [TestMethod]
        public async Task TestChanged()
        {
            using (var oldDir = new TemporaryDirectory())
            using (var newDir = new TemporaryDirectory())
            using (var patchDir = new TemporaryDirectory())
            using (var builder = new DirectoryPatchBuilder(new XdeltaPatchBuilder(XdeltaPatchSystemFactory.Preferred)))
            {
                File.WriteAllText(Path.Combine(oldDir.Path, "a"), DummyDataA);
                File.WriteAllText(Path.Combine(newDir.Path, "a"), DummyDataB);

                await builder.CreatePatchAsync(oldDir.Path, newDir.Path, patchDir.Path);
                string instructionsString = File.ReadAllText(patchDir.Path + Path.DirectorySeparatorChar + "instructions.json");
                var instructions = JsonConvert.DeserializeObject<List<FilePatchInstruction>>(instructionsString);

                Assert.AreEqual(1, instructions.Count);
                Assert.AreEqual("a", instructions[0].Path);
                Assert.AreEqual(GetStringHash(DummyDataA), instructions[0].OldHash);
                Assert.AreEqual(GetStringHash(DummyDataB), instructions[0].NewHash);
                Assert.IsTrue(File.Exists(patchDir.Path + Path.DirectorySeparatorChar + "full" + Path.DirectorySeparatorChar + instructions[0].NewHash));
                Assert.AreEqual(instructions[0].HasDelta, File.Exists(patchDir.Path + Path.DirectorySeparatorChar + "delta" + Path.DirectorySeparatorChar + instructions[0].NewHash + "_from_" + instructions[0].OldHash));
            }
        }

        [TestMethod]
        public async Task TestUncompressible()
        {
            using (var oldDir = new TemporaryDirectory())
            using (var newDir = new TemporaryDirectory())
            using (var patchDir = new TemporaryDirectory())
            using (var builder = new DirectoryPatchBuilder(new XdeltaPatchBuilder(XdeltaPatchSystemFactory.Preferred)))
            {
                File.WriteAllText(Path.Combine(oldDir.Path, "a"), "0");
                File.WriteAllText(Path.Combine(newDir.Path, "a"), "1");

                await builder.CreatePatchAsync(oldDir.Path, newDir.Path, patchDir.Path);
                string instructionsString = File.ReadAllText(patchDir.Path + Path.DirectorySeparatorChar + "instructions.json");
                var instructions = JsonConvert.DeserializeObject<List<FilePatchInstruction>>(instructionsString);

                Assert.AreEqual(1, instructions.Count);
                Assert.AreEqual("a", instructions[0].Path);
                Assert.AreEqual(GetStringHash("0"), instructions[0].OldHash);
                Assert.AreEqual(GetStringHash("1"), instructions[0].NewHash);
                Assert.AreEqual(false, instructions[0].HasDelta);
                Assert.IsTrue(File.Exists(patchDir.Path + Path.DirectorySeparatorChar + "full" + Path.DirectorySeparatorChar + instructions[0].NewHash));
                Assert.IsFalse(File.Exists(patchDir.Path + Path.DirectorySeparatorChar + "delta" + Path.DirectorySeparatorChar + instructions[0].NewHash + "_from_" + instructions[0].OldHash));
            }
        }

        [TestMethod]
        public async Task TestUnchanged()
        {
            using (var oldDir = new TemporaryDirectory())
            using (var newDir = new TemporaryDirectory())
            using (var patchDir = new TemporaryDirectory())
            using (var builder = new DirectoryPatchBuilder(new XdeltaPatchBuilder(XdeltaPatchSystemFactory.Preferred)))
            {
                File.WriteAllText(Path.Combine(oldDir.Path, "a"), DummyDataA);
                File.WriteAllText(Path.Combine(newDir.Path, "a"), DummyDataA);

                await builder.CreatePatchAsync(oldDir.Path, newDir.Path, patchDir.Path);
                string instructionsString = File.ReadAllText(patchDir.Path + Path.DirectorySeparatorChar + "instructions.json");
                var instructions = JsonConvert.DeserializeObject<List<FilePatchInstruction>>(instructionsString);

                Assert.AreEqual(1, instructions.Count);
                Assert.AreEqual("a", instructions[0].Path);
                Assert.AreEqual(GetStringHash(DummyDataA), instructions[0].OldHash);
                Assert.AreEqual(GetStringHash(DummyDataA), instructions[0].NewHash);
                Assert.AreEqual(false, instructions[0].HasDelta);
                Assert.IsTrue(File.Exists(patchDir.Path + Path.DirectorySeparatorChar + "full" + Path.DirectorySeparatorChar + instructions[0].NewHash));
                Assert.IsFalse(File.Exists(patchDir.Path + Path.DirectorySeparatorChar + "delta" + Path.DirectorySeparatorChar + instructions[0].NewHash + "_from_" + instructions[0].OldHash));
            }
        }

        [TestMethod]
        public async Task TestIdenticalFiles() // Detects bug when attempting to overwrite a file with the same SHA1.
        {
            using (var oldDir = new TemporaryDirectory())
            using (var newDir = new TemporaryDirectory())
            using (var patchDir = new TemporaryDirectory())
            using (var builder = new DirectoryPatchBuilder(new XdeltaPatchBuilder(XdeltaPatchSystemFactory.Preferred)))
            {
                File.WriteAllText(Path.Combine(oldDir.Path, "a"), DummyDataA);
                File.WriteAllText(Path.Combine(oldDir.Path, "b"), DummyDataA);
                File.WriteAllText(Path.Combine(newDir.Path, "a"), DummyDataB);
                File.WriteAllText(Path.Combine(newDir.Path, "b"), DummyDataB);

                await builder.CreatePatchAsync(oldDir.Path, newDir.Path, patchDir.Path);
                string instructionsString = File.ReadAllText(patchDir.Path + Path.DirectorySeparatorChar + "instructions.json");
                var instructions = JsonConvert.DeserializeObject<List<FilePatchInstruction>>(instructionsString);

                Assert.AreEqual(2, instructions.Count);
                Assert.AreEqual("a", instructions[0].Path);
                Assert.AreEqual(GetStringHash(DummyDataA), instructions[0].OldHash);
                Assert.AreEqual(GetStringHash(DummyDataB), instructions[0].NewHash);
                Assert.IsTrue(File.Exists(patchDir.Path + Path.DirectorySeparatorChar + "full" + Path.DirectorySeparatorChar + instructions[0].NewHash));
                Assert.AreEqual(instructions[0].HasDelta, File.Exists(patchDir.Path + Path.DirectorySeparatorChar + "delta" + Path.DirectorySeparatorChar + instructions[0].NewHash + "_from_" + instructions[0].OldHash));
                Assert.AreEqual("b", instructions[1].Path);
                Assert.AreEqual(GetStringHash(DummyDataA), instructions[1].OldHash);
                Assert.AreEqual(GetStringHash(DummyDataB), instructions[1].NewHash);
                Assert.IsTrue(File.Exists(patchDir.Path + Path.DirectorySeparatorChar + "full" + Path.DirectorySeparatorChar + instructions[1].NewHash));
                Assert.AreEqual(instructions[1].HasDelta, File.Exists(patchDir.Path + Path.DirectorySeparatorChar + "delta" + Path.DirectorySeparatorChar + instructions[1].NewHash + "_from_" + instructions[1].OldHash));
            }
        }
    }
}
