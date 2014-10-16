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
        Task Load();
        Task Execute();
    }

    class RemoveAction : IFilePatchAction
    {
        private DirectoryPatcher DirectoryPatcher;
        private string SubPath;
        private bool NeedsBackup;

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

        public DeltaPatchAction(DirectoryPatcher directoryPatcher, string subPath, string patchSubPath)
        {
            DirectoryPatcher = directoryPatcher;
            SubPath = subPath;
            PatchSubPath = patchSubPath;
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

        public FullReplaceAction(DirectoryPatcher directoryPatcher, string subPath, string patchSubPath, bool needsBackup)
        {
            DirectoryPatcher = directoryPatcher;
            SubPath = subPath;
            PatchSubPath = patchSubPath;
            NeedsBackup = needsBackup;
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

        internal async Task Analyze(Action<IFilePatchAction> callback)
        {
            await PatchSource.Load("instructions.json");
            string headerFileContents;
            using (var file = File.Open(PatchSource.GetSystemPath("instructions.json"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var streamReader = new StreamReader(file, Encoding.UTF8))
            {
                headerFileContents = streamReader.ReadToEnd();
            }
            List<FilePatchInstruction> instructions = JsonConvert.DeserializeObject<List<FilePatchInstruction>>(headerFileContents);

            foreach (var instruction in instructions)
            {
                string targetFilePath = Path.Combine(TargetPath, instruction.Path);
                string userHash = await SHA1.GetFileHashAsync(targetFilePath);
                IFilePatchAction action = BuildFilePatchAction(instruction, userHash);
                if (action != null)
                {
                    callback(action);
                }
            }
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
                    action = new DeltaPatchAction(this, instruction.Path, deltaFileName);
                }
                else
                {
                    string fullFileName = Path.Combine("full", instruction.NewHash);
                    action = new FullReplaceAction(this, instruction.Path, fullFileName, needsBackup);
                }
            }
            return action;
        }

        public async Task ApplyPatchAsync()
        {
            var actions = new List<IFilePatchAction>();
            var loaders = new List<Task>();
            await Analyze(action =>
            {
                loaders.Add(action.Load());
                actions.Add(action);
            });
            await Task.WhenAll(loaders);
            foreach (var action in actions)
            {
                await action.Execute();
            }
        }
    }
}
