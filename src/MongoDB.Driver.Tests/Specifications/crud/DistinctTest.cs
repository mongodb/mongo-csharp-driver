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
    public class DistinctTest : CrudOperationWithResultTestBase<List<int>>
    {
        private string _fieldName;
        private BsonDocument _filter;
        private DistinctOptions _options = new DistinctOptions();

        protected override bool TrySetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "fieldName":
                    _fieldName = value.ToString();
                    return true;
                case "filter":
                    _filter = (BsonDocument)value;
                    return true;
            }

            return false;
        }

        protected override List<int> ConvertExpectedResult(BsonValue expectedResult)
        {
            return ((BsonArray)expectedResult).Select(x => x.ToInt32()).ToList();
        }

        protected override async Task<List<int>> ExecuteAndGetResultAsync(IMongoCollection<BsonDocument> collection)
        {
            using (var cursor = await collection.DistinctAsync<int>(_fieldName, _filter, _options))
            {
                return await cursor.ToListAsync();
            }
        }

        protected override void VerifyResult(List<int> actualResult, List<int> expectedResult)
        {
            actualResult.Should().Equal(expectedResult);
        }
    }
}
