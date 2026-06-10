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
using MongoDB.Driver;
using MongoDB.Driver.TestHelpers;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira;

public class CSharp5519Tests : LinqIntegrationTest<CSharp5519Tests.ClassFixture>
{
    public CSharp5519Tests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void Array_constant_Any_should_serialize_array_correctly()
    {
        var collection = Fixture.Collection;
        var array = new[] { E.A, E.B };

        var find = collection.Find(x => array.Any(e => x.E == e));

        var filter = TranslateFindFilter(collection, find);
        filter.Should().Be("{ E : { $in : ['A', 'B'] } }");

        var results = find.ToList();
        results.Select(x => x.Id).Should().Equal(1, 2);
    }

    public class C
    {
        public int Id { get; set; }
        [BsonRepresentation(BsonType.String)] public E E { get; set; }
    }

    public enum E { A, B, C }

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        protected override IEnumerable<C> InitialData =>
        [
            new C { Id = 1, E = E.A },
            new C { Id = 2, E = E.B },
            new C { Id = 3, E = E.C }
        ];
    }
}
