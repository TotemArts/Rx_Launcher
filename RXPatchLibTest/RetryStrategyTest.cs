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
        readonly DateTime _startTime = new DateTime(2015, 1, 2, 3, 4, 5, 678);
        ITimeProvider _timeProvider;
        RetryStrategy _retryStrategy;
        int _attempts;

        [TestInitialize]
        public void Initialize()
        {
            _timeProvider = new StaticTimeProvider(_startTime);
            _retryStrategy = new RetryStrategy(_timeProvider);
            _attempts = 0;
        }

        [TestMethod]
        public void TestWithoutRetries()
        {
            _retryStrategy.Run(() => { ++_attempts; return Task.FromResult<Exception>(null); });
            Assert.AreEqual(1, _attempts);
        }

        [TestMethod]
        public void TestWithSingleRetry()
        {
            RunWithFailures(1, new TimeSpan(0, 0, 0));
            Assert.AreEqual(2, _attempts);
            Assert.AreEqual(new TimeSpan(0, 0, 1 * 4), _timeProvider.Now - _startTime);
        }

        [TestMethod]
        public void TestWithFewQuickRetries()
        {
            RunWithFailures(5, new TimeSpan(0, 0, 0));
            Assert.AreEqual(6, _attempts);
            Assert.AreEqual(new TimeSpan(0, 0, 5 * 4), _timeProvider.Now - _startTime);
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
                Assert.AreEqual(6, _attempts);
                Assert.AreEqual(new TimeSpan(0, 0, 5 * 4), _timeProvider.Now - _startTime);
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
            Assert.AreEqual(11, _attempts);
            Assert.AreEqual(new TimeSpan(0, 0, 10 * (4 + 4)), _timeProvider.Now - _startTime);
        }

        private void RunWithFailures(int failureCount, TimeSpan timeBeforeFailure)
        {
            try
            {
                _retryStrategy.Run(() =>
                {
                    ++_attempts;
                    if (_attempts == failureCount + 1)
                    {
                        return Task.FromResult<Exception>(null);
                    }
                    else
                    {
                        _timeProvider.Delay(timeBeforeFailure);
                        return Task.FromResult<Exception>(new Exception("attempt " + _attempts));
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
