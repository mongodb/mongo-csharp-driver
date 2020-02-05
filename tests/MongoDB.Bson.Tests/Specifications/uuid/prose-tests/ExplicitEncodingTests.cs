/* Copyright 2019-present MongoDB Inc.
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
using MongoDB.Bson.TestHelpers;
using Xunit;

namespace MongoDB.Bson.Tests.Specifications.uuid.prose_tests
{
    public class ExplicitEncodingTests
    {
        [Fact]
        [ResetGuidModeAfterTest]
        public void BsonBinaryData_constructor_with_a_Guid_and_no_representation_should_throw()
        {
            GuidMode.Set(GuidRepresentationMode.V3);

            var guid = new Guid("00112233445566778899aabbccddeeff");

#pragma warning disable 618
            var exception = Record.Exception(() => new BsonBinaryData(guid));
#pragma warning disable 618

            exception.Should().BeOfType<InvalidOperationException>();
        }

        [Theory]
        [InlineData(GuidRepresentation.CSharpLegacy, BsonBinarySubType.UuidLegacy, "33221100554477668899aabbccddeeff")]
        [InlineData(GuidRepresentation.JavaLegacy, BsonBinarySubType.UuidLegacy, "7766554433221100ffeeddccbbaa9988")]
        [InlineData(GuidRepresentation.PythonLegacy, BsonBinarySubType.UuidLegacy, "00112233445566778899aabbccddeeff")]
        [InlineData(GuidRepresentation.Standard, BsonBinarySubType.UuidStandard, "00112233445566778899aabbccddeeff")]
        [ResetGuidModeAfterTest]
        public void BsonBinaryData_constructor_with_a_Guid_and_a_representation_should_return_expected_result(GuidRepresentation guidRepresentation, BsonBinarySubType expectedSubType, string expectedBytes)
        {
            GuidMode.Set(GuidRepresentationMode.V3);

            var guid = new Guid("00112233445566778899aabbccddeeff");

            var result = new BsonBinaryData(guid, guidRepresentation);

            result.SubType.Should().Be(expectedSubType);
            result.Bytes.Should().Equal(BsonUtils.ParseHexString(expectedBytes));
        }

        [Fact]
        [ResetGuidModeAfterTest]
        public void BsonBinaryData_constructor_with_a_Guid_and_representation_Unspecified_should_throw()
        {
            GuidMode.Set(GuidRepresentationMode.V3);

            var guid = new Guid("00112233445566778899aabbccddeeff");

#pragma warning disable 618
            var exception = Record.Exception(() => new BsonBinaryData(guid, GuidRepresentation.Unspecified));
#pragma warning disable 618

            exception.Should().BeOfType<InvalidOperationException>();
        }
    }
}
