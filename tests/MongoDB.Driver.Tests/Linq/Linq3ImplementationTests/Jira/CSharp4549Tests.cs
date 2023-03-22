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
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp4549Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Projecting_a_tuple_using_constructor_with_1_item_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new Tuple<int>(x.A));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$A'], _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(Tuple.Create(1));
        }

        [Fact]
        public void Projecting_a_tuple_using_constructor_with_2_items_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new Tuple<int, int>(x.A, x.B));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$A', '$B'], _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(Tuple.Create(1, 2));
        }

        [Fact]
        public void Projecting_a_tuple_using_constructor_with_3_items_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new Tuple<int, int, int>(x.A, x.B, x.C));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$A', '$B', '$C'], _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(Tuple.Create(1, 2, 3));
        }

        [Fact]
        public void Projecting_a_tuple_using_constructor_with_4_items_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new Tuple<int, int, int, int>(x.A, x.B, x.C, x.D));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$A', '$B', '$C', '$D'], _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(Tuple.Create(1, 2, 3, 4));
        }

        [Fact]
        public void Projecting_a_tuple_using_constructor_with_5_items_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new Tuple<int, int, int, int, int>(x.A, x.B, x.C, x.D, x.E));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E'], _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(Tuple.Create(1, 2, 3, 4, 5));
        }

        [Fact]
        public void Projecting_a_tuple_using_constructor_with_6_items_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new Tuple<int, int, int, int, int, int>(x.A, x.B, x.C, x.D, x.E, x.F));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F'], _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(Tuple.Create(1, 2, 3, 4, 5, 6));
        }

        [Fact]
        public void Projecting_a_tuple_using_constructor_with_7_items_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new Tuple<int, int, int, int, int, int, int>(x.A, x.B, x.C, x.D, x.E, x.F, x.G));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G'], _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(Tuple.Create(1, 2, 3, 4, 5, 6, 7));
        }

        [Fact]
        public void Projecting_a_tuple_using_constructor_with_8_items_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new Tuple<int, int, int, int, int, int, int, Tuple<int>>(x.A, x.B, x.C, x.D, x.E, x.F, x.G, new Tuple<int>(x.H)));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H']], _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(Tuple.Create(1, 2, 3, 4, 5, 6, 7, 8));
        }

        [Fact]
        public void Projecting_a_tuple_using_constructor_with_9_items_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(x.A, x.B, x.C, x.D, x.E, x.F, x.G, new Tuple<int, int>(x.H, x.I)));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I']], _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(1, 2, 3, 4, 5, 6, 7, new Tuple<int, int>(8, 9)));
        }

        [Fact]
        public void Projecting_a_tuple_using_constructor_with_16_items_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new Tuple<int, int, int, int, int, int, int, Tuple<int, int, int, int, int, int, int, Tuple<int, int>>>(
                    x.A, x.B, x.C, x.D, x.E, x.F, x.G,
                    new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(
                        x.H, x.I, x.J, x.K, x.L, x.M, x.N,
                        new Tuple<int, int>(x.O, x.P))));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I', '$J', '$K', '$L', '$M', '$N', ['$O', '$P']]], _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(new Tuple<int, int, int, int, int, int, int, Tuple<int, int, int, int, int, int, int, Tuple<int, int>>>(
                1, 2, 3, 4, 5, 6, 7,
                new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(
                    8, 9, 10, 11, 12, 13, 14,
                    new Tuple<int, int>(15, 16))));
        }

        [Fact]
        public void Projecting_a_tuple_using_create_with_1_item_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => Tuple.Create(x.A));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$A'], _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(Tuple.Create(1));
        }

        [Fact]
        public void Projecting_a_tuple_using_create_with_2_items_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => Tuple.Create(x.A, x.B));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$A', '$B'], _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(Tuple.Create(1, 2));
        }

        [Fact]
        public void Projecting_a_tuple_using_create_with_3_items_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => Tuple.Create(x.A, x.B, x.C));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$A', '$B', '$C'], _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(Tuple.Create(1, 2, 3));
        }

        [Fact]
        public void Projecting_a_tuple_using_create_with_4_items_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => Tuple.Create(x.A, x.B, x.C, x.D));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$A', '$B', '$C', '$D'], _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(Tuple.Create(1, 2, 3, 4));
        }

        [Fact]
        public void Projecting_a_tuple_using_create_with_5_items_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => Tuple.Create(x.A, x.B, x.C, x.D, x.E));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E'], _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(Tuple.Create(1, 2, 3, 4, 5));
        }

        [Fact]
        public void Projecting_a_tuple_using_create_with_6_items_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => Tuple.Create(x.A, x.B, x.C, x.D, x.E, x.F));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F'], _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(Tuple.Create(1, 2, 3, 4, 5, 6));
        }

        [Fact]
        public void Projecting_a_tuple_using_create_with_7_items_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => Tuple.Create(x.A, x.B, x.C, x.D, x.E, x.F, x.G));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G'], _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(Tuple.Create(1, 2, 3, 4, 5, 6, 7));
        }

        [Fact]
        public void Projecting_a_tuple_using_create_with_8_items_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => Tuple.Create(x.A, x.B, x.C, x.D, x.E, x.F, x.G, x.H));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H']], _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(Tuple.Create(1, 2, 3, 4, 5, 6, 7, 8));
        }

        [Fact]
        public void Projecting_a_value_tuple_using_constructor_with_1_item_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new ValueTuple<int>(x.A));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$A'], _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(ValueTuple.Create(1));
        }

        [Fact]
        public void Projecting_a_value_tuple_using_constructor_with_2_items_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new ValueTuple<int, int>(x.A, x.B));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$A', '$B'], _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal((1, 2));
        }

        [Fact]
        public void Projecting_a_value_tuple_using_constructor_with_3_items_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new ValueTuple<int, int, int>(x.A, x.B, x.C));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$A', '$B', '$C'], _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal((1, 2, 3));
        }

        [Fact]
        public void Projecting_a_value_tuple_using_constructor_with_4_items_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new ValueTuple<int, int, int, int>(x.A, x.B, x.C, x.D));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$A', '$B', '$C', '$D'], _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal((1, 2, 3, 4));
        }

        [Fact]
        public void Projecting_a_value_tuple_using_constructor_with_5_items_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new ValueTuple<int, int, int, int, int>(x.A, x.B, x.C, x.D, x.E));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E'], _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal((1, 2, 3, 4, 5));
        }

        [Fact]
        public void Projecting_a_value_tuple_using_constructor_with_6_items_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new ValueTuple<int, int, int, int, int, int>(x.A, x.B, x.C, x.D, x.E, x.F));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F'], _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal((1, 2, 3, 4, 5, 6));
        }

        [Fact]
        public void Projecting_a_value_tuple_using_constructor_with_7_items_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new ValueTuple<int, int, int, int, int, int, int>(x.A, x.B, x.C, x.D, x.E, x.F, x.G));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G'], _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal((1, 2, 3, 4, 5, 6, 7));
        }

        [Fact]
        public void Projecting_a_value_tuple_using_constructor_with_8_items_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int>>(x.A, x.B, x.C, x.D, x.E, x.F, x.G, new ValueTuple<int>(x.H)));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H']], _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal((1, 2, 3, 4, 5, 6, 7, 8));
        }

        [Fact]
        public void Projecting_a_value_tuple_using_constructor_with_9_items_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>(x.A, x.B, x.C, x.D, x.E, x.F, x.G, new ValueTuple<int, int>(x.H, x.I)));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I']], _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal((1, 2, 3, 4, 5, 6, 7, 8, 9));
        }

        [Fact]
        public void Projecting_a_value_tuple_using_constructor_with_16_items_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>>(
                    x.A, x.B, x.C, x.D, x.E, x.F, x.G,
                    new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>(
                        x.H, x.I, x.J, x.K, x.L, x.M, x.N,
                        new ValueTuple<int, int>(x.O, x.P))));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I', '$J', '$K', '$L', '$M', '$N', ['$O', '$P']]], _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal((1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16));
        }

        [Fact]
        public void Projecting_a_value_tuple_using_create_with_1_item_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => ValueTuple.Create(x.A));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$A'], _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(ValueTuple.Create(1));
        }

        [Fact]
        public void Projecting_a_value_tuple_using_create_with_2_items_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => ValueTuple.Create(x.A, x.B));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$A', '$B'], _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal((1, 2));
        }

        [Fact]
        public void Projecting_a_value_tuple_using_create_with_3_items_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => ValueTuple.Create(x.A, x.B, x.C));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$A', '$B', '$C'], _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal((1, 2, 3));
        }

        [Fact]
        public void Projecting_a_value_tuple_using_create_with_4_items_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => ValueTuple.Create(x.A, x.B, x.C, x.D));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$A', '$B', '$C', '$D'], _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal((1, 2, 3, 4));
        }

        [Fact]
        public void Projecting_a_value_tuple_using_create_with_5_items_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => ValueTuple.Create(x.A, x.B, x.C, x.D, x.E));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E'], _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal((1, 2, 3, 4, 5));
        }

        [Fact]
        public void Projecting_a_value_tuple_using_create_with_6_items_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => ValueTuple.Create(x.A, x.B, x.C, x.D, x.E, x.F));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F'], _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal((1, 2, 3, 4, 5, 6));
        }

        [Fact]
        public void Projecting_a_value_tuple_using_create_with_7_items_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => ValueTuple.Create(x.A, x.B, x.C, x.D, x.E, x.F, x.G));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G'], _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal((1, 2, 3, 4, 5, 6, 7));
        }

        [Fact]
        public void Projecting_a_value_tuple_using_create_with_8_items_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => ValueTuple.Create(x.A, x.B, x.C, x.D, x.E, x.F, x.G, x.H));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H']], _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal((1, 2, 3, 4, 5, 6, 7, 8));
        }

        [Fact]
        public void Select_Tuple1_Item1_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => Tuple.Create(x.A))
                .Select(x => x.Item1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A'], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : ['$_v', 0] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1);
        }

        [Fact]
        public void Select_Tuple2_Item1_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => Tuple.Create(x.A, x.B))
                .Select(x => x.Item1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B'], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : ['$_v', 0] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1);
        }

        [Fact]
        public void Select_Tuple2_Item2_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => Tuple.Create(x.A, x.B))
                .Select(x => x.Item2);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B'], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : ['$_v', 1] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(2);
        }

        [Fact]
        public void Select_Tuple7_Item1_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => Tuple.Create(x.A, x.B, x.C, x.D, x.E, x.F, x.G))
                .Select(x => x.Item1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G'], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : ['$_v', 0] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1);
        }

        [Fact]
        public void Select_Tuple7_Item7_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => Tuple.Create(x.A, x.B, x.C, x.D, x.E, x.F, x.G))
                .Select(x => x.Item7);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G'], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : ['$_v', 6] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(7);
        }

        [Fact]
        public void Select_Tuple8_Item1_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => Tuple.Create(x.A, x.B, x.C, x.D, x.E, x.F, x.G, x.H))
                .Select(x => x.Item1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H']], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : ['$_v', 0] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1);
        }

        [Fact]
        public void Select_Tuple8_Item7_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => Tuple.Create(x.A, x.B, x.C, x.D, x.E, x.F, x.G, x.H))
                .Select(x => x.Item7);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H']], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : ['$_v', 6] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(7);
        }

        [Fact]
        public void Select_Tuple8_Item8_should_work()
        {
            var collection = CreateCollection();
            var queryable = collection.AsQueryable()
                .Select(x => Tuple.Create(x.A, x.B, x.C, x.D, x.E, x.F, x.G, x.H))
                .Select(x => x.Rest.Item1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H']], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : [{ $arrayElemAt : ['$_v', 7] }, 0] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(8);
        }

        [Fact]
        public void Select_Tuple8_Rest_should_work()
        {
            var collection = CreateCollection();
            var queryable = collection.AsQueryable()
                .Select(x => Tuple.Create(x.A, x.B, x.C, x.D, x.E, x.F, x.G, x.H))
                .Select(x => x.Rest);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H']], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : ['$_v', 7] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(Tuple.Create(8));
        }

        [Fact]
        public void Select_Tuple9_Item1_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(x.A, x.B, x.C, x.D, x.E, x.F, x.G, new Tuple<int, int>(x.H, x.I)))
                .Select(x => x.Item1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I']], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : ['$_v', 0] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1);
        }

        [Fact]
        public void Select_Tuple9_Item7_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(x.A, x.B, x.C, x.D, x.E, x.F, x.G, new Tuple<int, int>(x.H, x.I)))
                .Select(x => x.Item7);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I']], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : ['$_v', 6] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(7);
        }

        [Fact]
        public void Select_Tuple9_Item8_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(x.A, x.B, x.C, x.D, x.E, x.F, x.G, new Tuple<int, int>(x.H, x.I)))
                .Select(x => x.Rest.Item1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I']], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : [{ $arrayElemAt : ['$_v', 7] }, 0] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(8);
        }

        [Fact]
        public void Select_Tuple9_Item9_should_work()
        {
            var collection = CreateCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(x.A, x.B, x.C, x.D, x.E, x.F, x.G, new Tuple<int, int>(x.H, x.I)))
                .Select(x => x.Rest.Item2);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I']], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : [{ $arrayElemAt : ['$_v', 7] }, 1] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(9);
        }

        [Fact]
        public void Select_Tuple9_Rest_should_work()
        {
            var collection = CreateCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(x.A, x.B, x.C, x.D, x.E, x.F, x.G, new Tuple<int, int>(x.H, x.I)))
                .Select(x => x.Rest);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I']], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : ['$_v', 7] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(Tuple.Create(8, 9));
        }

        [Fact]
        public void Select_Tuple16_Item1_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new Tuple<int, int, int, int, int, int, int, Tuple<int, int, int, int, int, int, int, Tuple<int, int>>>(
                    x.A, x.B, x.C, x.D, x.E, x.F, x.G,
                    new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(
                        x.H, x.I, x.J, x.K, x.L, x.M, x.N,
                        new Tuple<int, int>(x.O, x.P))))
                .Select(x => x.Item1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I', '$J', '$K', '$L', '$M', '$N', ['$O', '$P']]], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : ['$_v', 0] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1);
        }

        [Fact]
        public void Select_Tuple16_Item7_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new Tuple<int, int, int, int, int, int, int, Tuple<int, int, int, int, int, int, int, Tuple<int, int>>>(
                    x.A, x.B, x.C, x.D, x.E, x.F, x.G,
                    new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(
                        x.H, x.I, x.J, x.K, x.L, x.M, x.N,
                        new Tuple<int, int>(x.O, x.P))))
                .Select(x => x.Item7);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I', '$J', '$K', '$L', '$M', '$N', ['$O', '$P']]], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : ['$_v', 6] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(7);
        }

        [Fact]
        public void Select_Tuple16_Item8_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new Tuple<int, int, int, int, int, int, int, Tuple<int, int, int, int, int, int, int, Tuple<int, int>>>(
                    x.A, x.B, x.C, x.D, x.E, x.F, x.G,
                    new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(
                        x.H, x.I, x.J, x.K, x.L, x.M, x.N,
                        new Tuple<int, int>(x.O, x.P))))
                .Select(x => x.Rest.Item1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I', '$J', '$K', '$L', '$M', '$N', ['$O', '$P']]], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : [{ $arrayElemAt : ['$_v', 7] }, 0] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(8);
        }

        [Fact]
        public void Select_Tuple16_Item14_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new Tuple<int, int, int, int, int, int, int, Tuple<int, int, int, int, int, int, int, Tuple<int, int>>>(
                    x.A, x.B, x.C, x.D, x.E, x.F, x.G,
                    new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(
                        x.H, x.I, x.J, x.K, x.L, x.M, x.N,
                        new Tuple<int, int>(x.O, x.P))))
                .Select(x => x.Rest.Item7);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I', '$J', '$K', '$L', '$M', '$N', ['$O', '$P']]], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : [{ $arrayElemAt : ['$_v', 7] }, 6] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(14);
        }

        [Fact]
        public void Select_Tuple16_Item15_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new Tuple<int, int, int, int, int, int, int, Tuple<int, int, int, int, int, int, int, Tuple<int, int>>>(
                    x.A, x.B, x.C, x.D, x.E, x.F, x.G,
                    new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(
                        x.H, x.I, x.J, x.K, x.L, x.M, x.N,
                        new Tuple<int, int>(x.O, x.P))))
                .Select(x => x.Rest.Rest.Item1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I', '$J', '$K', '$L', '$M', '$N', ['$O', '$P']]], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : [{ $arrayElemAt : [{ $arrayElemAt : ['$_v', 7] }, 7] }, 0] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(15);
        }

        [Fact]
        public void Select_Tuple16_Item16_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new Tuple<int, int, int, int, int, int, int, Tuple<int, int, int, int, int, int, int, Tuple<int, int>>>(
                    x.A, x.B, x.C, x.D, x.E, x.F, x.G,
                    new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(
                        x.H, x.I, x.J, x.K, x.L, x.M, x.N,
                        new Tuple<int, int>(x.O, x.P))))
                .Select(x => x.Rest.Rest.Item2);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I', '$J', '$K', '$L', '$M', '$N', ['$O', '$P']]], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : [{ $arrayElemAt : [{ $arrayElemAt : ['$_v', 7] }, 7] }, 1] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(16);
        }

        [Fact]
        public void Select_Tuple16_Rest_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new Tuple<int, int, int, int, int, int, int, Tuple<int, int, int, int, int, int, int, Tuple<int, int>>>(
                    x.A, x.B, x.C, x.D, x.E, x.F, x.G,
                    new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(
                        x.H, x.I, x.J, x.K, x.L, x.M, x.N,
                        new Tuple<int, int>(x.O, x.P))))
                .Select(x => x.Rest);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I', '$J', '$K', '$L', '$M', '$N', ['$O', '$P']]], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : ['$_v', 7] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(8, 9, 10, 11, 12, 13, 14, new Tuple<int, int>(15, 16)));
        }

        [Fact]
        public void Select_Tuple16_Rest_Rest_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new Tuple<int, int, int, int, int, int, int, Tuple<int, int, int, int, int, int, int, Tuple<int, int>>>(
                    x.A, x.B, x.C, x.D, x.E, x.F, x.G,
                    new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(
                        x.H, x.I, x.J, x.K, x.L, x.M, x.N,
                        new Tuple<int, int>(x.O, x.P))))
                .Select(x => x.Rest.Rest);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I', '$J', '$K', '$L', '$M', '$N', ['$O', '$P']]], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : [{ $arrayElemAt : ['$_v', 7] }, 7] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(Tuple.Create(15, 16));
        }

        [Fact]
        public void Select_ValueTuple1_Item1_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => ValueTuple.Create(x.A))
                .Select(x => x.Item1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A'], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : ['$_v', 0] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1);
        }

        [Fact]
        public void Select_ValueTuple2_Item1_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => ValueTuple.Create(x.A, x.B))
                .Select(x => x.Item1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B'], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : ['$_v', 0] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1);
        }

        [Fact]
        public void Select_ValueTuple2_Item2_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => ValueTuple.Create(x.A, x.B))
                .Select(x => x.Item2);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B'], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : ['$_v', 1] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(2);
        }

        [Fact]
        public void Select_ValueTuple7_Item1_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => ValueTuple.Create(x.A, x.B, x.C, x.D, x.E, x.F, x.G))
                .Select(x => x.Item1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G'], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : ['$_v', 0] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1);
        }

        [Fact]
        public void Select_ValueTuple7_Item7_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => ValueTuple.Create(x.A, x.B, x.C, x.D, x.E, x.F, x.G))
                .Select(x => x.Item7);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G'], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : ['$_v', 6] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(7);
        }

        [Fact]
        public void Select_ValueTuple8_Item1_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => ValueTuple.Create(x.A, x.B, x.C, x.D, x.E, x.F, x.G, x.H))
                .Select(x => x.Item1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H']], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : ['$_v', 0] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1);
        }

        [Fact]
        public void Select_ValueTuple8_Item7_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => ValueTuple.Create(x.A, x.B, x.C, x.D, x.E, x.F, x.G, x.H))
                .Select(x => x.Item7);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H']], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : ['$_v', 6] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(7);
        }

        [Fact]
        public void Select_ValueTuple8_Item8_should_work()
        {
            var collection = CreateCollection();
            var queryable = collection.AsQueryable()
                .Select(x => ValueTuple.Create(x.A, x.B, x.C, x.D, x.E, x.F, x.G, x.H))
                .Select(x => x.Item8);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H']], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : [{ $arrayElemAt : ['$_v', 7] }, 0] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(8);
        }

        [Fact]
        public void Select_ValueTuple8_Rest_should_work()
        {
            var collection = CreateCollection();
            var queryable = collection.AsQueryable()
                .Select(x => ValueTuple.Create(x.A, x.B, x.C, x.D, x.E, x.F, x.G, x.H))
                .Select(x => x.Rest);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H']], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : ['$_v', 7] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(ValueTuple.Create(8));
        }

        [Fact]
        public void Select_ValueTuple9_Item1_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>(x.A, x.B, x.C, x.D, x.E, x.F, x.G, new ValueTuple<int, int>(x.H, x.I)))
                .Select(x => x.Item1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I']], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : ['$_v', 0] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1);
        }

        [Fact]
        public void Select_ValueTuple9_Item7_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>(x.A, x.B, x.C, x.D, x.E, x.F, x.G, new ValueTuple<int, int>(x.H, x.I)))
                .Select(x => x.Item7);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I']], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : ['$_v', 6] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(7);
        }

        [Fact]
        public void Select_ValueTuple9_Item8_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>(x.A, x.B, x.C, x.D, x.E, x.F, x.G, new ValueTuple<int, int>(x.H, x.I)))
                .Select(x => x.Item8);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I']], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : [{ $arrayElemAt : ['$_v', 7] }, 0] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(8);
        }

        [Fact]
        public void Select_ValueTuple9_Item9_should_work()
        {
            var collection = CreateCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>(x.A, x.B, x.C, x.D, x.E, x.F, x.G, new ValueTuple<int, int>(x.H, x.I)))
                .Select(x => x.Item9);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I']], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : [{ $arrayElemAt : ['$_v', 7] }, 1] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(9);
        }

        [Fact]
        public void Select_ValueTuple9_Rest_should_work()
        {
            var collection = CreateCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>(x.A, x.B, x.C, x.D, x.E, x.F, x.G, new ValueTuple<int, int>(x.H, x.I)))
                .Select(x => x.Rest);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I']], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : ['$_v', 7] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal((8, 9));
        }

        [Fact]
        public void Select_ValueTuple16_Item1_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>>(
                    x.A, x.B, x.C, x.D, x.E, x.F, x.G,
                    new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>(
                        x.H, x.I, x.J, x.K, x.L, x.M, x.N,
                        new ValueTuple<int, int>(x.O, x.P))))
                .Select(x => x.Item1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I', '$J', '$K', '$L', '$M', '$N', ['$O', '$P']]], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : ['$_v', 0] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1);
        }

        [Fact]
        public void Select_ValueTuple16_Item7_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>>(
                    x.A, x.B, x.C, x.D, x.E, x.F, x.G,
                    new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>(
                        x.H, x.I, x.J, x.K, x.L, x.M, x.N,
                        new ValueTuple<int, int>(x.O, x.P))))
                .Select(x => x.Item7);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I', '$J', '$K', '$L', '$M', '$N', ['$O', '$P']]], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : ['$_v', 6] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(7);
        }

        [Fact]
        public void Select_ValueTuple16_Item8_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>>(
                    x.A, x.B, x.C, x.D, x.E, x.F, x.G,
                    new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>(
                        x.H, x.I, x.J, x.K, x.L, x.M, x.N,
                        new ValueTuple<int, int>(x.O, x.P))))
                .Select(x => x.Item8);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I', '$J', '$K', '$L', '$M', '$N', ['$O', '$P']]], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : [{ $arrayElemAt : ['$_v', 7] }, 0] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(8);
        }

        [Fact]
        public void Select_ValueTuple16_Item14_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>>(
                    x.A, x.B, x.C, x.D, x.E, x.F, x.G,
                    new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>(
                        x.H, x.I, x.J, x.K, x.L, x.M, x.N,
                        new ValueTuple<int, int>(x.O, x.P))))
                .Select(x => x.Item14);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I', '$J', '$K', '$L', '$M', '$N', ['$O', '$P']]], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : [{ $arrayElemAt : ['$_v', 7] }, 6] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(14);
        }

        [Fact]
        public void Select_ValueTuple16_Item15_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>>(
                    x.A, x.B, x.C, x.D, x.E, x.F, x.G,
                    new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>(
                        x.H, x.I, x.J, x.K, x.L, x.M, x.N,
                        new ValueTuple<int, int>(x.O, x.P))))
                .Select(x => x.Item15);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I', '$J', '$K', '$L', '$M', '$N', ['$O', '$P']]], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : [{ $arrayElemAt : [{ $arrayElemAt : ['$_v', 7] }, 7] }, 0] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(15);
        }

        [Fact]
        public void Select_ValueTuple16_Item16_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>>(
                    x.A, x.B, x.C, x.D, x.E, x.F, x.G,
                    new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>(
                        x.H, x.I, x.J, x.K, x.L, x.M, x.N,
                        new ValueTuple<int, int>(x.O, x.P))))
                .Select(x => x.Item16);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I', '$J', '$K', '$L', '$M', '$N', ['$O', '$P']]], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : [{ $arrayElemAt : [{ $arrayElemAt : ['$_v', 7] }, 7] }, 1] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(16);
        }

        [Fact]
        public void Select_ValueTuple16_Rest_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>>(
                    x.A, x.B, x.C, x.D, x.E, x.F, x.G,
                    new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>(
                        x.H, x.I, x.J, x.K, x.L, x.M, x.N,
                        new ValueTuple<int, int>(x.O, x.P))))
                .Select(x => x.Rest);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I', '$J', '$K', '$L', '$M', '$N', ['$O', '$P']]], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : ['$_v', 7] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal((8, 9, 10, 11, 12, 13, 14, 15, 16));
        }

        [Fact]
        public void Select_ValueTuple16_Rest_Rest_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>>(
                    x.A, x.B, x.C, x.D, x.E, x.F, x.G,
                    new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>(
                        x.H, x.I, x.J, x.K, x.L, x.M, x.N,
                        new ValueTuple<int, int>(x.O, x.P))))
                .Select(x => x.Rest.Rest);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I', '$J', '$K', '$L', '$M', '$N', ['$O', '$P']]], _id : 0 } }",
                "{ $project : { _v : { $arrayElemAt : [{ $arrayElemAt : ['$_v', 7] }, 7] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal((15, 16));
        }

        [Fact]
        public void Where_Tuple1_Item1_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => Tuple.Create(x.A))
                .Where(x => x.Item1 == 1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A'], _id : 0 } }",
                "{ $match : { '_v.0' : 1 } }");

            var results = queryable.ToList();
            results.Should().Equal(Tuple.Create(1));
        }

        [Fact]
        public void Where_Tuple2_Item1_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => Tuple.Create(x.A, x.B))
                .Where(x => x.Item1 == 1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B'], _id : 0 } }",
                "{ $match : { '_v.0' : 1 } }");

            var results = queryable.ToList();
            results.Should().Equal(Tuple.Create(1, 2));
        }

        [Fact]
        public void Where_Tuple2_Item2_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => Tuple.Create(x.A, x.B))
                .Where(x => x.Item2 == 2);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B'], _id : 0 } }",
                "{ $match : { '_v.1' : 2 } }");

            var results = queryable.ToList();
            results.Should().Equal(Tuple.Create(1, 2));
        }

        [Fact]
        public void Where_Tuple7_Item1_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => Tuple.Create(x.A, x.B, x.C, x.D, x.E, x.F, x.G))
                .Where(x => x.Item1 == 1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G'], _id : 0 } }",
                "{ $match : { '_v.0' : 1 } }");

            var results = queryable.ToList();
            results.Should().Equal(Tuple.Create(1, 2, 3, 4, 5, 6, 7));
        }

        [Fact]
        public void Where_Tuple7_Item7_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => Tuple.Create(x.A, x.B, x.C, x.D, x.E, x.F, x.G))
                .Where(x => x.Item7 == 7);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G'], _id : 0 } }",
                "{ $match : { '_v.6' : 7 } }");

            var results = queryable.ToList();
            results.Should().Equal(Tuple.Create(1, 2, 3, 4, 5, 6, 7));
        }

        [Fact]
        public void Where_Tuple8_Item1_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => Tuple.Create(x.A, x.B, x.C, x.D, x.E, x.F, x.G, x.H))
                .Where(x => x.Item1 == 1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H']], _id : 0 } }",
                "{ $match : { '_v.0' : 1 } }");

            var results = queryable.ToList();
            results.Should().Equal(Tuple.Create(1, 2, 3, 4, 5, 6, 7, 8));
        }

        [Fact]
        public void Where_Tuple8_Item7_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => Tuple.Create(x.A, x.B, x.C, x.D, x.E, x.F, x.G, x.H))
                .Where(x => x.Item7 == 7);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H']], _id : 0 } }",
                "{ $match : { '_v.6' : 7 } }");

            var results = queryable.ToList();
            results.Should().Equal(Tuple.Create(1, 2, 3, 4, 5, 6, 7, 8));
        }

        [Fact]
        public void Where_Tuple8_Item8_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => Tuple.Create(x.A, x.B, x.C, x.D, x.E, x.F, x.G, x.H))
                .Where(x => x.Rest.Item1 == 8);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H']], _id : 0 } }",
                "{ $match : { '_v.7.0' : 8 } }");

            var results = queryable.ToList();
            results.Should().Equal(Tuple.Create(1, 2, 3, 4, 5, 6, 7, 8));
        }

        [Fact]
        public void Where_Tuple9_Item1_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(x.A, x.B, x.C, x.D, x.E, x.F, x.G, new Tuple<int, int>(x.H, x.I)))
                .Where(x => x.Item1 == 1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I']], _id : 0 } }",
                "{ $match : { '_v.0' : 1 } }");

            var results = queryable.ToList();
            results.Should().Equal(new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(1, 2, 3, 4, 5, 6, 7, new Tuple<int, int>(8, 9)));
        }

        [Fact]
        public void Where_Tuple9_Item7_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(x.A, x.B, x.C, x.D, x.E, x.F, x.G, new Tuple<int, int>(x.H, x.I)))
                .Where(x => x.Item7 == 7);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I']], _id : 0 } }",
                "{ $match : { '_v.6' : 7 } }");

            var results = queryable.ToList();
            results.Should().Equal(new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(1, 2, 3, 4, 5, 6, 7, new Tuple<int, int>(8, 9)));
        }

        [Fact]
        public void Where_Tuple9_Item8_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(x.A, x.B, x.C, x.D, x.E, x.F, x.G, new Tuple<int, int>(x.H, x.I)))
                .Where(x => x.Rest.Item1 == 8);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I']], _id : 0 } }",
                "{ $match : { '_v.7.0' : 8 } }");

            var results = queryable.ToList();
            results.Should().Equal(new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(1, 2, 3, 4, 5, 6, 7, new Tuple<int, int>(8, 9)));
        }

        [Fact]
        public void Where_Tuple9_Item9_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(x.A, x.B, x.C, x.D, x.E, x.F, x.G, new Tuple<int, int>(x.H, x.I)))
                .Where(x => x.Rest.Item2 == 9);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I']], _id : 0 } }",
                "{ $match : { '_v.7.1' : 9 } }");

            var results = queryable.ToList();
            results.Should().Equal(new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(1, 2, 3, 4, 5, 6, 7, new Tuple<int, int>(8, 9)));
        }

        [Fact]
        public void Where_Tuple16_Item1_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new Tuple<int, int, int, int, int, int, int, Tuple<int, int, int, int, int, int, int, Tuple<int, int>>>(
                    x.A, x.B, x.C, x.D, x.E, x.F, x.G,
                    new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(
                        x.H, x.I, x.J, x.K, x.L, x.M, x.N,
                        new Tuple<int, int>(x.O, x.P))))
                .Where(x => x.Item1 == 1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I', '$J', '$K', '$L', '$M', '$N', ['$O', '$P']]], _id : 0 } }",
                "{ $match : { '_v.0' : 1 } }");

            var results = queryable.ToList();
            results.Should().Equal(new Tuple<int, int, int, int, int, int, int, Tuple<int, int, int, int, int, int, int, Tuple<int, int>>>(
                1, 2, 3, 4, 5, 6, 7,
                new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(
                    8, 9, 10, 11, 12, 13, 14,
                    new Tuple<int, int>(15, 16))));
        }

        [Fact]
        public void Where_Tuple16_Item7_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new Tuple<int, int, int, int, int, int, int, Tuple<int, int, int, int, int, int, int, Tuple<int, int>>>(
                    x.A, x.B, x.C, x.D, x.E, x.F, x.G,
                    new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(
                        x.H, x.I, x.J, x.K, x.L, x.M, x.N,
                        new Tuple<int, int>(x.O, x.P))))
                .Where(x => x.Item7 == 7);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I', '$J', '$K', '$L', '$M', '$N', ['$O', '$P']]], _id : 0 } }",
                "{ $match : { '_v.6' : 7 } }");

            var results = queryable.ToList();
            results.Should().Equal(new Tuple<int, int, int, int, int, int, int, Tuple<int, int, int, int, int, int, int, Tuple<int, int>>>(
                1, 2, 3, 4, 5, 6, 7,
                new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(
                    8, 9, 10, 11, 12, 13, 14,
                    new Tuple<int, int>(15, 16))));
        }

        [Fact]
        public void Where_Tuple16_Item8_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new Tuple<int, int, int, int, int, int, int, Tuple<int, int, int, int, int, int, int, Tuple<int, int>>>(
                    x.A, x.B, x.C, x.D, x.E, x.F, x.G,
                    new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(
                        x.H, x.I, x.J, x.K, x.L, x.M, x.N,
                        new Tuple<int, int>(x.O, x.P))))
                .Where(x => x.Rest.Item1 == 8);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I', '$J', '$K', '$L', '$M', '$N', ['$O', '$P']]], _id : 0 } }",
                "{ $match : { '_v.7.0' : 8 } }");

            var results = queryable.ToList();
            results.Should().Equal(new Tuple<int, int, int, int, int, int, int, Tuple<int, int, int, int, int, int, int, Tuple<int, int>>>(
                1, 2, 3, 4, 5, 6, 7,
                new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(
                    8, 9, 10, 11, 12, 13, 14,
                    new Tuple<int, int>(15, 16))));
        }

        [Fact]
        public void Where_Tuple16_Item14_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new Tuple<int, int, int, int, int, int, int, Tuple<int, int, int, int, int, int, int, Tuple<int, int>>>(
                    x.A, x.B, x.C, x.D, x.E, x.F, x.G,
                    new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(
                        x.H, x.I, x.J, x.K, x.L, x.M, x.N,
                        new Tuple<int, int>(x.O, x.P))))
                .Where(x => x.Rest.Item7 == 14);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I', '$J', '$K', '$L', '$M', '$N', ['$O', '$P']]], _id : 0 } }",
                "{ $match : { '_v.7.6' : 14 } }");

            var results = queryable.ToList();
            results.Should().Equal(new Tuple<int, int, int, int, int, int, int, Tuple<int, int, int, int, int, int, int, Tuple<int, int>>>(
                1, 2, 3, 4, 5, 6, 7,
                new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(
                    8, 9, 10, 11, 12, 13, 14,
                    new Tuple<int, int>(15, 16))));
        }

        [Fact]
        public void Where_Tuple16_Item15_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new Tuple<int, int, int, int, int, int, int, Tuple<int, int, int, int, int, int, int, Tuple<int, int>>>(
                    x.A, x.B, x.C, x.D, x.E, x.F, x.G,
                    new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(
                        x.H, x.I, x.J, x.K, x.L, x.M, x.N,
                        new Tuple<int, int>(x.O, x.P))))
                .Where(x => x.Rest.Rest.Item1 == 15);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I', '$J', '$K', '$L', '$M', '$N', ['$O', '$P']]], _id : 0 } }",
                "{ $match : { '_v.7.7.0' : 15 } }");

            var results = queryable.ToList();
            results.Should().Equal(new Tuple<int, int, int, int, int, int, int, Tuple<int, int, int, int, int, int, int, Tuple<int, int>>>(
                1, 2, 3, 4, 5, 6, 7,
                new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(
                    8, 9, 10, 11, 12, 13, 14,
                    new Tuple<int, int>(15, 16))));
        }

        [Fact]
        public void Where_Tuple16_Item16_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new Tuple<int, int, int, int, int, int, int, Tuple<int, int, int, int, int, int, int, Tuple<int, int>>>(
                    x.A, x.B, x.C, x.D, x.E, x.F, x.G,
                    new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(
                        x.H, x.I, x.J, x.K, x.L, x.M, x.N,
                        new Tuple<int, int>(x.O, x.P))))
                .Where(x => x.Rest.Rest.Item2 == 16);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I', '$J', '$K', '$L', '$M', '$N', ['$O', '$P']]], _id : 0 } }",
                "{ $match : { '_v.7.7.1' : 16 } }");

            var results = queryable.ToList();
            results.Should().Equal(new Tuple<int, int, int, int, int, int, int, Tuple<int, int, int, int, int, int, int, Tuple<int, int>>>(
                1, 2, 3, 4, 5, 6, 7,
                new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(
                    8, 9, 10, 11, 12, 13, 14,
                    new Tuple<int, int>(15, 16))));
        }

        [Fact]
        public void Where_ValueTuple1_Item1_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => ValueTuple.Create(x.A))
                .Where(x => x.Item1 == 1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A'], _id : 0 } }",
                "{ $match : { '_v.0' : 1 } }");

            var results = queryable.ToList();
            results.Should().Equal(ValueTuple.Create(1));
        }

        [Fact]
        public void Where_ValueTuple2_Item1_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => ValueTuple.Create(x.A, x.B))
                .Where(x => x.Item1 == 1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B'], _id : 0 } }",
                "{ $match : { '_v.0' : 1 } }");

            var results = queryable.ToList();
            results.Should().Equal((1, 2));
        }

        [Fact]
        public void Where_ValueTuple2_Item2_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => ValueTuple.Create(x.A, x.B))
                .Where(x => x.Item2 == 2);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B'], _id : 0 } }",
                "{ $match : { '_v.1' : 2 } }");

            var results = queryable.ToList();
            results.Should().Equal((1, 2));
        }

        [Fact]
        public void Where_ValueTuple7_Item1_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => ValueTuple.Create(x.A, x.B, x.C, x.D, x.E, x.F, x.G))
                .Where(x => x.Item1 == 1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G'], _id : 0 } }",
                "{ $match : { '_v.0' : 1 } }");

            var results = queryable.ToList();
            results.Should().Equal((1, 2, 3, 4, 5, 6, 7));
        }

        [Fact]
        public void Where_ValueTuple7_Item7_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => ValueTuple.Create(x.A, x.B, x.C, x.D, x.E, x.F, x.G))
                .Where(x => x.Item7 == 7);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G'], _id : 0 } }",
                "{ $match : { '_v.6' : 7 } }");

            var results = queryable.ToList();
            results.Should().Equal((1, 2, 3, 4, 5, 6, 7));
        }

        [Fact]
        public void Where_ValueTuple8_Item1_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => ValueTuple.Create(x.A, x.B, x.C, x.D, x.E, x.F, x.G, x.H))
                .Where(x => x.Item1 == 1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H']], _id : 0 } }",
                "{ $match : { '_v.0' : 1 } }");

            var results = queryable.ToList();
            results.Should().Equal((1, 2, 3, 4, 5, 6, 7, 8));
        }

        [Fact]
        public void Where_ValueTuple8_Item7_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => ValueTuple.Create(x.A, x.B, x.C, x.D, x.E, x.F, x.G, x.H))
                .Where(x => x.Item7 == 7);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H']], _id : 0 } }",
                "{ $match : { '_v.6' : 7 } }");

            var results = queryable.ToList();
            results.Should().Equal((1, 2, 3, 4, 5, 6, 7, 8));
        }

        [Fact]
        public void Where_ValueTuple8_Item8_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => ValueTuple.Create(x.A, x.B, x.C, x.D, x.E, x.F, x.G, x.H))
                .Where(x => x.Rest.Item1 == 8);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H']], _id : 0 } }",
                "{ $match : { '_v.7.0' : 8 } }");

            var results = queryable.ToList();
            results.Should().Equal((1, 2, 3, 4, 5, 6, 7, 8));
        }

        [Fact]
        public void Where_ValueTuple9_Item1_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>(x.A, x.B, x.C, x.D, x.E, x.F, x.G, new ValueTuple<int, int>(x.H, x.I)))
                .Where(x => x.Item1 == 1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I']], _id : 0 } }",
                "{ $match : { '_v.0' : 1 } }");

            var results = queryable.ToList();
            results.Should().Equal((1, 2, 3, 4, 5, 6, 7, 8, 9));
        }

        [Fact]
        public void Where_ValueTuple9_Item7_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>(x.A, x.B, x.C, x.D, x.E, x.F, x.G, new ValueTuple<int, int>(x.H, x.I)))
                .Where(x => x.Item7 == 7);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I']], _id : 0 } }",
                "{ $match : { '_v.6' : 7 } }");

            var results = queryable.ToList();
            results.Should().Equal((1, 2, 3, 4, 5, 6, 7, 8, 9));
        }

        [Fact]
        public void Where_ValueTuple9_Item8_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>(x.A, x.B, x.C, x.D, x.E, x.F, x.G, new ValueTuple<int, int>(x.H, x.I)))
                .Where(x => x.Rest.Item1 == 8);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I']], _id : 0 } }",
                "{ $match : { '_v.7.0' : 8 } }");

            var results = queryable.ToList();
            results.Should().Equal((1, 2, 3, 4, 5, 6, 7, 8, 9));
        }

        [Fact]
        public void Where_ValueTuple9_Item9_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>(x.A, x.B, x.C, x.D, x.E, x.F, x.G, new ValueTuple<int, int>(x.H, x.I)))
                .Where(x => x.Rest.Item2 == 9);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I']], _id : 0 } }",
                "{ $match : { '_v.7.1' : 9 } }");

            var results = queryable.ToList();
            results.Should().Equal((1, 2, 3, 4, 5, 6, 7, 8, 9));
        }

        [Fact]
        public void Where_ValueTuple16_Item1_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>>(
                    x.A, x.B, x.C, x.D, x.E, x.F, x.G,
                    new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>(
                        x.H, x.I, x.J, x.K, x.L, x.M, x.N,
                        new ValueTuple<int, int>(x.O, x.P))))
                .Where(x => x.Item1 == 1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I', '$J', '$K', '$L', '$M', '$N', ['$O', '$P']]], _id : 0 } }",
                "{ $match : { '_v.0' : 1 } }");

            var results = queryable.ToList();
            results.Should().Equal((1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16));
        }

        [Fact]
        public void Where_ValueTuple16_Item7_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>>(
                    x.A, x.B, x.C, x.D, x.E, x.F, x.G,
                    new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>(
                        x.H, x.I, x.J, x.K, x.L, x.M, x.N,
                        new ValueTuple<int, int>(x.O, x.P))))
                .Where(x => x.Item7 == 7);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I', '$J', '$K', '$L', '$M', '$N', ['$O', '$P']]], _id : 0 } }",
                "{ $match : { '_v.6' : 7 } }");

            var results = queryable.ToList();
            results.Should().Equal((1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16));
        }

        [Fact]
        public void Where_ValueTuple16_Item8_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>>(
                    x.A, x.B, x.C, x.D, x.E, x.F, x.G,
                    new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>(
                        x.H, x.I, x.J, x.K, x.L, x.M, x.N,
                        new ValueTuple<int, int>(x.O, x.P))))
                .Where(x => x.Item8 == 8);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I', '$J', '$K', '$L', '$M', '$N', ['$O', '$P']]], _id : 0 } }",
                "{ $match : { '_v.7.0' : 8 } }");

            var results = queryable.ToList();
            results.Should().Equal((1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16));
        }

        [Fact]
        public void Where_ValueTuple16_Item14_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>>(
                    x.A, x.B, x.C, x.D, x.E, x.F, x.G,
                    new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>(
                        x.H, x.I, x.J, x.K, x.L, x.M, x.N,
                        new ValueTuple<int, int>(x.O, x.P))))
                .Where(x => x.Item14 == 14);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I', '$J', '$K', '$L', '$M', '$N', ['$O', '$P']]], _id : 0 } }",
                "{ $match : { '_v.7.6' : 14 } }");

            var results = queryable.ToList();
            results.Should().Equal((1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16));
        }

        [Fact]
        public void Where_ValueTuple16_Item15_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>>(
                    x.A, x.B, x.C, x.D, x.E, x.F, x.G,
                    new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>(
                        x.H, x.I, x.J, x.K, x.L, x.M, x.N,
                        new ValueTuple<int, int>(x.O, x.P))))
                .Where(x => x.Item15 == 15);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I', '$J', '$K', '$L', '$M', '$N', ['$O', '$P']]], _id : 0 } }",
                "{ $match : { '_v.7.7.0' : 15 } }");

            var results = queryable.ToList();
            results.Should().Equal((1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16));
        }

        [Fact]
        public void Where_ValueTuple16_Item16_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>>(
                    x.A, x.B, x.C, x.D, x.E, x.F, x.G,
                    new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>(
                        x.H, x.I, x.J, x.K, x.L, x.M, x.N,
                        new ValueTuple<int, int>(x.O, x.P))))
                .Where(x => x.Item16 == 16);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$A', '$B', '$C', '$D', '$E', '$F', '$G', ['$H', '$I', '$J', '$K', '$L', '$M', '$N', ['$O', '$P']]], _id : 0 } }",
                "{ $match : { '_v.7.7.1' : 16 } }");

            var results = queryable.ToList();
            results.Should().Equal((1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16));
        }

        private IMongoCollection<T> CreateCollection()
        {
            var collection = GetCollection<T>("test");

            CreateCollection(
                collection,
                new T { Id = 1, A = 1, B = 2, C = 3, D = 4, E = 5, F = 6, G = 7, H = 8, I = 9, J = 10, K = 11, L = 12, M = 13, N = 14, O = 15, P = 16 });

            return collection;
        }

        private class T
        {
            public int Id { get; set; }
            public int A { get; set; }
            public int B { get; set; }
            public int C { get; set; }
            public int D { get; set; }
            public int E { get; set; }
            public int F { get; set; }
            public int G { get; set; }
            public int H { get; set; }
            public int I { get; set; }
            public int J { get; set; }
            public int K { get; set; }
            public int L { get; set; }
            public int M { get; set; }
            public int N { get; set; }
            public int O { get; set; }
            public int P { get; set; }
        }
    }
}
