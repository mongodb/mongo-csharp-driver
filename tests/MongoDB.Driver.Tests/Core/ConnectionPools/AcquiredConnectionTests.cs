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
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Logging;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using MongoDB.TestHelpers.XunitExtensions;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Core.ConnectionPools;

public class AcquiredConnectionTests : LoggableTestClass
{
    private readonly EventCapturer _capturedEvents;
    private readonly DnsEndPoint _endPoint;
    private readonly MessageEncoderSettings _messageEncoderSettings = new MessageEncoderSettings();
    private readonly Mock<IConnectionInitializer> _mockConnectionInitializer;
    private readonly ServerId _serverId;

    public AcquiredConnectionTests(ITestOutputHelper output) : base(output)
    {
        _capturedEvents = new EventCapturer().Capture<ConnectionPoolRemovedConnectionEvent>();
        _endPoint = new DnsEndPoint("localhost", 27017);
        _serverId = new ServerId(new ClusterId(), _endPoint);

        var connectionId = new ConnectionId(_serverId);
        var helloResult = new HelloResult(new BsonDocument { { "ok", 1 }, { "maxMessageSizeBytes", 48000000 }, { "maxWireVersion", WireVersion.Server36 } });
        var connectionDescription = new ConnectionDescription(connectionId, helloResult);
        var connectionInitializerContext = new ConnectionInitializerContext(connectionDescription, null);

        _mockConnectionInitializer = new Mock<IConnectionInitializer>();
        _mockConnectionInitializer
            .Setup(i => i.SendHello(It.IsAny<OperationContext>(), It.IsAny<IConnection>()))
            .Returns(connectionInitializerContext);
        _mockConnectionInitializer
            .Setup(i => i.SendHelloAsync(It.IsAny<OperationContext>(), It.IsAny<IConnection>()))
            .ReturnsAsync(connectionInitializerContext);
        _mockConnectionInitializer
            .Setup(i => i.Authenticate(It.IsAny<OperationContext>(), It.IsAny<IConnection>(), It.IsAny<ConnectionInitializerContext>()))
            .Returns(connectionInitializerContext);
        _mockConnectionInitializer
            .Setup(i => i.AuthenticateAsync(It.IsAny<OperationContext>(), It.IsAny<IConnection>(), It.IsAny<ConnectionInitializerContext>()))
            .ReturnsAsync(connectionInitializerContext);
    }

    [Theory]
    [ParameterAttributeData]
    public async Task SendMessage_should_throw_retryable_exception_when_connection_was_interrupted_by_pool_clear(
        [Values(false, true)]
        bool async)
    {
        using var pool = CreatePool(new MemoryStream());
        pool.Initialize();
        pool.SetReady();
        using var acquired = pool.AcquireConnection(OperationContext.NoTimeout);
        var message = MessageHelper.BuildCommand(new BsonDocument("ping", 1));

        pool.Clear(closeInUseConnections: true);
        _capturedEvents.WaitForEventOrThrowIfTimeout<ConnectionPoolRemovedConnectionEvent>(TimeSpan.FromSeconds(5));

        var exception = async ?
            await Record.ExceptionAsync(() => acquired.SendMessageAsync(OperationContext.NoTimeout, message, _messageEncoderSettings)) :
            Record.Exception(() => acquired.SendMessage(OperationContext.NoTimeout, message, _messageEncoderSettings));

        exception.Should().BeOfType<MongoConnectionException>("an operation whose connection was interrupted by a pool clear should observe a connection error, not a raw ObjectDisposedException");
    }

    [Theory]
    [ParameterAttributeData]
    public async Task ReceiveMessage_should_throw_retryable_exception_when_connection_was_interrupted_by_pool_clear_mid_receive(
        [Values(false, true)]
        bool async)
    {
        var readTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var readStarted = new ManualResetEventSlim(initialState: false);
        var mockStream = CreateBlockingStream(readTcs, readStarted);
        using var pool = CreatePool(mockStream.Object);
        pool.Initialize();
        pool.SetReady();
        using var acquired = pool.AcquireConnection(OperationContext.NoTimeout);
        var encoderSelector = new ReplyMessageEncoderSelector<BsonDocument>(BsonDocumentSerializer.Instance);

        var receiveTask = async ?
            acquired.ReceiveMessageAsync(OperationContext.NoTimeout, 1, encoderSelector, _messageEncoderSettings) :
            Task.Run(() => acquired.ReceiveMessage(OperationContext.NoTimeout, 1, encoderSelector, _messageEncoderSettings));
        readStarted.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();

        pool.Clear(closeInUseConnections: true);
        _capturedEvents.WaitForEventOrThrowIfTimeout<ConnectionPoolRemovedConnectionEvent>(TimeSpan.FromSeconds(5));

        var exception = await Record.ExceptionAsync(() => receiveTask);

        exception.Should().BeOfType<MongoConnectionException>("an operation whose connection was interrupted mid-receive by a pool clear should observe a connection error, not a raw ObjectDisposedException");
    }

    // private methods
    private static Mock<Stream> CreateBlockingStream(TaskCompletionSource<int> readTcs, ManualResetEventSlim readStarted)
    {
        var mockStream = new Mock<Stream>();
        mockStream
            .Setup(s => s.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns((byte[] _, int __, int ___) =>
            {
                readStarted.Set();
                return readTcs.Task.GetAwaiter().GetResult();
            });
        mockStream
            .Setup(s => s.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                readStarted.Set();
                return readTcs.Task;
            });
        mockStream
            .Setup(s => s.Close())
            .Callback(() => readTcs.TrySetException(new ObjectDisposedException("stream")));
        return mockStream;
    }

    private ExclusiveConnectionPool CreatePool(Stream stream)
    {
        var mockStreamFactory = new Mock<IStreamFactory>();
        mockStreamFactory
            .Setup(f => f.CreateStream(It.IsAny<EndPoint>(), It.IsAny<CancellationToken>()))
            .Returns(stream);
        mockStreamFactory
            .Setup(f => f.CreateStreamAsync(It.IsAny<EndPoint>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stream);

        var mockConnectionFactory = new Mock<IConnectionFactory>();
        mockConnectionFactory.Setup(f => f.ConnectionSettings).Returns(() => new ConnectionSettings());
        mockConnectionFactory
            .Setup(f => f.CreateConnection(_serverId, _endPoint))
            .Returns(() => new BinaryConnection(
                serverId: _serverId,
                endPoint: _endPoint,
                settings: new ConnectionSettings(),
                streamFactory: mockStreamFactory.Object,
                connectionInitializer: _mockConnectionInitializer.Object,
                eventSubscriber: _capturedEvents,
                loggerFactory: LoggerFactory,
                tracingOptions: null,
                socketReadTimeout: Timeout.InfiniteTimeSpan,
                socketWriteTimeout: Timeout.InfiniteTimeSpan));

        return new ExclusiveConnectionPool(
            _serverId,
            _endPoint,
            new ConnectionPoolSettings(maintenanceInterval: TimeSpan.FromDays(1), minConnections: 0),
            mockConnectionFactory.Object,
            Mock.Of<IConnectionExceptionHandler>(),
            _capturedEvents.ToEventLogger<LogCategories.Connection>());
    }
}
