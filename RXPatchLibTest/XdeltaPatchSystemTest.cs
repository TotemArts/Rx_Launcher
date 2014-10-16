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
            var ps = new XdeltaPatchSystem();
            await ps.RunCommandAsync("test");
        }

        [TestMethod]
        public async Task FailedExitCodeTest()
        {
            var ps = new XdeltaPatchSystem();
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
                var ps = new XdeltaPatchSystem();
                await ps.RunCommandAsync(file.Path);
            }
        }
    }
}
