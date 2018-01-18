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
        static TemporaryDirectory _patchDir;
        static string[] _patchDirFiles;
        readonly DateTime _dummyLastWriteTime = new DateTime(2015, 1, 2, 3, 4, 5, 678);
        TemporaryDirectory _targetDir;
        TemporaryDirectory _backupDir;
        TemporaryDirectory _tempDir;
        XdeltaPatcher _filePatcher;
        FileSystemPatchSource _patchSource;
        DirectoryPatcher _directoryPatcher;


        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            _patchDir = new TemporaryDirectory();

            using (var oldFile = new TemporaryFile())
            using (var newDeltaFile = new TemporaryFile())
            using (var newFullFile = new TemporaryFile())
            {
                File.WriteAllText(oldFile.Path, "old");
                File.WriteAllText(newDeltaFile.Path, "new_delta");
                File.WriteAllText(newFullFile.Path, "new_full");
                string oldHash = Sha256.Get(Encoding.UTF8.GetBytes("old"));
                string newDeltaHash = Sha256.Get(Encoding.UTF8.GetBytes("new_delta"));
                string newFullHash = Sha256.Get(Encoding.UTF8.GetBytes("new_full"));

                Directory.CreateDirectory(Path.Combine(_patchDir.Path, "delta"));
                Directory.CreateDirectory(Path.Combine(_patchDir.Path, "full"));
                var patchBuilder = new XdeltaPatchBuilder(XdeltaPatchSystemFactory.Preferred);
                patchBuilder.CreatePatchAsync(oldFile.Path, newDeltaFile.Path, Path.Combine(_patchDir.Path, "delta/" + newDeltaHash + "_from_" + oldHash)).Wait();
                patchBuilder.CompressAsync(newFullFile.Path, Path.Combine(_patchDir.Path, "full/" + newFullHash)).Wait();
                _patchDirFiles = DirectoryPathIterator.GetChildPathsRecursive(_patchDir.Path).ToArray();
            }
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            _patchDir.Dispose();
        }

        [TestInitialize]
        public void Initialize()
        {
            _targetDir = new TemporaryDirectory();
            _backupDir = new TemporaryDirectory();
            _tempDir = new TemporaryDirectory();
            _filePatcher = new XdeltaPatcher(XdeltaPatchSystemFactory.Preferred);
            _patchSource = new FileSystemPatchSource(_patchDir.Path);
            _directoryPatcher = new DirectoryPatcher(_filePatcher, _targetDir.Path, _backupDir.Path, _tempDir.Path, _patchSource);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _targetDir.Dispose();
            _backupDir.Dispose();
            _tempDir.Dispose();
        }

        private async Task ApplyPatchWithInstructions(IEnumerable<FilePatchInstruction> instructions)
        {
            var instructionsJson = JsonConvert.SerializeObject(instructions);
            var instructionsFilename = Path.Combine(_patchDir.Path, "instructions.json");
            File.WriteAllText(instructionsFilename, instructionsJson);

            try
            {
                await _directoryPatcher.ApplyPatchAsync(TestProgressHandlerFactory.Create(), new CancellationToken(), await Sha256.GetFileHashAsync(instructionsFilename));
            }
            finally
            {
                File.Delete(instructionsFilename);
            }
        }

        private void TestInvariants()
        {
            CollectionAssert.AreEquivalent(new string[] { }, DirectoryPathIterator.GetChildPathsRecursive(_tempDir.Path).ToArray());
            CollectionAssert.AreEquivalent(_patchDirFiles, DirectoryPathIterator.GetChildPathsRecursive(_patchDir.Path).ToArray());
            TestTargetFileDatesInvariant();
        }

        private void TestTargetFileDatesInvariant()
        {
            var files = DirectoryPathIterator.GetChildPathsRecursive(_targetDir.Path).ToArray();
            var offenders = files.Where(x => new FileInfo(Path.Combine(_targetDir.Path, x)).LastWriteTimeUtc != _dummyLastWriteTime);
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
            File.WriteAllText(Path.Combine(_targetDir.Path, "file"), "old");
            var instructions = new FilePatchInstruction[] {
                new FilePatchInstruction()
                {
                    Path = "file",
                    OldHash = Sha256.Get(Encoding.UTF8.GetBytes("old")),
                    NewHash = null,
                    OldLastWriteTime = _dummyLastWriteTime,
                    NewLastWriteTime = _dummyLastWriteTime,
                    HasDelta = false,
                }
            };
            await ApplyPatchWithInstructions(instructions);
            CollectionAssert.AreEquivalent(new string[] { }, DirectoryPathIterator.GetChildPathsRecursive(_targetDir.Path).ToArray());
            CollectionAssert.AreEquivalent(new string[] { }, DirectoryPathIterator.GetChildPathsRecursive(_backupDir.Path).ToArray());
            TestInvariants();
        }

        [TestMethod]
        public async Task TestApplyPatchRemoveNew()
        {
            var instructions = new FilePatchInstruction[] {
                new FilePatchInstruction()
                {
                    Path = "file",
                    OldHash = Sha256.Get(Encoding.UTF8.GetBytes("old")),
                    NewHash = null,
                    OldLastWriteTime = _dummyLastWriteTime,
                    NewLastWriteTime = _dummyLastWriteTime,
                    HasDelta = false,
                }
            };
            await ApplyPatchWithInstructions(instructions);
            CollectionAssert.AreEquivalent(new string[] { }, DirectoryPathIterator.GetChildPathsRecursive(_targetDir.Path).ToArray());
            CollectionAssert.AreEquivalent(new string[] { }, DirectoryPathIterator.GetChildPathsRecursive(_backupDir.Path).ToArray());
            TestInvariants();
        }

        [TestMethod]
        public async Task TestApplyPatchRemoveModified()
        {
            File.WriteAllText(Path.Combine(_targetDir.Path, "file"), "modified");
            var instructions = new FilePatchInstruction[] {
                new FilePatchInstruction()
                {
                    Path = "file",
                    OldHash = Sha256.Get(Encoding.UTF8.GetBytes("old")),
                    NewHash = null,
                    OldLastWriteTime = _dummyLastWriteTime,
                    NewLastWriteTime = _dummyLastWriteTime,
                    HasDelta = false,
                }
            };
            await ApplyPatchWithInstructions(instructions);
            CollectionAssert.AreEquivalent(new string[] { }, DirectoryPathIterator.GetChildPathsRecursive(_targetDir.Path).ToArray());
            CollectionAssert.AreEquivalent(new string[] { "file" }, DirectoryPathIterator.GetChildPathsRecursive(_backupDir.Path).ToArray());
            TestInvariants();
            Assert.AreEqual("modified", File.ReadAllText(Path.Combine(_backupDir.Path, "file")));
        }

        [TestMethod]
        public async Task TestApplyPatchAddOld()
        {
            var instructions = new FilePatchInstruction[] {
                new FilePatchInstruction()
                {
                    Path = "file",
                    OldHash = null,
                    NewHash = Sha256.Get(Encoding.UTF8.GetBytes("new_full")),
                    OldLastWriteTime = _dummyLastWriteTime,
                    NewLastWriteTime = _dummyLastWriteTime,
                    HasDelta = false,
                }
            };
            await ApplyPatchWithInstructions(instructions);
            CollectionAssert.AreEquivalent(new string[] { "file" }, DirectoryPathIterator.GetChildPathsRecursive(_targetDir.Path).ToArray());
            CollectionAssert.AreEquivalent(new string[] { }, DirectoryPathIterator.GetChildPathsRecursive(_backupDir.Path).ToArray());
            TestInvariants();
            Assert.AreEqual("new_full", File.ReadAllText(Path.Combine(_targetDir.Path, "file")));
        }

        [TestMethod]
        public async Task TestApplyPatchAddNew()
        {
            File.WriteAllText(Path.Combine(_targetDir.Path, "file"), "new_nopatch");
            var instructions = new FilePatchInstruction[] {
                new FilePatchInstruction()
                {
                    Path = "file",
                    OldHash = null,
                    NewHash = Sha256.Get(Encoding.UTF8.GetBytes("new_nopatch")),
                    OldLastWriteTime = _dummyLastWriteTime,
                    NewLastWriteTime = _dummyLastWriteTime,
                    HasDelta = false,
                }
            };
            await ApplyPatchWithInstructions(instructions);
            CollectionAssert.AreEquivalent(new string[] { "file" }, DirectoryPathIterator.GetChildPathsRecursive(_targetDir.Path).ToArray());
            CollectionAssert.AreEquivalent(new string[] { }, DirectoryPathIterator.GetChildPathsRecursive(_backupDir.Path).ToArray());
            TestInvariants();
            Assert.AreEqual("new_nopatch", File.ReadAllText(Path.Combine(_targetDir.Path, "file")));
        }

        [TestMethod]
        public async Task TestApplyPatchAddModified()
        {
            File.WriteAllText(Path.Combine(_targetDir.Path, "file"), "modified");
            var instructions = new FilePatchInstruction[] {
                new FilePatchInstruction()
                {
                    Path = "file",
                    OldHash = null,
                    NewHash = Sha256.Get(Encoding.UTF8.GetBytes("new_full")),
                    OldLastWriteTime = _dummyLastWriteTime,
                    NewLastWriteTime = _dummyLastWriteTime,
                    HasDelta = false,
                }
            };
            await ApplyPatchWithInstructions(instructions);
            CollectionAssert.AreEquivalent(new string[] { "file" }, DirectoryPathIterator.GetChildPathsRecursive(_targetDir.Path).ToArray());
            CollectionAssert.AreEquivalent(new string[] { "file" }, DirectoryPathIterator.GetChildPathsRecursive(_backupDir.Path).ToArray());
            TestInvariants();
            Assert.AreEqual("new_full", File.ReadAllText(Path.Combine(_targetDir.Path, "file")));
            Assert.AreEqual("modified", File.ReadAllText(Path.Combine(_backupDir.Path, "file")));
        }

        [TestMethod]
        public async Task TestApplyPatchWithDeltaOld()
        {
            File.WriteAllText(Path.Combine(_targetDir.Path, "file"), "old");
            var instructions = new FilePatchInstruction[] {
                new FilePatchInstruction()
                {
                    Path = "file",
                    OldHash = Sha256.Get(Encoding.UTF8.GetBytes("old")),
                    NewHash = Sha256.Get(Encoding.UTF8.GetBytes("new_delta")),
                    OldLastWriteTime = _dummyLastWriteTime,
                    NewLastWriteTime = _dummyLastWriteTime,
                    HasDelta = true,
                }
            };
            await ApplyPatchWithInstructions(instructions);
            CollectionAssert.AreEquivalent(new string[] { "file" }, DirectoryPathIterator.GetChildPathsRecursive(_targetDir.Path).ToArray());
            CollectionAssert.AreEquivalent(new string[] { }, DirectoryPathIterator.GetChildPathsRecursive(_backupDir.Path).ToArray());
            TestInvariants();
            Assert.AreEqual("new_delta", File.ReadAllText(Path.Combine(_targetDir.Path, "file")));
        }

        [TestMethod]
        public async Task TestApplyPatchWithDeltaNew()
        {
            File.WriteAllText(Path.Combine(_targetDir.Path, "file"), "new_nopatch");
            var instructions = new FilePatchInstruction[] {
                new FilePatchInstruction()
                {
                    Path = "file",
                    OldHash = Sha256.Get(Encoding.UTF8.GetBytes("old")),
                    NewHash = Sha256.Get(Encoding.UTF8.GetBytes("new_nopatch")),
                    OldLastWriteTime = _dummyLastWriteTime,
                    NewLastWriteTime = _dummyLastWriteTime,
                    HasDelta = true,
                }
            };
            await ApplyPatchWithInstructions(instructions);
            CollectionAssert.AreEquivalent(new string[] { "file" }, DirectoryPathIterator.GetChildPathsRecursive(_targetDir.Path).ToArray());
            CollectionAssert.AreEquivalent(new string[] { }, DirectoryPathIterator.GetChildPathsRecursive(_backupDir.Path).ToArray());
            TestInvariants();
            Assert.AreEqual("new_nopatch", File.ReadAllText(Path.Combine(_targetDir.Path, "file")));
        }

        [TestMethod]
        public async Task TestApplyPatchWithDeltaModified()
        {
            File.WriteAllText(Path.Combine(_targetDir.Path, "file"), "modified");
            var instructions = new FilePatchInstruction[] {
                new FilePatchInstruction()
                {
                    Path = "file",
                    OldHash = Sha256.Get(Encoding.UTF8.GetBytes("old")),
                    NewHash = Sha256.Get(Encoding.UTF8.GetBytes("new_full")),
                    OldLastWriteTime = _dummyLastWriteTime,
                    NewLastWriteTime = _dummyLastWriteTime,
                    HasDelta = true,
                }
            };
            await ApplyPatchWithInstructions(instructions);
            CollectionAssert.AreEquivalent(new string[] { "file" }, DirectoryPathIterator.GetChildPathsRecursive(_targetDir.Path).ToArray());
            CollectionAssert.AreEquivalent(new string[] { "file" }, DirectoryPathIterator.GetChildPathsRecursive(_backupDir.Path).ToArray());
            TestInvariants();
            Assert.AreEqual("new_full", File.ReadAllText(Path.Combine(_targetDir.Path, "file")));
            Assert.AreEqual("modified", File.ReadAllText(Path.Combine(_backupDir.Path, "file")));
        }

        [TestMethod]
        public async Task TestApplyPatchWithDeltaRemoved()
        {
            var instructions = new FilePatchInstruction[] {
                new FilePatchInstruction()
                {
                    Path = "file",
                    OldHash = Sha256.Get(Encoding.UTF8.GetBytes("old")),
                    NewHash = Sha256.Get(Encoding.UTF8.GetBytes("new_full")),
                    OldLastWriteTime = _dummyLastWriteTime,
                    NewLastWriteTime = _dummyLastWriteTime,
                    HasDelta = true,
                }
            };
            await ApplyPatchWithInstructions(instructions);
            CollectionAssert.AreEquivalent(new string[] { "file" }, DirectoryPathIterator.GetChildPathsRecursive(_targetDir.Path).ToArray());
            CollectionAssert.AreEquivalent(new string[] { }, DirectoryPathIterator.GetChildPathsRecursive(_backupDir.Path).ToArray());
            TestInvariants();
            Assert.AreEqual("new_full", File.ReadAllText(Path.Combine(_targetDir.Path, "file")));
        }

        [TestMethod]
        public async Task TestApplyPatchWithoutDeltaOld()
        {
            File.WriteAllText(Path.Combine(_targetDir.Path, "file"), "old");
            var instructions = new FilePatchInstruction[] {
                new FilePatchInstruction()
                {
                    Path = "file",
                    OldHash = Sha256.Get(Encoding.UTF8.GetBytes("old")),
                    NewHash = Sha256.Get(Encoding.UTF8.GetBytes("new_full")),
                    OldLastWriteTime = _dummyLastWriteTime,
                    NewLastWriteTime = _dummyLastWriteTime,
                    HasDelta = false,
                }
            };
            await ApplyPatchWithInstructions(instructions);
            CollectionAssert.AreEquivalent(new string[] { "file" }, DirectoryPathIterator.GetChildPathsRecursive(_targetDir.Path).ToArray());
            CollectionAssert.AreEquivalent(new string[] { }, DirectoryPathIterator.GetChildPathsRecursive(_backupDir.Path).ToArray());
            TestInvariants();
            Assert.AreEqual("new_full", File.ReadAllText(Path.Combine(_targetDir.Path, "file")));
        }

        [TestMethod]
        public async Task TestApplyPatchWithoutDeltaNew()
        {
            File.WriteAllText(Path.Combine(_targetDir.Path, "file"), "new_nopatch");
            var instructions = new FilePatchInstruction[] {
                new FilePatchInstruction()
                {
                    Path = "file",
                    OldHash = Sha256.Get(Encoding.UTF8.GetBytes("old")),
                    NewHash = Sha256.Get(Encoding.UTF8.GetBytes("new_nopatch")),
                    OldLastWriteTime = _dummyLastWriteTime,
                    NewLastWriteTime = _dummyLastWriteTime,
                    HasDelta = false,
                }
            };
            await ApplyPatchWithInstructions(instructions);
            CollectionAssert.AreEquivalent(new string[] { "file" }, DirectoryPathIterator.GetChildPathsRecursive(_targetDir.Path).ToArray());
            CollectionAssert.AreEquivalent(new string[] { }, DirectoryPathIterator.GetChildPathsRecursive(_backupDir.Path).ToArray());
            TestInvariants();
            Assert.AreEqual("new_nopatch", File.ReadAllText(Path.Combine(_targetDir.Path, "file")));
        }

        [TestMethod]
        public async Task TestApplyPatchWithoutDeltaModified()
        {
            File.WriteAllText(Path.Combine(_targetDir.Path, "file"), "modified");
            var instructions = new FilePatchInstruction[] {
                new FilePatchInstruction()
                {
                    Path = "file",
                    OldHash = Sha256.Get(Encoding.UTF8.GetBytes("old")),
                    NewHash = Sha256.Get(Encoding.UTF8.GetBytes("new_full")),
                    OldLastWriteTime = _dummyLastWriteTime,
                    NewLastWriteTime = _dummyLastWriteTime,
                    HasDelta = false,
                }
            };
            await ApplyPatchWithInstructions(instructions);
            CollectionAssert.AreEquivalent(new string[] { "file" }, DirectoryPathIterator.GetChildPathsRecursive(_targetDir.Path).ToArray());
            CollectionAssert.AreEquivalent(new string[] { "file" }, DirectoryPathIterator.GetChildPathsRecursive(_backupDir.Path).ToArray());
            TestInvariants();
            Assert.AreEqual("new_full", File.ReadAllText(Path.Combine(_targetDir.Path, "file")));
            Assert.AreEqual("modified", File.ReadAllText(Path.Combine(_backupDir.Path, "file")));
        }

        [TestMethod]
        public async Task TestApplyPatchWithoutDeltaRemoved()
        {
            var instructions = new FilePatchInstruction[] {
                new FilePatchInstruction()
                {
                    Path = "file",
                    OldHash = Sha256.Get(Encoding.UTF8.GetBytes("old")),
                    NewHash = Sha256.Get(Encoding.UTF8.GetBytes("new_full")),
                    OldLastWriteTime = _dummyLastWriteTime,
                    NewLastWriteTime = _dummyLastWriteTime,
                    HasDelta = false,
                }
            };
            await ApplyPatchWithInstructions(instructions);
            CollectionAssert.AreEquivalent(new string[] { "file" }, DirectoryPathIterator.GetChildPathsRecursive(_targetDir.Path).ToArray());
            CollectionAssert.AreEquivalent(new string[] { }, DirectoryPathIterator.GetChildPathsRecursive(_backupDir.Path).ToArray());
            TestInvariants();
            Assert.AreEqual("new_full", File.ReadAllText(Path.Combine(_targetDir.Path, "file")));
        }
    }
}
