using RXPatchLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RXPatchLibTest
{
    [TestClass]
    public class UpdateServerSelectorTest
    {
        [TestMethod]
        public void TestIsFirstServerCloser()
        {
            int hostIndex = new UpdateServerSelector().SelectHostIndex(new System.Uri[] { new System.Uri("http://google.nl"), new System.Uri("http://google.co.ca") }).Result;
            Assert.AreEqual(0, hostIndex);
        }

        [TestMethod]
        public void TestIsSecondServerCloser()
        {
            int hostIndex = new UpdateServerSelector().SelectHostIndex(new System.Uri[] { new System.Uri("http://google.co.ca"), new System.Uri("http://google.nl") }).Result;
            Assert.AreEqual(1, hostIndex);
        }

        [TestMethod]
        public void TestIsFirstServerUnreachable()
        {
            // australia.gov.au does not accept pings.
            int hostIndex = new UpdateServerSelector().SelectHostIndex(new System.Uri[] { new System.Uri("http://australia.gov.au"), new System.Uri("http://google.nl") }).Result;
            Assert.AreEqual(1, hostIndex);
        }

        [TestMethod]
        public void TestIsSecondServerUnreachable()
        {
            // australia.gov.au does not accept pings.
            int hostIndex = new UpdateServerSelector().SelectHostIndex(new System.Uri[] { new System.Uri("http://google.nl"), new System.Uri("http://australia.gov.au") }).Result;
            Assert.AreEqual(0, hostIndex);
        }

        [TestMethod]
        public void TestSingleServer()
        {
            int hostIndex = new UpdateServerSelector().SelectHostIndex(new System.Uri[] { new System.Uri("http://google.nl") }).Result;
            Assert.AreEqual(0, hostIndex);
        }
    }
}
