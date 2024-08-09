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
using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Linq.Linq3Implementation;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation
{
    public class MongoQueryProviderTests
    {
        [Fact]
        public void CreateQuery_non_generic_should_return_expected_result()
        {
            var (subject, expression) = CreateSubject();

            var result = subject.CreateQuery(expression);

            result.ElementType.Should().Be(typeof(int));
            result.Expression.Should().BeSameAs(expression);
            result.Provider.Should().BeOfType<MongoQueryProvider<C>>();
        }

        [Fact]
        public void CreateQuery_generic_should_return_expected_result()
        {
            var (subject, expression) = CreateSubject();

            var result = subject.CreateQuery<int>(expression);

            result.ElementType.Should().Be(typeof(int));
            result.Expression.Should().BeSameAs(expression);
            result.Provider.Should().BeOfType<MongoQueryProvider<C>>();
        }

        private (IQueryProvider, Expression) CreateSubject()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase("test");
            var collection = database.GetCollection<C>("test");
            var provider = new MongoQueryProvider<C>(collection, session: null, options: null);
            var queryable = collection.AsQueryable();
            var parameter = Expression.Parameter(typeof(C), "x");
            var expression =
                Expression.Call(
                    QueryableMethod.Select.MakeGenericMethod(typeof(C), typeof(int)),
                    Expression.Constant(queryable, typeof(IQueryable<C>)),
                    Expression.Quote(
                        Expression.Lambda<Func<C, int>>(
                            Expression.Property(parameter, "X"),
                            parameter)));
            return (provider, expression);
        }

        public class C
        {
            public int Id { get; set; }
            public int X { get; set; }
        }
    }

    internal static class MongoQueryProviderExtensions
    {
        public static IMongoCollection<TDocument> _collection<TDocument>(this MongoQueryProvider<TDocument> provider)
            => (IMongoCollection<TDocument>)Reflector.GetFieldValue(provider, nameof(_collection));

        public static AggregateOptions _options(this MongoQueryProvider provider)
            => (AggregateOptions)Reflector.GetFieldValue(provider, nameof(_options));

        public static IClientSessionHandle _session(this MongoQueryProvider provider)
            => (IClientSessionHandle)Reflector.GetFieldValue(provider, nameof(_session));
    }
}
