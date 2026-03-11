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
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp3713Tests : LinqIntegrationTest<CSharp3713Tests.ClassFixture>
    {
        public CSharp3713Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void DefaultIfEmpty_should_work()
        {
            var collection = Fixture.Collection;
            var subject = collection.AsQueryable();

            var queryable = subject.SelectMany(outerObject => outerObject.InnerArray.DefaultIfEmpty(), (o, a) => new { o, a });

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $map : { input : { $cond : { if : { $eq : [{ $size : '$InnerArray' }, 0] }, then : [null], else : '$InnerArray' } }, as : 'a', in : { o : '$$ROOT', a : '$$a' } } }, _id : 0 } }",
                "{ $unwind : '$_v' }"
            };
            AssertStages(stages, expectedStages);

            var result = queryable.ToList();
            result.Count.Should().Be(2);
            result[0].o.Id.Should().Be(1);
            result[0].a.Should().Be(null);
            result[1].o.Id.Should().Be(2);
            result[1].a.S.Should().Be("abc");
        }

        [Fact]
        public void DefaultIfEmpty_with_explicit_default_should_work()
        {
            var collection = Fixture.Collection;
            var subject = collection.AsQueryable();

            var defaultValue = new A { S = "default" };
            var queryable = subject.SelectMany(outerObject => outerObject.InnerArray.DefaultIfEmpty(defaultValue), (o, a) => new { o, a });

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $map : { input : { $cond : { if : { $eq : [{ $size : '$InnerArray' }, 0] }, then : [{ S : 'default' }], else : '$InnerArray' } }, as : 'a', in : { o : '$$ROOT', a : '$$a' } } }, _id : 0 } }",
                "{ $unwind : '$_v' }"
            };
            AssertStages(stages, expectedStages);

            var result = queryable.ToList();
            result.Count.Should().Be(2);
            result[0].o.Id.Should().Be(1);
            result[0].a.S.Should().Be("default");
            result[1].o.Id.Should().Be(2);
            result[1].a.S.Should().Be("abc");
        }

        public class C
        {
            public int Id { get; set; }
            public A[] InnerArray { get; set; }
        }

        public class A
        {
            public string S { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<C>
        {
            protected override IEnumerable<C> InitialData =>
            [
                new C { Id = 1, InnerArray = new A[0] },
                new C { Id = 2, InnerArray = new[] { new A { S = "abc" } } }
            ];
        }
    }
}
