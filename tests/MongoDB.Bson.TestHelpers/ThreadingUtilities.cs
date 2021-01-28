using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace MongoDB.Bson.TestHelpers
{
    public static class ThreadingUtilities
    {
        public static void ExecuteOnNewThread(int threadsCount, Action<int> action, int timeoutMilliseconds = 100000)
        {
            var exceptions = new ConcurrentBag<Exception>();

            var threads = Enumerable.Range(0, threadsCount).Select(i =>
            {
                var thread = new Thread(_ =>
                {
                    try
                    {
                        action(i);
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
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

            if (exceptions.Any())
            {
                throw exceptions.First();
            }
        }
    }
}
