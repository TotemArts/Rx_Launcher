using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RXPatchLib
{
    class RetryStrategy
    {
        struct Entry
        {
            public DateTime DateTime;
            public Exception Exception;
        }

        readonly ITimeProvider _timeProvider;
        readonly TimeSpan _testIntervalLength = new TimeSpan(0, 0, 60);
        readonly int _maxExceptionsInInterval = 2;
        readonly TimeSpan _delayAfterException = new TimeSpan(0, 0, 2);

        public RetryStrategy(ITimeProvider timeProvider = null)
        {
            _timeProvider = timeProvider ?? SystemTimeProvider.StaticInstance;
        }

        public async Task Run(Func<Task<Exception>> function)
        {
            List<Entry> entries = new List<Entry>();
            int firstEntryInIntervalIndex = 0;

            for (; ; )
            {
                var exception = await function();
                if (exception == null)
                {
                    break;
                }

                var now = _timeProvider.Now;
                var intervalBegin = now.Subtract(_testIntervalLength);

                entries.Add(new Entry
                {
                    DateTime = now,
                    Exception = exception,
                });

                firstEntryInIntervalIndex = entries.FindIndex(firstEntryInIntervalIndex, entry => entry.DateTime.CompareTo(intervalBegin) > 0);
                Debug.Assert(firstEntryInIntervalIndex != -1);

                int exceptionsInInterval = entries.Count - firstEntryInIntervalIndex;
                if (exceptionsInInterval > _maxExceptionsInInterval)
                {
                    throw new TooManyRetriesException(entries.Select(_ => _.Exception));
                }

                await _timeProvider.Delay(_delayAfterException);
            }
        }
    }
}
