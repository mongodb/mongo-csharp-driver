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
    public class CSharp4337Tests : Linq3IntegrationTest
    {
        private static (Expression<Func<C, R<bool>>> Projection, string ExpectedStage, bool[] ExpectedResults)[] __predicate_should_use_correct_representation_test_cases = new (Expression<Func<C, R<bool>>> Projection, string ExpectedStage, bool[] ExpectedResults)[]
        {
            (d => new R<bool> { N = d.Id, V = d.I1 == E.E1 ? true : false }, "{ $project : { N : '$_id', V : { $cond : { if : { $eq : ['$I1', 1] }, then : true, else : false } }, _id : 0 } }", new[] { true, false }),
            (d => new R<bool> { N = d.Id, V = d.S1 == E.E1 ? true : false }, "{ $project : { N : '$_id', V : { $cond : { if : { $eq : ['$S1', 'E1'] }, then : true, else : false } }, _id : 0 } }", new[] { true, false }),
            (d => new R<bool> { N = d.Id, V = E.E1 == d.I1 ? true : false }, "{ $project : { N : '$_id', V : { $cond : { if : { $eq : [1, '$I1'] }, then : true, else : false } }, _id : 0 } }", new[] { true, false }),
            (d => new R<bool> { N = d.Id, V = E.E1 == d.S1 ? true : false }, "{ $project : { N : '$_id', V : { $cond : { if : { $eq : ['E1', '$S1'] }, then : true, else : false } }, _id : 0 } }", new[] { true, false })
        };

        public static IEnumerable<object[]> Predicate_should_use_correct_representation_member_data()
        {
            var testCases = __predicate_should_use_correct_representation_test_cases;
            for (var i = 0; i < testCases.Length; i++)
            {
                yield return new object[] { i, testCases[i].Projection.ToString(), testCases[i].ExpectedStage, testCases[i].ExpectedResults };
            }
        }

        [Theory]
        [MemberData(nameof(Predicate_should_use_correct_representation_member_data))]
        public void Predicate_should_use_correct_representation(int i, string projectionAsString, string expectedStage, bool[] expectedResults)
        {
            var collection = CreateCollection();
            var projection = __predicate_should_use_correct_representation_test_cases[i].Projection;

            var aggregate = collection.Aggregate()
                .Project(projection);

            var stages = Translate(collection, aggregate);
            AssertStages(stages, expectedStage);

            var results = aggregate.ToList();
            results.OrderBy(r => r.N).Select(r => r.V).Should().Equal(expectedResults);
        }

        private static (Expression<Func<C, R<E>>> Projection, string ExpectedStage, E[] ExpectedResults)[] __result_should_use_correct_representation_test_cases = new (Expression<Func<C, R<E>>> Projection, string ExpectedStage, E[] ExpectedResults)[]
        {
            (d => new R<E> { N = d.Id, V = true ? E.E1 : E.E2 }, "{ $project : { N : '$_id', V : { $literal : 1 }, _id : 0 } }", new[] { E.E1, E.E1 }),
            (d => new R<E> { N = d.Id, V = d.I1 == E.E1 ? E.E1 : E.E2 }, "{ $project : { N : '$_id', V : { $cond : { if : { $eq : ['$I1', 1] }, then : 1, else : 2 } }, _id : 0 } }", new[] { E.E1, E.E2 }),
            (d => new R<E> { N = d.Id, V = d.I1 == E.E1 ? d.I1 : E.E2 }, "{ $project : { N : '$_id', V : { $cond : { if : { $eq : ['$I1', 1] }, then : '$I1', else : 2 } }, _id : 0 } }", new[] { E.E1, E.E2 }),
            (d => new R<E> { N = d.Id, V = d.I1 == E.E1 ? E.E1 : d.I2 }, "{ $project : { N : '$_id', V : { $cond : { if : { $eq : ['$I1', 1] }, then : 1, else : '$I2' } }, _id : 0 } }", new[] { E.E1, E.E2 }),
            (d => new R<E> { N = d.Id, V = d.I1 == E.E1 ? d.I1 : d.I2 }, "{ $project : { N : '$_id', V : { $cond : { if : { $eq : ['$I1', 1] }, then : '$I1', else : '$I2' } }, _id : 0 } }", new[] { E.E1, E.E2 }),
            (d => new R<E> { N = d.Id, V = d.I1 == E.E1 ? d.S1 : E.E2 }, "{ $project : { N : '$_id', V : { $cond : { if : { $eq : ['$I1', 1] }, then : '$S1', else : 'E2' } }, _id : 0 } }", new[] { E.E1, E.E2 }),
            (d => new R<E> { N = d.Id, V = d.I1 == E.E1 ? E.E1 : d.S2 }, "{ $project : { N : '$_id', V : { $cond : { if : { $eq : ['$I1', 1] }, then : 'E1', else : '$S2' } }, _id : 0 } }", new[] { E.E1, E.E2 }),
            (d => new R<E> { N = d.Id, V = d.I1 == E.E1 ? d.S1 : d.S2 }, "{ $project : { N : '$_id', V : { $cond : { if : { $eq : ['$I1', 1] }, then : '$S1', else : '$S2' } }, _id : 0 } }", new[] { E.E1, E.E2 }),
            (d => new R<E> { N = d.Id, V = d.S1 == E.E1 ? E.E1 : E.E2 }, "{ $project : { N : '$_id', V : { $cond : { if : { $eq : ['$S1', 'E1'] }, then : 'E1', else : 'E2' } }, _id : 0 } }", new[] { E.E1, E.E2 }),
            (d => new R<E> { N = d.Id, V = d.S1 == E.E1 ? d.I1 : E.E2 }, "{ $project : { N : '$_id', V : { $cond : { if : { $eq : ['$S1', 'E1'] }, then : '$I1', else : 2 } }, _id : 0 } }", new[] { E.E1, E.E2 }),
            (d => new R<E> { N = d.Id, V = d.S1 == E.E1 ? E.E1 : d.I2 }, "{ $project : { N : '$_id', V : { $cond : { if : { $eq : ['$S1', 'E1'] }, then : 1, else : '$I2' } }, _id : 0 } }", new[] { E.E1, E.E2 }),
            (d => new R<E> { N = d.Id, V = d.S1 == E.E1 ? d.I1 : d.I2 }, "{ $project : { N : '$_id', V : { $cond : { if : { $eq : ['$S1', 'E1'] }, then : '$I1', else : '$I2' } }, _id : 0 } }", new[] { E.E1, E.E2 }),
            (d => new R<E> { N = d.Id, V = d.S1 == E.E1 ? d.S1 : E.E2 }, "{ $project : { N : '$_id', V : { $cond : { if : { $eq : ['$S1', 'E1'] }, then : '$S1', else : 'E2' } }, _id : 0 } }", new[] { E.E1, E.E2 }),
            (d => new R<E> { N = d.Id, V = d.S1 == E.E1 ? E.E1 : d.S2 }, "{ $project : { N : '$_id', V : { $cond : { if : { $eq : ['$S1', 'E1'] }, then : 'E1', else : '$S2' } }, _id : 0 } }", new[] { E.E1, E.E2 }),
            (d => new R<E> { N = d.Id, V = d.S1 == E.E1 ? d.S1 : d.S2 }, "{ $project : { N : '$_id', V : { $cond : { if : { $eq : ['$S1', 'E1'] }, then : '$S1', else : '$S2' } }, _id : 0 } }", new[] { E.E1, E.E2 }),
        };

        public static IEnumerable<object[]> Result_should_use_correct_representation_member_data()
        {
            var testCases = __result_should_use_correct_representation_test_cases;
            for (var i = 0; i < testCases.Length; i++)
            {
                yield return new object[] { i, testCases[i].Projection.ToString(), testCases[i].ExpectedStage, testCases[i].ExpectedResults };
            }
        }

        [Theory]
        [MemberData(nameof(Result_should_use_correct_representation_member_data))]
        public void Result_should_use_correct_representation(int i, string projectionAsString, string expectedStage, E[] expectedResults)
        {
            var collection = CreateCollection();
            var projection = __result_should_use_correct_representation_test_cases[i].Projection;

            var aggregate = collection.Aggregate()
                .Project(projection);

            var stages = Translate(collection, aggregate);
            AssertStages(stages, expectedStage);

            var results = aggregate.ToList();
            results.OrderBy(r => r.N).Select(r => r.V).Should().Equal(expectedResults);
        }

        private static Expression<Func<C, R<E>>>[] __result_with_mixed_representations_should_throw_test_cases = new Expression<Func<C, R<E>>>[]
        {
            d => new R<E> { N = d.Id, V = d.I1 == E.E1 ? d.I1 : d.S2 },
            d => new R<E> { N = d.Id, V = d.I1 == E.E1 ? d.S1 : d.I2 }
        };

        public static IEnumerable<object[]> Result_with_mixed_representations_should_throw_member_data()
        {
            var testCases = __result_with_mixed_representations_should_throw_test_cases;
            for (var i = 0; i < testCases.Length; i++)
            {
                yield return new object[] { i, testCases[i].ToString() };
            }
        }

        [Theory]
        [MemberData(nameof(Result_with_mixed_representations_should_throw_member_data))]
        public void Result_with_mixed_representations_should_throw(int i, string projectionAsString)
        {
            var collection = CreateCollection();
            var projection = __result_with_mixed_representations_should_throw_test_cases[i];

            var aggregate = collection.Aggregate()
                .Project(projection);

            List<BsonDocument> stages;
            var exception = Record.Exception(() => stages = Translate(collection, aggregate));

            var notSupportedException = exception.Should().BeOfType<ExpressionNotSupportedException>().Subject;
            notSupportedException.Message.Should().Contain("because IfTrue and IfFalse expressions have different serializers");
        }

        private IMongoCollection<C> CreateCollection()
        {
            var collection = GetCollection<C>();

            CreateCollection(
                collection,
                new C { Id = 1, I1 = E.E1, I2 = E.E1, S1 = E.E1, S2 = E.E1 },
                new C { Id = 2, I1 = E.E2, I2 = E.E2, S1 = E.E2, S2 = E.E2 });

            return collection;
        }

        public class C
        {
            public int Id { get; set; }

            public E I1 { get; set; }
            public E I2 { get; set; }

            [BsonRepresentation(BsonType.String)] public E S1 { get; set; }
            [BsonRepresentation(BsonType.String)] public E S2 { get; set; }
        }

        public class R<TValue>
        {
            public int N { get; set; }
            public TValue V { get; set; }
        }

        public enum E { E1 = 1, E2 }
    }
}
