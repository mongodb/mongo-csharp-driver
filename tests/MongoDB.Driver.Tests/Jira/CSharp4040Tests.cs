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

using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Driver.Tests.Jira
{
    public class CSharp4040Tests
    {
        private class BaseDocument
        {
            [BsonId] public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

            [BsonElement("_t")]
            public string Field1 { get; set; }
        }

        private class DerivedDocument : BaseDocument {}

        [Fact]
        public void BsonClassMapSerializer_serialization_when_using_field_with_same_element_name_as_discriminator_should_throw()
        {
            var obj = new DerivedDocument { Field1 = "field1" };

            var recordedException = Record.Exception(() => obj.ToJson(typeof(BaseDocument)));
            recordedException.Should().NotBeNull();
            recordedException.Should().BeOfType<BsonSerializationException>();
            recordedException.Message.Should().Be("The discriminator element name cannot be _t because it is already being used" +
                                                  " by the property Field1 of type MongoDB.Driver.Tests.Jira.CSharp4040Tests+DerivedDocument");
        }
    }
}
