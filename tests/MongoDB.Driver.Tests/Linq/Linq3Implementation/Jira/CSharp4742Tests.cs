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
using MongoDB.Bson.Serialization;
using MongoDB.Driver.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4742Tests : LinqIntegrationTest<CSharp4742Tests.ClassFixture>
    {
        public CSharp4742Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void Find_with_identity_projection_should_work()
        {
            var collection = Fixture.Collection;
            var projection = Builders<C>.Projection.Expression(x => x);

            var find = collection
                .Find("{}")
                .Project(projection);

            var renderedProjection = TranslateFindProjection(collection, find);
            renderedProjection.Should().BeNull();

            var result = find.Single();
            result.Id.Should().Be(1);
            result.X.Should().Be(2);
        }

        [Fact]
        public void Aggregate_Project_with_identity_projection_should_work()
        {
            var collection = Fixture.Collection;
            var projection = Builders<C>.Projection.Expression(x => x);

            var aggregate = collection
                .Aggregate()
                .Project(projection);

            var stages = Translate(collection, aggregate);
            stages.Should().BeEmpty();

            var result = aggregate.Single();
            result.Id.Should().Be(1);
            result.X.Should().Be(2);
        }

        [Theory]
        [ParameterAttributeData]
        public void ExpressionProjectionDefinition_with_identity_projection_Render_should_work(
            [Values(true, false)] bool renderForFind)
        {
            var collection = Fixture.Collection;
            var projection = new ExpressionProjectionDefinition<C, C>(x => x);
            var sourceSerializer = collection.DocumentSerializer;
            var serializerRegistry = BsonSerializer.SerializerRegistry;

            var renderedProjection = projection.Render(new(sourceSerializer, serializerRegistry, renderForFind: renderForFind));

            renderedProjection.Document.Should().BeNull();
            renderedProjection.ProjectionSerializer.Should().BeSameAs(sourceSerializer);
        }

        [Theory]
        [ParameterAttributeData]
        public void FindExpressionProjectionDefinition_with_identity_projection_Render_should_work(
            [Values(true, false)] bool renderForFind)
        {
            var collection = Fixture.Collection;
            var projection = new FindExpressionProjectionDefinition<C, C>(x => x);
            var sourceSerializer = collection.DocumentSerializer;
            var serializerRegistry = BsonSerializer.SerializerRegistry;

            var renderedProjection = projection.Render(new(sourceSerializer, serializerRegistry, renderForFind: renderForFind));

            renderedProjection.Document.Should().BeNull();
            renderedProjection.ProjectionSerializer.Should().BeSameAs(sourceSerializer);
        }

        public class C
        {
            public int Id { get; set; }
            public int X { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<C>
        {
            protected override IEnumerable<C> InitialData =>
            [
                new C { Id = 1, X = 2 }
            ];
        }
    }
}
