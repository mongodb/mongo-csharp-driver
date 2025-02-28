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
using FluentAssertions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.TestHelpers;
using Xunit;

#if NET6_0_OR_GREATER
namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4586Tests : LinqIntegrationTest<CSharp4586Tests.ClassFixture>
    {
        public CSharp4586Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void Project_View1_with_constructor_should_work()
        {
            var collection = Fixture.Collection;
            var id = "a";
            var filter = Builders<Model>.Filter.Eq(m => m.Id, id);

            var find = collection
                .Find(filter)
                .Project(Builders<Model>.Projection.Expression(m => new View1(m.Id)));

            var projection = TranslateFindProjection(collection, find);
            projection.Should().Be("{ _id : 1 }");

            var results = find.ToList();
            results.Should().HaveCount(1);
            results[0].Id.Should().Be("a");

            var deleteResult = collection.DeleteOne(filter);
            deleteResult.DeletedCount.Should().Be(1);
        }

        [Fact]
        public void Project_View1_with_empty_initializer_should_work()
        {
            var collection = Fixture.Collection;
            var id = "a";
            var filter = Builders<Model>.Filter.Eq(m => m.Id, id);

            var find = collection
                .Find(filter)
                .Project(Builders<Model>.Projection.Expression(m => new View1(m.Id) { }));

            var projection = TranslateFindProjection(collection, find);
            projection.Should().Be("{ _id : 1 }");

            var results = find.ToList();
            results.Should().HaveCount(1);
            results[0].Id.Should().Be("a");

            var deleteResult = collection.DeleteOne(filter);
            deleteResult.DeletedCount.Should().Be(1);
        }

        [Fact]
        public void Project_View2_with_constructor_should_work()
        {
            var collection = Fixture.Collection;
            var id = "a";
            var filter = Builders<Model>.Filter.Eq(m => m.Id, id);

            var find = collection
                .Find(filter)
                .Project(Builders<Model>.Projection.Expression(m => new View2(m.Id)));

            var projection = TranslateFindProjection(collection, find);
            projection.Should().Be("{ _id : 1 }");

            var results = find.ToList();
            results.Should().HaveCount(1);
            results[0].Id.Should().Be("a");
            results[0].Version.Should().NotHaveValue();

            var deleteResult = collection.DeleteOne(filter);
            deleteResult.DeletedCount.Should().Be(1);
        }

        [Fact]
        public void Project_View2_with_empty_initializer_should_work()
        {
            var collection = Fixture.Collection;
            var id = "a";
            var filter = Builders<Model>.Filter.Eq(m => m.Id, id);

            var find = collection
                .Find(filter)
                .Project(Builders<Model>.Projection.Expression(m => new View2(m.Id) { }));

            var projection = TranslateFindProjection(collection, find);
            projection.Should().Be("{ _id : 1 }");

            var results = find.ToList();
            results.Should().HaveCount(1);
            results[0].Id.Should().Be("a");
            results[0].Version.Should().NotHaveValue();

            var deleteResult = collection.DeleteOne(filter);
            deleteResult.DeletedCount.Should().Be(1);
        }

        [Fact]
        public void Project_View2_with_initializer_should_work()
        {
            RequireServer.Check().Supports(Feature.FindProjectionExpressions);
            var collection = Fixture.Collection;
            var id = "a";
            var filter = Builders<Model>.Filter.Eq(m => m.Id, id);

            var find = collection
                .Find(filter)
                .Project(Builders<Model>.Projection.Expression(m => new View2(m.Id) { Version = 1 }));

            var projection = TranslateFindProjection(collection, find);
            projection.Should().Be("{ _id : 1, Version : { $literal : 1 } }");

            var results = find.ToList();
            results.Should().HaveCount(1);
            results[0].Id.Should().Be("a");
            results[0].Version.Should().Be(1);

            var deleteResult = collection.DeleteOne(filter);
            deleteResult.DeletedCount.Should().Be(1);
        }

        public class Model
        {
            public Model(string id)
            {
                Id = id;
            }

            public string Id { get; private set; }
        }

        private class View1
        {
            public View1(string id)
            {
                Id = id;
            }

            public string Id { get; }
        }

        private class View2
        {
            public View2(string id)
            {
                Id = id;
            }

            public string Id { get; }
            public int? Version { get; init; } // View1 does not have this property
        }

        public sealed class ClassFixture : MongoCollectionFixture<Model>
        {
            public override bool InitializeDataBeforeEachTestCase => true;
            protected override IEnumerable<Model> InitialData =>
            [
                new Model("a")
            ];
        }
    }
}
#endif
