/* Copyright 2010-2016 MongoDB Inc.
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Moq;
using Xunit;

namespace MongoDB.Bson.Tests.IO
{
    public class ByteBufferFactoryTests
    {
        [Theory]
        [ParameterAttributeData]
        public void Create_should_return_expected_result(
            [Values(1, 63, 64, 65, 128)]
            int minimumCapacity)
        {
            var chunkSource = new BsonChunkPool(1, 64);

            var result = ByteBufferFactory.Create(chunkSource, minimumCapacity);

            result.Capacity.Should().BeGreaterOrEqualTo(minimumCapacity);
        }

        [Theory]
        [ParameterAttributeData]
        public void Create_should_return_SingleChunkBuffer_when_a_single_chunk_is_sufficient(
            [Values(1, 63, 64)]
            int minimumCapacity)
        {
            var chunkSource = new BsonChunkPool(1, 64);

            var result = ByteBufferFactory.Create(chunkSource, minimumCapacity);

            result.Should().BeOfType<SingleChunkBuffer>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Create_should_return_MultiChunkBuffer_when_multiple_chunks_are_required(
            [Values(65, 128)]
            int minimumCapacity)
        {
            var chunkSource = new BsonChunkPool(1, 64);

            var result = ByteBufferFactory.Create(chunkSource, minimumCapacity);

            result.Should().BeOfType<MultiChunkBuffer>();
        }

        [Fact]
        public void Create_should_throw_when_chunkSource_is_null()
        {
            Action action = () => ByteBufferFactory.Create(null, 0);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("chunkSource");
        }

        [Theory]
        [ParameterAttributeData]
        public void Create_should_throw_when_minimumCapacity_is_invalid(
            [Values(-1, 0)]
            int minimumCapacity)
        {
            var mockChunkSource = new Mock<IBsonChunkSource>();

            Action action = () => ByteBufferFactory.Create(mockChunkSource.Object, minimumCapacity);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("minimumCapacity");
        }
    }
}
