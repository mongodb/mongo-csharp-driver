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
        public static Task CreateTaskOnOwnThread(Action action) =>
            Task.Factory.StartNew(
                action,
                CancellationToken.None,
                TaskCreationOptions.None,
                new ThreadPerTaskScheduler());

        public static Task<T> CreateTaskOnOwnThread<T>(Func<T> function) =>
            Task.Factory.StartNew(
                function,
                CancellationToken.None,
                TaskCreationOptions.None,
                new ThreadPerTaskScheduler());

        public static Task<T>[] CreateTasksOnOwnThread<T>(int count, Func<int, T> function) =>
            Enumerable.Range(0, count)
            .Select(i => Task.Factory.StartNew(
                () => function(i),
                CancellationToken.None,
                TaskCreationOptions.None,
                new ThreadPerTaskScheduler()))
            .ToArray();

        public static Task<T>[] CreateTasks<T>(int count, Func<int, Task<T>> taskCreator) =>
            Enumerable.Range(0, count)
            .Select(i => taskCreator(i))
            .ToArray();
    }
}
