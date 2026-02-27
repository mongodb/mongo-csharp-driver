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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
    public class BsonSerializationInfoTests
    {
        [Fact]
        public void Constructor_with_element_name_should_initialize_properties()
        {
            // Arrange
            var elementName = "testElement";
            var serializer = Int32Serializer.Instance;
            var nominalType = typeof(int);

            // Act
            var info = new BsonSerializationInfo(elementName, serializer, nominalType);

            // Assert
            info.ElementName.Should().Be(elementName);
            info.Serializer.Should().BeSameAs(serializer);
            info.NominalType.Should().Be(nominalType);
            info.ElementPath.Should().BeNull();
        }

        [Fact]
        public void CreateWithPath_should_handle_empty_path()
        {
            // Arrange
            var elementPath = Array.Empty<string>();
            var serializer = Int32Serializer.Instance;
            var nominalType = typeof(int);

            // Act
            var info = BsonSerializationInfo.CreateWithPath(elementPath, serializer, nominalType);

            // Assert
            info.ElementPath.Should().NotBeNull();
            info.ElementPath.Should().BeEmpty();
        }

        [Fact]
        public void CreateWithPath_should_initialize_instance_with_element_path()
        {
            // Arrange
            var elementPath = new[] { "parent", "child", "grandchild" };
            var serializer = Int32Serializer.Instance;
            var nominalType = typeof(int);

            // Act
            var info = BsonSerializationInfo.CreateWithPath(elementPath, serializer, nominalType);

            // Assert
            info.Serializer.Should().BeSameAs(serializer);
            info.NominalType.Should().Be(nominalType);
            info.ElementPath.Should().NotBeNull();
            info.ElementPath.Count.Should().Be(3);
            info.ElementPath.Should().Equal(elementPath);

            // Accessing ElementName should throw
            Action act = () => { var name = info.ElementName; };
            var exception = Record.Exception(act);
            
            exception.Should().BeOfType<InvalidOperationException>()
                .Which.Message.Should().Be("When ElementPath is not null you must use it instead.");
        }

        [Fact]
        public void DeserializeValue_should_deserialize_bson_value()
        {
            // Arrange
            var elementName = "testElement";
            var serializer = Int32Serializer.Instance;
            var nominalType = typeof(int);
            var info = new BsonSerializationInfo(elementName, serializer, nominalType);
            var value = new BsonInt32(42);

            // Act
            var result = info.DeserializeValue(value);

            // Assert
            result.Should().BeOfType<int>();
            result.Should().Be(42);
        }

        [Fact]
        public void ElementName_should_throw_when_element_path_is_used()
        {
            // Arrange
            var elementPath = new[] { "parent", "child" };
            var info = BsonSerializationInfo.CreateWithPath(elementPath, Int32Serializer.Instance, typeof(int));

            // Act
            Action act = () => { var name = info.ElementName; };
            var exception = Record.Exception(act);

            // Assert
            exception.Should().BeOfType<InvalidOperationException>()
                .Which.Message.Should().Be("When ElementPath is not null you must use it instead.");
        }

        [Fact]
        public void ElementPath_property_should_return_null_when_created_with_element_name()
        {
            // Arrange
            var elementName = "testElement";
            var serializer = Int32Serializer.Instance;
            var nominalType = typeof(int);
            var info = new BsonSerializationInfo(elementName, serializer, nominalType);

            // Act
            var result = info.ElementPath;

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void Equals_derived_should_return_false()
        {
            var x = new BsonSerializationInfo("elementName", Int32Serializer.Instance, typeof(int));
            var y = new DerivedFromBsonSerializationInfo("elementName", Int32Serializer.Instance, typeof(int));

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new BsonSerializationInfo("elementName", Int32Serializer.Instance, typeof(int));

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new BsonSerializationInfo("elementName", Int32Serializer.Instance, typeof(int));
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new BsonSerializationInfo("elementName", Int32Serializer.Instance, typeof(int));

            var result = x.Equals(x);

            result.Should().Be(true);

        }

        // TODO Should elementPath be compared as well?
        //[Fact]
        //public void Equals_with_element_path_should_compare_correctly()
        //{
        //    // Arrange
        //    var path1 = new[] { "parent", "child" };
        //    var path2 = new[] { "parent", "child" };
        //    var path3 = new[] { "different", "path" };

        //    var info1 = BsonSerializationInfo.CreateWithPath(path1, Int32Serializer.Instance, typeof(int));
        //    var info2 = BsonSerializationInfo.CreateWithPath(path2, Int32Serializer.Instance, typeof(int));
        //    var info3 = BsonSerializationInfo.CreateWithPath(path3, Int32Serializer.Instance, typeof(int));

        //    // Act & Assert
        //    info1.Equals(info2).Should().BeTrue();
        //    info1.Equals(info3).Should().BeFalse();
        //}

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new BsonSerializationInfo("elementName", Int32Serializer.Instance, typeof(int));
            var y = new BsonSerializationInfo("elementName", Int32Serializer.Instance, typeof(int));

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Theory]
        [InlineData("elementName")]
        [InlineData("elementPath")]
        [InlineData("serializer")]
        [InlineData("nominalType")]
        public void Equals_with_not_equal_field_should_return_false(string notEqualFieldName)
        {
            var elementPath1 = new[] { "elementName1" };
            var elementPath2 = new[] { "elementName2" };
            var serializer1 = new Int32Serializer(BsonType.Int32);
            var serializer2 = new Int32Serializer(BsonType.String);
            var nominalType1 = typeof(int);
            var nominalType2 = typeof(object);
            var x = notEqualFieldName == "elementPath" ?
                BsonSerializationInfo.CreateWithPath(elementPath1, serializer1, nominalType1) :
                new BsonSerializationInfo("elementName1", serializer1, nominalType1);
            var y = notEqualFieldName switch
            {
                "elementName" => new BsonSerializationInfo("elementName2", serializer1, nominalType1),
                "elementPath" => BsonSerializationInfo.CreateWithPath(elementPath2, serializer1, nominalType1),
                "serializer" => new BsonSerializationInfo("elementName1", serializer2, nominalType1),
                "nominalType" => new BsonSerializationInfo("elementName1", serializer1, nominalType2),
                _ => throw new Exception()
            };

            var result = x.Equals(y);

            result.Should().Be(notEqualFieldName == null ? true : false);
        }

        [Fact]
        public void Equals_with_null_element_path_should_compare_correctly()
        {
            // Arrange
            var info1 = new BsonSerializationInfo("name", Int32Serializer.Instance, typeof(int));
            var info2 = new BsonSerializationInfo("name", Int32Serializer.Instance, typeof(int));
            var info3 = new BsonSerializationInfo("different", Int32Serializer.Instance, typeof(int));

            // Act & Assert
            info1.Equals(info2).Should().BeTrue();
            info1.Equals(info3).Should().BeFalse();
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new BsonSerializationInfo("elementName", Int32Serializer.Instance, typeof(int));

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        [Fact]
        public void NominalType_property_should_return_configured_type()
        {
            // Arrange
            var elementName = "testElement";
            var serializer = Int32Serializer.Instance;
            var nominalType = typeof(int);
            var info = new BsonSerializationInfo(elementName, serializer, nominalType);

            // Act
            var result = info.NominalType;

            // Assert
            result.Should().Be(nominalType);
        }

        [Fact]
        public void Serializer_property_should_return_configured_serializer()
        {
            // Arrange
            var elementName = "testElement";
            var serializer = Int32Serializer.Instance;
            var nominalType = typeof(int);
            var info = new BsonSerializationInfo(elementName, serializer, nominalType);

            // Act
            var result = info.Serializer;

            // Assert
            result.Should().BeSameAs(serializer);
        }

        [Fact]
        public void SerializeValue_should_serialize_value()
        {
            // Arrange
            var elementName = "testElement";
            var serializer = Int32Serializer.Instance;
            var nominalType = typeof(int);
            var info = new BsonSerializationInfo(elementName, serializer, nominalType);
            var value = 42;

            // Act
            var result = info.SerializeValue(value);

            // Assert
            result.Should().BeOfType<BsonInt32>();
            result.AsInt32.Should().Be(42);
        }

        [Fact]
        public void SerializeValues_should_handle_empty_collection()
        {
            // Arrange
            var elementName = "testElement";
            var serializer = Int32Serializer.Instance;
            var nominalType = typeof(int);
            var info = new BsonSerializationInfo(elementName, serializer, nominalType);
            var values = Array.Empty<int>();

            // Act
            var result = info.SerializeValues(values);

            // Assert
            result.Should().BeOfType<BsonArray>();
            result.AsBsonArray.Count.Should().Be(0);
        }

        [Fact]
        public void SerializeValues_should_serialize_multiple_values()
        {
            // Arrange
            var elementName = "testElement";
            var serializer = Int32Serializer.Instance;
            var nominalType = typeof(int);
            var info = new BsonSerializationInfo(elementName, serializer, nominalType);
            var values = new[] { 1, 2, 3, 4, 5 };

            // Act
            var result = info.SerializeValues(values);

            // Assert
            result.Should().BeOfType<BsonArray>();
            result.AsBsonArray.Count.Should().Be(5);
            for (var i = 0; i < 5; i++)
            {
                result[i].AsInt32.Should().Be(i + 1);
            }
        }

        [Fact]
        public void WithNewName_should_create_new_instance_with_different_name()
        {
            // Arrange
            var originalName = "originalName";
            var newName = "newName";
            var serializer = Int32Serializer.Instance;
            var nominalType = typeof(int);
            var originalInfo = new BsonSerializationInfo(originalName, serializer, nominalType);

            // Act
            var newInfo = originalInfo.WithNewName(newName);

            // Assert
            newInfo.Should().NotBeSameAs(originalInfo);
            newInfo.ElementName.Should().Be(newName);
            newInfo.Serializer.Should().BeSameAs(serializer);
            newInfo.NominalType.Should().Be(nominalType);
        }

        private class DerivedFromBsonSerializationInfo(string elementName, IBsonSerializer serializer, Type nominalType) : BsonSerializationInfo(elementName, serializer, nominalType)
        {
        }
    }
}
