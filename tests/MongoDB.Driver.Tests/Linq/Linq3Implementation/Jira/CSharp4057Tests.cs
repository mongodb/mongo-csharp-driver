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
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4057Tests : LinqIntegrationTest<CSharp4057Tests.ClassFixture>
    {
        public CSharp4057Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void Aggregate_Project_should_work()
        {
            var collection = Fixture.Collection;

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
            var collection = Fixture.Collection;

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

        public class Product
        {
            public int Id { get; set; }
            public string ShopUrl { get; set; }
        }

        private class ProductTypeSearchResult
        {
            public bool IsExternalUrl { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<Product>
        {
            protected override IEnumerable<Product> InitialData =>
            [
                new Product { Id = 1, ShopUrl = null },
                new Product { Id = 2, ShopUrl = "" },
                new Product { Id = 3, ShopUrl = "abc" }
            ];
        }
    }
}
