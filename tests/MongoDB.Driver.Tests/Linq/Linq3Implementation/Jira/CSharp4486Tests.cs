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
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4486Tests : Linq3IntegrationTest
    {
        [Fact]
        public void And_with_two_arguments_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => x.P & x.Q);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $and : ['$P', '$Q'] }, _id : 0 } }");

            var result = queryable.Single();
            result.Should().Be(true);
        }

        [Fact]
        public void And_with_three_arguments_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => x.P & x.Q & x.R);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $and : ['$P', '$Q', '$R'] }, _id : 0 } }");

            var result = queryable.Single();
            result.Should().Be(false);
        }

        [Fact]
        public void Not_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => !x.P);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $not : '$P' }, _id : 0 } }");

            var result = queryable.Single();
            result.Should().Be(false);
        }

        [Fact]
        public void Or_with_two_arguments_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => x.P | x.Q);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $or : ['$P', '$Q'] }, _id : 0 } }");

            var result = queryable.Single();
            result.Should().Be(true);
        }

        [Fact]
        public void Or_with_three_arguments_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => x.P | x.Q | x.R);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $or : ['$P', '$Q', '$R'] }, _id : 0 } }");

            var result = queryable.Single();
            result.Should().Be(true);
        }

        [Fact]
        public void Xor_with_two_arguments_should_throw()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => x.P ^ x.Q);

            var exception = Record.Exception(() => Translate(collection, queryable));
            exception.Should().BeOfType<ExpressionNotSupportedException>();
            exception.Message.Should().Contain("because MongoDB does not have an $xor operator");
        }

        [Fact]
        public void BitAnd_with_two_arguments_should_work()
        {
            RequireServer.Check().Supports(Feature.BitwiseOperators);
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => x.X & x.Z);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $bitAnd : ['$X', '$Z'] }, _id : 0 } }");

            var result = queryable.Single();
            result.Should().Be(1);
        }

        [Fact]
        public void BitAnd_with_three_arguments_should_work()
        {
            RequireServer.Check().Supports(Feature.BitwiseOperators);
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => x.X & x.Y & x.Z);

            var stages = Translate(collection, queryable);
            {
                AssertStages(stages, "{ $project : { _v : { $bitAnd : ['$X', '$Y', '$Z'] }, _id : 0 } }");

                var result = queryable.Single();
                result.Should().Be(0);
            }
        }

        [Fact]
        public void BitNot_should_work()
        {
            RequireServer.Check().Supports(Feature.BitwiseOperators);
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => ~x.X);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $bitNot : '$X' }, _id : 0 } }");

            var result = queryable.Single();
            result.Should().Be(~1);
        }

        [Fact]
        public void BitOr_with_two_arguments_should_work()
        {
            RequireServer.Check().Supports(Feature.BitwiseOperators);
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => x.X | x.Z);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $bitOr : ['$X', '$Z'] }, _id : 0 } }");

            var result = queryable.Single();
            result.Should().Be(3);
        }

        [Fact]
        public void BitOr_with_three_arguments_should_work()
        {
            RequireServer.Check().Supports(Feature.BitwiseOperators);
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => x.X | x.Y | x.Z);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $bitOr : ['$X', '$Y', '$Z'] }, _id : 0 } }");

            var result = queryable.Single();
            result.Should().Be(3);
        }

        [Fact]
        public void BitXor_with_two_arguments_should_work()
        {
            RequireServer.Check().Supports(Feature.BitwiseOperators);
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => x.X ^ x.Z);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $bitXor : ['$X', '$Z'] }, _id : 0 } }");

            var result = queryable.Single();
            result.Should().Be(2);
        }

        [Fact]
        public void BitXor_with_three_arguments_should_work()
        {
            RequireServer.Check().Supports(Feature.BitwiseOperators);
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => x.X ^ x.Y ^ x.Z);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $bitXor : ['$X', '$Y', '$Z'] }, _id : 0 } }");

            var result = queryable.Single();
            result.Should().Be(0);
        }

        private IMongoCollection<C> GetCollection()
        {
            var collection = GetCollection<C>("test");
            CreateCollection(
                collection,
                new C { Id = 1, P = true, Q = true, R = false, X = 1, Y = 2, Z = 3 });
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public bool P { get; set; }
            public bool Q { get; set; }
            public bool R { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public int Z { get; set; }
        }
    }
}
