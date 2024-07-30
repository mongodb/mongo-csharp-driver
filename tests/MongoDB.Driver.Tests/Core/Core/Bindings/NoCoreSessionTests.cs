﻿/* Copyright 2017-present MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Bindings
{
    public class NoCoreSessionTests
    {
        [Fact]
        public void Instance_should_return_expected_result()
        {
            var result = NoCoreSession.Instance;

            result.ClusterTime.Should().BeNull();
            result.Id.Should().BeNull();
            result.IsCausallyConsistent.Should().BeFalse();
            result.IsImplicit.Should().BeTrue();
            result.OperationTime.Should().BeNull();
            result.Options.Should().BeNull();
            result.ServerSession.Should().BeOfType<NoCoreServerSession>();
        }

        [Fact]
        public void Instance_should_return_cached_instance()
        {
            var result1 = NoCoreSession.Instance;
            var result2 = NoCoreSession.Instance;

            result2.Should().BeSameAs(result1);
        }

        [Fact]
        public void NewHandle_should_return_expected_result()
        {
            var result = NoCoreSession.NewHandle();

            var handle = result.Should().BeOfType<CoreSessionHandle>().Subject;
            var referenceCounted = handle.Wrapped.Should().BeOfType<ReferenceCountedCoreSession>().Subject;
            referenceCounted.Wrapped.Should().BeSameAs(NoCoreSession.Instance);
        }

        [Fact]
        public void ClusterTime_should_return_expected_result()
        {
            var subject = CreateSubject();

            var result = subject.ClusterTime;

            result.Should().BeNull();
        }

        [Fact]
        public void CurrentTransaction_should_return_expected_result()
        {
            var subject = CreateSubject();

            var result = subject.CurrentTransaction;

            result.Should().BeNull();
        }

        [Fact]
        public void Id_should_return_expected_result()
        {
            var subject = CreateSubject();

            var result = subject.Id;

            result.Should().BeNull();
        }

        [Fact]
        public void IsCausallyConsistent_should_return_expected_result()
        {
            var subject = CreateSubject();

            var result = subject.IsCausallyConsistent;

            result.Should().BeFalse();
        }

        [Fact]
        public void IsImplicit_should_return_expected_result()
        {
            var subject = CreateSubject();

            var result = subject.IsImplicit;

            result.Should().BeTrue();
        }

        [Fact]
        public void IsInTransaction_should_return_expected_result()
        {
            var subject = CreateSubject();

            var result = subject.IsInTransaction;

            result.Should().BeFalse();
        }

        [Fact]
        public void OperationTime_should_return_expected_result()
        {
            var subject = CreateSubject();

            var result = subject.OperationTime;

            result.Should().BeNull();
        }

        [Fact]
        public void Options_should_return_expected_result()
        {
            var subject = CreateSubject();

            var result = subject.Options;

            result.Should().BeNull();
        }

        [Fact]
        public void ServerSession_should_return_expected_result()
        {
            var subject = CreateSubject();

            var result = subject.ServerSession;

            result.Should().BeSameAs(NoCoreServerSession.Instance);
        }

        [Fact]
        public void AbortTransaction_should_throw()
        {
            var subject = CreateSubject();

            var exception = Record.Exception(() => subject.AbortTransaction(CancellationToken.None));

            exception.Should().BeOfType<NotSupportedException>();
        }

        [Fact]
        public void AbortTransactionAsync_should_throw()
        {
            var subject = CreateSubject();

            var exception = Record.ExceptionAsync(() => subject.AbortTransactionAsync(CancellationToken.None)).GetAwaiter().GetResult();

            exception.Should().BeOfType<NotSupportedException>();
        }

        [Fact]
        public void AboutToSendCommand_should_do_nothing()
        {
            var subject = CreateSubject();

            subject.AboutToSendCommand();
        }

        [Fact]
        public void AdvanceClusterTime_should_do_nothing()
        {
            var subject = CreateSubject();
            var newClusterTime = CreateClusterTime();

            subject.AdvanceClusterTime(newClusterTime);
        }

        [Fact]
        public void AdvanceOperationTime_should_do_nothing()
        {
            var subject = CreateSubject();
            var newOperationTime = CreateOperationTime();

            subject.AdvanceOperationTime(newOperationTime);
        }

        [Fact]
        public void AdvanceTransactionNumber_should_return_minus_one()
        {
            var subject = CreateSubject();

            var result = subject.AdvanceTransactionNumber();

            result.Should().Be(-1);
        }

        [Fact]
        public void CommitTransaction_should_throw()
        {
            var subject = CreateSubject();

            var exception = Record.Exception(() => subject.CommitTransaction(CancellationToken.None));

            exception.Should().BeOfType<NotSupportedException>();
        }

        [Fact]
        public void CommitTransactionAsync_should_throw()
        {
            var subject = CreateSubject();

            var exception = Record.ExceptionAsync(() => subject.CommitTransactionAsync(CancellationToken.None)).GetAwaiter().GetResult();

            exception.Should().BeOfType<NotSupportedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Dispose_should_do_nothing(
            [Values(1, 2)] int timesCalled)
        {
            var subject = CreateSubject();

            for (var i = 0; i < timesCalled; i++)
            {
                subject.Dispose();
            }
        }

        [Fact]
        public void StartTransaction_should_throw()
        {
            var subject = CreateSubject();

            var exception = Record.Exception(() => subject.StartTransaction());

            exception.Should().BeOfType<NotSupportedException>();
        }

        [Fact]
        public void WasUsed_should_do_nothing()
        {
            var subject = CreateSubject();

            subject.WasUsed();
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

        private BsonTimestamp CreateOperationTime()
        {
            return new BsonTimestamp(1L);
        }

        private NoCoreSession CreateSubject()
        {
            return new NoCoreSession();
        }
    }
}
