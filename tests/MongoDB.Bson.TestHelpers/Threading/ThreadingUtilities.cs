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
using FluentAssertions;

namespace MongoDB.Bson.TestHelpers
{
    public static class ThreadingUtilities
    {
        public static void ExecuteOnNewThreads(int threadsCount, Action<int> action, int timeoutMilliseconds = 10000)
        {
            var actionsExecutedCount = 0;

            var exceptions = new ConcurrentBag<Exception>();

            var threads = Enumerable.Range(0, threadsCount).Select(i =>
            {
                var thread = new Thread(_ =>
                {
                    try
                    {
                        action(i);
                        Interlocked.Increment(ref actionsExecutedCount);
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

            actionsExecutedCount.Should().Be(threadsCount);
        }
    }
}
