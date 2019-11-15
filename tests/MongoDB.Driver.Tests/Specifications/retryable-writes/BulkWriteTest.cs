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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;

namespace MongoDB.Driver.Tests.Specifications.retryable_writes
{
    public class BulkWriteTest : RetryableWriteTestBase
    {
        // private fields
        private IEnumerable<WriteModel<BsonDocument>> _requests;
        private BulkWriteOptions _options = new BulkWriteOptions();
        private BulkWriteResult<BsonDocument> _result;

        // public methods
        public override void Initialize(BsonDocument operation)
        {
            VerifyFields(operation, "name", "arguments");

            foreach (var argument in operation["arguments"].AsBsonDocument)
            {
                switch (argument.Name)
                {
                    case "requests":
                        _requests = ParseRequests(argument.Value.AsBsonArray);
                        break;

                    case "options":
                        _options = ParseOptions(argument.Value.AsBsonDocument);
                        break;

                    default:
                        throw new ArgumentException($"Unexpected argument: {argument.Name}.");
                }
            }
        }

        // protected methods
        protected override void ExecuteAsync(IMongoCollection<BsonDocument> collection)
        {
            _result = collection.BulkWriteAsync(_requests, _options).GetAwaiter().GetResult();
        }

        protected override void ExecuteSync(IMongoCollection<BsonDocument> collection)
        {
            _result = collection.BulkWrite(_requests, _options);
        }

        protected override void VerifyResult(BsonDocument result)
        {
            var expectedResult = ParseResult(result, out var expectedInsertedIds);
            foreach (var insertedId in expectedInsertedIds)
            {
                var insertModel = (InsertOneModel<BsonDocument>)_result.ProcessedRequests[insertedId.Key];
                insertModel.Document["_id"].Should().Be(insertedId.Value);
            }
            _result.DeletedCount.Should().Be(expectedResult.DeletedCount);
            _result.InsertedCount.Should().Be(expectedResult.InsertedCount);
            _result.MatchedCount.Should().Be(expectedResult.MatchedCount);
            _result.ModifiedCount.Should().Be(expectedResult.ModifiedCount);
            _result.Upserts.Should().Equal(expectedResult.Upserts, UpsertEquals);
        }

        // private methods
        private IEnumerable<WriteModel<BsonDocument>> ParseRequests(BsonArray requests)
        {
            foreach (var request in requests.Cast<BsonDocument>())
            {
                yield return ParseRequest(request);
            }
        }

        private WriteModel<BsonDocument> ParseRequest(BsonDocument request)
        {
            VerifyFields(request, "name", "arguments");
            var name = request["name"].AsString;
            var arguments = request["arguments"].AsBsonDocument;

            switch (name)
            {
                case "deleteMany":
                    return ParseDeleteMany(arguments);

                case "deleteOne":
                    return ParseDeleteOne(arguments);

                case "insertOne":
                    return ParseInsertOne(arguments);

                case "replaceOne":
                    return ParseReplaceOne(arguments);

                case "updateMany":
                    return ParseUpdateMany(arguments);

                case "updateOne":
                    return ParseUpdateOne(arguments);

                default:
                    throw new ArgumentException($"Unexpected request: {name}.");
            }
        }

        private DeleteManyModel<BsonDocument> ParseDeleteMany(BsonDocument arguments)
        {
            VerifyFields(arguments, "filter");
            var filter = arguments["filter"].AsBsonDocument;
            return new DeleteManyModel<BsonDocument>(filter);
        }

        private DeleteOneModel<BsonDocument> ParseDeleteOne(BsonDocument arguments)
        {
            VerifyFields(arguments, "filter");
            var filter = arguments["filter"].AsBsonDocument;
            return new DeleteOneModel<BsonDocument>(filter);
        }

        private InsertOneModel<BsonDocument> ParseInsertOne(BsonDocument arguments)
        {
            VerifyFields(arguments, "document");
            var document = arguments["document"].AsBsonDocument;
            return new InsertOneModel<BsonDocument>(document);
        }

        private ReplaceOneModel<BsonDocument> ParseReplaceOne(BsonDocument arguments)
        {
            VerifyFields(arguments, "filter", "replacement");
            var filter = arguments["filter"].AsBsonDocument;
            var replacement = arguments["replacement"].AsBsonDocument;
            return new ReplaceOneModel<BsonDocument>(filter, replacement);
        }

        private UpdateManyModel<BsonDocument> ParseUpdateMany(BsonDocument arguments)
        {
            VerifyFields(arguments, "filter", "update", "upsert");
            var filter = arguments["filter"].AsBsonDocument;
            var update = arguments["update"].AsBsonDocument;
            var isUpsert = arguments.GetValue("upsert", false).ToBoolean();
            return new UpdateManyModel<BsonDocument>(filter, update) { IsUpsert = isUpsert };
        }

        private UpdateOneModel<BsonDocument> ParseUpdateOne(BsonDocument arguments)
        {
            VerifyFields(arguments, "filter", "update", "upsert");
            var filter = arguments["filter"].AsBsonDocument;
            var update = arguments["update"].AsBsonDocument;
            var isUpsert = arguments.GetValue("upsert", false).ToBoolean();
            return new UpdateOneModel<BsonDocument>(filter, update) { IsUpsert = isUpsert };
        }

        private BulkWriteOptions ParseOptions(BsonDocument options)
        {
            var result = new BulkWriteOptions();

            foreach (var option in options)
            {
                switch (option.Name)
                {
                    case "ordered":
                        result.IsOrdered = option.Value.ToBoolean();
                        break;

                    default:
                        throw new ArgumentException($"Unexpected option: {option.Name}.");
                }
            }

            return result;
        }

        private BulkWriteResult<BsonDocument> ParseResult(BsonDocument result, out Dictionary<int, int> expectedInsertedIds)
        {
            VerifyFields(result, "deletedCount", "insertedCount", "insertedIds", "matchedCount", "modifiedCount", "upsertedCount", "upsertedIds");

            var deletedCount = result["deletedCount"].ToInt64();
            var insertedCount = result["insertedCount"].ToInt64();
            expectedInsertedIds = result["insertedIds"]
                .AsBsonDocument
                .ToDictionary(k => Convert.ToInt32(k.Name), v => v.Value.ToInt32());
            var matchedCount = result["matchedCount"].ToInt64();
            var modifiedCount = result["modifiedCount"].ToInt64();
            var processedRequests = new List<WriteModel<BsonDocument>>(_requests);
            var requestCount = _requests.Count();
            var upsertedCount = result.GetValue("upsertedCount", 0).ToInt32();
            var upserts = new List<BulkWriteUpsert>();
            if (result.Contains("upsertedIds"))
            {
                foreach (var element in result["upsertedIds"].AsBsonDocument.Elements)
                {
                    var index = int.Parse(element.Name, NumberFormatInfo.InvariantInfo);
                    var id = element.Value;
                    var upsert = new BulkWriteUpsert(index, id);
                    upserts.Add(upsert);
                }
            }
            upserts.Count.Should().Be(upsertedCount);

            return new BulkWriteResult<BsonDocument>.Acknowledged(requestCount, matchedCount, deletedCount, insertedCount, modifiedCount, processedRequests, upserts);
        }

        private bool UpsertEquals(BulkWriteUpsert x, BulkWriteUpsert y)
        {
            return x.Index == y.Index && x.Id.Equals(y.Id);
        }
    }
}
