using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RXPatch
{
    static class ProgressReporter
    {
        public static async Task AwaitWithProgressReporting<T>(Func<Progress<T>, Task> createTask)
        {
            T lastReport = default(T);
            var progress = new Progress<T>(report => lastReport = report);
            var task = createTask(progress);
            while (await Task.WhenAny(task, Task.Delay(1000)) != task)
            {
                Console.WriteLine(lastReport == null ? "starting" : lastReport.ToString());
                Console.WriteLine();
            }
            Console.WriteLine(lastReport == null ? "starting" : lastReport.ToString());
            await task; // Collect exceptions.
        }
    }
}
