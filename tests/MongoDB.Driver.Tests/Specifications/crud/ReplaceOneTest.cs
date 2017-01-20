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

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Tests.Specifications.crud
{
    public class ReplaceOneTest : CrudOperationWithResultTestBase<ReplaceOneResult>
    {
        private BsonDocument _filter;
        private BsonDocument _replacement;
        private UpdateOptions _options = new UpdateOptions();

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
                case "upsert":
                    _options.IsUpsert = value.ToBoolean();
                    return true;
                case "collation":
                    _options.Collation = Collation.FromBsonDocument(value.AsBsonDocument);
                    return true;
            }

            return false;
        }

        protected override ReplaceOneResult ConvertExpectedResult(BsonValue expectedResult)
        {
            BsonValue modifiedCountValue;
            long? modifiedCount = null;
            if (((BsonDocument)expectedResult).TryGetValue("modifiedCount", out modifiedCountValue))
            {
                modifiedCount = modifiedCountValue.ToInt64();
            }
            BsonValue upsertedId = null;
            ((BsonDocument)expectedResult).TryGetValue("upsertedId", out upsertedId);
            return new ReplaceOneResult.Acknowledged(expectedResult["matchedCount"].ToInt64(), modifiedCount, upsertedId);
        }

        protected override ReplaceOneResult ExecuteAndGetResult(IMongoCollection<BsonDocument> collection, bool async)
        {
            if (async)
            {
                return collection.ReplaceOneAsync(_filter, _replacement, _options).GetAwaiter().GetResult();
            }
            else
            {
                return collection.ReplaceOne(_filter, _replacement, _options);
            }
        }

        protected override void VerifyResult(ReplaceOneResult actualResult, ReplaceOneResult expectedResult)
        {
            if (actualResult.IsModifiedCountAvailable)
            {
                actualResult.ModifiedCount.Should().Be(expectedResult.ModifiedCount);
            }
            actualResult.MatchedCount.Should().Be(expectedResult.MatchedCount);

            if (ClusterDescription.Servers[0].Version >= new SemanticVersion(2, 6, 0) || !_options.IsUpsert)
            {
                actualResult.UpsertedId.Should().Be(expectedResult.UpsertedId);
            }
        }

        protected override void VerifyCollection(IMongoCollection<BsonDocument> collection, BsonArray expectedData)
        {
            var data = collection.FindSync("{}").ToList();

            if (ClusterDescription.Servers[0].Version < new SemanticVersion(2, 6, 0) && _options.IsUpsert)
            {
                foreach (var doc in data)
                {
                    doc.Remove("_id");
                }

                foreach (var doc in expectedData.Cast<BsonDocument>())
                {
                    doc.Remove("_id");
                }
            }

            data.Should().BeEquivalentTo(expectedData);
        }
    }
}
