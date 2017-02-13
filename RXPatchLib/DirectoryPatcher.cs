using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RXPatchLib
{
    interface IFilePatchAction
    {
        long PatchSize { get; }
        Task Load(CancellationToken cancellationToken, Action<long, long> progressCallback);
        Task Execute();
    }

    class RemoveAction : IFilePatchAction
    {
        private DirectoryPatcher DirectoryPatcher;
        private string SubPath;
        private bool NeedsBackup;
        public long PatchSize { get { return 0; } }

        public RemoveAction(DirectoryPatcher directoryPatcher, string subPath, bool needsBackup)
        {
            DirectoryPatcher = directoryPatcher;
            SubPath = subPath;
            NeedsBackup = needsBackup;
        }

        public Task Load(CancellationToken cancellationToken, Action<long, long> progressCallback)
        {
            return TaskExtensions.CompletedTask;
        }

        public Task Execute()
        {
            string targetPath = DirectoryPatcher.GetTargetPath(SubPath);
            string backupPath = DirectoryPatcher.GetBackupPath(SubPath);
            if (File.Exists(targetPath))
            {
                if (NeedsBackup)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(backupPath));
                    File.Move(targetPath, backupPath);
                }
                else
                {
                    File.Delete(targetPath);
                }
            }
            return TaskExtensions.CompletedTask;
        }
    }

    class DeltaPatchAction : IFilePatchAction
    {
        private DirectoryPatcher DirectoryPatcher;
        private string SubPath;
        private string PatchSubPath;
        private string Hash;
        public long PatchSize { get; private set; }

        public DeltaPatchAction(DirectoryPatcher directoryPatcher, string subPath, string patchSubPath, long patchSize, string hash)
        {
            DirectoryPatcher = directoryPatcher;
            SubPath = subPath;
            PatchSubPath = patchSubPath;
            Hash = hash;
            PatchSize = patchSize;
        }

        public Task Load(CancellationToken cancellationToken, Action<long, long> progressCallback)
        {
            return DirectoryPatcher.PatchSource.Load(PatchSubPath, Hash, cancellationToken, progressCallback);
        }

        public async Task Execute()
        {
            string tempPath = DirectoryPatcher.GetTempPath(SubPath);
            string targetPath = DirectoryPatcher.GetTargetPath(SubPath);
            string patchPath = DirectoryPatcher.PatchSource.GetSystemPath(PatchSubPath);

            // Ensure the temp directory exists, and generate the new file based on the old file and the delta
            Directory.CreateDirectory(Path.GetDirectoryName(tempPath));
            await DirectoryPatcher.FilePatcher.ApplyPatchAsync(targetPath, tempPath, patchPath);

            // Delete the old file and move the new one into place
            File.Delete(targetPath);
            File.Move(tempPath, targetPath);
        }
    }

    class FullReplaceAction : IFilePatchAction
    {
        private DirectoryPatcher DirectoryPatcher;
        private string SubPath;
        private string PatchSubPath;
        private bool NeedsBackup;
        private string Hash;
        public long PatchSize { get; private set; }

        public FullReplaceAction(DirectoryPatcher directoryPatcher, string subPath, string patchSubPath, bool needsBackup, long patchSize, string hash)
        {
            DirectoryPatcher = directoryPatcher;
            SubPath = subPath;
            PatchSubPath = patchSubPath;
            NeedsBackup = needsBackup;
            Hash = hash;
            PatchSize = patchSize;
        }

        public Task Load(CancellationToken cancellationToken, Action<long, long> progressCallback)
        {
            return DirectoryPatcher.PatchSource.Load(PatchSubPath, Hash, cancellationToken, progressCallback);
        }

        public async Task Execute()
        {
            string tempPath = DirectoryPatcher.GetTempPath(SubPath);
            string newPath = DirectoryPatcher.PatchSource.GetSystemPath(PatchSubPath);
            string targetPath = DirectoryPatcher.GetTargetPath(SubPath);
            string backupPath = DirectoryPatcher.GetBackupPath(SubPath);

            // Ensure the temp directory exists, and decompress the file
            Directory.CreateDirectory(Path.GetDirectoryName(tempPath));
            await DirectoryPatcher.FilePatcher.DecompressAsync(tempPath, newPath); // Extract to a temp location, so that after copying, swapping the old and new file is a quick operation (i.e. not likely to cause inconsistency when interrupted). Copying is also necessary because the file may be shared (moving is not allowed).

            // Get the old file out of the way, if it exists
            if (File.Exists(targetPath))
            {
                if (NeedsBackup)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(backupPath));
                    File.Move(targetPath, backupPath);
                }
                else
                {
                    File.Delete(targetPath);
                }
            }

            // Move the new file into place
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
            File.Move(tempPath, targetPath);
        }
    }

    public class ModifiedTimeReplaceAction : IFilePatchAction
    {
        DirectoryPatcher DirectoryPatcher;
        string SubPath;
        DateTime LastWriteTime;

        public long PatchSize
        {
            get { return 0; }
        }

        public ModifiedTimeReplaceAction(DirectoryPatcher directoryPatcher, string subPath, DateTime lastWriteTime)
        {
            DirectoryPatcher = directoryPatcher;
            SubPath = subPath;
            LastWriteTime = lastWriteTime;
        }

        public Task Load(CancellationToken cancellationToken, Action<long, long> progressCallback)
        {
            return TaskExtensions.CompletedTask;
        }

        public Task Execute()
        {
            string targetPath = DirectoryPatcher.GetTargetPath(SubPath);
            new FileInfo(targetPath).LastWriteTimeUtc = LastWriteTime;
            return TaskExtensions.CompletedTask;
        }
    }

    public class DirectoryPatcher
    {
        class LoadPhase
        {
            DirectoryPatchPhaseProgress Progress = new DirectoryPatchPhaseProgress();
            List<Task> Tasks = new List<Task>();
            CancellationToken CancellationToken;
            Action<DirectoryPatchPhaseProgress> ProgressCallback;

            public LoadPhase(CancellationToken cancellationToken, Action<DirectoryPatchPhaseProgress> progressCallback)
            {
                CancellationToken = cancellationToken;
                ProgressCallback = progressCallback;
                Progress.State = DirectoryPatchPhaseProgress.States.Indeterminate;
                ProgressCallback(Progress);
            }

            public async void StartLoading(IFilePatchAction action)
            {
                var progressItem = Progress.AddItem();
                progressItem.Total = action.PatchSize;
                var task = action.Load(CancellationToken, (done, total) => {
                    Debug.Assert(total == progressItem.Total);
                    progressItem.Done = done;
                    ProgressCallback(Progress);
                });
                Tasks.Add(task);
                await task;
                progressItem.Finish();
                ProgressCallback(Progress);
            }

            public async Task AwaitAllTasksAndFinish()
            {
                Progress.State = DirectoryPatchPhaseProgress.States.Started;
                await Task.WhenAll(Tasks);
                Progress.State = DirectoryPatchPhaseProgress.States.Finished;
                ProgressCallback(Progress);
            }
        }

        internal XdeltaPatcher FilePatcher;
        internal IPatchSource PatchSource;
        string TargetPath;
        string BackupPath;
        string TempPath;

        public string GetTargetPath(string subPath) { return Path.Combine(TargetPath, subPath); }
        public string GetBackupPath(string subPath) { return Path.Combine(BackupPath, subPath); }
        public string GetTempPath(string subPath) { return Path.Combine(TempPath, subPath); }

        public DirectoryPatcher(XdeltaPatcher filePatcher, string targetPath, string backupPath, string tempPath, IPatchSource patchSource)
        {
            FilePatcher = filePatcher;
            TargetPath = targetPath;
            BackupPath = backupPath;
            TempPath = tempPath;
            PatchSource = patchSource;
        }

        internal async Task Analyze(CancellationToken cancellationToken, Action<IFilePatchAction> callback, Action<DirectoryPatchPhaseProgress> progressCallback, string instructions_hash)
        {
            await PatchSource.Load("instructions.json", instructions_hash, cancellationToken, (done, total) => {});
            string headerFileContents;
            using (var file = File.Open(PatchSource.GetSystemPath("instructions.json"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var streamReader = new StreamReader(file, Encoding.UTF8))
            {
                headerFileContents = streamReader.ReadToEnd();
            }
            List<FilePatchInstruction> instructions = JsonConvert.DeserializeObject<List<FilePatchInstruction>>(headerFileContents);

            var progress = new DirectoryPatchPhaseProgress();
            var paths = instructions.Select(i => Path.Combine(TargetPath, i.Path));
            var sizes = paths.Select(p => !File.Exists(p) ? 0 : new FileInfo(p).Length);
            progress.SetTotals(instructions.Count, sizes.Sum());
            progress.State = DirectoryPatchPhaseProgress.States.Started;
            progressCallback(progress);

            foreach (var pair in instructions.Zip(sizes, (i, s) => new { Instruction = i, Size = s }))
            {
                var instruction = pair.Instruction;
                var size = pair.Size;
                cancellationToken.ThrowIfCancellationRequested();
                string targetFilePath = Path.Combine(TargetPath, instruction.Path);
                await BuildFilePatchAction(instruction, targetFilePath, callback);
                progress.AdvanceItem(pair.Size);
                progressCallback(progress);
            }

            progress.State = DirectoryPatchPhaseProgress.States.Finished;
            progressCallback(progress);
        }

        private async Task BuildFilePatchAction(FilePatchInstruction instruction, string targetFilePath, Action<IFilePatchAction> callback)
        {
            string installedHash = await SHA1.GetFileHashAsync(targetFilePath);
            bool isOld = installedHash == instruction.OldHash;

            // Patch file only if it is different from the new version
            if (installedHash != instruction.NewHash)
            {
                // Backup any existing files that don't match the old hash
                bool needsBackup = !isOld && installedHash != null;

                if (instruction.NewHash == null)
                {
                    // File deleted
                    callback(new RemoveAction(this, instruction.Path, needsBackup));
                }
                else if (isOld && instruction.HasDelta)
                {
                    // Incremental update
                    string deltaFileName = Path.Combine("delta", instruction.NewHash + "_from_" + instruction.OldHash);
                    callback(new DeltaPatchAction(this, instruction.Path, deltaFileName, instruction.DeltaSize, instruction.DeltaHash));
                }
                else
                {
                    // Full download
                    string fullFileName = Path.Combine("full", instruction.NewHash);
                    callback(new FullReplaceAction(this, instruction.Path, fullFileName, needsBackup, instruction.FullReplaceSize, instruction.CompressedHash));
                }
            }

            // Update LastWriteTime
            if (instruction.NewHash != null)
            {
                callback(new ModifiedTimeReplaceAction(this, instruction.Path, instruction.NewLastWriteTime));
            }
        }

        private static async Task Apply(List<IFilePatchAction> actions, Action<DirectoryPatchPhaseProgress> progressCallback)
        {
            var progress = new DirectoryPatchPhaseProgress();
            progress.SetTotals(actions.Count, (from a in actions select a.PatchSize).Sum());
            progress.State = DirectoryPatchPhaseProgress.States.Started;
            progressCallback(progress);

            foreach (var action in actions)
            {
                await action.Execute();
                progress.AdvanceItem(action.PatchSize);
                progressCallback(progress);
            }

            progress.State = DirectoryPatchPhaseProgress.States.Finished;
            progressCallback(progress);
        }

        public async Task ApplyPatchAsync(IProgress<DirectoryPatcherProgressReport> progressCallback, CancellationToken cancellationToken, string instructions_hash)
        {
            var actions = new List<IFilePatchAction>();
            var progress = new DirectoryPatcherProgressReport();
            Action<Action> reportProgress = (phaseAction) => {
                phaseAction();
                progressCallback.Report(ObjectEx.DeepClone(progress));
            };
            var loadPhase = new LoadPhase(cancellationToken, phaseProgress => reportProgress(() => progress.Load = phaseProgress));

            await Analyze(cancellationToken, action =>
            {
                loadPhase.StartLoading(action);
                actions.Add(action);
            }, phaseProgress => reportProgress(() => progress.Analyze = phaseProgress), instructions_hash);

            await loadPhase.AwaitAllTasksAndFinish();

            await Apply(actions, phaseProgress => reportProgress(() => progress.Apply = phaseProgress));
        }
    }
}
