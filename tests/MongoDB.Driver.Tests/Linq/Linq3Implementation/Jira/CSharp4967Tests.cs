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
using MongoDB.Driver.TestHelpers;
using FluentAssertions;
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira;

public class CSharp4967Tests : LinqIntegrationTest<CSharp4967Tests.ClassFixture>
{
    public CSharp4967Tests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void Set_Nested_should_work()
    {
        var collection = Fixture.Collection;
        var update = Builders<MyDocument>.Update
            .Pipeline(new EmptyPipelineDefinition<MyDocument>()
                .Set(c => new MyDocument
                {
                    Nested = new MyNestedDocument
                    {
                        ValueCopy = c.Value,
                    },
                }));

        var renderedUpdate = update.Render(new(collection.DocumentSerializer, BsonSerializer.SerializerRegistry)).AsBsonArray;
        renderedUpdate.Count.Should().Be(1);
        renderedUpdate[0].Should().Be("{ $set : { Nested : { ValueCopy : '$Value' } } }");

        collection.UpdateMany("{ }", update);

        var updatedDocument = collection.FindSync("{}").Single();
        updatedDocument.Nested.ValueCopy.Should().Be("Value");
    }

    public class MyDocument
    {
        public int Id { get; set; }
        public string Value { get; set; }
        public string AnotherValue { get; set; }
        public MyNestedDocument Nested { get; set; }
    }

    public class MyNestedDocument
    {
        public string ValueCopy { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<MyDocument>
    {
        protected override IEnumerable<MyDocument> InitialData =>
        [
            new MyDocument { Id = 1, Value = "Value" }
        ];
    }
}
