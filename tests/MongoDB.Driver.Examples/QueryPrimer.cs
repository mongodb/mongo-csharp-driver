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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using Xunit;

namespace MongoDB.Driver.Examples
{
    public class QueryPrimer : PrimerTestFixture
    {
        [Fact]
        public async Task QueryAll()
        {
            // @begin: query-all
            // @code: start
            var collection = __database.GetCollection<BsonDocument>("restaurants");
            var filter = new BsonDocument();
            var count = 0;
            using (var cursor = await collection.FindAsync(filter))
            {
                while (await cursor.MoveNextAsync())
                {
                    var batch = cursor.Current;
                    foreach (var document in batch)
                    {
                        // process document
                        count++;
                    }
                }
            }
            // @code: end

            // @results: start
            count.Should().Be(25359);
            // @results: end

            // @end: query-all
        }

        [Fact]
        public async Task LogicalAnd()
        {
            // @begin: logical-and
            // @code: start
            var collection = __database.GetCollection<BsonDocument>("restaurants");
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("cuisine", "Italian") & builder.Eq("address.zipcode", "10075");
            var result = await collection.Find(filter).ToListAsync();
            // @code: end

            // @results: start
            result.Count().Should().Be(15);
            // @results: end

            // @end: logical-and
        }

        [Fact]
        public async Task LogicalOr()
        {
            // @begin: logical-or
            // @code: start
            var collection = __database.GetCollection<BsonDocument>("restaurants");
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("cuisine", "Italian") | builder.Eq("address.zipcode", "10075");
            var result = await collection.Find(filter).ToListAsync();
            // @code: end

            // @results: start
            result.Count().Should().Be(1153);
            // @results: end

            // @end: logical-or
        }

        [Fact]
        public async Task QueryTopLevelField()
        {
            // @begin: query-top-level-field
            // @code: start
            var collection = __database.GetCollection<BsonDocument>("restaurants");
            var filter = Builders<BsonDocument>.Filter.Eq("borough", "Manhattan");
            var result = await collection.Find(filter).ToListAsync();
            // @code: end

            // @results: start
            result.Count().Should().Be(10259);
            // @results: end

            // @end: query-top-level-field
        }

        [Fact]
        public async Task QueryEmbeddedDocument()
        {
            // @begin: query-embedded-document
            // @code: start
            var collection = __database.GetCollection<BsonDocument>("restaurants");
            var filter = Builders<BsonDocument>.Filter.Eq("address.zipcode", "10075");
            var result = await collection.Find(filter).ToListAsync();
            // @code: end

            // @results: start
            result.Count().Should().Be(99);
            // @results: end

            // @end: query-embedded-document
        }

        [Fact]
        public async Task QueryFieldInArray()
        {
            // @begin: query-field-in-array
            // @code: start
            var collection = __database.GetCollection<BsonDocument>("restaurants");
            var filter = Builders<BsonDocument>.Filter.Eq("grades.grade", "B");
            var result = await collection.Find(filter).ToListAsync();
            // @code: end

            // @results: start
            result.Count().Should().Be(8280);
            // @results: end

            // @end: query-field-in-array
        }

        [Fact]
        public async Task GreaterThan()
        {
            // @begin: greater-than
            // @code: start
            var collection = __database.GetCollection<BsonDocument>("restaurants");
            var filter = Builders<BsonDocument>.Filter.Gt("grades.score", 30);
            var result = await collection.Find(filter).ToListAsync();
            // @code: end

            // @results: start
            result.Count().Should().Be(1959);
            // @results: end

            // @end: greater-than
        }

        [Fact]
        public async Task LessThan()
        {
            // @begin: less-than
            // @code: start
            var collection = __database.GetCollection<BsonDocument>("restaurants");
            var filter = Builders<BsonDocument>.Filter.Lt("grades.score", 10);
            var result = await collection.Find(filter).ToListAsync();
            // @code: end

            // @results: start
            result.Count().Should().Be(19065);
            // @results: end

            // @end: less-than
        }

        [Fact]
        public async Task Sort()
        {
            // @begin: sort
            // @code: start
            var collection = __database.GetCollection<BsonDocument>("restaurants");
            var filter = new BsonDocument();
            var sort = Builders<BsonDocument>.Sort.Ascending("borough").Ascending("address.zipcode");
            var result = await collection.Find(filter).Sort(sort).ToListAsync();
            // @code: end

            // @results: start
            Func<BsonDocument, BsonDocument> keyFunc = document => new BsonDocument { { "borough", document["borough"] }, { "address.zipcode", document.GetValue("address.zipcode", "") } };
            IsInAscendingOrder(result, keyFunc).Should().BeTrue();
            // @results: end

            // @end: sort
        }

        // helper methods
        private bool IsInAscendingOrder(List<BsonDocument> documents, Func<BsonDocument, BsonDocument> keyFunc)
        {
            BsonDocument previousKey = null;
            foreach (var document in documents)
            {
                if (previousKey == null)
                {
                    previousKey = keyFunc(document);
                }
                else
                {
                    var key = keyFunc(document);
                    if (key < previousKey)
                    {
                        return false;
                    }
                    previousKey = key;
                }
            }

            return true;
        }
    }
}
