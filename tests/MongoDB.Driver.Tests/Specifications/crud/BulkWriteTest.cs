/* Copyright 2017-present MongoDB Inc.
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

using FluentAssertions;
using MongoDB.Bson;
using System;
using System.Collections.Generic;

namespace MongoDB.Driver.Tests.Specifications.crud
{
    public class BulkWriteTest : CrudOperationWithResultTestBase<BulkWriteResult<BsonDocument>>
    {
        private BulkWriteOptions _options = new BulkWriteOptions();
        private List<WriteModel<BsonDocument>> _requests;

        protected override bool TrySetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "options":
                    ParseOptions((BsonDocument)value);
                    return true;
                case "requests":
                    ParseRequests((BsonArray)value);
                    return true;
            }

            return false;
        }

        protected override BulkWriteResult<BsonDocument> ConvertExpectedResult(BsonValue expectedResult)
        {
            int requestCount = _requests.Count;
            long matchedCount = 0;
            long deletedCount = 0;
            long insertedCount = 0;
            long? modifiedCount = null;
            long upsertedCount = 0;
            List<BulkWriteUpsert> upserts = new List<BulkWriteUpsert>();

            foreach (var element in expectedResult.AsBsonDocument)
            {
                switch (element.Name)
                {
                    case "deletedCount":
                        deletedCount = element.Value.ToInt64();
                        break;

                    case "insertedCount":
                        insertedCount = element.Value.ToInt64();
                        break;

                    case "insertedIds":
                        break;

                    case "matchedCount":
                        matchedCount = element.Value.ToInt64();
                        break;

                    case "modifiedCount":
                        modifiedCount = element.Value.ToInt64();
                        break;

                    case "upsertedCount":
                        upsertedCount = element.Value.ToInt64();
                        break;

                    case "upsertedIds":
                        foreach (var upsertedId in element.Value.AsBsonDocument.Elements)
                        {
                            var index = int.Parse(upsertedId.Name);
                            var id = upsertedId.Value;
                            var upsert = new BulkWriteUpsert(index, id);
                            upserts.Add(upsert);
                        }
                        break;

                    default:
                        throw new ArgumentException($"Unexpected result field: \"{element.Name}\".");
                }
            }

            if (upserts.Count != upsertedCount)
            {
                throw new FormatException("upsertedIds count != upsertedCount");
            }

            return new BulkWriteResult<BsonDocument>.Acknowledged(requestCount, matchedCount, deletedCount, insertedCount, modifiedCount, _requests, upserts);
        }

        protected override BulkWriteResult<BsonDocument> ExecuteAndGetResult(IMongoDatabase database, IMongoCollection<BsonDocument> collection, bool async)
        {
            if (async)
            {
                return collection.BulkWriteAsync(_requests, _options).GetAwaiter().GetResult();
            }
            else
            {
                return collection.BulkWrite(_requests, _options);
            }
        }

        protected override void VerifyResult(BulkWriteResult<BsonDocument> actualResult, BulkWriteResult<BsonDocument> expectedResult)
        {
            actualResult.DeletedCount.Should().Be(expectedResult.DeletedCount);
            actualResult.InsertedCount.Should().Be(expectedResult.InsertedCount);
            actualResult.IsModifiedCountAvailable.Should().Be(expectedResult.IsModifiedCountAvailable);
            actualResult.MatchedCount.Should().Be(expectedResult.MatchedCount);
            actualResult.ModifiedCount.Should().Be(expectedResult.ModifiedCount);
            actualResult.ProcessedRequests.Should().Equal(expectedResult.ProcessedRequests);
            actualResult.RequestCount.Should().Be(expectedResult.RequestCount);
            actualResult.Upserts.Should().Equal(expectedResult.Upserts);
        }

        // private methods
        private List<ArrayFilterDefinition> ParseArrayFilters(BsonArray values)
        {
            var arrayFilters = new List<ArrayFilterDefinition>();

            foreach (BsonDocument value in values)
            {
                var arrayFilter = new BsonDocumentArrayFilterDefinition<BsonValue>(value);
                arrayFilters.Add(arrayFilter);
            }

            return arrayFilters;
        }

        private void ParseDeleteArguments(
            BsonDocument value,
            out FilterDefinition<BsonDocument> filter,
            out Collation collation,
            out BsonValue hint)
        {
            filter = null;
            collation = null;
            hint = null;

            foreach (BsonElement argument in value["arguments"].AsBsonDocument)
            {
                switch (argument.Name)
                {
                    case "collation":
                        collation = Collation.FromBsonDocument(argument.Value.AsBsonDocument);
                        break;
                    case "filter":
                        filter = ParseFilter(argument.Value.AsBsonDocument);
                        break;
                    case "hint":
                        hint = argument.Value;
                        break;
                    default:
                        throw new FormatException($"Unexpected argument: {argument.Name}.");
                }
            }
        }

        private WriteModel<BsonDocument> ParseDeleteManyModel(BsonDocument value)
        {
            ParseDeleteArguments(value, out var filter, out var collation, out var hint);
            return new DeleteManyModel<BsonDocument>(filter)
            {
                Collation = collation,
                Hint = hint
            };
        }

        private WriteModel<BsonDocument> ParseDeleteOneModel(BsonDocument value)
        {
            ParseDeleteArguments(value, out var filter, out var collation, out var hint);
            return new DeleteOneModel<BsonDocument>(filter)
            {
                Collation = collation,
                Hint = hint
            };
        }

        private FilterDefinition<BsonDocument> ParseFilter(BsonDocument value)
        {
            return new BsonDocumentFilterDefinition<BsonDocument>(value);
        }

        private void ParseInsertArguments(
            BsonDocument value,
            out BsonDocument document)
        {
            document = null;

            foreach (BsonElement argument in value["arguments"].AsBsonDocument)
            {
                switch (argument.Name)
                {
                    case "document":
                        document = argument.Value.AsBsonDocument;
                        break;
                    default:
                        throw new FormatException($"Unexpected argument: {argument.Name}.");
                }
            }
        }

        private WriteModel<BsonDocument> ParseInsertOneModel(BsonDocument value)
        {
            ParseInsertArguments(value, out var document);
            return new InsertOneModel<BsonDocument>(document);
        }

        private void ParseOptions(BsonDocument value)
        {
            foreach (var element in value)
            {
                switch (element.Name)
                {
                    case "ordered":
                        _options.IsOrdered = element.Value.ToBoolean();
                        break;
                    default:
                        throw new FormatException($"Unexpected option: {element.Name}.");
                }
            }
        }

        private void ParseReplaceArguments(
            BsonDocument value,
            out FilterDefinition<BsonDocument> filter,
            out BsonDocument replacement,
            out Collation collation,
            out BsonValue hint,
            out bool isUpsert)
        {
            filter = null;
            replacement = null;
            collation = null;
            hint = null;
            isUpsert = false;

            foreach (BsonElement argument in value["arguments"].AsBsonDocument)
            {
                switch (argument.Name)
                {
                    case "collation":
                        collation = Collation.FromBsonDocument(argument.Value.AsBsonDocument);
                        break;
                    case "filter":
                        filter = ParseFilter(argument.Value.AsBsonDocument);
                        break;
                    case "hint":
                        hint = argument.Value;
                        break;
                    case "replacement":
                        replacement = argument.Value.AsBsonDocument;
                        break;
                    case "upsert":
                        isUpsert = argument.Value.ToBoolean();
                        break;
                    default:
                        throw new FormatException($"Unexpected argument: {argument.Name}.");
                }
            }
        }

        private WriteModel<BsonDocument> ParseReplaceOneModel(BsonDocument value)
        {
            ParseReplaceArguments(value, out var filter, out var replacement, out var collation, out var hint, out var isUpsert);
            return new ReplaceOneModel<BsonDocument>(filter, replacement) { Collation = collation, Hint = hint, IsUpsert = isUpsert };
        }

        private void ParseRequests(BsonArray value)
        {
            _requests = new List<WriteModel<BsonDocument>>();

            foreach (BsonDocument requestValue in value)
            {
                var request = ParseWriteModel(requestValue);
                _requests.Add(request);
            }
        }

        private UpdateDefinition<BsonDocument> ParseUpdate(BsonDocument value)
        {
            return new BsonDocumentUpdateDefinition<BsonDocument>(value);
        }

        private void ParseUpdateArguments(
            BsonDocument value,
            out FilterDefinition<BsonDocument> filter,
            out UpdateDefinition<BsonDocument> update,
            out List<ArrayFilterDefinition> arrayFilters,
            out Collation collation,
            out BsonValue hint,
            out bool isUpsert)
        {
            arrayFilters = null;
            filter = null;
            update = null;
            collation = null;
            hint = null;
            isUpsert = false;

            foreach (BsonElement argument in value["arguments"].AsBsonDocument)
            {
                switch (argument.Name)
                {
                    case "arrayFilters":
                        arrayFilters = ParseArrayFilters(argument.Value.AsBsonArray);
                        break;
                    case "collation":
                        collation = Collation.FromBsonDocument(argument.Value.AsBsonDocument);
                        break;
                    case "filter":
                        filter = ParseFilter(argument.Value.AsBsonDocument);
                        break;
                    case "hint":
                        hint = argument.Value;
                        break;
                    case "update":
                        update = ParseUpdate(argument.Value.AsBsonDocument);
                        break;
                    case "upsert":
                        isUpsert = argument.Value.ToBoolean();
                        break;
                    default:
                        throw new FormatException($"Unexpected argument: {argument.Name}.");
                }
            }
        }

        private WriteModel<BsonDocument> ParseUpdateManyModel(BsonDocument value)
        {
            ParseUpdateArguments(value, out var filter, out var update, out var arrayFilters, out var collation, out var hint, out var isUpsert);
            return new UpdateManyModel<BsonDocument>(filter, update) { ArrayFilters = arrayFilters, Collation = collation, Hint = hint, IsUpsert = isUpsert };
        }

        private WriteModel<BsonDocument> ParseUpdateOneModel(BsonDocument value)
        {
            ParseUpdateArguments(value, out var filter, out var update, out var arrayFilters, out var collation, out var hint, out var isUpsert);
            return new UpdateOneModel<BsonDocument>(filter, update) { ArrayFilters = arrayFilters, Collation = collation, Hint = hint, IsUpsert = isUpsert };
        }

        private WriteModel<BsonDocument> ParseWriteModel(BsonDocument value)
        {
            var name = value["name"].AsString;
            switch (name)
            {
                case "deleteMany":
                    return ParseDeleteManyModel(value);
                case "deleteOne":
                    return ParseDeleteOneModel(value);
                case "insertOne":
                    return ParseInsertOneModel(value);
                case "replaceOne":
                    return ParseReplaceOneModel(value);
                case "updateMany":
                    return ParseUpdateManyModel(value);
                case "updateOne":
                    return ParseUpdateOneModel(value);
                default:
                    throw new FormatException($"Unexpected model name: {name}.");
            }
        }
    }
}
