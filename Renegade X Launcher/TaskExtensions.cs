using System;
using System.Threading;
using System.Threading.Tasks;

namespace LauncherTwo
{
    public static class TaskExtensions
    {
        public static async Task<T> WithCancellationToken<T>(this Task<T> antecedent, CancellationToken token, Action cancellationAction)
        {
            var tcs = new TaskCompletionSource<T>();
            using (token.Register(() => tcs.SetCanceled()))
            {
                if (await Task.WhenAny(antecedent, tcs.Task) == antecedent)
                {
                    return await antecedent;
                }
                else
                {
                    cancellationAction();
                    throw new OperationCanceledException(token);
                }
            }
        }

        public static Task<T> WithCancellationToken<T>(this Task<T> antecedent, CancellationToken token)
        {
            return WithCancellationToken(antecedent, token, () => { });
        }

        public static void Forget(this Task task)
        {
        }

        public static void Forget<T>(this Task<T> task)
        {
        }

        public static async Task ProceedAfter(this Task task, int milliseconds)
        {
            if (await Task.WhenAny(task, Task.Delay(milliseconds)) == task)
            {
                await task;
            }
        }

        public static async Task CancelAfter(this Task task, int milliseconds)
        {
            if (await Task.WhenAny(task, Task.Delay(milliseconds)) == task)
            {
                await task;
            }
            else
            {
                throw new OperationCanceledException();
            }
        }

        public static async Task ProceedIfCanceled(this Task task)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
            }
        }

        public static Task CompletedTask { get { return Task.FromResult<object>(null); } }
        public static Task CanceledTask { get { var tcs = new TaskCompletionSource<object>(); tcs.SetCanceled(); return tcs.Task; } }
    }
}
