/* Copyright 2020–present MongoDB Inc.
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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Tests.JsonDrivenTests
{
    public sealed class JsonDrivenWaitForPrimaryChangeTest : JsonDrivenTestRunnerTest
    {
        private readonly IMongoClient _client;
        private readonly JsonDrivenRecordedPrimaryState _testState;
        private TimeSpan _timeout;

        public JsonDrivenWaitForPrimaryChangeTest(JsonDrivenTestsStateHolder stateHolder, IJsonDrivenTestRunner testRunner, IMongoClient client, Dictionary<string, object> objectMap)
            : base(testRunner, objectMap)
        {
            _testState = Ensure.IsNotNull(stateHolder, nameof(stateHolder)).GetTestState<JsonDrivenRecordedPrimaryState>(JsonDrivenRecordedPrimaryState.Key);
            _client = Ensure.IsNotNull(client, nameof(client));
        }

        protected override void CallMethod(CancellationToken cancellationToken)
        {
            var newPrimary = WaitForPrimaryChange(_testState.RecordedPrimary);
            if (newPrimary != null)
            {
                _testState.RecordedPrimary = newPrimary ;
            }
            else
            {
                throw new Exception($"The primary has not been changed from {_testState.RecordedPrimary} or timeout {_timeout} has been exceeded.");
            }
        }

        protected override async Task CallMethodAsync(CancellationToken cancellationToken)
        {
            await Task.Run(() => CallMethod(cancellationToken));
        }

        protected override void SetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "timeoutMS":
                    _timeout = TimeSpan.FromMilliseconds(value.ToInt32());
                    return;
            }

            base.SetArgument(name, value);
        }

        // private methods
        private EndPoint GetPrimary()
        {
            foreach (var server in _client.Cluster.Description.Servers)
            {
                if (server.Type == ServerType.ReplicaSetPrimary)
                {
                    return server.EndPoint;
                }
            }

            return null;
        }

        private EndPoint WaitForPrimaryChange(EndPoint previousPrimary)
        {
            EndPoint currentPrimary = null;
            if (SpinWait.SpinUntil(
                () =>
                {
                    currentPrimary = GetPrimary();
                    return currentPrimary != null && currentPrimary != previousPrimary;
                },
                _timeout))
            {
                return currentPrimary;
            }
            else
            {
                return null;
            }
        }
    }
}
