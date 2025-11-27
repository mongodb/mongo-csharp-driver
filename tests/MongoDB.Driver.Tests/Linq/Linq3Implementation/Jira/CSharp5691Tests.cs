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
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira;

public class CSharp5691Tests : LinqIntegrationTest<CSharp5691Tests.ClassFixture>
{
    public CSharp5691Tests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void DateTime_Now_should_not_be_evaluated_client_side()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => DateTime.Now);

        var exception = Record.Exception(() => Translate(collection, queryable));
        exception.Should().BeOfType<ExpressionNotSupportedException>();
        exception.Message.Should().Contain("non-deterministic field or property 'DateTime.Now' should not be evaluated client-side and is not currently supported server-side");
    }

    [Fact]
    public void DateTime_UtcNow_should_not_be_evaluated_client_side()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => DateTime.UtcNow);

        var exception = Record.Exception(() => Translate(collection, queryable));
        exception.Should().BeOfType<ExpressionNotSupportedException>();
        exception.Message.Should().Contain("non-deterministic field or property 'DateTime.UtcNow' should not be evaluated client-side and is not currently supported server-side");
    }

    [Fact]
    public void DateTime_Today_should_not_be_evaluated_client_side()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => DateTime.Today);

        var exception = Record.Exception(() => Translate(collection, queryable));
        exception.Should().BeOfType<ExpressionNotSupportedException>();
        exception.Message.Should().Contain("non-deterministic field or property 'DateTime.Today' should not be evaluated client-side and is not currently supported server-side");
    }

    [Fact]
    public void DateTimeOffset_Now_should_not_be_evaluated_client_side()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => DateTimeOffset.Now);

        var exception = Record.Exception(() => Translate(collection, queryable));
        exception.Should().BeOfType<ExpressionNotSupportedException>();
        exception.Message.Should().Contain("non-deterministic field or property 'DateTimeOffset.Now' should not be evaluated client-side and is not currently supported server-side");
    }

    [Fact]
    public void DateTimeOffset_UtcNow_should_not_be_evaluated_client_side()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => DateTimeOffset.UtcNow);

        var exception = Record.Exception(() => Translate(collection, queryable));
        exception.Should().BeOfType<ExpressionNotSupportedException>();
        exception.Message.Should().Contain("non-deterministic field or property 'DateTimeOffset.UtcNow' should not be evaluated client-side and is not currently supported server-side");
    }

    [Fact]
    public void Guid_NewGuid_should_not_be_evaluated_client_side()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => Guid.NewGuid());

        var exception = Record.Exception(() => Translate(collection, queryable));
        exception.Should().BeOfType<ExpressionNotSupportedException>();
        exception.Message.Should().Contain("non-deterministic method 'Guid.NewGuid' should not be evaluated client-side and is not currently supported server-side");
    }

    [Fact]
    public void Random_Next_should_not_be_evaluated_client_side()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => new Random().Next());

        var exception = Record.Exception(() => Translate(collection, queryable));
        exception.Message.Should().Contain("non-deterministic method 'Random.Next' should not be evaluated client-side and is not currently supported server-side");
    }

    public class C
    {
        public int Id { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        protected override IEnumerable<C> InitialData =>
        [
            new C { Id = 1 }
        ];
    }
}
