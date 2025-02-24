﻿/* Copyright 2010-present MongoDB Inc.
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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using MongoDB.Driver.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp5043Tests : LinqIntegrationTest<CSharp5043Tests.ClassFixture>
    {
        public CSharp5043Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void Convert_E1_to_E1_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Select(x => (E1)x.E1);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : '$E1', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(E1.A, E1.B);
        }

        [Fact]
        public void Convert_E1_to_E2_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Select(x => (E2)x.E1);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : '$E1', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(E2.C, E2.D);
        }

        [Fact]
        public void Convert_E1_to_nullable_E1_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Select(x => (E1?)x.E1);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : '$E1', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(E1.A, E1.B);
        }

        [Fact]
        public void Convert_E1_to_nullable_E2_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Select(x => (E2?)x.E1);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : '$E1', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(E2.C, E2.D);
        }

        [Fact]
        public void Convert_nullable_E1_to_E1_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Select(x => (E1)x.NE1);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : '$NE1', _id : 0 } }");

            var exception = Record.Exception(() => queryable.ToList());
            exception.Should().BeOfType<FormatException>();
            exception.Message.Should().Contain("Cannot deserialize a 'E1' from BsonType 'Null'");
        }

        [Fact]
        public void Convert_nullable_E1_to_E2_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Select(x => (E2)x.NE1);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : '$NE1', _id : 0 } }");

            var exception = Record.Exception(() => queryable.ToList());
            exception.Should().BeOfType<FormatException>();
            exception.Message.Should().Contain("Cannot deserialize a 'E2' from BsonType 'Null'");
        }

        [Fact]
        public void Convert_nullable_E1_to_nullable_E1_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Select(x => (E1?)x.NE1);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : '$NE1', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(E1.A, null);
        }

        [Fact]
        public void Convert_nullable_E1_to_nullable_E2_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Select(x => (E2?)x.NE1);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : '$NE1', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(E2.C, null);
        }

        [Fact]
        public void Convert_ES1_to_E1_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Select(x => (E1)x.ES1);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : '$ES1', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(E1.A, E1.B);
        }

        [Theory]
        [ParameterAttributeData]
        public void Convert_ES1_to_E2_should_throw(
            [Values(false, true)] bool enableClientSideProjections)
        {
            RequireServer.Check().Supports(Feature.FindProjectionExpressions);
            var collection = Fixture.Collection;
            var translationOptions = new ExpressionTranslationOptions { EnableClientSideProjections = enableClientSideProjections };

            var queryable = collection.AsQueryable(translationOptions)
                .Select(x => (E2)x.ES1);

            if (enableClientSideProjections)
            {
                var stages = Translate(collection, queryable, out var outputSerializer);
                AssertStages(stages, "{ $project : { _snippets : ['$ES1'], _id : 0 } }");
                outputSerializer.Should().BeAssignableTo<IClientSideProjectionDeserializer>();

                var results = queryable.ToList();
                results.Should().Equal(E2.C, E2.D);
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("because source enum is not represented as a number");
            }
        }

        [Fact]
        public void Convert_ES1_to_nullable_E1_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Select(x => (E1?)x.ES1);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : '$ES1', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(E1.A, E1.B);
        }

        [Theory]
        [ParameterAttributeData]
        public void Convert_ES1_to_nullable_E2_should_throw(
            [Values(false, true)] bool enableClientSideProjections)
        {
            RequireServer.Check().Supports(Feature.FindProjectionExpressions);
            var collection = Fixture.Collection;
            var translationOptions = new ExpressionTranslationOptions { EnableClientSideProjections = enableClientSideProjections };

            var queryable = collection.AsQueryable(translationOptions)
                .Select(x => (E2?)x.ES1);

            if (enableClientSideProjections)
            {
                var stages = Translate(collection, queryable, out var outputSerializer);
                AssertStages(stages, "{ $project : { _snippets : ['$ES1'], _id : 0 } }");
                outputSerializer.Should().BeAssignableTo<IClientSideProjectionDeserializer>();

                var results = queryable.ToList();
                results.Should().Equal(E2.C, E2.D);
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("because source enum is not represented as a number");
            }
        }

        public class C
        {
            public int Id { get; set; }
            public E1 E1 { get; set; }
            public E1? NE1 { get; set; }
            [BsonRepresentation(BsonType.String)] public E1 ES1 { get; set; }
            [BsonRepresentation(BsonType.String)] public E1? NES1 { get; set; }
        }

        public enum E1 { A, B }
        public enum E2 { C, D }

        public sealed class ClassFixture : MongoCollectionFixture<C>
        {
            protected override IEnumerable<C> InitialData =>
            [
                new C { Id = 1, E1 = E1.A, NE1 = E1.A, ES1 = E1.A, NES1 = E1.A },
                new C { Id = 2, E1 = E1.B, NE1 = null, ES1 = E1.B, NES1 = null }
            ];
        }
    }
}
