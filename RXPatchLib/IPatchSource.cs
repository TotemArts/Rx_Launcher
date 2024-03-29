﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace RXPatchLib
{
    public interface IPatchSource
    {
        string GetSystemPath(string subPath);
        Task LoadAsync(string subPath, string hash, CancellationToken cancellationToken, Action<long, long, byte> progressCallback);
    }
}
