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

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToExecutableQueryTranslators
{
    public class ExecutableQueryTests : Linq3IntegrationTest
    {
        [Fact]
        public void Cast_to_object_should_work()
        {
            var collection = GetCollection();
            var queryable1 = collection.AsQueryable();
            var queryable2 = queryable1.Provider.CreateQuery<object>(queryable1.Expression);

            var results = queryable2.ToList();

            results.Should().HaveCount(5);
        }

        [Fact]
        public void Cast_aggregation_to_object_should_work()
        {
            var collection = GetCollection();
            var queryable1 = collection.AsQueryable().GroupBy(
                p => p.Type,
                (k, p) => new ProductAggregation {Type = k, MaxPrice = p.Select(i => i.Price).Max()});
            var queryable2 = queryable1.Provider.CreateQuery<object>(queryable1.Expression);

            var results = queryable2.ToList();

            results.Should().HaveCount(2);
        }

        [Fact]
        public void Cast_int_to_object_should_work()
        {
            var collection = GetCollection();
            var queryable1 = collection.AsQueryable().Select(p => p.Id);
            var queryable2 = queryable1.Provider.CreateQuery<object>(queryable1.Expression);

            var results = queryable2.ToList();

            results.Should().HaveCount(5);
        }

        [Fact]
        public void Cast_to_nullable_should_work()
        {
            var collection = GetCollection();
            var queryable1 = collection.AsQueryable().Select(p => p.Id);
            var queryable2 = queryable1.Provider.CreateQuery<int?>(queryable1.Expression);

            var results = queryable2.ToList();

            results.Should().HaveCount(5);
        }

        [Fact]
        public void Cast_to_incompatible_type_should_throw()
        {
            var collection = GetCollection();
            var queryable1 = collection.AsQueryable();
            var queryable2 = queryable1.Provider.CreateQuery<ProductAggregation>(queryable1.Expression);

            var exception = Record.Exception(() => queryable2.ToList());

            exception.Should().BeOfType<NotSupportedException>();
            exception.Message.Should().Contain($"The type of the pipeline output is {typeof(DerivedProduct)} which is not assignable to {typeof(ProductAggregation)}");
        }

        [Fact]
        public void Cast_to_interface_should_work()
        {
            var collection = GetCollection();
            var queryable1 = collection.AsQueryable();
            var queryable2 = queryable1.Provider.CreateQuery<IProduct>(queryable1.Expression);

            var results = queryable2.ToList();

            results.Should().HaveCount(5);
        }

        [Fact]
        public void Cast_to_base_class_should_work()
        {
            var collection = GetCollection();
            var queryable1 = collection.AsQueryable();
            var queryable2 = queryable1.Provider.CreateQuery<ProductBase>(queryable1.Expression);

            var results = queryable2.ToList();

            results.Should().HaveCount(5);
        }

        private IMongoCollection<DerivedProduct> GetCollection()
        {
            var collection = GetCollection<DerivedProduct>("test");
            CreateCollection(
                collection,
                new DerivedProduct { Id = 1, Type = "a", Price = 1 },
                new DerivedProduct { Id = 2, Type = "a", Price = 5 },
                new DerivedProduct { Id = 3, Type = "a", Price = 12 },
                new DerivedProduct { Id = 4, Type = "b", Price = 2 },
                new DerivedProduct { Id = 5, Type = "b", Price = 7 });
            return collection;
        }

        private interface IProduct
        {
            string Type { get; set; }
            decimal Price { get; set; }
        }

        private class ProductBase : IProduct
        {
            public int Id { get; set; }
            public string Type { get; set; }
            public decimal Price { get; set; }
        }

        private class DerivedProduct : ProductBase
        {
        }

        private class ProductAggregation
        {
            public string Type { get; set; }
            public decimal MaxPrice { get; set; }
        }
    }
}
