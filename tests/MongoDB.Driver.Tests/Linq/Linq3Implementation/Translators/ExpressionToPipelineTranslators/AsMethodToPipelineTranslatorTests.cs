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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToPipelineTranslators
{
    public class AsMethodToPipelineTranslatorTests : LinqIntegrationTest<AsMethodToPipelineTranslatorTests.ClassFixture>
    {
        public AsMethodToPipelineTranslatorTests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void As_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Where(x => x.Name == "John")
                .As(BsonDocumentSerializer.Instance);

            var stages = Translate(collection, queryable, out var outputSerializer);
            AssertStages(
                stages,
                "{ $match : { Name : 'John' } }");
            outputSerializer.Should().BeSameAs(BsonDocumentSerializer.Instance);

            var result = queryable.Single();
            result.Should().BeOfType<BsonDocument>();
            result.Should().Be("{ _id : 1, Name : 'John' }");
        }

        public class C
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<C>
        {
            protected override IEnumerable<C> InitialData =>
            [
                new C { Id = 1, Name = "John" },
                new C { Id = 2, Name = "Jane" }
            ];
        }
    }
}
