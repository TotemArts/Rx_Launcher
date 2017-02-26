using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RXPatchLib;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text;
using System.Threading;

namespace RXPatchLibTest
{
    [TestClass]
    public class DirectoryPatcherTest
    {
        static TemporaryDirectory PatchDir;
        static string[] PatchDirFiles;
        DateTime DummyLastWriteTime = new DateTime(2015, 1, 2, 3, 4, 5, 678);
        TemporaryDirectory TargetDir;
        TemporaryDirectory BackupDir;
        TemporaryDirectory TempDir;
        XdeltaPatcher FilePatcher;
        FileSystemPatchSource PatchSource;
        DirectoryPatcher DirectoryPatcher;


        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            PatchDir = new TemporaryDirectory();

            using (var oldFile = new TemporaryFile())
            using (var newDeltaFile = new TemporaryFile())
            using (var newFullFile = new TemporaryFile())
            {
                File.WriteAllText(oldFile.Path, "old");
                File.WriteAllText(newDeltaFile.Path, "new_delta");
                File.WriteAllText(newFullFile.Path, "new_full");
                string oldHash = SHA256.Get(Encoding.UTF8.GetBytes("old"));
                string newDeltaHash = SHA256.Get(Encoding.UTF8.GetBytes("new_delta"));
                string newFullHash = SHA256.Get(Encoding.UTF8.GetBytes("new_full"));

                Directory.CreateDirectory(Path.Combine(PatchDir.Path, "delta"));
                Directory.CreateDirectory(Path.Combine(PatchDir.Path, "full"));
                var patchBuilder = new XdeltaPatchBuilder(XdeltaPatchSystemFactory.Preferred);
                patchBuilder.CreatePatchAsync(oldFile.Path, newDeltaFile.Path, Path.Combine(PatchDir.Path, "delta/" + newDeltaHash + "_from_" + oldHash)).Wait();
                patchBuilder.CompressAsync(newFullFile.Path, Path.Combine(PatchDir.Path, "full/" + newFullHash)).Wait();
                PatchDirFiles = DirectoryPathIterator.GetChildPathsRecursive(PatchDir.Path).ToArray();
            }
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            PatchDir.Dispose();
        }

        [TestInitialize]
        public void Initialize()
        {
            TargetDir = new TemporaryDirectory();
            BackupDir = new TemporaryDirectory();
            TempDir = new TemporaryDirectory();
            FilePatcher = new XdeltaPatcher(XdeltaPatchSystemFactory.Preferred);
            PatchSource = new FileSystemPatchSource(PatchDir.Path);
            DirectoryPatcher = new DirectoryPatcher(FilePatcher, TargetDir.Path, BackupDir.Path, TempDir.Path, PatchSource);
        }

        [TestCleanup]
        public void Cleanup()
        {
            TargetDir.Dispose();
            BackupDir.Dispose();
            TempDir.Dispose();
        }

        private async Task ApplyPatchWithInstructions(IEnumerable<FilePatchInstruction> instructions)
        {
            var instructionsJson = JsonConvert.SerializeObject(instructions);
            var instructionsFilename = Path.Combine(PatchDir.Path, "instructions.json");
            File.WriteAllText(instructionsFilename, instructionsJson);

            try
            {
                await DirectoryPatcher.ApplyPatchAsync(TestProgressHandlerFactory.Create(), new CancellationToken(), await SHA256.GetFileHashAsync(instructionsFilename));
            }
            finally
            {
                File.Delete(instructionsFilename);
            }
        }

        private void TestInvariants()
        {
            CollectionAssert.AreEquivalent(new string[] { }, DirectoryPathIterator.GetChildPathsRecursive(TempDir.Path).ToArray());
            CollectionAssert.AreEquivalent(PatchDirFiles, DirectoryPathIterator.GetChildPathsRecursive(PatchDir.Path).ToArray());
            TestTargetFileDatesInvariant();
        }

