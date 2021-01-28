using System;
using System.Linq;
using System.Threading;

namespace MongoDB.Bson.TestHelpers
{
    public static class ThreadingUtilities
    {
        public static void ExecuteOnNewThread(int threadsCount, Action<int> action, int timeoutMilliseconds = 10000)
        {
            var threads = Enumerable.Range(0, threadsCount).Select(i =>
            {
                var thread = new Thread(_ => action(i));
                thread.Start();

                return thread;
            }).ToArray();


            foreach (var thread in threads)
            {
                if (!thread.Join(timeoutMilliseconds))
                {
                    throw new TimeoutException();
                }
            }
        }
    }
}
