using LauncherTwo;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LauncherTest
{
    [TestClass]
    public class UpdateServerSelectorTest
    {
        [TestMethod]
        public void TestIsFirstServerCloser()
        {
            int hostIndex = new UpdateServerSelector().SelectHostIndex(new string[] { "google.nl", "google.co.ca" }).Result;
            Assert.AreEqual(0, hostIndex);
        }

        [TestMethod]
        public void TestIsSecondServerCloser()
        {
            int hostIndex = new UpdateServerSelector().SelectHostIndex(new string[] { "google.co.ca", "google.nl" }).Result;
            Assert.AreEqual(1, hostIndex);
        }

        [TestMethod]
        public void TestIsFirstServerUnreachable()
        {
            // australia.gov.au does not accept pings.
            int hostIndex = new UpdateServerSelector().SelectHostIndex(new string[] { "australia.gov.au", "google.nl" }).Result;
            Assert.AreEqual(1, hostIndex);
        }

        [TestMethod]
        public void TestIsSecondServerUnreachable()
        {
            // australia.gov.au does not accept pings.
            int hostIndex = new UpdateServerSelector().SelectHostIndex(new string[] { "google.nl", "australia.gov.au" }).Result;
            Assert.AreEqual(0, hostIndex);
        }
    }
}
