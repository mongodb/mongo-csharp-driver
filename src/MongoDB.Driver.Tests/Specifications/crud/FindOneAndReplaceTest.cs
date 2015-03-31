﻿/* Copyright 2010-2014 MongoDB Inc.
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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Tests.Specifications.crud
{
    public class FindOneAndReplaceTest : CrudOperationWithResultTestBase<BsonDocument>
    {
        private BsonDocument _filter;
        private BsonDocument _replacement;
        private FindOneAndReplaceOptions<BsonDocument> _options = new FindOneAndReplaceOptions<BsonDocument>();

        protected override bool TrySetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "filter":
                    _filter = (BsonDocument)value;
                    return true;
                case "replacement":
                    _replacement = (BsonDocument)value;
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
            }

            return false;
        }

        protected override BsonDocument ConvertExpectedResult(BsonValue expectedResult)
        {
            if (expectedResult.IsBsonNull)
            {
                if (ClusterDescription.Servers[0].Version < new SemanticVersion(3, 0, 0) && _options.IsUpsert)
                {
                    return new BsonDocument();
                }
                return null;
            }

            return (BsonDocument)expectedResult;
        }

        protected override Task<BsonDocument> ExecuteAndGetResultAsync(IMongoCollection<BsonDocument> collection)
        {
            return collection.FindOneAndReplaceAsync(_filter, _replacement, _options);
        }

        protected override void VerifyResult(BsonDocument actualResult, BsonDocument expectedResult)
        {
            actualResult.Should().Be(expectedResult);
        }

        protected override async Task VerifyCollectionAsync(IMongoCollection<BsonDocument> collection, BsonArray expectedData)
        {
            var data = await collection.Find("{}").ToListAsync();

            if (ClusterDescription.Servers[0].Version < new SemanticVersion(2, 6, 0) && _options.IsUpsert)
            {
                foreach(var doc in data)
                {
                    doc.Remove("_id");
                }

                foreach(var doc in expectedData.Cast<BsonDocument>())
                {
                    doc.Remove("_id");
                }
            }

            data.Should().BeEquivalentTo(expectedData);
        }
    }
}
