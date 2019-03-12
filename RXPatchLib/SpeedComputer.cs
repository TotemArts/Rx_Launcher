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

        private readonly Queue<Sample> _samples = new Queue<Sample>();
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        private Sample FirstSample
        {
            get
            {
                return _samples.Peek();
            }
        }
        private Sample _lastSample;
        private readonly long _maxSamples = 10;


        public long BytesPerSecond
        {
            get
            {
                if (_samples.Count < 2)
                {
                    return 0;
                }
                var firstSample = FirstSample;
                var lastSample = _lastSample;
                var dTime = lastSample.Time - firstSample.Time;
                var dValue = lastSample.Value - firstSample.Value;
                if (dTime <= 0)
                {
                    return 0;
                }
                long bps = (long)((double)dValue / ((double)dTime / 1000.0) + .5);
                return (bps > 0) ? bps : 0;
            }
        }

        public void AddSample(long value)
        {
            long time = _stopwatch.ElapsedMilliseconds;
            _lastSample = new Sample { Time = time, Value = value };
            _samples.Enqueue(_lastSample);
            if (_samples.Count > _maxSamples)
            {
                _samples.Dequeue();
            }
        }
    }

}
