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
    public sealed class JsonDrivenCreateCollectionTest : JsonDrivenDatabaseTest
    {
        // private fields
        private string _collectionName;
        private IClientSessionHandle _session;

        // public constructors
        public JsonDrivenCreateCollectionTest(IMongoDatabase database, Dictionary<string, object> objectMap)
            : base(database, objectMap)
        {
        }

        // public methods
        public override void Arrange(BsonDocument document)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(document, "name", "object", "arguments");
            base.Arrange(document);
        }

        // protected methods
        protected override void CallMethod(CancellationToken cancellationToken)
        {
            if (_session == null)
            {
                _database.CreateCollection(_collectionName, cancellationToken: cancellationToken);
            }
            else
            {
                _database.CreateCollection(_session, _collectionName, cancellationToken: cancellationToken);
            }
        }

        protected override async Task CallMethodAsync(CancellationToken cancellationToken)
        {
            if (_session == null)
            {
                await _database.CreateCollectionAsync(_collectionName, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _database.CreateCollectionAsync(_session, _collectionName, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        protected override void SetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "collection":
                    _collectionName = value.AsString;
                    return;
                case "session":
                    _session = (IClientSessionHandle)_objectMap[value.AsString];
                    return;
            }

            base.SetArgument(name, value);
        }
    }
}
