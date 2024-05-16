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

using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4066Tests : IntegrationTest<MongoDatabaseFixture>
    {
        public CSharp4066Tests(ITestOutputHelper testOutputHelper, MongoDatabaseFixture fixture)
            : base(testOutputHelper, fixture)
        {
        }

        [Fact]
        public void String_comparison_in_filter_should_use_custom_serializer()
        {
            var collection = Fixture.GetCollection<C>();

            var id = "0102030405060708090a0b0c";
            collection.InsertMany(
                new[]
                {
                    new C { Id = id, X = 1 },
                    new C { Id = "000000000000000000000000", X = 2 }
                });

            var find = collection.Find(x => x.Id == id);

            var rendered = find.ToString();
            rendered.Should().Be("find({ \"_id\" : { \"$oid\" : \"0102030405060708090a0b0c\" } })");

            var results = find.ToList();
            results.Count.Should().Be(1);
            results[0].Id.Should().Be(id);
            results[0].X.Should().Be(1);
        }

        public class C
        {
            [BsonRepresentation(BsonType.ObjectId)]
            public string Id { get; set; }
            public int X { get; set; }
        }
    }
}
