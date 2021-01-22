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
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq3;
using MongoDB.Driver.Linq3.Translators;
using MongoDB.Driver.Linq3.Translators.ExpressionToPipelineTranslators;
using Xunit;

namespace Tests.MongoDB.Driver.Linq3.Legacy.Translators
{
    public class AggregateGroupTranslatorTests : IntegrationTestBase
    {
        [Fact]
        public void Should_translate_using_non_anonymous_type_with_default_constructor()
        {
            var result = Group(x => x.A, g => new RootView { Property = g.Key, Field = g.First().B });

            result.Projection.Should().Be("{ $project : { Property : '$_id', Field : { $let : { vars : { this : { $arrayElemAt : [ '$_elements', 0 ] } }, in : '$$this.B' } }, _id : 0 } }");

            result.Value.Property.Should().Be("Amazing");
            result.Value.Field.Should().Be("Baby");
        }

        [Fact]
        public void Should_translate_using_non_anonymous_type_with_parameterized_constructor()
        {
            var result = Group(x => x.A, g => new RootView(g.Key) { Field = g.First().B });

            result.Projection.Should().Be("{ $project : { Property : '$_id', Field : { $let : { vars : { this : { $arrayElemAt : ['$_elements', 0] } }, in : '$$this.B' } }, _id : 0 } }");

            result.Value.Property.Should().Be("Amazing");
            result.Value.Field.Should().Be("Baby");
        }

        [Fact]
        public void Should_translate_just_id()
        {
            var result = Group(x => x.A, g => new { _id = g.Key });

            result.Projection.Should().Be("{ $project : { _id : '$_id' } }");

            result.Value._id.Should().Be("Amazing");
        }

        [Fact]
        public void Should_translate_id_when_not_named_specifically()
        {
            var result = Group(x => x.A, g => new { Test = g.Key });

            result.Projection.Should().Be("{ $project : { Test : '$_id', _id : 0 } }");

            result.Value.Test.Should().Be("Amazing");
        }

        [Fact]
        public void Should_translate_addToSet()
        {
            var result = Group(x => x.A, g => new { Result = new HashSet<int>(g.Select(x => x.C.E.F)) });

            result.Projection.Should().Be("{ $project : { Result : { $setUnion : ['$_elements.C.E.F'] }, _id : 0 } }");

            result.Value.Result.Should().Equal(111);
        }

        [Fact]
        public void Should_translate_addToSet_using_Distinct()
        {
            var result = Group(x => x.A, g => new { Result = g.Select(x => x.C.E.F).Distinct() });

            result.Projection.Should().Be("{ $project : { Result : { $setIntersection : ['$_elements.C.E.F'] }, _id : 0 } }");

            result.Value.Result.Should().Equal(111);
        }

        [Fact]
        public void Should_translate_average_with_embedded_projector()
        {
            var result = Group(x => x.A, g => new { Result = g.Average(x => x.C.E.F) });

            result.Projection.Should().Be("{ $project : { Result : { $avg : '$_elements.C.E.F' }, _id : 0 } }");

            result.Value.Result.Should().Be(111);
        }

        [Fact]
        public void Should_translate_average_with_selected_projector()
        {
            var result = Group(x => x.A, g => new { Result = g.Select(x => x.C.E.F).Average() });

            result.Projection.Should().Be("{ $project : { Result : { $avg : '$_elements.C.E.F' }, _id : 0 } }");

            result.Value.Result.Should().Be(111);
        }

        [Fact]
        public void Should_translate_count()
        {
            var result = Group(x => x.A, g => new { Result = g.Count() });

            result.Projection.Should().Be("{ $project : { Result : { $size : '$_elements' }, _id : 0 } }");

            result.Value.Result.Should().Be(1);
        }

        [Fact]
        public void Should_translate_count_with_a_predicate()
        {
            var result = Group(x => x.A, g => new { Result = g.Count(x => x.A != "Awesome") });

            result.Projection.Should().Be("{ $project : { Result : { $size : { $filter : { input : '$_elements', as : 'x', cond : { $ne : ['$$x.A', 'Awesome' ] } } } }, _id : 0 } }");

            result.Value.Result.Should().Be(1);
        }

