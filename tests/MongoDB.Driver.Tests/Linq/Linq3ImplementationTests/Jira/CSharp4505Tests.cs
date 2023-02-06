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
using MongoDB.Bson;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp4505Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Find_projection_with_client_side_projection_should_throw()
        {
            var collection = CreateCollection();

            var find =
                collection
                .Find("{}")
                .Project(x => new Person(x));

            var exception = Record.Exception(() => find.ToList());

            exception.Should().BeOfType<ExpressionNotSupportedException>();
        }

        [Fact]
        public void Find_projection_with_client_side_projection_factored_out_should_work()
        {
            var collection = CreateCollection();

            var find =
                collection
                .Find("{}");

            var results = find.ToList().Select(x => new Person(x)).ToList();

            results.Should().HaveCount(1);
            results[0].Id.Should().Be(1);
            results[0].Name.Should().Be("Me");
        }

        private IMongoCollection<BsonDocument> CreateCollection()
        {
            var collection = GetCollection<BsonDocument>("people");

            CreateCollection(
                collection,
                new BsonDocument
                {
                    ["_id"] = 1,
                    ["Name"] = "Me",
                });

            return collection;
        }

        public class Person
        {
            public Person(BsonDocument document)
            {
                Id = document["_id"].AsInt32;
                Name = document["Name"].AsString;
            }

            public int Id { get; set; }

            public string Name { get; set; }
        }
    }
}
