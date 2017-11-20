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
    public class ReferenceCountedClientSessionTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var wrapped = new Mock<IClientSession>().Object;

            var result = new ReferenceCountedClientSession(wrapped);

            result.Wrapped.Should().BeSameAs(wrapped);
            result._referenceCount().Should().Be(1);
        }

        [Fact]
        public void DecrementReferenceCount_should_decrement_reference_count()
        {
            var subject = CreateSubject();
            var originalReferenceCount = subject._referenceCount();

            subject.DecrementReferenceCount();

            subject._referenceCount().Should().Be(originalReferenceCount - 1);
        }

        [Fact]
        public void DecrementReferenceCount_should_call_Dispose_when_reference_count_reaches_zero()
        {
            var subject = CreateSubject();

            subject.DecrementReferenceCount();

            subject.IsDisposed().Should().BeTrue();
        }

        [Fact]
        public void DecrementReferenceCount_should_not_call_Dispose_when_reference_is_not_zero()
        {
            var subject = CreateSubject();
            subject.IncrementReferenceCount();

            subject.DecrementReferenceCount();

            subject.IsDisposed().Should().BeFalse();
        }

        [Fact]
        public void DecrementReferenceCount_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();

            var exception = Record.Exception(() => subject.DecrementReferenceCount());

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void IncrementReferenceCount_should_increment_reference_count()
        {
            var subject = CreateSubject();
            var originalReferenceCount = subject._referenceCount();

            subject.IncrementReferenceCount();

            subject._referenceCount().Should().Be(originalReferenceCount + 1);
        }

        [Fact]
        public void IncrementReferenceCount_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();

            var exception = Record.Exception(() => subject.IncrementReferenceCount());

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void Dispose_should_set_disposed_flag()
        {
            var subject = CreateSubject();

            subject.Dispose();

            subject.IsDisposed().Should().BeTrue();
        }

        [Fact]
        public void Dispose_should_call_wrapped_Dispose()
        {
            Mock<IClientSession> mockWrapped;
            var subject = CreateSubject(out mockWrapped);

            subject.Dispose();

            mockWrapped.Verify(m => m.Dispose(), Times.Once);
        }

        [Fact]
        public void Dispose_can_be_called_more_than_once()
        {
            Mock<IClientSession> mockWrapped;
            var subject = CreateSubject(out mockWrapped);

            subject.Dispose();
            subject.Dispose();

            mockWrapped.Verify(m => m.Dispose(), Times.Once);
        }

        // private methods
        private ReferenceCountedClientSession CreateDisposedSubject()
        {
            var subject = CreateSubject();
            subject.Dispose();
            return subject;
        }

        private ReferenceCountedClientSession CreateSubject()
        {
            var wrapped = new Mock<IClientSession>().Object;
            return new ReferenceCountedClientSession(wrapped);
        }

        private ReferenceCountedClientSession CreateSubject(out Mock<IClientSession> mockWrapped)
        {
            mockWrapped = new Mock<IClientSession>();
            return new ReferenceCountedClientSession(mockWrapped.Object);
        }
    }

    internal static class ReferenceCountedClientSessionReflector
    {
        public static object _lock(this ReferenceCountedClientSession obj)
        {
            var fieldInfo = typeof(ReferenceCountedClientSession).GetField("_lock", BindingFlags.NonPublic | BindingFlags.Instance);
            return fieldInfo.GetValue(obj);
        }

        public static int _referenceCount(this ReferenceCountedClientSession obj)
        {
            lock (obj._lock())
            {
                var fieldInfo = typeof(ReferenceCountedClientSession).GetField("_referenceCount", BindingFlags.NonPublic | BindingFlags.Instance);
                return (int)fieldInfo.GetValue(obj);
            }
        }
    }
}
