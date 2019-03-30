using System;

namespace RXPatchLib
{
    [Serializable]
    public class DiscreteProgress
    {
        public long Done { get; set; }
        public long Total { get; set; }

        public double Fraction
        {
            get
            {
                return (double)Done / (double)Total;
            }
        }
    }

}
