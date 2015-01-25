using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RXPatchLib
{
    public struct UnitAndScale
    {
        public string Unit;
        public double Scale;
        public int Decimals;

        public string GetFormatted(long value)
        {
            return (value * Scale).ToString("F" + Decimals);
        }

        public static UnitAndScale GetPreferredByteFormat(long value)
        {
            string[] scaleNames = { "B", "KiB", "MiB", "GiB" };
            int scaleIndex;
            long scaleDiv = 1;
            for (scaleIndex = 0; scaleIndex < scaleNames.Length-1; ++scaleIndex)
            {
                if (value < 1000 * scaleDiv)
                {
                    break;
                }
                scaleDiv *= 1024;
            }
            long scaled = (value + scaleDiv / 2) / scaleDiv;
            int decimals =
                (scaleIndex == 0) ? 0 :
                (scaled < 10) ? 3 :
                (scaled < 100) ? 2 :
                (scaled < 1000) ? 1 :
                0;
            return new UnitAndScale { Unit = scaleNames[scaleIndex], Scale = 1.0 / (scaleDiv), Decimals = decimals };
        }
    }

}
