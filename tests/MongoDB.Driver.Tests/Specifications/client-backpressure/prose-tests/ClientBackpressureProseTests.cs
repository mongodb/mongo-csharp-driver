﻿/* Copyright 2010-present MongoDB Inc.
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests.Specifications.client_backpressure.prose_tests;

public class ClientBackpressureProseTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReadExecute_should_apply_backoff_when_backpressure_errors_occurs(bool async)
    {
        var operationMock = new Mock<IRetryableReadOperation<int>>();
        var exception = CoreExceptionHelper.CreateMongoCommandExceptionWithLabels(2, "SystemOverloadedError", "RetryableError");
        operationMock
            .Setup(o => o.ExecuteAttempt(It.IsAny<OperationContext>(), It.IsAny<RetryableReadContext>(), It.IsAny<int>(), It.IsAny<long?>()))
            .Throws(exception);
        operationMock
            .Setup(o => o.ExecuteAttemptAsync(It.IsAny<OperationContext>(), It.IsAny<RetryableReadContext>(), It.IsAny<int>(), It.IsAny<long?>()))
            .ThrowsAsync(exception);

        await AssertBackoffBehavior(
            async,
            random => CreateRetryableReadContext(random),
            (operationContext, context) => RetryableReadOperationExecutor.Execute(operationContext, operationMock.Object, context),
            async (operationContext, context) => await RetryableReadOperationExecutor.ExecuteAsync(operationContext, operationMock.Object, context));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WriteExecute_should_apply_backoff_when_backpressure_errors_occurs(bool async)
    {
        var operationMock = new Mock<IRetryableWriteOperation<int>>();
        var exception = CoreExceptionHelper.CreateMongoCommandExceptionWithLabels(2, "SystemOverloadedError", "RetryableError");
        operationMock.SetupGet(o => o.WriteConcern).Returns(WriteConcern.Acknowledged);
        operationMock
            .Setup(o => o.ExecuteAttempt(It.IsAny<OperationContext>(), It.IsAny<RetryableWriteContext>(), It.IsAny<int>(), It.IsAny<long?>()))
            .Throws(exception);
        operationMock
            .Setup(o => o.ExecuteAttemptAsync(It.IsAny<OperationContext>(), It.IsAny<RetryableWriteContext>(), It.IsAny<int>(), It.IsAny<long?>()))
            .ThrowsAsync(exception);

        await AssertBackoffBehavior(
            async,
            random => CreateRetryableWriteContext(random),
            (operationContext, context) => RetryableWriteOperationExecutor.Execute(operationContext, operationMock.Object, context),
            async (operationContext, context) => await RetryableWriteOperationExecutor.ExecuteAsync(operationContext, operationMock.Object, context));
    }

    // Test 3: Overload Errors are Retried a Maximum of MAX_RETRIES times
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Overload_errors_are_retried_a_maximum_of_MAX_RETRIES_times(bool async)
    {
        // MAX_RETRIES = 2, so total started commands = MAX_RETRIES + 1 = 3
        await AssertMaxRetries(async, maxAdaptiveRetries: 2, expectedAttempts: 3);
    }

    // Test 4: Overload Errors are Retried a Maximum of maxAdaptiveRetries times when configured
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Overload_errors_are_retried_a_maximum_of_maxAdaptiveRetries_times_when_configured(bool async)
    {
        // maxAdaptiveRetries = 1, so total started commands = maxAdaptiveRetries + 1 = 2
        await AssertMaxRetries(async, maxAdaptiveRetries: 1, expectedAttempts: 2);
    }

    private async Task AssertMaxRetries(bool async, int maxAdaptiveRetries, int expectedAttempts)
    {
        var operationMock = new Mock<IRetryableReadOperation<int>>();
        var exception = CoreExceptionHelper.CreateMongoCommandExceptionWithLabels(462, "SystemOverloadedError", "RetryableError");
        operationMock
            .Setup(o => o.ExecuteAttempt(It.IsAny<OperationContext>(), It.IsAny<RetryableReadContext>(), It.IsAny<int>(), It.IsAny<long?>()))
            .Throws(exception);
        operationMock
            .Setup(o => o.ExecuteAttemptAsync(It.IsAny<OperationContext>(), It.IsAny<RetryableReadContext>(), It.IsAny<int>(), It.IsAny<long?>()))
            .ThrowsAsync(exception);

        var noBackoffRandom = new Mock<IRandom>();
        noBackoffRandom.Setup(r => r.NextDouble()).Returns(0.0);
        var context = CreateRetryableReadContext(noBackoffRandom.Object, maxAdaptiveRetries);

        var operationContext = new OperationContext(TimeSpan.FromSeconds(30), CancellationToken.None);

        Exception caughtException;
        if (async)
        {
            caughtException = await Record.ExceptionAsync(() =>
                RetryableReadOperationExecutor.ExecuteAsync(operationContext, operationMock.Object, context));
        }
        else
        {
            caughtException = Record.Exception(() =>
                RetryableReadOperationExecutor.Execute(operationContext, operationMock.Object, context));
        }

        var mongoException = caughtException.Should().BeAssignableTo<MongoException>().Subject;
        mongoException.HasErrorLabel("RetryableError").Should().BeTrue();
        mongoException.HasErrorLabel("SystemOverloadedError").Should().BeTrue();

        if (async)
        {
            operationMock.Verify(
                o => o.ExecuteAttemptAsync(It.IsAny<OperationContext>(), It.IsAny<RetryableReadContext>(), It.IsAny<int>(), It.IsAny<long?>()),
                Times.Exactly(expectedAttempts));
        }
        else
        {
            operationMock.Verify(
                o => o.ExecuteAttempt(It.IsAny<OperationContext>(), It.IsAny<RetryableReadContext>(), It.IsAny<int>(), It.IsAny<long?>()),
                Times.Exactly(expectedAttempts));
        }
    }

    private async Task AssertBackoffBehavior<TContext>(
        bool async,
        Func<IRandom, TContext> createContext,
        Action<OperationContext, TContext> executeSync,
        Func<OperationContext, TContext, Task> executeAsync)
    {
        var operationContext = new OperationContext(TimeSpan.FromSeconds(30), CancellationToken.None);

        // Test with no backoff (jitter = 0)
        var noBackoffRandom = new Mock<IRandom>();
        noBackoffRandom.Setup(r => r.NextDouble()).Returns(0.0);
        var noBackoffContext = createContext(noBackoffRandom.Object);

        var stopwatch = Stopwatch.StartNew();
        var noBackoffException = async
            ? await Record.ExceptionAsync(() => executeAsync(operationContext, noBackoffContext))
            : Record.Exception(() => executeSync(operationContext, noBackoffContext));
        stopwatch.Stop();
        var noBackoffTime = stopwatch.ElapsedMilliseconds;

        noBackoffException.Should().NotBeNull().And.BeOfType<MongoCommandException>();

        // Test with full backoff (jitter = 1)
        var withBackoffRandom = new Mock<IRandom>();
        withBackoffRandom.Setup(r => r.NextDouble()).Returns(1.0);
        var withBackoffContext = createContext(withBackoffRandom.Object);

        stopwatch.Restart();
        var withBackoffException = async
            ? await Record.ExceptionAsync(() => executeAsync(operationContext, withBackoffContext))
            : Record.Exception(() => executeSync(operationContext, withBackoffContext));
        stopwatch.Stop();
        var withBackoffTime = stopwatch.ElapsedMilliseconds;

        withBackoffException.Should().NotBeNull().And.BeOfType<MongoCommandException>();

        // With MAX_RETRIES=2 and jitter=1, total backoff = 100ms + 200ms = 300ms.
        // Per the spec: assertTrue(abs(with_backoff_time - (no_backoff_time + 0.3s)) < 0.3s)
        var difference = withBackoffTime - noBackoffTime;
        Math.Abs(difference - 300).Should().BeLessThan(300,
            $"backoff difference should be approximately 300ms, got {difference}ms (noBackoff: {noBackoffTime}ms, withBackoff: {withBackoffTime}ms)");
    }

    private static RetryableReadContext CreateRetryableReadContext(IRandom random, int maxAdaptiveRetries = 2)
    {
        var sessionMock = new Mock<ICoreSessionHandle>();
        sessionMock.SetupGet(s => s.IsInTransaction).Returns(false);
        var (channelSourceMock, channelMock) = CreateChannelMocks();

        var bindingMock = new Mock<IReadBinding>();
        bindingMock.SetupGet(b => b.Session).Returns(sessionMock.Object);
        bindingMock.SetupGet(b => b.MaxAdaptiveRetries).Returns(maxAdaptiveRetries);
        bindingMock.SetupGet(b => b.EnableOverloadRetargeting).Returns(false);
        bindingMock.Setup(b => b.GetReadChannelSource(It.IsAny<OperationContext>(), It.IsAny<IReadOnlyCollection<ServerDescription>>()))
            .Returns(channelSourceMock.Object);
        bindingMock.Setup(b => b.GetReadChannelSourceAsync(It.IsAny<OperationContext>(), It.IsAny<IReadOnlyCollection<ServerDescription>>()))
            .ReturnsAsync(channelSourceMock.Object);

        var context = new RetryableReadContext(bindingMock.Object, retryRequested: true, random: random);
        SetContextChannelFields(context, channelSourceMock.Object, channelMock.Object, typeof(RetryableReadContext));

        return context;
    }

    private static RetryableWriteContext CreateRetryableWriteContext(IRandom random)
    {
        var sessionMock = new Mock<ICoreSessionHandle>();
        sessionMock.SetupGet(s => s.IsInTransaction).Returns(false);
        sessionMock.SetupGet(s => s.Id).Returns(new BsonDocument("id", 1));
        var (channelSourceMock, channelMock) = CreateChannelMocks();

        var bindingMock = new Mock<IWriteBinding>();
        bindingMock.SetupGet(b => b.Session).Returns(sessionMock.Object);
        bindingMock.SetupGet(b => b.MaxAdaptiveRetries).Returns(2);
        bindingMock.SetupGet(b => b.EnableOverloadRetargeting).Returns(false);
        bindingMock.Setup(b => b.GetWriteChannelSource(It.IsAny<OperationContext>(), It.IsAny<IReadOnlyCollection<ServerDescription>>()))
            .Returns(channelSourceMock.Object);
        bindingMock.Setup(b => b.GetWriteChannelSourceAsync(It.IsAny<OperationContext>(), It.IsAny<IReadOnlyCollection<ServerDescription>>()))
            .ReturnsAsync(channelSourceMock.Object);

        var context = new RetryableWriteContext(bindingMock.Object, retryRequested: true, random);
        SetContextChannelFields(context, channelSourceMock.Object, channelMock.Object, typeof(RetryableWriteContext));

        return context;
    }

    private static (Mock<IChannelSourceHandle> channelSource, Mock<IChannelHandle> channel) CreateChannelMocks()
    {
        var serverMock = new Mock<IServer>();
        var serverId = new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017));
        var serverDescription = new ServerDescription(serverId, serverId.EndPoint);

        var channelMock = new Mock<IChannelHandle>();

        var channelSourceMock = new Mock<IChannelSourceHandle>();
        channelSourceMock.SetupGet(cs => cs.Server).Returns(serverMock.Object);
        channelSourceMock.SetupGet(cs => cs.ServerDescription).Returns(serverDescription);
        channelSourceMock.Setup(cs => cs.GetChannel(It.IsAny<OperationContext>())).Returns(channelMock.Object);
        channelSourceMock.Setup(cs => cs.GetChannelAsync(It.IsAny<OperationContext>())).ReturnsAsync(channelMock.Object);

        return (channelSourceMock, channelMock);
    }

    private static void SetContextChannelFields(object context, IChannelSourceHandle channelSource, IChannelHandle channel, Type contextType)
    {
        var channelSourceField = contextType.GetField("_channelSource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var channelField = contextType.GetField("_channel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        channelSourceField.SetValue(context, channelSource);
        channelField.SetValue(context, channel);
    }
}
