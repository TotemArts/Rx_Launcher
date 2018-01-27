﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RXPatchLib
{
    public class RxPatchBuilder
    {
        public async Task CreatePatchAsync(PatchInfo patchInfo)
        {
            using (var builder = new DirectoryPatchBuilder(new XdeltaPatchBuilder(XdeltaPatchSystemFactory.Preferred)))
            {
                await builder.CreatePatchAsync(patchInfo.OldPath, patchInfo.NewPath, patchInfo.PatchPath);
            }
        }
    }
}
