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
using FluentAssertions;
using MongoDB.Bson;

namespace MongoDB.Driver.Tests.Specifications.retryable_writes
{
    public class FindOneAndUpdateTest : RetryableWriteTestBase
    {
        // private fields
        private BsonDocument _filter;
        private BsonDocument _update;
        private FindOneAndUpdateOptions<BsonDocument> _options = new FindOneAndUpdateOptions<BsonDocument>();
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

                    case "update":
                        _update = argument.Value.AsBsonDocument;
                        break;

                    case "projection":
                        _options.Projection = argument.Value.AsBsonDocument;
                        break;

                    case "sort":
                        _options.Sort = argument.Value.AsBsonDocument;
                        break;

                    case "upsert":
                        _options.IsUpsert = argument.Value.ToBoolean();
                        break;

                    case "returnDocument":
                        _options.ReturnDocument = (ReturnDocument)Enum.Parse(typeof(ReturnDocument), argument.Value.AsString);
                        break;

                    case "collation":
                        _options.Collation = Collation.FromBsonDocument(argument.Value.AsBsonDocument);
                        break;

                    case "arrayFilters":
                        var arrayFilters = new List<ArrayFilterDefinition>();
                        foreach (var arrayFilter in argument.Value.AsBsonArray)
                        {
                            var arrayFilterDefinition = new BsonDocumentArrayFilterDefinition<BsonDocument>(arrayFilter.AsBsonDocument);
                            arrayFilters.Add(arrayFilterDefinition);
                        }
                        _options.ArrayFilters = arrayFilters;
                        break;

                    default:
                        throw new ArgumentException($"Unexpected argument: {argument.Name}.");
                }
            }
        }

        // protected methods
        protected override void ExecuteAsync(IMongoCollection<BsonDocument> collection)
        {
            _result = collection.FindOneAndUpdateAsync(_filter, _update, _options).GetAwaiter().GetResult();
        }

        protected override void ExecuteSync(IMongoCollection<BsonDocument> collection)
        {
            _result = collection.FindOneAndUpdate(_filter, _update, _options);
        }

        protected override void VerifyResult(BsonDocument result)
        {
            _result.Should().Be(result);
        }
    }
}
