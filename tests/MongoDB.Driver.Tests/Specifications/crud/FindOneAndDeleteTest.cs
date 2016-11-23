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

using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;

namespace MongoDB.Driver.Tests.Specifications.crud
{
    public class FindOneAndDeleteTest : CrudOperationWithResultTestBase<BsonDocument>
    {
        private BsonDocument _filter;
        private FindOneAndDeleteOptions<BsonDocument> _options = new FindOneAndDeleteOptions<BsonDocument>();

        protected override bool TrySetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "filter":
                    _filter = (BsonDocument)value;
                    return true;
                case "projection":
                    _options.Projection = (BsonDocument)value;
                    return true;
                case "sort":
                    _options.Sort = (BsonDocument)value;
                    return true;
                case "collation":
                    _options.Collation = Collation.FromBsonDocument(value.AsBsonDocument);
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

        protected override BsonDocument ExecuteAndGetResult(IMongoCollection<BsonDocument> collection, bool async)
        {
            if (async)
            {
                return collection.FindOneAndDeleteAsync(_filter, _options).GetAwaiter().GetResult();
            }
            else
            {
                return collection.FindOneAndDelete(_filter, _options);
            }
        }

        protected override void VerifyResult(BsonDocument actualResult, BsonDocument expectedResult)
        {
            actualResult.Should().Be(expectedResult);
        }
    }
}
