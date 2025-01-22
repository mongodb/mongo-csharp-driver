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

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4048Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Array_ArrayIndex_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.ToArray()[0] })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push: '$$ROOT' } } }",
                "{ $project : { _id : '$_id', Result : { $arrayElemAt : ['$_elements', 0] } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Should().HaveCount(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = new C { Id = 1, X = 1 } });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = new C { Id = 2, X = 2 } });
        }

        [Fact]
        public void Array_ArrayIndex_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.ToArray()[0] })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push: '$X' } } }",
                "{ $project : { _id : '$_id', Result : { $arrayElemAt : ['$_elements', 0] } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Should().HaveCount(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 1 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 2 });
        }

        [Fact]
        public void List_get_Item_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.ToList()[0] })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push: '$$ROOT' } } }",
                "{ $project : { _id : '$_id', Result : { $arrayElemAt : ['$_elements', 0] } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Should().HaveCount(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = new C { Id = 1, X = 1 } });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = new C { Id = 2, X = 2 } });
        }

        [Fact]
        public void List_get_Item_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.ToList()[0] })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push: '$X' } } }",
                "{ $project : { _id : '$_id' Result : { $arrayElemAt : ['$_elements', 0] } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Should().HaveCount(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 1 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 2 });
        }

        [Fact]
        public void IGrouping_Aggregate_with_func_of_root_should_return_expected_result()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.Aggregate((a, e) => a) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push: '$$ROOT' } } }",
                "{ $project : { _id : '$_id', Result : { $let : { vars : { seed : { $arrayElemAt : ['$_elements', 0] }, rest : { $slice : ['$_elements', 1, 2147483647] } }, in : { $cond : { if : { $eq : [{ $size : '$$rest' }, 0] }, then : '$$seed', else : { $reduce : { input : '$$rest', initialValue : '$$seed', in : '$$value' } } } } }  } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = new C { Id = 1, X = 1 } });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = new C { Id = 2, X = 2 } });
        }

        [Fact]
        public void IGrouping_Aggregate_with_func_of_scalar_should_return_expected_result()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.Aggregate((a, e) => a) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push: '$X' } } }",
                "{ $project : { _id : '$_id', Result : { $let : { vars : { seed : { $arrayElemAt : ['$_elements', 0] }, rest : { $slice : ['$_elements', 1, 2147483647] } }, in : { $cond : { if : { $eq : [{ $size : '$$rest' }, 0] }, then : '$$seed', else : { $reduce : { input : '$$rest', initialValue : '$$seed', in : '$$value' } } } } } } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 1 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 2 });
        }

        [Fact]
        public void IGrouping_Aggregate_with_seed_and_func_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.Aggregate(0, (a, e) => a + e.X) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push: '$$ROOT' } } }",
                "{ $project : { _id : '$_id', Result : { $reduce : { input : '$_elements', initialValue : 0, in : { $add : ['$$value', '$$this.X'] } } } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 1 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 2 });
        }

        [Fact]
        public void IGrouping_Aggregate_with_seed_and_func_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.Aggregate(0, (a, e) => a + e) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push: '$X' } } }",
                "{ $project : { _id : '$_id', Result : { $reduce : { input : '$_elements', initialValue : 0, in : { $add : ['$$value', '$$this'] } } } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 1 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 2 });
        }

        [Fact]
        public void IGrouping_Aggregate_with_seed_and_func_and_resultSelector_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.Aggregate(0, (a, e) => a + e.X, a => a) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push: '$$ROOT' } } }",
                "{ $project : { _id : '$_id', Result : { $reduce : { input : '$_elements', initialValue : 0, in : { $add : ['$$value', '$$this.X'] } } } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 1 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 2 });
        }

        [Fact]
        public void IGrouping_Aggregate_with_seed_and_func_and_resultSelector_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.Aggregate(0, (a, e) => a + e, a => a) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push: '$X' } } }",
                "{ $project : { _id : '$_id', Result : { $reduce : { input : '$_elements', initialValue : 0, in : { $add : ['$$value', '$$this'] } } } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 1 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 2 });
        }

        [Fact]
        public void IGrouping_All_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.All(e => e.X > 0) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $push: { $gt : ['$X', 0] } } } }", // MQL could be optimized further
                "{ $project : { _id : '$_id', Result : { $allElementsTrue : '$__agg0' } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = true });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = true });
        }

        [Fact]
        public void IGrouping_All_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.All(e => e > 0) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $push: { $gt : ['$X', 0] } } } }", // MQL could be optimized further
                "{ $project : { _id : '$_id', Result : { $allElementsTrue : '$__agg0' } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = true });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = true });
        }

        [Fact]
        public void IGrouping_Any_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.Any() })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $sum : 1 } } }",
                "{ $project : { _id : '$_id', Result : { $gt : ['$__agg0', 0] } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = true });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = true });
        }

        [Fact]
        public void IGrouping_Any_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.Any() })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $sum : 1 } } }",
                "{ $project : { _id : '$_id', Result : { $gt : ['$__agg0', 0] } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = true });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = true });
        }

        [Fact]
        public void IGrouping_Any_with_predicate_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.Any(e => e.X >0) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $push: { $gt : ['$X', 0] } } } }", // MQL could be optimized further
                "{ $project : { _id : '$_id', Result : { $anyElementTrue : '$__agg0' } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = true });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = true });
        }

        [Fact]
        public void IGrouping_Any_with_predicate_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.Any(e => e > 0) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $push : { $gt : ['$X', 0] } } } }", // MQL could be optimized further
                "{ $project : { _id : '$_id', Result : { $anyElementTrue : '$__agg0' } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = true });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = true });
        }

        [Fact]
        public void IGrouping_Average_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.Average(e => e.X) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $avg : '$X' } } }",
                "{ $project : { _id : '$_id', Result : '$__agg0'} }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 1.0 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 2.0 });
        }

        [Fact]
        public void IGrouping_Average_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.Average() })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $avg : '$X' } } }",
                "{ $project : { _id : '$_id', Result : '$__agg0'} }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 1.0 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 2.0 });
        }

        [Fact]
        public void IGrouping_Average_with_selector_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.Average(e => e.X) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $avg : '$X' } } }",
                "{ $project : { _id : '$_id', Result : '$__agg0'} }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 1.0 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 2.0 });
        }

        [Fact]
        public void IGrouping_Average_with_selector_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.Average(e => e) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $avg : '$X' } } }",
                "{ $project : { _id : '$_id', Result : '$__agg0'} }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 1.0 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 2.0 });
        }

        [Fact]
        public void IGrouping_Concat_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.Concat(new[] { new C { Id = 3, X = 3 } }) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push : '$$ROOT' } } }",
                "{ $project : { _id : '$_id', Result : { $concatArrays : ['$_elements', [{ _id : 3, X : 3 }]] } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = new List<C> { new C { Id = 1, X = 1 }, new C { Id = 3, X = 3 } } });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = new List<C> { new C { Id = 2, X = 2 }, new C { Id = 3, X = 3 } } });
        }

        [Fact]
        public void IGrouping_Concat_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.Concat(new[] { 3 }) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push : '$X' } } }",
                "{ $project : { _id : '$_id', Result : { $concatArrays : ['$_elements', [3]] } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = new List<int> { 1, 3 } });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = new List<int> { 2, 3 } });
        }

        [Fact]
        public void IGrouping_Contains_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.Contains(new C { Id = 1, X = 1 }) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push : '$$ROOT' } } }", // MQL could be optimized further
                "{ $project : { _id : '$_id', Result : { $in : [{ _id : 1, X : 1 }, '$_elements'] } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = true });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = false });
        }

        [Fact]
        public void IGrouping_Contains_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.Contains(1) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push : '$X' } } }", // MQL could be optimized further
                "{ $project : { _id : '$_id', Result : { $in : [1, '$_elements'] } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = true });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = false });
        }

        [Fact]
        public void IGrouping_Count_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.Count() })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $sum : 1 } } }",
                "{ $project : { _id : '$_id', Result : '$__agg0' } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 1 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 1 });
        }

        [Fact]
        public void IGrouping_Count_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.Count() })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $sum : 1 } } }",
                "{ $project : { _id : '$_id', Result : '$__agg0' } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 1 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 1 });
        }

        [Fact]
        public void IGrouping_Count_with_predicate_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.Count(e => e.X == 1) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $sum : { $cond : { if : { $eq : ['$X', 1] }, then : 1, else : 0 } } } } }",
                "{ $project : { _id : '$_id', Result : '$__agg0' } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 1 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 0 });
        }

        [Fact]
        public void IGrouping_Count_with_predicate_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.Count(e => e == 1) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $sum : { $cond : { if : { $eq : ['$X', 1] }, then : 1, else : 0 } } } } }",
                "{ $project : { _id : '$_id', Result : '$__agg0' } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 1 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 0 });
        }

        [Fact]
        public void IGrouping_DefaultIfEmpty_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.DefaultIfEmpty() })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push : '$$ROOT' } } }",
                "{ $project : { _id : '$_id', Result : { $cond : { if : { $eq : [{ $size : '$_elements' }, 0] }, then : [null], else : '$_elements' } } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = new List<C> { new C { Id = 1, X = 1 } } });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = new List<C> { new C { Id = 2, X = 2 } } });
        }

        [Fact]
        public void IGrouping_DefaultIfEmpty_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.DefaultIfEmpty() })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push : '$X' } } }",
                "{ $project : { _id : '$_id', Result : { $cond : { if : { $eq : [{ $size : '$_elements' }, 0] }, then : [0], else : '$_elements' } } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = new List<int> { 1 } });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = new List<int> { 2 } });
        }

        [Fact]
        public void IGrouping_Distinct_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.Distinct() })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $addToSet : '$$ROOT' } } }",
                "{ $project : { _id : '$_id', Result : '$__agg0' } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = new List<C> { new C { Id = 1, X = 1 } } });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = new List<C> { new C { Id = 2, X = 2 } } });
        }

        [Fact]
        public void IGrouping_Distinct_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.Distinct() })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $addToSet : '$X' } } }",
                "{ $project : { _id : '$_id', Result : '$__agg0' } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = new List<int> { 1 } });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = new List<int> { 2 } });
        }

        [Fact]
        public void IGrouping_ElementAt_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.ElementAt(0) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push : '$$ROOT' } } }",
                "{ $project : { _id : '$_id', Result : { $arrayElemAt : ['$_elements', 0] } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = new C { Id = 1, X = 1 } });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = new C { Id = 2, X = 2 } });
        }

        [Fact]
        public void IGrouping_ElementAt_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.ElementAt(0) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push : '$X' } } }",
                "{ $project : { _id : '$_id', Result : { $arrayElemAt : ['$_elements', 0] } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 1 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 2 });
        }

        [Fact]
        public void IGrouping_ElementAtOrDefault_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.ElementAtOrDefault(0) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push : '$$ROOT' } } }",
                "{ $project : { _id : '$_id', Result : { $cond : { if : { $gte : [0, { $size : '$_elements' }] }, then : null, else : { $arrayElemAt : ['$_elements', 0] } } } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = new C { Id = 1, X = 1 } });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = new C { Id = 2, X = 2 } });
        }

        [Fact]
        public void IGrouping_ElementAtOrDefault_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.ElementAtOrDefault(0) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push : '$X' } } }",
                "{ $project : { _id : '$_id', Result : { $cond : { if : { $gte : [0, { $size : '$_elements' }] }, then : 0, else : { $arrayElemAt : ['$_elements', 0] } } } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 1 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 2 });
        }

        [Fact]
        public void IGrouping_Except_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.Except(new[] { new C {  Id = 1, X = 1 } }) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push : '$$ROOT' } } }",
                "{ $project : { _id : '$_id', Result : { $setDifference : ['$_elements', [{ _id : 1, X : 1 }]] } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = new List<C>() });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = new List<C> { new C { Id = 2, X = 2 } } });
        }

        [Fact]
        public void IGrouping_Except_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.Except(new[] { 1 }) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push : '$X' } } }",
                "{ $project : { _id : '$_id', Result : { $setDifference : ['$_elements', [1]] } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = new List<int>() });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = new List<int> { 2 } });
        }

        [Fact]
        public void IGrouping_First_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.First() })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $first : '$$ROOT' } } }",
                "{ $project : { _id : '$_id', Result : '$__agg0' } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = new C { Id = 1, X = 1 } });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = new C { Id = 2, X = 2 } });
        }

        [Fact]
        public void IGrouping_First_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.First() })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $first : '$X' } } }",
                "{ $project : { _id : '$_id', Result : '$__agg0' } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 1 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 2 });
        }

        [Fact]
        public void IGrouping_First_with_predicate_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.First(e => e.X != 1) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push : '$$ROOT' } } }",
                "{ $project : { _id : '$_id', Result : { $arrayElemAt : [{ $filter : { input : '$_elements', as : 'e', cond : { $ne : ['$$e.X', 1] } } }, 0] } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = (C)null });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = new C { Id = 2, X = 2 } });
        }

        [Fact]
        public void IGrouping_First_with_predicate_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.First(e => e != 1) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push : '$X' } } }",
                "{ $project : { _id : '$_id', Result : { $arrayElemAt : [{ $filter : { input : '$_elements', as : 'e', cond : { $ne : ['$$e', 1] } } }, 0] } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 0 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 2 });
        }

        [Fact]
        public void IGrouping_FirstOrDefault_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.FirstOrDefault() })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $sum : 1 }, __agg1 : { $first : '$$ROOT' } } }",
                "{ $project : { _id : '$_id', Result : { $cond : { if : { $eq : ['$__agg0', 0] }, then : null, else : '$__agg1' } } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = new C { Id = 1, X = 1 } });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = new C { Id = 2, X = 2 } });
        }

        [Fact]
        public void IGrouping_FirstOrDefault_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.FirstOrDefault() })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $sum : 1 }, __agg1 : { $first : '$X' } } }",
                "{ $project : { _id : '$_id', Result : { $cond : { if : { $eq : ['$__agg0', 0] }, then : 0, else : '$__agg1' } } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 1 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 2 });
        }

        [Fact]
        public void IGrouping_FirstOrDefault_with_predicate_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.FirstOrDefault(e => e.X != 1) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push : '$$ROOT' } } }",
                "{ $project : { _id : '$_id', Result : { $let : { vars : { values : { $filter : { input : '$_elements', as : 'e', cond : { $ne : ['$$e.X', 1] } } } }, in : { $cond : { if : { $eq : [{ $size : '$$values' }, 0] }, then : null, else : { $arrayElemAt : ['$$values', 0] } } } } } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].Id.Should().Be(1);
            results[0].Result.Should().BeNull();
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = new { Id = 2, X = 2 } });
        }

        [Fact]
        public void IGrouping_FirstOrDefault_with_predicate_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.FirstOrDefault(e => e != 1) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push : '$X' } } }",
                "{ $project : { _id : '$_id', Result : { $let : { vars : { values : { $filter : { input : '$_elements', as : 'e', cond : { $ne : ['$$e', 1] } } } }, in : { $cond : { if : { $eq : [{ $size : '$$values' }, 0] }, then : 0, else : { $arrayElemAt : ['$$values', 0] } } } } } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 0 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 2 });
        }

        [Fact]
        public void IGrouping_Intersect_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.Intersect(new[] { new C { Id = 1, X = 1 } }) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push : '$$ROOT' } } }",
                "{ $project : { _id : '$_id', Result : { $setIntersection : ['$_elements', [{ _id : 1, X : 1 }]] } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = new List<C> { new C { Id = 1, X = 1 } } });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = new List<C>() });
        }

        [Fact]
        public void IGrouping_Intersect_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.Intersect(new[] { 1 }) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push : '$X' } } }",
                "{ $project : { _id : '$_id', Result : { $setIntersection : ['$_elements', [1]] } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = new List<int> { 1 } });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = new List<int>() });
        }

        [Fact]
        public void IGrouping_Last_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.Last() })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $last : '$$ROOT' } } }",
                "{ $project : { _id : '$_id', Result : '$__agg0' } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = new C { Id = 1, X = 1 } });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = new C { Id = 2, X = 2 } });
        }

        [Fact]
        public void IGrouping_Last_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.Last() })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $last : '$X' } } }",
                "{ $project : { _id : '$_id', Result : '$__agg0' } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 1 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 2 });
        }

        [Fact]
        public void IGrouping_Last_with_predicate_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.Last(e => e.X != 1) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push : '$$ROOT' } } }",
                "{ $project : { _id : '$_id', Result : { $arrayElemAt : [{ $filter : { input : '$_elements', as : 'e', cond : { $ne : ['$$e.X', 1] } } }, -1] } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = (C)null });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = new C { Id = 2, X = 2 } });
        }

        [Fact]
        public void IGrouping_Last_with_predicate_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.Last(e => e != 1) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push : '$X' } } }", // MQL could be optimized further
                "{ $project : { _id : '$_id', Result : { $arrayElemAt : [{ $filter : { input : '$_elements', as : 'e', cond : { $ne : ['$$e', 1] } } }, -1] } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 0 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 2 });
        }

        [Fact]
        public void IGrouping_LastOrDefault_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.LastOrDefault() })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $sum : 1 }, __agg1 : { $last : '$$ROOT' } } }",
                "{ $project : { _id : '$_id', Result : { $cond : { if : { $eq : ['$__agg0', 0] }, then : null, else : '$__agg1' } } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = new { Id = 1, X = 1 } });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = new { Id = 2, X = 2 } });
        }

        [Fact]
        public void IGrouping_LastOrDefault_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.LastOrDefault() })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $sum : 1 }, __agg1 : { $last : '$X' } } }",
                "{ $project : { _id : '$_id', Result : { $cond : { if : { $eq : ['$__agg0', 0] }, then : 0, else : '$__agg1' } } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 1 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 2 });
        }

        [Fact]
        public void IGrouping_LastOrDefault_with_predicate_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.LastOrDefault(e => e.X != 1) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push : '$$ROOT' } } }",
                "{ $project : { _id : '$_id', Result : { $let : { vars : { values : { $filter : { input : '$_elements', as : 'e', cond : { $ne : ['$$e.X', 1] } } } }, in : { $cond : { if : { $eq : [{ $size : '$$values' }, 0] }, then : null, else : { $arrayElemAt : ['$$values', -1] } } } } } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].Id.Should().Be(1);
            results[0].Result.Should().BeNull();
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = new { Id = 2, X = 2 } });
        }

        [Fact]
        public void IGrouping_LastOrDefault_with_predicate_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.LastOrDefault(e => e != 1) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push : '$X' } } }",
                "{ $project : { _id : '$_id', Result : { $let : { vars : { values : { $filter : { input : '$_elements', as : 'e', cond : { $ne : ['$$e', 1] } } } }, in : { $cond : { if : { $eq : [{ $size : '$$values' }, 0] }, then : 0, else : { $arrayElemAt : ['$$values', -1] } } } } } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 0 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 2 });
        }

        [Fact]
        public void IGrouping_LongCount_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.LongCount() })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $sum : 1 } } }",
                "{ $project : { _id : '$_id', Result : '$__agg0' } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 1 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 1 });
        }

        [Fact]
        public void IGrouping_LongCount_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.LongCount() })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $sum : 1 } } }",
                "{ $project : { _id : '$_id', Result : '$__agg0' } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 1 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 1 });
        }

        [Fact]
        public void IGrouping_LongCount_with_predicate_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.LongCount(e => e.X == 1) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $sum : { $cond : { if : { $eq : ['$X', 1] }, then : 1, else : 0 } } } } }",
                "{ $project : { _id : '$_id', Result : '$__agg0' } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 1 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 0 });
        }

        [Fact]
        public void IGrouping_LongCount_with_predicate_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.LongCount(e => e == 1) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $sum : { $cond : { if : { $eq : ['$X', 1] }, then : 1, else : 0 } } } } }",
                "{ $project : { _id : '$_id', Result : '$__agg0' } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 1 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 0 });
        }

        [Fact]
        public void IGrouping_Max_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.Max(e => e.X) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $max : '$X' } } }",
                "{ $project : { _id : '$_id', Result : '$__agg0' } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 1.0 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 2.0 });
        }

        [Fact]
        public void IGrouping_Max_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.Max() })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $max : '$X' } } }",
                "{ $project : { _id : '$_id', Result : '$__agg0' } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 1.0 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 2.0 });
        }

        [Fact]
        public void IGrouping_Max_with_selector_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.Max(e => e.X) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $max : '$X' } } }",
                "{ $project : { _id : '$_id', Result : '$__agg0' } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 1.0 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 2.0 });
        }

        [Fact]
        public void IGrouping_Max_with_selector_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.Max(e => e) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $max : '$X' } } }",
                "{ $project : { _id : '$_id', Result : '$__agg0' } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 1.0 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 2.0 });
        }

        [Fact]
        public void IGrouping_Min_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.Min(e => e.X) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $min : '$X' } } }",
                "{ $project : { _id : '$_id', Result : '$__agg0' } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 1.0 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 2.0 });
        }

        [Fact]
        public void IGrouping_Min_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.Min() })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $min : '$X' } } }",
                "{ $project : { _id : '$_id', Result : '$__agg0' } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 1.0 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 2.0 });
        }

        [Fact]
        public void IGrouping_Min_with_selector_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.Min(e => e.X) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $min : '$X' } } }",
                "{ $project : { _id : '$_id', Result : '$__agg0' } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 1.0 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 2.0 });
        }

        [Fact]
        public void IGrouping_Min_with_selector_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.Min(e => e) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $min : '$X' } } }",
                "{ $project : { _id : '$_id', Result : '$__agg0' } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 1.0 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 2.0 });
        }

        [Fact]
        public void IGrouping_Reverse_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.Reverse() })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push : '$$ROOT' } } }",
                "{ $project : { _id : '$_id', Result : { $reverseArray : '$_elements' } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = new List<C> { new C { Id = 1, X = 1 } } });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = new List<C> { new C { Id = 2, X = 2 } } });
        }

        [Fact]
        public void IGrouping_Reverse_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.Reverse() })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push : '$X' } } }",
                "{ $project : { _id : '$_id', Result : { $reverseArray : '$_elements' } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = new List<int> { 1 } });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = new List<int> { 2 } });
        }

        [Fact]
        public void IGrouping_Select_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.Select(e => e.X) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $push : '$X' } } }",
                "{ $project : { _id : '$_id', Result : '$__agg0' } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = new List<int> { 1 } });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = new List<int> { 2 } });
        }

        [Fact]
        public void IGrouping_Select_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.Select(e => e) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $push : '$X' } } }",
                "{ $project : { _id : '$_id', Result : '$__agg0' } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = new List<int> { 1 } });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = new List<int> { 2 } });
        }

        [Fact]
        public void IGrouping_StandardDeviationPopulation_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.StandardDeviationPopulation(e => e.X) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $stdDevPop : '$X' } } }",
                "{ $project : { _id : '$_id', Result : '$__agg0' } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 0.0 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 0.0 });
        }

        [Fact]
        public void IGrouping_StandardDeviationPopulation_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.StandardDeviationPopulation() })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $stdDevPop : '$X' } } }",
                "{ $project : { _id : '$_id', Result : '$__agg0' } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 0.0 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 0.0 });
        }

        [Fact]
        public void IGrouping_StandardDeviationPopulation_with_selector_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.StandardDeviationPopulation(e => e.X) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $stdDevPop : '$X' } } }",
                "{ $project : { _id : '$_id', Result : '$__agg0' } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 0.0 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 0.0 });
        }

        [Fact]
        public void IGrouping_StandardDeviationPopulation_with_selector_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.StandardDeviationPopulation(e => e) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $stdDevPop : '$X' } } }",
                "{ $project : { _id : '$_id', Result : '$__agg0' } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 0.0 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 0.0 });
        }

        [Fact]
        public void IGrouping_StandardDeviationSample_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = (double?)g.StandardDeviationSample(e => e.X) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $stdDevSamp : '$X' } } }",
                "{ $project : { _id : '$_id', Result : '$__agg0' } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = (double?)null });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = (double?)null });
        }

        [Fact]
        public void IGrouping_StandardDeviationSample_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = (double?)g.StandardDeviationSample() })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $stdDevSamp : '$X' } } }",
                "{ $project : { _id : '$_id', Result : '$__agg0' } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = (double?)null });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = (double?)null });
        }

        [Fact]
        public void IGrouping_StandardDeviationSample_with_selector_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = (double?)g.StandardDeviationSample(e => e.X) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $stdDevSamp : '$X' } } }",
                "{ $project : { _id : '$_id', Result : '$__agg0' } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = (double?)null });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = (double?)null });
        }

        [Fact]
        public void IGrouping_StandardDeviationSample_with_selector_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = (double?)g.StandardDeviationSample(e => e) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $stdDevSamp : '$X' } } }",
                "{ $project : { _id : '$_id', Result : '$__agg0' } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = (double?)null });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = (double?)null });
        }

        [Fact]
        public void IGrouping_Sum_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.Sum(e => e.X) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $sum : '$X' } } }",
                "{ $project : { _id : '$_id', Result : '$__agg0' } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 1.0 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 2.0 });
        }

        [Fact]
        public void IGrouping_Sum_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.Sum() })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $sum : '$X' } } }",
                "{ $project : { _id : '$_id', Result : '$__agg0' } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = 1.0 });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = 2.0 });
        }

        [Fact]
        public void IGrouping_Take_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.Take(1) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push: '$$ROOT' } } }",
                "{ $project : { _id : '$_id', Result : { $slice : ['$_elements', 1] } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Should().HaveCount(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = new List<C> { new C { Id = 1, X = 1 } } });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = new List<C> { new C { Id = 2, X = 2 } } });
        }

        [Fact]
        public void IGrouping_Take_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.Take(1) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push: '$X' } } }",
                "{ $project : { _id : '$_id', Result : { $slice : ['$_elements', 1] } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Should().HaveCount(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = new List<int> { 1 } });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = new List<int> { 2 } });
        }

        [Fact]
        public void IGrouping_ToArray_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.ToArray() })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push: '$$ROOT' } } }",
                "{ $project : { _id : '$_id', Result : '$_elements' } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Should().HaveCount(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = new List<C> { new C { Id = 1, X = 1 } } });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = new List<C> { new C { Id = 2, X = 2 } } });
        }

        [Fact]
        public void IGrouping_ToArray_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.ToArray() })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push: '$X' } } }",
                "{ $project : { _id : '$_id', Result : '$_elements' } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Should().HaveCount(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = new List<int> { 1 } });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = new List<int> { 2 } });
        }

        [Fact]
        public void IGrouping_ToList_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.ToList() })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push: '$$ROOT' } } }",
                "{ $project : { _id : '$_id', Result : '$_elements' } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Should().HaveCount(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = new List<C> { new C { Id = 1, X = 1 } } });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = new List<C> { new C { Id = 2, X = 2 } } });
        }

        [Fact]
        public void IGrouping_ToList_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.ToList() })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push: '$X' } } }",
                "{ $project : { _id : '$_id', Result : '$_elements' } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Should().HaveCount(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = new List<int> { 1 } });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = new List<int> { 2 } });
        }

        [Fact]
        public void IGrouping_Union_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.Union(new[] { new C { Id = 1, X = 1 } }) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push : '$$ROOT' } } }",
                "{ $project : { _id : '$_id', Result : { $setUnion : ['$_elements', [{ _id : 1, X : 1 }]] } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = new List<C> { new C { Id = 1, X = 1 } } });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = new List<C> { new C { Id = 1, X = 1 }, new C { Id = 2, X = 2 } } });
        }

        [Fact]
        public void IGrouping_Union_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.Union(new[] { 1 }) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push : '$X' } } }",
                "{ $project : { _id : '$_id', Result : { $setUnion : ['$_elements', [1]] } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = new List<int> { 1 } });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = new List<int> { 1, 2 } });
        }

        [Fact]
        public void IGrouping_Where_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.Where(e => e.X == 1) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push : '$$ROOT' } } }",
                "{ $project : { _id : '$_id', Result : { $filter : { input : '$_elements', as : 'e', cond : { $eq : ['$$e.X', 1] } } } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = new List<C> { new C { Id = 1, X = 1 } } });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = new List<C>() });
        }

        [Fact]
        public void IGrouping_Where_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.Where(e => e == 1) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push : '$X' } } }", // MQL could be optimized further
                "{ $project : { _id : '$_id', Result : { $filter : { input : '$_elements', as : 'e', cond : { $eq : ['$$e', 1] } } } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = new List<int> { 1 } });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = new List<int>() });
        }

        [Fact]
        public void IGrouping_Zip_of_root_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id)
                .Select(g => new { Id = g.Key, Result = g.Zip(new[] { new { Y = 3 } }, (x, y) => new { Id = x.Id, X = x.X, Y = y.Y }) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push: '$$ROOT' } } }",
                "{ $project : { _id : '$_id', Result : { $map : { input : { $zip : { inputs : ['$_elements', [{ Y : 3 }]] } }, as : 'pair', in : { $let : { vars : { x : { $arrayElemAt : ['$$pair', 0] }, y : { $arrayElemAt : ['$$pair', 1] } }, in : { _id : '$$x._id', X : '$$x.X', Y : '$$y.Y' } } } } } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Should().HaveCount(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = CreateList(new { Id = 1, X = 1, Y = 3 }) });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = CreateList(new { Id = 2, X = 2, Y = 3 }) });
        }

        [Fact]
        public void IGrouping_Zip_of_scalar_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => c.Id, c => c.X)
                .Select(g => new { Id = g.Key, Result = g.Zip(new[] { 3 }, (x, y) => new { X = x, Y = y }) })
                .OrderBy(x => x.Id);

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push: '$X' } } }",
                "{ $project : { _id : '$_id', Result : { $map : { input : { $zip : { inputs : ['$_elements', [3]] } }, as : 'pair', in : { $let : { vars : { x : { $arrayElemAt : ['$$pair', 0] }, y : { $arrayElemAt : ['$$pair', 1] } }, in : { X : '$$x', Y : '$$y' } } } } } } }",
                "{ $sort : { _id : 1 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Should().HaveCount(2);
            results[0].ShouldBeEquivalentTo(new { Id = 1, Result = CreateList(new { X = 1, Y = 3 }) });
            results[1].ShouldBeEquivalentTo(new { Id = 2, Result = CreateList(new { X = 2, Y = 3 }) });
        }

        private IMongoCollection<C>  CreateCollection()
        {
            var collection = GetCollection<C>();
            var documents = new[]
            {
                new C { Id = 1, X = 1 },
                new C { Id = 2, X = 2 }
            };
            CreateCollection(collection, documents);
            return collection;
        }

        private List<TAnonymous> CreateList<TAnonymous>(params TAnonymous[] items)
        {
            return new List<TAnonymous>(items);
        }

        private class C
        {
            public int Id { get; set; }
            public int X { get; set; }
        }
    }
}
