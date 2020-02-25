/* Copyright 2010-present MongoDB Inc.
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
using System.Collections.Generic;
using System.Linq;

namespace MongoDB.Driver.Tests.Specifications.crud
{
    public class UpdateManyTest : CrudOperationWithResultTestBase<UpdateResult>
    {
        private BsonDocument _filter;
        private UpdateOptions _options = new UpdateOptions();
        private BsonValue _update;

        protected override bool TrySetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "filter":
                    _filter = (BsonDocument)value;
                    return true;
                case "update":
                    _update = value;
                    return true;
                case "upsert":
                    _options.IsUpsert = value.ToBoolean();
                    return true;
                case "collation":
                    _options.Collation = Collation.FromBsonDocument(value.AsBsonDocument);
                    return true;
                case "arrayFilters":
                    var arrayFilters = new List<ArrayFilterDefinition>();
                    foreach (BsonDocument arrayFilterDocument in value.AsBsonArray)
                    {
                        var arrayFilter = new BsonDocumentArrayFilterDefinition<BsonDocument>(arrayFilterDocument);
                        arrayFilters.Add(arrayFilter);
                    }
                    _options.ArrayFilters = arrayFilters;
                    return true;
                case "hint":
                    _options.Hint = value;
                    return true;
            }

            return false;
        }

        protected override UpdateResult ConvertExpectedResult(BsonValue expectedResult)
        {
            BsonValue modifiedCountValue;
            long? modifiedCount = null;
            if (((BsonDocument)expectedResult).TryGetValue("modifiedCount", out modifiedCountValue))
            {
                modifiedCount = modifiedCountValue.ToInt64();
            }
            BsonValue upsertedId = null;
            ((BsonDocument)expectedResult).TryGetValue("upsertedId", out upsertedId);
            return new UpdateResult.Acknowledged(expectedResult["matchedCount"].ToInt64(), modifiedCount, upsertedId);
        }

        protected override UpdateResult ExecuteAndGetResult(IMongoDatabase database, IMongoCollection<BsonDocument> collection, bool async)
        {
            UpdateDefinition<BsonDocument> updateDefinition = null;
            if (_update is BsonDocument updateDocument)
            {
                updateDefinition = new BsonDocumentUpdateDefinition<BsonDocument>(updateDocument);
            }
            else if (_update is BsonArray stages)
            {
                var pipeline = new BsonDocumentStagePipelineDefinition<BsonDocument, BsonDocument>(stages.Cast<BsonDocument>());
                updateDefinition = new PipelineUpdateDefinition<BsonDocument>(pipeline);
            }

            if (async)
            {
                return collection
                    .UpdateManyAsync(_filter, updateDefinition, _options)
                    .GetAwaiter()
                    .GetResult();
            }
            else
            {
                return collection.UpdateMany(_filter, updateDefinition, _options);
            }
        }

        protected override void VerifyResult(UpdateResult actualResult, UpdateResult expectedResult)
        {
            if (actualResult.IsModifiedCountAvailable)
            {
                actualResult.ModifiedCount.Should().Be(expectedResult.ModifiedCount);
            }
            actualResult.MatchedCount.Should().Be(expectedResult.MatchedCount);
            actualResult.UpsertedId.Should().Be(expectedResult.UpsertedId);
        }
    }
}
