using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Async
{
    internal static class AsyncBackgroundTask
    {
        public static async Task Start(Func<CancellationToken, Task> action, TimeSpan delay, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(action, "action");
            Ensure.IsInfiniteOrGreaterThanOrEqualToZero(delay, "delay");

            try
            {
                if(delay.Equals(Timeout.InfiniteTimeSpan))
                {
                    await action(cancellationToken).ConfigureAwait(false);
                    return;
                }

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await action(cancellationToken).ConfigureAwait(false);
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }
            catch(TaskCanceledException)
            { }
        }
    }
}