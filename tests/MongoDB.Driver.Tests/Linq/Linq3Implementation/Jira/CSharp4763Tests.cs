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
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4763Tests : Linq3IntegrationTest
    {
        [Theory]
        [InlineData(false, false, null)]
        [InlineData(true, false, null)]
        [InlineData(true, true, null)]
        [InlineData(true, true, false)]
        [InlineData(true, true, true)]
        public void Find_with_client_side_projection_ToList_should_work(
            bool useFindOptions,
            bool useTranslationOptions,
            bool? enableClientSideProjections)
        {
            var collection = GetCollection();
            var findOptions = GetFindOptions(useFindOptions, useTranslationOptions, enableClientSideProjections);

            var find = collection
                .Find("{}", findOptions)
                .Project(x => MyFunction(x.X));

            if (enableClientSideProjections ?? false)
            {
                var projection = TranslateFindProjection(collection, find, out var projectionSerializer);
                projection.Should().BeNull();
                projectionSerializer.Should().BeAssignableTo<IClientSideProjectionDeserializer>();

                var results = find.ToList();
                results.Should().Equal(2);
            }
            else
            {
                var exception = Record.Exception(() => TranslateFindProjection(collection, find));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("MyFunction");
            }
        }

        [Theory]
        [InlineData(false, false, null)]
        [InlineData(true, false, null)]
        [InlineData(true, true, null)]
        [InlineData(true, true, false)]
        [InlineData(true, true, true)]
        public void Find_with_client_side_projection_First_should_work(
            bool useFindOptions,
            bool useTranslationOptions,
            bool? enableClientSideProjections)
        {
            var collection = GetCollection();
            var translationOptions = new ExpressionTranslationOptions { EnableClientSideProjections = enableClientSideProjections };
            var findOptions = new FindOptions { TranslationOptions = translationOptions };

            var find = collection
                .Find("{}", findOptions)
                .Project(x => MyFunction(x.X));

            if (enableClientSideProjections ?? false)
            {
                var projection = TranslateFindProjection(collection, find, out var projectionSerializer);
                projection.Should().BeNull();
                projectionSerializer.Should().BeAssignableTo<IClientSideProjectionDeserializer>();

                var result = find.First();
                result.Should().Be(2);
            }
            else
            {
                var exception = Record.Exception(() => TranslateFindProjection(collection, find));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("MyFunction");
            }
        }

        [Theory]
        [InlineData(false, false, null)]
        [InlineData(true, false, null)]
        [InlineData(true, true, null)]
        [InlineData(true, true, false)]
        [InlineData(true, true, true)]
        public void Find_with_client_side_projection_Single_should_work(
            bool useFindOptions,
            bool useTranslationOptions,
            bool? enableClientSideProjections)
        {
            var collection = GetCollection();
            var translationOptions = new ExpressionTranslationOptions { EnableClientSideProjections = enableClientSideProjections };
            var findOptions = new FindOptions { TranslationOptions = translationOptions };

            var find = collection
                .Find("{}", findOptions)
                .Project(x => MyFunction(x.X));

            if (enableClientSideProjections ?? false)
            {
                var projection = TranslateFindProjection(collection, find, out var projectionSerializer);
                projection.Should().BeNull();
                projectionSerializer.Should().BeAssignableTo<IClientSideProjectionDeserializer>();

                var result = find.Single();
                result.Should().Be(2);
            }
            else
            {
                var exception = Record.Exception(() => TranslateFindProjection(collection, find));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("MyFunction");
            }
        }

        [Theory]
        [InlineData(false, false, null)]
        [InlineData(true, false, null)]
        [InlineData(true, true, null)]
        [InlineData(true, true, false)]
        [InlineData(true, true, true)]
        public void Aggregate_Project_with_client_side_projection_ToList_should_work(
            bool useAggregateOptions,
            bool useTranslationOptions,
            bool? enableClientSideProjections)
        {
            var collection = GetCollection();
            var aggregateOptions = GetAggregateOptions(useAggregateOptions, useTranslationOptions, enableClientSideProjections);

            var aggregate = collection.Aggregate(aggregateOptions)
                .Project(x => MyFunction(x.X));

            if (enableClientSideProjections ?? false)
            {
                var stages = Translate(collection, aggregate, out var outputSerializer);
                AssertStages(stages, Array.Empty<string>());
                outputSerializer.Should().BeAssignableTo<IClientSideProjectionDeserializer>();

                var results = aggregate.ToList();
                results.Should().Equal(2);
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection, aggregate));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("MyFunction");
            }
        }

        [Theory]
        [InlineData(false, false, null)]
        [InlineData(true, false, null)]
        [InlineData(true, true, null)]
        [InlineData(true, true, false)]
        [InlineData(true, true, true)]
        public void Aggregate_Project_with_client_side_projection_First_should_work(
            bool useAggregateOptions,
            bool useTranslationOptions,
            bool? enableClientSideProjections)
        {
            var collection = GetCollection();
            var aggregateOptions = GetAggregateOptions(useAggregateOptions, useTranslationOptions, enableClientSideProjections);

            var aggregate = collection.Aggregate(aggregateOptions)
                .Project(x => MyFunction(x.X));

            if (enableClientSideProjections ?? false)
            {
                var stages = Translate(collection, aggregate, out var serializer);
                AssertStages(stages, Array.Empty<string>());
                serializer.Should().BeAssignableTo<IClientSideProjectionDeserializer>();

                var result = aggregate.First();
                result.Should().Be(2);
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection, aggregate));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("MyFunction");
            }
        }

        [Theory]
        [InlineData(false, false, null)]
        [InlineData(true, false, null)]
        [InlineData(true, true, null)]
        [InlineData(true, true, false)]
        [InlineData(true, true, true)]
        public void Aggregate_Project_with_client_side_projection_Match_should_throw(
            bool useAggregateOptions,
            bool useTranslationOptions,
            bool? enableClientSideProjections)
        {
            var collection = GetCollection();
            var aggregateOptions = GetAggregateOptions(useAggregateOptions, useTranslationOptions, enableClientSideProjections);

            var aggregate = collection.Aggregate(aggregateOptions)
                .Project(x => MyFunction(x.X))
                .Match(x => x == 2);

            var exception = Record.Exception(() => Translate(collection, aggregate));
            if (enableClientSideProjections ?? false)
            {
                exception.Should().BeOfType<NotSupportedException>();
                exception.Message.Should().Contain("A $match stage cannot follow a client side projection");
            }
            else
            {
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("MyFunction");
            }
        }

        [Theory]
        [InlineData(false, false, null)]
        [InlineData(true, false, null)]
        [InlineData(true, true, null)]
        [InlineData(true, true, false)]
        [InlineData(true, true, true)]
        public void Queryable_Select_with_client_side_projection_ToList_should_work(
            bool useAggregateOptions,
            bool useTranslationOptions,
            bool? enableClientSideProjections)
        {
            var collection = GetCollection();
            var aggregateOptions = GetAggregateOptions(useAggregateOptions, useTranslationOptions, enableClientSideProjections);

            var queryable = collection.AsQueryable(aggregateOptions)
                .Select(x => MyFunction(x.X));

            if (enableClientSideProjections ?? false)
            {
                var stages = Translate(collection, queryable, out var outputSerializer);
                AssertStages(stages, Array.Empty<string>());
                outputSerializer.Should().BeAssignableTo<IClientSideProjectionDeserializer>();

                var results = queryable.ToList();
                results.Should().Equal(2);
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("MyFunction");
            }
        }

        [Theory]
        [InlineData(false, false, null)]
        [InlineData(true, false, null)]
        [InlineData(true, true, null)]
        [InlineData(true, true, false)]
        [InlineData(true, true, true)]
        public void Queryable_Select_with_client_side_projection_First_should_work(
            bool useAggregateOptions,
            bool useTranslationOptions,
            bool? enableClientSideProjections)
        {
            var collection = GetCollection();
            var aggregateOptions = GetAggregateOptions(useAggregateOptions, useTranslationOptions, enableClientSideProjections);

            var queryable = collection.AsQueryable(aggregateOptions)
                .Select(x => MyFunction(x.X));

            if (enableClientSideProjections ?? false)
            {
                var stages = Translate(collection, queryable, out var outputSerializer);
                AssertStages(stages, Array.Empty<string>());
                outputSerializer.Should().BeAssignableTo<IClientSideProjectionDeserializer>();

                var result = queryable.First();
                result.Should().Be(2);
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("MyFunction");
            }
        }

        [Theory]
        [InlineData(false, false, null)]
        [InlineData(true, false, null)]
        [InlineData(true, true, null)]
        [InlineData(true, true, false)]
        [InlineData(true, true, true)]
        public void Queryable_Select_with_client_side_projection_First_with_predicate_should_throw(
            bool useAggregateOptions,
            bool useTranslationOptions,
            bool? enableClientSideProjections)
        {
            var collection = GetCollection();
            var aggregateOptions = GetAggregateOptions(useAggregateOptions, useTranslationOptions, enableClientSideProjections);

            var queryable = collection.AsQueryable(aggregateOptions)
                .Select(x => MyFunction(x.X));

            var exception = Record.Exception(() => queryable.First(x => x == 2));
            if (enableClientSideProjections ?? false)
            {
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("because First with a predicate cannot follow a client side projection");
            }
            else
            {
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("MyFunction");
            }
        }

        [Theory]
        [InlineData(false, false, null)]
        [InlineData(true, false, null)]
        [InlineData(true, true, null)]
        [InlineData(true, true, false)]
        [InlineData(true, true, true)]
        public void Queryable_Select_with_client_side_projection_Single_should_work(
            bool useAggregateOptions,
            bool useTranslationOptions,
            bool? enableClientSideProjections)
        {
            var collection = GetCollection();
            var aggregateOptions = GetAggregateOptions(useAggregateOptions, useTranslationOptions, enableClientSideProjections);

            var queryable = collection.AsQueryable(aggregateOptions)
                .Select(x => MyFunction(x.X));

            if (enableClientSideProjections ?? false)
            {
                var stages = Translate(collection, queryable, out var outputSerializer);
                AssertStages(stages, Array.Empty<string>());
                outputSerializer.Should().BeAssignableTo<IClientSideProjectionDeserializer>();

                var result = queryable.Single();
                result.Should().Be(2);
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("MyFunction");
            }
        }

        [Theory]
        [InlineData(false, false, null)]
        [InlineData(true, false, null)]
        [InlineData(true, true, null)]
        [InlineData(true, true, false)]
        [InlineData(true, true, true)]
        public void Queryable_Select_with_client_side_projection_Single_with_predicate_should_throw(
            bool useAggregateOptions,
            bool useTranslationOptions,
            bool? enableClientSideProjections)
        {
            var collection = GetCollection();
            var aggregateOptions = GetAggregateOptions(useAggregateOptions, useTranslationOptions, enableClientSideProjections);

            var queryable = collection.AsQueryable(aggregateOptions)
                .Select(x => MyFunction(x.X));

            var exception = Record.Exception(() => queryable.Single(x => x == 2));
            if (enableClientSideProjections ?? false)
            {
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("because Single with a predicate cannot follow a client side projection");
            }
            else
            {
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("MyFunction");
            }
        }

        [Theory]
        [InlineData(false, false, null)]
        [InlineData(true, false, null)]
        [InlineData(true, true, null)]
        [InlineData(true, true, false)]
        [InlineData(true, true, true)]
        public void Queryable_Select_with_client_side_projection_Where_should_throw(
            bool useAggregateOptions,
            bool useTranslationOptions,
            bool? enableClientSideProjections)
        {
            var collection = GetCollection();
            var aggregateOptions = GetAggregateOptions(useAggregateOptions, useTranslationOptions, enableClientSideProjections);

            var queryable = collection.AsQueryable(aggregateOptions)
                .Select(x => MyFunction(x.X))
                .Where(x => x == 2);

            var exception = Record.Exception(() => queryable.ToList());
            if (enableClientSideProjections ?? false)
            {
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("because Where cannot follow a client side projection");
            }
            else
            {
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("MyFunction");
            }
        }

        [Theory]
        [InlineData(false, false, null)]
        [InlineData(true, false, null)]
        [InlineData(true, true, null)]
        [InlineData(true, true, false)]
        [InlineData(true, true, true)]
        public void Queryable_Select_with_client_side_projection_Select_should_throw(
            bool useAggregateOptions,
            bool useTranslationOptions,
            bool? enableClientSideProjections)
        {
            var collection = GetCollection();
            var aggregateOptions = GetAggregateOptions(useAggregateOptions, useTranslationOptions, enableClientSideProjections);

            var queryable = collection.AsQueryable(aggregateOptions)
                .Select(x => MyFunction(x.X))
                .Select(x => x + 1);

            var exception = Record.Exception(() => queryable.ToList());
            if (enableClientSideProjections ?? false)
            {
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("because Select cannot follow a client side projection");
            }
            else
            {
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("MyFunction");
            }
        }

        private AggregateOptions GetAggregateOptions(bool useFindOptions, bool useTranslationOptions, bool? enableClientSideProjections) =>
            (useFindOptions, useTranslationOptions) switch
            {
                (false, _) => null,
                (true, false) => new AggregateOptions { TranslationOptions = null },
                (true, true) => new AggregateOptions { TranslationOptions = new ExpressionTranslationOptions { EnableClientSideProjections = enableClientSideProjections } },
            };

        private IMongoCollection<C> GetCollection()
        {
            var collection = GetCollection<C>("test");
            CreateCollection(
                collection,
                new C { Id = 1, X = 1 });
            return collection;
        }

        private FindOptions GetFindOptions(bool useFindOptions, bool useTranslationOptions, bool? enableClientSideProjections) =>
            (useFindOptions, useTranslationOptions) switch
            {
                (false, _) => null,
                (true, false) => new FindOptions { TranslationOptions = null },
                (true, true) => new FindOptions { TranslationOptions = new ExpressionTranslationOptions { EnableClientSideProjections = enableClientSideProjections } },
            };

        private int MyFunction(int x) => 2 * x;

        private class C
        {
            public int Id { get; set; }
            public int X { get; set; }
        }
    }
}
