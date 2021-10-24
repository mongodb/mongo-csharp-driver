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
using System.Reflection;
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
            var mockSession = new Mock<ICoreSessionHandle>();
            mockSession.Setup(s => s.Fork()).Returns(mockSession.Object);
            _mockReadBinding.SetupGet(b => b.Session).Returns(mockSession.Object);
            _mockWriteBinding.SetupGet(b => b.Session).Returns(mockSession.Object);
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

        [Fact]
        public void constructor_with_read_and_write_bindings_should_initialize_instance()
        {
            var session = new Mock<ICoreSessionHandle>().Object;
            var mockReadBinding = new Mock<IReadBinding>();
            var mockWriteBinding = new Mock<IWriteBinding>();
            mockReadBinding.SetupGet(m => m.Session).Returns(session);
            mockWriteBinding.SetupGet(m => m.Session).Returns(session);

            var result = new SplitReadWriteBinding(mockReadBinding.Object, mockWriteBinding.Object);

            result._readBinding().Should().BeSameAs(mockReadBinding.Object);
            result._writeBinding().Should().BeSameAs(mockWriteBinding.Object);
        }

        [Fact]
        public void constructor_with_read_and_write_bindings_should_throw_if_read_binding_is_null()
        {
            var writeBinding = new Mock<IWriteBinding>().Object;

            var exception = Record.Exception(() => new SplitReadWriteBinding(null, writeBinding));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("readBinding");
        }

        [Fact]
        public void constructor_with_read_and_write_bindings_should_throw_if_write_binding_is_null()
        {
            var readBinding = new Mock<IReadBinding>().Object;

            var exception = Record.Exception(() => new SplitReadWriteBinding(readBinding, null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("writeBinding");
        }

        [Fact]
        public void constructor_with_read_and_write_bindings_should_throw_if_bindings_have_different_sessions()
        {
            var cluster = new Mock<ICluster>().Object;
            var session1 = new Mock<ICoreSessionHandle>().Object;
            var session2 = new Mock<ICoreSessionHandle>().Object;
            var readBinding = new ReadPreferenceBinding(cluster, ReadPreference.Secondary, session1);
            var writeBinding = new WritableServerBinding(cluster, session2);

            var exception = Record.Exception(() => new SplitReadWriteBinding(readBinding, writeBinding));

            exception.Should().BeOfType<ArgumentException>();
        }

        [Fact]
        public void constructor_with_cluster_read_preference_and_session_should_initialize_instance()
        {
            var cluster = new Mock<ICluster>().Object;
            var readPreference = ReadPreference.Secondary;
            var mockSession = new Mock<ICoreSessionHandle>();
            var forkedSession = new Mock<ICoreSessionHandle>().Object;
            mockSession.Setup(m => m.Fork()).Returns(forkedSession);

            var result = new SplitReadWriteBinding(cluster, readPreference, mockSession.Object);

            var readBinding = result._readBinding().Should().BeOfType<ReadPreferenceBinding>().Subject;
            readBinding._cluster().Should().BeSameAs(cluster);
            readBinding.ReadPreference.Should().BeSameAs(readPreference);
            readBinding.Session.Should().BeSameAs(mockSession.Object);

            var writeBinding = result._writeBinding().Should().BeOfType<WritableServerBinding>().Subject;
            writeBinding._cluster().Should().BeSameAs(cluster);
            writeBinding.Session.Should().BeSameAs(forkedSession);
        }

        [Fact]
        public void constructor_with_cluster_read_preference_and_session_should_throw_when_cluster_is_null()
        {
            var readPreference = ReadPreference.Primary;
            var session = new Mock<ICoreSessionHandle>().Object;

            var exception = Record.Exception(() => new SplitReadWriteBinding(null, readPreference, session));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("cluster");
        }

        [Fact]
        public void constructor_with_cluster_read_preference_and_session_should_throw_when_readPreference_is_null()
        {
            var cluster = new Mock<ICluster>().Object;
            var session = new Mock<ICoreSessionHandle>().Object;

            var exception = Record.Exception(() => new SplitReadWriteBinding(cluster, null, session));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("readPreference");
        }

        [Fact]
        public void constructor_with_cluster_read_preference_and_session_should_throw_when_session_is_null()
        {
            var cluster = new Mock<ICluster>().Object;
            var readPreference = ReadPreference.Primary;

            var exception = Record.Exception(() => new SplitReadWriteBinding(cluster, readPreference, null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("session");
        }

        [Fact]
        public void Session_should_delegate_to_read_binding()
        {
            var session = new Mock<ICoreSessionHandle>().Object;
            var mockReadBinding = new Mock<IReadBinding>();
            var mockWriteBinding = new Mock<IWriteBinding>();
            mockReadBinding.SetupGet(m => m.Session).Returns(session);
            mockWriteBinding.SetupGet(m => m.Session).Returns(session);
            var subject = new SplitReadWriteBinding(mockReadBinding.Object, mockWriteBinding.Object);

            var result = subject.Session;

            mockReadBinding.Verify(m => m.Session, Times.Exactly(2)); // called once in the constructor also
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

    public static class SplitReadWriteBindingReflector
    {
        public static IReadBinding _readBinding(this SplitReadWriteBinding obj)
        {
            var fieldInfo = typeof(SplitReadWriteBinding).GetField("_readBinding", BindingFlags.NonPublic | BindingFlags.Instance);
            return (IReadBinding)fieldInfo.GetValue(obj);
        }

        public static IWriteBinding _writeBinding(this SplitReadWriteBinding obj)
        {
            var fieldInfo = typeof(SplitReadWriteBinding).GetField("_writeBinding", BindingFlags.NonPublic | BindingFlags.Instance);
            return (IWriteBinding)fieldInfo.GetValue(obj);
        }
    }
}
