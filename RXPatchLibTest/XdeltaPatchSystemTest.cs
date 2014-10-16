using Microsoft.VisualStudio.TestTools.UnitTesting;
using RXPatchLib;
using System.Threading.Tasks;

namespace RXPatchLibTest
{
    [TestClass]
    public class XdeltaPatchSystemTest
    {
        [TestMethod]
        [Ignore]
        public async Task RunBuiltinTests()
        {
            var ps = XdeltaPatchSystemFactory.Preferred;
            await ps.RunCommandAsync("test");
        }

        [TestMethod]
        public async Task FailedExitCodeTest()
        {
            var ps = XdeltaPatchSystemFactory.Preferred;
            try
            {
                await ps.RunCommandAsync("--help");
                Assert.Fail();
            }
            catch (CommandExecutionException e)
            {
                Assert.AreEqual(1, e.ExitCode);
            }
        }

        [TestMethod]
        public async Task SuccessExitCodeTest()
        {
            using (var file = new TemporaryFile())
            {
                var ps = XdeltaPatchSystemFactory.Preferred;
                await ps.RunCommandAsync(file.Path);
            }
        }
    }
}
