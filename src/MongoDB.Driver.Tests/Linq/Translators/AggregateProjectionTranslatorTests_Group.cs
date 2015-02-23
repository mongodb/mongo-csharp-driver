/* Copyright 2010-2014 MongoDB Inc.
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
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Linq.Translators;
using MongoDB.Driver.Linq.Utils;
using NUnit.Framework;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson;
using MongoDB.Driver.Tests;

namespace MongoDB.Driver.Core.Linq
{
    public class AggregateProjectionTranslatorTests_Group
    {
        private IMongoCollection<Root> _collection;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            var client = DriverTestConfiguration.Client;
            var db = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            _collection = db.GetCollection<Root>(DriverTestConfiguration.CollectionNamespace.CollectionName);
            db.DropCollectionAsync(_collection.CollectionNamespace.CollectionName).GetAwaiter().GetResult();

            var root = new Root
            {
                A = "Awesome",
                B = "Balloon",
                C = new C
                {
                    D = "Dexter",
                    E = new E
                    {
                        F = 11,
                        H = 22,
                        I = new[] { "it", "icky" }
                    }
                },
                G = new[] { 
                        new C
                        {
                            D = "Don't",
                            E = new E
                            {
                                F = 33,
                                H = 44,
                                I = new [] { "ignanimous"}
                            }
                        },
                        new C
                        {
                            D = "Dolphin",
                            E = new E
                            {
                                F = 55,
                                H = 66,
                                I = new [] { "insecure"}
                            }
                        }
                },
                Id = 10,
                J = new DateTime(2012, 12, 1, 13, 14, 15, 16),
                K = true
            };
            _collection.InsertOneAsync(root).GetAwaiter().GetResult();
        }

        [Test]
        public async Task Should_translate_using_non_anonymous_type_with_default_constructor()
        {
            var result = await Group(x => x.A, g => new RootView { Property = g.Key, Field = g.First().B });

            result.Projection.Should().Be("{ _id: \"$A\", Field: { \"$first\" : \"$B\" } }");

            result.Value.Property.Should().Be("Awesome");
            result.Value.Field.Should().Be("Balloon");
        }

        [Test]
        public async Task Should_translate_using_non_anonymous_type_with_parameterized_constructor()
        {
            var result = await Group(x => x.A, g => new RootView(g.Key) { Field = g.First().B });

            result.Projection.Should().Be("{ _id: \"$A\", Field: { \"$first\" : \"$B\" } }");

            result.Value.Property.Should().Be("Awesome");
            result.Value.Field.Should().Be("Balloon");
        }

        [Test]
        public async Task Should_translate_just_id()
        {
            var result = await Group(x => x.A, g => new { _id = g.Key });

            result.Projection.Should().Be("{ _id: \"$A\" }");

            result.Value._id.Should().Be("Awesome");
        }

        [Test]
        public async Task Should_translate_id_when_not_named_specifically()
        {
            var result = await Group(x => x.A, g => new { Test = g.Key });

            result.Projection.Should().Be("{ _id: \"$A\" }");

            result.Value.Test.Should().Be("Awesome");
        }

        [Test]
        public async Task Should_translate_addToSet()
        {
            var result = await Group(x => x.A, g => new { Result = new HashSet<int>(g.Select(x => x.C.E.F)) });

            result.Projection.Should().Be("{ _id: \"$A\", Result: { \"$addToSet\": \"$C.E.F\" } }");

            result.Value.Result.Should().Equal(11);
        }

        [Test]
        public async Task Should_translate_addToSet_using_Distinct()
        {
            var result = await Group(x => x.A, g => new { Result = g.Select(x => x.C.E.F).Distinct() });

            result.Projection.Should().Be("{ _id: \"$A\", Result: { \"$addToSet\": \"$C.E.F\" } }");

            result.Value.Result.Should().Equal(11);
        }

        [Test]
        public async Task Should_translate_average_with_embedded_projector()
        {
            var result = await Group(x => x.A, g => new { Result = g.Average(x => x.C.E.F) });

            result.Projection.Should().Be("{ _id: \"$A\", Result: { \"$avg\": \"$C.E.F\" } }");

            result.Value.Result.Should().Be(11);
        }

        [Test]
        public async Task Should_translate_average_with_selected_projector()
        {
            var result = await Group(x => x.A, g => new { Result = g.Select(x => x.C.E.F).Average() });

            result.Projection.Should().Be("{ _id: \"$A\", Result: { \"$avg\": \"$C.E.F\" } }");

            result.Value.Result.Should().Be(11);
        }

        [Test]
        public async Task Should_translate_count()
        {
            var result = await Group(x => x.A, g => new { Result = g.Count() });

            result.Projection.Should().Be("{ _id: \"$A\", Result: { \"$sum\": 1 } }");

            result.Value.Result.Should().Be(1);
        }

        [Test]
        public async Task Should_translate_long_count()
        {
            var result = await Group(x => x.A, g => new { Result = g.LongCount() });

            result.Projection.Should().Be("{ _id: \"$A\", Result: { \"$sum\": 1 } }");

            result.Value.Result.Should().Be(1);
        }

        [Test]
        public async Task Should_translate_first()
        {
            var result = await Group(x => x.A, g => new { B = g.Select(x => x.B).First() });

            result.Projection.Should().Be("{ _id: \"$A\", B: { \"$first\": \"$B\" } }");

            result.Value.B.Should().Be("Balloon");
        }

        [Test]
        public async Task Should_translate_first_with_normalization()
        {
            var result = await Group(x => x.A, g => new { g.First().B });

            result.Projection.Should().Be("{ _id: \"$A\", B: { \"$first\": \"$B\" } }");

            result.Value.B.Should().Be("Balloon");
        }

        [Test]
        public async Task Should_translate_last()
        {
            var result = await Group(x => x.A, g => new { B = g.Select(x => x.B).Last() });

            result.Projection.Should().Be("{ _id: \"$A\", B: { \"$last\": \"$B\" } }");

            result.Value.B.Should().Be("Balloon");
        }

        [Test]
        public async Task Should_translate_last_with_normalization()
        {
            var result = await Group(x => x.A, g => new { g.Last().B });

            result.Projection.Should().Be("{ _id: \"$A\", B: { \"$last\": \"$B\" } }");

            result.Value.B.Should().Be("Balloon");
        }

        [Test]
        public async Task Should_translate_max_with_embedded_projector()
        {
            var result = await Group(x => x.A, g => new { Result = g.Max(x => x.C.E.F) });

            result.Projection.Should().Be("{ _id: \"$A\", Result: { \"$max\": \"$C.E.F\" } }");

            result.Value.Result.Should().Be(11);
        }

        [Test]
        public async Task Should_translate_max_with_selected_projector()
        {
            var result = await Group(x => x.A, g => new { Result = g.Select(x => x.C.E.F).Max() });

            result.Projection.Should().Be("{ _id: \"$A\", Result: { \"$max\": \"$C.E.F\" } }");

            result.Value.Result.Should().Be(11);
        }

        [Test]
        public async Task Should_translate_min_with_embedded_projector()
        {
            var result = await Group(x => x.A, g => new { Result = g.Min(x => x.C.E.F) });

            result.Projection.Should().Be("{ _id: \"$A\", Result: { \"$min\": \"$C.E.F\" } }");

            result.Value.Result.Should().Be(11);
        }

        [Test]
        public async Task Should_translate_min_with_selected_projector()
        {
            var result = await Group(x => x.A, g => new { Result = g.Select(x => x.C.E.F).Min() });

            result.Projection.Should().Be("{ _id: \"$A\", Result: { \"$min\": \"$C.E.F\" } }");

            result.Value.Result.Should().Be(11);
        }

        [Test]
        public async Task Should_translate_push_with_just_a_select()
        {
            var result = await Group(x => x.A, g => new { Result = g.Select(x => x.C.E.F) });

            result.Projection.Should().Be("{ _id: \"$A\", Result: { \"$push\": \"$C.E.F\" } }");

            result.Value.Result.Should().Equal(11);
        }

        [Test]
        public async Task Should_translate_push_with_ToArray()
        {
            var result = await Group(x => x.A, g => new { Result = g.Select(x => x.C.E.F).ToArray() });

            result.Projection.Should().Be("{ _id: \"$A\", Result: { \"$push\": \"$C.E.F\" } }");

            result.Value.Result.Should().Equal(11);
        }

        [Test]
        public async Task Should_translate_push_with_ToList()
        {
            var result = await Group(x => x.A, g => new { Result = g.Select(x => x.C.E.F).ToList() });

            result.Projection.Should().Be("{ _id: \"$A\", Result: { \"$push\": \"$C.E.F\" } }");

            result.Value.Result.Should().Equal(11);
        }

        [Test]
        public async Task Should_translate_sum_with_embedded_projector()
        {
            var result = await Group(x => x.A, g => new { Result = g.Sum(x => x.C.E.F) });

            result.Projection.Should().Be("{ _id: \"$A\", Result: { \"$sum\": \"$C.E.F\" } }");

            result.Value.Result.Should().Be(11);
        }

        [Test]
        public async Task Should_translate_sum_with_selected_projector()
        {
            var result = await Group(x => x.A, g => new { Result = g.Select(x => x.C.E.F).Sum() });

            result.Projection.Should().Be("{ _id: \"$A\", Result: { \"$sum\": \"$C.E.F\" } }");

            result.Value.Result.Should().Be(11);
        }

        [Test]
        public async Task Should_translate_complex_selector()
        {
            var result = await Group(x => x.A, g => new
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
            result.Value.Sum.Should().Be(33);
            result.Value.First.Should().Be("Balloon");
            result.Value.Last.Should().Be(true);
            result.Value.Min.Should().Be(33);
            result.Value.Max.Should().Be(33);
        }

        private async Task<ProjectedResult<TResult>> Group<TKey, TResult>(Expression<Func<Root, TKey>> idProjector, Expression<Func<IGrouping<TKey, Root>, TResult>> groupProjector)
        {
            var serializer = BsonSerializer.SerializerRegistry.GetSerializer<Root>();
            var projectionInfo = AggregateProjectionTranslator.TranslateGroup<TKey, Root, TResult>(idProjector, groupProjector, serializer, BsonSerializer.SerializerRegistry);

            var pipelineOperator = new BsonDocument("$group", projectionInfo.Document);
            using (var cursor = await _collection.AggregateAsync<TResult>(new PipelineStagePipeline<Root, TResult>(new PipelineStage<Root, TResult>[] { pipelineOperator }, projectionInfo.Serializer)))
            {
                var list = await cursor.ToListAsync();
                return new ProjectedResult<TResult>
                {
                    Projection = projectionInfo.Document,
                    Value = (TResult)list[0]
                };
            }
        }

        private class ProjectedResult<T>
        {
            public BsonDocument Projection;
            public T Value;
        }

        private class RootView
        {
            public RootView()
            {
            }

            public RootView(string property)
            {
                Property = property;
            }

            public string Property { get; set; }

            public string Field = null;
        }

        private class Root
        {
            public int Id { get; set; }

            public string A { get; set; }

            public string B { get; set; }

            public C C { get; set; }

            public IEnumerable<C> G { get; set; }

            public DateTime J { get; set; }

            public bool K { get; set; }
        }

        public class C
        {
            public string D { get; set; }

            public E E { get; set; }
        }

        public class E
        {
            public int F { get; set; }

            public int H { get; set; }

            public IEnumerable<string> I { get; set; }
        }
    }
}
