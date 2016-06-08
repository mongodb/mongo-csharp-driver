/* Copyright 2013-2016 MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Bindings
{
    public class ReadBindingHandleTests
    {
        private Mock<IReadBinding> _mockReadBinding;

        public ReadBindingHandleTests()
        {
            _mockReadBinding = new Mock<IReadBinding>();
        }

        [Fact]
        public void Constructor_should_throw_if_readWriteBinding_is_null()
        {
            Action act = () => new ReadBindingHandle(null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void GetReadChannelSource_should_throw_if_disposed(
            [Values(false, true)]
            bool async)
        {
            var subject = new ReadBindingHandle(_mockReadBinding.Object);
            subject.Dispose();

            Action act;
            if (async)
            {
                act = () => subject.GetReadChannelSourceAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => subject.GetReadChannelSource(CancellationToken.None);
            }

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void GetReadChannelSource_should_delegate_to_reference(
            [Values(false, true)]
            bool async)
        {
            var subject = new ReadBindingHandle(_mockReadBinding.Object);

            if (async)
            {
                subject.GetReadChannelSourceAsync(CancellationToken.None).GetAwaiter().GetResult();

                _mockReadBinding.Verify(b => b.GetReadChannelSourceAsync(CancellationToken.None), Times.Once);
            }
            else
            {
                subject.GetReadChannelSource(CancellationToken.None);

                _mockReadBinding.Verify(b => b.GetReadChannelSource(CancellationToken.None), Times.Once);
            }
        }

        [Fact]
        public void Fork_should_throw_if_disposed()
        {
            var subject = new ReadBindingHandle(_mockReadBinding.Object);
            subject.Dispose();

            Action act = () => subject.Fork();

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Fact]
        public void Disposing_of_handle_after_fork_should_not_dispose_of_channelSource()
        {
            var subject = new ReadBindingHandle(_mockReadBinding.Object);

            var forked = subject.Fork();

            subject.Dispose();

            _mockReadBinding.Verify(b => b.Dispose(), Times.Never);

            forked.Dispose();

            _mockReadBinding.Verify(b => b.Dispose(), Times.Once);
        }

        [Fact]
        public void Disposing_of_fork_before_disposing_of_subject_hould_not_dispose_of_channelSource()
        {
            var subject = new ReadBindingHandle(_mockReadBinding.Object);

            var forked = subject.Fork();

            forked.Dispose();

            _mockReadBinding.Verify(b => b.Dispose(), Times.Never);

            subject.Dispose();

            _mockReadBinding.Verify(b => b.Dispose(), Times.Once);
        }

        [Fact]
        public void Disposing_of_last_handle_should_dispose_of_connectioSource()
        {
            var subject = new ReadBindingHandle(_mockReadBinding.Object);

            var forked = subject.Fork();

            subject.Dispose();
            forked.Dispose();

            _mockReadBinding.Verify(b => b.Dispose(), Times.Once);
        }
    }
}