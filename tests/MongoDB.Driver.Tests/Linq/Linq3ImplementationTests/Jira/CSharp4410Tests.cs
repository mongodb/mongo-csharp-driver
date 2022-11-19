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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp4410Tests : Linq3IntegrationTest
    {
        private static readonly SelectTestCase[] __selectTestCases;

        private class SelectTestCase
        {
            public Expression<Func<C, bool>> Projection { get; set; }
            public string ProjectionAsString { get; set; }
            public string ExpectedStage { get; set; }
            public int[] ExpectedResults { get; set; }
        }

        private static SelectTestCase CreateSelectTestCase(
            Expression<Func<C, bool>>  projection,
            string expectedStage,
            params int[] expectedResults)
        {
            var projectionAsString = projection.ToString();
            return new SelectTestCase { Projection = projection, ProjectionAsString = projectionAsString, ExpectedStage = expectedStage, ExpectedResults = expectedResults };
        }

        static CSharp4410Tests()
        {
            __selectTestCases = new SelectTestCase[]
            {
                // test all comparison operators
                CreateSelectTestCase(x => x.E == E.X, "{ $project : { _v : { $eq : ['$E', 6] }, _id : 0 } }", 4),
                CreateSelectTestCase(x => x.E != E.X, "{ $project : { _v : { $ne : ['$E', 6] }, _id : 0 } }", 1, 2, 3, 5, 6),
                CreateSelectTestCase(x => x.E >= E.X, "{ $project : { _v : { $gte : ['$E', 6] }, _id : 0 } }", 4, 5, 6),
                CreateSelectTestCase(x => x.E <= E.X, "{ $project : { _v : { $lte : ['$E', 6] }, _id : 0 } }", 1, 2, 3, 4),
                CreateSelectTestCase(x => x.E > E.X, "{ $project : { _v : { $gt : ['$E', 6] }, _id : 0 } }", 5, 6),
                CreateSelectTestCase(x => x.E < E.X, "{ $project : { _v : { $lt : ['$E', 6] }, _id : 0 } }", 1, 2, 3),

                // test all combinations of field/constant and nullable/not nullable
                CreateSelectTestCase(x => x.E == E.B, "{ $project : { _v : { $eq : ['$E', 2] }, _id : 0 } }", 2),
                CreateSelectTestCase(x => x.E == (E?)E.B, "{ $project : { _v : { $eq : ['$E', 2] }, _id : 0 } }", 2),
                CreateSelectTestCase(x => x.E == x.F, "{ $project : { _v : { $eq : ['$E', '$F'] }, _id : 0 } }", 1, 4),
                CreateSelectTestCase(x => x.E == x.NF, "{ $project : { _v : { $eq : ['$E', '$NF'] }, _id : 0 } }", 1),
                CreateSelectTestCase(x => x.NE == E.B, "{ $project : { _v : { $eq : ['$NE', 2] }, _id : 0 } }", 2),
                CreateSelectTestCase(x => x.NE == (E?)E.B, "{ $project : { _v : { $eq : ['$NE', 2] }, _id : 0 } }", 2),
                CreateSelectTestCase(x => x.NE == (E?)null, "{ $project : { _v : { $eq : ['$NE', null] }, _id : 0 } }", 5, 6),
                CreateSelectTestCase(x => x.NE == null, "{ $project : { _v : { $eq : ['$NE', null] }, _id : 0 } }", 5, 6),
                CreateSelectTestCase(x => x.NE == x.F, "{ $project : { _v : { $eq : ['$NE', '$F'] }, _id : 0 } }", 1, 4),
                CreateSelectTestCase(x => x.NE == x.NF, "{ $project : { _v : { $eq : ['$NE', '$NF'] }, _id : 0 } }", 1, 6),
                CreateSelectTestCase(x => E.A == x.F, "{ $project : { _v : { $eq : [1, '$F'] }, _id : 0 } }", 1),
                CreateSelectTestCase(x => E.A == x.NF, "{ $project : { _v : { $eq : [1, '$NF'] }, _id : 0 } }", 1),
                CreateSelectTestCase(x => (E?)E.A == x.F, "{ $project : { _v : { $eq : [1, '$F'] }, _id : 0 } }", 1),
                CreateSelectTestCase(x => (E?)E.A == x.NF, "{ $project : { _v : { $eq : [1, '$NF'] }, _id : 0 } }", 1),
                CreateSelectTestCase(x => (E?)null == x.NF, "{ $project : { _v : { $eq : [null, '$NF'] }, _id : 0 } }", 4, 6),
                CreateSelectTestCase(x => null == x.NF, "{ $project : { _v : { $eq : [null, '$NF'] }, _id : 0 } }", 4, 6),

                // test all combinations of field/constant and nullable/not nullable with '+' (sometimes combined with '-' on other side)
                CreateSelectTestCase(x => x.E + x.One == E.B, "{ $project : { _v : { $eq : [{ $add : ['$E', '$One'] }, 2] }, _id : 0 } }", 1),
                CreateSelectTestCase(x => x.E + x.One == (E?)E.B, "{ $project : { _v : { $eq : [{ $add : ['$E', '$One'] }, 2] }, _id : 0 } }", 1),
                CreateSelectTestCase(x => x.E + x.One == x.F, "{ $project : { _v : { $eq : [{ $add : ['$E', '$One'] }, '$F'] }, _id : 0 } }", 2),
                CreateSelectTestCase(x => x.E + x.One == x.F - x.One, "{ $project : { _v : { $eq : [{ $add : ['$E', '$One'] }, { $subtract : ['$F', '$One'] }] }, _id : 0 } }", 5),
                CreateSelectTestCase(x => x.E + x.One == x.NF, "{ $project : { _v : { $eq : [{ $add : ['$E', '$One'] }, '$NF'] }, _id : 0 } }", 2),
                CreateSelectTestCase(x => x.E + x.One == x.NF - x.One, "{ $project : { _v : { $eq : [{ $add : ['$E', '$One'] }, { $subtract : ['$NF', '$One'] }] }, _id : 0 } }", 5),
                CreateSelectTestCase(x => x.NE + x.One == E.B, "{ $project : { _v : { $eq : [{ $add : ['$NE', '$One'] }, 2] }, _id : 0 } }", 1),
                CreateSelectTestCase(x => x.NE + x.One == (E?)E.B, "{ $project : { _v : { $eq : [{ $add : ['$NE', '$One'] }, 2] }, _id : 0 } }", 1),
                CreateSelectTestCase(x => x.NE + x.One == (E?)null, "{ $project : { _v : { $eq : [{ $add : ['$NE', '$One'] }, null] }, _id : 0 } }", 5, 6),
                CreateSelectTestCase(x => x.NE + x.One == null, "{ $project : { _v : { $eq : [{ $add : ['$NE', '$One'] }, null] }, _id : 0 } }", 5, 6),
                CreateSelectTestCase(x => x.NE + x.One == x.F, "{ $project : { _v : { $eq : [{ $add : ['$NE', '$One'] }, '$F'] }, _id : 0 } }", 2),
                CreateSelectTestCase(x => x.NE + x.One == x.F - x.One, "{ $project : { _v : { $eq : [{ $add : ['$NE', '$One'] }, { $subtract : ['$F', '$One'] }] }, _id : 0 } }"),
                CreateSelectTestCase(x => x.NE + x.One == x.NF, "{ $project : { _v : { $eq : [{ $add : ['$NE', '$One'] }, '$NF'] }, _id : 0 } }", 2, 6),
                CreateSelectTestCase(x => x.NE + x.One == x.NF - x.One, "{ $project : { _v : { $eq : [{ $add : ['$NE', '$One'] }, { $subtract : ['$NF', '$One'] }] }, _id : 0 } }", 6),
                CreateSelectTestCase(x => E.A == x.F + x.One, "{ $project : { _v : { $eq : [1, { $add : ['$F', '$One'] }] }, _id : 0 } }"),
                CreateSelectTestCase(x => E.A == x.NF + x.One, "{ $project : { _v : { $eq : [1, { $add : ['$NF', '$One'] }] }, _id : 0 } }"),
                CreateSelectTestCase(x => (E?)E.A == x.F + x.One, "{ $project : { _v : { $eq : [1, { $add : ['$F', '$One'] }] }, _id : 0 } }"),
                CreateSelectTestCase(x => (E?)E.A == x.NF + x.One, "{ $project : { _v : { $eq : [1, { $add : ['$NF', '$One'] }] }, _id : 0 } }"),
                CreateSelectTestCase(x => (E?)null == x.NF + x.One, "{ $project : { _v : { $eq : [null, { $add : ['$NF', '$One'] }] }, _id : 0 } }", 4, 6),
                CreateSelectTestCase(x => null == x.NF + x.One, "{ $project : { _v : { $eq : [null, { $add : ['$NF', '$One'] }] }, _id : 0 } }", 4, 6),

                // test all combinations of field/constant and nullable/not nullable with '-' (sometimes combined with '+' on other side)
                CreateSelectTestCase(x => x.E - x.One == E.B, "{ $project : { _v : { $eq : [{ $subtract : ['$E', '$One'] }, 2] }, _id : 0 } }", 3),
                CreateSelectTestCase(x => x.E - x.One == (E?)E.B, "{ $project : { _v : { $eq : [{ $subtract : ['$E', '$One'] }, 2] }, _id : 0 } }", 3),
                CreateSelectTestCase(x => x.E - x.One == x.F, "{ $project : { _v : { $eq : [{ $subtract : ['$E', '$One'] }, '$F'] }, _id : 0 } }", 3),
                CreateSelectTestCase(x => x.E - x.One == x.F + x.One, "{ $project : { _v : { $eq : [{ $subtract : ['$E', '$One'] }, { $add : ['$F', '$One'] }] }, _id : 0 } }", 6),
                CreateSelectTestCase(x => x.E - x.One == x.NF, "{ $project : { _v : { $eq : [{ $subtract : ['$E', '$One'] }, '$NF'] }, _id : 0 } }", 3),
                CreateSelectTestCase(x => x.E - x.One == x.NF + x.One, "{ $project : { _v : { $eq : [{ $subtract : ['$E', '$One'] }, { $add : ['$NF', '$One'] }] }, _id : 0 } }"),
                CreateSelectTestCase(x => x.NE - x.One == E.B, "{ $project : { _v : { $eq : [{ $subtract : ['$NE', '$One'] }, 2] }, _id : 0 } }", 3),
                CreateSelectTestCase(x => x.NE - x.One == (E?)E.B, "{ $project : { _v : { $eq : [{ $subtract : ['$NE', '$One'] }, 2] }, _id : 0 } }", 3),
                CreateSelectTestCase(x => x.NE - x.One == (E?)null, "{ $project : { _v : { $eq : [{ $subtract : ['$NE', '$One'] }, null] }, _id : 0 } }", 5, 6),
                CreateSelectTestCase(x => x.NE - x.One == null, "{ $project : { _v : { $eq : [{ $subtract : ['$NE', '$One'] }, null] }, _id : 0 } }", 5, 6),
                CreateSelectTestCase(x => x.NE - x.One == x.F, "{ $project : { _v : { $eq : [{ $subtract : ['$NE', '$One'] }, '$F'] }, _id : 0 } }", 3),
                CreateSelectTestCase(x => x.NE - x.One == x.F + x.One, "{ $project : { _v : { $eq : [{ $subtract : ['$NE', '$One'] }, { $add : ['$F', '$One'] }] }, _id : 0 } }"),
                CreateSelectTestCase(x => x.NE - x.One == x.NF, "{ $project : { _v : { $eq : [{ $subtract : ['$NE', '$One'] }, '$NF'] }, _id : 0 } }", 3, 6),
                CreateSelectTestCase(x => x.NE - x.One == x.NF + x.One, "{ $project : { _v : { $eq : [{ $subtract : ['$NE', '$One'] }, { $add : ['$NF', '$One'] }] }, _id : 0 } }", 6),
                CreateSelectTestCase(x => E.A == x.F - x.One, "{ $project : { _v : { $eq : [1, { $subtract : ['$F', '$One'] }] }, _id : 0 } }", 3),
                CreateSelectTestCase(x => E.A == x.NF - x.One, "{ $project : { _v : { $eq : [1, { $subtract : ['$NF', '$One'] }] }, _id : 0 } }", 3),
                CreateSelectTestCase(x => (E?)E.A == x.F - x.One, "{ $project : { _v : { $eq : [1, { $subtract : ['$F', '$One'] }] }, _id : 0 } }", 3),
                CreateSelectTestCase(x => (E?)E.A == x.NF - x.One, "{ $project : { _v : { $eq : [1, { $subtract : ['$NF', '$One'] }] }, _id : 0 } }", 3),
                CreateSelectTestCase(x => (E?)null == x.NF - x.One, "{ $project : { _v : { $eq : [null, { $subtract : ['$NF', '$One'] }] }, _id : 0 } }", 4, 6),
                CreateSelectTestCase(x => null == x.NF - x.One, "{ $project : { _v : { $eq : [null, { $subtract : ['$NF', '$One'] }] }, _id : 0 } }", 4, 6),

                // test all combinations of field/constant and nullable/ not nullable with string representation (only comparisons allowed with string representation)
                CreateSelectTestCase(x => x.S == E.B, "{ $project : { _v : { $eq : ['$S', 'B'] }, _id : 0 } }", 2),
                CreateSelectTestCase(x => x.S == (E?)E.B, "{ $project : { _v : { $eq : ['$S', 'B'] }, _id : 0 } }", 2),
                CreateSelectTestCase(x => x.S == x.T, "{ $project : { _v : { $eq : ['$S', '$T'] }, _id : 0 } }", 1, 4),
                CreateSelectTestCase(x => x.S == x.NT, "{ $project : { _v : { $eq : ['$S', '$NT'] }, _id : 0 } }", 1),
                CreateSelectTestCase(x => x.NS == E.B, "{ $project : { _v : { $eq : ['$NS', 'B'] }, _id : 0 } }", 2),
                CreateSelectTestCase(x => x.NS == (E?)E.B, "{ $project : { _v : { $eq : ['$NS', 'B'] }, _id : 0 } }", 2),
                CreateSelectTestCase(x => x.NS == (E?)null, "{ $project : { _v : { $eq : ['$NS', null] }, _id : 0 } }", 5, 6),
                CreateSelectTestCase(x => x.NS == null, "{ $project : { _v : { $eq : ['$NS', null] }, _id : 0 } }", 5, 6),
                CreateSelectTestCase(x => x.NS == x.T, "{ $project : { _v : { $eq : ['$NS', '$T'] }, _id : 0 } }", 1, 4),
                CreateSelectTestCase(x => x.NS == x.NT, "{ $project : { _v : { $eq : ['$NS', '$NT'] }, _id : 0 } }", 1, 6),
                CreateSelectTestCase(x => E.A == x.T, "{ $project : { _v : { $eq : ['A', '$T'] }, _id : 0 } }", 1),
                CreateSelectTestCase(x => E.A == x.NT, "{ $project : { _v : { $eq : ['A', '$NT'] }, _id : 0 } }", 1),
                CreateSelectTestCase(x => (E?)E.A == x.T, "{ $project : { _v : { $eq : ['A', '$T'] }, _id : 0 } }", 1),
                CreateSelectTestCase(x => (E?)E.A == x.NT, "{ $project : { _v : { $eq : ['A', '$NT'] }, _id : 0 } }", 1),
                CreateSelectTestCase(x => (E?)null == x.NT, "{ $project : { _v : { $eq : [null, '$NT'] }, _id : 0 } }", 4, 6),
                CreateSelectTestCase(x => null == x.NT, "{ $project : { _v : { $eq : [null, '$NT'] }, _id : 0 } }", 4, 6)
            };
        }

        public static IEnumerable<object[]> Select_with_enum_comparison_should_work_member_data()
        {
            for (var i = 0; i < __selectTestCases.Length; i++)
            {
                var projectionAsString = __selectTestCases[i].ProjectionAsString;
                var expectedStage = __selectTestCases[i].ExpectedStage;
                var expectedResults = __selectTestCases[i].ExpectedResults;
                yield return new object[] { i, projectionAsString, expectedStage, expectedResults };
            }
        }

        [Theory]
        [MemberData(nameof(Select_with_enum_comparison_should_work_member_data))]
        public void Select_with_enum_comparison_should_work(int i, string projectionAsString, string expectedProjectStage, int[] expectedResults)
        {
            var collection = CreateCollection();
            var projection = __selectTestCases[i].Projection;
            expectedResults ??= new int[0];

            var queryable = collection
                .AsQueryable()
                .OrderBy(x => x.Id)
                .Select(projection);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $sort : { _id : 1 } }",
                expectedProjectStage);

            var results = queryable.ToList();
            Enumerable.Range(1, 6).Where(i => results[i - 1] == true).Should().Equal(expectedResults);
        }

        [Fact]
        public void Comparison_of_enum_and_enum_with_mismatched_serializers_should_throw()
        {
            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .Select(x => x.E == x.S);

            var exception = Record.Exception(() => Translate(collection, queryable));

            exception.Should().BeOfType<ExpressionNotSupportedException>();
            exception.Message.Should().Contain("because the two enums being compared are serialized using different serializers");
        }

        [Fact]
        public void Comparison_of_enum_and_nullable_enum_with_mismatched_serializers_should_throw()
        {
            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .Select(x => x.E == x.NS);

            var exception = Record.Exception(() => Translate(collection, queryable));

            exception.Should().BeOfType<ExpressionNotSupportedException>();
            exception.Message.Should().Contain("because the two enums being compared are serialized using different serializers");
        }

        [Fact]
        public void Comparison_of_nullable_enum_and_enum_with_mismatched_serializers_should_throw()
        {
            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .Select(x => x.NE == x.S);

            var exception = Record.Exception(() => Translate(collection, queryable));

            exception.Should().BeOfType<ExpressionNotSupportedException>();
            exception.Message.Should().Contain("because the two enums being compared are serialized using different serializers");
        }

        [Fact]
        public void Comparison_of_nullable_enum_and_nullable_enum_with_mismatched_serializers_should_throw()
        {
            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .Select(x => x.NE == x.NS);

            var exception = Record.Exception(() => Translate(collection, queryable));

            exception.Should().BeOfType<ExpressionNotSupportedException>();
            exception.Message.Should().Contain("because the two enums being compared are serialized using different serializers");
        }

        [Fact]
        public void Arithmetic_with_enum_represented_as_string_should_throw()
        {
            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .Select(x => x.S + 1);

            var exception = Record.Exception(() => Translate(collection, queryable));

            exception.Should().BeOfType<ExpressionNotSupportedException>();
            exception.Message.Should().Contain("because arithmetic on enums is only allowed when the enum is represented as an integer");
        }

        [Fact]
        public void Arithmetic_with_nullable_enum_represented_as_string_should_throw()
        {
            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .Select(x => x.NS + 1);

            var exception = Record.Exception(() => Translate(collection, queryable));

            exception.Should().BeOfType<ExpressionNotSupportedException>();
            exception.Message.Should().Contain("because arithmetic on enums is only allowed when the enum is represented as an integer");
        }

        private IMongoCollection<C> CreateCollection()
        {
            var collection = GetCollection<C>();

            CreateCollection(
                collection,
                new C { Id = 1, One = 1, E = E.A, F = E.A, NE = E.A, NF = E.A, S = E.A, T = E.A, NS = E.A, NT = E.A },
                new C { Id = 2, One = 1, E = E.B, F = E.C, NE = E.B, NF = E.C, S = E.B, T = E.C, NS = E.B, NT = E.C },
                new C { Id = 3, One = 1, E = E.C, F = E.B, NE = E.C, NF = E.B, S = E.C, T = E.B, NS = E.C, NT = E.B },
                new C { Id = 4, One = 1, E = E.X, F = E.X, NE = E.X, NF = null, S = E.X, T = E.X, NS = E.X, NT = null },
                new C { Id = 5, One = 1, E = E.Y, F = E.Z, NE = null, NF = E.Z, S = E.Y, T = E.Z, NS = null, NT = E.Z },
                new C { Id = 6, One = 1, E = E.Z, F = E.Y, NE = null, NF = null, S = E.Z, T = E.Y, NS = null, NT = null });

            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public int One { get; set; }
            public E E { get; set; }
            public E F { get; set; }
            public E? NE { get; set; }
            public E? NF { get; set; }
            [BsonRepresentation(BsonType.String)] public E S { get; set; }
            [BsonRepresentation(BsonType.String)] public E T { get; set; }
            [BsonRepresentation(BsonType.String)] public E? NS { get; set; }
            [BsonRepresentation(BsonType.String)] public E? NT { get; set; }
        }

        private enum E { A = 1, B = 2, C = 3, X = 6, Y = 7, Z = 9 }
    }
}
