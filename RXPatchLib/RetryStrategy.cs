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

        ITimeProvider TimeProvider;
        TimeSpan TestIntervalLength = new TimeSpan(0, 0, 40);
        int MaxExceptionsInInterval = 10;
        TimeSpan DelayAfterException = new TimeSpan(0, 0, 4);

        public RetryStrategy(ITimeProvider timeProvider = null)
        {
            TimeProvider = timeProvider ?? SystemTimeProvider.StaticInstance;
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

                var now = TimeProvider.Now;
                var intervalBegin = now.Subtract(TestIntervalLength);

                entries.Add(new Entry
                {
                    DateTime = now,
                    Exception = exception,
                });

                firstEntryInIntervalIndex = entries.FindIndex(firstEntryInIntervalIndex, entry => entry.DateTime.CompareTo(intervalBegin) > 0);
                Debug.Assert(firstEntryInIntervalIndex != -1);

                int exceptionsInInterval = entries.Count - firstEntryInIntervalIndex;
                if (exceptionsInInterval > MaxExceptionsInInterval)
                {
                    throw new TooManyRetriesException(entries.Select(_ => _.Exception));
                }

                await TimeProvider.Delay(DelayAfterException);
            }
        }
    }
}
