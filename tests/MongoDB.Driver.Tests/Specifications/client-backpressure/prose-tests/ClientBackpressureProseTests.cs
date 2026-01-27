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
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.Servers;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests.Specifications.client_backpressure.prose_tests;

public class ClientBackpressureProseTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReadExecute_should_apply_backoff_when_SystemOverloadedError_occurs(bool async)
    {
        var operationMock = new Mock<IRetryableReadOperation<int>>();
        var exception = CreateSystemOverloadedErrorException();
        operationMock
            .Setup(o => o.ExecuteAttempt(It.IsAny<OperationContext>(), It.IsAny<RetryableReadContext>(), It.IsAny<int>(), It.IsAny<long?>()))
            .Throws(exception);
        operationMock
            .Setup(o => o.ExecuteAttemptAsync(It.IsAny<OperationContext>(), It.IsAny<RetryableReadContext>(), It.IsAny<int>(), It.IsAny<long?>()))
            .ThrowsAsync(exception);

        await AssertBackoffBehavior(
            async,
            CreateRetryableReadContext,
            (operationContext, context) => RetryableReadOperationExecutor.Execute(operationContext, operationMock.Object, context),
            async (operationContext, context) => await RetryableReadOperationExecutor.ExecuteAsync(operationContext, operationMock.Object, context));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WriteExecute_should_apply_backoff_when_SystemOverloadedError_occurs(bool async)
    {
        var operationMock = new Mock<IRetryableWriteOperation<int>>();
        var exception = CreateSystemOverloadedErrorException();
        operationMock.SetupGet(o => o.WriteConcern).Returns(WriteConcern.Acknowledged);
        operationMock
            .Setup(o => o.ExecuteAttempt(It.IsAny<OperationContext>(), It.IsAny<RetryableWriteContext>(), It.IsAny<int>(), It.IsAny<long?>()))
            .Throws(exception);
        operationMock
            .Setup(o => o.ExecuteAttemptAsync(It.IsAny<OperationContext>(), It.IsAny<RetryableWriteContext>(), It.IsAny<int>(), It.IsAny<long?>()))
            .ThrowsAsync(exception);

        await AssertBackoffBehavior(
            async,
            CreateRetryableWriteContext,
            (operationContext, context) => RetryableWriteOperationExecutor.Execute(operationContext, operationMock.Object, context),
            async (operationContext, context) => await RetryableWriteOperationExecutor.ExecuteAsync(operationContext, operationMock.Object, context));
    }

    private async Task AssertBackoffBehavior<TContext>(
        bool async,
        Func<IRandom, TContext> createContext,
        Action<OperationContext, TContext> executeSync,
        Func<OperationContext, TContext, Task> executeAsync)
    {
        // Test with no backoff (jitter = 0)
        var noBackoffRandom = new Mock<IRandom>();
        noBackoffRandom.Setup(r => r.NextDouble()).Returns(0.0);
        var noBackoffContext = createContext(noBackoffRandom.Object);

        var stopwatch = Stopwatch.StartNew();
        Exception noBackoffException;
        if (async)
        {
            noBackoffException = await Record.ExceptionAsync(() =>
                executeAsync(new OperationContext(TimeSpan.FromSeconds(30), CancellationToken.None), noBackoffContext));
        }
        else
        {
            noBackoffException = Record.Exception(() =>
                executeSync(new OperationContext(TimeSpan.FromSeconds(30), CancellationToken.None), noBackoffContext));
        }
        stopwatch.Stop();
        var noBackoffTime = stopwatch.ElapsedMilliseconds;

        noBackoffException.Should().NotBeNull();
        noBackoffException.Should().BeOfType<MongoCommandException>();

        // Test with full backoff (jitter = 1)
        var withBackoffRandom = new Mock<IRandom>();
        withBackoffRandom.Setup(r => r.NextDouble()).Returns(1.0);
        var withBackoffContext = createContext(withBackoffRandom.Object);

        stopwatch.Restart();
        Exception withBackoffException;
        if (async)
        {
            withBackoffException = await Record.ExceptionAsync(() =>
                executeAsync(new OperationContext(TimeSpan.FromSeconds(30), CancellationToken.None), withBackoffContext));
        }
        else
        {
            withBackoffException = Record.Exception(() =>
                executeSync(new OperationContext(TimeSpan.FromSeconds(30), CancellationToken.None), withBackoffContext));
        }
        stopwatch.Stop();
        var withBackoffTime = stopwatch.ElapsedMilliseconds;

        withBackoffException.Should().NotBeNull();
        withBackoffException.Should().BeOfType<MongoCommandException>();

        // Assert - Backoff should add at least 2100ms
        // The sum of 5 backoffs with jitter=1 is approximately 3100ms
        // We allow a 1-second tolerance window, so the difference should be at least 2100ms
        var difference = withBackoffTime - noBackoffTime;
        Assert.True(difference >= 2100, $"Expected at least 2100ms difference, got {difference}ms (noBackoff: {noBackoffTime}ms, withBackoff: {withBackoffTime}ms)");
    }

    private static MongoCommandException CreateSystemOverloadedErrorException()
    {
        var result = BsonDocument.Parse("{ ok: 0, code: 2, codeName: 'SystemOverloaded', errmsg: 'System overloaded', errorLabels: ['SystemOverloadedError', 'RetryableError'] }");
        var connectionId = new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017)));
        return new MongoCommandException(connectionId, "System overloaded", new BsonDocument("insert", "test"), result);
    }

    private static RetryableReadContext CreateRetryableReadContext(IRandom random)
    {
        // Create mock session
        var sessionMock = new Mock<ICoreSessionHandle>();
        sessionMock.SetupGet(s => s.IsInTransaction).Returns(false);
        sessionMock.SetupGet(s => s.Id).Returns((BsonDocument)null);

        // Create mock server with TokenBucket
        var serverMock = new Mock<IServer>();
        var tokenBucket = new TokenBucket();
        serverMock.SetupGet(s => s.TokenBucket).Returns(tokenBucket);

        // Create server description
        var endPoint = new DnsEndPoint("localhost", 27017);
        var serverId = new ServerId(new ClusterId(), endPoint);
        var serverDescription = new ServerDescription(serverId, endPoint);

        // Create mock channel
        var channelMock = new Mock<IChannelHandle>();

        // Create mock channel source
        var channelSourceMock = new Mock<IChannelSourceHandle>();
        channelSourceMock.SetupGet(cs => cs.Server).Returns(serverMock.Object);
        channelSourceMock.SetupGet(cs => cs.ServerDescription).Returns(serverDescription);
        channelSourceMock.Setup(cs => cs.GetChannel(It.IsAny<OperationContext>())).Returns(channelMock.Object);
        channelSourceMock.Setup(cs => cs.GetChannelAsync(It.IsAny<OperationContext>())).ReturnsAsync(channelMock.Object);

        // Create mock binding
        var bindingMock = new Mock<IReadBinding>();
        bindingMock.SetupGet(b => b.Session).Returns(sessionMock.Object);
        bindingMock.Setup(b => b.GetReadChannelSource(It.IsAny<OperationContext>(), It.IsAny<IReadOnlyCollection<ServerDescription>>()))
            .Returns(channelSourceMock.Object);
        bindingMock.Setup(b => b.GetReadChannelSourceAsync(It.IsAny<OperationContext>(), It.IsAny<IReadOnlyCollection<ServerDescription>>()))
            .ReturnsAsync(channelSourceMock.Object);

        // Create context with custom random - RetryableReadContext is sealed, so we use reflection
        var context = new RetryableReadContext(bindingMock.Object, retryRequested: true, random);

        // Use reflection to set the private _channelSource and _channel fields
        var contextType = typeof(RetryableReadContext);
        var channelSourceField = contextType.GetField("_channelSource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var channelField = contextType.GetField("_channel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        channelSourceField.SetValue(context, channelSourceMock.Object);
        channelField.SetValue(context, channelMock.Object);

        return context;
    }

    private static RetryableWriteContext CreateRetryableWriteContext(IRandom random)
    {
        // Create mock session
        var sessionMock = new Mock<ICoreSessionHandle>();
        sessionMock.SetupGet(s => s.IsInTransaction).Returns(false);
        sessionMock.SetupGet(s => s.Id).Returns(new BsonDocument("id", 1));

        // Create mock server with TokenBucket
        var serverMock = new Mock<IServer>();
        var tokenBucket = new TokenBucket();
        serverMock.SetupGet(s => s.TokenBucket).Returns(tokenBucket);

        // Create server description
        var endPoint = new DnsEndPoint("localhost", 27017);
        var serverId = new ServerId(new ClusterId(), endPoint);
        var serverDescription = new ServerDescription(serverId, endPoint, wireVersionRange: new Range<int>(0, 21), type: ServerType.ReplicaSetPrimary);

        // Create mock channel
        var channelMock = new Mock<IChannelHandle>();

        // Create mock channel source
        var channelSourceMock = new Mock<IChannelSourceHandle>();
        channelSourceMock.SetupGet(cs => cs.Server).Returns(serverMock.Object);
        channelSourceMock.SetupGet(cs => cs.ServerDescription).Returns(serverDescription);
        channelSourceMock.Setup(cs => cs.GetChannel(It.IsAny<OperationContext>())).Returns(channelMock.Object);
        channelSourceMock.Setup(cs => cs.GetChannelAsync(It.IsAny<OperationContext>())).ReturnsAsync(channelMock.Object);

        // Create mock binding
        var bindingMock = new Mock<IWriteBinding>();
        bindingMock.SetupGet(b => b.Session).Returns(sessionMock.Object);
        bindingMock.Setup(b => b.GetWriteChannelSource(It.IsAny<OperationContext>(), It.IsAny<IReadOnlyCollection<ServerDescription>>()))
            .Returns(channelSourceMock.Object);
        bindingMock.Setup(b => b.GetWriteChannelSourceAsync(It.IsAny<OperationContext>(), It.IsAny<IReadOnlyCollection<ServerDescription>>()))
            .ReturnsAsync(channelSourceMock.Object);

        // Create context with custom random - RetryableWriteContext is sealed, so we use reflection
        var context = new RetryableWriteContext(bindingMock.Object, retryRequested: true, random);

        // Use reflection to set the private _channelSource and _channel fields
        var contextType = typeof(RetryableWriteContext);
        var channelSourceField = contextType.GetField("_channelSource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var channelField = contextType.GetField("_channel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        channelSourceField.SetValue(context, channelSourceMock.Object);
        channelField.SetValue(context, channelMock.Object);

        return context;
    }
}
