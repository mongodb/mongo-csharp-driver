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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Linq;
using MongoDB.Driver.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4993Tests : IntegrationTest<CSharp4993Tests.CollectionFixture>
    {
        public CSharp4993Tests(ITestOutputHelper testOutputHelper, CollectionFixture fixture)
            : base(testOutputHelper, fixture)
        {
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_decimal_divide_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = Fixture.GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.D / 1.3M);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { $divide : ['$D', NumberDecimal('1.3')] }, _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $divide : ['$D', NumberDecimal('1.3')] }, _id : 0 } }");
            }

            var result = queryable.First();
            result.Should().Be(769.23076923076923076923076923M);
        }

        public class C
        {
            public int Id { get; set; }
            [BsonRepresentation(BsonType.Decimal128)] public decimal D { get; set; }
        }

        public class CollectionFixture : TemporaryCollectionFixture<C>
        {
            protected override IEnumerable<C> GetInitialData()
                => new[]
                {
                    new C { Id = 1, D = 1000.0M }
                };
        }
    }
}
