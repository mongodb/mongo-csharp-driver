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
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4681Tests : LinqIntegrationTest<CSharp4681Tests.ClassFixture>
    {
        public CSharp4681Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void Find_projection_render_should_work()
        {
            var collection = Fixture.Collection;

            var fluentFind = collection.Find(a => a.Id == "1").Project(a => a.Id);

            var documentSerializer = collection.DocumentSerializer;
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var renderedProjection = fluentFind.Options.Projection.Render(new(documentSerializer, serializerRegistry));

            var result = fluentFind.Single();
            result.Should().Be("1");
        }

        public class A
        {
            public string Id { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<A>
        {
            protected override IEnumerable<A> InitialData =>
            [
                new A { Id = "1" },
                new A { Id = "2" }
            ];
        }
    }
}
