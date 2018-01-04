/* Copyright 2017-present MongoDB Inc.
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
    public class CoreSessionHandleTests
    {
        [Fact]
        public void constructor_with_session_should_intialize_instance()
        {
            var session = new Mock<ICoreSession>().Object;

            var result = new CoreSessionHandle(session);

            result._ownsWrapped().Should().BeFalse();               
            var referenceCounted = result.Wrapped.Should().BeOfType<ReferenceCountedCoreSession>().Subject;
            referenceCounted.Wrapped.Should().BeSameAs(session);
            referenceCounted._referenceCount().Should().Be(1);
        }

        [Fact]
        public void constructor_with_session_should_throw_when_session_is_null()
        {
            var exception = Record.Exception(() => new CoreSessionHandle((ICoreSession)null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("session");
        }

        [Fact]
        public void constructor_with_reference_counted_should_intialize_instance()
        {
            var session = new Mock<ICoreSession>().Object;
            var referenceCounted = new ReferenceCountedCoreSession(session);

            var result = new CoreSessionHandle(referenceCounted);

            result._ownsWrapped().Should().BeFalse();
            result.Wrapped.Should().BeSameAs(referenceCounted);
            referenceCounted._referenceCount().Should().Be(1);
        }

        [Fact]
        public void constructor_with_reference_counted_should_throw_when_session_is_null()
        {
            var exception = Record.Exception(() => new CoreSessionHandle((ReferenceCountedCoreSession)null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("wrapped");
        }

        [Fact]
        public void Fork_should_return_a_new_handle_to_same_reference_counted_session()
        {
            ReferenceCountedCoreSession referenceCounted;
            var subject = CreateSubject(out referenceCounted);

            var result = subject.Fork();

            var newHandle = result.Should().BeOfType<CoreSessionHandle>().Subject;
            newHandle.Wrapped.Should().BeSameAs(referenceCounted);
        }

        [Fact]
        public void Fork_should_increment_reference_count()
        {
            ReferenceCountedCoreSession referenceCounted;
            var subject = CreateSubject(out referenceCounted);
            var originalReferenceCount = referenceCounted._referenceCount();

            var result = subject.Fork();

            referenceCounted._referenceCount().Should().Be(originalReferenceCount + 1);
        }

        [Fact]
        public void Fork_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();

            var exception = Record.Exception(() => subject.Fork());

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void Dispose_can_be_called_more_than_once()
        {
            ReferenceCountedCoreSession referenceCounted;
            var subject = CreateSubject(out referenceCounted);
            var originalReferenceCount = referenceCounted._referenceCount();

            subject.Dispose();
            subject.Dispose();

            referenceCounted._referenceCount().Should().Be(originalReferenceCount - 1);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Dispose_with_disposing_should_decrement_reference_count_when_appropriate(bool disposing)
        {
            ReferenceCountedCoreSession referenceCounted;
            var subject = CreateSubject(out referenceCounted);
            var originalReferenceCount = referenceCounted._referenceCount();

            subject.Dispose(disposing);

            var expectedReferenceCount = disposing ? originalReferenceCount - 1 : originalReferenceCount;
            referenceCounted._referenceCount().Should().Be(expectedReferenceCount);
        }

        // private methods
        private CoreSessionHandle CreateDisposedSubject()
        {
            var subject = CreateSubject();
            subject.Dispose();
            return subject;
        }

        private CoreSessionHandle CreateSubject()
        {
            var session = new Mock<ICoreSession>().Object;
            return new CoreSessionHandle(session);
        }

        private CoreSessionHandle CreateSubject(out ReferenceCountedCoreSession referenceCounted)
        {
            var session = new Mock<ICoreSession>().Object;
            referenceCounted = new ReferenceCountedCoreSession(session);
            return new CoreSessionHandle(referenceCounted);
        }
    }

    internal static class CoreSessionHandleReflector
    {
        public static void Dispose(this CoreSessionHandle obj, bool disposing)
        {
            var methodInfo = typeof(CoreSessionHandle).GetMethod("Dispose", BindingFlags.NonPublic | BindingFlags.Instance);
            methodInfo.Invoke(obj, new object[] { disposing });
        }
    }
}
