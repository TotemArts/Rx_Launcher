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

        public static UnitAndScale GetPreferredByteFormat(long value, string minScale = null, string maxScale = null)
        {
            string[] scaleNames = { "B", "KiB", "MiB", "GiB" };
            int scaleIndex;
            long scaleDiv = 1;
            bool allowedMinScale = (minScale == null);
            for (scaleIndex = 0; scaleIndex < scaleNames.Length-1; ++scaleIndex)
            {
                string scaleName = scaleNames[scaleIndex];
                if (!allowedMinScale && scaleName == minScale)
                    allowedMinScale = true;
                if (allowedMinScale && value < 1000 * scaleDiv || scaleName == maxScale)
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
