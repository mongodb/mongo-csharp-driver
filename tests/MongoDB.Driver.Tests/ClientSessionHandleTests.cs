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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Support;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class ClientSessionHandleTests
    {
        private const string TransientTransactionErrorLabel = "TransientTransactionError";
        private const string UnknownTransactionCommitResultLabel = "UnknownTransactionCommitResult";

        public enum WithTransactionErrorState
        {
            NoError,
            ErrorWithoutLabel,
            TransientTransactionError,
            UnknownTransactionCommitResult
        }

        public interface ICallbackProcessing
        {
            Func<IClientSessionHandle, bool> Callback { get; set; }
            Func<IClientSessionHandle, Task<bool>> CallbackAsync { get; set; }
        }

        [Fact]
        public void constructor_should_initialize_instance()
        {
            var client = Mock.Of<IMongoClient>();
            var options = new ClientSessionOptions();
            var coreSession = CreateCoreSession();

            var result = new ClientSessionHandle(client, options, coreSession);

            result.Client.Should().BeSameAs(client);
            result.Options.Should().BeSameAs(options);
            result.WrappedCoreSession.Should().BeSameAs(coreSession);
            result._disposed().Should().BeFalse();

            var serverSession = result.ServerSession.Should().BeOfType<ServerSession>().Subject;
            serverSession._coreServerSession().Should().BeSameAs(coreSession.ServerSession);
        }

        [Fact]
        public void Client_returns_expected_result()
        {
            var client = Mock.Of<IMongoClient>();
            var subject = CreateSubject(client: client);

            var result = subject.Client;

            result.Should().BeSameAs(client);
        }

        [Fact]
        public void ClusterTime_should_return_expected_result()
        {
            var subject = CreateSubject();
            var value = new BsonDocument();
            var mockCoreSession = Mock.Get(subject.WrappedCoreSession);
            mockCoreSession.SetupGet(m => m.ClusterTime).Returns(value);

            var result = subject.ClusterTime;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void IsImplicit_should_call_coreSession(
            [Values(false, true)] bool value)
        {
            var subject = CreateSubject();
            var mockCoreSession = Mock.Get(subject.WrappedCoreSession);
            mockCoreSession.SetupGet(m => m.IsImplicit).Returns(value);

            var result = subject.IsImplicit;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void IsInTransaction_should_call_coreSession(
            [Values(false, true)] bool value)
        {
            var subject = CreateSubject();
            var mockCoreSession = Mock.Get(subject.WrappedCoreSession);
            mockCoreSession.SetupGet(m => m.IsInTransaction).Returns(value);

            var result = subject.IsInTransaction;

            result.Should().Be(value);
        }

        [Fact]
        public void OperationTime_should_call_coreSession()
        {
            var subject = CreateSubject();
            var value = new BsonTimestamp(0);
            var mockCoreSession = Mock.Get(subject.WrappedCoreSession);
            mockCoreSession.SetupGet(m => m.OperationTime).Returns(value);

            var result = subject.OperationTime;

            result.Should().BeSameAs(value);
        }

        [Fact]
        public void Options_returns_expected_result()
        {
            var options = new ClientSessionOptions();
            var subject = CreateSubject(options: options);

            var result = subject.Options;

            result.Should().BeSameAs(options);
        }

        [Fact]
        public void ServerSession_returns_expected_result()
        {
            var subject = CreateSubject();

            var result = subject.ServerSession;

            result.Should().BeSameAs(subject._serverSession());
        }

        [Fact]
        public void WrappedCoreSession_returns_expected_result()
        {
            var coreSession = CreateCoreSession();
            var subject = CreateSubject(coreSession: coreSession);

            var result = subject.WrappedCoreSession;

            result.Should().BeSameAs(coreSession);
        }

        [Fact]
        public void AbortTransactionAsync_should_call_coreSession()
        {
            var subject = CreateSubject();
            var cancellationToken = new CancellationToken();
            var task = Task.FromResult(true);
            Mock.Get(subject.WrappedCoreSession).Setup(m => m.AbortTransactionAsync(cancellationToken)).Returns(task);

            var result = subject.AbortTransactionAsync(cancellationToken);

            result.Should().BeSameAs(task);
            Mock.Get(subject.WrappedCoreSession).Verify(m => m.AbortTransactionAsync(cancellationToken), Times.Once);
        }

        [Theory]
        [ParameterAttributeData]
        public void AdvanceClusterTime_should_call_coreSession(
           [Values(false, true)] bool value)
        {
            var subject = CreateSubject();
            var newClusterTime = new BsonDocument();

            subject.AdvanceClusterTime(newClusterTime);

            Mock.Get(subject.WrappedCoreSession).Verify(m => m.AdvanceClusterTime(newClusterTime), Times.Once);
        }

        [Theory]
        [ParameterAttributeData]
        public void AdvanceOperationTime_should_call_coreSession(
           [Values(false, true)] bool value)
        {
            var subject = CreateSubject();
            var newOperationTime = new BsonTimestamp(0);

            subject.AdvanceOperationTime(newOperationTime);

            Mock.Get(subject.WrappedCoreSession).Verify(m => m.AdvanceOperationTime(newOperationTime), Times.Once);
        }

        [Fact]
        public void CommitTransaction_should_call_coreSession()
        {
            var subject = CreateSubject();
            var cancellationToken = new CancellationToken();

            subject.CommitTransaction(cancellationToken);

            Mock.Get(subject.WrappedCoreSession).Verify(m => m.CommitTransaction(cancellationToken), Times.Once);
        }

        [Fact]
        public void CommitTransactionAsync_should_call_coreSession()
        {
            var subject = CreateSubject();
            var cancellationToken = new CancellationToken();
            var task = Task.FromResult(true);
            Mock.Get(subject.WrappedCoreSession).Setup(m => m.CommitTransactionAsync(cancellationToken)).Returns(task);

            var result = subject.CommitTransactionAsync(cancellationToken);

            result.Should().BeSameAs(task);
            Mock.Get(subject.WrappedCoreSession).Verify(m => m.CommitTransactionAsync(cancellationToken), Times.Once);
        }

        [Theory]
        [ParameterAttributeData]
        public void Dispose_should_have_expected_result(
            [Values(1, 2)] int timesCalled)
        {
            var subject = CreateSubject();

            for (var i = 0; i < timesCalled; i++)
            {
                subject.Dispose();
            }

            subject._disposed().Should().BeTrue();
            Mock.Get(subject.WrappedCoreSession).Verify(m => m.Dispose(), Times.Once);
        }

        [Fact]
        public void Fork_should_return_expected_result()
        {
            var cluster = Mock.Of<IClusterInternal>();
            var coreServerSession = new CoreServerSession();
            var options = new ClientSessionOptions();
            var coreSession = new CoreSession(cluster, coreServerSession, options.ToCore());
            var coreSessionHandle = new CoreSessionHandle(coreSession);
            var subject = CreateSubject(coreSession: coreSessionHandle);
            coreSessionHandle.ReferenceCount().Should().Be(1);

            var result = subject.Fork();

            result.Client.Should().BeSameAs(subject.Client);
            result.Options.Should().BeSameAs(subject.Options);
            result.WrappedCoreSession.Should().NotBeSameAs(subject.WrappedCoreSession);
            var coreSessionHandle1 = (CoreSessionHandle)subject.WrappedCoreSession;
            var coreSessionHandle2 = (CoreSessionHandle)result.WrappedCoreSession;
            coreSessionHandle2.Wrapped.Should().BeSameAs(coreSessionHandle1.Wrapped);
            coreSessionHandle.ReferenceCount().Should().Be(2);
        }

        [Fact]
        public void StartTransaction_should_call_coreSession()
        {
            var subject = CreateSubject();
            var transactionOptions = new TransactionOptions();

            subject.StartTransaction(transactionOptions);

            Mock.Get(subject.WrappedCoreSession).Verify(m => m.StartTransaction(It.IsAny<TransactionOptions>()), Times.Once);
        }

        [Theory]
        // sync
        [InlineData(null, new[] { WithTransactionErrorState.NoError }, true, false /*Should exception be thrown*/, 1, 1, false)]
        [InlineData(null, new[] { WithTransactionErrorState.NoError }, false, false /*Should exception be thrown*/, 1, 0, false)]
        [InlineData(new[] { false }, new[] { WithTransactionErrorState.ErrorWithoutLabel }, false, true /*Should exception be thrown*/, 1, 0, false)]

        [InlineData(new[] { true }, new[] { WithTransactionErrorState.TransientTransactionError }, true, true /*Should exception be thrown*/, 1, 0, false)]
        [InlineData(new[] { false, true }, new[] { WithTransactionErrorState.TransientTransactionError, WithTransactionErrorState.TransientTransactionError }, true, true /*Should exception be thrown*/, 2, 0, false)]
        [InlineData(new[] { false }, new[] { WithTransactionErrorState.TransientTransactionError, WithTransactionErrorState.NoError }, true, false /*Should exception be thrown*/, 2, 1, false)]
        [InlineData(new[] { false, false }, new[] { WithTransactionErrorState.TransientTransactionError, WithTransactionErrorState.TransientTransactionError, WithTransactionErrorState.NoError }, true, false /*Should exception be thrown*/, 3, 1, false)]

        [InlineData(new[] { false, false, false }, new[] { WithTransactionErrorState.TransientTransactionError, WithTransactionErrorState.TransientTransactionError, WithTransactionErrorState.ErrorWithoutLabel }, true, true /*Should exception be thrown*/, 3, 0, false)]
        [InlineData(new[] { false, false, true }, new[] { WithTransactionErrorState.TransientTransactionError, WithTransactionErrorState.TransientTransactionError, WithTransactionErrorState.ErrorWithoutLabel }, true, true /*Should exception be thrown*/, 3, 0, false)]
        [InlineData(new[] { false, true, true }, new[] { WithTransactionErrorState.TransientTransactionError, WithTransactionErrorState.TransientTransactionError, WithTransactionErrorState.ErrorWithoutLabel }, true, true /*Should exception be thrown*/, 2, 0, false)]

        // async
        [InlineData(null, new[] { WithTransactionErrorState.NoError }, true, false /*Should exception be thrown*/, 1, 1, true)]
        [InlineData(null, new[] { WithTransactionErrorState.NoError }, false, false /*Should exception be thrown*/, 1, 0, true)]
        [InlineData(new[] { false }, new[] { WithTransactionErrorState.ErrorWithoutLabel }, false, true /*Should exception be thrown*/, 1, 0, true)]

        [InlineData(new[] { true }, new[] { WithTransactionErrorState.TransientTransactionError }, true, true /*Should exception be thrown*/, 1, 0, true)]
        [InlineData(new[] { false, true }, new[] { WithTransactionErrorState.TransientTransactionError, WithTransactionErrorState.TransientTransactionError }, true, true /*Should exception be thrown*/, 2, 0, true)]
        [InlineData(new[] { false }, new[] { WithTransactionErrorState.TransientTransactionError, WithTransactionErrorState.NoError }, true, false /*Should exception be thrown*/, 2, 1, true)]
        [InlineData(new[] { false, false }, new[] { WithTransactionErrorState.TransientTransactionError, WithTransactionErrorState.TransientTransactionError, WithTransactionErrorState.NoError }, true, false /*Should exception be thrown*/, 3, 1, true)]

        [InlineData(new[] { false, false, false }, new[] { WithTransactionErrorState.TransientTransactionError, WithTransactionErrorState.TransientTransactionError, WithTransactionErrorState.ErrorWithoutLabel }, true, true /*Should exception be thrown*/, 3, 0, true)]
        [InlineData(new[] { false, false, true }, new[] { WithTransactionErrorState.TransientTransactionError, WithTransactionErrorState.TransientTransactionError, WithTransactionErrorState.ErrorWithoutLabel }, true, true /*Should exception be thrown*/, 3, 0, true)]
        [InlineData(new[] { false, true, true }, new[] { WithTransactionErrorState.TransientTransactionError, WithTransactionErrorState.TransientTransactionError, WithTransactionErrorState.ErrorWithoutLabel }, true, true /*Should exception be thrown*/, 2, 0, true)]

        public void WithTransaction_callback_should_be_processed_with_expected_result(
            bool[] isRetryAttemptsWithTimeout, // the array length should be the same with a number of failed attempts from `callbackTransactionErrorStates`
            WithTransactionErrorState[] callbackTransactionErrorStates,
            bool isTransactionInProgress,
            bool shouldExceptionBeThrown,
            int expectedStartTransactionAttempts,
            int expectedCommitTransactionAttempts,
            bool async)
        {
            var mockClock = CreateClockMock(DateTime.UtcNow, isRetryAttemptsWithTimeout, true);

            var mockCoreSession = CreateCoreSessionMock();
            mockCoreSession.Setup(c => c.StartTransaction(It.IsAny<TransactionOptions>()));

            // CommitTransaction
            if (async)
            {
                mockCoreSession
                    .Setup(c => c.CommitTransactionAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(0));
            }
            else
            {
                mockCoreSession.Setup(c => c.CommitTransaction(It.IsAny<CancellationToken>()));
            }

            // Initialize callbacks
            var mockCallbackProcessing = new Mock<ICallbackProcessing>();
            if (async)
            {
                var mockCallbackProcessingSetup = mockCallbackProcessing.SetupSequence(c => c.CallbackAsync);
                foreach (var callbackTransactionErrorState in callbackTransactionErrorStates)
                {
                    if (callbackTransactionErrorState == WithTransactionErrorState.NoError)
                    {
                        mockCallbackProcessingSetup.Returns(() => async handle => await Task.FromResult(true));
                    }
                    else
                    {
                        var callbackException = PrepareException(callbackTransactionErrorState);
                        mockCallbackProcessingSetup.Throws(callbackException);
                    }
                }
            }
            else
            {
                var mockCallbackProcessingSetup = mockCallbackProcessing.SetupSequence(c => c.Callback);
                foreach (var callbackTransactionErrorState in callbackTransactionErrorStates)
                {
                    if (callbackTransactionErrorState == WithTransactionErrorState.NoError)
                    {
                        mockCallbackProcessingSetup.Returns(() => handle => true);
                    }
                    else
                    {
                        var callbackException = PrepareException(callbackTransactionErrorState);
                        mockCallbackProcessingSetup.Throws(callbackException);
                    }
                }
            }
            var subject = CreateSubject(coreSession: mockCoreSession.Object, clock: mockClock.Object);

            SetupTransactionState(subject, isTransactionInProgress);

            // Callback processing
            if (async)
            {
                if (shouldExceptionBeThrown)
                {
                    Assert.ThrowsAsync<MongoException>(() => subject.WithTransactionAsync(async (handle, cancellationToken) => await mockCallbackProcessing.Object.CallbackAsync(It.IsAny<IClientSessionHandle>()))).GetAwaiter().GetResult();
                }
                else
                {
                    var withTransactionResult = subject.WithTransactionAsync(async (handle, cancellationToken) => await mockCallbackProcessing.Object.CallbackAsync(It.IsAny<IClientSessionHandle>())).Result;
                    withTransactionResult.Should().BeTrue();
                    if (isTransactionInProgress)
                    {
                        var expectedAbortTransactionNumberOfCalls = callbackTransactionErrorStates.Count(c => c != WithTransactionErrorState.NoError);
                        mockCoreSession.Verify(handle => handle.AbortTransactionAsync(It.IsAny<CancellationToken>()), Times.Exactly(expectedAbortTransactionNumberOfCalls));
                    }
                    else
                    {
                        mockCoreSession.Verify(handle => handle.AbortTransactionAsync(It.IsAny<CancellationToken>()), Times.Never());
                    }
                }

                mockCoreSession.Verify(handle => handle.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Exactly(expectedCommitTransactionAttempts));
            }
            else
            {
                if (shouldExceptionBeThrown)
                {
                    Assert.Throws<MongoException>(() => subject.WithTransaction((handle, cancellationToken) => mockCallbackProcessing.Object.Callback(It.IsAny<IClientSessionHandle>())));
                }
                else
                {
                    var withTransactionResult = subject.WithTransaction((handle, cancellationToken) => mockCallbackProcessing.Object.Callback(It.IsAny<IClientSessionHandle>()));
                    withTransactionResult.Should().BeTrue();
                    if (isTransactionInProgress)
                    {
                        var expectedAbortTransactionNumberOfCalls = callbackTransactionErrorStates.Count(c => c != WithTransactionErrorState.NoError);
                        mockCoreSession.Verify(handle => handle.AbortTransaction(It.IsAny<CancellationToken>()), Times.Exactly(expectedAbortTransactionNumberOfCalls));
                    }
                    else
                    {
                        mockCoreSession.Verify(handle => handle.AbortTransaction(It.IsAny<CancellationToken>()), Times.Never());
                    }
                }

                mockCoreSession.Verify(handle => handle.CommitTransaction(It.IsAny<CancellationToken>()), Times.Exactly(expectedCommitTransactionAttempts));
            }

            mockCoreSession.Verify(handle => handle.StartTransaction(It.IsAny<TransactionOptions>()), Times.Exactly(expectedStartTransactionAttempts));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(true)]
        [InlineData("test")]
        public void WithTransaction_callback_should_propagate_result(object value)
        {
            var subject = CreateSubject();
            var result = subject.WithTransaction<object>((handle, cancellationToken) => value);
            result.Should().Be(value);
        }

        [Fact]
        public void WithTransaction_callback_with_a_custom_error_should_not_be_retried()
        {
            var mockCoreSession = CreateCoreSessionMock();
            mockCoreSession.Setup(c => c.StartTransaction(It.IsAny<TransactionOptions>()));
            mockCoreSession.Setup(c => c.AbortTransaction(It.IsAny<CancellationToken>())); // abort ignores exceptions
            mockCoreSession.Setup(c => c.CommitTransaction(It.IsAny<CancellationToken>()));

            var subject = CreateSubject(coreSession: mockCoreSession.Object);

            Assert.Throws<MongoException>(() => subject.WithTransaction<bool>((handle, cancellationToken) => throw new MongoException("test")));

            mockCoreSession.Verify(handle => handle.StartTransaction(It.IsAny<TransactionOptions>()), Times.Once);
            mockCoreSession.Verify(handle => handle.CommitTransaction(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public void WithTransaction_callback_with_a_TransientTransactionError_and_exceeded_retry_timeout_should_not_be_retried()
        {
            var now = DateTime.UtcNow;
            var mockClock = new Mock<IClock>();
            mockClock
                .SetupSequence(c => c.UtcNow)
                .Returns(now)
                .Returns(now.AddSeconds(CalculateTime(true))); // the retry timeout has been exceeded

            var subject = CreateSubject(clock: mockClock.Object);

            var exResult = Assert.Throws<MongoException>(() => subject.WithTransaction<bool>((handle, cancellationToken) =>
            {
                throw PrepareException(WithTransactionErrorState.TransientTransactionError);
            }));
            exResult.HasErrorLabel(TransientTransactionErrorLabel).Should().BeTrue();
        }

        [Theory]
        [ParameterAttributeData]
        public void WithTransaction_callback_with_a_UnknownTransactionCommitResult_should_not_be_retried([Values(true, false)] bool hasTimedOut)
        {
            var now = DateTime.UtcNow;
            var mockClock = new Mock<IClock>();
            mockClock
                .SetupSequence(c => c.UtcNow)
                .Returns(now)
                .Returns(now.AddSeconds(CalculateTime(hasTimedOut)));

            var subject = CreateSubject(clock: mockClock.Object);

            var exResult = Assert.Throws<MongoException>(() => subject.WithTransaction<bool>((handle, cancellationToken) =>
            {
                throw PrepareException(WithTransactionErrorState.UnknownTransactionCommitResult);
            }));
            exResult.HasErrorLabel(UnknownTransactionCommitResultLabel).Should().BeTrue();
        }

        [Theory]
        // sync
        [InlineData(null, new[] { WithTransactionErrorState.NoError }, false /*Should exception be thrown*/, 1, true, false)]
        [InlineData(null, new[] { WithTransactionErrorState.TransientTransactionError }, false /*Should exception be thrown*/, 1, false, false)]

        [InlineData(null, new[] { WithTransactionErrorState.ErrorWithoutLabel }, true /*Should exception be thrown*/, 1, false, false)]

        [InlineData(new[] { false, false }, new[] { WithTransactionErrorState.TransientTransactionError, WithTransactionErrorState.TransientTransactionError }, false /*Should exception be thrown*/, 1, false, false)]
        [InlineData(new[] { true, true }, new[] { WithTransactionErrorState.TransientTransactionError, WithTransactionErrorState.TransientTransactionError }, true /*Should exception be thrown*/, 1, null, false)]

        [InlineData(new[] { false, false }, new[] { WithTransactionErrorState.UnknownTransactionCommitResult, WithTransactionErrorState.UnknownTransactionCommitResult, WithTransactionErrorState.NoError }, false /*Should exception be thrown*/, 3, true, false)]
        [InlineData(new[] { false, true }, new[] { WithTransactionErrorState.UnknownTransactionCommitResult, WithTransactionErrorState.UnknownTransactionCommitResult, WithTransactionErrorState.NoError }, true /*Should exception be thrown*/, 2, null, false)]

        [InlineData(new[] { false, false }, new[] { WithTransactionErrorState.UnknownTransactionCommitResult, WithTransactionErrorState.UnknownTransactionCommitResult, WithTransactionErrorState.NoError }, false /*Should exception be thrown*/, 3, true, false)]

        // async
        [InlineData(null, new[] { WithTransactionErrorState.NoError }, false /*Should exception be thrown*/, 1, true, true)]
        [InlineData(null, new[] { WithTransactionErrorState.TransientTransactionError }, false /*Should exception be thrown*/, 1, false, true)]

        [InlineData(null, new[] { WithTransactionErrorState.ErrorWithoutLabel }, true /*Should exception be thrown*/, 1, false, true)]

        [InlineData(new[] { false, false }, new[] { WithTransactionErrorState.TransientTransactionError, WithTransactionErrorState.TransientTransactionError }, false /*Should exception be thrown*/, 1, false, true)]
        [InlineData(new[] { true, true }, new[] { WithTransactionErrorState.TransientTransactionError, WithTransactionErrorState.TransientTransactionError }, true /*Should exception be thrown*/, 1, null, true)]

        [InlineData(new[] { false, false }, new[] { WithTransactionErrorState.UnknownTransactionCommitResult, WithTransactionErrorState.UnknownTransactionCommitResult, WithTransactionErrorState.NoError }, false /*Should exception be thrown*/, 3, true, true)]
        [InlineData(new[] { false, true }, new[] { WithTransactionErrorState.UnknownTransactionCommitResult, WithTransactionErrorState.UnknownTransactionCommitResult, WithTransactionErrorState.NoError }, true /*Should exception be thrown*/, 2, null, true)]

        [InlineData(new[] { false, false }, new[] { WithTransactionErrorState.UnknownTransactionCommitResult, WithTransactionErrorState.UnknownTransactionCommitResult, WithTransactionErrorState.NoError }, false /*Should exception be thrown*/, 3, true, true)]
        public void WithTransaction_commit_after_callback_processing_should_be_processed_with_expected_result(
            bool[] isRetryAttemptsWithTimeout, // the array length should be the same with a number of failed attempts from `commitTransactionErrorStates`
            WithTransactionErrorState[] commitTransactionErrorStates,
            bool shouldExceptionBeThrown,
            int expectedCommitTransactionAttempts,
            bool? expectedFullTransactionBeRetriedState,
            bool async)
        {
            var now = DateTime.UtcNow;
            var mockClock = CreateClockMock(now, isRetryAttemptsWithTimeout, false);

            var mockCoreSession = CreateCoreSessionMock();

            // Initialize commit result
            if (async)
            {
                var mockCommitProcessing = mockCoreSession.SetupSequence(c => c.CommitTransactionAsync(It.IsAny<CancellationToken>()));
                foreach (var commitTransactionErrorState in commitTransactionErrorStates)
                {
                    if (commitTransactionErrorState == WithTransactionErrorState.NoError)
                    {
                        mockCommitProcessing.Returns(Task.FromResult(0));
                    }
                    else
                    {
                        var commitException = PrepareException(commitTransactionErrorState);
                        mockCommitProcessing.Throws(commitException);
                    }
                }
            }
            else
            {
                var mockCommitProcessing = mockCoreSession.SetupSequence(c => c.CommitTransaction(It.IsAny<CancellationToken>()));
                foreach (var commitTransactionErrorState in commitTransactionErrorStates)
                {
                    if (commitTransactionErrorState == WithTransactionErrorState.NoError)
                    {
                        mockCommitProcessing.Pass();
                    }
                    else
                    {
                        var commitException = PrepareException(commitTransactionErrorState);
                        mockCommitProcessing.Throws(commitException);
                    }
                }
            }

            var subject = CreateSubject(coreSession: mockCoreSession.Object, clock: mockClock.Object);

            // Commit processing
            if (async)
            {
                if (shouldExceptionBeThrown)
                {
                    Assert.ThrowsAnyAsync<MongoException>(() => TransactionExecutorReflector.CommitWithRetriesAsync(subject, now, mockClock.Object, CancellationToken.None)).GetAwaiter().GetResult();
                }
                else
                {
                    var result = TransactionExecutorReflector.CommitWithRetriesAsync(subject, now, mockClock.Object, CancellationToken.None).Result;
                    expectedFullTransactionBeRetriedState.Should().Be(result);
                }

                mockCoreSession.Verify(handle => handle.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Exactly(expectedCommitTransactionAttempts));
            }
            else
            {
                if (shouldExceptionBeThrown)
                {
                    Assert.ThrowsAny<MongoException>(() => TransactionExecutorReflector.CommitWithRetries(subject, now, mockClock.Object, CancellationToken.None));
                }
                else
                {
                    var result = TransactionExecutorReflector.CommitWithRetries(subject, now, mockClock.Object, CancellationToken.None);
                    expectedFullTransactionBeRetriedState.Should().Be(result);
                }

                mockCoreSession.Verify(handle => handle.CommitTransaction(It.IsAny<CancellationToken>()), Times.Exactly(expectedCommitTransactionAttempts));
            }
        }

        [Fact]
        public void WithTransaction_should_set_valid_session_to_callback()
        {
            var mockCoreSession = CreateCoreSessionMock();
            mockCoreSession.Setup(c => c.StartTransaction(It.IsAny<TransactionOptions>()));
            mockCoreSession.Setup(c => c.CommitTransaction(It.IsAny<CancellationToken>()));
            var subject = CreateSubject(coreSession: mockCoreSession.Object);

            var result = subject.WithTransaction<object>((session, cancellationToken) => session);

            result.Should().BeSameAs(subject);
        }

        [Theory]
        [InlineData(CoreTransactionState.Starting, true)]
        [InlineData(CoreTransactionState.InProgress, true)]
        [InlineData(CoreTransactionState.Aborted, false)]
        [InlineData(CoreTransactionState.Committed, false)]
        public void WithTransaction_with_error_in_callback_should_call_AbortTransaction_according_to_transaction_state(CoreTransactionState transactionState, bool shouldAbortTransactionBeCalled)
        {
            var mockCoreSession = CreateCoreSessionMock();
            mockCoreSession.Setup(c => c.StartTransaction(It.IsAny<TransactionOptions>()));
            mockCoreSession.Setup(c => c.AbortTransaction(It.IsAny<CancellationToken>())); // abort ignores exceptions
            mockCoreSession.Setup(c => c.CommitTransaction(It.IsAny<CancellationToken>()));
            var subject = CreateSubject(coreSession: mockCoreSession.Object);

            subject.WrappedCoreSession.CurrentTransaction.SetState(transactionState);

            Assert.Throws<Exception>(() => subject.WithTransaction<object>((handle, cancellationToken) => throw new Exception("test")));

            mockCoreSession.Verify(handle => handle.StartTransaction(It.IsAny<TransactionOptions>()), Times.Once);
            mockCoreSession.Verify(handle => handle.AbortTransaction(It.IsAny<CancellationToken>()), shouldAbortTransactionBeCalled ? Times.Once() : Times.Never());
            mockCoreSession.Verify(handle => handle.CommitTransaction(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public void WithTransaction_with_error_in_StartTransaction_should_return_control_immediately()
        {
            var mockCoreSession = CreateCoreSessionMock();
            mockCoreSession
                .Setup(c => c.StartTransaction(It.IsAny<TransactionOptions>()))
                .Throws<Exception>();
            mockCoreSession.Setup(c => c.CommitTransaction(It.IsAny<CancellationToken>()));
            var subject = CreateSubject(coreSession: mockCoreSession.Object);

            Assert.Throws<Exception>(() => subject.WithTransaction<object>((handle, cancellationToken) => 1));
            mockCoreSession.Verify(handle => handle.StartTransaction(It.IsAny<TransactionOptions>()), Times.Once);
            mockCoreSession.Verify(handle => handle.AbortTransaction(It.IsAny<CancellationToken>()), Times.Never);
            mockCoreSession.Verify(handle => handle.CommitTransaction(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public void WithTransaction_without_errors_should_call_transaction_infrastructure_once()
        {
            var mockCoreSession = CreateCoreSessionMock();
            mockCoreSession.Setup(c => c.StartTransaction(It.IsAny<TransactionOptions>()));
            mockCoreSession.Setup(c => c.CommitTransaction(It.IsAny<CancellationToken>()));
            var subject = CreateSubject(coreSession: mockCoreSession.Object);

            SetupTransactionState(subject, true);

            subject.WithTransaction<object>((handle, cancellationToken) => 1);

            mockCoreSession.Verify(handle => handle.StartTransaction(It.IsAny<TransactionOptions>()), Times.Once);
            mockCoreSession.Verify(handle => handle.CommitTransaction(It.IsAny<CancellationToken>()), Times.Once);
        }

        // private methods
        private Mock<ICoreSessionHandle> CreateCoreSessionMock(
            ICoreServerSession serverSession = null,
            CoreSessionOptions options = null)
        {
            serverSession = serverSession ?? new CoreServerSession();
            options = options ?? new CoreSessionOptions();

            var mockCoreSession = new Mock<ICoreSessionHandle>();
            mockCoreSession.Setup(m => m.CurrentTransaction).Returns(new CoreTransaction(It.IsAny<long>(), It.IsAny<TransactionOptions>()));
            mockCoreSession.SetupGet(m => m.Options).Returns(options);
            mockCoreSession.SetupGet(m => m.ServerSession).Returns(serverSession);
            mockCoreSession.Setup(m => m.Fork()).Returns(() => CreateCoreSession(serverSession: serverSession, options: options));
            return mockCoreSession;
        }

        private ICoreSessionHandle CreateCoreSession(
            ICoreServerSession serverSession = null,
            CoreSessionOptions options = null)
        {
            return CreateCoreSessionMock(serverSession, options).Object;
        }

        private ClientSessionHandle CreateSubject(
            IMongoClient client = null,
            ClientSessionOptions options = null,
            ICoreSessionHandle coreSession = null,
            IClock clock = null)
        {
            client = client ?? Mock.Of<IMongoClient>();
            options = options ?? new ClientSessionOptions();
            coreSession = coreSession ?? CreateCoreSession(options: options.ToCore());
            clock = clock ?? SystemClock.Instance;
            return new ClientSessionHandle(client, options, coreSession, clock);
        }

        private MongoException PrepareException(WithTransactionErrorState state)
        {
            var mongoException = new MongoException("test");
            switch (state)
            {
                case WithTransactionErrorState.TransientTransactionError:
                    {
                        mongoException.AddErrorLabel(TransientTransactionErrorLabel);
                        return mongoException;
                    }
                case WithTransactionErrorState.UnknownTransactionCommitResult:
                    {
                        mongoException.AddErrorLabel(UnknownTransactionCommitResultLabel);
                        return mongoException;
                    }
                case WithTransactionErrorState.NoError:
                    return null;
                case WithTransactionErrorState.ErrorWithoutLabel:
                    {
                        return mongoException;
                    }
            }

            throw new ArgumentException("Not supported ErrorState", state.ToString());
        }

        private Mock<IClock> CreateClockMock(DateTime now, bool[] isRetryAttemptsWithTimeout, bool shouldNowBeAdded)
        {
            if (isRetryAttemptsWithTimeout == null)
            {
                isRetryAttemptsWithTimeout = new[] { false };
            }

            var mockClock = new Mock<IClock>();

            var nowSetup = mockClock.SetupSequence(c => c.UtcNow);
            if (shouldNowBeAdded)
            {
                nowSetup.Returns(now);
            }
            foreach (var isTimeoutAttempt in isRetryAttemptsWithTimeout)
            {
                var passedTime = CalculateTime(isTimeoutAttempt);
                nowSetup.Returns(now.AddSeconds(passedTime));
            }
            return mockClock;
        }

        private int CalculateTime(bool timeout)
        {
            return (int)TransactionExecutorReflector.__transactionTimeout().TotalSeconds + (timeout ? 10 : -10);
        }

        private void SetupTransactionState(ClientSessionHandle clientSession, bool isTransactionInProgress)
        {
            clientSession.WrappedCoreSession.CurrentTransaction.SetState(isTransactionInProgress ? CoreTransactionState.InProgress : CoreTransactionState.Aborted);
        }
    }

    internal static class ClientSessionHandleReflector
    {
        public static bool _disposed(this ClientSessionHandle obj) => (bool)Reflector.GetFieldValue(obj, nameof(_disposed));

        public static IServerSession _serverSession(this ClientSessionHandle obj) => (IServerSession)Reflector.GetFieldValue(obj, nameof(_serverSession));
    }

    // TransactionExecutor
    internal static class TransactionExecutorReflector
    {
        public static TimeSpan __transactionTimeout() => (TimeSpan)Reflector.GetStaticFieldValue(typeof(TransactionExecutor), nameof(__transactionTimeout));

        public static bool CommitWithRetries(IClientSessionHandle session, DateTime startTime, IClock clock, CancellationToken cancellationToken)
            => (bool)Reflector.InvokeStatic(typeof(TransactionExecutor), nameof(CommitWithRetries), session, startTime, clock, cancellationToken);

        public static Task<bool> CommitWithRetriesAsync(IClientSessionHandle session, DateTime startTime, IClock clock, CancellationToken cancellationToken)
            => (Task<bool>)Reflector.InvokeStatic(typeof(TransactionExecutor), nameof(CommitWithRetriesAsync), session, startTime, clock, cancellationToken);
    }
}
