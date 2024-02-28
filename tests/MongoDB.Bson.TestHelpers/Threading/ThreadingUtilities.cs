/* Copyright 2021-present MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Bson.TestHelpers
{
    public static class ThreadingUtilities
    {
        public static void ExecuteOnNewThreads(int threadsCount, Action<int> action, int timeoutMilliseconds = 10000)
        {
            var exceptions = ExecuteOnNewThreadsCollectExceptions(threadsCount, action, timeoutMilliseconds);

            if (exceptions.Any())
            {
                throw exceptions.First();
            }
        }

        public static Exception[] ExecuteOnNewThreadsCollectExceptions(int threadsCount, Action<int> action, int timeoutMilliseconds = 10000)
        {
            var exceptions = new ConcurrentBag<Exception>();
            var startEvent = new ManualResetEventSlim(false);

            var threads = Enumerable.Range(0, threadsCount).Select(i =>
            {
                var thread = new Thread(_ =>
                {
                    try
                    {
                        if (!startEvent.Wait(timeoutMilliseconds))
                        {
                            throw new TimeoutException();
                        }

                        action(i);
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                });

                thread.IsBackground = true;
                thread.Start();

                return thread;
            })
            .ToArray();

            startEvent.Set();

            foreach (var thread in threads)
            {
                if (!thread.Join(timeoutMilliseconds))
                {
                    throw new TimeoutException();
                }
            }

            return exceptions.ToArray();
        }

        public static async Task ExecuteTasksOnNewThreads(int threadsCount, Func<int, Task> action, int timeoutMilliseconds = 10000)
        {
            var exceptions = await ExecuteTasksOnNewThreadsCollectExceptions(threadsCount, action, timeoutMilliseconds);

            if (exceptions.Any())
            {
                throw exceptions.First();
            }
        }

        public static async Task<Exception[]> ExecuteTasksOnNewThreadsCollectExceptions(int threadsCount, Func<int, Task> action, int timeoutMilliseconds = 10000)
        {
            var exceptions = new ConcurrentBag<Exception>();
            var tasksExecutingCountEvent = new CountdownEvent(threadsCount);

            var allTasks = TasksUtils.RunTasksOnOwnThread(threadsCount, async i =>
            {
                try
                {
                    tasksExecutingCountEvent.Signal();
                    if (!tasksExecutingCountEvent.Wait(timeoutMilliseconds))
                    {
                        throw new TimeoutException();
                    }

                    await action(i);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            var taskAll = Task.WhenAll(allTasks);
            if (await Task.WhenAny(taskAll, Task.Delay(timeoutMilliseconds)) != taskAll)
            {
                exceptions.Add(new TimeoutException());
            }

            return exceptions.ToArray();
        }
    }
}
