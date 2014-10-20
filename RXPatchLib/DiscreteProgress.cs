using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RXPatchLib
{
    [Serializable]
    public struct DiscreteProgress
    {
        public long Done;
        public long Total;

        public double Fraction
        {
            get
            {
                return (double)Done / (double)Total;
            }
        }
    }

}
