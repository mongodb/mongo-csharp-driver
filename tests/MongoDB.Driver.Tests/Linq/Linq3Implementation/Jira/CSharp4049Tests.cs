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

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4049Tests : LinqIntegrationTest<CSharp4049Tests.ClassFixture>
    {
        public CSharp4049Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void Aggregate_Project_should_translate_as_expected()
        {
            var collection = Fixture.Collection;
            var aggregate = collection.Aggregate()
                .Project(x => new TestClass { Property = x.Property.ToUpper() });

            var stages = Translate(collection, aggregate);
            AssertStages(stages, "{ $project : { _p : { $toUpper : '$_p' }, _id : 0 } }");

            var results = aggregate.ToList().Single();
            results.Property.Should().Be("ABC");
        }

        [Fact]
        public void Queryable_Select_should_translate_as_expected()
        {
            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Select(x => new TestClass { Property = x.Property.ToUpper() });

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _p : { $toUpper : '$_p' }, _id : 0 } }");

            var results = queryable.ToList().Single();
            results.Property.Should().Be("ABC");
        }

        public class TestClass
        {
            [BsonElement("_p")]
            public string Property { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<TestClass>
        {
            protected override IEnumerable<TestClass> InitialData =>
            [
                new TestClass { Property = "abc" }
            ];
        }
    }
}
