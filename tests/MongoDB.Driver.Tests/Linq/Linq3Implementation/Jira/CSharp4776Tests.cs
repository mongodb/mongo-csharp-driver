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
    public class CSharp4776Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Where_with_Count_method_equal_to_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.A.Count() == 2);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { A : { $size : 2 } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(4);
        }

        [Fact]
        public void Where_with_Count_method_greater_than_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.A.Count() > 2);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'A.2' : { $exists : true } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(5);
        }

        [Fact]
        public void Where_with_Count_method_greater_than_or_equal_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.A.Count() >= 2);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'A.1' : { $exists : true } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(4, 5);
        }

        [Fact]
        public void Where_with_Count_method_less_than_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.A.Count() < 2);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'A.1' : { $exists : false } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1, 2, 3);
        }

        [Fact]
        public void Where_with_Count_method_less_than_or_equal_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.A.Count() <= 2);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'A.2' : { $exists : false } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1, 2, 3, 4);
        }

        [Fact]
        public void Where_with_Count_method_not_equal_to_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.A.Count() != 2);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { A : { $not : { $size : 2 } } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1, 2, 3, 5);
        }

        [Fact]
        public void Where_with_Count_property_method_equal_to_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.L.Count == 2);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { L : { $size : 2 } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(4);
        }

        [Fact]
        public void Where_with_Count_property_greater_than_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.L.Count > 2);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'L.2' : { $exists : true } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(5);
        }

        [Fact]
        public void Where_with_Count_property_greater_than_or_equal_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.L.Count >= 2);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'L.1' : { $exists : true } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(4, 5);
        }

        [Fact]
        public void Where_with_Count_property_less_than_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.L.Count < 2);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'L.1' : { $exists : false } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1, 2, 3);
        }

        [Fact]
        public void Where_with_Count_property_less_than_or_equal_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.L.Count <= 2);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'L.2' : { $exists : false } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1, 2, 3, 4);
        }

        [Fact]
        public void Where_with_Count_property_method_not_equal_to_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.L.Count != 2);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { L : { $not : { $size : 2 } } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1, 2, 3, 5);
        }

        [Fact]
        public void Where_with_Length_property_equal_to_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.A.Length == 2);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { A : { $size : 2 } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(4);
        }

        [Fact]
        public void Where_with_Length_property_greater_than_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.A.Length > 2);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'A.2' : { $exists : true } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(5);
        }

        [Fact]
        public void Where_with_Length_property_greater_than_or_equal_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.A.Length >= 2);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'A.1' : { $exists : true } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(4, 5);
        }

        [Fact]
        public void Where_with_Length_property_less_than_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.A.Length < 2);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'A.1' : { $exists : false } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1, 2, 3);
        }

        [Fact]
        public void Where_with_Length_property_less_than_or_equal_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.A.Length <= 2);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'A.2' : { $exists : false } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1, 2, 3, 4);
        }

        [Fact]
        public void Where_with_Length_property_not_equal_to_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.A.Length != 2);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { A : { $not : { $size : 2 } } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1, 2, 3, 5);
        }

        [Fact]
        public void Where_with_LongCount_method_method_equal_to_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.L.LongCount() == 2);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { L : { $size : 2 } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(4);
        }

        [Fact]
        public void Where_with_LongCount_method_greater_than_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.L.LongCount() > 2);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'L.2' : { $exists : true } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(5);
        }

        [Fact]
        public void Where_with_LongCount_method_greater_than_or_equal_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.L.LongCount() >= 2);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'L.1' : { $exists : true } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(4, 5);
        }

        [Fact]
        public void Where_with_LongCount_method_less_than_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.L.LongCount() < 2);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'L.1' : { $exists : false } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1, 2, 3);
        }

        [Fact]
        public void Where_with_LongCount_method_less_than_or_equal_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.L.LongCount() <= 2);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'L.2' : { $exists : false } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1, 2, 3, 4);
        }

        [Fact]
        public void Where_with_LongCount_method_method_not_equal_to_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.L.LongCount() != 2);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { L : { $not : { $size : 2 } } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1, 2, 3, 5);
        }

        private IMongoCollection<C> GetCollection()
        {
            var collection = GetCollection<C>("test");
            CreateCollection(
                collection,
                new C { Id = 1, A = null, L = null },
                new C { Id = 2, A = new int[0], L = new List<int>() },
                new C { Id = 3, A = new[] { 1 }, L = new List<int> { 1 } },
                new C { Id = 4, A = new[] { 1, 2 }, L = new List<int> { 1, 2 } },
                new C { Id = 5, A = new[] { 1, 2, 3 }, L = new List<int> { 1, 2, 3 } });
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public int[] A { get; set; }
            public IList<int> L { get; set; }
        }

    }
}
