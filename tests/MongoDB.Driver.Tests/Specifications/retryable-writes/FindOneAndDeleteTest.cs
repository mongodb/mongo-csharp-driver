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
using FluentAssertions;
using MongoDB.Bson;

namespace MongoDB.Driver.Tests.Specifications.retryable_writes
{
    public class FindOneAndDeleteTest : RetryableWriteTestBase
    {
        // private fields
        private BsonDocument _filter;
        private FindOneAndDeleteOptions<BsonDocument> _options = new FindOneAndDeleteOptions<BsonDocument>();
        private BsonDocument _result;

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

                    case "projection":
                        _options.Projection = argument.Value.AsBsonDocument;
                        break;

                    case "sort":
                        _options.Sort = argument.Value.AsBsonDocument;
                        break;

                    case "collation":
                        _options.Collation = Collation.FromBsonDocument(argument.Value.AsBsonDocument);
                        break;

                    case "hint":
                        _options.Hint = argument.Value;
                        break;

                    default:
                        throw new ArgumentException($"Unexpected argument: {argument.Name}.");
                }
            }
        }

        // protected methods
        protected override void ExecuteAsync(IMongoCollection<BsonDocument> collection)
        {
            _result = collection.FindOneAndDeleteAsync(_filter, _options).GetAwaiter().GetResult();
        }

        protected override void ExecuteSync(IMongoCollection<BsonDocument> collection)
        {
            _result = collection.FindOneAndDelete(_filter, _options);
        }

        protected override void VerifyResult(BsonDocument result)
        {
            _result.Should().Be(result);
        }
    }
}
