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
using System.Linq;
using FluentAssertions;
using MongoDB.Driver.Linq;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq;

public class IQueryableExtensionsTests
{
    [Fact]
    public void GetClient_should_return_client_for_collection_queryable()
    {
        var client = CreateClient();
        var queryable = CreateCollection<C>(client).AsQueryable();

        var result = queryable.GetClient();

        result.Should().BeSameAs(client);
    }

    [Fact]
    public void GetClient_should_return_client_for_projected_queryable()
    {
        var client = CreateClient();
        var queryable = CreateCollection<C>(client).AsQueryable().Select(c => c.X);

        var result = queryable.GetClient();

        result.Should().BeSameAs(client);
    }

    [Fact]
    public void GetClient_should_return_client_for_group_joined_queryable()
    {
        var client = CreateClient();
        var queryable = CreateCollection<C>(client).AsQueryable()
            .GroupJoin(
                CreateCollection<D>(client).AsQueryable(),
                c => c.Id,
                d => d.CId,
                (c, ds) => new { C = c, Ds = ds });
        queryable.ElementType.Should().NotBe(typeof(C));

        var result = queryable.GetClient();

        result.Should().BeSameAs(client);
    }

    [Fact]
    public void GetClient_should_return_client_for_database_queryable()
    {
        var client = CreateClient();
        var queryable = CreateDatabase(client).AsQueryable();

        var result = queryable.GetClient();

        result.Should().BeSameAs(client);
    }

    [Fact]
    public void GetClient_should_throw_when_source_is_not_a_MongoDB_queryable()
    {
        var queryable = new[] { 1, 2, 3 }.AsQueryable();

        var exception = Record.Exception(() => queryable.GetClient());

        exception.Should().BeOfType<ArgumentException>()
            .Subject.ParamName.Should().Be("source");
    }

    [Fact]
    public void GetClient_should_throw_when_source_is_null()
    {
        IQueryable source = null;

        var exception = Record.Exception(() => source.GetClient());

        exception.Should().BeOfType<ArgumentNullException>()
            .Subject.ParamName.Should().Be("source");
    }

    private static IMongoClient CreateClient() => Mock.Of<IMongoClient>();

    private static IMongoDatabase CreateDatabase(IMongoClient client)
    {
        var database = Mock.Of<IMongoDatabase>();
        Mock.Get(database).SetupGet(d => d.Client).Returns(client);
        Mock.Get(database).SetupGet(d => d.Settings).Returns(new MongoDatabaseSettings());
        return database;
    }

    private static IMongoCollection<TDocument> CreateCollection<TDocument>(IMongoClient client)
    {
        var collection = Mock.Of<IMongoCollection<TDocument>>();
        Mock.Get(collection).SetupGet(c => c.Database).Returns(CreateDatabase(client));
        Mock.Get(collection).SetupGet(c => c.Settings).Returns(new MongoCollectionSettings());
        return collection;
    }

    // nested types
    public class C
    {
        public int Id { get; set; }
        public int X { get; set; }
    }

    public class D
    {
        public int Id { get; set; }
        public int CId { get; set; }
    }
}
