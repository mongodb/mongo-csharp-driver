﻿/* Copyright 2013-present MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Misc;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Bindings
{
    public class ChannelSourceHandleTests
    {
        private Mock<IChannelSource> _mockChannelSource;

        public ChannelSourceHandleTests()
        {
            _mockChannelSource = new Mock<IChannelSource>();
        }

        [Fact]
        public void Constructor_should_throw_if_channelSource_is_null()
        {
            Action act = () => new ChannelSourceHandle(null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Session_should_delegate_to_reference()
        {
            var subject = new ChannelSourceHandle(_mockChannelSource.Object);

            var result = subject.Session;

            _mockChannelSource.Verify(m => m.Session, Times.Once);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task GetChannel_should_throw_if_disposed(
            [Values(false, true)]
            bool async)
        {
            var subject = new ChannelSourceHandle(_mockChannelSource.Object);
            subject.Dispose();

            var exception = async ?
                await Record.ExceptionAsync(() => subject.GetChannelAsync(OperationContext.NoTimeout)) :
                Record.Exception(() => subject.GetChannel(OperationContext.NoTimeout));

            exception.Should().BeOfType<ObjectDisposedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task GetChannel_should_delegate_to_reference(
            [Values(false, true)]
            bool async)
        {
            var subject = new ChannelSourceHandle(_mockChannelSource.Object);

            if (async)
            {
                await subject.GetChannelAsync(OperationContext.NoTimeout);

                _mockChannelSource.Verify(s => s.GetChannelAsync(It.IsAny<OperationContext>()), Times.Once);
            }
            else
            {
                subject.GetChannel(OperationContext.NoTimeout);

                _mockChannelSource.Verify(s => s.GetChannel(It.IsAny<OperationContext>()), Times.Once);
            }
        }

        [Fact]
        public void Fork_should_throw_if_disposed()
        {
            var subject = new ChannelSourceHandle(_mockChannelSource.Object);
            subject.Dispose();

            Action act = () => subject.Fork();

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Fact]
        public void Disposing_of_handle_after_fork_should_not_dispose_of_channelSource()
        {
            var subject = new ChannelSourceHandle(_mockChannelSource.Object);

            var forked = subject.Fork();

            subject.Dispose();

            _mockChannelSource.Verify(s => s.Dispose(), Times.Never);

            forked.Dispose();

            _mockChannelSource.Verify(s => s.Dispose(), Times.Once);
        }

        [Fact]
        public void Disposing_of_fork_before_disposing_of_subject_hould_not_dispose_of_channelSource()
        {
            var subject = new ChannelSourceHandle(_mockChannelSource.Object);

            var forked = subject.Fork();

            forked.Dispose();

            _mockChannelSource.Verify(s => s.Dispose(), Times.Never);

            subject.Dispose();

            _mockChannelSource.Verify(s => s.Dispose(), Times.Once);
        }

        [Fact]
        public void Disposing_of_last_handle_should_dispose_of_connectioSource()
        {
            var subject = new ChannelSourceHandle(_mockChannelSource.Object);

            var forked = subject.Fork();

            subject.Dispose();
            forked.Dispose();

            _mockChannelSource.Verify(s => s.Dispose(), Times.Once);
        }
    }

    internal static class ChannelSourceHandleReflector
    {
        // private fields
        public static bool _disposed(this ChannelSourceHandle obj) => (bool)Reflector.GetFieldValue(obj, nameof(_disposed));
        public static ReferenceCounted<IChannelSource> _reference(this ChannelSourceHandle obj) => (ReferenceCounted<IChannelSource>)Reflector.GetFieldValue(obj, nameof(_reference));
    }
}
