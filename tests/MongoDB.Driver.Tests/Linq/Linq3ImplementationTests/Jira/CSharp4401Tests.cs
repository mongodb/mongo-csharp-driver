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
    public class CSharp4401Tests : Linq3IntegrationTest
    {
        private static readonly WhereTestCase[] __whereTestCases;

        private class WhereTestCase
        {
            public Expression<Func<C, bool>> Predicate { get; set; }
            public string PredicateAsString { get; set; }
            public string ExpectedStage { get; set; }
            public int[] ExpectedResults { get; set; }
        }

        private static WhereTestCase CreateWhereTestCase(
            Expression<Func<C, bool>> predicate,
            string expectedStage,
            params int[] expectedResults)
        {
            var predicateAsString = predicate.ToString();
            return new WhereTestCase { Predicate = predicate, PredicateAsString = predicateAsString, ExpectedStage = expectedStage, ExpectedResults = expectedResults };
        }

        static CSharp4401Tests()
        {
            __whereTestCases = new WhereTestCase[]
            {
                // test all comparison operators
                CreateWhereTestCase(x => x.E == E.X, "{ $match : { E : 6 } }", 4),
                CreateWhereTestCase(x => x.E != E.X, "{ $match : { E : { $ne : 6 } } }", 1, 2, 3, 5, 6),
                CreateWhereTestCase(x => x.E >= E.X, "{ $match : { E : { $gte :  6 } } }", 4, 5, 6),
                CreateWhereTestCase(x => x.E <= E.X, "{ $match : { E : { $lte : 6 } } }", 1, 2, 3, 4),
                CreateWhereTestCase(x => x.E > E.X, "{ $match : { E : { $gt : 6 } } }", 5, 6),
                CreateWhereTestCase(x => x.E < E.X, "{ $match : { E : { $lt : 6 } } }", 1, 2, 3),

                // test all combinations of field/constant and nullable/not nullable with ==
                CreateWhereTestCase(x => x.E == E.B, "{ $match : { E : 2 } }", 2),
                CreateWhereTestCase(x => x.E == (E?)E.B, "{ $match : { E : 2 } }", 2),
                CreateWhereTestCase(x => x.E == x.F, "{ $match : { $expr : { $eq : ['$E', '$F'] } } }", 1, 4),
                CreateWhereTestCase(x => x.E == x.NF, "{ $match : { $expr : { $eq : ['$E', '$NF'] } } }", 1),
                CreateWhereTestCase(x => x.NE == E.B, "{ $match : { NE : 2 } }", 2),
                CreateWhereTestCase(x => x.NE == (E?)E.B, "{ $match : { NE : 2 } }", 2),
                CreateWhereTestCase(x => x.NE == (E?)null, "{ $match : { NE : null } }", 5, 6),
                CreateWhereTestCase(x => x.NE == null, "{ $match : { NE : null } }", 5, 6),
                CreateWhereTestCase(x => x.NE == x.F, "{ $match : { $expr : { $eq : ['$NE', '$F'] } } }", 1, 4),
                CreateWhereTestCase(x => x.NE == x.NF, "{ $match : { $expr : { $eq : ['$NE', '$NF'] } } }", 1, 6),
                CreateWhereTestCase(x => E.A == x.F, "{ $match : { F : 1 } }", 1),
                CreateWhereTestCase(x => E.A == x.NF, "{ $match : { NF : 1 } }", 1),
                CreateWhereTestCase(x => (E?)E.A == x.F, "{ $match : { F : 1 } }", 1),
                CreateWhereTestCase(x => (E?)E.A == x.NF, "{ $match : { NF : 1 } }", 1),
                CreateWhereTestCase(x => (E?)null == x.NF, "{ $match : { NF : null } }", 4, 6),
                CreateWhereTestCase(x => null == x.NF, "{ $match : { NF : null } }", 4, 6),

                // test all combinations of field/constant and nullable/not nullable !=
                CreateWhereTestCase(x => x.E != E.B, "{ $match : { E : { $ne : 2 } } }", 1, 3, 4, 5, 6),
                CreateWhereTestCase(x => x.E != (E?)E.B, "{ $match : { E : { $ne : 2 } } }", 1, 3, 4, 5, 6),
                CreateWhereTestCase(x => x.E != x.F, "{ $match : { $expr : { $ne : ['$E', '$F'] } } }", 2, 3, 5, 6),
                CreateWhereTestCase(x => x.E != x.NF, "{ $match : { $expr : { $ne : ['$E', '$NF'] } } }", 2, 3, 4, 5, 6),
                CreateWhereTestCase(x => x.NE != E.B, "{ $match : { NE : { $ne : 2 } } }", 1, 3, 4, 5, 6),
                CreateWhereTestCase(x => x.NE != (E?)E.B, "{ $match : { NE : { $ne : 2 } } }", 1, 3, 4, 5, 6),
                CreateWhereTestCase(x => x.NE != (E?)null, "{ $match : { NE : { $ne : null } } }", 1, 2, 3, 4),
                CreateWhereTestCase(x => x.NE != null, "{ $match : { NE : { $ne : null } } }", 1, 2, 3, 4),
                CreateWhereTestCase(x => x.NE != x.F, "{ $match : { $expr : { $ne : ['$NE', '$F'] } } }", 2, 3, 5, 6),
                CreateWhereTestCase(x => x.NE != x.NF, "{ $match : { $expr : { $ne : ['$NE', '$NF'] } } }", 2, 3, 4, 5),
                CreateWhereTestCase(x => E.A != x.F, "{ $match : { F : { $ne : 1 } } }", 2, 3, 4, 5, 6),
                CreateWhereTestCase(x => E.A != x.NF, "{ $match : { NF : { $ne : 1 } } }", 2, 3, 4, 5, 6),
                CreateWhereTestCase(x => (E?)E.A != x.F, "{ $match : { F : { $ne : 1 } } }", 2, 3, 4, 5, 6),
                CreateWhereTestCase(x => (E?)E.A != x.NF, "{ $match : { NF : { $ne : 1 } } }", 2, 3, 4, 5, 6),
                CreateWhereTestCase(x => (E?)null != x.NF, "{ $match : { NF : { $ne : null } } }", 1, 2, 3, 5),
                CreateWhereTestCase(x => null != x.NF, "{ $match : { NF : { $ne : null } } }", 1, 2, 3, 5),

                // test all combinations of field/constant and nullable/not nullable with '+' (sometimes combined with '-' on other side)
                CreateWhereTestCase(x => x.E + x.One == E.B, "{ $match : { $expr : { $eq : [{ $add : ['$E', '$One'] }, 2] } } }", 1),
                CreateWhereTestCase(x => x.E + x.One == (E?)E.B, "{ $match : { $expr : { $eq : [{ $add : ['$E', '$One'] }, 2] } } }", 1),
                CreateWhereTestCase(x => x.E + x.One == x.F, "{ $match : { $expr : { $eq : [{ $add : ['$E', '$One'] }, '$F'] } } }", 2),
                CreateWhereTestCase(x => x.E + x.One == x.F - x.One, "{ $match : { $expr : { $eq : [{ $add : ['$E', '$One'] }, { $subtract : ['$F', '$One'] }] } } }", 5),
                CreateWhereTestCase(x => x.E + x.One == x.NF, "{ $match : { $expr : { $eq : [{ $add : ['$E', '$One'] }, '$NF'] } } }", 2),
                CreateWhereTestCase(x => x.E + x.One == x.NF - x.One, "{ $match : { $expr : { $eq : [{ $add : ['$E', '$One'] }, { $subtract : ['$NF', '$One'] }] } } }", 5),
                CreateWhereTestCase(x => x.NE + x.One == E.B, "{ $match : { $expr : { $eq : [{ $add : ['$NE', '$One'] }, 2] } } }", 1),
                CreateWhereTestCase(x => x.NE + x.One == (E?)E.B, "{ $match : { $expr : { $eq : [{ $add : ['$NE', '$One'] }, 2] } } }", 1),
                CreateWhereTestCase(x => x.NE + x.One == (E?)null, "{ $match : { $expr : { $eq : [{ $add : ['$NE', '$One'] }, null] } } }", 5, 6),
                CreateWhereTestCase(x => x.NE + x.One == null, "{ $match : { $expr : { $eq : [{ $add : ['$NE', '$One'] }, null] } } }", 5, 6),
                CreateWhereTestCase(x => x.NE + x.One == x.F, "{ $match : { $expr : { $eq : [{ $add : ['$NE', '$One'] }, '$F'] } } }", 2),
                CreateWhereTestCase(x => x.NE + x.One == x.F - x.One, "{ $match : { $expr : { $eq : [{ $add : ['$NE', '$One'] }, { $subtract : ['$F', '$One'] }] } } }"),
                CreateWhereTestCase(x => x.NE + x.One == x.NF, "{ $match : { $expr : { $eq : [{ $add : ['$NE', '$One'] }, '$NF'] } } }", 2, 6),
                CreateWhereTestCase(x => x.NE + x.One == x.NF - x.One, "{ $match : { $expr : { $eq : [{ $add : ['$NE', '$One'] }, { $subtract : ['$NF', '$One'] }] } } }", 6),
                CreateWhereTestCase(x => E.A == x.F + x.One, "{ $match : { $expr : { $eq : [1, { $add : ['$F', '$One'] }] } } }"),
                CreateWhereTestCase(x => E.A == x.NF + x.One, "{ $match : { $expr : { $eq : [1, { $add : ['$NF', '$One'] }] } } }"),
                CreateWhereTestCase(x => (E?)E.A == x.F + x.One, "{ $match : { $expr : { $eq : [1, { $add : ['$F', '$One'] }] } } }"),
                CreateWhereTestCase(x => (E?)E.A == x.NF + x.One, "{ $match : { $expr : { $eq : [1, { $add : ['$NF', '$One'] }] } } }"),
                CreateWhereTestCase(x => (E?)null == x.NF + x.One, "{ $match : { $expr : { $eq : [null, { $add : ['$NF', '$One'] }] } } }", 4, 6),
                CreateWhereTestCase(x => null == x.NF + x.One, "{ $match : { $expr : { $eq : [null, { $add : ['$NF', '$One'] }] } } }", 4, 6),

                // test all combinations of field/constant and nullable/not nullable with '-' (sometimes combined with '+' on other side)
                CreateWhereTestCase(x => x.E - x.One == E.B, "{ $match : { $expr : { $eq : [{ $subtract : ['$E', '$One'] }, 2] } } }", 3),
                CreateWhereTestCase(x => x.E - x.One == (E?)E.B, "{ $match : { $expr : { $eq : [{ $subtract : ['$E', '$One'] }, 2] } } }", 3),
                CreateWhereTestCase(x => x.E - x.One == x.F, "{ $match : { $expr : { $eq : [{ $subtract : ['$E', '$One'] }, '$F'] } } }", 3),
                CreateWhereTestCase(x => x.E - x.One == x.F + x.One, "{ $match : { $expr : { $eq : [{ $subtract : ['$E', '$One'] }, { $add : ['$F', '$One'] }] } } }", 6),
                CreateWhereTestCase(x => x.E - x.One == x.NF, "{ $match : { $expr : { $eq : [{ $subtract : ['$E', '$One'] }, '$NF'] } } }", 3),
                CreateWhereTestCase(x => x.E - x.One == x.NF + x.One, "{ $match : { $expr : { $eq : [{ $subtract : ['$E', '$One'] }, { $add : ['$NF', '$One'] }] } } }"),
                CreateWhereTestCase(x => x.NE - x.One == E.B, "{ $match : { $expr : { $eq : [{ $subtract : ['$NE', '$One'] }, 2] } } }", 3),
                CreateWhereTestCase(x => x.NE - x.One == (E?)E.B, "{ $match : { $expr : { $eq : [{ $subtract : ['$NE', '$One'] }, 2] } } }", 3),
                CreateWhereTestCase(x => x.NE - x.One == (E?)null, "{ $match : { $expr : { $eq : [{ $subtract : ['$NE', '$One'] }, null] } } }", 5, 6),
                CreateWhereTestCase(x => x.NE - x.One == null, "{ $match : { $expr : { $eq : [{ $subtract : ['$NE', '$One'] }, null] } } }", 5, 6),
                CreateWhereTestCase(x => x.NE - x.One == x.F, "{ $match : { $expr : { $eq : [{ $subtract : ['$NE', '$One'] }, '$F'] } } }", 3),
                CreateWhereTestCase(x => x.NE - x.One == x.F + x.One, "{ $match : { $expr : { $eq : [{ $subtract : ['$NE', '$One'] }, { $add : ['$F', '$One'] }] } } }"),
                CreateWhereTestCase(x => x.NE - x.One == x.NF, "{ $match : { $expr : { $eq : [{ $subtract : ['$NE', '$One'] }, '$NF'] } } }", 3, 6),
                CreateWhereTestCase(x => x.NE - x.One == x.NF + x.One, "{ $match : { $expr : { $eq : [{ $subtract : ['$NE', '$One'] }, { $add : ['$NF', '$One'] }] } } }", 6),
                CreateWhereTestCase(x => E.A == x.F - x.One, "{ $match : { $expr : { $eq : [1, { $subtract : ['$F', '$One'] }] } } }", 3),
                CreateWhereTestCase(x => E.A == x.NF - x.One, "{ $match : { $expr : { $eq : [1, { $subtract : ['$NF', '$One'] }] } } }", 3),
                CreateWhereTestCase(x => (E?)E.A == x.F - x.One, "{ $match : { $expr : { $eq : [1, { $subtract : ['$F', '$One'] }] } } }", 3),
                CreateWhereTestCase(x => (E?)E.A == x.NF - x.One, "{ $match : { $expr : { $eq : [1, { $subtract : ['$NF', '$One'] }] } } }", 3),
                CreateWhereTestCase(x => (E?)null == x.NF - x.One, "{ $match : { $expr : { $eq : [null, { $subtract : ['$NF', '$One'] }] } } }", 4, 6),
                CreateWhereTestCase(x => null == x.NF - x.One, "{ $match : { $expr : { $eq : [null, { $subtract : ['$NF', '$One'] }] } } }", 4, 6),

                // test all combinations of field/constant and nullable/ not nullable with string representation (only comparisons allowed with string representation)
                CreateWhereTestCase(x => x.S == E.B, "{ $match : { S : 'B' } }", 2),
                CreateWhereTestCase(x => x.S == (E?)E.B, "{ $match : { S : 'B' } }", 2),
                CreateWhereTestCase(x => x.S == x.T, "{ $match : { $expr : { $eq : ['$S', '$T'] } } }", 1, 4),
                CreateWhereTestCase(x => x.S == x.NT, "{ $match : { $expr : { $eq : ['$S', '$NT'] } } }", 1),
                CreateWhereTestCase(x => x.NS == E.B, "{ $match : { NS : 'B' } }", 2),
                CreateWhereTestCase(x => x.NS == (E?)E.B, "{ $match : { NS : 'B' } }", 2),
                CreateWhereTestCase(x => x.NS == (E?)null, "{ $match : { NS : null } }", 5, 6),
                CreateWhereTestCase(x => x.NS == null, "{ $match : { NS : null } }", 5, 6),
                CreateWhereTestCase(x => x.NS == x.T, "{ $match : { $expr : { $eq : ['$NS', '$T'] } } }", 1, 4),
                CreateWhereTestCase(x => x.NS == x.NT, "{ $match : { $expr : { $eq : ['$NS', '$NT'] } } }", 1, 6),
                CreateWhereTestCase(x => E.A == x.T, "{ $match : { T : 'A' } }", 1),
                CreateWhereTestCase(x => E.A == x.NT, "{ $match : { NT : 'A' } }", 1),
                CreateWhereTestCase(x => (E?)E.A == x.T, "{ $match : { T : 'A' } }", 1),
                CreateWhereTestCase(x => (E?)E.A == x.NT, "{ $match : { NT : 'A' } }", 1),
                CreateWhereTestCase(x => (E?)null == x.NT, "{ $match : { NT : null } }", 4, 6),
                CreateWhereTestCase(x => null == x.NT, "{ $match : { NT : null } }", 4, 6)
            };
        }

        public static IEnumerable<object[]> Where_with_enum_comparison_should_work_member_data()
        {
            for (var i = 0; i < __whereTestCases.Length; i++)
            {
                var predicateAsString = __whereTestCases[i].PredicateAsString;
                var expectedStage = __whereTestCases[i].ExpectedStage;
                var expectedResults = __whereTestCases[i].ExpectedResults;
                yield return new object[] { i, predicateAsString, expectedStage, expectedResults };
            }
        }

        [Theory]
        [MemberData(nameof(Where_with_enum_comparison_should_work_member_data))]
        public void Where_with_enum_comparison_should_work(int i, string predicateAsString, string expectedMatchStage, int[] expectedResults)
        {
            var collection = CreateCollection();
            var predicate = __whereTestCases[i].Predicate;
            expectedResults ??= new int[0];

            var queryable = collection
                .AsQueryable()
                .OrderBy(x => x.Id)
                .Where(predicate);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $sort : { _id : 1 } }",
                expectedMatchStage);

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(expectedResults);
        }

        [Fact]
        public void Comparison_of_enum_and_enum_with_mismatched_serializers_should_throw()
        {
            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .Where(x => x.E == x.S);

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
                .Where(x => x.E == x.NS);

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
                .Where(x => x.NE == x.S);

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
                .Where(x => x.NE == x.NS);

            var exception = Record.Exception(() => Translate(collection, queryable));

            exception.Should().BeOfType<ExpressionNotSupportedException>();
            exception.Message.Should().Contain("because the two enums being compared are serialized using different serializers");
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
