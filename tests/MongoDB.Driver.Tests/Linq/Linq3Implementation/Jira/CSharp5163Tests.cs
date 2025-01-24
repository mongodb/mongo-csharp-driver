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
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira;

public class CSharp5163Tests : LinqIntegrationTest<CSharp5163Tests.TestDataFixture>
{
    public CSharp5163Tests(ITestOutputHelper testOutputHelper, TestDataFixture fixture)
        : base(testOutputHelper, fixture)
    {
    }

    [Fact]
    public void Select_muliply_int_long_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Int * 36000000000L);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $multiply : ['$Int', NumberLong('36000000000')] }, _id : 0 } }");

        var result = queryable.ToList();
        result[0].Should().Be(36000000000L);
    }

    [Fact]
    public void Select_muliply_byte_short_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Byte * (short)256);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $multiply : ['$Byte', 256] }, _id : 0 } }");

        var result = queryable.ToList();
        result[0].Should().Be(256);
    }

    public class C
    {
        public int Int { get; set; }
        public byte Byte { get; set; }
    }

    public class TestDataFixture : MongoCollectionFixture<C>
    {
        protected override IEnumerable<C> InitialData
            => [ new C { Int = 1, Byte = 1 } ];
    }
}
