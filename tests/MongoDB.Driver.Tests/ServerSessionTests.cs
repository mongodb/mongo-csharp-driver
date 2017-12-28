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

namespace MongoDB.Driver.Tests
{
    public class ServerSessionTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var coreServerSession = new Mock<ICoreServerSession>().Object;

            var result = new ServerSession(coreServerSession);

            result._coreServerSession().Should().BeSameAs(coreServerSession);
        }

        [Fact]
        public void constructor_should_throw_when_coreServerSession_is_null()
        {
            var exception = Record.Exception(() => new ServerSession(null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("coreServerSession");
        }

        [Fact]
        public void Id_should_call_coreServerSession_Id()
        {
            Mock<ICoreServerSession> mockCoreServerSession;
            var subject = CreateSubject(out mockCoreServerSession);
            var id = new BsonDocument("id", 1);
            mockCoreServerSession.SetupGet(m => m.Id).Returns(id);

            var result = subject.Id;

            result.Should().BeSameAs(id);
            mockCoreServerSession.VerifyGet(m => m.Id, Times.Once);
        }

        [Fact]
        public void LastUsedAt_should_call_coreServerSession_LastUsedAt()
        {
            Mock<ICoreServerSession> mockCoreServerSession;
            var subject = CreateSubject(out mockCoreServerSession);
            var lastUsedAt = DateTime.UtcNow;
            mockCoreServerSession.SetupGet(m => m.LastUsedAt).Returns(lastUsedAt);

            var result = subject.LastUsedAt;

            result.Should().Be(lastUsedAt);
            mockCoreServerSession.VerifyGet(m => m.LastUsedAt, Times.Once);
        }

        [Fact]
        public void AdvanceTransactionNumber_should_call_coreServerSession_AdvanceTransactionNumber()
        {
            Mock<ICoreServerSession> mockCoreServerSession;
            var subject = CreateSubject(out mockCoreServerSession);
            var transactionNumber = 123;
            mockCoreServerSession.Setup(m => m.AdvanceTransactionNumber()).Returns(transactionNumber);

            var result = subject.AdvanceTransactionNumber();

            result.Should().Be(transactionNumber);
            mockCoreServerSession.Verify(m => m.AdvanceTransactionNumber(), Times.Once);
        }

        [Fact]
        public void Dispose_should_call_coreServerSession_Dispose()
        {
            Mock<ICoreServerSession> mockCoreServerSession;
            var subject = CreateSubject(out mockCoreServerSession);

            subject.Dispose();

            mockCoreServerSession.Verify(m => m.Dispose(), Times.Once);
        }

        [Fact]
        public void WasUsed_should_call_coreServerSession_WasUsed()
        {
            Mock<ICoreServerSession> mockCoreServerSession;
            var subject = CreateSubject(out mockCoreServerSession);

            subject.WasUsed();

            mockCoreServerSession.Verify(m => m.WasUsed(), Times.Once);
        }

        // private methods
        private ServerSession CreateSubject(out Mock<ICoreServerSession> mockCoreServerSession)
        {
            mockCoreServerSession = new Mock<ICoreServerSession>();
            return new ServerSession(mockCoreServerSession.Object);
        }
    }

    internal static class ServerSessionReflector
    {
        public static ICoreServerSession _coreServerSession(this ServerSession obj)
        {
            var fieldInfo = typeof(ServerSession).GetField("_coreServerSession", BindingFlags.NonPublic | BindingFlags.Instance);
            return (ICoreServerSession)fieldInfo.GetValue(obj);
        }
    }
}
