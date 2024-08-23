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
using MongoDB.TestHelpers.JsonDrivenTests;

namespace MongoDB.Driver.Tests.JsonDrivenTests
{
    public sealed class JsonDrivenCreateIndexTest : JsonDrivenCollectionTest
    {
        // private fields
        private BsonDocument _keys;
        private string _name;
        private IClientSessionHandle _session;

        // public constructors
        public JsonDrivenCreateIndexTest(IMongoCollection<BsonDocument> collection, Dictionary<string, object> objectMap)
            : base(collection, objectMap)
        {
        }

        // public methods
        public override void Arrange(BsonDocument document)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(document, "name", "object", "collectionOptions", "arguments");
            base.Arrange(document);
        }

        // protected methods
        protected override void AssertResult()
        {
        }

        protected override void CallMethod(CancellationToken cancellationToken)
        {
            var model = CreateCreateIndexModel();
            if (_session == null)
            {
                _collection.Indexes.CreateOne(model, new CreateOneIndexOptions(), cancellationToken);
            }
            else
            {
                _collection.Indexes.CreateOne(_session, model, new CreateOneIndexOptions(), cancellationToken);
            }
        }

        protected override async Task CallMethodAsync(CancellationToken cancellationToken)
        {
            var model = CreateCreateIndexModel();
            if (_session == null)
            {
                await _collection.Indexes.CreateOneAsync(model, new CreateOneIndexOptions(), cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _collection.Indexes.CreateOneAsync(_session, model, new CreateOneIndexOptions(), cancellationToken).ConfigureAwait(false);
            }
        }

        protected override void SetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "keys":
                    _keys = value.AsBsonDocument;
                    return;
                case "name":
                    _name = value.AsString;
                    return;
                case "session":
                    _session = (IClientSessionHandle)_objectMap[value.AsString];
                    return;
            }

            base.SetArgument(name, value);
        }

        // private methods
        private CreateIndexModel<BsonDocument> CreateCreateIndexModel()
        {
            var keysDefinition = new BsonDocumentIndexKeysDefinition<BsonDocument>(_keys);
            var options = new CreateIndexOptions { Name = _name };
            return new CreateIndexModel<BsonDocument>(keysDefinition, options);
        }
    }
}
