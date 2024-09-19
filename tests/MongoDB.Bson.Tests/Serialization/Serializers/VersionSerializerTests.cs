/* Copyright 2015-present MongoDB Inc.
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

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
    public class VersionSerializerTests
    {
        [Fact]
        public void TestDeserializeInvalidElement()
        {
            var json = "{ Invalid : 1 }";
            var exception = Assert.Throws<BsonSerializationException>(() => BsonSerializer.Deserialize<Version>(json));
            var expectedMessage = "Invalid element: 'Invalid'.";
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestDeserializeMissingMajor()
        {
            var json = "{ }";
            var exception = Assert.Throws<BsonSerializationException>(() => BsonSerializer.Deserialize<Version>(json));
            var expectedMessage = "Missing element: 'Major'.";
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestDeserializeMissingMinor()
        {
            var json = "{ Major : 1 }";
            var exception = Assert.Throws<BsonSerializationException>(() => BsonSerializer.Deserialize<Version>(json));
            var expectedMessage = "Missing element: 'Minor'.";
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestDeserializeMajorMinor()
        {
            var json = "{ Major : 1, Minor : 2 }";
            var version = BsonSerializer.Deserialize<Version>(json);
            Assert.Equal(1, version.Major);
            Assert.Equal(2, version.Minor);
            Assert.Equal(-1, version.Build);
            Assert.Equal(-1, version.Revision);
        }

        [Fact]
        public void TestDeserializeMajorMinorOutOfOrder()
        {
            var json = "{ Minor : 2, Major : 1 }";
            var version = BsonSerializer.Deserialize<Version>(json);
            Assert.Equal(1, version.Major);
            Assert.Equal(2, version.Minor);
            Assert.Equal(-1, version.Build);
            Assert.Equal(-1, version.Revision);
        }

        [Fact]
        public void TestDeserializeMajorMinorBuild()
        {
            var json = "{ Major : 1, Minor : 2, Build : 3 }";
            var version = BsonSerializer.Deserialize<Version>(json);
            Assert.Equal(1, version.Major);
            Assert.Equal(2, version.Minor);
            Assert.Equal(3, version.Build);
            Assert.Equal(-1, version.Revision);
        }

        [Fact]
        public void TestDeserializeMajorMinorBuildOutOfOrder()
        {
            var json = "{ Build : 3, Major : 1, Minor : 2  }";
            var version = BsonSerializer.Deserialize<Version>(json);
            Assert.Equal(1, version.Major);
            Assert.Equal(2, version.Minor);
            Assert.Equal(3, version.Build);
            Assert.Equal(-1, version.Revision);
        }

        [Fact]
        public void TestDeserializeMajorMinorBuildRevision()
        {
            var json = "{ Major : 1, Minor : 2, Build : 3, Revision : 4 }";
            var version = BsonSerializer.Deserialize<Version>(json);
            Assert.Equal(1, version.Major);
            Assert.Equal(2, version.Minor);
            Assert.Equal(3, version.Build);
            Assert.Equal(4, version.Revision);
        }

        [Fact]
        public void TestDeserializeMajorMinorBuildRevisionOutOfOrder()
        {
            var json = "{ Revision : 4, Major : 1, Minor : 2, Build : 3 }";
            var version = BsonSerializer.Deserialize<Version>(json);
            Assert.Equal(1, version.Major);
            Assert.Equal(2, version.Minor);
            Assert.Equal(3, version.Build);
            Assert.Equal(4, version.Revision);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new VersionSerializer();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new VersionSerializer();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new VersionSerializer();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new VersionSerializer();
            var y = new VersionSerializer();

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_not_equal_field_should_return_false()
        {
            var x = new VersionSerializer(BsonType.String);
            var y = new VersionSerializer(BsonType.Document);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new VersionSerializer();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }
    }
}
