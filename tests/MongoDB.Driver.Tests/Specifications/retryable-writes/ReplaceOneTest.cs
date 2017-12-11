/* Copyright 2017 MongoDB Inc.
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
    public class ReplaceOneTest : RetryableWriteTestBase
    {
        // private fields
        FilterDefinition<BsonDocument> _filter;
        BsonDocument _replacement;
        ReplaceOneResult _result;

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

                    case "replacement":
                        _replacement = argument.Value.AsBsonDocument;
                        break;

                    default:
                        throw new ArgumentException($"Unexpected argument: {argument.Name}.");
                }
            }
        }


        // protected methods
        protected override void ExecuteAsync(IMongoCollection<BsonDocument> collection)
        {
            _result = collection.ReplaceOneAsync(_filter, _replacement).GetAwaiter().GetResult();
        }

        protected override void ExecuteSync(IMongoCollection<BsonDocument> collection)
        {
            _result = collection.ReplaceOne(_filter, _replacement);
        }

        protected override void VerifyResult(BsonDocument result)
        {
            var expectedResult = ParseResult(result);
            _result.MatchedCount.Should().Be(expectedResult.MatchedCount);
            _result.ModifiedCount.Should().Be(expectedResult.ModifiedCount);
        }

        // private methods
        private ReplaceOneResult ParseResult(BsonValue result)
        {
            VerifyFields((BsonDocument)result, "matchedCount", "modifiedCount", "upsertedCount");
            var matchedCount = result["matchedCount"].ToInt64();
            var modifiedCount = result["modifiedCount"].ToInt64();
            var upsertedCount = result["upsertedCount"].ToInt64();
            upsertedCount.Should().Be(0);

            return new ReplaceOneResult.Acknowledged(matchedCount, modifiedCount, null);
        }
    }
}
