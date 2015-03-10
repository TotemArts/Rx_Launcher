using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RXPatchLib
{
    class SystemTimeProvider : ITimeProvider
    {
        public DateTime Now
        {
            get
            {
                return DateTime.UtcNow;
            }
        }
        public static SystemTimeProvider StaticInstance = new SystemTimeProvider();

        public Task Delay(TimeSpan timespan)
        {
            return Task.Delay(timespan);
        }
    }
}
