using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RXPatchLib;

namespace RXPatchLibTest
{
    [TestClass]
    public class ProcessExTest
    {
        [TestMethod]
        public void TestEmpty()
        {
            var actual = ProcessEx.EscapeCommandLineArgument("");
            var expected = "\"\"";
            Assert.AreEqual(expected, actual);
        }
        [TestMethod]
        public void TestSimple()
        {
            var actual = ProcessEx.EscapeCommandLineArgument("test");
            var expected = "\"test\"";
            Assert.AreEqual(expected, actual);
        }
        [TestMethod]
        public void TestSingleQuote()
        {
            var actual = ProcessEx.EscapeCommandLineArgument("\"");
            var expected = "\"\\\"\"";
            Assert.AreEqual(expected, actual);
        }
        [TestMethod]
        public void TestDoubleQuote()
        {
            var actual = ProcessEx.EscapeCommandLineArgument("\"\"");
            var expected = "\"\\\"\\\"\"";
            Assert.AreEqual(expected, actual);
        }
        [TestMethod]
        public void TestTripleQuote()
        {
            var actual = ProcessEx.EscapeCommandLineArgument("\"\"\"");
            var expected = "\"\\\"\\\"\\\"\"";
            Assert.AreEqual(expected, actual);
        }
        [TestMethod]
        public void TestSingleSlash()
        {
            var actual = ProcessEx.EscapeCommandLineArgument("\\");
            var expected = "\"\\\\\"";
            Assert.AreEqual(expected, actual);
        }
        [TestMethod]
        public void TestDoubleSlash()
        {
            var actual = ProcessEx.EscapeCommandLineArgument("\\\\");
            var expected = "\"\\\\\\\\\"";
            Assert.AreEqual(expected, actual);
        }
        [TestMethod]
        public void TestTripleSlash()
        {
            var actual = ProcessEx.EscapeCommandLineArgument("\\\\\\");
            var expected = "\"\\\\\\\\\\\\\"";
            Assert.AreEqual(expected, actual);
        }
        [TestMethod]
        public void TestSingleSlashQuote()
        {
            var actual = ProcessEx.EscapeCommandLineArgument("\\\"");
            var expected = "\"\\\\\\\"\"";
            Assert.AreEqual(expected, actual);
        }
        [TestMethod]
        public void TestDoubleSlashQuote()
        {
            var actual = ProcessEx.EscapeCommandLineArgument("\\\\\"");
            var expected = "\"\\\\\\\\\\\"\"";
            Assert.AreEqual(expected, actual);
        }
        [TestMethod]
        public void TestTripleSlashQuote()
        {
            var actual = ProcessEx.EscapeCommandLineArgument("\\\\\\\"");
            var expected = "\"\\\\\\\\\\\\\\\"\"";
            Assert.AreEqual(expected, actual);
        }
        [TestMethod]
        public void TestSingleSlashNonQuote()
        {
            var actual = ProcessEx.EscapeCommandLineArgument("\\a");
            var expected = "\"\\a\"";
            Assert.AreEqual(expected, actual);
        }
        [TestMethod]
        public void TestDoubleSlashNonQuote()
        {
            var actual = ProcessEx.EscapeCommandLineArgument("\\\\a");
            var expected = "\"\\\\a\"";
            Assert.AreEqual(expected, actual);
        }
        [TestMethod]
        public void TestTripleSlashNonQuote()
        {
            var actual = ProcessEx.EscapeCommandLineArgument("\\\\\\a");
            var expected = "\"\\\\\\a\"";
            Assert.AreEqual(expected, actual);
        }
    }
}
