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
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharpxxxxTests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public void Find_with_implicit_client_side_projection_should_throw_when_using_LINQ3(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var find = collection
                .Find(x => x.Id == 1)
                .Project(x => new { R = MyFunction(x.X) }); // MyFunction cannot be translated to MQL

            if (linqProvider == LinqProvider.V2)
            {
                var translatedProjection = TranslateFindProjection(collection, find);
                translatedProjection.Should().Be("{ X : 1, _id : 0 }");

                var result = find.Single();
                result.R.Should().Be(2);
            }
            else
            {
                var exception = Record.Exception(() => TranslateFindProjection(collection, find));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Find_with_explicit_client_side_projection_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var find = collection
                .Find(x => x.Id == 1)
                .ToEnumerable() // execute the rest of this chain client side
                .Select(x => new { R = MyFunction(x.X) }); // MyFunction will be executed client side

            var result = find.Single();
            result.R.Should().Be(2);
        }

        [Theory]
        [ParameterAttributeData]
        public void Aggregate_with_implicit_client_side_projection_should_throw(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var aggregate = collection.Aggregate()
                .Match(x => x.Id == 1)
                .Project(x => new { R = MyFunction(x.X) }); // MyFunction cannot be translated to MQL

            var exception = Record.Exception(() => Translate(collection, aggregate));
            if (linqProvider == LinqProvider.V2)
            {
                exception.Should().BeOfType<NotSupportedException>();
            }
            else
            {
                exception.Should().BeOfType<ExpressionNotSupportedException>();
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Aggregate_with_explicit_client_side_projection_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var aggregate = collection.Aggregate()
                .Match(x => x.Id == 1)
                .ToEnumerable() // execute the rest of this chain client side
                .Select(x => new { R = MyFunction(x.X) }); // MyFunction will be executed client side

            var result = aggregate.Single();
            result.R.Should().Be(2);
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_with_implicit_client_side_projection_should_throw(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Where(x => x.Id == 1)
                .Select(x => new { R = MyFunction(x.X) }); // MyFunction cannot be translated to MQL

            var exception = Record.Exception(() => Translate(collection, queryable));
            if (linqProvider == LinqProvider.V2)
            {
                exception.Should().BeOfType<NotSupportedException>();
            }
            else
            {
                exception.Should().BeOfType<ExpressionNotSupportedException>();
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_with_explicit_client_side_projection_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var enumerable = collection.AsQueryable()
                .Where(x => x.Id == 1)
                .AsEnumerable() // execute the rest of this chain client side
                .Select(x => new { R = MyFunction(x.X) }); // MyFunction will be executed client side

            var result = enumerable.Single();
            result.R.Should().Be(2);
        }

        private static int MyFunction(int x) => x + 1;

        private IMongoCollection<C> GetCollection(LinqProvider linqProvider)
        {
            var collection = GetCollection<C>("test", linqProvider);
            CreateCollection(
                collection,
                new C { Id = 1, X = 1 });
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public int X { get; set; }
        }
    }
}
