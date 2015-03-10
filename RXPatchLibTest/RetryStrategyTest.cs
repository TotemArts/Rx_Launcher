using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RXPatchLib;
using System.Threading.Tasks;
using System.Linq;

namespace RXPatchLibTest
{
    [TestClass]
    public class RetryStrategyTest
    {
        DateTime StartTime = new DateTime(2015, 1, 2, 3, 4, 5, 678);
        ITimeProvider TimeProvider;
        RetryStrategy RetryStrategy;
        int Attempts;

        [TestInitialize]
        public void Initialize()
        {
            TimeProvider = new StaticTimeProvider(StartTime);
            RetryStrategy = new RetryStrategy(TimeProvider);
            Attempts = 0;
        }

        [TestMethod]
        public void TestWithoutRetries()
        {
            RetryStrategy.Run(() => { ++Attempts; return Task.FromResult<Exception>(null); });
            Assert.AreEqual(1, Attempts);
        }

        [TestMethod]
        public void TestWithSingleRetry()
        {
            RunWithFailures(1, new TimeSpan(0, 0, 0));
            Assert.AreEqual(2, Attempts);
            Assert.AreEqual(new TimeSpan(0, 0, 1 * 4), TimeProvider.Now - StartTime);
        }

        [TestMethod]
        public void TestWithFewQuickRetries()
        {
            RunWithFailures(5, new TimeSpan(0, 0, 0));
            Assert.AreEqual(6, Attempts);
            Assert.AreEqual(new TimeSpan(0, 0, 5 * 4), TimeProvider.Now - StartTime);
        }

        [TestMethod]
        public void TestWithManyQuickRetries()
        {
            try
            {
                RunWithFailures(6, new TimeSpan(0, 0, 0));
                Assert.Fail("Expected exception.");
            }
            catch (TooManyRetriesException e)
            {
                Assert.AreEqual(6, Attempts);
                Assert.AreEqual(new TimeSpan(0, 0, 5 * 4), TimeProvider.Now - StartTime);
                CollectionAssert.AreEquivalent(new string[] {
                    "attempt 1",
                    "attempt 2",
                    "attempt 3",
                    "attempt 4",
                    "attempt 5",
                    "attempt 6",
                }, e.Exceptions.Select(_ => _.Message).ToArray());
            }
        }

        [TestMethod]
        public void TestWithManySlowRetries()
        {
            RunWithFailures(10, new TimeSpan(0, 0, 4));
            Assert.AreEqual(11, Attempts);
            Assert.AreEqual(new TimeSpan(0, 0, 10 * (4 + 4)), TimeProvider.Now - StartTime);
        }

        private void RunWithFailures(int failureCount, TimeSpan timeBeforeFailure)
        {
            try
            {
                RetryStrategy.Run(() =>
                {
                    ++Attempts;
                    if (Attempts == failureCount + 1)
                    {
                        return Task.FromResult<Exception>(null);
                    }
                    else
                    {
                        TimeProvider.Delay(timeBeforeFailure);
                        return Task.FromResult<Exception>(new Exception("attempt " + Attempts));
                    }
                }).Wait();
            }
            catch (AggregateException e)
            {
                throw e.InnerExceptions.Single();
            }
        }
    }
}
