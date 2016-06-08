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
using System.Reflection;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Moq;
using Xunit;

namespace MongoDB.Bson.Tests.IO
{
    public class InputBufferChunkSourceTests
    {
        [Fact]
        public void BaseSource_get_should_return_expected_result()
        {
            var mockBaseSource = new Mock<IBsonChunkSource>();
            var subject = new InputBufferChunkSource(mockBaseSource.Object);

            var result = subject.BaseSource;

            result.Should().BeSameAs(mockBaseSource.Object);
        }

        [Fact]
        public void BaseSource_get_should_throw_when_subject_is_disposed()
        {
            var mockBaseSource = new Mock<IBsonChunkSource>();
            var subject = new InputBufferChunkSource(mockBaseSource.Object);
            subject.Dispose();

            Action action = () => { var _ = subject.BaseSource; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("InputBufferChunkSource");
        }

        [Fact]
        public void constructor_should_initialize_subject()
        {
            var mockBaseSource = new Mock<IBsonChunkSource>();
            var maxUnpooledChunkSize = 2;
            var minChunkSize = 4;
            var maxChunkSize = 8;

            var subject = new InputBufferChunkSource(mockBaseSource.Object, maxUnpooledChunkSize, minChunkSize, maxChunkSize);

            var reflector = new Reflector(subject);
            subject.BaseSource.Should().BeSameAs(mockBaseSource.Object);
            subject.MaxChunkSize.Should().Be(maxChunkSize);
            subject.MaxUnpooledChunkSize.Should().Be(maxUnpooledChunkSize);
            subject.MinChunkSize.Should().Be(minChunkSize);
            reflector._disposed.Should().BeFalse();
        }

        [Fact]
        public void constructor_should_throw_when_maxChunkSize_is_less_than_minChunkSize()
        {
            Action action = () => new InputBufferChunkSource(BsonChunkPool.Default, minChunkSize: 2, maxChunkSize: 1);

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("maxChunkSize");
        }

        [Fact]
        public void constructor_should_throw_when_maxChunkSize_is_negative()
        {
            Action action = () => new InputBufferChunkSource(BsonChunkPool.Default, maxChunkSize: -1);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("maxChunkSize");
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_should_throw_when_maxChunkSize_is_not_a_power_of_2(
            [Values(3, 5, 6, 7, 9, 15, 17)]
            int maxChunkSize)
        {
            Action action = () => new InputBufferChunkSource(BsonChunkPool.Default, maxChunkSize: maxChunkSize);

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("maxChunkSize");
        }

        [Fact]
        public void constructor_should_throw_when_baseSource_is_null()
        {
            Action action = () => new InputBufferChunkSource(null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("baseSource");
        }

        [Fact]
        public void constructor_should_throw_when_maxUnpooledChunkSize_is_negative()
        {
            Action action = () => new InputBufferChunkSource(BsonChunkPool.Default, maxUnpooledChunkSize: -1);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("maxUnpooledChunkSize");
        }

        [Fact]
        public void constructor_should_throw_when_minChunkSize_is_negative()
        {
            Action action = () => new InputBufferChunkSource(BsonChunkPool.Default, minChunkSize: -1);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("minChunkSize");
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_should_throw_when_minChunkSize_is_not_a_power_of_2(
            [Values(3, 5, 6, 7, 9, 15, 17)]
            int minChunkSize)
        {
            Action action = () => new InputBufferChunkSource(BsonChunkPool.Default, minChunkSize: minChunkSize);

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("minChunkSize");
        }

        [Fact]
        public void constructor_should_use_default_value_for_maxChunkSize()
        {
            var mockBaseSource = new Mock<IBsonChunkSource>();

            var subject = new InputBufferChunkSource(mockBaseSource.Object);

            subject.MaxChunkSize.Should().Be(1 * 1024 * 1024);
        }

        [Fact]
        public void constructor_should_use_default_value_for_maxUnpooledChunkSize()
        {
            var mockBaseSource = new Mock<IBsonChunkSource>();

            var subject = new InputBufferChunkSource(mockBaseSource.Object);

            subject.MaxUnpooledChunkSize.Should().Be(4 * 1024);
        }

        [Fact]
        public void constructor_should_use_default_value_for_minChunkSize()
        {
            var mockBaseSource = new Mock<IBsonChunkSource>();

            var subject = new InputBufferChunkSource(mockBaseSource.Object);

            subject.MinChunkSize.Should().Be(16 * 1024);
        }

        [Fact]
        public void Dispose_can_be_called_more_than_once()
        {
            var mockBaseSource = new Mock<IBsonChunkSource>();
            var subject = new InputBufferChunkSource(mockBaseSource.Object);

            subject.Dispose();
            subject.Dispose();
        }

        [Fact]
        public void Dispose_should_dispose_subject()
        {
            var mockBaseSource = new Mock<IBsonChunkSource>();
            var subject = new InputBufferChunkSource(mockBaseSource.Object);

            subject.Dispose();

            var reflector = new Reflector(subject);
            reflector._disposed.Should().BeTrue();
        }

        [Theory]
        [ParameterAttributeData]
        public void GetChunk_should_return_unpooled_chunk_when_requestedSize_is_less_than_or_equal_to_maxUnpooledChunkSize(
            [Values(1, 2, 4 * 1024 - 1, 4 * 1024)]
            int requestedSize)
        {
            var mockBaseSource = new Mock<IBsonChunkSource>();
            var subject = new InputBufferChunkSource(mockBaseSource.Object);

            var result = subject.GetChunk(requestedSize);

            result.Should().BeOfType<ByteArrayChunk>();
            result.Bytes.Count.Should().Be(requestedSize);
            mockBaseSource.Verify(s => s.GetChunk(It.IsAny<int>()), Times.Never);
        }

        [Theory]
        [InlineData(1, 4)]
        [InlineData(2, 4)]
        [InlineData(3, 4)]
        [InlineData(4, 4)]
        [InlineData(5, 8)]
        [InlineData(6, 8)]
        [InlineData(7, 8)]
        [InlineData(8, 8)]
        [InlineData(9, 8)]
        [InlineData(10, 8)]
        [InlineData(11, 8)]
        [InlineData(12, 16)]
        [InlineData(13, 16)]
        [InlineData(14, 16)]
        [InlineData(15, 16)]
        [InlineData(16, 16)]
        public void GetChunk_should_round_requestedSize_to_power_of_2_without_wasting_too_much_space(int requestedSize, int roundedSize)
        {
            var mockBaseSource = new Mock<IBsonChunkSource>();
            var subject = new InputBufferChunkSource(mockBaseSource.Object, maxUnpooledChunkSize: 0, minChunkSize: 4, maxChunkSize: 16);
            mockBaseSource.Setup(s => s.GetChunk(It.IsAny<int>())).Returns(() => new Mock<IBsonChunk>().Object);

            subject.GetChunk(requestedSize);

            mockBaseSource.Verify(s => s.GetChunk(roundedSize), Times.Once);
        }

        [Theory]
        [ParameterAttributeData]
        public void GetChunk_should_throw_when_requestedSize_is_less_than_or_equal_to_zero(
            [Values(-1, 0)]
            int requestedSize)
        {
            var mockBaseSource = new Mock<IBsonChunkSource>();
            var subject = new InputBufferChunkSource(mockBaseSource.Object);

            Action action = () => subject.GetChunk(requestedSize);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("requestedSize");
        }

        [Fact]
        public void GetChunk_should_throw_when_subject_is_disposed()
        {
            var mockBaseSource = new Mock<IBsonChunkSource>();
            var subject = new InputBufferChunkSource(mockBaseSource.Object);
            subject.Dispose();

            Action action = () => subject.GetChunk(1);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("InputBufferChunkSource");
        }

        [Fact]
        public void MaxChunkSize_get_should_return_expected_result()
        {
            var mockBaseSource = new Mock<IBsonChunkSource>();
            var maxChunkSize = 32 * 1024 * 1024;
            var subject = new InputBufferChunkSource(mockBaseSource.Object, maxChunkSize: maxChunkSize);

            var result = subject.MaxChunkSize;

            result.Should().Be(maxChunkSize);
        }

        [Fact]
        public void MaxChunkUnpooledSize_get_should_return_expected_result()
        {
            var mockBaseSource = new Mock<IBsonChunkSource>();
            var maxUnpooledChunkSize = 8 * 1024;
            var subject = new InputBufferChunkSource(mockBaseSource.Object, maxUnpooledChunkSize: maxUnpooledChunkSize);

            var result = subject.MaxUnpooledChunkSize;

            result.Should().Be(maxUnpooledChunkSize);
        }

        [Fact]
        public void MinChunkSize_get_should_return_expected_result()
        {
            var mockBaseSource = new Mock<IBsonChunkSource>();
            var minChunkSize = 8 * 1024;
            var subject = new InputBufferChunkSource(mockBaseSource.Object, minChunkSize: minChunkSize);

            var result = subject.MinChunkSize;

            result.Should().Be(minChunkSize);
        }

        // nested types
        private class Reflector
        {
            private readonly InputBufferChunkSource _instance;

            public Reflector(InputBufferChunkSource instance)
            {
                _instance = instance;
            }

            public bool _disposed
            {
                get
                {
                    var field = typeof(InputBufferChunkSource).GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance);
                    return (bool)field.GetValue(_instance);
                }
            }
        }
    }
}