        [Fact]
        public void Should_translate_where_with_a_predicate_and_count()
        {
            var result = Group(x => x.A, g => new { Result = g.Where(x => x.A != "Awesome").Count() });

            result.Projection.Should().Be("{ $project : { Result : { $size : { $filter : { input : '$_elements', as : 'x', cond : { $ne : ['$$x.A', 'Awesome'] } } } }, _id : 0 } }");

            result.Value.Result.Should().Be(1);
        }

        [Fact]
        public void Should_translate_where_select_and_count_with_predicates()
        {
            var result = Group(x => x.A, g => new { Result = g.Select(x => new { A = x.A }).Count(x => x.A != "Awesome") });

            result.Projection.Should().Be("{ $project : { Result : { $size : { $filter : { input : { $map : { input : '$_elements', as : 'x', in : { A : '$$x.A' } } }, as : 'x', cond : { $ne : ['$$x.A', 'Awesome'] } } } }, _id : 0 } }");

            result.Value.Result.Should().Be(1);
        }

        [Fact]
        public void Should_translate_where_select_with_predicate_and_count()
        {
            var result = Group(x => x.A, g => new { Result = g.Select(x => new { A = x.A }).Count() });

            result.Projection.Should().Be("{ $project : { Result : { $size : { $map : { input : '$_elements', as : 'x', in : { A : '$$x.A' } } } }, _id : 0 } }");

            result.Value.Result.Should().Be(1);
        }

        [Fact]
        public void Should_translate_long_count()
        {
            var result = Group(x => x.A, g => new { Result = g.LongCount() });

            result.Projection.Should().Be("{ $project : { Result : { $size : '$_elements' }, _id : 0 } }");

            result.Value.Result.Should().Be(1);
        }

        [Fact]
        public void Should_translate_first()
        {
            var result = Group(x => x.A, g => new { B = g.Select(x => x.B).First() });

            result.Projection.Should().Be("{ $project : { B : { $arrayElemAt : ['$_elements.B', 0] }, _id : 0 } }");

            result.Value.B.Should().Be("Baby");
        }

        [Fact]
        public void Should_translate_first_with_normalization()
        {
            var result = Group(x => x.A, g => new { g.First().B });

            result.Projection.Should().Be("{ $project : { B : { $let : { vars : { this : { $arrayElemAt : ['$_elements', 0] } }, in : '$$this.B' } }, _id : 0 } }");

            result.Value.B.Should().Be("Baby");
        }

        [Fact]
        public void Should_translate_last()
        {
            var result = Group(x => x.A, g => new { B = g.Select(x => x.B).Last() });

            result.Projection.Should().Be("{ $project : { B : { $arrayElemAt : ['$_elements.B', -1] }, _id : 0 } }");

            result.Value.B.Should().Be("Baby");
        }

        [Fact]
        public void Should_translate_last_with_normalization()
        {
            var result = Group(x => x.A, g => new { g.Last().B });

            result.Projection.Should().Be("{ $project : { B : { $let : { vars : { this : { $arrayElemAt : ['$_elements', -1] } }, in : '$$this.B' } }, _id : 0 } }");

            result.Value.B.Should().Be("Baby");
        }

        [Fact]
        public void Should_throw_an_exception_when_last_is_used_with_a_predicate()
        {
            Action act = () => Group(x => x.A, g => new { g.Last(x => x.A == "bin").B });

            act.ShouldThrow<NotSupportedException>();
        }

        [Fact]
        public void Should_translate_max_with_embedded_projector()
        {
            var result = Group(x => x.A, g => new { Result = g.Max(x => x.C.E.F) });

            result.Projection.Should().Be("{ $project : { Result : { $max : '$_elements.C.E.F' }, _id : 0 } }");

            result.Value.Result.Should().Be(111);
        }

        [Fact]
        public void Should_translate_max_with_selected_projector()
        {
            var result = Group(x => x.A, g => new { Result = g.Select(x => x.C.E.F).Max() });

            result.Projection.Should().Be("{ $project : { Result : { $max : '$_elements.C.E.F' }, _id : 0 } }");

            result.Value.Result.Should().Be(111);
        }

