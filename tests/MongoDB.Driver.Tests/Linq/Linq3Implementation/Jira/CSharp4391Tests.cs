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
using MongoDB.Bson.Serialization;
using MongoDB.Bson;
using Xunit;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.TestHelpers;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4391Tests : LinqIntegrationTest<CSharp4391Tests.ClassFixture>
    {
        public CSharp4391Tests(ClassFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public void Serializing_struct_with_constructor_should_work()
        {
            var s = new S(1, 2, 3);

            var result = s.ToJson();

            result.Should().Be("{ \"_id\" : 1, \"X\" : 2, \"Y\" : 3 }");
        }

        [Fact]
        public void Serializing_struct_with_partial_constructor_should_work()
        {
            var t = new T(1, 2) { Y = 3 };

            var result = t.ToJson();

            result.Should().Be("{ \"Y\" : 3, \"_id\" : 1, \"X\" : 2 }");
        }

        [Fact]
        public void Serializing_struct_with_no_constructor_should_work()
        {
            var u = new U() { Id = 1, X = 2, Y = 3 };

            var result = u.ToJson();

            result.Should().Be("{ \"_id\" : 1, \"X\" : 2, \"Y\" : 3 }");
        }

        [Fact]
        public void Deserializing_struct_with_constructor_should_work()
        {
            var json = "{ _id : 1, X : 2, Y : 3 }";

            var result = BsonSerializer.Deserialize<S>(json);

            result.Should().Be(new S(1, 2, 3));
        }

        [Fact]
        public void Deserializing_struct_with_partial_constructor_should_work()
        {
            var json = "{ _id : 1, X : 2, Y : 3 }";

            var result = BsonSerializer.Deserialize<T>(json);

            result.Id.Should().Be(1);
            result.X.Should().Be(2);
            result.Y.Should().Be(3);
        }

        [Fact]
        public void Deserializing_struct_with_no_constructor_should_work()
        {
            var json = "{ _id : 1, X : 2, Y : 3 }";

            var result = BsonSerializer.Deserialize<U>(json);

            result.Id.Should().Be(1);
            result.X.Should().Be(2);
            result.Y.Should().Be(3);
        }

        [Fact]
        public void Queryable_with_struct_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection
                .AsQueryable();

            var stages = Translate(collection, queryable);
            AssertStages(stages, new string[0]);

            var results = queryable.ToList();
            results.Should().Equal(new S(1, 2, 3), new S(2, 3, 4));
        }

        [Fact]
        public void Queryable_with_projected_struct_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection
                .AsQueryable()
                .Select(x => new S(x.Id, x.X + 1, x.Y + 1));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _id : '$_id', X : { $add : ['$X', 1] }, Y : { $add : ['$Y', 1] } } }");

            var results = queryable.ToList();
            results.Should().Equal(new S(1, 3, 4), new S(2, 4, 5));
        }

        public struct S
        {
            public S(int id, int x, int y)
            {
                Id = id;
                X = x;
                Y = y;
            }

            public int Id { get; }
            public int X { get; }
            public int Y { get; }
        }

        private struct T
        {
            [BsonConstructor]
            public T(int id, int x)
            {
                Id = id;
                X = x;
                Y = 0;
            }

            public int Id { get; }
            [BsonElement("X")] public int X { get; }
            public int Y { get; set; }
        }

        private struct U
        {
            public int Id { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<S>
        {
            protected override IEnumerable<S> InitialData =>
            [
                new S(1, 2, 3),
                new S(2, 3, 4)
            ];
        }
    }
}
