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
using System.Linq;
using FluentAssertions;
using MongoDB.Bson.TestHelpers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Bson.Tests
{
    public class BsonBinaryDataTests
    {
        [Theory]
        [ParameterAttributeData]
        public void constructor_should_throw_when_bytes_is_null(
            [Range(1, 3)] int overload)
        {
            var bytes = (byte[])null;

            Exception exception = null;
            switch (overload)
            {
                case 1: exception = Record.Exception(() => new BsonBinaryData(bytes)); break;
                case 2: exception = Record.Exception(() => new BsonBinaryData(bytes, BsonBinarySubType.Binary)); break;
#pragma warning disable 618
                case 3: exception = Record.Exception(() => new BsonBinaryData(bytes, BsonBinarySubType.Binary, GuidRepresentation.Unspecified)); break;
#pragma warning restore 618
            }

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("bytes");
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_should_throw_when_bytes_length_is_not_16_and_sub_type_is_uuid(
            [Values(BsonBinarySubType.UuidLegacy, BsonBinarySubType.UuidStandard)] BsonBinarySubType subType,
            [Values(0, 15, 17)] int length,
            [Range(1, 2)] int overload)
        {
            var bytes = new byte[length];
            var guidRepresentation = subType == BsonBinarySubType.UuidLegacy ? GuidRepresentation.CSharpLegacy : GuidRepresentation.Standard;

            Exception exception = null;
            switch (overload)
            {
                case 1: exception = Record.Exception(() => new BsonBinaryData(bytes, subType)); break;
#pragma warning disable 618
                case 2: exception = Record.Exception(() => new BsonBinaryData(bytes, subType, guidRepresentation)); break;
#pragma warning restore 618
            }

            var e = exception.Should().BeOfType<ArgumentException>().Subject;
            e.Message.Should().StartWith($"Length must be 16, not {length}, when subType is {subType}.");
            e.ParamName.Should().Be("bytes");
        }

        [Theory]
        [ParameterAttributeData]
        [ResetGuidModeAfterTest]
        public void constructor_should_throw_when_sub_type_is_uuid_and_guid_representation_is_invalid(
            [ClassValues(typeof(GuidModeValues))] GuidMode mode,
            [Values(5)] GuidRepresentation guidRepresentation,
            [Range(1, 2)] int overload)
        {
            mode.Set();

#pragma warning disable 618
            var bytes = new byte[16];
            var guid = Guid.Empty;

            Exception exception = null;
            switch (overload)
            {
                case 1: exception = Record.Exception(() => new BsonBinaryData(bytes, BsonBinarySubType.UuidLegacy, guidRepresentation)); break;
                case 2: exception = Record.Exception(() => new BsonBinaryData(guid, guidRepresentation)); break;
            }

            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2 || overload == 2)
            {
                var e = exception.Should().BeOfType<ArgumentException>().Subject;
                e.Message.Should().StartWith($"Invalid guidRepresentation: 5.");
                e.ParamName.Should().Be("guidRepresentation");
            }
            else
            {
                exception.Should().BeOfType<InvalidOperationException>();
            }
#pragma warning restore 618
        }

        [Theory]
        [InlineData(BsonBinarySubType.UuidLegacy, GuidRepresentation.Standard, "GuidRepresentation Standard is only valid with subType UuidStandard, not with subType UuidLegacy.")]
        [InlineData(BsonBinarySubType.UuidStandard, GuidRepresentation.CSharpLegacy, "GuidRepresentation CSharpLegacy is only valid with subType UuidLegacy, not with subType UuidStandard.")]
        [InlineData(BsonBinarySubType.UuidStandard, GuidRepresentation.JavaLegacy, "GuidRepresentation JavaLegacy is only valid with subType UuidLegacy, not with subType UuidStandard.")]
        [InlineData(BsonBinarySubType.UuidStandard, GuidRepresentation.PythonLegacy, "GuidRepresentation PythonLegacy is only valid with subType UuidLegacy, not with subType UuidStandard.")]
        [InlineData(BsonBinarySubType.UuidStandard, GuidRepresentation.Unspecified, "GuidRepresentation Unspecified is only valid with subType UuidLegacy, not with subType UuidStandard.")]
        [ResetGuidModeAfterTest]
        public void constructor_should_throw_when_sub_type_is_uuid_and_guid_representation_is_invalid_with_sub_type(BsonBinarySubType subType, GuidRepresentation guidRepresentation, string expectedMessage)
        {
#pragma warning disable 618
            foreach (var mode in GuidMode.All)
            {
                mode.Set();

                var bytes = new byte[16];

                var exception = Record.Exception(() => new BsonBinaryData(bytes, subType, guidRepresentation));

                if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
                {
                    var e = exception.Should().BeOfType<ArgumentException>().Subject;
                    e.Message.Should().StartWith(expectedMessage);
                    e.ParamName.Should().Be("guidRepresentation");
                }
                else
                {
                    exception.Should().BeOfType<InvalidOperationException>();
                }
            }
#pragma warning restore 618
        }

        [Theory]
        [ParameterAttributeData]
        [ResetGuidModeAfterTest]
        public void constructor_should_throw_when_sub_type_is_not_uuid_and_guid_representation_is_not_unspecified(
            [ClassValues(typeof(GuidModeValues))] GuidMode mode,
            [Values(BsonBinarySubType.Binary)] BsonBinarySubType subType,
            [Values(GuidRepresentation.CSharpLegacy, GuidRepresentation.Standard)] GuidRepresentation guidRepresentation)
        {
            mode.Set();

#pragma warning disable 618
            var bytes = new byte[0];

            var exception = Record.Exception(() => new BsonBinaryData(bytes, subType, guidRepresentation));

            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
            {
                var e = exception.Should().BeOfType<ArgumentException>().Subject;
                e.Message.Should().StartWith($"GuidRepresentation must be Unspecified, not {guidRepresentation}, when subType is not UuidStandard or UuidLegacy.");
                e.ParamName.Should().Be("guidRepresentation");
            }
            else
            {
                exception.Should().BeOfType<InvalidOperationException>();
            }
#pragma warning restore 618
        }

        [Fact]
        public void TestCreateNull()
        {
            object obj = null;
            Assert.Throws<ArgumentNullException>(() => { BsonBinaryData.Create(obj); });
        }

        [Theory]
        [ParameterAttributeData]
        [ResetGuidModeAfterTest]
        public void TestGuidCSharpLegacy(
            [ClassValues(typeof(GuidModeValues))] GuidMode mode)
        {
            mode.Set();

#pragma warning disable 618
            var guid = new Guid("01020304-0506-0708-090a-0b0c0d0e0f10");
            var binaryData = new BsonBinaryData(guid, GuidRepresentation.CSharpLegacy);
            var expected = new byte[] { 4, 3, 2, 1, 6, 5, 8, 7, 9, 10, 11, 12, 13, 14, 15, 16 };
            Assert.True(expected.SequenceEqual(binaryData.Bytes));
            Assert.Equal(BsonBinarySubType.UuidLegacy, binaryData.SubType);

            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
            {
                Assert.Equal(GuidRepresentation.CSharpLegacy, binaryData.GuidRepresentation);
                Assert.Equal(guid, binaryData.AsGuid);
                Assert.Equal(guid, binaryData.RawValue);
            }
#pragma warning restore
        }

        [Theory]
        [ParameterAttributeData]
        [ResetGuidModeAfterTest]
        public void TestGuidPythonLegacy(
            [ClassValues(typeof(GuidModeValues))] GuidMode mode)
        {
            mode.Set();

#pragma warning disable 618
            var guid = new Guid("01020304-0506-0708-090a-0b0c0d0e0f10");
            var binaryData = new BsonBinaryData(guid, GuidRepresentation.PythonLegacy);
            var expected = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
            Assert.True(expected.SequenceEqual(binaryData.Bytes));
            Assert.Equal(BsonBinarySubType.UuidLegacy, binaryData.SubType);

            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
            {
                Assert.Equal(GuidRepresentation.PythonLegacy, binaryData.GuidRepresentation);
                Assert.Equal(guid, binaryData.AsGuid);
                Assert.Equal(guid, binaryData.RawValue);
            }
#pragma warning restore 618
        }

        [Theory]
        [ParameterAttributeData]
        [ResetGuidModeAfterTest]
        public void TestGuidJavaLegacy(
            [ClassValues(typeof(GuidModeValues))] GuidMode mode)
        {
            mode.Set();

#pragma warning disable 618
            var guid = new Guid("01020304-0506-0708-090a-0b0c0d0e0f10");
            var binaryData = new BsonBinaryData(guid, GuidRepresentation.JavaLegacy);
            var expected = new byte[] { 8, 7, 6, 5, 4, 3, 2, 1, 16, 15, 14, 13, 12, 11, 10, 9 };
            Assert.True(expected.SequenceEqual(binaryData.Bytes));
            Assert.Equal(BsonBinarySubType.UuidLegacy, binaryData.SubType);
            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
            {
                Assert.Equal(GuidRepresentation.JavaLegacy, binaryData.GuidRepresentation);
                Assert.Equal(guid, binaryData.AsGuid);
                Assert.Equal(guid, binaryData.RawValue);
            }
#pragma warning restore
        }
    }
}
