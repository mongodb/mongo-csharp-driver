/* Copyright 2017 MongoDB Inc.
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
using MongoDB.Bson;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Bindings
{
    public class WrappingCoreSessionTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void constructor_should_initialize_instance(bool ownsWrapped)
        {
            var wrapped = new Mock<ICoreSession>().Object;

            var result = new MockWrappingCoreSession(wrapped, ownsWrapped);

            result.Wrapped.Should().BeSameAs(wrapped);
            result.IsDisposed().Should().BeFalse();
            result._ownsWrapped().Should().Be(ownsWrapped);
        }

        [Fact]
        public void constructor_should_throw_when_wrapped_is_null()
        {
            var exception = Record.Exception(() => new MockWrappingCoreSession(null, false));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("wrapped");
        }

        [Fact]
        public void ClusterTime_should_call_wrapped()
        {
            Mock<ICoreSession> mockWrapped;
            var subject = CreateSubject(out mockWrapped);

            var result = subject.ClusterTime;

            mockWrapped.Verify(m => m.ClusterTime, Times.Once);
        }

        [Fact]
        public void ClusterTime_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();

            var exception = Record.Exception(() => subject.ClusterTime);

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void Id_should_call_wrapped()
        {
            Mock<ICoreSession> mockWrapped;
            var subject = CreateSubject(out mockWrapped);

            var result = subject.Id;

            mockWrapped.Verify(m => m.Id, Times.Once);
        }

        [Fact]
        public void Id_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();

            var exception = Record.Exception(() => subject.Id);

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void IsImplicit_should_call_wrapped()
        {
            Mock<ICoreSession> mockWrapped;
            var subject = CreateSubject(out mockWrapped);

            var result = subject.IsImplicit;

            mockWrapped.Verify(m => m.IsImplicit, Times.Once);
        }

        [Fact]
        public void IsImplicit_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();

            var exception = Record.Exception(() => subject.IsImplicit);

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void OperationTime_should_call_wrapped()
        {
            Mock<ICoreSession> mockWrapped;
            var subject = CreateSubject(out mockWrapped);

            var result = subject.OperationTime;

            mockWrapped.Verify(m => m.OperationTime, Times.Once);
        }

        [Fact]
        public void OperationTime_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();

            var exception = Record.Exception(() => subject.OperationTime);

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void Wrapped_should_return_expected_result()
        {
            Mock<ICoreSession> mockWrapped;
            var subject = CreateSubject(out mockWrapped);

            var result = subject.Wrapped;

            result.Should().BeSameAs(mockWrapped.Object);
        }

        [Fact]
        public void Wrapped_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();

            var exception = Record.Exception(() => subject.Wrapped);

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void AdvanceClusterTime_should_call_wrapped()
        {
            Mock<ICoreSession> mockWrapped;
            var subject = CreateSubject(out mockWrapped);
            var newClusterTime = CreateClusterTime();

            subject.AdvanceClusterTime(newClusterTime);

            mockWrapped.Verify(m => m.AdvanceClusterTime(newClusterTime), Times.Once);
        }

        [Fact]
        public void AdvanceClusterTime_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();
            var newClusterTime = CreateClusterTime();

            var exception = Record.Exception(() => subject.AdvanceClusterTime(newClusterTime));

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void AdvanceOperationTime_should_call_wrapped()
        {
            Mock<ICoreSession> mockWrapped;
            var subject = CreateSubject(out mockWrapped);
            var newOperationTime = CreateOperationTime();

            subject.AdvanceOperationTime(newOperationTime);

            mockWrapped.Verify(m => m.AdvanceOperationTime(newOperationTime), Times.Once);
        }

        [Fact]
        public void AdvanceOperationTime_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();
            var newOperationTime = CreateOperationTime();

            var exception = Record.Exception(() => subject.AdvanceOperationTime(newOperationTime));

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void Dispose_should_call_Dispose_true()
        {
            var subject = CreateSubject();

            subject.Dispose();

            ((MockWrappingCoreSession)subject).DisposeTrueWasCalled.Should().BeTrue();
        }

        [Fact]
        public void Dispose_can_be_called_more_than_once()
        {
            Mock<ICoreSession> mockWrapped;
            var subject = CreateSubject(out mockWrapped);

            subject.Dispose();
            subject.Dispose();

            mockWrapped.Verify(m => m.Dispose(), Times.Once);
        }

        [Fact]
        public void WasUsed_should_call_wrapped()
        {
            Mock<ICoreSession> mockWrapped;
            var subject = CreateSubject(out mockWrapped);

            subject.WasUsed();

            mockWrapped.Verify(m => m.WasUsed(), Times.Once);
        }

        [Fact]
        public void WasUsed_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();

            var exception = Record.Exception(() => subject.WasUsed());

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void Dispose_bool_should_set_disposed()
        {
            var subject = CreateSubject();

            subject.Dispose();

            subject.IsDisposed().Should().BeTrue();
        }

        [Theory]
        [InlineData(false, false, false)]
        [InlineData(false, true, false)]
        [InlineData(true, false, false)]
        [InlineData(true, true, true)]
        public void Dispose_bool_should_call_wrapped_when_expected(bool ownsWrapped, bool disposing, bool disposeExpected)
        {
            Mock<ICoreSession> mockWrapped;
            var subject = CreateSubject(ownsWrapped, out mockWrapped);

            subject.Dispose(disposing);

            mockWrapped.Verify(m => m.Dispose(), Times.Exactly(disposeExpected ? 1 : 0));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void IsDisposed_should_return_expected_result(bool disposed)
        {
            var subject = CreateSubject();
            if (disposed)
            {
                subject.Dispose();
            }

            var result = subject.IsDisposed();

            result.Should().Be(disposed);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ThrowIfDisposed_should_throw_when_disposed(bool disposed)
        {
            var subject = CreateSubject();
            if (disposed)
            {
                subject.Dispose();
            }

            var exception = Record.Exception(() => subject.ThrowIfDisposed());

            if (disposed)
            {
                var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
                e.ObjectName.Should().Be(subject.GetType().FullName);
            }
            else
            {
                exception.Should().BeNull();
            }
        }

        // private methods
        private BsonDocument CreateClusterTime()
        {
            return new BsonDocument
            {
                { "xyz", 1 },
                { "clusterTime", new BsonTimestamp(1L) }
            };
        }

        private WrappingCoreSession CreateDisposedSubject()
        {
            var subject = CreateSubject();
            subject.Dispose();
            return subject;
        }

        private BsonTimestamp CreateOperationTime()
        {
            return new BsonTimestamp(1L);
        }

        private WrappingCoreSession CreateSubject()
        {
            Mock<ICoreSession> mockWrapped;
            return CreateSubject(out mockWrapped);
        }

        private WrappingCoreSession CreateSubject(out Mock<ICoreSession> mockWrapped)
        {
            return CreateSubject(true, out mockWrapped);
        }

        private WrappingCoreSession CreateSubject(bool ownsWrapped, out Mock<ICoreSession> mockWrapped)
        {
            mockWrapped = new Mock<ICoreSession>();
            return new MockWrappingCoreSession(mockWrapped.Object, ownsWrapped);
        }

        // nested types
        private class MockWrappingCoreSession : WrappingCoreSession
        {
            public MockWrappingCoreSession(ICoreSession wrapped, bool ownsWrapped)
                : base(wrapped, ownsWrapped)
            {
            }

            public bool DisposeTrueWasCalled { get; private set; }

            protected override void Dispose(bool disposing)
            {
                DisposeTrueWasCalled = true;
                base.Dispose(disposing);
            }
        }
    }

    public static class WrappingCoreSessionReflector
    {
        public static bool _ownsWrapped(this WrappingCoreSession obj)
        {
            var fieldInfo = typeof(WrappingCoreSession).GetField("_ownsWrapped", BindingFlags.NonPublic | BindingFlags.Instance);
            return (bool)fieldInfo.GetValue(obj);
        }

        public static void Dispose(this WrappingCoreSession obj, bool disposing)
        {
            var methodInfo = typeof(WrappingCoreSession).GetMethod("Dispose", BindingFlags.NonPublic | BindingFlags.Instance);
            methodInfo.Invoke(obj, new object[] { disposing });
        }

        public static bool IsDisposed(this WrappingCoreSession obj)
        {
            var methodInfo = typeof(WrappingCoreSession).GetMethod("IsDisposed", BindingFlags.NonPublic | BindingFlags.Instance);
            return (bool)methodInfo.Invoke(obj, new object[] { });
        }

        public static void ThrowIfDisposed(this WrappingCoreSession obj)
        {
            try
            {
                var methodInfo = typeof(WrappingCoreSession).GetMethod("ThrowIfDisposed", BindingFlags.NonPublic | BindingFlags.Instance);
                methodInfo.Invoke(obj, new object[] { });
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }
    }
}
