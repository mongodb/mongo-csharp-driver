/* Copyright 2019-present MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;

namespace MongoDB.Driver.Tests.Specifications.retryable_writes
{
    public class UpdateManyTest : RetryableWriteTestBase
    {
        private FilterDefinition<BsonDocument> _filter;
        private UpdateDefinition<BsonDocument> _update;
        private readonly UpdateOptions _options = new UpdateOptions();
        private UpdateResult _result;

        // public methods
        public override void Initialize(BsonDocument operation)
        {
            VerifyFields(operation, "name", "arguments");

            foreach (var argument in operation["arguments"].AsBsonDocument)
            {
                switch (argument.Name)
                {
                    case "filter":
                        _filter = argument.Value.AsBsonDocument;
                        break;

                    case "update":
                        _update = argument.Value.AsBsonDocument;
                        break;

                    case "upsert":
                        _options.IsUpsert = argument.Value.ToBoolean();
                        break;

                    default:
                        throw new ArgumentException($"Unexpected argument: {argument.Name}.");
                }
            }
        }

        // protected methods
        protected override void ExecuteAsync(IMongoCollection<BsonDocument> collection)
        {
            _result = collection.UpdateManyAsync(_filter, _update, _options).GetAwaiter().GetResult();
        }

        protected override void ExecuteSync(IMongoCollection<BsonDocument> collection)
        {
            _result = collection.UpdateMany(_filter, _update, _options);
        }

        protected override void VerifyResult(BsonDocument result)
        {
            var expectedResult = ParseResult(result);
            _result.MatchedCount.Should().Be(expectedResult.MatchedCount);
            _result.ModifiedCount.Should().Be(expectedResult.ModifiedCount);
            _result.UpsertedId.Should().Be(expectedResult.UpsertedId);
        }

        // private methods
        private UpdateResult ParseResult(BsonDocument result)
        {
            VerifyFields(result, "matchedCount", "modifiedCount", "upsertedCount", "upsertedId");

            var matchedCount = result["matchedCount"].ToInt64();
            var modifiedCount = result["modifiedCount"].ToInt64();
            var upsertedCount = result["upsertedCount"].ToInt32();
            var upsertedId = result.GetValue("upsertedId", null);
            upsertedCount.Should().Be(upsertedId == null ? 0 : 1);

            return new UpdateResult.Acknowledged(matchedCount, modifiedCount, upsertedId);
        }
    }
}
