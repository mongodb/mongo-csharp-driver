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
using MongoDB.Driver.Core;
using NUnit.Framework;

namespace MongoDB.Driver.Examples
{
    [TestFixture]
    public class UpdatePrimer : PrimerTestFixture
    {
        [Test]
        [AltersCollection]
        [RequiresServer(MinimumVersion = "2.6.0")]
        public async Task UpdateTopLevelFields()
        {
            // @begin: update-top-level-fields
            // @code: start
            var collection = _database.GetCollection<BsonDocument>("restaurants");
            var filter = Builders<BsonDocument>.Filter.Eq("name", "Juni");
            var update = Builders<BsonDocument>.Update
                .Set("cuisine", "American (New)")
                .CurrentDate("lastModified");
            var result = await collection.UpdateOneAsync(filter, update);
            // @code: end

            // @results: start
            result.MatchedCount.Should().Be(1);
            if (result.IsModifiedCountAvailable)
            {
                result.ModifiedCount.Should().Be(1);
            }
            // @results: end

            // @end: update-top-level-fields
        }

        [Test]
        [AltersCollection]
        public async Task UpdateEmbeddedField()
        {
            // @begin: update-embedded-field
            // @code: start
            var collection = _database.GetCollection<BsonDocument>("restaurants");
            var filter = Builders<BsonDocument>.Filter.Eq("restaurant_id", "41156888");
            var update = Builders<BsonDocument>.Update.Set("address.street", "East 31st Street");
            var result = await collection.UpdateOneAsync(filter, update);
            // @code: end

            // @results: start
            result.MatchedCount.Should().Be(1);
            if (result.IsModifiedCountAvailable)
            {
                result.ModifiedCount.Should().Be(1);
            }
            // @results: end

            // @end: update-embedded-field
        }

        [Test]
        [AltersCollection]
        [RequiresServer(MinimumVersion = "2.6.0")]
        public async Task UpdateMultipleDocuments()
        {
            // @begin: update-multiple-documents
            // @code: start
            var collection = _database.GetCollection<BsonDocument>("restaurants");
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("address.zipcode", "10016") & builder.Eq("cuisine", "Other");
            var update = Builders<BsonDocument>.Update
                .Set("cuisine", "Category To Be Determined")
                .CurrentDate("lastModified");
            var result = await collection.UpdateManyAsync(filter, update);
            // @code: end

            // @results: start
            result.MatchedCount.Should().Be(20);
            if (result.IsModifiedCountAvailable)
            {
                result.ModifiedCount.Should().Be(20);
            }
            // @results: end

            // @end: update-multiple-documents
        }
    }
}
