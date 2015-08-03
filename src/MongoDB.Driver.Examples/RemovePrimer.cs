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
using NUnit.Framework;

namespace MongoDB.Driver.Examples
{
    [TestFixture]
    public class RemovePrimer : PrimerTestFixture
    {
        [Test]
        [AltersCollection]
        public async Task RemoveMatchingDocument()
        {
            // @begin: remove-matching-documents
            // @code: start
            var collection = _database.GetCollection<BsonDocument>("restaurants");
            var filter = Builders<BsonDocument>.Filter.Eq("borough", "Manhattan");
            var result = await collection.DeleteManyAsync(filter);
            // @code: end

            // @results: start
            result.DeletedCount.Should().Be(10259);
            // @results: end

            // @end: remove-matching-documents
        }

        [Test]
        [AltersCollection]
        public async Task RemoveAllDocuments()
        {
            // @begin: remove-all-documents
            // @code: start
            var collection = _database.GetCollection<BsonDocument>("restaurants");
            var filter = new BsonDocument();
            var result = await collection.DeleteManyAsync(filter);
            // @code: end

            // @results: start
            result.DeletedCount.Should().Be(25359);
            // @results: end

            // @end: remove-all-documents
        }

        [Test]
        [AltersCollection]
        public async Task DropCollection()
        {
            // @begin: drop-collection
            // @code: start
            await _database.DropCollectionAsync("restaurants");
            // @code: end

            // @results: start
            using (var cursor = await _database.ListCollectionsAsync())
            {
                var collections = await cursor.ToListAsync();
                collections.Should().NotContain(document => document["name"] == "restaurants");
            }
            // @results: end

            // @end: drop-collection
        }
    }
}
