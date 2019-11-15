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
    public class DeleteManyTest : RetryableWriteTestBase
    {
        // private fields
        private FilterDefinition<BsonDocument> _filter;
        private DeleteResult _result;

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

                    default:
                        throw new ArgumentException($"Unexpected argument: {argument.Name}.");
                }
            }
        }

        // protected methods
        protected override void ExecuteAsync(IMongoCollection<BsonDocument> collection)
        {
            _result = collection.DeleteManyAsync(_filter).GetAwaiter().GetResult();
        }

        protected override void ExecuteSync(IMongoCollection<BsonDocument> collection)
        {
            _result = collection.DeleteMany(_filter);
        }

        protected override void VerifyResult(BsonDocument result)
        {
            var expectedResult = ParseResult(result);
            _result.DeletedCount.Should().Be(expectedResult.DeletedCount);
        }

        // private methods
        private DeleteResult ParseResult(BsonDocument result)
        {
            VerifyFields(result, "deletedCount");
            var deletedCount = result["deletedCount"].ToInt64();
            return new DeleteResult.Acknowledged(deletedCount);
        }
    }
}
