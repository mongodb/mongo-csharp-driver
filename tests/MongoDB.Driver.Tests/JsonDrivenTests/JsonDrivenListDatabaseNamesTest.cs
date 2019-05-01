/* Copyright 2019-present MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;

namespace MongoDB.Driver.Tests.JsonDrivenTests
{
    public sealed class JsonDrivenListDatabaseNamesTest : JsonDrivenClientTest
    {
        // private fields
        private IClientSessionHandle _session;

        // public constructors
        public JsonDrivenListDatabaseNamesTest(IMongoClient client, Dictionary<string, object> objectMap)
            : base(client, objectMap)
        {
        }

        // public methods
        public override void Arrange(BsonDocument document)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(document, "name", "object", "command_name", "arguments", "result", "error");
            base.Arrange(document);
        }

        // protected methods
        protected override void AssertResult()
        {
        }

        protected override void CallMethod(CancellationToken cancellationToken)
        {
            if (_session == null)
            {
                var cursor = _client.ListDatabaseNames(cancellationToken);
            }
            else
            {
                var cursor = _client.ListDatabaseNames(_session, cancellationToken);
            }
        }

        protected override async Task CallMethodAsync(CancellationToken cancellationToken)
        {
            if (_session == null)
            {
                var cursor = await _client.ListDatabaseNamesAsync(cancellationToken);
            }
            else
            {
                var cursor = await _client.ListDatabaseNamesAsync(_session, cancellationToken);
            }
        }

        protected override void SetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "session":
                    _session = (IClientSessionHandle)_objectMap[value.AsString];
                    return;
            }

            base.SetArgument(name, value);
        }
    }
}
