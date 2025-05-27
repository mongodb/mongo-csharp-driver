/* Copyright 2013-present MongoDB Inc.
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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.TestHelpers.XunitExtensions;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Bindings
{
    public class ReadWriteBindingHandleTests
    {
        private Mock<IReadWriteBinding> _mockReadWriteBinding;

        public ReadWriteBindingHandleTests()
        {
            _mockReadWriteBinding = new Mock<IReadWriteBinding>();
        }

        [Fact]
        public void Constructor_should_throw_if_readWriteBinding_is_null()
        {
            Action act = () => new ReadWriteBindingHandle(null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Session_should_delegate_to_reference()
        {
            var subject = new ReadWriteBindingHandle(_mockReadWriteBinding.Object);

            var result = subject.Session;

            _mockReadWriteBinding.Verify(m => m.Session, Times.Once);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task GetReadChannelSource_should_throw_if_disposed(
            [Values(false, true)]
            bool async)
        {
            var subject = new ReadWriteBindingHandle(_mockReadWriteBinding.Object);
            subject.Dispose();

            var exception = async ?
                await Record.ExceptionAsync(() => subject.GetReadChannelSourceAsync(OperationCancellationContext.NoTimeout)) :
                Record.Exception(() => subject.GetReadChannelSource(OperationCancellationContext.NoTimeout));

            exception.Should().BeOfType<ObjectDisposedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task GetReadChannelSource_should_delegate_to_reference(
            [Values(false, true)]
            bool async)
        {
            var subject = new ReadWriteBindingHandle(_mockReadWriteBinding.Object);

            if (async)
            {
                await subject.GetReadChannelSourceAsync(OperationCancellationContext.NoTimeout);

                _mockReadWriteBinding.Verify(b => b.GetReadChannelSourceAsync(It.IsAny<OperationCancellationContext>()), Times.Once);
            }
            else
            {
                subject.GetReadChannelSource(OperationCancellationContext.NoTimeout);

                _mockReadWriteBinding.Verify(b => b.GetReadChannelSource(It.IsAny<OperationCancellationContext>()), Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public async Task GetWriteChannelSource_should_throw_if_disposed(
            [Values(false, true)]
            bool async)
        {
            var subject = new ReadWriteBindingHandle(_mockReadWriteBinding.Object);
            subject.Dispose();

            var exception = async ?
                await Record.ExceptionAsync(() => subject.GetWriteChannelSourceAsync(OperationCancellationContext.NoTimeout)) :
                Record.Exception(() => subject.GetWriteChannelSource(OperationCancellationContext.NoTimeout));

            exception.Should().BeOfType<ObjectDisposedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task GetWriteChannelSource_should_delegate_to_reference(
            [Values(false, true)]
            bool async)
        {
            var subject = new ReadWriteBindingHandle(_mockReadWriteBinding.Object);

            if (async)
            {
                await subject.GetWriteChannelSourceAsync(OperationCancellationContext.NoTimeout);

                _mockReadWriteBinding.Verify(b => b.GetWriteChannelSourceAsync(It.IsAny<OperationCancellationContext>()), Times.Once);
            }
            else
            {
                subject.GetWriteChannelSource(OperationCancellationContext.NoTimeout);

                _mockReadWriteBinding.Verify(b => b.GetWriteChannelSource(It.IsAny<OperationCancellationContext>()), Times.Once);
            }
        }

        [Fact]
        public void Fork_should_throw_if_disposed()
        {
            var subject = new ReadWriteBindingHandle(_mockReadWriteBinding.Object);
            subject.Dispose();

            Action act = () => subject.Fork();

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Fact]
        public void Disposing_of_handle_after_fork_should_not_dispose_of_channelSource()
        {
            var subject = new ReadWriteBindingHandle(_mockReadWriteBinding.Object);

            var forked = subject.Fork();

            subject.Dispose();

            _mockReadWriteBinding.Verify(b => b.Dispose(), Times.Never);

            forked.Dispose();

            _mockReadWriteBinding.Verify(b => b.Dispose(), Times.Once);
        }

        [Fact]
        public void Disposing_of_fork_before_disposing_of_subject_hould_not_dispose_of_channelSource()
        {
            var subject = new ReadWriteBindingHandle(_mockReadWriteBinding.Object);

            var forked = subject.Fork();

            forked.Dispose();

            _mockReadWriteBinding.Verify(b => b.Dispose(), Times.Never);

            subject.Dispose();

            _mockReadWriteBinding.Verify(b => b.Dispose(), Times.Once);
        }

        [Fact]
        public void Disposing_of_last_handle_should_dispose_of_connectioSource()
        {
            var subject = new ReadWriteBindingHandle(_mockReadWriteBinding.Object);

            var forked = subject.Fork();

            subject.Dispose();
            forked.Dispose();

            _mockReadWriteBinding.Verify(b => b.Dispose(), Times.Once);
        }
    }
}
