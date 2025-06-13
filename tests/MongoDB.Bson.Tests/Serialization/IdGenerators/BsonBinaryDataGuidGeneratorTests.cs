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

using System;
using FluentAssertions;
using MongoDB.Bson.Serialization.IdGenerators;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.IdGenerators
{
    public class BsonBinaryDataGuidGeneratorTests
    {
        [Fact]
        public void Constructor_should_initialize_instance()
        {
            // Arrange
            var guidRepresentation = GuidRepresentation.Standard;

            // Act
            var generator = new BsonBinaryDataGuidGenerator(guidRepresentation);

            // Assert
            generator.GuidRepresentation.Should().Be(guidRepresentation);
        }

        [Fact]
        public void CSharpLegacyInstance_should_return_singleton_with_correct_representation()
        {
            // Act
            var instance = BsonBinaryDataGuidGenerator.CSharpLegacyInstance;

            // Assert
            instance.Should().NotBeNull();
            instance.GuidRepresentation.Should().Be(GuidRepresentation.CSharpLegacy);
            BsonBinaryDataGuidGenerator.CSharpLegacyInstance.Should().BeSameAs(instance);
        }

        [Fact]
        public void GenerateId_should_return_BsonBinaryData_with_new_guid()
        {
            // Arrange
            var guidRepresentation = GuidRepresentation.Standard;
            var generator = new BsonBinaryDataGuidGenerator(guidRepresentation);

            // Act
            var id = generator.GenerateId(null, null);

            // Assert
            id.Should().BeOfType<BsonBinaryData>();
            var binaryData = (BsonBinaryData)id;
            binaryData.SubType.Should().Be(BsonBinarySubType.UuidStandard);
            binaryData.Bytes.Length.Should().Be(16);
            binaryData.Bytes.Should().NotEqual(Guid.Empty.ToByteArray());
        }

        [Fact]
        public void GetInstance_should_return_CSharpLegacyInstance_when_CSharpLegacy()
        {
            // Act
            var instance = BsonBinaryDataGuidGenerator.GetInstance(GuidRepresentation.CSharpLegacy);

            // Assert
            instance.Should().BeSameAs(BsonBinaryDataGuidGenerator.CSharpLegacyInstance);
        }

        [Fact]
        public void GetInstance_should_return_JavaLegacyInstance_when_JavaLegacy()
        {
            // Act
            var instance = BsonBinaryDataGuidGenerator.GetInstance(GuidRepresentation.JavaLegacy);

            // Assert
            instance.Should().BeSameAs(BsonBinaryDataGuidGenerator.JavaLegacyInstance);
        }

        [Fact]
        public void GetInstance_should_return_PythonLegacyInstance_when_PythonLegacy()
        {
            // Act
            var instance = BsonBinaryDataGuidGenerator.GetInstance(GuidRepresentation.PythonLegacy);

            // Assert
            instance.Should().BeSameAs(BsonBinaryDataGuidGenerator.PythonLegacyInstance);
        }

        [Fact]
        public void GetInstance_should_return_StandardInstance_when_Standard()
        {
            // Act
            var instance = BsonBinaryDataGuidGenerator.GetInstance(GuidRepresentation.Standard);

            // Assert
            instance.Should().BeSameAs(BsonBinaryDataGuidGenerator.StandardInstance);
        }

        [Fact]
        public void GetInstance_should_return_UnspecifiedInstance_when_Unspecified()
        {
            // Act
            var instance = BsonBinaryDataGuidGenerator.GetInstance(GuidRepresentation.Unspecified);

            // Assert
            instance.Should().BeSameAs(BsonBinaryDataGuidGenerator.UnspecifedInstance);
        }

        [Fact]
        public void GetInstance_should_throw_for_invalid_representation()
        {
            // Arrange
            var invalidRepresentation = (GuidRepresentation)999;

            // Act
            Action act = () => BsonBinaryDataGuidGenerator.GetInstance(invalidRepresentation);
            var exception = Record.Exception(act);

            // Assert
            exception.Should().BeOfType<ArgumentOutOfRangeException>()
                .Which.ParamName.Should().Be("guidRepresentation");
        }

        [Fact]
        public void GuidRepresentation_property_should_return_correct_value()
        {
            // Arrange
            var guidRepresentation = GuidRepresentation.Standard;
            var generator = new BsonBinaryDataGuidGenerator(guidRepresentation);

            // Act
            var result = generator.GuidRepresentation;

            // Assert
            result.Should().Be(guidRepresentation);
        }

        [Fact]
        public void IsEmpty_should_return_false_for_non_empty_guid()
        {
            // Arrange
            var generator = new BsonBinaryDataGuidGenerator(GuidRepresentation.Standard);
            var guid = new BsonBinaryData(Guid.NewGuid(), GuidRepresentation.Standard);

            // Act
            var result = generator.IsEmpty(guid);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsEmpty_should_return_true_for_BsonNull()
        {
            // Arrange
            var generator = new BsonBinaryDataGuidGenerator(GuidRepresentation.Standard);
            var bsonNull = BsonNull.Value;

            // Act
            var result = generator.IsEmpty(bsonNull);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsEmpty_should_return_true_for_empty_guid_legacy()
        {
            // Arrange
            var generator = new BsonBinaryDataGuidGenerator(GuidRepresentation.CSharpLegacy);
            var emptyGuid = new BsonBinaryData(Guid.Empty, GuidRepresentation.CSharpLegacy);

            // Act
            var result = generator.IsEmpty(emptyGuid);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsEmpty_should_return_true_for_empty_guid_standard()
        {
            // Arrange
            var generator = new BsonBinaryDataGuidGenerator(GuidRepresentation.Standard);
            var emptyGuid = new BsonBinaryData(Guid.Empty, GuidRepresentation.Standard);

            // Act
            var result = generator.IsEmpty(emptyGuid);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsEmpty_should_return_true_for_null()
        {
            // Arrange
            var generator = new BsonBinaryDataGuidGenerator(GuidRepresentation.Standard);

            // Act
            var result = generator.IsEmpty(null);

            // Assert
            result.Should().BeTrue();
        }
        [Fact]
        public void IsEmpty_should_throw_for_invalid_binary_subtype()
        {
            // Arrange
            var generator = new BsonBinaryDataGuidGenerator(GuidRepresentation.Standard);
            var invalidId = new BsonBinaryData(new byte[] { 1, 2, 3 }, BsonBinarySubType.Binary);

            // Act
            Action act = () => generator.IsEmpty(invalidId);
            var exception = Record.Exception(act);

            // Assert
            exception.Should().BeOfType<ArgumentOutOfRangeException>()
                .Which.ParamName.Should().Be("id");
        }

        [Fact]
        public void JavaLegacyInstance_should_return_singleton_with_correct_representation()
        {
            // Act
            var instance = BsonBinaryDataGuidGenerator.JavaLegacyInstance;

            // Assert
            instance.Should().NotBeNull();
            instance.GuidRepresentation.Should().Be(GuidRepresentation.JavaLegacy);
            BsonBinaryDataGuidGenerator.JavaLegacyInstance.Should().BeSameAs(instance);
        }

        [Fact]
        public void PythonLegacyInstance_should_return_singleton_with_correct_representation()
        {
            // Act
            var instance = BsonBinaryDataGuidGenerator.PythonLegacyInstance;

            // Assert
            instance.Should().NotBeNull();
            instance.GuidRepresentation.Should().Be(GuidRepresentation.PythonLegacy);
            BsonBinaryDataGuidGenerator.PythonLegacyInstance.Should().BeSameAs(instance);
        }

        [Fact]
        public void StandardInstance_should_return_singleton_with_correct_representation()
        {
            // Act
            var instance = BsonBinaryDataGuidGenerator.StandardInstance;

            // Assert
            instance.Should().NotBeNull();
            instance.GuidRepresentation.Should().Be(GuidRepresentation.Standard);
            BsonBinaryDataGuidGenerator.StandardInstance.Should().BeSameAs(instance);
        }

        [Fact]
        public void UnspecifiedInstance_should_return_singleton_with_correct_representation()
        {
            // Act
            var instance = BsonBinaryDataGuidGenerator.UnspecifedInstance;

            // Assert
            instance.Should().NotBeNull();
            instance.GuidRepresentation.Should().Be(GuidRepresentation.Unspecified);
            BsonBinaryDataGuidGenerator.UnspecifedInstance.Should().BeSameAs(instance);
        }
    }
}
