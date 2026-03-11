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
using MongoDB.Driver.TestHelpers;
using FluentAssertions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira;

public class CSharp4566Tests : LinqIntegrationTest<CSharp4566Tests.ClassFixture>
{
    public CSharp4566Tests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void Find_NullableChar_equals_char_should_work()
    {
        var collection = Fixture.Collection;

        var find = collection.Find(x => x.NullableChar == 'a');

        var filter = TranslateFindFilter(collection, find);
        filter.Should().Be("""{ NullableChar : 97 }""");

        var result = find.Single();
        result.Id.Should().Be(1);
    }

    [Fact]
    public void Find_NullableChar_equals_char_null_should_work()
    {
        var collection = Fixture.Collection;

        var find = collection.Find(x => x.NullableChar == null);

        var filter = TranslateFindFilter(collection, find);
        filter.Should().Be("""{ NullableChar : null }""");

        var result = find.Single();
        result.Id.Should().Be(3);
    }

    [Fact]
    public void Find_NullableLong_equals_double_should_work()
    {
        var collection = Fixture.Collection;

        var find = collection.Find(x => x.NullableLong == 1.0);

        var filter = TranslateFindFilter(collection, find);
        filter.Should().Be("""{ NullableLong : 1.0 }""");

        var result = find.Single();
        result.Id.Should().Be(1);
    }

    [Fact]
    public void Find_NullableLong_equals_double_null_should_work()
    {
        var collection = Fixture.Collection;

        var find = collection.Find(x => x.NullableLong == null);

        var filter = TranslateFindFilter(collection, find);
        filter.Should().Be("""{ NullableLong : null }""");

        var result = find.Single();
        result.Id.Should().Be(3);
    }

    [Fact]
    public void Find_NullableSByte_equals_int_should_work()
    {
        var collection = Fixture.Collection;

        var find = collection.Find(x => x.NullableSByte == 1);

        var filter = TranslateFindFilter(collection, find);
        filter.Should().Be("""{ NullableSByte : 1 }""");

        var result = find.Single();
        result.Id.Should().Be(1);
    }

    [Fact]
    public void Find_NullableSByte_equals_int_null_should_work()
    {
        var collection = Fixture.Collection;

        var find = collection.Find(x => x.NullableSByte == null);

        var filter = TranslateFindFilter(collection, find);
        filter.Should().Be("""{ NullableSByte : null }""");

        var result = find.Single();
        result.Id.Should().Be(3);
    }

    public class C
    {
        public int Id  { get; set; }
        public char? NullableChar  { get; set; }
        public long? NullableLong  { get; set; }
        public sbyte? NullableSByte  { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        protected override IEnumerable<C> InitialData =>
        [
            new C { Id = 1, NullableChar = 'a', NullableLong = 1, NullableSByte = 1 },
            new C { Id = 2, NullableChar = 'b', NullableLong = 2, NullableSByte = 2 },
            new C { Id = 3, NullableChar = null, NullableLong = null, NullableSByte = null }
        ];
    }
}
