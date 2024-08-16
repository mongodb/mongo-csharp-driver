/* Copyright 2010-present MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Search
{
    public class MongoQueryableTests
    {
        [Fact]
        public void Search()
        {
            var subject = CreateSubject();

            var query = subject
                .Search(Builders<Person>.Search.Text(x => x.FirstName, "Alex"));

            query.ToString().Should().EndWith("Aggregate([{ \"$search\" : { \"text\" : { \"query\" : \"Alex\", \"path\" : \"fn\" } } }])");
        }

        [Fact]
        public void SearchMeta()
        {
            var subject = CreateSubject();

            var query = subject
                .SearchMeta(Builders<Person>.Search.Text(x => x.FirstName, "Alex"));

            query.ToString().Should().EndWith("Aggregate([{ \"$searchMeta\" : { \"text\" : { \"query\" : \"Alex\", \"path\" : \"fn\" } } }])");
        }

        [Fact]
        public void VectorSearch()
        {
            var subject = CreateSubject();

            var query = subject
                .VectorSearch(p => p.FirstName, new[] { 123, 456 }, 10, new() { IndexName = "my_index", NumberOfCandidates = 33 });

            query.ToString().Should().EndWith("Aggregate([{ \"$vectorSearch\" : { \"queryVector\" : [123.0, 456.0], \"path\" : \"fn\", \"limit\" : 10, \"numCandidates\" : 33, \"index\" : \"my_index\" } }])");
        }

        private IQueryable<Person> CreateSubject()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            var collection = database.GetCollection<Person>(DriverTestConfiguration.CollectionNamespace.CollectionName);
            return collection.AsQueryable();
        }

        private class Person
        {
            [BsonElement("fn")]
            public string FirstName { get; set; }

            [BsonElement("ln")]
            public string LastName { get; set; }
        }
    }
}