        private void TestTargetFileDatesInvariant()
        {
            var files = DirectoryPathIterator.GetChildPathsRecursive(TargetDir.Path).ToArray();
            var offenders = files.Where(x => new FileInfo(Path.Combine(TargetDir.Path, x)).LastWriteTimeUtc != DummyLastWriteTime);
            CollectionAssert.AreEquivalent(new string[] { }, offenders.ToArray());
        }

        [TestMethod]
        public async Task TestApplyPatchEmpty()
        {
            var instructions = new FilePatchInstruction[] { };
            await ApplyPatchWithInstructions(instructions);
        }

        [TestMethod]
        public async Task TestApplyPatchRemoveOld()
        {
            File.WriteAllText(Path.Combine(TargetDir.Path, "file"), "old");
            var instructions = new FilePatchInstruction[] {
                new FilePatchInstruction()
                {
                    Path = "file",
                    OldHash = SHA256.Get(Encoding.UTF8.GetBytes("old")),
                    NewHash = null,
                    OldLastWriteTime = DummyLastWriteTime,
                    NewLastWriteTime = DummyLastWriteTime,
                    HasDelta = false,
                }
            };
            await ApplyPatchWithInstructions(instructions);
            CollectionAssert.AreEquivalent(new string[] { }, DirectoryPathIterator.GetChildPathsRecursive(TargetDir.Path).ToArray());
            CollectionAssert.AreEquivalent(new string[] { }, DirectoryPathIterator.GetChildPathsRecursive(BackupDir.Path).ToArray());
            TestInvariants();
        }

        [TestMethod]
        public async Task TestApplyPatchRemoveNew()
        {
            var instructions = new FilePatchInstruction[] {
                new FilePatchInstruction()
                {
                    Path = "file",
                    OldHash = SHA256.Get(Encoding.UTF8.GetBytes("old")),
                    NewHash = null,
                    OldLastWriteTime = DummyLastWriteTime,
                    NewLastWriteTime = DummyLastWriteTime,
                    HasDelta = false,
                }
            };
            await ApplyPatchWithInstructions(instructions);
            CollectionAssert.AreEquivalent(new string[] { }, DirectoryPathIterator.GetChildPathsRecursive(TargetDir.Path).ToArray());
            CollectionAssert.AreEquivalent(new string[] { }, DirectoryPathIterator.GetChildPathsRecursive(BackupDir.Path).ToArray());
            TestInvariants();
        }

        [TestMethod]
        public async Task TestApplyPatchRemoveModified()
        {
            File.WriteAllText(Path.Combine(TargetDir.Path, "file"), "modified");
            var instructions = new FilePatchInstruction[] {
                new FilePatchInstruction()
                {
                    Path = "file",
                    OldHash = SHA256.Get(Encoding.UTF8.GetBytes("old")),
                    NewHash = null,
                    OldLastWriteTime = DummyLastWriteTime,
                    NewLastWriteTime = DummyLastWriteTime,
                    HasDelta = false,
                }
            };
            await ApplyPatchWithInstructions(instructions);
            CollectionAssert.AreEquivalent(new string[] { }, DirectoryPathIterator.GetChildPathsRecursive(TargetDir.Path).ToArray());
            CollectionAssert.AreEquivalent(new string[] { "file" }, DirectoryPathIterator.GetChildPathsRecursive(BackupDir.Path).ToArray());
            TestInvariants();
            Assert.AreEqual("modified", File.ReadAllText(Path.Combine(BackupDir.Path, "file")));
        }

        [TestMethod]
        public async Task TestApplyPatchAddOld()
        {
            var instructions = new FilePatchInstruction[] {
                new FilePatchInstruction()
                {
                    Path = "file",
                    OldHash = null,
                    NewHash = SHA256.Get(Encoding.UTF8.GetBytes("new_full")),
                    OldLastWriteTime = DummyLastWriteTime,
                    NewLastWriteTime = DummyLastWriteTime,
                    HasDelta = false,
                }
            };
            await ApplyPatchWithInstructions(instructions);
            CollectionAssert.AreEquivalent(new string[] { "file" }, DirectoryPathIterator.GetChildPathsRecursive(TargetDir.Path).ToArray());
            CollectionAssert.AreEquivalent(new string[] { }, DirectoryPathIterator.GetChildPathsRecursive(BackupDir.Path).ToArray());
            TestInvariants();
            Assert.AreEqual("new_full", File.ReadAllText(Path.Combine(TargetDir.Path, "file")));
        }

