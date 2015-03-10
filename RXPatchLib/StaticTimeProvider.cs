using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RXPatchLib
{
    class StaticTimeProvider : ITimeProvider
    {
        DateTime _Now;
        public DateTime Now
        {
            get
            {
                return _Now;
            }
            set
            {
                _Now = value;
            }
        }

        public StaticTimeProvider(DateTime now)
        {
            _Now = now;
        }

        public Task Delay(TimeSpan timespan)
        {
            Now += timespan;
            return TaskExtensions.CompletedTask;
        }
    }
}
