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
                    return TryParseOptions((BsonDocument)value);
                case "requests":
                    return TryParseRequests((BsonArray)value);
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
            List<BulkWriteUpsert> upserts = new List<BulkWriteUpsert>();

            foreach (var element in expectedResult.AsBsonDocument)
            {
                switch (element.Name)
                {
                    case "deletedCount":
                        deletedCount = element.Value.ToInt64();
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
                        if (element.Value.ToInt64() != 0)
                        {
                            throw new ArgumentException($"Unexpected value for upsertedCount: {element.Value.ToJson()}.");
                        }
                        break;

                    case "upsertedIds":
                        if (element.Value.AsBsonDocument.ElementCount != 0)
                        {
                            throw new ArgumentException($"Unexpected value for upsertedIds: {element.Value.ToJson()}.");
                        }
                        break;

                    default:
                        throw new ArgumentException($"Unexpected result field: \"{element.Name}\".");
                }
            }

            return new BulkWriteResult<BsonDocument>.Acknowledged(requestCount, matchedCount, deletedCount, insertedCount, modifiedCount, _requests, upserts);
        }

        protected override BulkWriteResult<BsonDocument> ExecuteAndGetResult(IMongoCollection<BsonDocument> collection, bool async)
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
        private bool TryParseArguments(
            BsonDocument value,
            out FilterDefinition<BsonDocument> filter,
            out UpdateDefinition<BsonDocument> update,
            out List<ArrayFilterDefinition> arrayFilters)
        {
            arrayFilters = null;
            filter = null;
            update = null;

            foreach (BsonElement argument in value["arguments"].AsBsonDocument)
            {
                switch (argument.Name)
                {
                    case "arrayFilters":
                        if (!TryParseArrayFilters(argument.Value.AsBsonArray, out arrayFilters))
                        {
                            return false;
                        }
                        break;
                    case "filter":
                        if (!TryParseFilter(argument.Value.AsBsonDocument, out filter))
                        {
                            return false;
                        }
                        break;
                    case "update":
                        if (!TryParseUpdate(argument.Value.AsBsonDocument, out update))
                        {
                            return false;
                        }
                        break;
                    default:
                        return false;
                }
            }

            return true;
        }

        private bool TryParseArrayFilters(BsonArray values, out List<ArrayFilterDefinition> arrayFilters)
        {
            arrayFilters = new List<ArrayFilterDefinition>();

            foreach (BsonDocument value in values)
            {
                var arrayFilter = new BsonDocumentArrayFilterDefinition<BsonValue>(value);
                arrayFilters.Add(arrayFilter);
            }

            return true;
        }

        private bool TryParseFilter(BsonDocument value, out FilterDefinition<BsonDocument> filter)
        {
            filter = new BsonDocumentFilterDefinition<BsonDocument>(value);
            return true;
        }

        private bool TryParseOptions(BsonDocument value)
        {
            foreach (var element in value)
            {
                switch (element.Name)
                {
                    case "ordered":
                        _options.IsOrdered = element.Value.ToBoolean();
                        break;
                    default:
                        return false;
                }
            }

            return true;
        }

        private bool TryParseRequests(BsonArray value)
        {
            _requests = new List<WriteModel<BsonDocument>>();

            foreach (BsonDocument requestValue in value)
            {
                WriteModel<BsonDocument> request;
                if (TryParseWriteModel(requestValue, out request))
                {
                    _requests.Add(request);
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private bool TryParseUpdate(BsonDocument value, out UpdateDefinition<BsonDocument> update)
        {
            update = new BsonDocumentUpdateDefinition<BsonDocument>(value);
            return true;
        }

        private bool TryParseUpdateManyModel(BsonDocument value, out WriteModel<BsonDocument> model)
        {
            FilterDefinition<BsonDocument> filter;
            UpdateDefinition<BsonDocument> update;
            List<ArrayFilterDefinition> arrayFilters;

            if (TryParseArguments(value, out filter, out update, out arrayFilters))
            {
                model = new UpdateManyModel<BsonDocument>(filter, update) { ArrayFilters = arrayFilters };
                return true;
            }
            else
            {
                model = null;
                return false;
            }
        }

        private bool TryParseUpdateOneModel(BsonDocument value, out WriteModel<BsonDocument> model)
        {
            FilterDefinition<BsonDocument> filter;
            UpdateDefinition<BsonDocument> update;
            List<ArrayFilterDefinition> arrayFilters;

            if (TryParseArguments(value, out filter, out update, out arrayFilters))
            {
                model = new UpdateOneModel<BsonDocument>(filter, update) { ArrayFilters = arrayFilters };
                return true;
            }
            else
            {
                model = null;
                return false;
            }
        }

        private bool TryParseWriteModel(BsonDocument value, out WriteModel<BsonDocument> request)
        {
            var name = value["name"].AsString;
            switch (name)
            {
                case "updateMany":
                    return TryParseUpdateManyModel(value, out request);
                case "updateOne":
                    return TryParseUpdateOneModel(value, out request);
                default:
                    request = null;
                    return false;
            }
        }
    }
}
