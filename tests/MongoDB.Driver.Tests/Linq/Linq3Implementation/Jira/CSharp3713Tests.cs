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
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp3713Tests
    {
        [Fact]
        public void DefaultIfEmpty_should_work()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            var collection = database.GetCollection<C>(DriverTestConfiguration.CollectionNamespace.CollectionName);
            var subject = collection.AsQueryable();

            database.DropCollection(collection.CollectionNamespace.CollectionName);
            collection.InsertMany(new[] {
                new C { Id = 1, InnerArray = new A[0] },
                new C { Id = 2, InnerArray = new[] { new A { S = "abc" } } }
            });

            var queryable = subject.SelectMany(outerObject => outerObject.InnerArray.DefaultIfEmpty(), (o, a) => new { o, a });

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $map : { input : { $cond : { if : { $eq : [{ $size : '$InnerArray' }, 0] }, then : [null], else : '$InnerArray' } }, as : 'a', in : { o : '$$ROOT', a : '$$a' } } }, _id : 0 } }",
                "{ $unwind : '$_v' }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);

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
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            var collection = database.GetCollection<C>(DriverTestConfiguration.CollectionNamespace.CollectionName);
            var subject = collection.AsQueryable();

            database.DropCollection(collection.CollectionNamespace.CollectionName);
            collection.InsertMany(new[] {
                new C { Id = 1, InnerArray = new A[0] },
                new C { Id = 2, InnerArray = new[] { new A { S = "abc" } } }
            });

            var defaultValue = new A { S = "default" };
            var queryable = subject.SelectMany(outerObject => outerObject.InnerArray.DefaultIfEmpty(defaultValue), (o, a) => new { o, a });

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $map : { input : { $cond : { if : { $eq : [{ $size : '$InnerArray' }, 0] }, then : [{ S : 'default' }], else : '$InnerArray' } }, as : 'a', in : { o : '$$ROOT', a : '$$a' } } }, _id : 0 } }",
                "{ $unwind : '$_v' }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);

            var result = queryable.ToList();
            result.Count.Should().Be(2);
            result[0].o.Id.Should().Be(1);
            result[0].a.S.Should().Be("default");
            result[1].o.Id.Should().Be(2);
            result[1].a.S.Should().Be("abc");
        }

        private class C
        {
            public int Id { get; set; }
            public A[] InnerArray { get; set; }
        }

        private class A
        {
            public string S { get; set; }
        }
    }
}