        [Fact]
        public void Should_translate_min_with_embedded_projector()
        {
            var result = Group(x => x.A, g => new { Result = g.Min(x => x.C.E.F) });

            result.Projection.Should().Be("{ $project : { Result : { $min : '$_elements.C.E.F' }, _id : 0 } }");

            result.Value.Result.Should().Be(111);
        }

        [Fact]
        public void Should_translate_min_with_selected_projector()
        {
            var result = Group(x => x.A, g => new { Result = g.Select(x => x.C.E.F).Min() });

            result.Projection.Should().Be("{ $project : { Result : { $min : '$_elements.C.E.F' }, _id : 0 } }");

            result.Value.Result.Should().Be(111);
        }

        [Fact]
        public void Should_translate_push_with_just_a_select()
        {
            var result = Group(x => x.A, g => new { Result = g.Select(x => x.C.E.F) });

            result.Projection.Should().Be("{ $project : { Result : '$_elements.C.E.F', _id : 0 } }");

            result.Value.Result.Should().Equal(111);
        }

        [Fact]
        public void Should_translate_push_with_ToArray()
        {
            var result = Group(x => x.A, g => new { Result = g.Select(x => x.C.E.F).ToArray() });

            result.Projection.Should().Be("{ $project : { Result : '$_elements.C.E.F', _id : 0 } }");

            result.Value.Result.Should().Equal(111);
        }

        [Fact]
        public void Should_translate_push_with_new_list()
        {
            var result = Group(x => x.A, g => new { Result = new List<int>(g.Select(x => x.C.E.F)) });

            result.Projection.Should().Be("{ $project : { Result : '$_elements.C.E.F', _id : 0 } }");

            result.Value.Result.Should().Equal(111);
        }

        [Fact]
        public void Should_translate_push_with_ToList()
        {
            var result = Group(x => x.A, g => new { Result = g.Select(x => x.C.E.F).ToList() });

            result.Projection.Should().Be("{ $project : { Result : '$_elements.C.E.F', _id : 0 } }");

            result.Value.Result.Should().Equal(111);
        }

        [SkippableFact]
        public void Should_translate_stdDevPop_with_embedded_projector()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.1.7");

            var result = Group(x => 1, g => new { Result = g.StandardDeviationPopulation(x => x.C.E.F) });

            result.Projection.Should().Be("{ $project : { Result : { $stdDevPop : '$_elements.C.E.F' }, _id : 0 } }");

            result.Value.Result.Should().Be(50);
        }

        [SkippableFact]
        public void Should_translate_stdDevPop_with_selected_projector()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.1.7");

            var result = Group(x => 1, g => new { Result = g.Select(x => x.C.E.F).StandardDeviationPopulation() });

            result.Projection.Should().Be("{ $project : { Result : { $stdDevPop : '$_elements.C.E.F' }, _id : 0 } }");

