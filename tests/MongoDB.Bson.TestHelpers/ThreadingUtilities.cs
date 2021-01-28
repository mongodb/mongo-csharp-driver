using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace MongoDB.Bson.TestHelpers
{
    public static class ThreadingUtilities
    {
        public static void ExecuteOnNewThread(int threadsCount, Action<int, Action<Action>> action, int timeoutMilliseconds = 100000)
        {
            var validations = new ConcurrentBag<Action>();

            var threads = Enumerable.Range(0, threadsCount).Select(i =>
            {
                var thread = new Thread(_ =>
                {
                     action(i, validations.Add);
                });

                thread.Start();

                return thread;
            })
            .ToArray();

            foreach (var thread in threads)
            {
                if (!thread.Join(timeoutMilliseconds))
                {
                    throw new TimeoutException();
                }
            }

            foreach (var v in validations)
            {
                v();
            }
        }
    }
}
