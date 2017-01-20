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
using Xunit;

namespace MongoDB.Driver.Examples
{
    public class IndexesPrimer : PrimerTestFixture
    {
        [Fact]
        public async Task SingleFieldIndex()
        {
            AltersCollection();

            // @begin: single-field-index
            // @code: start
            var collection = __database.GetCollection<BsonDocument>("restaurants");
            var keys = Builders<BsonDocument>.IndexKeys.Ascending("cuisine");
            await collection.Indexes.CreateOneAsync(keys);
            // @code: end

            // @results: start
            using (var cursor = await collection.Indexes.ListAsync())
            {
                var indexes = await cursor.ToListAsync();
                indexes.Should().Contain(index => index["name"] == "cuisine_1");
            }
            // @results: end

            // @end: single-field-index
        }

        [Fact]
        public async Task CreateCompoundIndex()
        {
            AltersCollection();

            // @begin: create-compound-index
            // @code: start
            var collection = __database.GetCollection<BsonDocument>("restaurants");
            var keys = Builders<BsonDocument>.IndexKeys.Ascending("cuisine").Ascending("address.zipcode");
            await collection.Indexes.CreateOneAsync(keys);
            // @code: end

            // @results: start
            using (var cursor = await collection.Indexes.ListAsync())
            {
                var indexes = await cursor.ToListAsync();
                indexes.Should().Contain(index => index["name"] == "cuisine_1_address.zipcode_1");
            }
            // @results: end

            // @end: create-compound-index
        }
    }
}
