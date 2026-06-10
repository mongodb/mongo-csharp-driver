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
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira;

public class CSharp1775Tests : LinqIntegrationTest<CSharp1775Tests.ClassFixture>
{
    public CSharp1775Tests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void Test1()
    {
        var awardProviderProductCollection = Fixture.AwardProviderProduct;
        var productCollection = Fixture.ProductCollection;

        var querySyntax =
            from p in awardProviderProductCollection.AsQueryable()
            join o in productCollection.AsQueryable() on p.Sku equals o.Sku into joined
            from j in joined.DefaultIfEmpty()
            where p.Reference["AwardChoiceSource"] == "NORC"
            select new AwardProviderProduct()
            {
                Id = p.Id,
                Reference = p.Reference,
                Sku = p.Sku
            };

        var stages = Translate(awardProviderProductCollection, querySyntax);
        string[] expectedStages =
        [
            "{ $project : { _outer : '$$ROOT', _id : 0 } }",
            "{ $lookup : { from : 'product', localField : '_outer.Sku', foreignField : 'Sku', as : '_inner' } }",
            "{ $project : { p : '$_outer', joined : '$_inner', _id : 0 } }",
            "{ $project : { _v : { $map : { input : { $cond : { if : { $eq : [{ $size : '$joined' }, 0] }, then : [null], else : '$joined' } }, as : 'j', in : { '<>h__TransparentIdentifier0' : '$$ROOT', j : '$$j' } } }, _id : 0 } }",
            "{ $unwind : '$_v' }",
            "{ $match : { '_v.<>h__TransparentIdentifier0.p.Reference.AwardChoiceSource' : 'NORC' } }",
            "{ $project : { _id : '$_v.<>h__TransparentIdentifier0.p._id', Reference : '$_v.<>h__TransparentIdentifier0.p.Reference', Sku : '$_v.<>h__TransparentIdentifier0.p.Sku' } }"
        ];
        AssertStages(stages, expectedStages);

        var results = querySyntax.ToList();
        results.Count.Should().Be(2);
        results[0].Id.Should().Be(1);
        results[0].Sku.Should().Be(1);
        results[0].Reference.Values.Should().Equal("NORC");
        results[1].Id.Should().Be(2);
        results[1].Sku.Should().Be(2);
        results[1].Reference.Values.Should().Equal("NORC");

        var methodSyntaxEquivalent = awardProviderProductCollection.AsQueryable()
            .GroupJoin(
                productCollection.AsQueryable(),
                p => p.Sku,
                o => o.Sku,
                (p, joined) => new { p = p, joined = joined })
            .SelectMany(
                TransparentIdentifier0 => TransparentIdentifier0.joined.DefaultIfEmpty(),
                (TransparentIdentifier0, j) => new { TransparentIdentifier0 = TransparentIdentifier0, j = j })
            .Where(
                TransparentIdentifier1 => TransparentIdentifier1.TransparentIdentifier0.p.Reference["AwardChoiceSource"] == "NORC")
            .Select(
                TransparentIdentifier1 => new AwardProviderProduct
                {
                    Id = TransparentIdentifier1.TransparentIdentifier0.p.Id,
                    Reference = TransparentIdentifier1.TransparentIdentifier0.p.Reference,
                    Sku = TransparentIdentifier1.TransparentIdentifier0.p.Sku
                });

        stages = Translate(awardProviderProductCollection, methodSyntaxEquivalent);
        expectedStages = expectedStages.Select(s => s.Replace("<>h__", "")).ToArray();
        AssertStages(stages, expectedStages);

        results = methodSyntaxEquivalent.ToList();

        results = querySyntax.ToList();
        results.Count.Should().Be(2);
        results[0].Id.Should().Be(1);
        results[0].Sku.Should().Be(1);
        results[0].Reference.Values.Should().Equal("NORC");
        results[1].Id.Should().Be(2);
        results[1].Sku.Should().Be(2);
        results[1].Reference.Values.Should().Equal("NORC");
    }

    public class AwardProviderProduct
    {
        public int Id { get; set; }
        public int Sku { get; set; }
        public Dictionary<string, string> Reference { get; set; }
    }

    public class Product
    {
        public int Id { get; set; }
        public int Sku { get; set; }
    }

    public sealed class ClassFixture : MongoDatabaseFixture
    {
        public IMongoCollection<AwardProviderProduct> AwardProviderProduct { get; private set; }
        public IMongoCollection<Product> ProductCollection { get; private set; }

        protected override void InitializeFixture()
        {
            AwardProviderProduct = CreateCollection<AwardProviderProduct>("awardProviderProduct");
            ProductCollection = CreateCollection<Product>("product");

            AwardProviderProduct.InsertMany([
                new AwardProviderProduct { Id = 1, Sku = 1, Reference = new Dictionary<string, string> { { "AwardChoiceSource", "NORC" } } },
                new AwardProviderProduct { Id = 2, Sku = 2, Reference = new Dictionary<string, string> { { "AwardChoiceSource", "NORC" } } },
                new AwardProviderProduct { Id = 3, Sku = 1, Reference = new Dictionary<string, string> { { "AwardChoiceSource", "abcd" } } },
                new AwardProviderProduct { Id = 4, Sku = 2, Reference = new Dictionary<string, string> { { "AwardChoiceSource", "abcd" } } }
            ]);

            ProductCollection.InsertMany([
                new Product { Id = 1, Sku = 1 }
            ]);
        }
    }
}
