using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RxLogger;

namespace RXPatchLib
{
    interface IFilePatchAction
    {
        long PatchSize { get; }
        Task LoadAsync(CancellationToken cancellationToken, Action<long, long, byte> progressCallback);
        Task ExecuteAsync();
    }

    class RemoveAction : IFilePatchAction
    {
        private readonly DirectoryPatcher _directoryPatcher;
        private readonly string _subPath;
        private readonly bool _needsBackup;
        public long PatchSize => 0;

        public bool IsActive { get; set; }
        public bool IsComplete { get; set; }

        public RemoveAction(DirectoryPatcher directoryPatcher, string subPath, bool needsBackup)
        {
            _directoryPatcher = directoryPatcher;
            _subPath = subPath;
            _needsBackup = needsBackup;
        }

        public Task LoadAsync(CancellationToken cancellationToken, Action<long, long, byte> progressCallback)
        {
            return TaskExtensions.CompletedTask;
        }

        public Task ExecuteAsync()
        {
            string targetPath = _directoryPatcher.GetTargetPath(_subPath);
            string backupPath = _directoryPatcher.GetBackupPath(_subPath);
            
            if (File.Exists(targetPath))
            {
                if (_needsBackup)
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
        private readonly DirectoryPatcher DirectoryPatcher;
        private readonly string SubPath;
        private readonly string PatchSubPath;
        private readonly string _hash;
        public long PatchSize { get; private set; }
        public bool IsActive { get; set; }
        public bool IsComplete { get; set; }

        public DeltaPatchAction(DirectoryPatcher directoryPatcher, string subPath, string patchSubPath, long patchSize, string hash)
        {
            DirectoryPatcher = directoryPatcher;
            SubPath = subPath;
            PatchSubPath = patchSubPath;
            _hash = hash;
            PatchSize = patchSize;
        }

        public Task LoadAsync(CancellationToken cancellationToken, Action<long, long, byte> progressCallback)
        {
            return DirectoryPatcher.PatchSource.LoadAsync(PatchSubPath, _hash, cancellationToken, progressCallback);
        }

        public async Task ExecuteAsync()
        {
            string tempPath = DirectoryPatcher.GetTempPath(SubPath);
            string targetPath = DirectoryPatcher.GetTargetPath(SubPath);
            string patchPath = DirectoryPatcher.PatchSource.GetSystemPath(PatchSubPath);
            Directory.CreateDirectory(Path.GetDirectoryName(tempPath));
            await DirectoryPatcher.FilePatcher.ApplyPatchAsync(targetPath, tempPath, patchPath);
            File.Delete(targetPath);
            File.Move(tempPath, targetPath);
        }
    }

    class FullReplaceAction : IFilePatchAction
    {
        private readonly DirectoryPatcher DirectoryPatcher;
        private readonly string SubPath;
        private readonly string PatchSubPath;
        private readonly bool NeedsBackup;
        private readonly string _hash;
        public long PatchSize { get; private set; }
        public bool IsActive { get; set; }
        public bool IsComplete { get; set; }

        public FullReplaceAction(DirectoryPatcher directoryPatcher, string subPath, string patchSubPath, bool needsBackup, long patchSize, string hash)
        {
            DirectoryPatcher = directoryPatcher;
            SubPath = subPath;
            PatchSubPath = patchSubPath;
            NeedsBackup = needsBackup;
            _hash = hash;
            PatchSize = patchSize;
        }

        public Task LoadAsync(CancellationToken cancellationToken, Action<long, long, byte> progressCallback)
        {
            return DirectoryPatcher.PatchSource.LoadAsync(PatchSubPath, _hash, cancellationToken, progressCallback);
        }

        public async Task ExecuteAsync()
        {
            string tempPath = DirectoryPatcher.GetTempPath(SubPath);
            string newPath = DirectoryPatcher.PatchSource.GetSystemPath(PatchSubPath);
            string targetPath = DirectoryPatcher.GetTargetPath(SubPath);
            string backupPath = DirectoryPatcher.GetBackupPath(SubPath);
            Directory.CreateDirectory(Path.GetDirectoryName(tempPath));
            await DirectoryPatcher.FilePatcher.DecompressAsync(tempPath, newPath); // Extract to a temp location, so that after copying, swapping the old and new file is a quick operation (i.e. not likely to cause inconsistency when interrupted). Copying is also necessary because the file may be shared (moving is not allowed).
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
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
            File.Move(tempPath, targetPath);
        }
    }

    public class ModifiedTimeReplaceAction : IFilePatchAction
    {
        readonly DirectoryPatcher _directoryPatcher;
        readonly string _subPath;
        readonly DateTime _lastWriteTime;
        public bool IsActive { get; set; }
        public bool IsComplete { get; set; }

        public long PatchSize => 0;

        public ModifiedTimeReplaceAction(DirectoryPatcher directoryPatcher, string subPath, DateTime lastWriteTime)
        {
            _directoryPatcher = directoryPatcher;
            _subPath = subPath;
            _lastWriteTime = lastWriteTime;
        }

        public Task LoadAsync(CancellationToken cancellationToken, Action<long, long, byte> progressCallback)
        {
            return TaskExtensions.CompletedTask;
        }

        public Task ExecuteAsync()
        {
            string targetPath = _directoryPatcher.GetTargetPath(_subPath);
            new FileInfo(targetPath).LastWriteTimeUtc = _lastWriteTime;
            return TaskExtensions.CompletedTask;
        }
    }

    public class DirectoryPatcher
    {
        class LoadPhase
        {
            readonly DirectoryPatchPhaseProgress _progress = new DirectoryPatchPhaseProgress();
            readonly List<Task> _tasks = new List<Task>();
            readonly CancellationToken _cancellationToken;
            readonly Action<DirectoryPatchPhaseProgress> _progressCallback;

            public LoadPhase(CancellationToken cancellationToken, Action<DirectoryPatchPhaseProgress> progressCallback)
            {
                _cancellationToken = cancellationToken;
                _progressCallback = progressCallback;
                _progress.State = DirectoryPatchPhaseProgress.States.Indeterminate;
                _progressCallback(_progress);
            }

            public void StartLoading(IFilePatchAction action)
            {
                var progressItem = _progress.AddItem();
                progressItem.Total = action.PatchSize;

                var task = action.LoadAsync(_cancellationToken, (done, total, totalThreads) => {
                    Debug.Assert(total == progressItem.Total);
                    progressItem.Done = done;
                    _progress.DownloadThreads = totalThreads;
                    _progressCallback(_progress);
                }).ContinueWith(continuedTask =>
                {
                    progressItem.Finish();
                    _progressCallback(_progress);
                }, _cancellationToken);

                _tasks.Add(task);
            }

            public async Task AwaitAllTasksAndFinish()
            {
                _progress.State = DirectoryPatchPhaseProgress.States.Started;
                await Task.WhenAll(_tasks);
                _progress.State = DirectoryPatchPhaseProgress.States.Finished;
                _progressCallback(_progress);
            }
        }

        internal XdeltaPatcher FilePatcher;
        internal IPatchSource PatchSource;
        readonly string _targetPath;
        readonly string _backupPath;
        readonly string _tempPath;

        public string GetTargetPath(string subPath) { return Path.Combine(_targetPath, subPath); }
        public string GetBackupPath(string subPath) { return Path.Combine(_backupPath, subPath); }
        public string GetTempPath(string subPath) { return Path.Combine(_tempPath, subPath); }
        private const int MaximumDeltaThreads = 4;

        public DirectoryPatcher(XdeltaPatcher filePatcher, string targetPath, string backupPath, string tempPath, IPatchSource patchSource)
        {
            FilePatcher = filePatcher;
            _targetPath = targetPath;
            _backupPath = backupPath;
            _tempPath = tempPath;
            PatchSource = patchSource;
        }

        internal async Task AnalyzeAsync(CancellationToken cancellationToken, Action<IFilePatchAction> callback, Action<DirectoryPatchPhaseProgress> progressCallback, string instructions_hash)
        {
            await PatchSource.LoadAsync("instructions.json", instructions_hash, cancellationToken, (done, total, totalThreads) => { });
            string headerFileContents;
            using (var file = File.Open(PatchSource.GetSystemPath("instructions.json"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var streamReader = new StreamReader(file, Encoding.UTF8))
            {
                headerFileContents = streamReader.ReadToEnd();
            }
            List<FilePatchInstruction> instructions = JsonConvert.DeserializeObject<List<FilePatchInstruction>>(headerFileContents);

            var progress = new DirectoryPatchPhaseProgress();
            var paths = instructions.Select(i => Path.Combine(_targetPath, i.Path));
            var sizes = paths.Select(p => !File.Exists(p) ? 0 : new FileInfo(p).Length);
            progress.SetTotals(instructions.Count, sizes.Sum());
            progress.State = DirectoryPatchPhaseProgress.States.Started;
            progressCallback(progress);

            foreach (var pair in instructions.Zip(sizes, (i, s) => new { Instruction = i, Size = s }))
            {
                var instruction = pair.Instruction;
                var size = pair.Size;
                cancellationToken.ThrowIfCancellationRequested();
                string targetFilePath = Path.Combine(_targetPath, instruction.Path);
                await BuildFilePatchAction(instruction, targetFilePath, callback);
                progress.AdvanceItem(size);
                progressCallback(progress);
            }

            progress.State = DirectoryPatchPhaseProgress.States.Finished;
            progressCallback(progress);
        }

        private async Task BuildFilePatchAction(FilePatchInstruction instruction, string targetFilePath, Action<IFilePatchAction> callback)
        {
            string installedHash = await SHA256.GetFileHashAsync(targetFilePath);
            bool isOld = installedHash == instruction.OldHash;
            bool isNew = installedHash == instruction.NewHash;
            if (!isNew)
            {
                bool needsBackup = !isOld && installedHash != null;
                if (instruction.NewHash == null)
                {
                    callback(new RemoveAction(this, instruction.Path, needsBackup));
                }
                else if (isOld && instruction.HasDelta)
                {
                    string deltaFileName = Path.Combine("delta", instruction.NewHash + "_from_" + instruction.OldHash);
                    callback(new DeltaPatchAction(this, instruction.Path, deltaFileName, instruction.DeltaSize, instruction.DeltaHash));
                }
                else
                {
                    string fullFileName = Path.Combine("full", instruction.NewHash);
                    callback(new FullReplaceAction(this, instruction.Path, fullFileName, needsBackup, instruction.FullReplaceSize, instruction.CompressedHash));
                }
            }

            if (instruction.NewHash != null)
            {
                callback(new ModifiedTimeReplaceAction(this, instruction.Path, instruction.NewLastWriteTime));
            }
        }

        private static async Task ApplyAsync(CancellationToken cancellationToken, List<IFilePatchAction> actions, Action<DirectoryPatchPhaseProgress> progressCallback)
        {
            var progress = new DirectoryPatchPhaseProgress();
            var secondPhaseProgress = new DirectoryPatchPhaseProgress();
            secondPhaseProgress.SetTotals(actions.Count(m => m.PatchSize == 0), actions.Count(m => m.PatchSize == 0));
            secondPhaseProgress.State = DirectoryPatchPhaseProgress.States.Started;
            progress.SetTotals(actions.Count, (from a in actions select a.PatchSize).Sum());
            progress.State = DirectoryPatchPhaseProgress.States.Started;
            progressCallback(progress);

            var list = new List<Task>();

            foreach (var action in actions)
            {
                list.Add(action.ExecuteAsync().ContinueWith(task =>
                {
                    progress.AdvanceItem(action.PatchSize);
                    progressCallback(progress);
                }, cancellationToken));
            }

            await Task.WhenAll(list);

            progress.State = DirectoryPatchPhaseProgress.States.Finished;
            progressCallback(progress);
        }

        public async Task ApplyPatchAsync(IProgress<DirectoryPatcherProgressReport> progressCallback, CancellationToken cancellationToken, string instructionsHash)
        {
            var actions = new List<IFilePatchAction>();
            var progress = new DirectoryPatcherProgressReport();
            Action<Action> reportProgress = (phaseAction) => {
                phaseAction();
                progressCallback.Report(ObjectEx.DeepClone(progress));
            };
            var loadPhase = new LoadPhase(cancellationToken, phaseProgress => reportProgress(() => progress.Load = phaseProgress));

            await AnalyzeAsync(cancellationToken, action =>
            {
                loadPhase.StartLoading(action);
                actions.Add(action);
            }, phaseProgress => reportProgress(() => progress.Analyze = phaseProgress), instructionsHash);

            await loadPhase.AwaitAllTasksAndFinish();

            await ApplyAsync(cancellationToken, actions, phaseProgress => reportProgress(() => progress.Apply = phaseProgress));
        }
    }
}
