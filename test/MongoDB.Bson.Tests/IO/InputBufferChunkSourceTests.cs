/* Copyright 2010-2015 MongoDB Inc.
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
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Bson.Tests.IO
{
    [TestFixture]
    public class InputBufferChunkSourceTests
    {
        [Test]
        public void BaseSource_get_should_return_expected_result()
        {
            var baseSource = Substitute.For<IBsonChunkSource>();
            var subject = new InputBufferChunkSource(baseSource);

            var result = subject.BaseSource;

            result.Should().BeSameAs(baseSource);
        }

        [Test]
        public void BaseSource_get_should_throw_when_subject_is_disposed()
        {
            var baseSource = Substitute.For<IBsonChunkSource>();
            var subject = new InputBufferChunkSource(baseSource);
            subject.Dispose();

            Action action = () => { var _ = subject.BaseSource; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("InputBufferChunkSource");
        }

        [Test]
        public void constructor_should_initialize_subject()
        {
            var baseSource = Substitute.For<IBsonChunkSource>();
            var maxUnpooledChunkSize = 2;
            var minChunkSize = 4;
            var maxChunkSize = 8;

            var subject = new InputBufferChunkSource(baseSource, maxUnpooledChunkSize, minChunkSize, maxChunkSize);

            var reflector = new Reflector(subject);
            subject.BaseSource.Should().BeSameAs(baseSource);
            subject.MaxChunkSize.Should().Be(maxChunkSize);
            subject.MaxUnpooledChunkSize.Should().Be(maxUnpooledChunkSize);
            subject.MinChunkSize.Should().Be(minChunkSize);
            reflector._disposed.Should().BeFalse();
        }

        [Test]
        public void constructor_should_throw_when_maxChunkSize_is_less_than_minChunkSize()
        {
            Action action = () => new InputBufferChunkSource(BsonChunkPool.Default, minChunkSize: 2, maxChunkSize: 1);

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("maxChunkSize");
        }

        [Test]
        public void constructor_should_throw_when_maxChunkSize_is_negative()
        {
            Action action = () => new InputBufferChunkSource(BsonChunkPool.Default, maxChunkSize: -1);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("maxChunkSize");
        }

        [Test]
        public void constructor_should_throw_when_maxChunkSize_is_not_a_power_of_2(
            [Values(3, 5, 6, 7, 9, 15, 17)]
            int maxChunkSize)
        {
            Action action = () => new InputBufferChunkSource(BsonChunkPool.Default, maxChunkSize: maxChunkSize);

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("maxChunkSize");
        }

        [Test]
        public void constructor_should_throw_when_baseSource_is_null()
        {
            Action action = () => new InputBufferChunkSource(null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("baseSource");
        }

        [Test]
        public void constructor_should_throw_when_maxUnpooledChunkSize_is_negative()
        {
            Action action = () => new InputBufferChunkSource(BsonChunkPool.Default, maxUnpooledChunkSize: -1);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("maxUnpooledChunkSize");
        }

        [Test]
        public void constructor_should_throw_when_minChunkSize_is_negative()
        {
            Action action = () => new InputBufferChunkSource(BsonChunkPool.Default, minChunkSize: -1);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("minChunkSize");
        }

        [Test]
        public void constructor_should_throw_when_minChunkSize_is_not_a_power_of_2(
            [Values(3, 5, 6, 7, 9, 15, 17)]
            int minChunkSize)
        {
            Action action = () => new InputBufferChunkSource(BsonChunkPool.Default, minChunkSize: minChunkSize);

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("minChunkSize");
        }

        [Test]
        public void constructor_should_use_default_value_for_maxChunkSize()
        {
            var baseSource = Substitute.For<IBsonChunkSource>();

            var subject = new InputBufferChunkSource(baseSource);

            subject.MaxChunkSize.Should().Be(1 * 1024 * 1024);
        }

        [Test]
        public void constructor_should_use_default_value_for_maxUnpooledChunkSize()
        {
            var baseSource = Substitute.For<IBsonChunkSource>();

            var subject = new InputBufferChunkSource(baseSource);

            subject.MaxUnpooledChunkSize.Should().Be(4 * 1024);
        }

        [Test]
        public void constructor_should_use_default_value_for_minChunkSize()
        {
            var baseSource = Substitute.For<IBsonChunkSource>();

            var subject = new InputBufferChunkSource(baseSource);

            subject.MinChunkSize.Should().Be(16 * 1024);
        }

        [Test]
        public void Dispose_can_be_called_more_than_once()
        {
            var baseSource = Substitute.For<IBsonChunkSource>();
            var subject = new InputBufferChunkSource(baseSource);

            subject.Dispose();
            subject.Dispose();
        }

        [Test]
        public void Dispose_should_dispose_subject()
        {
            var baseSource = Substitute.For<IBsonChunkSource>();
            var subject = new InputBufferChunkSource(baseSource);

            subject.Dispose();

            var reflector = new Reflector(subject);
            reflector._disposed.Should().BeTrue();
        }

        [Test]
        public void GetChunk_should_return_unpooled_chunk_when_requestedSize_is_less_than_or_equal_to_maxUnpooledChunkSize(
            [Values(1, 2, 4 * 1024 - 1, 4 * 1024)]
            int requestedSize)
        {
            var baseSource = Substitute.For<IBsonChunkSource>();
            var subject = new InputBufferChunkSource(baseSource);

            var result = subject.GetChunk(requestedSize);

            result.Should().BeOfType<ByteArrayChunk>();
            result.Bytes.Count.Should().Be(requestedSize);
            baseSource.DidNotReceive().GetChunk(Arg.Any<int>());
        }

        [TestCase(1, 4)]
        [TestCase(2, 4)]
        [TestCase(3, 4)]
        [TestCase(4, 4)]
        [TestCase(5, 8)]
        [TestCase(6, 8)]
        [TestCase(7, 8)]
        [TestCase(8, 8)]
        [TestCase(9, 8)]
        [TestCase(10, 8)]
        [TestCase(11, 8)]
        [TestCase(12, 16)]
        [TestCase(13, 16)]
        [TestCase(14, 16)]
        [TestCase(15, 16)]
        [TestCase(16, 16)]
        public void GetChunk_should_round_requestedSize_to_power_of_2_without_wasting_too_much_space(int requestedSize, int roundedSize)
        {
            var baseSource = Substitute.For<IBsonChunkSource>();
            var subject = new InputBufferChunkSource(baseSource, maxUnpooledChunkSize: 0, minChunkSize: 4, maxChunkSize: 16);
            baseSource.GetChunk(Arg.Any<int>()).Returns(Substitute.For<IBsonChunk>());

            subject.GetChunk(requestedSize);

            baseSource.Received(1).GetChunk(roundedSize);
        }

        [Test]
        public void GetChunk_should_throw_when_requestedSize_is_less_than_or_equal_to_zero(
            [Values(-1, 0)]
            int requestedSize)
        {
            var baseSource = Substitute.For<IBsonChunkSource>();
            var subject = new InputBufferChunkSource(baseSource);

            Action action = () => subject.GetChunk(requestedSize);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("requestedSize");
        }

        [Test]
        public void GetChunk_should_throw_when_subject_is_disposed()
        {
            var baseSource = Substitute.For<IBsonChunkSource>();
            var subject = new InputBufferChunkSource(baseSource);
            subject.Dispose();

            Action action = () => subject.GetChunk(1);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("InputBufferChunkSource");
        }

        [Test]
        public void MaxChunkSize_get_should_return_expected_result()
        {
            var baseSource = Substitute.For<IBsonChunkSource>();
            var maxChunkSize = 32 * 1024 * 1024;
            var subject = new InputBufferChunkSource(baseSource, maxChunkSize: maxChunkSize);

            var result = subject.MaxChunkSize;

            result.Should().Be(maxChunkSize);
        }

        [Test]
        public void MaxChunkUnpooledSize_get_should_return_expected_result()
        {
            var baseSource = Substitute.For<IBsonChunkSource>();
            var maxUnpooledChunkSize = 8 * 1024;
            var subject = new InputBufferChunkSource(baseSource, maxUnpooledChunkSize: maxUnpooledChunkSize);

            var result = subject.MaxUnpooledChunkSize;

            result.Should().Be(maxUnpooledChunkSize);
        }

        [Test]
        public void MinChunkSize_get_should_return_expected_result()
        {
            var baseSource = Substitute.For<IBsonChunkSource>();
            var minChunkSize = 8 * 1024;
            var subject = new InputBufferChunkSource(baseSource, minChunkSize: minChunkSize);

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
