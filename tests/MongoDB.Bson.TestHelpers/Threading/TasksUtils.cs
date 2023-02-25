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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Bson.TestHelpers
{
    public static class TasksUtils
    {
        public static Task CreateTaskOnOwnThread(Action action, CancellationToken cancellationToken = default) =>
            Task.Factory.StartNew(
                action,
                cancellationToken,
                TaskCreationOptions.None,
                new ThreadPerTaskScheduler());

        public static Task<T> CreateTaskOnOwnThread<T>(Func<T> function, CancellationToken cancellationToken = default) =>
            Task.Factory.StartNew(
                function,
                cancellationToken,
                TaskCreationOptions.None,
                new ThreadPerTaskScheduler());

        public static Task<T>[] CreateTasksOnOwnThread<T>(int count, Func<int, T> function, CancellationToken cancellationToken = default) =>
            Enumerable.Range(0, count)
            .Select(i => Task.Factory.StartNew(
                () => function(i),
                cancellationToken,
                TaskCreationOptions.None,
                new ThreadPerTaskScheduler()))
            .ToArray();

        public static Task<T>[] CreateTasks<T>(int count, Func<int, Task<T>> taskCreator) =>
            Enumerable.Range(0, count)
            .Select(i => taskCreator(i))
            .ToArray();

        public static void WaitOrThrow(this Task task, TimeSpan timeout)
        {
            if (!task.Wait(timeout))
            {
                throw new TimeoutException($"Task timed out after {timeout}.");
            }
        }

        public static Task WithTimeout(this Task task, TimeSpan timeout)
        {
            return WithTimeout(task, (int)timeout.TotalMilliseconds);
        }

        public static async Task WithTimeout(this Task task, int timeoutMS)
        {
            var firstFinishedTask = await Task.WhenAny(task, Task.Delay(timeoutMS));

            if (firstFinishedTask != task)
            {
                throw new TimeoutException($"Task timed out after {timeoutMS}ms.");
            }
        }
    }
}
