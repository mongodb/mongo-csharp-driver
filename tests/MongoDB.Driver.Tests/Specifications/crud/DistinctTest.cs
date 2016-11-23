/* Copyright 2010-2015 MongoDB Inc.
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
    public class DistinctTest : CrudOperationWithResultTestBase<List<BsonValue>>
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
                case "collation":
                    _options.Collation = Collation.FromBsonDocument(value.AsBsonDocument);
                    return true;
            }

            return false;
        }

        protected override List<BsonValue> ConvertExpectedResult(BsonValue expectedResult)
        {
            return ((BsonArray)expectedResult).ToList();
        }

        protected override List<BsonValue> ExecuteAndGetResult(IMongoCollection<BsonDocument> collection, bool async)
        {
            var filter = _filter ?? new BsonDocument();
            if (async)
            {
                var cursor = collection.DistinctAsync<BsonValue>(_fieldName, filter, _options).GetAwaiter().GetResult();
                return cursor.ToListAsync().GetAwaiter().GetResult();
            }
            else
            {
                return collection.Distinct<BsonValue>(_fieldName, filter, _options).ToList();
            }
        }

        protected override void VerifyResult(List<BsonValue> actualResult, List<BsonValue> expectedResult)
        {
            actualResult.Should().Equal(expectedResult);
        }
    }
}
