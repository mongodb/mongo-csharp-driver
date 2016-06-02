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
    public class AggregatePrimer : PrimerTestFixture
    {
        [Fact]
        public async Task GroupDocumentsByAFieldAndCalculateCount()
        {
            // @begin: group-documents-by-a-field-and-calculate-count
            // @code: start
            var collection = __database.GetCollection<BsonDocument>("restaurants");
            var aggregate = collection.Aggregate().Group(new BsonDocument { { "_id", "$borough" }, { "count", new BsonDocument("$sum", 1) } });
            var results = await aggregate.ToListAsync();
            // @code: end

            // @results: start
            var expectedResults = new[]
            {
                BsonDocument.Parse("{ _id : 'Staten Island', count : 969 }"),
                BsonDocument.Parse("{ _id : 'Brooklyn', count : 6086 }"),
                BsonDocument.Parse("{ _id : 'Manhattan', count : 10259 }"),
                BsonDocument.Parse("{ _id : 'Queens', count : 5656 }"),
                BsonDocument.Parse("{ _id : 'Bronx', count : 2338 }"),
                BsonDocument.Parse("{ _id : 'Missing', count : 51 }")
            };
            results.Should().BeEquivalentTo(expectedResults);
            // @results: end

            // @end: group-documents-by-a-field-and-calculate-count
        }

        [Fact]
        public async Task FilterAndGroupDocuments()
        {
            // @begin: filter-and-group-documents
            // @code: start
            var collection = __database.GetCollection<BsonDocument>("restaurants");
            var aggregate = collection.Aggregate()
                .Match(new BsonDocument { { "borough", "Queens" }, { "cuisine", "Brazilian" } })
                .Group(new BsonDocument { { "_id", "$address.zipcode" }, { "count", new BsonDocument("$sum", 1) } });
            var results = await aggregate.ToListAsync();
            // @code: end

            // @results: start
            var expectedResults = new[]
            {
                BsonDocument.Parse("{ _id : '11368', count : 1 }"),
                BsonDocument.Parse("{ _id : '11106', count : 3 }"),
                BsonDocument.Parse("{ _id : '11377', count : 1 }"),
                BsonDocument.Parse("{ _id : '11103', count : 1 }"),
                BsonDocument.Parse("{ _id : '11101', count : 2 }")
            };
            results.Should().BeEquivalentTo(expectedResults);
            // @results: end

            // @end: filter-and-group-documents
        }
    }
}
