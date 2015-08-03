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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson.IO;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Bson.Tests.IO
{
    [TestFixture]
    public class OutputBufferChunkSourceTests
    {
        [Test]
        public void BaseSource_get_should_return_expected_result()
        {
            var baseSource = Substitute.For<IBsonChunkSource>();
            var subject = new OutputBufferChunkSource(baseSource);

            var result = subject.BaseSource;

            result.Should().BeSameAs(baseSource);
        }

        [Test]
        public void BaseSource_get_should_throw_when_subject_is_disposed()
        {
            var baseSource = Substitute.For<IBsonChunkSource>();
            var subject = new OutputBufferChunkSource(baseSource);
            subject.Dispose();

            Action action = () => { var _ = subject.BaseSource; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("OutputBufferChunkSource");
        }

        [Test]
        public void constructor_should_initialize_subject()
        {
            var baseSource = Substitute.For<IBsonChunkSource>();
            var initialUnpooledChunkSize = 2;
            var minChunkSize = 4;
            var maxChunkSize = 8;

            var subject = new OutputBufferChunkSource(baseSource, initialUnpooledChunkSize, minChunkSize, maxChunkSize);

            var reflector = new Reflector(subject);
            subject.BaseSource.Should().BeSameAs(baseSource);
            subject.InitialUnpooledChunkSize.Should().Be(initialUnpooledChunkSize);
            subject.MaxChunkSize.Should().Be(maxChunkSize);
            subject.MinChunkSize.Should().Be(minChunkSize);
            reflector._disposed.Should().BeFalse();
        }

        [Test]
        public void constructor_should_throw_when_baseSource_is_null()
        {
            Action action = () => new OutputBufferChunkSource(null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("baseSource");
        }

        [Test]
        public void constructor_should_throw_when_initialUnpooledChunkSize_is_less_than_zero()
        {
            var baseSource = Substitute.For<IBsonChunkSource>();

            Action action = () => new OutputBufferChunkSource(baseSource, initialUnpooledChunkSize: -1);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("initialUnpooledChunkSize");
        }

        [Test]
        public void constructor_should_throw_when_maxChunkSize_is_less_than_or_equal_to_zero(
            [Values(-1, 0)]
            int maxChunkSize)
        {
            var baseSource = Substitute.For<IBsonChunkSource>();

            Action action = () => new OutputBufferChunkSource(baseSource, maxChunkSize: -1);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("maxChunkSize");
        }

        [Test]
        public void constructor_should_throw_when_maxChunkSize_is_less_than_minChunkSize()
        {
            var baseSource = Substitute.For<IBsonChunkSource>();

            Action action = () => new OutputBufferChunkSource(baseSource, minChunkSize: 8, maxChunkSize: 4);

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("maxChunkSize");
        }

        [Test]
        public void constructor_should_throw_when_maxChunkSize_is_not_a_power_of_2(
            [Values(3, 5, 7, 9, 15, 17)]
            int maxChunkSize)
        {
            var baseSource = Substitute.For<IBsonChunkSource>();

            Action action = () => new OutputBufferChunkSource(baseSource, maxChunkSize: -1);

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("maxChunkSize");
        }

        [Test]
        public void constructor_should_throw_when_minChunkSize_is_less_than_or_equal_to_zero(
            [Values(-1, 0)]
            int minChunkSize)
        {
            var baseSource = Substitute.For<IBsonChunkSource>();

            Action action = () => new OutputBufferChunkSource(baseSource, minChunkSize: -1);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("minChunkSize");
        }

        [Test]
        public void constructor_should_throw_when_minChunkSize_is_not_a_power_of_2(
            [Values(3, 5, 7, 9, 15, 17)]
            int minChunkSize)
        {
            var baseSource = Substitute.For<IBsonChunkSource>();

            Action action = () => new OutputBufferChunkSource(baseSource, minChunkSize: -1);

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("minChunkSize");
        }

        [Test]
        public void Dispose_can_be_called_more_than_once()
        {
            var baseSource = Substitute.For<IBsonChunkSource>();
            var subject = new OutputBufferChunkSource(baseSource);

            subject.Dispose();
            subject.Dispose();

        }

        [Test]
        public void Dispose_should_dispose_subject()
        {
            var baseSource = Substitute.For<IBsonChunkSource>();
            var subject = new OutputBufferChunkSource(baseSource);

            subject.Dispose();

            var reflector = new Reflector(subject);
            reflector._disposed.Should().BeTrue();

        }

        [TestCase(1, new int[] { })]
        [TestCase(2, new int[] { 4 })]
        [TestCase(3, new int[] { 4, 8 })]
        [TestCase(4, new int[] { 4, 8, 16 })]
        [TestCase(5, new int[] { 4, 8, 16, 32 })]
        [TestCase(6, new int[] { 4, 8, 16, 32, 32 })]
        [TestCase(7, new int[] { 4, 8, 16, 32, 32, 32 })]
        public void GetChunk_should_return_expected_result_when_initialUnpooledChunkSize_is_not_zero(int numberOfCalls, int[] expectedRequestSizes)
        {
            var baseSource = Substitute.For<IBsonChunkSource>();
            baseSource.GetChunk(Arg.Any<int>()).Returns(x =>
            {
                var requestedSize = (int)x[0];
                return new ByteArrayChunk(requestedSize);
            });
            var subject = new OutputBufferChunkSource(baseSource, initialUnpooledChunkSize: 2, minChunkSize: 4, maxChunkSize: 32);

            var result = subject.GetChunk(1);
            result.Bytes.Count.Should().Be(2);

            for (var n = 0; n < numberOfCalls - 1; n++)
            {
                result = subject.GetChunk(1);
                result.Bytes.Count.Should().Be(expectedRequestSizes[n]);
            }

            baseSource.Received(numberOfCalls - 1).GetChunk(Arg.Any<int>());
            foreach (var expectedRequestSize in expectedRequestSizes)
            {
                var requiredNumberOfCalls = expectedRequestSizes.Count(s => s == expectedRequestSize);
                baseSource.Received(requiredNumberOfCalls).GetChunk(expectedRequestSize);
            }
        }

        [TestCase(1, new int[] { 4 })]
        [TestCase(2, new int[] { 4, 8 })]
        [TestCase(3, new int[] { 4, 8, 16 })]
        [TestCase(4, new int[] { 4, 8, 16, 32 })]
        [TestCase(5, new int[] { 4, 8, 16, 32, 32 })]
        [TestCase(6, new int[] { 4, 8, 16, 32, 32, 32 })]
        public void GetChunk_should_return_expected_result_when_initialUnpooledChunkSize_is_zero(int numberOfCalls, int[] expectedRequestSizes)
        {
            var baseSource = Substitute.For<IBsonChunkSource>();
            baseSource.GetChunk(Arg.Any<int>()).Returns(x =>
            {
                var requestedSize = (int)x[0];
                return new ByteArrayChunk(requestedSize);
            });
            var subject = new OutputBufferChunkSource(baseSource, initialUnpooledChunkSize: 0, minChunkSize: 4, maxChunkSize: 32);

            for (var n = 0; n < numberOfCalls; n++)
            {
                var result = subject.GetChunk(1);
                result.Bytes.Count.Should().Be(expectedRequestSizes[n]);
            }

            baseSource.Received(numberOfCalls).GetChunk(Arg.Any<int>());
            foreach (var expectedRequestSize in expectedRequestSizes)
            {
                var requiredNumberOfCalls = expectedRequestSizes.Count(s => s == expectedRequestSize);
                baseSource.Received(requiredNumberOfCalls).GetChunk(expectedRequestSize);
            }
        }

        [Test]
        public void GetChunk_should_throw_when_requestedSize_is_less_than_or_equal_to_zero(
            [Values(-1, 0)]
            int requestedSize)
        {
            var baseSource = Substitute.For<IBsonChunkSource>();
            var subject = new OutputBufferChunkSource(baseSource);

            Action action = () => subject.GetChunk(requestedSize);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("requestedSize");
        }

        [Test]
        public void GetChunk_should_throw_when_subject_is_disposed()
        {
            var baseSource = Substitute.For<IBsonChunkSource>();
            var subject = new OutputBufferChunkSource(baseSource);
            subject.Dispose();

            Action action = () => subject.GetChunk(1);
 
            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("OutputBufferChunkSource");
       }

        [Test]
        public void InitialUnpooledChunkSize_get_should_return_expected_result()
        {
            var baseSource = Substitute.For<IBsonChunkSource>();
            var initialUnpooledChunkSize = Reflector.DefaultInitialUnpooledChunkSize * 2;
            var subject = new OutputBufferChunkSource(baseSource, initialUnpooledChunkSize: initialUnpooledChunkSize);

            var result = subject.InitialUnpooledChunkSize;

            result.Should().Be(initialUnpooledChunkSize);
        }

        [Test]
        public void MaxChunkSize_get_should_return_expected_result()
        {
            var baseSource = Substitute.For<IBsonChunkSource>();
            var maxChunkSize = Reflector.DefaultMaxChunkSize * 2;
            var subject = new OutputBufferChunkSource(baseSource, maxChunkSize: maxChunkSize);

            var result = subject.MaxChunkSize;

            result.Should().Be(maxChunkSize);
        }

        [Test]
        public void MinChunkSize_get_should_return_expected_result()
        {
            var baseSource = Substitute.For<IBsonChunkSource>();
            var minChunkSize = Reflector.DefaultMinChunkSize * 2;
            var subject = new OutputBufferChunkSource(baseSource, minChunkSize: minChunkSize);

            var result = subject.MinChunkSize;

            result.Should().Be(minChunkSize);
        }

        // nested types
        private class Reflector
        {
            #region static
            public static int DefaultInitialUnpooledChunkSize
            {
                get
                {
                    var field = typeof(OutputBufferChunkSource).GetField("DefaultInitialUnpooledChunkSize", BindingFlags.NonPublic | BindingFlags.Static);
                    return (int)field.GetValue(null);
                }
            }

            public static int DefaultMaxChunkSize
            {
                get
                {
                    var field = typeof(OutputBufferChunkSource).GetField("DefaultMaxChunkSize", BindingFlags.NonPublic | BindingFlags.Static);
                    return (int)field.GetValue(null);
                }
            }

            public static int DefaultMinChunkSize
            {
                get
                {
                    var field = typeof(OutputBufferChunkSource).GetField("DefaultMinChunkSize", BindingFlags.NonPublic | BindingFlags.Static);
                    return (int)field.GetValue(null);
                }
            }
            #endregion

            private readonly OutputBufferChunkSource _instance;

            public Reflector(OutputBufferChunkSource instance)
            {
                _instance = instance;
            }

            public bool _disposed
            {
                get
                {
                    var field = typeof(OutputBufferChunkSource).GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance);
                    return (bool)field.GetValue(_instance);
                }
            }
        }
    }
}
