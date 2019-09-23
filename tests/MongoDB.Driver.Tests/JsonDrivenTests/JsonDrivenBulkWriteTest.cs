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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;

namespace MongoDB.Driver.Tests.JsonDrivenTests
{
    public sealed class JsonDrivenBulkWriteTest : JsonDrivenCollectionTest
    {
        // private fields
        private BulkWriteOptions _options = new BulkWriteOptions();
        private List<WriteModel<BsonDocument>> _requests;
        private BulkWriteResult<BsonDocument> _result;
        private IClientSessionHandle _session;

        // public constructors
        public JsonDrivenBulkWriteTest(IMongoCollection<BsonDocument> collection, Dictionary<string, object> objectMap)
            : base(collection, objectMap)
        {
        }

        // public methods
        public override void Arrange(BsonDocument document)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(document, "name", "object", "collectionOptions", "arguments", "result");
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
            if (_session == null)
            {
                _result = _collection.BulkWrite(_requests, _options);
            }
            else
            {
                _result = _collection.BulkWrite(_session, _requests, _options);
            }
        }

        protected override async Task CallMethodAsync(CancellationToken cancellationToken)
        {
            if (_session == null)
            {
                _result = await _collection.BulkWriteAsync(_requests, _options).ConfigureAwait(false);
            }
            else
            {
                _result = await _collection.BulkWriteAsync(_session, _requests, _options).ConfigureAwait(false);
            }
        }

        protected override void SetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "options":
                    SetArguments(value.AsBsonDocument);
                    return;

                case "ordered":
                    _options.IsOrdered = value.ToBoolean();
                    return;

                case "requests":
                    _requests = ParseWriteModels(value.AsBsonArray.Cast<BsonDocument>());
                    return;

                case "session":
                    _session = (IClientSessionHandle)_objectMap[value.AsString];
                    return;
            }

            base.SetArgument(name, value);
        }

        // private methods
        private void AssertResultAspect(string name, BsonValue expectedValue)
        {
            switch (name)
            {
                case "deletedCount":
                    _result.DeletedCount.Should().Be(expectedValue.ToInt64());
                    break;

                case "insertedIds":
                    _result.InsertedCount.Should().Be(expectedValue.AsBsonDocument.ElementCount);
                    break;
                
                case "insertedCount":
                    _result.InsertedCount.Should().Be(expectedValue.ToInt64());
                    break;

                case "matchedCount":
                    _result.MatchedCount.Should().Be(expectedValue.ToInt64());
                    break;

                case "modifiedCount":
                    _result.ModifiedCount.Should().Be(expectedValue.ToInt64());
                    break;

                case "upsertedCount":
                    _result.Upserts.Count.Should().Be(expectedValue.ToInt32());
                    break;

                case "upsertedIds":
                    _result.Upserts.Select(u => u.Id).Should().Equal(expectedValue.AsBsonDocument.Values);
                    break;

                default: throw new FormatException($"Invalid BulkWriteResult aspect name: {name}.");
            }
        }

        private DeleteManyModel<BsonDocument> ParseDeleteManyModel(BsonDocument model)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(model, "name", "arguments");
            var arguments = model["arguments"].AsBsonDocument;

            JsonDrivenHelper.EnsureAllFieldsAreValid(arguments, "filter");
            var filter = new BsonDocumentFilterDefinition<BsonDocument>(arguments["filter"].AsBsonDocument);

            return new DeleteManyModel<BsonDocument>(filter);
        }

        private DeleteOneModel<BsonDocument> ParseDeleteOneModel(BsonDocument model)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(model, "name", "arguments");
            var arguments = model["arguments"].AsBsonDocument;

            JsonDrivenHelper.EnsureAllFieldsAreValid(arguments, "filter");
            var filter = new BsonDocumentFilterDefinition<BsonDocument>(arguments["filter"].AsBsonDocument);

            return new DeleteOneModel<BsonDocument>(filter);
        }

        private InsertOneModel<BsonDocument> ParseInsertOneModel(BsonDocument model)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(model, "name", "arguments");
            var arguments = model["arguments"].AsBsonDocument;

            JsonDrivenHelper.EnsureAllFieldsAreValid(arguments, "document");
            var document = arguments["document"].AsBsonDocument;

            return new InsertOneModel<BsonDocument>(document);
        }

        private ReplaceOneModel<BsonDocument> ParseReplaceOneModel(BsonDocument model)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(model, "name", "arguments");
            var arguments = model["arguments"].AsBsonDocument;

            JsonDrivenHelper.EnsureAllFieldsAreValid(arguments, "filter", "replacement");
            var filter = new BsonDocumentFilterDefinition<BsonDocument>(arguments["filter"].AsBsonDocument);
            var replacement = arguments["replacement"].AsBsonDocument;

            return new ReplaceOneModel<BsonDocument>(filter, replacement);
        }

        private UpdateManyModel<BsonDocument> ParseUpdateManyModel(BsonDocument model)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(model, "name", "arguments");
            var arguments = model["arguments"].AsBsonDocument;

            JsonDrivenHelper.EnsureAllFieldsAreValid(arguments, "filter", "update", "upsert");
            var filter = new BsonDocumentFilterDefinition<BsonDocument>(arguments["filter"].AsBsonDocument);
            var update = new BsonDocumentUpdateDefinition<BsonDocument>(arguments["update"].AsBsonDocument);
            var isUpsert = arguments.GetValue("upsert", false).ToBoolean();

            return new UpdateManyModel<BsonDocument>(filter, update)
            {
                IsUpsert = isUpsert
            };
        }

        private UpdateOneModel<BsonDocument> ParseUpdateOneModel(BsonDocument model)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(model, "name", "arguments");
            var arguments = model["arguments"].AsBsonDocument;

            JsonDrivenHelper.EnsureAllFieldsAreValid(arguments, "filter", "update", "upsert");
            var filter = new BsonDocumentFilterDefinition<BsonDocument>(arguments["filter"].AsBsonDocument);
            var update = new BsonDocumentUpdateDefinition<BsonDocument>(arguments["update"].AsBsonDocument);
            var isUpsert = arguments.GetValue("upsert", false).ToBoolean();

            return new UpdateOneModel<BsonDocument>(filter, update)
            {
                IsUpsert = isUpsert
            };
        }

        private WriteModel<BsonDocument> ParseWriteModel(BsonDocument model)
        {
            var modelName = model["name"].AsString;
            switch (modelName)
            {
                case "deleteMany": return ParseDeleteManyModel(model);
                case "deleteOne": return ParseDeleteOneModel(model);
                case "insertOne": return ParseInsertOneModel(model);
                case "replaceOne": return ParseReplaceOneModel(model);
                case "updateMany": return ParseUpdateManyModel(model);
                case "updateOne": return ParseUpdateOneModel(model);
                default: throw new FormatException($"Invalid write model name: {modelName}.");
            }
        }

        private List<WriteModel<BsonDocument>> ParseWriteModels(IEnumerable<BsonDocument> models)
        {
            var result = new List<WriteModel<BsonDocument>>();
            foreach (var model in models)
            {
                result.Add(ParseWriteModel(model));
            }
            return result;
        }
    }
}
