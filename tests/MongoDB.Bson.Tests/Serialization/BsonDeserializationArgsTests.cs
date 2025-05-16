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
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
    public class BsonDeserializationArgsTests
    {
        [Fact]
        public void Empty_instance_should_have_null_NominalType()
        {
            // Arrange
            var args = new BsonDeserializationArgs();

            // Act
            var nominalType = args.NominalType;

            // Assert
            nominalType.Should().BeNull();
        }

        [Fact]
        public void NominalType_property_should_allow_null_value()
        {
            // Arrange
            var args = new BsonDeserializationArgs
            {
                NominalType = typeof(int)
            };

            // Act
            args.NominalType = null;
            var result = args.NominalType;

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void NominalType_property_should_be_updatable()
        {
            // Arrange
            var args = new BsonDeserializationArgs();
            var firstType = typeof(int);
            var secondType = typeof(string);

            // Act
            args.NominalType = firstType;
            var firstResult = args.NominalType;
            args.NominalType = secondType;
            var secondResult = args.NominalType;

            // Assert
            firstResult.Should().BeSameAs(firstType);
            secondResult.Should().BeSameAs(secondType);
        }

        [Fact]
        public void NominalType_property_should_return_set_value()
        {
            // Arrange
            var args = new BsonDeserializationArgs();
            var expectedType = typeof(string);

            // Act
            args.NominalType = expectedType;
            var result = args.NominalType;

            // Assert
            result.Should().BeSameAs(expectedType);
        }
    }
}
