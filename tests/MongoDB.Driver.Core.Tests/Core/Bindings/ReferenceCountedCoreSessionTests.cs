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

namespace MongoDB.Driver.Core.Bindings
{
    public class ReferenceCountedCoreSessionTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var wrapped = new Mock<ICoreSession>().Object;

            var result = new ReferenceCountedCoreSession(wrapped);

            result.Wrapped.Should().BeSameAs(wrapped);
            result.IsDisposed().Should().BeFalse();
            result._ownsWrapped().Should().BeTrue();
            result._referenceCount().Should().Be(1);
        }

        [Fact]
        public void constructor_should_throw_when_wrapped_is_null()
        {
            var exception = Record.Exception(() => new ReferenceCountedCoreSession(null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("wrapped");
        }

        [Fact]
        public void DecrementReferenceCount_should_decrement_the_reference_count()
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
        public void DecrementReferenceCount_should_not_call_Dispose_when_reference_count_has_not_reached_zero()
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
        public void IncrementReferenceCount_should_increment_the_reference_count()
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

        // private methods
        private ReferenceCountedCoreSession CreateDisposedSubject()
        {
            var subject = CreateSubject();
            subject.Dispose();
            return subject;
        }

        private ReferenceCountedCoreSession CreateSubject()
        {
            var wrapped = new Mock<ICoreSession>().Object;
            return new ReferenceCountedCoreSession(wrapped);
        }
    }

    internal static class ReferenceCountedCoreSessionReflector
    {
        public static int _referenceCount(this ReferenceCountedCoreSession obj)
        {
            var fieldInfo = typeof(ReferenceCountedCoreSession).GetField("_referenceCount", BindingFlags.NonPublic | BindingFlags.Instance);
            return (int)fieldInfo.GetValue(obj);
        }
    }
}
