using System;
using System.Threading;
using System.Threading.Tasks;

namespace RXPatchLib
{
    public interface IPatchSource
    {
        string GetSystemPath(string subPath);
        Task Load(string subPath, string hash, CancellationTokenSource cancellationTokenSource, Action<long, long, byte> progressCallback);
    }
}
