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

using System;
using System.Linq;
using FluentAssertions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4486Tests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public void And_with_two_arguments_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.P & x.Q);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { $and : ['$P', '$Q'] }, _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $and : ['$P', '$Q'] }, _id : 0 } }");
            }

            var result = queryable.Single();
            result.Should().Be(true);
        }

        [Theory]
        [ParameterAttributeData]
        public void And_with_three_arguments_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.P & x.Q & x.R);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { $and : ['$P', '$Q', '$R'] }, _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $and : ['$P', '$Q', '$R'] }, _id : 0 } }");
            }

            var result = queryable.Single();
            result.Should().Be(false);
        }

        [Theory]
        [ParameterAttributeData]
        public void Not_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => !x.P);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { $not : ['$P'] }, _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $not : '$P' }, _id : 0 } }");
            }

            var result = queryable.Single();
            result.Should().Be(false);
        }

        [Theory]
        [ParameterAttributeData]
        public void Or_with_two_arguments_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.P | x.Q);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { $or : ['$P', '$Q'] }, _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $or : ['$P', '$Q'] }, _id : 0 } }");
            }

            var result = queryable.Single();
            result.Should().Be(true);
        }

        [Theory]
        [ParameterAttributeData]
        public void Or_with_three_arguments_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.P | x.Q | x.R);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { $or : ['$P', '$Q', '$R'] }, _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $or : ['$P', '$Q', '$R'] }, _id : 0 } }");
            }

            var result = queryable.Single();
            result.Should().Be(true);
        }

        [Theory]
        [ParameterAttributeData]
        public void Xor_with_two_arguments_should_throw(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.P ^ x.Q);

            var exception = Record.Exception(() => Translate(collection, queryable));
            if (linqProvider == LinqProvider.V2)
            {
                exception.Should().BeOfType<NotSupportedException>();
            }
            else
            {
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("because MongoDB does not have an $xor operator");
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void BitAnd_with_two_arguments_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            RequireServer.Check().Supports(Feature.BitwiseOperators);
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.X & x.Z);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { $and : ['$X', '$Z'] }, _id : 0 } }"); // LINQ2 translation is wrong

                var exception = Record.Exception(() => queryable.Single());
                exception.Should().BeOfType<FormatException>(); // LINQ2 result is wrong
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $bitAnd : ['$X', '$Z'] }, _id : 0 } }");

                var result = queryable.Single();
                result.Should().Be(1);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void BitAnd_with_three_arguments_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            RequireServer.Check().Supports(Feature.BitwiseOperators);
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.X & x.Y & x.Z);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { $and : ['$X', '$Y', '$Z'] }, _id : 0 } }"); // LINQ2 translation is wrong

                var exception = Record.Exception(() => queryable.Single());
                exception.Should().BeOfType<FormatException>(); // LINQ2 result is wrong
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $bitAnd : ['$X', '$Y', '$Z'] }, _id : 0 } }");

                var result = queryable.Single();
                result.Should().Be(0);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void BitNot_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            RequireServer.Check().Supports(Feature.BitwiseOperators);
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => ~x.X);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { $not : ['$X'] }, _id : 0 } }"); // LINQ2 translation is wrong

                var exception = Record.Exception(() => queryable.Single());
                exception.Should().BeOfType<FormatException>(); // LINQ2 result is wrong
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $bitNot : '$X' }, _id : 0 } }");

                var result = queryable.Single();
                result.Should().Be(~1);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void BitOr_with_two_arguments_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            RequireServer.Check().Supports(Feature.BitwiseOperators);
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.X | x.Z);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { $or : ['$X', '$Z'] }, _id : 0 } }"); // LINQ2 translation is wrong

                var exception = Record.Exception(() => queryable.Single());
                exception.Should().BeOfType<FormatException>(); // LINQ2 result is wrong
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $bitOr : ['$X', '$Z'] }, _id : 0 } }");

                var result = queryable.Single();
                result.Should().Be(3);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void BitOr_with_three_arguments_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            RequireServer.Check().Supports(Feature.BitwiseOperators);
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.X | x.Y | x.Z);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { $or : ['$X', '$Y', '$Z'] }, _id : 0 } }"); // LINQ2 translation is wrong

                var exception = Record.Exception(() => queryable.Single());
                exception.Should().BeOfType<FormatException>(); // LINQ2 result is wrong
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $bitOr : ['$X', '$Y', '$Z'] }, _id : 0 } }");

                var result = queryable.Single();
                result.Should().Be(3);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void BitXor_with_two_arguments_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            RequireServer.Check().Supports(Feature.BitwiseOperators);
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.X ^ x.Z);

            if (linqProvider == LinqProvider.V2)
            {
                var exception = Record.Exception(() => Translate(collection, queryable)); // LINQ2 throws exception
                exception.Should().BeOfType<NotSupportedException>();
            }
            else
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $project : { _v : { $bitXor : ['$X', '$Z'] }, _id : 0 } }");

                var result = queryable.Single();
                result.Should().Be(2);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void BitXor_with_three_arguments_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            RequireServer.Check().Supports(Feature.BitwiseOperators);
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.X ^ x.Y ^ x.Z);

            if (linqProvider == LinqProvider.V2)
            {
                var exception = Record.Exception(() => Translate(collection, queryable)); // LINQ2 throws exception
                exception.Should().BeOfType<NotSupportedException>();
            }
            else
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $project : { _v : { $bitXor : ['$X', '$Y', '$Z'] }, _id : 0 } }");

                var result = queryable.Single();
                result.Should().Be(0);
            }
        }

        private IMongoCollection<C> GetCollection(LinqProvider linqProvider)
        {
            var collection = GetCollection<C>("test", linqProvider);
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
