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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;

namespace MongoDB.Driver.Tests.JsonDrivenTests
{
    public sealed class JsonDrivenUpdateManyTest : JsonDrivenCollectionTest
    {
        // private fields
        private FilterDefinition<BsonDocument> _filter;
        private UpdateOptions _options = new UpdateOptions();
        private UpdateResult _result;
        private UpdateDefinition<BsonDocument> _update;

        // public constructors
        public JsonDrivenUpdateManyTest(IMongoClient client, IMongoDatabase database, IMongoCollection<BsonDocument> collection, Dictionary<string, IClientSessionHandle> sessionMap)
            : base(client, database, collection, sessionMap)
        {
        }

        // public methods
        public override void Arrange(BsonDocument document)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(document, "name", "arguments", "result");
            base.Arrange(document);
        }

        // protected methods
        protected override void AssertResult()
        {
            foreach (var aspect in _expectedResult.AsBsonDocument)
            {
                AssertResultAspect(aspect.Name, aspect.Value);
            }
        }

        protected override void CallMethod(CancellationToken cancellationToken)
        {
            _result = _collection.UpdateMany(_filter, _update, _options, cancellationToken);
        }

        protected override void CallMethod(IClientSessionHandle session, CancellationToken cancellationToken)
        {
            _result = _collection.UpdateMany(session, _filter, _update, _options, cancellationToken);
        }

        protected override async Task CallMethodAsync(CancellationToken cancellationToken)
        {
            _result = await _collection.UpdateManyAsync(_filter, _update, _options, cancellationToken).ConfigureAwait(false);
        }

        protected override async Task CallMethodAsync(IClientSessionHandle session, CancellationToken cancellationToken)
        {
            _result = await _collection.UpdateManyAsync(session, _filter, _update, _options, cancellationToken).ConfigureAwait(false);
        }

        protected override void SetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "filter":
                    _filter = new BsonDocumentFilterDefinition<BsonDocument>(value.AsBsonDocument);
                    return;

                case "update":
                    _update = new BsonDocumentUpdateDefinition<BsonDocument>(value.AsBsonDocument);
                    return;

                case "upsert":
                    _options.IsUpsert = value.ToBoolean();
                    return;
            }

            base.SetArgument(name, value);
        }

        // private methods
        private void AssertResultAspect(string name, BsonValue expectedValue)
        {
            switch (name)
            {
                case "matchedCount":
                    _result.MatchedCount.Should().Be(expectedValue.ToInt64());
                    break;

                case "modifiedCount":
                    _result.ModifiedCount.Should().Be(expectedValue.ToInt64());
                    break;

                case "upsertedCount":
                    var upsertedCount = _result.UpsertedId == null ? 0 : 1;
                    upsertedCount.Should().Be(expectedValue.ToInt32());
                    break;

                case "upsertedId":
                    _result.UpsertedId.Should().Be(expectedValue);
                    break;

                default:
                    throw new FormatException($"Invalid UpdateMany result aspect: {name}.");
            }
        }
    }
}
