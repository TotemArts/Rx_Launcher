using System.Threading.Tasks;

namespace RXPatchLib
{
    static public class TaskExtensions
    {
        public static Task CompletedTask { get { return Task.FromResult(false); } }
    }
}
