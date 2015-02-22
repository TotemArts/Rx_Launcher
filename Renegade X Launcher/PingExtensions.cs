using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace LauncherTwo
{
    public static class PingExtensions
    {
        public static Task<PingReply> SendPingAsync(this Ping ping, string hostNameOrAddress, int timeout, CancellationToken token)
        {
            return ping.SendPingAsync(hostNameOrAddress, timeout)
                .WithCancellationToken(token /*, () => ping.SendAsyncCancel() */); // Cancelling is unfortunately very slow for unreachable servers. But for test cases, cancellation is necessary! TODO: Find a better solution.
        }
    }
}
