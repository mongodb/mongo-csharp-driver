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
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Linq;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp5043Tests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public void Convert_E1_to_E1_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => (E1)x.E1);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { E1 : '$E1', _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : '$E1', _id : 0 } }");
            }

            var results = queryable.ToList();
            results.Should().Equal(E1.A, E1.B);
        }

        [Theory]
        [ParameterAttributeData]
        public void Convert_E1_to_E2_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => (E2)x.E1);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : '$E1', _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : '$E1', _id : 0 } }");
            }

            var results = queryable.ToList();
            results.Should().Equal(E2.C, E2.D);
        }

        [Theory]
        [ParameterAttributeData]
        public void Convert_E1_to_nullable_E1_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => (E1?)x.E1);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : '$E1', _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : '$E1', _id : 0 } }");
            }

            var results = queryable.ToList();
            results.Should().Equal(E1.A, E1.B);
        }

        [Theory]
        [ParameterAttributeData]
        public void Convert_E1_to_nullable_E2_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => (E2?)x.E1);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : '$E1', _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : '$E1', _id : 0 } }");
            }

            var results = queryable.ToList();
            results.Should().Equal(E2.C, E2.D);
        }

        [Theory]
        [ParameterAttributeData]
        public void Convert_nullable_E1_to_E1_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => (E1)x.NE1);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { NE1 : '$NE1', _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : '$NE1', _id : 0 } }");
            }

            var results = queryable.ToList();
            results.Should().Equal(E1.A, E1.B);
        }

        [Theory]
        [ParameterAttributeData]
        public void Convert_nullable_E1_to_E2_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => (E2)x.NE1);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : '$NE1', _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : '$NE1', _id : 0 } }");
            }

            var results = queryable.ToList();
            results.Should().Equal(E2.C, E2.D);
        }

        [Theory]
        [ParameterAttributeData]
        public void Convert_nullable_E1_to_nullable_E1_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => (E1?)x.NE1);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { NE1 : '$NE1', _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : '$NE1', _id : 0 } }");
            }

            var results = queryable.ToList();
            results.Should().Equal(E1.A, E1.B);
        }

        [Theory]
        [ParameterAttributeData]
        public void Convert_nullable_E1_to_nullable_E2_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => (E2?)x.NE1);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : '$NE1', _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : '$NE1', _id : 0 } }");
            }

            var results = queryable.ToList();
            results.Should().Equal(E2.C, E2.D);
        }

        [Theory]
        [ParameterAttributeData]
        public void Convert_ES1_to_E1_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => (E1)x.ES1);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { ES1 : '$ES1', _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : '$ES1', _id : 0 } }");
            }

            var results = queryable.ToList();
            results.Should().Equal(E1.A, E1.B);
        }

        [Theory]
        [ParameterAttributeData]
        public void Convert_ES1_to_E2_should_throw(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => (E2)x.ES1);

            if (linqProvider == LinqProvider.V2)
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $project : { __fld0 : '$ES1', _id : 0 } }");

                // LINQ2 fails during deserialization because E1 and E2 do not have compatible representations
                var exception = Record.Exception(() => queryable.ToList());
                exception.Should().BeOfType<ArgumentException>();
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("because source enum is not represented as a number");
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Convert_ES1_to_nullable_E1_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => (E1?)x.ES1);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : '$ES1', _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : '$ES1', _id : 0 } }");
            }

            var results = queryable.ToList();
            results.Should().Equal(E1.A, E1.B);
        }

        [Theory]
        [ParameterAttributeData]
        public void Convert_ES1_to_nullable_E2_should_throw(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => (E2?)x.ES1);

            if (linqProvider == LinqProvider.V2)
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $project : { __fld0 : '$ES1', _id : 0 } }");

                // LINQ2 fails during deserialization because E1 and E2 do not have compatible representations
                var exception = Record.Exception(() => queryable.ToList());
                exception.Should().BeOfType<ArgumentException>();
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("because source enum is not represented as a number");
            }
        }

        private IMongoCollection<C> GetCollection(LinqProvider linqProvider)
        {
            var collection = GetCollection<C>("test", linqProvider);
            CreateCollection(
                collection,
                new C { Id = 1, E1 = E1.A, NE1 = E1.A, ES1 = E1.A, NES1 = E1.A }, 
                new C { Id = 2, E1 = E1.B, NE1 = E1.B, ES1 = E1.B, NES1 = E1.B });
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public E1 E1 { get; set; }
            public E1? NE1 { get; set; }
            [BsonRepresentation(BsonType.String)] public E1 ES1 { get; set; }
            [BsonRepresentation(BsonType.String)] public E1? NES1 { get; set; }
        }

        private enum E1 { A, B }
        private enum E2 { C, D }
    }
}
