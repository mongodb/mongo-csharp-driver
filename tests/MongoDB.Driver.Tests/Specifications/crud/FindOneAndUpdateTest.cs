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

using System;
using FluentAssertions;
using MongoDB.Bson;
using System.Collections.Generic;
using System.Linq;

namespace MongoDB.Driver.Tests.Specifications.crud
{
    public class FindOneAndUpdateTest : CrudOperationWithResultTestBase<BsonDocument>
    {
        private BsonDocument _filter;
        private FindOneAndUpdateOptions<BsonDocument> _options = new FindOneAndUpdateOptions<BsonDocument>();
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
                case "projection":
                    _options.Projection = (BsonDocument)value;
                    return true;
                case "sort":
                    _options.Sort = (BsonDocument)value;
                    return true;
                case "upsert":
                    _options.IsUpsert = value.ToBoolean();
                    return true;
                case "returnDocument":
                    _options.ReturnDocument = (ReturnDocument)Enum.Parse(typeof(ReturnDocument), value.ToString());
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

        protected override BsonDocument ConvertExpectedResult(BsonValue expectedResult)
        {
            if (expectedResult.IsBsonNull)
            {
                return null;
            }

            return (BsonDocument)expectedResult;
        }

        protected override BsonDocument ExecuteAndGetResult(IMongoDatabase database, IMongoCollection<BsonDocument> collection, bool async)
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
                    .FindOneAndUpdateAsync(_filter, updateDefinition, _options)
                    .GetAwaiter()
                    .GetResult();
            }
            else
            {
                return collection.FindOneAndUpdate(_filter, updateDefinition, _options);
            }
        }

        protected override void VerifyResult(BsonDocument actualResult, BsonDocument expectedResult)
        {
            actualResult.Should().Be(expectedResult);
        }
    }
}
