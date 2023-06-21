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

using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToPipelineTranslators
{
    public class AsMethodToPipelineTranslatorTests : Linq3IntegrationTest
    {
        [Fact]
        public void As_should_work()
        {
            var collection = GetCollection();

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

        private IMongoCollection<C> GetCollection()
        {
            var collection = GetCollection<C>("test");
            CreateCollection(
                collection,
                new C { Id = 1, Name = "John" },
                new C { Id = 2, Name = "Jane" });
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}
