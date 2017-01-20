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
using Xunit;

namespace MongoDB.Bson.Tests.IO
{
    public class ByteArrayChunkTests
    {
        [Fact]
        public void Bytes_get_should_return_expected_result()
        {
            var size = 1;
            var bytes = new byte[size];
            var subject = new ByteArrayChunk(bytes);

            var result = subject.Bytes;

            result.Array.Should().BeSameAs(bytes);
            result.Offset.Should().Be(0);
            result.Count.Should().Be(size);
        }

        [Fact]
        public void Bytes_get_should_throw_when_subject_is_disposed()
        {
            var subject = new ByteArrayChunk(1);
            subject.Dispose();

            Action action = () => { var _ = subject.Bytes; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteArrayChunk");
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_with_bytes_should_initialize_subject(
            [Values(1, 2, 16)]
            int size)
        {
            var bytes = new byte[size];

            var subject = new ByteArrayChunk(bytes);

            var segment = subject.Bytes;
            segment.Array.Should().BeSameAs(bytes);
            segment.Offset.Should().Be(0);
            segment.Count.Should().Be(size);
        }

        [Fact]
        public void constructor_with_bytes_should_throw_when_bytes_is_null()
        {
            Action action = () => new ByteArrayChunk(null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("bytes");
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_with_size_should_initialize_subject(
            [Values(1, 2, 16)]
            int size)
        {
            var subject = new ByteArrayChunk(size);

            var segment = subject.Bytes;
            segment.Array.Should().NotBeNull();
            segment.Offset.Should().Be(0);
            segment.Count.Should().Be(size);
        }

        [Fact]
        public void constructor_with_size_should_throw_when_size_is_less_than_zero()
        {
            Action action = () => new ByteArrayChunk(-1);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("size");
        }

        [Fact]
        public void Disposed_can_be_called_more_than_once()
        {
            var subject = new ByteArrayChunk(1);

            subject.Dispose();
            subject.Dispose();
        }

        [Fact]
        public void Dispose_forked_handle_should_not_dispose_subject()
        {
            var subject = new ByteArrayChunk(1);
            var forked = subject.Fork();

            forked.Dispose();

            var reflector = new Reflector(subject);
            reflector._disposed.Should().BeFalse();
        }

        [Fact]
        public void Dispose_should_dispose_subject()
        {
            var subject = new ByteArrayChunk(1);

            subject.Dispose();

            var reflector = new Reflector(subject);
            reflector._disposed.Should().BeTrue();
        }

        [Fact]
        public void Dispose_should_not_dispose_forked_handle()
        {
            var subject = new ByteArrayChunk(1);
            var forked = subject.Fork();

            subject.Dispose();

            var reflector = new Reflector((ByteArrayChunk)forked);
            reflector._disposed.Should().BeFalse();
        }

        [Fact]
        public void Fork_should_return_a_new_handle()
        {
            var subject = new ByteArrayChunk(1);

            var result = subject.Fork();

            result.Should().NotBeSameAs(subject);
            var subjectSegment = subject.Bytes;
            var resultSegment = result.Bytes;
            resultSegment.Array.Should().BeSameAs(subjectSegment.Array);
            resultSegment.Offset.Should().Be(subjectSegment.Offset);
            resultSegment.Count.Should().Be(subjectSegment.Count);
        }

        [Fact]
        public void Fork_should_throw_when_subject_is_disposed()
        {
            var subject = new ByteArrayChunk(1);
            subject.Dispose();

            Action action = () => subject.Fork();

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteArrayChunk");
        }

        // nested types
        private class Reflector
        {
            private readonly ByteArrayChunk _instance;

            public Reflector(ByteArrayChunk instance)
            {
                _instance = instance;
            }

            public bool _disposed
            {
                get
                {
                    var field = typeof(ByteArrayChunk).GetField("_disposed", BindingFlags.Instance | BindingFlags.NonPublic);
                    return (bool)field.GetValue(_instance);
                }
            }
        }
    }
}
