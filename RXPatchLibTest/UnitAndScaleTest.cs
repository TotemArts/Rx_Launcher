using Microsoft.VisualStudio.TestTools.UnitTesting;
using RXPatchLib;

namespace RXPatchLibTest
{
    [TestClass]
    public class UnitAndScaleTest
    {
        [TestMethod]
        public void TestUnitAndScale()
        {
            TestFormatting((long)                       1, "1 B");
            TestFormatting((long)                       9, "9 B");
            TestFormatting((long)                      10, "10 B");
            TestFormatting((long)                      99, "99 B");
            TestFormatting((long)                     100, "100 B");
            TestFormatting((long)                     999, "999 B");
            TestFormatting((long)                    1000, "0.977 KiB");
            TestFormatting((long)                    1023, "0.999 KiB");
            TestFormatting((long)               1024*   1, "1.000 KiB");
            TestFormatting((long)               1024*   9, "9.000 KiB");
            TestFormatting((long)               1024*  10, "10.00 KiB");
            TestFormatting((long)               1024*  99, "99.00 KiB");
            TestFormatting((long)               1024* 100, "100.0 KiB");
            TestFormatting((long)               1024* 999, "999.0 KiB");
            TestFormatting((long)               1024*1000, "0.977 MiB");
            TestFormatting((long)               1024*1023, "0.999 MiB");
            TestFormatting((long)          1024*1024*   1, "1.000 MiB");
            TestFormatting((long)          1024*1024*   9, "9.000 MiB");
            TestFormatting((long)          1024*1024*  10, "10.00 MiB");
            TestFormatting((long)          1024*1024*  99, "99.00 MiB");
            TestFormatting((long)          1024*1024* 100, "100.0 MiB");
            TestFormatting((long)          1024*1024* 999, "999.0 MiB");
            TestFormatting((long)          1024*1024*1000, "0.977 GiB");
            TestFormatting((long)          1024*1024*1023, "0.999 GiB");
            TestFormatting((long)     1024*1024*1024*   1, "1.000 GiB");
            TestFormatting((long)     1024*1024*1024*   9, "9.000 GiB");
            TestFormatting((long)     1024*1024*1024*  10, "10.00 GiB");
            TestFormatting((long)     1024*1024*1024*  99, "99.00 GiB");
            TestFormatting((long)     1024*1024*1024* 100, "100.0 GiB");
            TestFormatting((long)     1024*1024*1024* 999, "999.0 GiB");
            TestFormatting((long)     1024*1024*1024*1000, "1000 GiB");
            TestFormatting((long)     1024*1024*1024*1023, "1023 GiB");
            TestFormatting((long)1024*1024*1024*1024*   1, "1024 GiB");
            TestFormatting((long)1024*1024*1024*1024*  10, "10240 GiB");
        }

        private void TestFormatting(long value, string expected)
        {
            var unitAndScale = UnitAndScale.GetPreferredByteFormat(value);
            string actual = unitAndScale.GetFormatted(value) + " " + unitAndScale.Unit;
            Assert.AreEqual(expected, actual);
        }
    }
}
