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
    public class BinaryVectorReaderTests
    {
        [Fact]
        public void ReadBinaryVector_should_throw_on_type_mismatch_for_Int8()
        {
            byte[] vectorBsonData = [(byte)BinaryVectorDataType.Int8, 0, 1];

            var exception = Record.Exception(() => BinaryVectorReader.ReadBinaryVector<byte>(vectorBsonData));
            exception.Should().NotBeNull();
            exception.Message.Should().Contain($"Expected {typeof(sbyte)}");
            exception.Message.Should().Contain($"but found {typeof(byte)}");
        }
    }
}
