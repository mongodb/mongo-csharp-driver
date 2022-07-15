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

using System.Linq;
using FluentAssertions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp4057Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Aggregate_Project_should_work()
        {
            var collection = CreateProductsCollection();

            var aggregate = collection.Aggregate()
               .Sort(Builders<Product>.Sort.Ascending(p => p.Id))
               .Project(
                    augmentedProductType => new ProductTypeSearchResult
                    {
                        IsExternalUrl = string.IsNullOrEmpty(augmentedProductType.ShopUrl ?? "")
                    });

            var stages = Translate(collection, aggregate);
            AssertStages(
                stages,
                "{ $sort : { _id : 1 } }",
                "{ $project : { IsExternalUrl : { $in : [{ $ifNull : ['$ShopUrl', ''] }, [null, '']] }, _id : 0 } }");

            var results = aggregate.ToList();
            results.Select(r => r.IsExternalUrl).Should().Equal(true, true, false);
        }

        [Fact]
        public void Queryable_Select()
        {
            var collection = CreateProductsCollection();

            var queryable = collection.AsQueryable()
                .OrderBy(p => p.Id)
                .Select(
                    augmentedProductType => new ProductTypeSearchResult
                    {
                        IsExternalUrl = string.IsNullOrEmpty(augmentedProductType.ShopUrl ?? "")
                    });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $sort : { _id : 1 } }",
                "{ $project : { IsExternalUrl : { $in : [{ $ifNull : ['$ShopUrl', ''] }, [null, '']] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Select(r => r.IsExternalUrl).Should().Equal(true, true, false);
        }

        private IMongoCollection<Product> CreateProductsCollection()
        {
            var collection = GetCollection<Product>();

            var documents = new[]
            {
                new Product { Id = 1, ShopUrl = null },
                new Product { Id = 2, ShopUrl = "" },
                new Product { Id = 3, ShopUrl = "abc" }
            };
            CreateCollection(collection, documents);

            return collection;
        }

        private class Product
        {
            public int Id { get; set; }
            public string ShopUrl { get; set; }
        }

        private class ProductTypeSearchResult
        {
            public bool IsExternalUrl { get; set; }
        }
    }
}
