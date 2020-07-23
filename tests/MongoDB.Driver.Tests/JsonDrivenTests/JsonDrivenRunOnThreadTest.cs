/* Copyright 2020-present MongoDB Inc.
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;

namespace MongoDB.Driver.Tests.JsonDrivenTests
{
    public sealed class JsonDrivenRunOnThreadTest : JsonDrivenWithThreadTest
    {
        private readonly JsonDrivenTestFactory _jsonDrivenTestFactory;
        private BsonDocument _operation;

        public JsonDrivenRunOnThreadTest(
            JsonDrivenTestsStateHolder stateHolder,
            IJsonDrivenTestRunner testRunner,
            Dictionary<string, object> objectMap,
            JsonDrivenTestFactory jsonDrivenTestFactory)
            : base(stateHolder, testRunner, objectMap)
        {
            _jsonDrivenTestFactory = jsonDrivenTestFactory;
        }

        protected override void CallMethod(CancellationToken cancellationToken)
        {
            AssignTask(() => SubTest(false));
        }

        protected override Task CallMethodAsync(CancellationToken cancellationToken)
        {
            AssignTask(() => SubTest(true));
            return Task.FromResult(true);
        }

        protected override void SetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "operation":
                    _operation = value.ToBsonDocument();
                    return;
            }

            base.SetArgument(name, value);
        }

        // private methods
        private void AssignTask(Action action)
        {
            if (_testState.Tasks.ContainsKey(_name))
            {
                var taskAction = _testState.Tasks[_name];
                if (taskAction != null)
                {
                    throw new Exception($"Task {_name} must not be processed.");
                }
                else
                {
                    _testState.Tasks[_name] = CreateTask(action);
                }
            }
            else
            {
                throw new ArgumentException($"Task {_name} must be started before usage.");
            }
        }

        private Task CreateTask(Action action)
        {
            return Task.Factory.StartNew(
                action,
                CancellationToken.None,
                TaskCreationOptions.None,
                new ThreadPerTaskScheduler());
        }

        private void SubTest(bool async)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(_operation, "name", "object", "arguments", "error");

            var receiver = _operation["object"].ToString();
            var name = _operation["name"].ToString();

            var test = _jsonDrivenTestFactory.CreateTest(receiver, name);
            test.Arrange(_operation);
            if (async)
            {
                test.ActAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                test.Act(CancellationToken.None);
            }
            test.Assert();
        }

        // nested types
        /// <summary>
        /// Originally this code was published here https://code.msdn.microsoft.com/Samples-for-Parallel-b4b76364/sourcecode?fileId=44488&pathId=2098696067
        /// We don't think this custom scheduler is necessary but we think it may have solved some test failures for reasons we don't understand
        /// so we are leaving it in for now with a TODO to either remove this custom scheduler or actually understand what is going on
        /// </summary>
        private class ThreadPerTaskScheduler : TaskScheduler
        {
            /// <summary>Gets the tasks currently scheduled to this scheduler.</summary> 
            /// <remarks>This will always return an empty enumerable, as tasks are launched as soon as they're queued.</remarks> 
            protected override IEnumerable<Task> GetScheduledTasks() { return Enumerable.Empty<Task>(); }

            /// <summary>Starts a new thread to process the provided task.</summary> 
            /// <param name="task">The task to be executed.</param> 
            protected override void QueueTask(Task task)
            {
                new Thread(() => TryExecuteTask(task)) { IsBackground = true }.Start();
            }

            /// <summary>Runs the provided task on the current thread.</summary> 
            /// <param name="task">The task to be executed.</param> 
            /// <param name="taskWasPreviouslyQueued">Ignored.</param> 
            /// <returns>Whether the task could be executed on the current thread.</returns> 
            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                return TryExecuteTask(task);
            }
        }
    }
}
