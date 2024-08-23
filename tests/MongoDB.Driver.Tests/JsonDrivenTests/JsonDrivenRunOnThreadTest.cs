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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;

namespace MongoDB.Driver.Tests.JsonDrivenTests
{
    internal sealed class JsonDrivenRunOnThreadTest : JsonDrivenWithThreadTest
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
                    // We don't think this custom scheduler is necessary but we think it may have solved some test failures for reasons we don't understand
                    // so we are leaving it in for now with a TODO to either remove this custom scheduler or actually understand what is going on
                    _testState.Tasks[_name] = TasksUtils.CreateTaskOnOwnThread(action);
                }
            }
            else
            {
                throw new ArgumentException($"Task {_name} must be started before usage.");
            }
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
    }
}
