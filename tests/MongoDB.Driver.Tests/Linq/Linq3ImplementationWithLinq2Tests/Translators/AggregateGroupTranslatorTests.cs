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
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Linq.Linq3Implementation.Ast;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Optimizers;
using MongoDB.Driver.Linq.Linq3Implementation.Translators;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToPipelineTranslators;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationWithLinq2Tests.Translators
{
    public class AggregateGroupTranslatorTests : IntegrationTestBase
    {
        [Fact]
        public void Should_translate_using_non_anonymous_type_with_default_constructor()
        {
            var result = Group(x => x.A, g => new RootView { Property = g.Key, Field = g.First().B });

            AssertStages(
                result.Stages,
                "{ $group : { _id : '$A', __agg0 : { $first : '$B' } } }",
                "{ $project : { Property : '$_id', Field : '$__agg0', _id : 0 } }");

            result.Value.Property.Should().Be("Amazing");
            result.Value.Field.Should().Be("Baby");
        }

        [Fact]
        public void Should_translate_using_non_anonymous_type_with_parameterized_constructor()
        {
            var result = Group(x => x.A, g => new RootView(g.Key) { Field = g.First().B });

            AssertStages(
                result.Stages,
                "{ $group : { _id : '$A', __agg0 : { $first : '$B' } } }",
                "{ $project : { Property : '$_id', Field : '$__agg0', _id : 0 } }");

            result.Value.Property.Should().Be("Amazing");
            result.Value.Field.Should().Be("Baby");
        }

        [Fact]
        public void Should_translate_just_id()
        {
            var result = Group(x => x.A, g => new { _id = g.Key });

            AssertStages(
                result.Stages,
                "{ $group : { _id : '$A' } }",
                "{ $project : { _id : '$_id' } }");

            result.Value._id.Should().Be("Amazing");
        }

        [Fact]
        public void Should_translate_id_when_not_named_specifically()
        {
            var result = Group(x => x.A, g => new { Test = g.Key });

            AssertStages(
                result.Stages,
                "{ $group : { _id : '$A' } }",
                "{ $project : { Test : '$_id', _id : 0 } }");

            result.Value.Test.Should().Be("Amazing");
        }

        [Fact]
        public void Should_translate_addToSet()
        {
            var result = Group(x => x.A, g => new { Result = new HashSet<int>(g.Select(x => x.C.E.F)) });

            AssertStages(
                result.Stages,
                "{ $group : { _id : '$A', __agg0 : { $addToSet : '$C.E.F' } } }",
                "{ $project : { Result : '$__agg0', _id : 0 } }");

            result.Value.Result.Should().Equal(111);
        }

        [Fact]
        public void Should_translate_addToSet_using_Distinct()
        {
            var result = Group(x => x.A, g => new { Result = g.Select(x => x.C.E.F).Distinct() });

            AssertStages(
                result.Stages,
                "{ $group : { _id : '$A', __agg0 : { $addToSet : '$C.E.F' } } }",
                "{ $project : { Result : '$__agg0', _id : 0 } }");

            result.Value.Result.Should().Equal(111);
        }

        [Fact]
        public void Should_translate_average_with_embedded_projector()
        {
            var result = Group(x => x.A, g => new { Result = g.Average(x => x.C.E.F) });

            AssertStages(
                 result.Stages,
               "{ $group : { _id : '$A', __agg0 : { $avg : '$C.E.F' } } }",
                "{ $project : { Result : '$__agg0', _id : 0 } }");

            result.Value.Result.Should().Be(111);
        }

        [Fact]
        public void Should_translate_average_with_selected_projector()
        {
            var result = Group(x => x.A, g => new { Result = g.Select(x => x.C.E.F).Average() });

            AssertStages(
                result.Stages,
                "{ $group : { _id : '$A', __agg0 : { $avg : '$C.E.F' } } }",
                "{ $project : { Result : '$__agg0', _id : 0 } }");

            result.Value.Result.Should().Be(111);
        }

        [Fact]
        public void Should_translate_count()
        {
            var result = Group(x => x.A, g => new { Result = g.Count() });

            AssertStages(
                result.Stages,
                "{ $group : { _id : '$A', __agg0 : { $sum : 1 } } }",
                "{ $project : { Result : '$__agg0', _id : 0 } }");

            result.Value.Result.Should().Be(1);
        }

        [Fact]
        public void Should_translate_count_with_a_predicate()
        {
            var result = Group(x => x.A, g => new { Result = g.Count(x => x.A != "Awesome") });

            AssertStages(
                result.Stages,
                "{ $group : { _id : '$A', __agg0 : { $sum : { $cond : { if : { $ne : ['$A', 'Awesome'] }, then : 1, else : 0 } } } } }",
                "{ $project : { Result : '$__agg0', _id : 0 } }");

            result.Value.Result.Should().Be(1);
        }

        [Fact]
        public void Should_translate_where_with_a_predicate_and_count()
        {
            var result = Group(x => x.A, g => new { Result = g.Where(x => x.A != "Awesome").Count() });

            AssertStages(
                result.Stages,
                "{ $group : { _id : '$A', _elements : { $push : '$$ROOT' } } }",
                "{ $project : { Result : { $size : { $filter : { input : '$_elements', as : 'x', cond : { $ne : ['$$x.A', 'Awesome'] } } } }, _id : 0 } }");

            result.Value.Result.Should().Be(1);
        }

        [Fact]
        public void Should_translate_where_select_and_count_with_predicates()
        {
            var result = Group(x => x.A, g => new { Result = g.Select(x => new { B = x.A }).Count(x => x.B != "Awesome") });

            AssertStages(
                result.Stages,
                "{ $group : { _id : '$A', __agg0 : { $push : { B : '$A' } } } }",
                "{ $project : { Result : { $sum : { $map : { input : '$__agg0', as : 'x', in : { $cond : { if : { $ne : ['$$x.B', 'Awesome'] }, then : 1, else : 0 } } } } }, _id : 0 } }");

            result.Value.Result.Should().Be(1);
        }

        [Fact]
        public void Should_translate_where_select_with_predicate_and_count()
        {
            var result = Group(x => x.A, g => new { Result = g.Select(x => new { A = x.A }).Count() });

            AssertStages(
                result.Stages,
                "{ $group : { _id : '$A', __agg0 : { $push : { A : '$A' } } } }",
                "{ $project : { Result : { $size : '$__agg0' }, _id : 0 } }");

            result.Value.Result.Should().Be(1);
        }

        [Fact]
        public void Should_translate_long_count()
        {
            var result = Group(x => x.A, g => new { Result = g.LongCount() });

            AssertStages(
                result.Stages,
                "{ $group : { _id : '$A', __agg0 : { $sum : 1 } } }",
                "{ $project : { Result : '$__agg0', _id : 0 } }");

            result.Value.Result.Should().Be(1);
        }

        [Fact]
        public void Should_translate_first()
        {
            var result = Group(x => x.A, g => new { B = g.Select(x => x.B).First() });

            AssertStages(
                result.Stages,
                "{ $group : { _id : '$A', __agg0 : { $first : '$B' } } }",
                "{ $project : { B : '$__agg0', _id : 0 } }");

            result.Value.B.Should().Be("Baby");
        }

        [Fact]
        public void Should_translate_first_with_normalization()
        {
            var result = Group(x => x.A, g => new { g.First().B });

            AssertStages(
                result.Stages,
                "{ $group : { _id : '$A', __agg0 : { $first : '$B' } } }",
                "{ $project : { B : '$__agg0', _id : 0 } }");

            result.Value.B.Should().Be("Baby");
        }

        [Fact]
        public void Should_translate_last()
        {
            var result = Group(x => x.A, g => new { B = g.Select(x => x.B).Last() });

            AssertStages(
                result.Stages,
                "{ $group : { _id : '$A', __agg0 : { $last : '$B' } } }",
                "{ $project : { B : '$__agg0', _id : 0 } }");

            result.Value.B.Should().Be("Baby");
        }

        [Fact]
        public void Should_translate_last_with_normalization()
        {
            var result = Group(x => x.A, g => new { g.Last().B });

            AssertStages(
                result.Stages,
                "{ $group : { _id : '$A', __agg0 : { $last : '$B' } } }",
                "{ $project : { B : '$__agg0', _id : 0 } }");

            result.Value.B.Should().Be("Baby");
        }

        [Fact]
        public void Should_translate_last_with_a_predicate()
        {
            var result = Group(x => x.A, g => new { g.Last(x => x.A != "").B }); // TODO: there is an issue when no items match the predicate for Last

            AssertStages(
                result.Stages,
                "{ $group : { _id : '$A', _elements : { $push : '$$ROOT' } } }",
                "{ $project : { B : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : '$_elements', as : 'x', cond : { $ne : ['$$x.A', ''] } } }, -1] } }, in : '$$this.B' } }, _id : 0 } }");

            result.Value.B.Should().Be("Baby");
        }

        [Fact]
        public void Should_translate_max_with_embedded_projector()
        {
            var result = Group(x => x.A, g => new { Result = g.Max(x => x.C.E.F) });

            AssertStages(
                result.Stages,
                "{ $group : { _id : '$A', __agg0 : { $max : '$C.E.F' } } }",
                "{ $project : { Result : '$__agg0', _id : 0 } }");

            result.Value.Result.Should().Be(111);
        }

        [Fact]
        public void Should_translate_max_with_selected_projector()
        {
            var result = Group(x => x.A, g => new { Result = g.Select(x => x.C.E.F).Max() });

            AssertStages(
                result.Stages,
                "{ $group : { _id : '$A', __agg0 : { $max : '$C.E.F' } } }",
                "{ $project : { Result : '$__agg0', _id : 0 } }");

            result.Value.Result.Should().Be(111);
        }

        [Fact]
        public void Should_translate_min_with_embedded_projector()
        {
            var result = Group(x => x.A, g => new { Result = g.Min(x => x.C.E.F) });

            AssertStages(
                result.Stages,
                "{ $group : { _id : '$A', __agg0 : { $min : '$C.E.F' } } }",
                "{ $project : { Result : '$__agg0', _id : 0 } }");

            result.Value.Result.Should().Be(111);
        }

        [Fact]
        public void Should_translate_min_with_selected_projector()
        {
            var result = Group(x => x.A, g => new { Result = g.Select(x => x.C.E.F).Min() });

            AssertStages(
                result.Stages,
                "{ $group : { _id : '$A', __agg0 : { $min : '$C.E.F' } } }",
                "{ $project : { Result : '$__agg0', _id : 0 } }");

            result.Value.Result.Should().Be(111);
        }

        [Fact]
        public void Should_translate_push_with_just_a_select()
        {
            var result = Group(x => x.A, g => new { Result = g.Select(x => x.C.E.F) });

            AssertStages(
                result.Stages,
                "{ $group : { _id : '$A', __agg0 : { $push : '$C.E.F' } } }",
                "{ $project : { Result : '$__agg0', _id : 0 } }");

            result.Value.Result.Should().Equal(111);
        }

        [Fact]
        public void Should_translate_push_with_ToArray()
        {
            var result = Group(x => x.A, g => new { Result = g.Select(x => x.C.E.F).ToArray() });

            AssertStages(
                result.Stages,
                "{ $group : { _id : '$A', __agg0 : { $push : '$C.E.F' } } }",
                "{ $project : { Result : '$__agg0', _id : 0 } }");

            result.Value.Result.Should().Equal(111);
        }

        [Fact]
        public void Should_translate_push_with_new_list()
        {
            var result = Group(x => x.A, g => new { Result = new List<int>(g.Select(x => x.C.E.F)) });

            AssertStages(
                result.Stages,
                "{ $group : { _id : '$A', __agg0 : { $push : '$C.E.F' } } }",
                "{ $project : { Result : '$__agg0', _id : 0 } }");

            result.Value.Result.Should().Equal(111);
        }

        [Fact]
        public void Should_translate_push_with_ToList()
        {
            var result = Group(x => x.A, g => new { Result = g.Select(x => x.C.E.F).ToList() });

            AssertStages(
                result.Stages,
                "{ $group : { _id : '$A', __agg0 : { $push : '$C.E.F' } } }",
                "{ $project : { Result : '$__agg0', _id : 0 } }");

            result.Value.Result.Should().Equal(111);
        }

        [Fact]
        public void Should_translate_stdDevPop_with_embedded_projector()
        {
            RequireServer.Check();

            var result = Group(x => 1, g => new { Result = g.StandardDeviationPopulation(x => x.C.E.F) });

            AssertStages(
                result.Stages,
                "{ $group : { _id : 1, __agg0 : { $stdDevPop : '$C.E.F' } } }",
                "{ $project : { Result : '$__agg0', _id : 0 } }");

            result.Value.Result.Should().Be(50);
        }

        [Fact]
        public void Should_translate_stdDevPop_with_selected_projector()
        {
            RequireServer.Check();

            var result = Group(x => 1, g => new { Result = g.Select(x => x.C.E.F).StandardDeviationPopulation() });

            AssertStages(
                result.Stages,
                "{ $group : { _id : 1, __agg0 : { $stdDevPop : '$C.E.F' } } }",
                "{ $project : { Result : '$__agg0', _id : 0 } }");

            result.Value.Result.Should().Be(50);
        }

        [Fact]
        public void Should_translate_stdDevSamp_with_embedded_projector()
        {
            RequireServer.Check();

            var result = Group(x => 1, g => new { Result = g.StandardDeviationSample(x => x.C.E.F) });

            AssertStages(
                result.Stages,
                "{ $group : { _id : 1, __agg0 : { $stdDevSamp : '$C.E.F' } } }",
                "{ $project : { Result : '$__agg0', _id : 0 } }");

            result.Value.Result.Should().BeApproximately(70.7106781156545, .0001);
        }

        [Fact]
        public void Should_translate_stdDevSamp_with_selected_projector()
        {
            RequireServer.Check();

            var result = Group(x => 1, g => new { Result = g.Select(x => x.C.E.F).StandardDeviationSample() });

            AssertStages(
                result.Stages,
                "{ $group : { _id : 1, __agg0 : { $stdDevSamp : '$C.E.F' } } }",
                "{ $project : { Result : '$__agg0', _id : 0 } }");

            result.Value.Result.Should().BeApproximately(70.7106781156545, .0001);
        }

        [Fact]
        public void Should_translate_sum_with_embedded_projector()
        {
            var result = Group(x => x.A, g => new { Result = g.Sum(x => x.C.E.F) });

            AssertStages(
                result.Stages,
                "{ $group : { _id : '$A', __agg0 : { $sum : '$C.E.F' } } }",
                "{ $project : { Result : '$__agg0', _id : 0 } }");

            result.Value.Result.Should().Be(111);
        }

        [Fact]
        public void Should_translate_sum_with_selected_projector()
        {
            var result = Group(x => x.A, g => new { Result = g.Select(x => x.C.E.F).Sum() });

            AssertStages(
                result.Stages,
                "{ $group : { _id : '$A', __agg0 : { $sum : '$C.E.F' } } }",
                "{ $project : { Result : '$__agg0', _id : 0 } }");

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

            AssertStages(
                result.Stages,
                @"
                {
                    $group : {
                        _id : '$A',
                        __agg0 : { $sum : 1 },
                        __agg1 : { $sum : { $add : ['$C.E.F', '$C.E.H'] } },
                        __agg2 : { $first : '$B' },
                        __agg3 : { $last : '$K' },
                        __agg4 : { $min : { $add : ['$C.E.F', '$C.E.H'] } },
                        __agg5 : { $max : { $add : ['$C.E.F', '$C.E.H'] } }
                    }
                }",
                @"
                {
                    $project : {
                        Count : '$__agg0',
                        Sum : '$__agg1',
                        First : '$__agg2',
                        Last : '$__agg3',
                        Min : '$__agg4',
                        Max : '$__agg5',
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

            AssertStages(
                result.Stages,
                "{ $group : { _id : 1, __agg0 : { $sum : '$U' } } }",
                "{ $project : { Sum : '$__agg0', _id : 0 } }");

            result.Value.Sum.Should().Be(-0.00000000714529169165701m);
        }

        private void AssertStages(List<BsonDocument> actual, params string[] expected)
        {
            actual.Should().Equal(expected.Select(e => BsonDocument.Parse(e)));
        }

        private ProjectedResult<TResult> Group<TKey, TResult>(Expression<Func<Root, TKey>> idProjector, Expression<Func<IGrouping<TKey, Root>, TResult>> groupProjector)
        {
            var queryable = __collection.AsQueryable()
                .GroupBy(idProjector)
                .Select(groupProjector);

            var domain = BsonSerializer.DefaultSerializationDomain;
            var context = TranslationContext.Create(translationOptions: null, domain);
            var pipeline = ExpressionToPipelineTranslator.Translate(context, queryable.Expression);
            var optimizedAstPipeline = AstPipelineOptimizer.Optimize(pipeline.Ast);
            pipeline = new TranslatedPipeline(optimizedAstPipeline, pipeline.OutputSerializer);

            var stages = pipeline.Ast.Stages.Select(s => s.Render()).Cast<BsonDocument>().ToList();
            stages.Insert(1, new BsonDocument("$sort", new BsonDocument("_id", 1))); // force a standard order for testing purposes
            var pipelineDefinition = new BsonDocumentStagePipelineDefinition<Root, TResult>(stages, outputSerializer: (IBsonSerializer<TResult>)pipeline.OutputSerializer);
            var results = __collection.Aggregate(pipelineDefinition).ToList();

            stages.RemoveAt(1); // remove $sort added above for predictable testing
            return new ProjectedResult<TResult>
            {
                Stages = stages,
                Value = results[0]
            };
        }

        private class ProjectedResult<T>
        {
            public List<BsonDocument> Stages { get; set; }
            public T Value { get; set; }
        }
    }
}
