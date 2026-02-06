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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using MongoDB.Driver.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4813Tests : LinqIntegrationTest<CSharp4813Tests.ClassFixture>
    {
        public CSharp4813Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void Where_BitArray_Count_should_throw()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Where(x => x.BitArray.Count == 1);

            var exception = Record.Exception(() => Translate(collection, queryable));
            exception.Should().BeOfType<ExpressionNotSupportedException>();
        }

        [Fact]
        public void Where_Count_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Where(x => x.Count == 1);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { Count : 1 } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Fact]
        public void Where_List_Count_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Where(x => x.List.Count == 1);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'List' : { $size : 1 } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Fact]
        public void Where_ListInterface_Count_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Where(x => x.ListInterface.Count == 1);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'ListInterface' : { $size : 1 } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_BitArray_Count_should_throw(
            [Values(false, true)] bool enableClientSideProjections)
        {
            RequireServer.Check().Supports(Feature.FindProjectionExpressions);
            var collection = Fixture.Collection;
            var translationOptions = new ExpressionTranslationOptions { EnableClientSideProjections = enableClientSideProjections };

            var queryable = collection.AsQueryable(translationOptions)
                .Select(x => x.BitArray.Count);

            if (enableClientSideProjections)
            {
                var stages = Translate(collection, queryable, out var outputSerializer);
                AssertStages(stages, "{ $project : { _snippets : ['$BitArray'], _id : 0 } }");
                outputSerializer.Should().BeAssignableTo<IClientSideProjectionDeserializer>();

                var results = queryable.ToList();
                results.Should().Equal(1, 2);
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("is not represented as an array");
            }
        }

        [Fact]
        public void Select_Count_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Select(x => x.Count);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : '$Count', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1, 2);
        }

        [Fact]
        public void Select_List_Count_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Select(x => x.List.Count);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $size : '$List' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1, 2);
        }

        [Fact]
        public void Select_ListInterface_Count_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Select(x => x.ListInterface.Count);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $size : '$ListInterface' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1, 2);
        }

        public class C
        {
            public int Id { get; set; }
            public BitArray BitArray { get; set; }
            public int Count { get; set; }
            public List<int> List { get; set; }
            public IList<int> ListInterface { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<C>
        {
            protected override IEnumerable<C> InitialData =>
            [
                new C
                {
                    Id = 1,
                    BitArray = new BitArray(length: 1),
                    Count = 1,
                    List = new() { 1 },
                    ListInterface = new List<int>() { 1 }
                },
                new C
                {
                    Id = 2,
                    BitArray = new BitArray(length: 2),
                    Count = 2,
                    List = new() { 1, 2 },
                    ListInterface = new List<int> { 1, 2 }
                }
            ];
        }
    }
}
