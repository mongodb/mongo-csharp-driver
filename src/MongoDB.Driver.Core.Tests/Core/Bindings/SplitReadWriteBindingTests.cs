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
using MongoDB.Driver.Core.Clusters;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Bindings
{
    public class SplitReadWriteBindingTests
    {
        private Mock<IReadBinding> _mockReadBinding;
        private Mock<IWriteBinding> _mockWriteBinding;

        public SplitReadWriteBindingTests()
        {
            _mockReadBinding = new Mock<IReadBinding>();
            _mockWriteBinding = new Mock<IWriteBinding>();
        }

        [Fact]
        public void Constructor_should_throw_if_readBinding_is_null()
        {
            Action act = () => new SplitReadWriteBinding(null, _mockWriteBinding.Object);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_if_readPreference_is_null()
        {
            Action act = () => new SplitReadWriteBinding(_mockReadBinding.Object, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void GetReadChannelSource_should_throw_if_disposed(
            [Values(false, true)]
            bool async)
        {
            var subject = new SplitReadWriteBinding(_mockReadBinding.Object, _mockWriteBinding.Object);
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
        public void GetReadChannelSource_should_get_the_connection_source_from_the_read_binding(
            [Values(false, true)]
            bool async)
        {
            var subject = new SplitReadWriteBinding(_mockReadBinding.Object, _mockWriteBinding.Object);

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

        [Theory]
        [ParameterAttributeData]
        public void GetWriteChannelSource_should_throw_if_disposed(
            [Values(false, true)]
            bool async)
        {
            var subject = new SplitReadWriteBinding(_mockReadBinding.Object, _mockWriteBinding.Object);
            subject.Dispose();

            Action act;
            if (async)
            {
                act = () => subject.GetWriteChannelSourceAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => subject.GetWriteChannelSource(CancellationToken.None);
            }

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void GetWriteChannelSource_should_get_the_connection_source_from_the_write_binding(
            [Values(false, true)]
            bool async)
        {
            var subject = new SplitReadWriteBinding(_mockReadBinding.Object, _mockWriteBinding.Object);

            if (async)
            {
                subject.GetWriteChannelSourceAsync(CancellationToken.None).GetAwaiter().GetResult();

                _mockWriteBinding.Verify(b => b.GetWriteChannelSourceAsync(CancellationToken.None), Times.Once);
            }
            else
            {
                subject.GetWriteChannelSource(CancellationToken.None);

                _mockWriteBinding.Verify(b => b.GetWriteChannelSource(CancellationToken.None), Times.Once);
            }
        }

        [Fact]
        public void Dispose_should_call_dispose_on_read_binding_and_write_binding()
        {
            var subject = new SplitReadWriteBinding(_mockReadBinding.Object, _mockWriteBinding.Object);

            subject.Dispose();

            _mockReadBinding.Verify(b => b.Dispose(), Times.Once);
            _mockWriteBinding.Verify(b => b.Dispose(), Times.Once);
        }
    }
}