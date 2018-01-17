using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RXPatchLib
{
    interface IFilePatchAction
    {
        bool isActive { get; set; }
        bool isComplete { get; set; }
        long PatchSize { get; }
        Task Load(CancellationToken cancellationToken, Action<long, long> progressCallback);
        Task Execute();
    }

    class RemoveAction : IFilePatchAction
    {
        private DirectoryPatcher DirectoryPatcher;
        private string SubPath;
        private bool NeedsBackup;
        public long PatchSize => 0;

        public bool isActive { get; set; }
        public bool isComplete { get; set; }

        public RemoveAction(DirectoryPatcher directoryPatcher, string subPath, bool needsBackup)
        {
            DirectoryPatcher = directoryPatcher;
            SubPath = subPath;
            NeedsBackup = needsBackup;
        }

        public Task Load(CancellationToken cancellationToken, Action<long, long> progressCallback)
        {
            // Nothing to download; return CompletedTask
            return TaskExtensions.CompletedTask;
        }

        public Task Execute()
        {
            string targetPath = DirectoryPatcher.GetTargetPath(SubPath);
            string backupPath = DirectoryPatcher.GetBackupPath(SubPath);

            RxLogger.Logger.Instance.Write($"RemoveAction - {targetPath} - {backupPath}");

            // Deletes or moves the file to backupPath, if it exists
            if (File.Exists(targetPath))
            {
                if (NeedsBackup)
                {
                    // Backup file
                    Directory.CreateDirectory(Path.GetDirectoryName(backupPath));
                    File.Move(targetPath, backupPath);
                }
                else
                {
                    // Delete file
                    File.Delete(targetPath);
                }
            }

            // We're done; return CompletedTask
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
        public bool isActive { get; set; }
        public bool isComplete { get; set; }

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
            // Downloads delta; Hash is 'DeltaHash'
            return DirectoryPatcher.PatchSource.Load(PatchSubPath, Hash, cancellationToken, progressCallback);
        }

        public async Task Execute()
        {
            string tempPath = DirectoryPatcher.GetTempPath(SubPath);
            string targetPath = DirectoryPatcher.GetTargetPath(SubPath);
            string patchPath = DirectoryPatcher.PatchSource.GetSystemPath(PatchSubPath);

            RxLogger.Logger.Instance.Write($"DeltaPatchAction - {tempPath} - {targetPath} - {patchPath}");

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
        public bool isActive { get; set; }
        public bool isComplete { get; set; }

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
            // Downloads full; Hash is 'CompressedHash' since full versions of files are compressed
            return DirectoryPatcher.PatchSource.Load(PatchSubPath, Hash, cancellationToken, progressCallback);
        }

        public async Task Execute()
        {
            string tempPath = DirectoryPatcher.GetTempPath(SubPath);
            string newPath = DirectoryPatcher.PatchSource.GetSystemPath(PatchSubPath);
            string targetPath = DirectoryPatcher.GetTargetPath(SubPath);
            string backupPath = DirectoryPatcher.GetBackupPath(SubPath);

            RxLogger.Logger.Instance.Write($"FullReplaceAction - {tempPath} - {newPath} - {targetPath} - {backupPath}");

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
        public bool isActive { get; set; }
        public bool isComplete { get; set; }

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
            // Nothing to download; return CompletedTask
            return TaskExtensions.CompletedTask;
        }

        public Task Execute()
        {
            string targetPath = DirectoryPatcher.GetTargetPath(SubPath);

            RxLogger.Logger.Instance.Write($"ModifiedTimeReplaceAction - {targetPath}");

            // Update LastWriteTime
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
                // Initialize progress-related variables
                var progressItem = Progress.AddItem();
                progressItem.Total = action.PatchSize;

                // Starts loading an action
                var task = action.Load(CancellationToken, (done, total) => {
                    // Anonymous function to update progress
                    Debug.Assert(total == progressItem.Total);
                    progressItem.Done = done;
                    ProgressCallback(Progress);
                });

                // Add task to task list; used mostly by 'AwaitAllTasksAndFinish'
                Tasks.Add(task);

                // Wait for task to complete
                await task;

                // Update progress
                progressItem.Finish();
                ProgressCallback(Progress);
            }

            public async Task AwaitAllTasksAndFinish()
            {
                Progress.State = DirectoryPatchPhaseProgress.States.Started;

                // Wait until all actions have finished loading
                await Task.WhenAll(Tasks);

                // We're done here; update our State and update progress
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

        private const int MaximumDeltaThreads = 4;

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
            // Download instructions
            await PatchSource.Load("instructions.json", instructions_hash, cancellationToken, (done, total) => {});

            // Open downloaded instructions.json and copy its contents to headerFileContents
            string headerFileContents;
            using (var file = File.Open(PatchSource.GetSystemPath("instructions.json"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var streamReader = new StreamReader(file, Encoding.UTF8))
            {
                headerFileContents = streamReader.ReadToEnd();
            }

            // Deserialize JSON data from headerFileContents
            List<FilePatchInstruction> instructions = JsonConvert.DeserializeObject<List<FilePatchInstruction>>(headerFileContents);

            // Initialize progress-related variables
            var progress = new DirectoryPatchPhaseProgress();
            var paths = instructions.Select(i => Path.Combine(TargetPath, i.Path));
            var sizes = paths.Select(p => !File.Exists(p) ? 0 : new FileInfo(p).Length);
            progress.SetTotals(instructions.Count, sizes.Sum());
            progress.State = DirectoryPatchPhaseProgress.States.Started;
            progressCallback(progress);

            // Process each instruction in instructions.json
            foreach (var pair in instructions.Zip(sizes, (i, s) => new { Instruction = i, Size = s }))
            {
                var instruction = pair.Instruction;
                cancellationToken.ThrowIfCancellationRequested();
                string targetFilePath = Path.Combine(TargetPath, instruction.Path);

                // Determine action(s) to take based on instruction; any new actions get passed to the callback
                await BuildFilePatchAction(instruction, targetFilePath, callback);

                // Update progress
                progress.AdvanceItem(pair.Size);
                progressCallback(progress);
            }

            // We're done here; update our State and update progress
            progress.State = DirectoryPatchPhaseProgress.States.Finished;
            progressCallback(progress);
        }

        private async Task BuildFilePatchAction(FilePatchInstruction instruction, string targetFilePath, Action<IFilePatchAction> callback)
        {
            string installedHash = await SHA256.GetFileHashAsync(targetFilePath);
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
            List<IFilePatchAction> _tmpActions = actions.ToList(); // Create a copy of our list
            List<BackgroundWorker> _bgWorkers = new List<BackgroundWorker>();

            // Initialize progress-related variables
            var progress = new DirectoryPatchPhaseProgress();
            var secondPhaseProgress = new DirectoryPatchPhaseProgress();
            secondPhaseProgress.SetTotals(_tmpActions.Count(m => m.PatchSize == 0), _tmpActions.Count(m => m.PatchSize == 0));
            secondPhaseProgress.State = DirectoryPatchPhaseProgress.States.Started;
            progress.SetTotals(actions.Count, (from a in actions select a.PatchSize).Sum());
            progress.State = DirectoryPatchPhaseProgress.States.Started;
            progressCallback(progress);

            // Create our workers, but clamp the value to 4, we dont want to cook peoples PC's
            // (on testing with using all cores, it maxed out my 24 core server, but it did patch in about 30 seconds!)
            RxLogger.Logger.Instance.Write(
                $"Spawning Background Workers - Detected {Environment.ProcessorCount} processors, using {(Environment.ProcessorCount > 4 ? 4 : Environment.ProcessorCount)} of them");
            for (var i = 0; i < (Environment.ProcessorCount > MaximumDeltaThreads ? MaximumDeltaThreads : Environment.ProcessorCount); i++)
            {
                RxLogger.Logger.Instance.Write($"Spawning new background worker for task with an ID of {i}");
                _bgWorkers.Add(new BackgroundWorker());
            }

            foreach (var x in _bgWorkers)
            {
                RxLogger.Logger.Instance.Write($"Assigning DoWork methods to bgworker");
                x.DoWork += async (sender, args) =>
                {
                    // While there are still some in the array to use.
                    while (_tmpActions.Any(checker => !checker.isComplete) && !x.CancellationPending)
                    {
                        // Execute action
                        IFilePatchAction thisAction;

                        // Lock our tmpActions variable so we have unique access to it now
                        lock (_tmpActions)
                        {
                            if (_tmpActions.Any(k => !k.isComplete && !k.isActive && k.PatchSize > 0))
                            {
                                // If an action is not complete, not active and is above zero patch size
                                thisAction = _tmpActions.DefaultIfEmpty(null).FirstOrDefault(xx => !xx.isComplete && !xx.isActive && xx.PatchSize > 0);
                            }
                            else
                            {
                                // We have no actions that are above a patch size of zero, do we have any that are zero that still need doing?
                                if (_tmpActions.Any(l => !l.isComplete && !l.isActive && l.PatchSize == 0))
                                {
                                    thisAction = _tmpActions.DefaultIfEmpty(null).FirstOrDefault(xx => !xx.isComplete && !xx.isActive && xx.PatchSize == 0);
                                }
                                else
                                {
                                    // We're done, break out of our loop to close this thread
                                    break;
                                }
                            }

                            // Grab an action that is not complete, and not active and has a file size
                            
                            if (thisAction == null)
                                continue;

                            thisAction.isActive = true;
                        }

                        RxLogger.Logger.Instance.Write($"Starting action with file size of {thisAction.PatchSize}");
                        await thisAction.Execute();

                        // Patch Size will ONLY equal ZERO when the instruction is one that expects the file to be present
                        // An operation such as moving the file, renaming the file or setting it's metadata timestamp.
                        // We have to check for zero, to ensure that the asyncronus method of performing this action
                        // does not attempt to move/rename/etc a file before it has been actually downloaded.
                        if (thisAction.PatchSize == 0)
                        {
                            secondPhaseProgress.AdvanceItem(1);
                            progressCallback(secondPhaseProgress);
                        }
                        else
                        {
                            progress.AdvanceItem(thisAction.PatchSize);
                            progressCallback(progress);
                        }
                        
                        // Complete this action
                        lock (_tmpActions)
                        {
                            thisAction.isComplete = true;
                            thisAction.isActive = false;
                        }
                    }

                    RxLogger.Logger.Instance.Write("Background worker terminated");
                };

                x.RunWorkerAsync();
            }

            while (_tmpActions.Any(x => !x.isComplete))
            {
                await Task.Delay(1000);
            }

            // Dispose of all of our workers
            foreach (var b in _bgWorkers)
                b.Dispose();

=======
                };

                x.RunWorkerAsync();
            }

            while (_tmpActions.Any(x => !x.isComplete))
            {
                await Task.Delay(3000);
            }
        
>>>>>>> Applying xdelta stage is now multi-threaded
            // We're done here; update our State and update progress
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

            // Analyze files to determine which files to download and how to download them
            await Analyze(cancellationToken, action =>
            {
                loadPhase.StartLoading(action);
                actions.Add(action);
            }, phaseProgress => reportProgress(() => progress.Analyze = phaseProgress), instructions_hash);

            // Wait for downloads to finish
            await loadPhase.AwaitAllTasksAndFinish();

            // Apply the new files
            await Apply(actions, phaseProgress => reportProgress(() => progress.Apply = phaseProgress));
        }
    }
}