            result.Value.Result.Should().Be(50);
        }

        [SkippableFact]
        public void Should_translate_stdDevSamp_with_embedded_projector()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.1.7");

            var result = Group(x => 1, g => new { Result = g.StandardDeviationSample(x => x.C.E.F) });

            result.Projection.Should().Be("{ $project : { Result : { $stdDevSamp : '$_elements.C.E.F' }, _id : 0 } }");

            result.Value.Result.Should().BeApproximately(70.7106781156545, .0001);
        }

        [SkippableFact]
        public void Should_translate_stdDevSamp_with_selected_projector()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.1.7");

            var result = Group(x => 1, g => new { Result = g.Select(x => x.C.E.F).StandardDeviationSample() });

            result.Projection.Should().Be("{ $project : { Result : { $stdDevSamp : '$_elements.C.E.F' }, _id : 0 } }");

            result.Value.Result.Should().BeApproximately(70.7106781156545, .0001);
        }

        [Fact]
        public void Should_translate_sum_with_embedded_projector()
        {
            var result = Group(x => x.A, g => new { Result = g.Sum(x => x.C.E.F) });

            result.Projection.Should().Be("{ $project : { Result : { $sum : '$_elements.C.E.F' }, _id : 0 } }");

            result.Value.Result.Should().Be(111);
        }

        [Fact]
        public void Should_translate_sum_with_selected_projector()
        {
            var result = Group(x => x.A, g => new { Result = g.Select(x => x.C.E.F).Sum() });

            result.Projection.Should().Be("{ $project : { Result : { $sum : '$_elements.C.E.F' }, _id : 0 } }");

            result.Value.Result.Should().Be(111);
        }

        [Fact]
        public void Should_translate_complex_selector()
        {
            var result = Group(x => x.A, g => new
            {
                Count = g.Count(),
                Sum = g.Sum(x => x.C.E.F + x.C.E.H),
                First = g.First().B,
                Last = g.Last().K,
                Min = g.Min(x => x.C.E.F + x.C.E.H),
                Max = g.Max(x => x.C.E.F + x.C.E.H)
            });

            result.Projection.Should().Be(
                @"
                {
                    $project : {
                        Count : { $size : '$_elements' },
                        Sum : { $sum : { $map : { input : '$_elements', as : 'x', in : { $add : ['$$x.C.E.F', '$$x.C.E.H'] } } } },
                        First : { $let : { vars : { this : { $arrayElemAt : ['$_elements', 0] } }, in : '$$this.B' } },
                        Last : { $let : { vars : { this : { $arrayElemAt : ['$_elements', -1] } }, in : '$$this.K' } },
                        Min : { $min : { $map : { input : '$_elements', as : 'x', in : { $add : ['$$x.C.E.F', '$$x.C.E.H'] } } } },
                        Max : { $max : { $map : { input : '$_elements', as : 'x', in : { $add : ['$$x.C.E.F', '$$x.C.E.H'] } } } },
                        _id : 0
                    }
                }");

            result.Value.Count.Should().Be(1);
            result.Value.Sum.Should().Be(333);
            result.Value.First.Should().Be("Baby");
            result.Value.Last.Should().Be(false);
            result.Value.Min.Should().Be(333);
            result.Value.Max.Should().Be(333);
        }

        [Fact]
        public void Should_translate_aggregate_expressions_with_user_provided_serializer_if_possible()
        {
            var result = Group(x => 1, g => new
            {
                Sum = g.Sum(x => x.U)
            });

            result.Projection.Should().Be("{ _id : 1, Sum : { \"$sum\" : \"$U\" } }");

            result.Value.Sum.Should().Be(-0.00000000714529169165701m);
        }

        private ProjectedResult<TResult> Group<TKey, TResult>(Expression<Func<Root, TKey>> idProjector, Expression<Func<IGrouping<TKey, Root>, TResult>> groupProjector)
        {
            return Group(idProjector, groupProjector, null);
        }

        private ProjectedResult<TResult> Group<TKey, TResult>(Expression<Func<Root, TKey>> idProjector, Expression<Func<IGrouping<TKey, Root>, TResult>> groupProjector, ExpressionTranslationOptions translationOptions)
        {
            var queryable = __collection.AsQueryable3()
                .GroupBy(idProjector)
                .Select(groupProjector);

            var context = new TranslationContext();
            var executableQuery = ExpressionToPipelineTranslator.Translate(context, queryable.Expression);

            var stages = executableQuery.Stages.Select(s => s.Render()).Cast<BsonDocument>().ToList();
            stages.Insert(1, new BsonDocument("$sort", new BsonDocument("_id", 1))); // force a standard order for testing purposes
            var pipeline = new BsonDocumentStagePipelineDefinition<Root, TResult>(stages, outputSerializer: (IBsonSerializer<TResult>)executableQuery.OutputSerializer);
            var results = __collection.Aggregate(pipeline).ToList();

            return new ProjectedResult<TResult>
            {
                Projection = stages[2],
                Value = results[0]
            };
        }

        private class ProjectedResult<T>
        {
            public BsonDocument Projection { get; set; }
            public T Value { get; set; }
        }
    }
}
