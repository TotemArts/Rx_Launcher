using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RXPatchLib
{
    interface IFilePatchAction
    {
        long PatchSize { get; }
        Task Load();
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

        public Task Load()
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
        public long PatchSize { get; private set; }

        public DeltaPatchAction(DirectoryPatcher directoryPatcher, string subPath, string patchSubPath, long patchSize)
        {
            DirectoryPatcher = directoryPatcher;
            SubPath = subPath;
            PatchSubPath = patchSubPath;
            PatchSize = patchSize;
        }

        public Task Load()
        {
            return DirectoryPatcher.PatchSource.Load(PatchSubPath);
        }

        public async Task Execute()
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
        private DirectoryPatcher DirectoryPatcher;
        private string SubPath;
        private string PatchSubPath;
        private bool NeedsBackup;
        public long PatchSize { get; private set; }

        public FullReplaceAction(DirectoryPatcher directoryPatcher, string subPath, string patchSubPath, bool needsBackup, long patchSize)
        {
            DirectoryPatcher = directoryPatcher;
            SubPath = subPath;
            PatchSubPath = patchSubPath;
            NeedsBackup = needsBackup;
            PatchSize = patchSize;
        }

        public Task Load()
        {
            return DirectoryPatcher.PatchSource.Load(PatchSubPath);
        }

        public Task Execute()
        {
            string tempPath = DirectoryPatcher.GetTempPath(SubPath);
            string newPath = DirectoryPatcher.PatchSource.GetSystemPath(PatchSubPath);
            string targetPath = DirectoryPatcher.GetTargetPath(SubPath);
            string backupPath = DirectoryPatcher.GetBackupPath(SubPath);
            Directory.CreateDirectory(Path.GetDirectoryName(tempPath));
            File.Copy(newPath, tempPath); // Copy to a temp location, so that after copying, swapping the old and new file is a quick operation (i.e. not likely to cause inconsistency when interrupted).
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
            return TaskExtensions.CompletedTask;
        }
    }

    public class DirectoryPatcher
    {
        class LoadPhase
        {
            DirectoryPatchPhaseProgress Progress = new DirectoryPatchPhaseProgress();
            List<Task> Tasks = new List<Task>();
            Action<DirectoryPatchPhaseProgress> ProgressCallback;

            public LoadPhase(Action<DirectoryPatchPhaseProgress> progressCallback)
            {
                ProgressCallback = progressCallback;
                Progress.State = DirectoryPatchPhaseProgress.States.Indeterminate;
                ProgressCallback(Progress);
            }

            public async Task StartLoading(IFilePatchAction action)
            {
                var task = action.Load();
                Tasks.Add(task);
                Progress.AddItem(action.PatchSize);
                ProgressCallback(Progress);
                await task;
                Progress.AdvanceItem(action.PatchSize);
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

        internal async Task Analyze(Action<IFilePatchAction> callback, Action<DirectoryPatchPhaseProgress> progressCallback)
        {
            await PatchSource.Load("instructions.json");
            string headerFileContents;
            using (var file = File.Open(PatchSource.GetSystemPath("instructions.json"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var streamReader = new StreamReader(file, Encoding.UTF8))
            {
                headerFileContents = streamReader.ReadToEnd();
            }
            List<FilePatchInstruction> instructions = JsonConvert.DeserializeObject<List<FilePatchInstruction>>(headerFileContents);

            var progress = new DirectoryPatchPhaseProgress();
            progress.SetTotals(instructions.Count, (from i in instructions select i.OldSize).Sum()); // Using the actual file size here would be better, but this is a fast and easy approximation.
            progress.State = DirectoryPatchPhaseProgress.States.Started;
            progressCallback(progress);

            foreach (var instruction in instructions)
            {
                string targetFilePath = Path.Combine(TargetPath, instruction.Path);
                string userHash = await SHA1.GetFileHashAsync(targetFilePath);
                IFilePatchAction action = BuildFilePatchAction(instruction, userHash);
                if (action != null)
                {
                    callback(action);
                }
                progress.AdvanceItem(instruction.OldSize);
                progressCallback(progress);
            }

            progress.State = DirectoryPatchPhaseProgress.States.Finished;
            progressCallback(progress);
        }

        private IFilePatchAction BuildFilePatchAction(FilePatchInstruction instruction, string userHash)
        {
            bool isOld = userHash == instruction.OldHash;
            bool isNew = userHash == instruction.NewHash;
            IFilePatchAction action = null;
            if (!isNew)
            {
                bool needsBackup = !isOld && userHash != null;
                if (instruction.NewHash == null)
                    action = new RemoveAction(this, instruction.Path, needsBackup);
                else if (isOld && instruction.HasDelta)
                {
                    string deltaFileName = Path.Combine("delta", instruction.NewHash + "_from_" + instruction.OldHash);
                    action = new DeltaPatchAction(this, instruction.Path, deltaFileName, instruction.DeltaSize);
                }
                else
                {
                    string fullFileName = Path.Combine("full", instruction.NewHash);
                    action = new FullReplaceAction(this, instruction.Path, fullFileName, needsBackup, instruction.NewSize);
                }
            }
            return action;
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

        public async Task ApplyPatchAsync(IProgress<DirectoryPatcherProgressReport> progressCallback)
        {
            var actions = new List<IFilePatchAction>();
            var progress = new DirectoryPatcherProgressReport();
            Action<Action> reportProgress = (phaseAction) => {
                phaseAction();
                progressCallback.Report(ObjectEx.DeepClone(progress));
            };
            var loadPhase = new LoadPhase(phaseProgress => reportProgress(() => progress.Load = phaseProgress));

            await Analyze(action =>
            {
                Task ignoreWarning = loadPhase.StartLoading(action);
                actions.Add(action);
            }, phaseProgress => reportProgress(() => progress.Analyze = phaseProgress));

            await loadPhase.AwaitAllTasksAndFinish();

            await Apply(actions, phaseProgress => reportProgress(() => progress.Apply = phaseProgress));
        }
    }
}
