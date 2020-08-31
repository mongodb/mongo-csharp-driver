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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;

namespace MongoDB.Driver.Tests.JsonDrivenTests
{
    public class JsonDrivenDoesNotExistTest : JsonDrivenCollectionTest
    {
        // private fields
        private BsonDocument _command;
        private IClientSessionHandle _session;

        // public constructors
        public JsonDrivenDoesNotExistTest(IMongoCollection<BsonDocument> collection, Dictionary<string, object> objectMap)
            : base(collection, objectMap)
        {
            _command = new BsonDocument("doesNotExist", 1);
        }

        protected override void AssertResult()
        {
        }

        // public methods
        public override void Arrange(BsonDocument document)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(document, "name", "object", "arguments");
            base.Arrange(document);
        }

        protected override void CallMethod(CancellationToken cancellationToken)
        {
            if (_session == null)
            {
                _collection
                    .Database
                    .RunCommand<BsonDocument>(_command, readPreference: null, cancellationToken);
            }
            else
            {
                _collection
                    .Database
                    .RunCommand<BsonDocument>(_session, _command, readPreference: null, cancellationToken);
            }
        }

        protected override async Task CallMethodAsync(CancellationToken cancellationToken)
        {
            if (_session == null)
            {
                await _collection
                    .Database
                    .RunCommandAsync<BsonDocument>(_command, readPreference: null, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _collection
                    .Database
                    .RunCommandAsync<BsonDocument>(_session, _command, readPreference: null, cancellationToken).ConfigureAwait(false);
            }
        }

        protected override void SetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "session":
                    _session = (IClientSessionHandle)_objectMap[value.AsString];
                    return;
                default:
                    _command.Merge(new BsonDocument(name, value));
                    return;
            }
        }
    }
}
