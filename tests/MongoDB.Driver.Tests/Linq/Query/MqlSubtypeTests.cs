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
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver.TestHelpers;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Query;

public class MqlSubtypeTests : LinqIntegrationTest<MqlSubtypeTests.ClassFixture>
{
    public MqlSubtypeTests(ClassFixture fixture)
        : base(fixture, server => server.Supports(Feature.SubtypeOperator))
    {
    }

    [Theory]
    [InlineData(BsonBinarySubType.Binary, 1)]
    [InlineData(BsonBinarySubType.Vector, 2)]
    [InlineData(BsonBinarySubType.UuidStandard, 3)]
    public void MqlSubtype_in_where(BsonBinarySubType subtype, int expectedId)
    {
        var collection = Fixture.Collection;
        var queryable = collection.AsQueryable()
            .Where(d => Mql.Subtype(d.Data) == subtype);

        var renderedStages = Translate(collection, queryable);
        AssertStages(renderedStages, $"{{ '$match' : {{ '$expr' : {{ '$eq' : [{{ '$subtype' : '$Data' }}, {(int)subtype}] }} }} }}");

        var result = queryable.Single();
        result.Id.Should().Be(expectedId);
    }

    [Fact]
    public void MqlSubtype_in_select()
    {
        var collection = Fixture.Collection;
        var queryable = collection.AsQueryable()
            .Select(d => Mql.Subtype(d.Data));

        var renderedStages = Translate(collection, queryable);
        AssertStages(renderedStages, "{ '$project' : { '_v' : { '$subtype' : '$Data' }, '_id' : 0 } }");

        var result = queryable.ToList();
        result.Should().BeEquivalentTo([BsonBinarySubType.Binary, BsonBinarySubType.Vector, BsonBinarySubType.UuidStandard]);
    }

    public class C
    {
        public int Id { get; set; }
        public BsonBinaryData Data { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        protected override IEnumerable<C> InitialData =>
        [
            new() { Id = 1, Data = new BsonBinaryData([0x01, 0x02]) },
            new() { Id = 2, Data = new BsonBinaryData([0x03, 0x04], BsonBinarySubType.Vector) },
            new() { Id = 3, Data = new BsonBinaryData(Guid.Parse("E4A10FB8-7A83-494C-9710-29BBFFB1C262"), GuidRepresentation.Standard) },
        ];
    }
}
