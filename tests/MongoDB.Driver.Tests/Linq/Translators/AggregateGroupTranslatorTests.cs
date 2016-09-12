/* Copyright 2010-2015 MongoDB Inc.
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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Linq.Translators;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Translators
{
    public class AggregateGroupTranslatorTests : IntegrationTestBase
    {
        [Fact]
        public void Should_translate_using_non_anonymous_type_with_default_constructor()
        {
            var result = Group(x => x.A, g => new RootView { Property = g.Key, Field = g.First().B });

            result.Projection.Should().Be("{ _id: \"$A\", Field: { \"$first\" : \"$B\" } }");

            result.Value.Property.Should().Be("Amazing");
            result.Value.Field.Should().Be("Baby");
        }

        [Fact]
        public void Should_translate_using_non_anonymous_type_with_parameterized_constructor()
        {
            var result = Group(x => x.A, g => new RootView(g.Key) { Field = g.First().B });

            result.Projection.Should().Be("{ _id: \"$A\", Field: { \"$first\" : \"$B\" } }");

            result.Value.Property.Should().Be("Amazing");
            result.Value.Field.Should().Be("Baby");
        }

        [Fact]
        public void Should_translate_just_id()
        {
            var result = Group(x => x.A, g => new { _id = g.Key });

            result.Projection.Should().Be("{ _id: \"$A\" }");

            result.Value._id.Should().Be("Amazing");
        }

        [Fact]
        public void Should_translate_id_when_not_named_specifically()
        {
            var result = Group(x => x.A, g => new { Test = g.Key });

            result.Projection.Should().Be("{ _id: \"$A\" }");

            result.Value.Test.Should().Be("Amazing");
        }

        [Fact]
        public void Should_translate_addToSet()
        {
            var result = Group(x => x.A, g => new { Result = new HashSet<int>(g.Select(x => x.C.E.F)) });

            result.Projection.Should().Be("{ _id: \"$A\", Result: { \"$addToSet\": \"$C.E.F\" } }");

            result.Value.Result.Should().Equal(111);
        }

        [Fact]
        public void Should_translate_addToSet_using_Distinct()
        {
            var result = Group(x => x.A, g => new { Result = g.Select(x => x.C.E.F).Distinct() });

            result.Projection.Should().Be("{ _id: \"$A\", Result: { \"$addToSet\": \"$C.E.F\" } }");

            result.Value.Result.Should().Equal(111);
        }

        [Fact]
        public void Should_translate_average_with_embedded_projector()
        {
            var result = Group(x => x.A, g => new { Result = g.Average(x => x.C.E.F) });

            result.Projection.Should().Be("{ _id: \"$A\", Result: { \"$avg\": \"$C.E.F\" } }");

            result.Value.Result.Should().Be(111);
        }

        [Fact]
        public void Should_translate_average_with_selected_projector()
        {
            var result = Group(x => x.A, g => new { Result = g.Select(x => x.C.E.F).Average() });

            result.Projection.Should().Be("{ _id: \"$A\", Result: { \"$avg\": \"$C.E.F\" } }");

            result.Value.Result.Should().Be(111);
        }

        [Fact]
        public void Should_translate_count()
        {
            var result = Group(x => x.A, g => new { Result = g.Count() });

            result.Projection.Should().Be("{ _id: \"$A\", Result: { \"$sum\": 1 } }");

            result.Value.Result.Should().Be(1);
        }

        [Fact]
        public void Should_translate_long_count()
        {
            var result = Group(x => x.A, g => new { Result = g.LongCount() });

            result.Projection.Should().Be("{ _id: \"$A\", Result: { \"$sum\": 1 } }");

            result.Value.Result.Should().Be(1);
        }

        [Fact]
        public void Should_translate_first()
        {
            var result = Group(x => x.A, g => new { B = g.Select(x => x.B).First() });

            result.Projection.Should().Be("{ _id: \"$A\", B: { \"$first\": \"$B\" } }");

            result.Value.B.Should().Be("Baby");
        }

        [Fact]
        public void Should_translate_first_with_normalization()
        {
            var result = Group(x => x.A, g => new { g.First().B });

            result.Projection.Should().Be("{ _id: \"$A\", B: { \"$first\": \"$B\" } }");

            result.Value.B.Should().Be("Baby");
        }

        [Fact]
        public void Should_translate_last()
        {
            var result = Group(x => x.A, g => new { B = g.Select(x => x.B).Last() });

            result.Projection.Should().Be("{ _id: \"$A\", B: { \"$last\": \"$B\" } }");

            result.Value.B.Should().Be("Baby");
        }

        [Fact]
        public void Should_translate_last_with_normalization()
        {
            var result = Group(x => x.A, g => new { g.Last().B });

            result.Projection.Should().Be("{ _id: \"$A\", B: { \"$last\": \"$B\" } }");

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

            result.Projection.Should().Be("{ _id: \"$A\", Result: { \"$max\": \"$C.E.F\" } }");

            result.Value.Result.Should().Be(111);
        }

        [Fact]
        public void Should_translate_max_with_selected_projector()
        {
            var result = Group(x => x.A, g => new { Result = g.Select(x => x.C.E.F).Max() });

            result.Projection.Should().Be("{ _id: \"$A\", Result: { \"$max\": \"$C.E.F\" } }");

            result.Value.Result.Should().Be(111);
        }

        [Fact]
        public void Should_translate_min_with_embedded_projector()
        {
            var result = Group(x => x.A, g => new { Result = g.Min(x => x.C.E.F) });

            result.Projection.Should().Be("{ _id: \"$A\", Result: { \"$min\": \"$C.E.F\" } }");

            result.Value.Result.Should().Be(111);
        }

        [Fact]
        public void Should_translate_min_with_selected_projector()
        {
            var result = Group(x => x.A, g => new { Result = g.Select(x => x.C.E.F).Min() });

            result.Projection.Should().Be("{ _id: \"$A\", Result: { \"$min\": \"$C.E.F\" } }");

            result.Value.Result.Should().Be(111);
        }

        [Fact]
        public void Should_translate_push_with_just_a_select()
        {
            var result = Group(x => x.A, g => new { Result = g.Select(x => x.C.E.F) });

            result.Projection.Should().Be("{ _id: \"$A\", Result: { \"$push\": \"$C.E.F\" } }");

            result.Value.Result.Should().Equal(111);
        }

        [Fact]
        public void Should_translate_push_with_ToArray()
        {
            var result = Group(x => x.A, g => new { Result = g.Select(x => x.C.E.F).ToArray() });

            result.Projection.Should().Be("{ _id: \"$A\", Result: { \"$push\": \"$C.E.F\" } }");

            result.Value.Result.Should().Equal(111);
        }

        [Fact]
        public void Should_translate_push_with_new_list()
        {
            var result = Group(x => x.A, g => new { Result = new List<int>(g.Select(x => x.C.E.F)) });

            result.Projection.Should().Be("{ _id: \"$A\", Result: { \"$push\": \"$C.E.F\" } }");

            result.Value.Result.Should().Equal(111);
        }

        [Fact]
        public void Should_translate_push_with_ToList()
        {
            var result = Group(x => x.A, g => new { Result = g.Select(x => x.C.E.F).ToList() });

            result.Projection.Should().Be("{ _id: \"$A\", Result: { \"$push\": \"$C.E.F\" } }");

            result.Value.Result.Should().Equal(111);
        }

        [SkippableFact]
        public void Should_translate_stdDevPop_with_embedded_projector()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.1.7");

            var result = Group(x => 1, g => new { Result = g.StandardDeviationPopulation(x => x.C.E.F) });

            result.Projection.Should().Be("{ _id: 1, Result: { \"$stdDevPop\": \"$C.E.F\" } }");

            result.Value.Result.Should().Be(50);
        }

        [SkippableFact]
        public void Should_translate_stdDevPop_with_selected_projector()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.1.7");

            var result = Group(x => 1, g => new { Result = g.Select(x => x.C.E.F).StandardDeviationPopulation() });

            result.Projection.Should().Be("{ _id: 1, Result: { \"$stdDevPop\": \"$C.E.F\" } }");

            result.Value.Result.Should().Be(50);
        }

        [SkippableFact]
        public void Should_translate_stdDevSamp_with_embedded_projector()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.1.7");

            var result = Group(x => 1, g => new { Result = g.StandardDeviationSample(x => x.C.E.F) });

            result.Projection.Should().Be("{ _id: 1, Result: { \"$stdDevSamp\": \"$C.E.F\" } }");

            result.Value.Result.Should().BeApproximately(70.7106781156545, .0001);
        }

        [SkippableFact]
        public void Should_translate_stdDevSamp_with_selected_projector()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.1.7");

            var result = Group(x => 1, g => new { Result = g.Select(x => x.C.E.F).StandardDeviationSample() });

            result.Projection.Should().Be("{ _id: 1, Result: { \"$stdDevSamp\": \"$C.E.F\" } }");

            result.Value.Result.Should().BeApproximately(70.7106781156545, .0001);
        }

        [Fact]
        public void Should_translate_sum_with_embedded_projector()
        {
            var result = Group(x => x.A, g => new { Result = g.Sum(x => x.C.E.F) });

            result.Projection.Should().Be("{ _id: \"$A\", Result: { \"$sum\": \"$C.E.F\" } }");

            result.Value.Result.Should().Be(111);
        }

        [Fact]
        public void Should_translate_sum_with_selected_projector()
        {
            var result = Group(x => x.A, g => new { Result = g.Select(x => x.C.E.F).Sum() });

            result.Projection.Should().Be("{ _id: \"$A\", Result: { \"$sum\": \"$C.E.F\" } }");

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

            result.Projection.Should().Be("{ _id : \"$A\", Count : { \"$sum\" : 1 }, Sum : { \"$sum\" : { \"$add\": [\"$C.E.F\", \"$C.E.H\"] } }, First : { \"$first\" : \"$B\" }, Last : { \"$last\" : \"$K\" }, Min : { \"$min\" : { \"$add\" : [\"$C.E.F\", \"$C.E.H\"] } }, Max : { \"$max\" : { \"$add\" : [\"$C.E.F\", \"$C.E.H\"] } } }");

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
            var serializer = BsonSerializer.SerializerRegistry.GetSerializer<Root>();
            var projectionInfo = AggregateGroupTranslator.Translate<TKey, Root, TResult>(idProjector, groupProjector, serializer, BsonSerializer.SerializerRegistry, translationOptions);

            var group = new BsonDocument("$group", projectionInfo.Document);
            var sort = new BsonDocument("$sort", new BsonDocument("_id", 1));
            var list = __collection.Aggregate<TResult>(new BsonDocumentStagePipelineDefinition<Root, TResult>(new[] { group, sort }, projectionInfo.ProjectionSerializer)).ToList();

            return new ProjectedResult<TResult>
            {
                Projection = projectionInfo.Document,
                Value = (TResult)list[0]
            };
        }

        private class ProjectedResult<T>
        {
            public BsonDocument Projection;
            public T Value;
        }
    }
}
