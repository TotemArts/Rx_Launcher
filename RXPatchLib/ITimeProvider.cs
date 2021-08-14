using System;
using System.Threading.Tasks;

namespace RXPatchLib
{
    interface ITimeProvider
    {
        DateTime Now { get; }

        Task Delay(TimeSpan timespan);
    }
}
