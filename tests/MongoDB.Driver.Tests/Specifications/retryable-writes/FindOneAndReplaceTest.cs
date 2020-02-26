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
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Tests.Specifications.retryable_writes
{
    public class FindOneAndReplaceTest : RetryableWriteTestBase
    {
        // private fields
        private BsonDocument _filter;
        private BsonDocument _replacement;
        private FindOneAndReplaceOptions<BsonDocument> _options = new FindOneAndReplaceOptions<BsonDocument>();
        private BsonDocument _result;
        private SemanticVersion _serverVersion;

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
            _result = collection.FindOneAndReplaceAsync(_filter, _replacement, _options).GetAwaiter().GetResult();
            _serverVersion = collection.Database.Client.Cluster.Description.Servers[0].Version;
        }

        protected override void ExecuteSync(IMongoCollection<BsonDocument> collection)
        {
            _result = collection.FindOneAndReplace(_filter, _replacement, _options);
            _serverVersion = collection.Database.Client.Cluster.Description.Servers[0].Version;
        }

        protected override void VerifyCollectionContents(List<BsonDocument> actualContents, List<BsonDocument> expectedContents)
        {
            if (_serverVersion < new SemanticVersion(2, 6, 0) && _options.IsUpsert)
            {
                RemoveIds(actualContents);
                RemoveIds(expectedContents);
            }
            base.VerifyCollectionContents(actualContents, expectedContents);
        }

        protected override void VerifyResult(BsonDocument result)
        {
            _result.Should().Be(result);
        }

        // private methods
        private void RemoveIds(List<BsonDocument> documents)
        {
            foreach (var document in documents)
            {
                document.Remove("_id");
            }
        }
    }
}
