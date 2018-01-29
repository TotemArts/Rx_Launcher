using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RXPatchLib
{
    class StaticTimeProvider : ITimeProvider
    {
        DateTime _now;
        public DateTime Now
        {
            get
            {
                return _now;
            }
            set
            {
                _now = value;
            }
        }

        public StaticTimeProvider(DateTime now)
        {
            _now = now;
        }

        public Task Delay(TimeSpan timespan)
        {
            Now += timespan;
            return TaskExtensions.CompletedTask;
        }
    }
}
