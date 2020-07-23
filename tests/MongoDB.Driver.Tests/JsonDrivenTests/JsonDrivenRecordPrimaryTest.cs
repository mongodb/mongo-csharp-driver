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

using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Tests.JsonDrivenTests
{
    public class JsonDrivenRecordedPrimaryState
    {
        #region static
        public static string Key => nameof(RecordedPrimary);
        #endregion

        private EndPoint _recordedPrimary;

        public EndPoint RecordedPrimary
        {
            get => _recordedPrimary;
            set => _recordedPrimary = Ensure.IsNotNull(value, nameof(RecordedPrimary));
        }
    }

    public sealed class JsonDrivenRecordPrimaryTest : JsonDrivenTestRunnerTest
    {
        private readonly IMongoClient _client;
        private readonly JsonDrivenRecordedPrimaryState _testState;

        public JsonDrivenRecordPrimaryTest(JsonDrivenTestsStateHolder stateHolder, IJsonDrivenTestRunner testRunner, IMongoClient client, Dictionary<string, object> objectMap)
            : base(testRunner, objectMap)
        {
            _client = Ensure.IsNotNull(client, nameof(client));
            _testState = Ensure.IsNotNull(stateHolder, nameof(stateHolder)).GetTestState<JsonDrivenRecordedPrimaryState>(JsonDrivenRecordedPrimaryState.Key);
        }

        protected override void CallMethod(CancellationToken cancellationToken)
        {
            _testState.RecordedPrimary = GetPrimary();
        }

        protected override Task CallMethodAsync(CancellationToken cancellationToken)
        {
            _testState.RecordedPrimary = GetPrimary();
            return Task.FromResult(true);
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
    }
}
