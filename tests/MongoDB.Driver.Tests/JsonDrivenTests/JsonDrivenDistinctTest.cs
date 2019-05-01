/* Copyright 2018-present MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;

namespace MongoDB.Driver.Tests.JsonDrivenTests
{
    public sealed class JsonDrivenDistinctTest : JsonDrivenCollectionTest
    {
        // private fields
        private FieldDefinition<BsonDocument, BsonValue> _field;
        private FilterDefinition<BsonDocument> _filter = new BsonDocument();
        private DistinctOptions _options = new DistinctOptions();
        private List<BsonValue> _result;
        private IClientSessionHandle _session;

        // public constructors
        public JsonDrivenDistinctTest(IMongoCollection<BsonDocument> collection, Dictionary<string, object> objectMap)
            : base(collection, objectMap)
        {
        }

        // public methods
        public override void Arrange(BsonDocument document)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(document, "name", "object", "collectionOptions", "arguments", "result", "error");
            base.Arrange(document);
        }

        // protected methods
        protected override void AssertResult()
        {
            _result.Should().Equal(_expectedResult.AsBsonArray.Values);
        }

        protected override void CallMethod(CancellationToken cancellationToken)
        {
            IAsyncCursor<BsonValue> cursor;
            if (_session == null)
            {
                cursor = _collection.Distinct(_field, _filter, _options, cancellationToken);
            }
            else
            {
                cursor = _collection.Distinct(_session, _field, _filter, _options, cancellationToken);
            }
            _result = cursor.ToList(cancellationToken);
        }

        protected override async Task CallMethodAsync(CancellationToken cancellationToken)
        {
            IAsyncCursor<BsonValue> cursor;
            if (_session == null)
            {
                cursor = await _collection.DistinctAsync(_field, _filter, _options, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                cursor = await _collection.DistinctAsync(_session, _field, _filter, _options, cancellationToken).ConfigureAwait(false);
            }
            _result = await cursor.ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        protected override void SetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "fieldName":
                    _field = new StringFieldDefinition<BsonDocument, BsonValue>(value.AsString);
                    return;

                case "filter":
                    _filter = new BsonDocumentFilterDefinition<BsonDocument>(value.AsBsonDocument);
                    return;

                case "session":
                    _session = (IClientSessionHandle)_objectMap[value.AsString];
                    return;
            }

            base.SetArgument(name, value);
        }
    }
}
