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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;

namespace MongoDB.Driver.Tests.Specifications.crud
{
    public class FindTest : CrudOperationWithResultTestBase<List<BsonDocument>>
    {
        private BsonDocument _filter;
        private FindOptions<BsonDocument> _options = new FindOptions<BsonDocument>();

        protected override bool TrySetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "filter":
                    _filter = (BsonDocument)value;
                    return true;
                case "sort":
                    _options.Sort = value.ToBsonDocument();
                    return true;
                case "limit":
                    _options.Limit = value.ToInt32();
                    return true;
                case "skip":
                    _options.Skip = value.ToInt32();
                    return true;
                case "batchSize":
                    _options.BatchSize = value.ToInt32();
                    return true;
            }

            return false;
        }

        protected override List<BsonDocument> ConvertExpectedResult(BsonValue expectedResult)
        {
            return ((BsonArray)expectedResult).Select(x => x.ToBsonDocument()).ToList();
        }

        protected override async Task<List<BsonDocument>> ExecuteAndGetResultAsync(IMongoCollection<BsonDocument> collection)
        {
            using (var cursor = await collection.FindAsync(_filter, _options))
            {
                return await cursor.ToListAsync();
            }
        }

        protected override void VerifyResult(List<BsonDocument> actualResult, List<BsonDocument> expectedResult)
        {
            actualResult.Should().Equal(expectedResult);
        }
    }
}
