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
    public class UpdateOneTest : CrudOperationWithResultTestBase<UpdateResult>
    {
        private BsonDocument _filter;
        private BsonDocument _update;
        private UpdateOptions _options = new UpdateOptions();

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
                case "upsert":
                    _options.IsUpsert = value.ToBoolean();
                    return true;
                case "collation":
                    _options.Collation = Collation.FromBsonDocument(value.AsBsonDocument);
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

        protected override UpdateResult ExecuteAndGetResult(IMongoCollection<BsonDocument> collection, bool async)
        {
            if (async)
            {
                return collection.UpdateOneAsync(_filter, _update, _options).GetAwaiter().GetResult();
            }
            else
            {
                return collection.UpdateOne(_filter, _update, _options);
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
