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
    public sealed class JsonDrivenDropIndexTest : JsonDrivenCollectionTest
    {
        // private fields
        private string _indexName;
        private IClientSessionHandle _session;

        // public constructors
        public JsonDrivenDropIndexTest(IMongoCollection<BsonDocument> collection, Dictionary<string, object> objectMap)
            : base(collection, objectMap)
        {
        }

        protected override void AssertResult()
        {
        }

        // public methods
        public override void Arrange(BsonDocument document)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(document, "name", "object", "collectionOptions", "arguments");
            base.Arrange(document);
        }

        protected override void CallMethod(CancellationToken cancellationToken)
        {
            if (_session == null)
            {
                _collection.Indexes.DropOne(_indexName, cancellationToken);
            }
            else
            {
                _collection.Indexes.DropOne(_session, _indexName, cancellationToken);
            }
        }

        protected override async Task CallMethodAsync(CancellationToken cancellationToken)
        {
            if (_session == null)
            {
                await _collection.Indexes.DropOneAsync(_indexName, cancellationToken);
            }
            else
            {
                await _collection.Indexes.DropOneAsync(_session, _indexName, cancellationToken);
            }
        }

        protected override void SetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "name":
                    _indexName = value.ToString();
                    return;
                case "session":
                    _session = (IClientSessionHandle)_objectMap[value.AsString];
                    return;
            }

            base.SetArgument(name, value);
        }
    }
}
