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
    public class ExplicitDecodingTests
    {
        [Fact]
        [ResetGuidModeAfterTest]
        public void Explicit_decoding_with_csharp_legacy_representation_should_work_as_expected()
        {
            GuidMode.Set(GuidRepresentationMode.V3);

            var bytes = BsonUtils.ParseHexString("33221100554477668899aabbccddeeff");
            var binaryData = new BsonBinaryData(bytes, BsonBinarySubType.UuidLegacy);

            var exception = Record.Exception(() => binaryData.ToGuid());
            exception.Should().BeOfType<InvalidOperationException>();

            exception = Record.Exception(() => binaryData.ToGuid(GuidRepresentation.Standard));
            exception.Should().BeOfType<InvalidOperationException>();

            exception = Record.Exception(() => binaryData.ToGuid(GuidRepresentation.Unspecified));
            exception.Should().BeOfType<ArgumentException>();

            var result = binaryData.ToGuid(GuidRepresentation.CSharpLegacy);
            result.Should().Be(new Guid("00112233445566778899aabbccddeeff"));
        }

        [Fact]
        [ResetGuidModeAfterTest]
        public void Explicit_decoding_with_java_legacy_representation_should_work_as_expected()
        {
            GuidMode.Set(GuidRepresentationMode.V3);

            var bytes = BsonUtils.ParseHexString("7766554433221100ffeeddccbbaa9988");
            var binaryData = new BsonBinaryData(bytes, BsonBinarySubType.UuidLegacy);

            var exception = Record.Exception(() => binaryData.ToGuid());
            exception.Should().BeOfType<InvalidOperationException>();

            exception = Record.Exception(() => binaryData.ToGuid(GuidRepresentation.Standard));
            exception.Should().BeOfType<InvalidOperationException>();

            exception = Record.Exception(() => binaryData.ToGuid(GuidRepresentation.Unspecified));
            exception.Should().BeOfType<ArgumentException>();

            var result = binaryData.ToGuid(GuidRepresentation.JavaLegacy);
            result.Should().Be(new Guid("00112233445566778899aabbccddeeff"));
        }

        [Fact]
        [ResetGuidModeAfterTest]
        public void Explicit_decoding_with_python_legacy_representation_should_work_as_expected()
        {
            GuidMode.Set(GuidRepresentationMode.V3);

            var bytes = BsonUtils.ParseHexString("00112233445566778899aabbccddeeff");
            var binaryData = new BsonBinaryData(bytes, BsonBinarySubType.UuidLegacy);

            var exception = Record.Exception(() => binaryData.ToGuid());
            exception.Should().BeOfType<InvalidOperationException>();

            exception = Record.Exception(() => binaryData.ToGuid(GuidRepresentation.Standard));
            exception.Should().BeOfType<InvalidOperationException>();

            exception = Record.Exception(() => binaryData.ToGuid(GuidRepresentation.Unspecified));
            exception.Should().BeOfType<ArgumentException>();

            var result = binaryData.ToGuid(GuidRepresentation.PythonLegacy);
            result.Should().Be(new Guid("00112233445566778899aabbccddeeff"));
        }

        [Fact]
        [ResetGuidModeAfterTest]
        public void Explicit_decoding_with_standard_representation_should_work_as_expected()
        {
            GuidMode.Set(GuidRepresentationMode.V3);

            var guid = new Guid("00112233445566778899aabbccddeeff");
            var bytes = GuidConverter.ToBytes(guid, GuidRepresentation.Standard);
            var binaryData = new BsonBinaryData(bytes, BsonBinarySubType.UuidStandard);

            var exception = Record.Exception(() => binaryData.ToGuid(GuidRepresentation.Unspecified));
            exception.Should().BeOfType<ArgumentException>();

            foreach (var guidRepresentation in new[] { GuidRepresentation.CSharpLegacy, GuidRepresentation.JavaLegacy, GuidRepresentation.PythonLegacy })
            {
                exception = Record.Exception(() => binaryData.ToGuid(guidRepresentation));
                exception.Should().BeOfType<InvalidOperationException>();
            }

            var result = binaryData.ToGuid();
            result.Should().Be(guid);

            result = binaryData.ToGuid(GuidRepresentation.Standard);
            result.Should().Be(guid);
        }
    }
}