        [TestMethod]
        public async Task TestApplyPatchAddNew()
        {
            File.WriteAllText(Path.Combine(TargetDir.Path, "file"), "new_nopatch");
            var instructions = new FilePatchInstruction[] {
                new FilePatchInstruction()
                {
                    Path = "file",
                    OldHash = null,
                    NewHash = SHA256.Get(Encoding.UTF8.GetBytes("new_nopatch")),
                    OldLastWriteTime = DummyLastWriteTime,
                    NewLastWriteTime = DummyLastWriteTime,
                    HasDelta = false,
                }
            };
            await ApplyPatchWithInstructions(instructions);
            CollectionAssert.AreEquivalent(new string[] { "file" }, DirectoryPathIterator.GetChildPathsRecursive(TargetDir.Path).ToArray());
            CollectionAssert.AreEquivalent(new string[] { }, DirectoryPathIterator.GetChildPathsRecursive(BackupDir.Path).ToArray());
            TestInvariants();
            Assert.AreEqual("new_nopatch", File.ReadAllText(Path.Combine(TargetDir.Path, "file")));
        }

        [TestMethod]
        public async Task TestApplyPatchAddModified()
        {
            File.WriteAllText(Path.Combine(TargetDir.Path, "file"), "modified");
            var instructions = new FilePatchInstruction[] {
                new FilePatchInstruction()
                {
                    Path = "file",
                    OldHash = null,
                    NewHash = SHA256.Get(Encoding.UTF8.GetBytes("new_full")),
                    OldLastWriteTime = DummyLastWriteTime,
                    NewLastWriteTime = DummyLastWriteTime,
                    HasDelta = false,
                }
            };
            await ApplyPatchWithInstructions(instructions);
            CollectionAssert.AreEquivalent(new string[] { "file" }, DirectoryPathIterator.GetChildPathsRecursive(TargetDir.Path).ToArray());
            CollectionAssert.AreEquivalent(new string[] { "file" }, DirectoryPathIterator.GetChildPathsRecursive(BackupDir.Path).ToArray());
            TestInvariants();
            Assert.AreEqual("new_full", File.ReadAllText(Path.Combine(TargetDir.Path, "file")));
            Assert.AreEqual("modified", File.ReadAllText(Path.Combine(BackupDir.Path, "file")));
        }

        [TestMethod]
        public async Task TestApplyPatchWithDeltaOld()
        {
            File.WriteAllText(Path.Combine(TargetDir.Path, "file"), "old");
            var instructions = new FilePatchInstruction[] {
                new FilePatchInstruction()
                {
                    Path = "file",
                    OldHash = SHA256.Get(Encoding.UTF8.GetBytes("old")),
                    NewHash = SHA256.Get(Encoding.UTF8.GetBytes("new_delta")),
                    OldLastWriteTime = DummyLastWriteTime,
                    NewLastWriteTime = DummyLastWriteTime,
                    HasDelta = true,
                }
            };
            await ApplyPatchWithInstructions(instructions);
            CollectionAssert.AreEquivalent(new string[] { "file" }, DirectoryPathIterator.GetChildPathsRecursive(TargetDir.Path).ToArray());
            CollectionAssert.AreEquivalent(new string[] { }, DirectoryPathIterator.GetChildPathsRecursive(BackupDir.Path).ToArray());
            TestInvariants();
            Assert.AreEqual("new_delta", File.ReadAllText(Path.Combine(TargetDir.Path, "file")));
        }

