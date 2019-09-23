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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;

namespace MongoDB.Driver.Tests.JsonDrivenTests
{
    public sealed class JsonDrivenFindTest : JsonDrivenCollectionTest
    {
        // private fields
        private FilterDefinition<BsonDocument> _filter = new BsonDocument();
        private FindOptions<BsonDocument> _options = new FindOptions<BsonDocument>();
        private List<BsonDocument> _result;
        private IClientSessionHandle _session;

        // public constructors
        public JsonDrivenFindTest(IMongoCollection<BsonDocument> collection, Dictionary<string, object> objectMap)
            : base(collection, objectMap)
        {
        }

        // public methods
        public override void Arrange(BsonDocument document)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(document, "name", "object", "collectionOptions", "arguments", "result", "results", "error");
            base.Arrange(document);
        }

        // protected methods
        protected override void AssertResult()
        {
            if (_expectedResult is BsonDocument resultDocument && _result.Count == 1)
            {
                _result[0].Should().Be(resultDocument);
            }
            else
            {
                _result.Should().Equal(_expectedResult.AsBsonArray.Cast<BsonDocument>());
            }
        }

        protected override void CallMethod(CancellationToken cancellationToken)
        {
            IAsyncCursor<BsonDocument> cursor;
            if (_session == null)
            {
                cursor = _collection.FindSync(_filter, _options, cancellationToken);
            }
            else
            {
                cursor = _collection.FindSync(_session, _filter, _options, cancellationToken);
            }
            _result = cursor.ToList();
        }

        protected override async Task CallMethodAsync(CancellationToken cancellationToken)
        {
            IAsyncCursor<BsonDocument> cursor;
            if (_session == null)
            {
                cursor = await _collection.FindAsync(_filter, _options, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                cursor = await _collection.FindAsync(_session, _filter, _options, cancellationToken).ConfigureAwait(false);
            }
            _result = await cursor.ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        protected override void SetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "batchSize":
                    _options.BatchSize = value.ToInt32();
                    return;

                case "filter":
                    _filter = new BsonDocumentFilterDefinition<BsonDocument>(value.AsBsonDocument);
                    return;
                
                case "limit":
                    _options.Limit = value.ToInt32();
                    return;

                case "result":
                    ParseExpectedResult(value.IsBsonArray ? value.AsBsonArray : new BsonArray(new[] { value }));
                    return;

                case "session":
                    _session = (IClientSessionHandle)_objectMap[value.AsString];
                    return;
                
                case "sort":
                    _options.Sort = (SortDefinition<BsonDocument>)value;
                    return;
            }

            base.SetArgument(name, value);
        }
    }
}
