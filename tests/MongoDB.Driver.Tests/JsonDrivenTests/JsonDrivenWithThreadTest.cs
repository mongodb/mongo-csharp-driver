﻿/* Copyright 2020-present MongoDB Inc.
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

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Tests.JsonDrivenTests
{
    internal sealed class JsonDrivenWithThreadState
    {
        public const string Key = nameof(Tasks);

        public ConcurrentDictionary<string, Task> Tasks { get; } = new ConcurrentDictionary<string, Task>();
    }

    internal abstract class JsonDrivenWithThreadTest : JsonDrivenTestRunnerTest
    {
        protected string _name;
        protected readonly JsonDrivenWithThreadState _testState;

        public JsonDrivenWithThreadTest(
            JsonDrivenTestsStateHolder stateHolder,
            IJsonDrivenTestRunner testRunner,
            Dictionary<string, object> objectMap)
            : base(testRunner, objectMap)
        {
            _testState = Ensure.IsNotNull(stateHolder, nameof(stateHolder)).GetTestState<JsonDrivenWithThreadState>(JsonDrivenWithThreadState.Key);
            Ensure.IsNotNull(_testState.Tasks, nameof(_testState.Tasks));
        }

        protected override void SetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "name":
                    _name = value.ToString();
                    return;
            }

            base.SetArgument(name, value);
        }
    }
}
