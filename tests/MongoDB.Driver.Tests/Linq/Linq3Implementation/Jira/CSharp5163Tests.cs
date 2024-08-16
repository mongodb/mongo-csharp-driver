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
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira;

public class CSharp5163Tests : Linq3IntegrationTest
{
    [Fact]
    public void Select_muliply_int_long_should_work()
    {
        var collection = GetCollection();

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
        var collection = GetCollection();

        var queryable = collection.AsQueryable()
            .Select(x => x.Byte * (short)256);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $multiply : ['$Byte', 256] }, _id : 0 } }");

        var result = queryable.ToList();
        result[0].Should().Be(256);
    }

    private IMongoCollection<C> GetCollection()
    {
        var collection = GetCollection<C>("test");
        CreateCollection(
            collection,
            new C { Int = 1, Byte = 1});
        return collection;
    }

    private class C
    {
        public int Int { get; set; }
        public byte Byte { get; set; }
    }
}
