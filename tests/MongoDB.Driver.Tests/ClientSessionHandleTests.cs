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
using FluentAssertions;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class ClientSessionHandleTests
    {
        [Fact]
        public void constructor_with_client_session_should_initialize_instance()
        {
            var session = new Mock<IClientSession>().Object;

            var subject = new ClientSessionHandle(session);

            subject.IsDisposed().Should().BeFalse();
            subject._ownsWrapped().Should().BeFalse();
            var referenceCounted = subject.Wrapped.Should().BeOfType<ReferenceCountedClientSession>().Subject;
            referenceCounted.Wrapped.Should().BeSameAs(session);
            referenceCounted._referenceCount().Should().Be(1);
            referenceCounted._ownsWrapped().Should().BeFalse();
        }

        [Fact]
        public void constructor_with_client_session_should_throw_when_session_is_null()
        {
            var exception = Record.Exception(() => new ClientSessionHandle((IClientSessionHandle)null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("wrapped");
        }

        [Fact]
        public void constructor_with_reference_counted_session_should_initialize_instance()
        {
            var session = new Mock<IClientSession>().Object;
            var referenceCounted = new ReferenceCountedClientSession(session);

            var subject = new ClientSessionHandle(referenceCounted);

            subject.Wrapped.Should().BeSameAs(referenceCounted);                                                        
        }

        [Fact]
        public void constructor_with_reference_counted_client_session_should_throw_when_session_is_null()
        {
            var exception = Record.Exception(() => new ClientSessionHandle((ReferenceCountedClientSession)null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("wrapped");
        }

        [Fact]
        public void Fork_should_return_new_handle_to_same_reference_counted_session()
        {
            ReferenceCountedClientSession referenceCounted;
            var subject = CreateSubject(out referenceCounted);

            var result = subject.Fork();

            var newHandle = result.Should().BeOfType<ClientSessionHandle>().Subject;
            newHandle.Wrapped.Should().BeSameAs(referenceCounted);
        }

        [Fact]
        public void Fork_should_increment_reference_count()
        {
            ReferenceCountedClientSession referenceCounted;
            var subject = CreateSubject(out referenceCounted);
            var originalReferenceCount = referenceCounted._referenceCount();

            subject.Fork();

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
        public void Dispose_should_set_disposed_flag()
        {
            var subject = CreateSubject();

            subject.Dispose();

            subject.IsDisposed().Should().BeTrue();
        }

        [Fact]
        public void Dispose_should_decrement_reference_count()
        {
            ReferenceCountedClientSession referenceCounted;
            var subject = CreateSubject(out referenceCounted);
            var fork = subject.Fork();
            var originalReferenceCount = referenceCounted._referenceCount();

            subject.Dispose();

            referenceCounted._referenceCount().Should().Be(originalReferenceCount - 1);
        }

        [Fact]
        public void Dispose_should_dispose_wrapped_session_when_reference_count_reaches_zero()
        {
            Mock<IClientSession> mockSession;
            var subject = CreateSubject(out mockSession);

            subject.Dispose();

            mockSession.Verify(m => m.Dispose(), Times.Once);
        }

        [Fact]
        public void Dispose_can_be_called_more_than_once()
        {
            ReferenceCountedClientSession referenceCounted;
            Mock<IClientSession> mockSession;
            var subject = CreateSubject(out referenceCounted, out mockSession);
            var originalReferenceCount = referenceCounted._referenceCount();

            subject.Dispose();
            subject.Dispose();

            referenceCounted._referenceCount().Should().Be(originalReferenceCount - 1);
            mockSession.Verify(m => m.Dispose(), Times.Once);
        }

        // private methods
        private ClientSessionHandle CreateDisposedSubject()
        {
            var subject = CreateSubject();
            subject.Dispose();
            return subject;
        }

        private ClientSessionHandle CreateSubject()
        {
            var session = new Mock<IClientSession>().Object;
            return new ClientSessionHandle(session);
        }

        private ClientSessionHandle CreateSubject(out Mock<IClientSession> mockSession)
        {
            mockSession = new Mock<IClientSession>();
            var referenceCounted = new ReferenceCountedClientSession(mockSession.Object);
            return new ClientSessionHandle(referenceCounted);
        }

        private ClientSessionHandle CreateSubject(out ReferenceCountedClientSession referenceCounted)
        {
            var session = new Mock<IClientSession>().Object;
            referenceCounted = new ReferenceCountedClientSession(session);
            return new ClientSessionHandle(referenceCounted);
        }

        private ClientSessionHandle CreateSubject(out ReferenceCountedClientSession referenceCounted, out Mock<IClientSession> mockSession)
        {
            mockSession = new Mock<IClientSession>();
            referenceCounted = new ReferenceCountedClientSession(mockSession.Object);
            return new ClientSessionHandle(referenceCounted);
        }
    }
}