        [TestMethod]
        public async Task TestApplyPatchWithDeltaNew()
        {
            File.WriteAllText(Path.Combine(TargetDir.Path, "file"), "new_nopatch");
            var instructions = new FilePatchInstruction[] {
                new FilePatchInstruction()
                {
                    Path = "file",
                    OldHash = SHA256.Get(Encoding.UTF8.GetBytes("old")),
                    NewHash = SHA256.Get(Encoding.UTF8.GetBytes("new_nopatch")),
                    OldLastWriteTime = DummyLastWriteTime,
                    NewLastWriteTime = DummyLastWriteTime,
                    HasDelta = true,
                }
            };
            await ApplyPatchWithInstructions(instructions);
            CollectionAssert.AreEquivalent(new string[] { "file" }, DirectoryPathIterator.GetChildPathsRecursive(TargetDir.Path).ToArray());
            CollectionAssert.AreEquivalent(new string[] { }, DirectoryPathIterator.GetChildPathsRecursive(BackupDir.Path).ToArray());
            TestInvariants();
            Assert.AreEqual("new_nopatch", File.ReadAllText(Path.Combine(TargetDir.Path, "file")));
        }

        [TestMethod]
        public async Task TestApplyPatchWithDeltaModified()
        {
            File.WriteAllText(Path.Combine(TargetDir.Path, "file"), "modified");
            var instructions = new FilePatchInstruction[] {
                new FilePatchInstruction()
                {
                    Path = "file",
                    OldHash = SHA256.Get(Encoding.UTF8.GetBytes("old")),
                    NewHash = SHA256.Get(Encoding.UTF8.GetBytes("new_full")),
                    OldLastWriteTime = DummyLastWriteTime,
                    NewLastWriteTime = DummyLastWriteTime,
                    HasDelta = true,
                }
            };
            await ApplyPatchWithInstructions(instructions);
            CollectionAssert.AreEquivalent(new string[] { "file" }, DirectoryPathIterator.GetChildPathsRecursive(TargetDir.Path).ToArray());
            CollectionAssert.AreEquivalent(new string[] { "file" }, DirectoryPathIterator.GetChildPathsRecursive(BackupDir.Path).ToArray());
            TestInvariants();
            Assert.AreEqual("new_full", File.ReadAllText(Path.Combine(TargetDir.Path, "file")));
            Assert.AreEqual("modified", File.ReadAllText(Path.Combine(BackupDir.Path, "file")));
        }

        [TestMethod]
        public async Task TestApplyPatchWithDeltaRemoved()
        {
            var instructions = new FilePatchInstruction[] {
                new FilePatchInstruction()
                {
                    Path = "file",
                    OldHash = SHA256.Get(Encoding.UTF8.GetBytes("old")),
                    NewHash = SHA256.Get(Encoding.UTF8.GetBytes("new_full")),
                    OldLastWriteTime = DummyLastWriteTime,
                    NewLastWriteTime = DummyLastWriteTime,
                    HasDelta = true,
                }
            };
            await ApplyPatchWithInstructions(instructions);
            CollectionAssert.AreEquivalent(new string[] { "file" }, DirectoryPathIterator.GetChildPathsRecursive(TargetDir.Path).ToArray());
            CollectionAssert.AreEquivalent(new string[] { }, DirectoryPathIterator.GetChildPathsRecursive(BackupDir.Path).ToArray());
            TestInvariants();
            Assert.AreEqual("new_full", File.ReadAllText(Path.Combine(TargetDir.Path, "file")));
        }

        [TestMethod]
        public async Task TestApplyPatchWithoutDeltaOld()
        {
            File.WriteAllText(Path.Combine(TargetDir.Path, "file"), "old");
            var instructions = new FilePatchInstruction[] {
                new FilePatchInstruction()
                {
                    Path = "file",
                    OldHash = SHA256.Get(Encoding.UTF8.GetBytes("old")),
                    NewHash = SHA256.Get(Encoding.UTF8.GetBytes("new_full")),
                    OldLastWriteTime = DummyLastWriteTime,
                    NewLastWriteTime = DummyLastWriteTime,
                    HasDelta = false,
                }
            };
            await ApplyPatchWithInstructions(instructions);
            CollectionAssert.AreEquivalent(new string[] { "file" }, DirectoryPathIterator.GetChildPathsRecursive(TargetDir.Path).ToArray());
            CollectionAssert.AreEquivalent(new string[] { }, DirectoryPathIterator.GetChildPathsRecursive(BackupDir.Path).ToArray());
            TestInvariants();
            Assert.AreEqual("new_full", File.ReadAllText(Path.Combine(TargetDir.Path, "file")));
        }

