/* Copyright 2010-present MongoDB Inc.
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
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests.Specifications.client_backpressure.prose_tests;

// Test 1 requires controlling the RNG for precise timing assertions, so it uses unit tests with mocks.
public class ClientBackpressureProseTestsUnit
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    // https://github.com/mongodb/specifications/blob/7039e69945d463a14b1b727d16db063e21f48f53/source/client-backpressure/tests/README.md#test-1-operation-retry-uses-exponential-backoff
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
    // https://github.com/mongodb/specifications/blob/7039e69945d463a14b1b727d16db063e21f48f53/source/client-backpressure/tests/README.md#test-1-operation-retry-uses-exponential-backoff
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    // https://github.com/mongodb/specifications/pull/1953 (Test 5: overload errors with baseBackoffMS override base backoff)
    public async Task ReadExecute_should_use_baseBackoffMs_as_backoff_base(bool async)
    {
        await AssertBaseBackoffMsOverridesBackoff(
            async,
            random => CreateRetryableReadContext(random),
            exception =>
            {
                var operationMock = new Mock<IRetryableReadOperation<int>>();
                operationMock
                    .Setup(o => o.ExecuteAttempt(It.IsAny<OperationContext>(), It.IsAny<RetryableReadContext>(), It.IsAny<int>(), It.IsAny<long?>()))
                    .Throws(exception);
                operationMock
                    .Setup(o => o.ExecuteAttemptAsync(It.IsAny<OperationContext>(), It.IsAny<RetryableReadContext>(), It.IsAny<int>(), It.IsAny<long?>()))
                    .ThrowsAsync(exception);
                return operationMock.Object;
            },
            (operationContext, operation, context) => RetryableReadOperationExecutor.Execute(operationContext, operation, context),
            (operationContext, operation, context) => RetryableReadOperationExecutor.ExecuteAsync(operationContext, operation, context));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    // https://github.com/mongodb/specifications/pull/1953 (Test 5: overload errors with baseBackoffMS override base backoff)
    public async Task WriteExecute_should_use_baseBackoffMs_as_backoff_base(bool async)
    {
        await AssertBaseBackoffMsOverridesBackoff(
            async,
            random => CreateRetryableWriteContext(random),
            exception =>
            {
                var operationMock = new Mock<IRetryableWriteOperation<int>>();
                operationMock.SetupGet(o => o.WriteConcern).Returns(WriteConcern.Acknowledged);
                operationMock
                    .Setup(o => o.ExecuteAttempt(It.IsAny<OperationContext>(), It.IsAny<RetryableWriteContext>(), It.IsAny<int>(), It.IsAny<long?>()))
                    .Throws(exception);
                operationMock
                    .Setup(o => o.ExecuteAttemptAsync(It.IsAny<OperationContext>(), It.IsAny<RetryableWriteContext>(), It.IsAny<int>(), It.IsAny<long?>()))
                    .ThrowsAsync(exception);
                return operationMock.Object;
            },
            (operationContext, operation, context) => RetryableWriteOperationExecutor.Execute(operationContext, operation, context),
            (operationContext, operation, context) => RetryableWriteOperationExecutor.ExecuteAsync(operationContext, operation, context));
    }

    private async Task AssertBackoffBehavior<TContext>(
        bool async,
        Func<IRandom, TContext> createContext,
        Action<OperationContext, TContext> executeSync,
        Func<OperationContext, TContext, Task> executeAsync)
    {
        using var session = NoCoreSession.NewHandle();
        using var operationContext = new OperationContext(session, timeout: TimeSpan.FromSeconds(30));

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

    private async Task AssertBaseBackoffMsOverridesBackoff<TOperation, TContext>(
        bool async,
        Func<IRandom, TContext> createContext,
        Func<Exception, TOperation> createOperation,
        Action<OperationContext, TOperation, TContext> executeSync,
        Func<OperationContext, TOperation, TContext, Task> executeAsync)
    {
        var operationContext = new OperationContext(TimeSpan.FromSeconds(30), CancellationToken.None);

        var fullJitterRandom = new Mock<IRandom>();
        fullJitterRandom.Setup(r => r.NextDouble()).Returns(1.0);

        // Exponential backoff run: overload error without baseBackoffMS -> 100ms + 200ms = 300ms with jitter = 1.
        var exponentialException = CoreExceptionHelper.CreateMongoCommandExceptionWithLabels(462, "SystemOverloadedError", "RetryableError");
        var exponentialMs = await MeasureBackoff(async, executeSync, executeAsync, operationContext, createOperation(exponentialException), createContext(fullJitterRandom.Object));

        // baseBackoffMS override run: overload error with baseBackoffMS = 50 -> 50ms + 100ms = 150ms with jitter = 1.
        var overrideResult = BsonDocument.Parse("{ ok : 0, code : 462, baseBackoffMS : 50 }");
        var overrideException = CoreExceptionHelper.CreateMongoCommandExceptionWithLabels(overrideResult, "SystemOverloadedError", "RetryableError");
        var withBaseBackoffMs = await MeasureBackoff(async, executeSync, executeAsync, operationContext, createOperation(overrideException), createContext(fullJitterRandom.Object));

        // Per the spec: assertTrue(abs(exponential_backoff_time - (with_retry_after_ms_time + 0.2s)) < 0.2s)
        Math.Abs(exponentialMs - (withBaseBackoffMs + 200)).Should().BeLessThan(200,
            $"baseBackoffMS should shorten the backoff base (exponential: {exponentialMs}ms, withBaseBackoffMS: {withBaseBackoffMs}ms)");
    }

    private static async Task<long> MeasureBackoff<TOperation, TContext>(
        bool async,
        Action<OperationContext, TOperation, TContext> executeSync,
        Func<OperationContext, TOperation, TContext, Task> executeAsync,
        OperationContext operationContext,
        TOperation operation,
        TContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var exception = async
            ? await Record.ExceptionAsync(() => executeAsync(operationContext, operation, context))
            : Record.Exception(() => executeSync(operationContext, operation, context));
        stopwatch.Stop();

        exception.Should().NotBeNull().And.BeOfType<MongoCommandException>();
        return stopwatch.ElapsedMilliseconds;
    }

    private static RetryableReadContext CreateRetryableReadContext(IRandom random, int maxAdaptiveRetries = 2)
    {
        var (channelSourceMock, channelMock) = CreateChannelMocks();

        var bindingMock = new Mock<IReadBinding>();
        bindingMock.Setup(b => b.GetReadChannelSource(It.IsAny<OperationContext>(), It.IsAny<IReadOnlyCollection<ServerDescription>>()))
            .Returns(channelSourceMock.Object);
        bindingMock.Setup(b => b.GetReadChannelSourceAsync(It.IsAny<OperationContext>(), It.IsAny<IReadOnlyCollection<ServerDescription>>()))
            .ReturnsAsync(channelSourceMock.Object);

        var context = new RetryableReadContext(bindingMock.Object, retryRequested: true, maxAdaptiveRetries, false, random);
        SetContextChannelFields(context, channelSourceMock.Object, channelMock.Object, typeof(RetryableReadContext));

        return context;
    }

    private static RetryableWriteContext CreateRetryableWriteContext(IRandom random)
    {
        var (channelSourceMock, channelMock) = CreateChannelMocks();

        var bindingMock = new Mock<IWriteBinding>();
        bindingMock.Setup(b => b.GetWriteChannelSource(It.IsAny<OperationContext>(), It.IsAny<IReadOnlyCollection<ServerDescription>>()))
            .Returns(channelSourceMock.Object);
        bindingMock.Setup(b => b.GetWriteChannelSourceAsync(It.IsAny<OperationContext>(), It.IsAny<IReadOnlyCollection<ServerDescription>>()))
            .ReturnsAsync(channelSourceMock.Object);

        var context = new RetryableWriteContext(bindingMock.Object, retryRequested: true, RetryabilityHelper.OperationRetryBackpressureConstants.DefaultMaxRetries, false, random);
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

[Trait("Category", "Integration")]
public class ClientBackpressureProseTestsIntegration
{
    [Fact]
    // https://github.com/mongodb/specifications/blob/7039e69945d463a14b1b727d16db063e21f48f53/source/client-backpressure/tests/README.md#test-3-overload-errors-are-retried-a-maximum-of-max_retries-times
    public void Overload_errors_are_retried_a_maximum_of_MAX_RETRIES_times()
    {
        RequireServer.Check()
            .ClusterTypes(ClusterType.ReplicaSet)
            .VersionGreaterThanOrEqualTo("4.4.0");

        var failPointCommand = BsonDocument.Parse(
            @"{
                configureFailPoint: ""failCommand"",
                mode: ""alwaysOn"",
                data:
                {
                    failCommands: [""find""],
                    errorCode: 462,
                    errorLabels: [""SystemOverloadedError"", ""RetryableError""]
                }
            }");

        var eventCapturer = new EventCapturer().CaptureCommandEvents("find");

        using var failPoint = FailPoint.Configure(failPointCommand);
        using var client = DriverTestConfiguration.CreateMongoClient(s =>
        {
            s.RetryReads = true;
            s.ClusterConfigurator = b => b.Subscribe(eventCapturer);
        });

        var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
        var collection = database.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);

        var exception = Record.Exception(() => collection.FindSync(Builders<BsonDocument>.Filter.Empty));

        var mongoException = exception.Should().BeAssignableTo<MongoException>().Subject;
        mongoException.HasErrorLabel("RetryableError").Should().BeTrue();
        mongoException.HasErrorLabel("SystemOverloadedError").Should().BeTrue();

        // MAX_RETRIES = 2, so total started commands = MAX_RETRIES + 1 = 3
        var expectedAttempts = RetryabilityHelper.OperationRetryBackpressureConstants.DefaultMaxRetries + 1;
        eventCapturer.Events.OfType<CommandStartedEvent>().Count().Should().Be(expectedAttempts);
    }

    [Fact]
    // https://github.com/mongodb/specifications/blob/7039e69945d463a14b1b727d16db063e21f48f53/source/client-backpressure/tests/README.md#test-4-overload-errors-are-retried-a-maximum-of-maxadaptiveretries-times-when-configured
    public void Overload_errors_are_retried_a_maximum_of_maxAdaptiveRetries_times_when_configured()
    {
        RequireServer.Check()
            .ClusterTypes(ClusterType.ReplicaSet)
            .VersionGreaterThanOrEqualTo("4.4.0");

        var failPointCommand = BsonDocument.Parse(
            @"{
                configureFailPoint: ""failCommand"",
                mode: ""alwaysOn"",
                data:
                {
                    failCommands: [""find""],
                    errorCode: 462,
                    errorLabels: [""SystemOverloadedError"", ""RetryableError""]
                }
            }");

        var eventCapturer = new EventCapturer().CaptureCommandEvents("find");

        using var failPoint = FailPoint.Configure(failPointCommand);
        using var client = DriverTestConfiguration.CreateMongoClient(s =>
        {
            s.RetryReads = true;
            s.MaxAdaptiveRetries = 1;
            s.ClusterConfigurator = b => b.Subscribe(eventCapturer);
        });

        var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
        var collection = database.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);

        var exception = Record.Exception(() => collection.FindSync(Builders<BsonDocument>.Filter.Empty));

        var mongoException = exception.Should().BeAssignableTo<MongoException>().Subject;
        mongoException.HasErrorLabel("RetryableError").Should().BeTrue();
        mongoException.HasErrorLabel("SystemOverloadedError").Should().BeTrue();

        // maxAdaptiveRetries = 1, so total started commands = maxAdaptiveRetries + 1 = 2
        eventCapturer.Events.OfType<CommandStartedEvent>().Count().Should().Be(2);
    }
}
