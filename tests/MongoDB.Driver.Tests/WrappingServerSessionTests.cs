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
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class WrappingServerSessionTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void constructor_should_initialize_instance(bool ownsWrapped)
        {
            var wrapped = new Mock<IServerSession>().Object;

            var result = new MockWrappingServerSession(wrapped, ownsWrapped);

            result.Wrapped.Should().BeSameAs(wrapped);
            result._disposed().Should().BeFalse();
            result._ownsWrapped().Should().Be(ownsWrapped);
        }

        [Fact]
        public void constructor_should_throw_when_wrapped_is_null()
        {
            var exception = Record.Exception(() => new MockWrappingServerSession(null, true));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("wrapped");
        }

        [Fact]
        public void Id_should_call_wrapped()
        {
            Mock<IServerSession> mockWrapped;
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
        public void LastUsedAt_should_call_wrapped()
        {
            Mock<IServerSession> mockWrapped;
            var subject = CreateSubject(out mockWrapped);

            var result = subject.LastUsedAt;

            mockWrapped.Verify(m => m.LastUsedAt, Times.Once);
        }

        [Fact]
        public void LastUsedAt_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();

            var exception = Record.Exception(() => subject.LastUsedAt);

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);

        }

        [Fact]
        public void Wrapped_should_return_expected_result()
        {
            Mock<IServerSession> mockWrapped;
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
        public void Dispose_should_call_Dispose_true()
        {
            var subject = CreateSubject();

            subject.Dispose();

            ((MockWrappingServerSession)subject).DisposeTrueWasCalled.Should().BeTrue();
        }

        [Fact]
        public void Dispose_can_be_called_more_than_once()
        {
            Mock<IServerSession> mockWrapped;
            var subject = CreateSubject(out mockWrapped);

            subject.Dispose();
            subject.Dispose();

            mockWrapped.Verify(m => m.Dispose(), Times.Once);
        }

        [Fact]
        public void WasUsed_should_call_wrapped()
        {
            Mock<IServerSession> mockWrapped;
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

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Dispose_with_disposing_should_set_disposed(bool disposing)
        {
            var subject = CreateSubject();

            subject.Dispose(disposing);

            subject._disposed().Should().BeTrue();
        }

        [Theory]
        [InlineData(false, false, false)]
        [InlineData(false, true, false)]
        [InlineData(true, false, false)]
        [InlineData(true, true, true)]
        public void Dispose_with_disposing_should_dispose_wrapped_when_appropriate(bool ownsWrapped, bool disposing, bool shouldDispose)
        {
            var mockWrapped = new Mock<IServerSession>();
            var subject = new MockWrappingServerSession(mockWrapped.Object, ownsWrapped);

            subject.Dispose(disposing);

            mockWrapped.Verify(m => m.Dispose(), Times.Exactly(shouldDispose ? 1 : 0));
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
        }

        // private methods
        private WrappingServerSession CreateDisposedSubject()
        {
            var subject = CreateSubject();
            subject.Dispose();
            return subject;
        }

        private WrappingServerSession CreateSubject()
        {
            var wrapped = new Mock<IServerSession>().Object;
            return new MockWrappingServerSession(wrapped, true);
        }

        private WrappingServerSession CreateSubject(out Mock<IServerSession> mockWrapped)
        {
            mockWrapped = new Mock<IServerSession>();
            return new MockWrappingServerSession(mockWrapped.Object, true);
        }

        // nested types
        private class MockWrappingServerSession : WrappingServerSession
        {
            public MockWrappingServerSession(IServerSession wrapped, bool ownsWrapped)
                : base(wrapped, ownsWrapped)
            {
            }

            public bool DisposeTrueWasCalled { get; private set; }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    DisposeTrueWasCalled = true;
                }
                base.Dispose(disposing);
            }
        }
    }

    internal static class WrappingServerSessionReflector
    {
        public static bool _disposed(this WrappingServerSession obj)
        {
            var fieldInfo = typeof(WrappingServerSession).GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance);
            return (bool)fieldInfo.GetValue(obj);
        }

        public static bool _ownsWrapped(this WrappingServerSession obj)
        {
            var fieldInfo = typeof(WrappingServerSession).GetField("_ownsWrapped", BindingFlags.NonPublic | BindingFlags.Instance);
            return (bool)fieldInfo.GetValue(obj);
        }

        public static void Dispose(this WrappingServerSession obj, bool disposing)
        {
            var methodInfo = typeof(WrappingServerSession).GetMethod("Dispose", BindingFlags.NonPublic | BindingFlags.Instance);
            methodInfo.Invoke(obj, new object[] { disposing });
        }

        public static void ThrowIfDisposed(this WrappingServerSession obj)
        {
            try
            {
                var methodInfo = typeof(WrappingServerSession).GetMethod("ThrowIfDisposed", BindingFlags.NonPublic | BindingFlags.Instance);
                methodInfo.Invoke(obj, new object[] { });
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }
    }
}
