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

using System;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Tests.Specifications.crud
{
    public class FindOneAndUpdateTest : CrudOperationWithResultTestBase<BsonDocument>
    {
        private BsonDocument _filter;
        private BsonDocument _update;
        private FindOneAndUpdateOptions<BsonDocument> _options = new FindOneAndUpdateOptions<BsonDocument>();

        protected override bool TrySetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "filter":
                    _filter = (BsonDocument)value;
                    return true;
                case "update":
                    _update = (BsonDocument)value;
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

        protected override BsonDocument ExecuteAndGetResult(IMongoCollection<BsonDocument> collection, bool async)
        {
            if (async)
            {
                return collection.FindOneAndUpdateAsync(_filter, _update, _options).GetAwaiter().GetResult();
            }
            else
            {
                return collection.FindOneAndUpdate(_filter, _update, _options);
            }
        }

        protected override void VerifyResult(BsonDocument actualResult, BsonDocument expectedResult)
        {
            actualResult.Should().Be(expectedResult);
        }
    }
}
