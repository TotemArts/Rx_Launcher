using System.Collections.Generic;
using System.Diagnostics;

namespace RXPatchLib
{
    public class SpeedComputer
    {
        private struct Sample
        {
            public long Time; // In milliseconds
            public long Value;
        }

        private Queue<Sample> Samples = new Queue<Sample>();
        private Stopwatch Stopwatch = Stopwatch.StartNew();

        private Sample FirstSample
        {
            get
            {
                return Samples.Peek();
            }
        }
        private Sample LastSample;
        private long MaxSamples = 10;


        public long BytesPerSecond
        {
            get
            {
                if (Samples.Count < 2)
                {
                    return 0;
                }
                var firstSample = FirstSample;
                var lastSample = LastSample;
                var dTime = lastSample.Time - firstSample.Time;
                var dValue = lastSample.Value - firstSample.Value;
                if (dTime <= 0)
                {
                    return 0;
                }
                return (long)((double)dValue / ((double)dTime / 1000.0) + .5);
            }
        }

        public void AddSample(long value)
        {
            long time = Stopwatch.ElapsedMilliseconds;
            LastSample = new Sample { Time = time, Value = value };
            Samples.Enqueue(LastSample);
            if (Samples.Count > MaxSamples)
            {
                Samples.Dequeue();
            }
        }
    }

}
