/* Copyright 2016 MongoDB Inc.
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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class CreateViewOptionsTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var subject = new CreateViewOptions<BsonDocument>();

            subject.Collation.Should().BeNull();
            subject.DocumentSerializer.Should().BeNull();
            subject.SerializerRegistry.Should().BeNull();
        }

        [Theory]
        [ParameterAttributeData]
        public void Collation_get_and_set_should_work(
            [Values(null, "en_US", "fr_CA")]
            string locale)
        {
            var subject = new CreateViewOptions<BsonDocument>();
            var value = locale == null ? null : new Collation(locale);

            subject.Collation = value;
            var result = subject.Collation;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void DocumentSerializer_get_and_set_should_work(
            [Values(false, true)]
            bool isValueNull)
        {
            var subject = new CreateViewOptions<BsonDocument>();
            var documentSerializer = isValueNull ? null : new BsonDocumentSerializer();

            subject.DocumentSerializer = documentSerializer;
            var result = subject.DocumentSerializer;

            result.Should().BeSameAs(documentSerializer);
        }

        [Theory]
        [ParameterAttributeData]
        public void SerializerRegistry_get_and_set_should_work(
            [Values(false, true)]
            bool isValueNull)
        {
            var subject = new CreateViewOptions<BsonDocument>();
            var value = isValueNull ? null : new BsonSerializerRegistry();

            subject.SerializerRegistry = value;
            var result = subject.SerializerRegistry;

            result.Should().BeSameAs(value);
        }
    }
}