        [TestMethod]
        public async Task TestApplyPatchWithoutDeltaNew()
        {
            File.WriteAllText(Path.Combine(TargetDir.Path, "file"), "new_nopatch");
            var instructions = new FilePatchInstruction[] {
                new FilePatchInstruction()
                {
                    Path = "file",
                    OldHash = SHA256.Get(Encoding.UTF8.GetBytes("old")),
                    NewHash = SHA256.Get(Encoding.UTF8.GetBytes("new_nopatch")),
                    OldLastWriteTime = DummyLastWriteTime,
                    NewLastWriteTime = DummyLastWriteTime,
                    HasDelta = false,
                }
            };
            await ApplyPatchWithInstructions(instructions);
            CollectionAssert.AreEquivalent(new string[] { "file" }, DirectoryPathIterator.GetChildPathsRecursive(TargetDir.Path).ToArray());
            CollectionAssert.AreEquivalent(new string[] { }, DirectoryPathIterator.GetChildPathsRecursive(BackupDir.Path).ToArray());
            TestInvariants();
            Assert.AreEqual("new_nopatch", File.ReadAllText(Path.Combine(TargetDir.Path, "file")));
        }

        [TestMethod]
        public async Task TestApplyPatchWithoutDeltaModified()
        {
            File.WriteAllText(Path.Combine(TargetDir.Path, "file"), "modified");
            var instructions = new FilePatchInstruction[] {
                new FilePatchInstruction()
                {
                    Path = "file",
                    OldHash = SHA256.Get(Encoding.UTF8.GetBytes("old")),
                    NewHash = SHA256.Get(Encoding.UTF8.GetBytes("new_full")),
                    OldLastWriteTime = DummyLastWriteTime,
                    NewLastWriteTime = DummyLastWriteTime,
                    HasDelta = false,
                }
            };
            await ApplyPatchWithInstructions(instructions);
            CollectionAssert.AreEquivalent(new string[] { "file" }, DirectoryPathIterator.GetChildPathsRecursive(TargetDir.Path).ToArray());
            CollectionAssert.AreEquivalent(new string[] { "file" }, DirectoryPathIterator.GetChildPathsRecursive(BackupDir.Path).ToArray());
            TestInvariants();
            Assert.AreEqual("new_full", File.ReadAllText(Path.Combine(TargetDir.Path, "file")));
            Assert.AreEqual("modified", File.ReadAllText(Path.Combine(BackupDir.Path, "file")));
        }

        [TestMethod]
        public async Task TestApplyPatchWithoutDeltaRemoved()
        {
            var instructions = new FilePatchInstruction[] {
                new FilePatchInstruction()
                {
                    Path = "file",
                    OldHash = SHA256.Get(Encoding.UTF8.GetBytes("old")),
                    NewHash = SHA256.Get(Encoding.UTF8.GetBytes("new_full")),
                    OldLastWriteTime = DummyLastWriteTime,
                    NewLastWriteTime = DummyLastWriteTime,
                    HasDelta = false,
                }
            };
            await ApplyPatchWithInstructions(instructions);
            CollectionAssert.AreEquivalent(new string[] { "file" }, DirectoryPathIterator.GetChildPathsRecursive(TargetDir.Path).ToArray());
            CollectionAssert.AreEquivalent(new string[] { }, DirectoryPathIterator.GetChildPathsRecursive(BackupDir.Path).ToArray());
            TestInvariants();
            Assert.AreEqual("new_full", File.ReadAllText(Path.Combine(TargetDir.Path, "file")));
        }
    }
}
