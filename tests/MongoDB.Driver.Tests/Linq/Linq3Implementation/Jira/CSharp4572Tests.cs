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
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    [Collection(RegisterObjectSerializerFixture.CollectionName)]
    public class CSharp4572Tests : LinqIntegrationTest<CSharp4572Tests.ClassFixture>
    {
        public CSharp4572Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void Builders_Filter_Eq_with_downcast_should_work()
        {
            var collection = Fixture.Collection;
            var filter = Builders<IMyInterface>.Filter.Eq(x => ((MyClass)x).TestValue, 42);

            var find = collection.Find(filter);

            var renderedFilter = TranslateFindFilter(collection, find);
            renderedFilter.Should().Be("{ TestValue : 42 }");

            var results = find.ToList();
            results.Select(x => x.Id).Should().Equal(2);
        }

        [Fact]
        public void Where_with_downcast_should_work()
        {
            var collection = Fixture.Collection;
            var filter = Builders<IMyInterface>.Filter.Eq(x => ((MyClass)x).TestValue, 42);

            var queryable = collection.AsQueryable()
                .Where(x => ((MyClass)x).TestValue == 42);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { TestValue : 42 } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2);
        }

        public interface IMyInterface
        {
            int Id { get; set; }
        }

        public class MyClass : IMyInterface
        {
            public int Id { get; set; }
            public int TestValue { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<IMyInterface>
        {
            protected override IEnumerable<IMyInterface> InitialData =>
            [
                new MyClass { Id = 1, TestValue = 1 },
                new MyClass { Id = 2, TestValue = 42 }
            ];
        }
    }
}
