using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RXPatchLib
{
    interface ITimeProvider
    {
        DateTime Now { get; }

        Task Delay(TimeSpan timespan);
    }
}
