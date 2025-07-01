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
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.IO;
using MongoDB.Driver.Authentication;
using MongoDB.Driver.Core.Compression;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Logging;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders;

namespace MongoDB.Driver.Core.Connections
{
    internal sealed class BinaryConnection : IConnection
    {
        // fields
        private readonly CommandEventHelper _commandEventHelper;
        private readonly ICompressorSource _compressorSource;
        private ConnectionId _connectionId;
        private readonly IConnectionInitializer _connectionInitializer;
        private ConnectionInitializerContext _connectionInitializerContext;
        private EndPoint _endPoint;
        private ConnectionDescription _description;
        private readonly Dropbox _dropbox = new Dropbox();
        private bool _failedEventHasBeenRaised;
        private DateTime _lastUsedAtUtc;
        private DateTime _openedAtUtc;
        private readonly object _openLock = new object();
        private Task _openTask;
        private readonly SemaphoreSlim _receiveLock;
        private CompressorType? _sendCompressorType;
        private readonly SemaphoreSlim _sendLock;
        private readonly ConnectionSettings _settings;
        private readonly InterlockedInt32 _state;
        private Stream _stream;
        private readonly IStreamFactory _streamFactory;
        private readonly EventLogger<LogCategories.Connection> _eventLogger;

        // constructors
        public BinaryConnection(
            ServerId serverId,
            EndPoint endPoint,
            ConnectionSettings settings,
            IStreamFactory streamFactory,
            IConnectionInitializer connectionInitializer,
            IEventSubscriber eventSubscriber,
            ILoggerFactory loggerFactory)
        {
            Ensure.IsNotNull(serverId, nameof(serverId));
            _endPoint = Ensure.IsNotNull(endPoint, nameof(endPoint));
            _settings = Ensure.IsNotNull(settings, nameof(settings));
            _streamFactory = Ensure.IsNotNull(streamFactory, nameof(streamFactory));
            _connectionInitializer = Ensure.IsNotNull(connectionInitializer, nameof(connectionInitializer));
            Ensure.IsNotNull(eventSubscriber, nameof(eventSubscriber));

            _connectionId = new ConnectionId(serverId, settings.ConnectionIdLocalValueProvider());
            _receiveLock = new SemaphoreSlim(1);
            _sendLock = new SemaphoreSlim(1);
            _state = new InterlockedInt32(State.Initial);

            _compressorSource = new CompressorSource(settings.Compressors);
            _eventLogger = loggerFactory.CreateEventLogger<LogCategories.Connection>(eventSubscriber);
            _commandEventHelper = new CommandEventHelper(loggerFactory.CreateEventLogger<LogCategories.Command>(eventSubscriber));
        }

        // properties
        public ConnectionId ConnectionId
        {
            get { return _connectionId; }
        }

        public ConnectionDescription Description
        {
            get { return _description; }
        }

        public EndPoint EndPoint
        {
            get { return _endPoint; }
        }

        public int Generation
        {
            get => throw new NotSupportedException();
        }

        public bool IsExpired
        {
            get
            {
                var now = DateTime.UtcNow;

                // connection has been alive for too long
                if (_settings.MaxLifeTime.TotalMilliseconds > -1 && now > _openedAtUtc.Add(_settings.MaxLifeTime))
                {
                    return true;
                }

                // connection has been idle for too long
                if (_settings.MaxIdleTime.TotalMilliseconds > -1 && now > _lastUsedAtUtc.Add(_settings.MaxIdleTime))
                {
                    return true;
                }

                return _state.Value > State.Open;
            }
        }

        public ConnectionSettings Settings
        {
            get { return _settings; }
        }

        private bool IsInitializing => _state.Value == State.Initializing;

        // methods
        private void ConnectionFailed(Exception exception)
        {
            if (!_state.TryChange(State.Open, State.Failed) && !_state.TryChange(State.Initializing, State.Failed))
            {
                var currentState = _state.Value;
                if (currentState != State.Failed && currentState != State.Disposed)
                {
                    throw new InvalidOperationException($"Invalid BinaryConnection state transition from {currentState} to Failed.");
                }
            }

            if (!_failedEventHasBeenRaised)
            {
                _failedEventHasBeenRaised = true;
                _eventLogger.LogAndPublish(new ConnectionFailedEvent(_connectionId, exception));
                _commandEventHelper.ConnectionFailed(_connectionId, _description?.ServiceId, exception, IsInitializing);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_state.TryChange(State.Disposed))
            {
                if (disposing)
                {
                    _eventLogger.LogAndPublish(new ConnectionClosingEvent(_connectionId, EventContext.OperationId));

                    var stopwatch = Stopwatch.StartNew();
                    _receiveLock.Dispose();
                    _sendLock.Dispose();

                    if (_stream != null)
                    {
                        try
                        {
                            _stream.Dispose();
                        }
                        catch
                        {
                            // eat this...
                        }
                    }

                    stopwatch.Stop();
                    _eventLogger.LogAndPublish(new ConnectionClosedEvent(_connectionId, stopwatch.Elapsed, EventContext.OperationId));
                }
            }
        }

        private void EnsureMessageSizeIsValid(int messageSize)
        {
            var maxMessageSize = _description?.MaxMessageSize ?? 48000000;

            if (messageSize < 0 || messageSize > maxMessageSize)
            {
                throw new FormatException("The size of the message is invalid.");
            }
        }

        public void Open(OperationContext operationContext)
        {
            ThrowIfCancelledOrDisposed(operationContext);

            TaskCompletionSource<bool> taskCompletionSource = null;
            var connecting = false;
            lock (_openLock)
            {
                if (_state.TryChange(State.Initial, State.Connecting))
                {
                    _openedAtUtc = DateTime.UtcNow;
                    taskCompletionSource = new TaskCompletionSource<bool>();
                    _openTask = taskCompletionSource.Task;
                    _openTask.IgnoreExceptions();
                    connecting = true;
                }
            }

            if (connecting)
            {
                try
                {
                    OpenHelper(operationContext);
                    taskCompletionSource.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    taskCompletionSource.TrySetException(ex);
                    throw;
                }
            }
            else
            {
                _openTask.GetAwaiter().GetResult();
            }
        }

        public Task OpenAsync(OperationContext operationContext)
        {
            ThrowIfCancelledOrDisposed(operationContext);

            lock (_openLock)
            {
                if (_state.TryChange(State.Initial, State.Connecting))
                {
                    _openedAtUtc = DateTime.UtcNow;
                    _openTask = OpenHelperAsync(operationContext);
                }
                return _openTask;
            }
        }

        private void OpenHelper(OperationContext operationContext)
        {
            var helper = new OpenConnectionHelper(this);
            ConnectionDescription handshakeDescription = null;
            try
            {
                helper.OpeningConnection();
#pragma warning disable CS0618 // Type or member is obsolete
                _stream = _streamFactory.CreateStream(_endPoint, operationContext.CombinedCancellationToken);
#pragma warning restore CS0618 // Type or member is obsolete
                helper.InitializingConnection();
                // TODO: CSOT: Implement operation context support for MongoDB Handshake
                _connectionInitializerContext = _connectionInitializer.SendHello(this, operationContext.CancellationToken);
                handshakeDescription = _connectionInitializerContext.Description;
                // TODO: CSOT: Implement operation context support for Auth
                _connectionInitializerContext = _connectionInitializer.Authenticate(this, _connectionInitializerContext, operationContext.CancellationToken);
                _description = _connectionInitializerContext.Description;
                _sendCompressorType = ChooseSendCompressorTypeIfAny(_description);

                helper.OpenedConnection();
            }
            catch (Exception ex)
            {
                _description ??= handshakeDescription;
                var wrappedException = WrapExceptionIfRequired(ex, "opening a connection to the server");
                helper.FailedOpeningConnection(wrappedException ?? ex);
                if (wrappedException == null) { throw; } else { throw wrappedException; }
            }
        }

        private async Task OpenHelperAsync(OperationContext operationContext)
        {
            var helper = new OpenConnectionHelper(this);
            ConnectionDescription handshakeDescription = null;
            try
            {
                helper.OpeningConnection();
#pragma warning disable CS0618 // Type or member is obsolete
                _stream = await _streamFactory.CreateStreamAsync(_endPoint, operationContext.CombinedCancellationToken).ConfigureAwait(false);
#pragma warning restore CS0618 // Type or member is obsolete
                helper.InitializingConnection();
                // TODO: CSOT: Implement operation context support for MongoDB Handshake
                _connectionInitializerContext = await _connectionInitializer.SendHelloAsync(this, operationContext.CancellationToken).ConfigureAwait(false);
                handshakeDescription = _connectionInitializerContext.Description;
                // TODO: CSOT: Implement operation context support for Auth
                _connectionInitializerContext = await _connectionInitializer.AuthenticateAsync(this, _connectionInitializerContext, operationContext.CancellationToken).ConfigureAwait(false);
                _description = _connectionInitializerContext.Description;
                _sendCompressorType = ChooseSendCompressorTypeIfAny(_description);
                helper.OpenedConnection();
            }
            catch (Exception ex)
            {
                _description ??= handshakeDescription;
                var wrappedException = WrapExceptionIfRequired(ex, "opening a connection to the server");
                helper.FailedOpeningConnection(wrappedException ?? ex);
                if (wrappedException == null) { throw; } else { throw wrappedException; }
            }
        }

        public void Reauthenticate(CancellationToken cancellationToken)
        {
            InvalidateAuthenticator();
            _connectionInitializerContext = _connectionInitializer.Authenticate(this, _connectionInitializerContext, cancellationToken);
        }

        public async Task ReauthenticateAsync(CancellationToken cancellationToken)
        {
            InvalidateAuthenticator();
            _connectionInitializerContext = await _connectionInitializer.AuthenticateAsync(this, _connectionInitializerContext, cancellationToken).ConfigureAwait(false);
        }

        private void InvalidateAuthenticator()
        {
            if (_connectionInitializerContext?.Authenticator is SaslAuthenticator saslAuthenticator)
            {
                saslAuthenticator.Mechanism.OnReAuthenticationRequired();
            }
        }

        private IByteBuffer ReceiveBuffer(OperationContext operationContext)
        {
            try
            {
                var messageSizeBytes = new byte[4];
                _stream.ReadBytes(operationContext, messageSizeBytes, 0, 4);
                var messageSize = BinaryPrimitives.ReadInt32LittleEndian(messageSizeBytes);
                EnsureMessageSizeIsValid(messageSize);
                var inputBufferChunkSource = new InputBufferChunkSource(BsonChunkPool.Default);
                var buffer = ByteBufferFactory.Create(inputBufferChunkSource, messageSize);
                buffer.Length = messageSize;
                buffer.SetBytes(0, messageSizeBytes, 0, 4);
                _stream.ReadBytes(operationContext, buffer, 4, messageSize - 4);
                _lastUsedAtUtc = DateTime.UtcNow;
                buffer.MakeReadOnly();
                return buffer;
            }
            catch (Exception ex)
            {
                var wrappedException = WrapExceptionIfRequired(ex, "receiving a message from the server");
                ConnectionFailed(wrappedException ?? ex);
                if (wrappedException == null) { throw; } else { throw wrappedException; }
            }
        }

        private IByteBuffer ReceiveBuffer(OperationContext operationContext, int responseTo)
        {
            using (var receiveLockRequest = new SemaphoreSlimRequest(_receiveLock, operationContext.RemainingTimeout, operationContext.CancellationToken))
            {
                var messageTask = _dropbox.GetMessageAsync(responseTo);
                try
                {
                    Task.WaitAny(messageTask, receiveLockRequest.Task);
                    if (messageTask.IsCompleted)
                    {
                        return _dropbox.RemoveMessage(responseTo); // also propagates exception if any
                    }

                    receiveLockRequest.Task.GetAwaiter().GetResult(); // propagate exceptions
                    while (true)
                    {
                        try
                        {
                            var buffer = ReceiveBuffer(operationContext);
                            _dropbox.AddMessage(buffer);
                        }
                        catch (Exception ex)
                        {
                            _dropbox.AddException(ex);
                        }

                        if (messageTask.IsCompleted)
                        {
                            return _dropbox.RemoveMessage(responseTo); // also propagates exception if any
                        }

                        operationContext.ThrowIfTimedOutOrCanceled();
                    }
                }
                catch
                {
                    var ignored = messageTask.ContinueWith(
                        t => { _dropbox.RemoveMessage(responseTo).Dispose(); },
                        TaskContinuationOptions.OnlyOnRanToCompletion);
                    throw;
                }
            }
        }

        private async Task<IByteBuffer> ReceiveBufferAsync(OperationContext operationContext)
        {
            try
            {
                var messageSizeBytes = new byte[4];
                await _stream.ReadBytesAsync(operationContext, messageSizeBytes, 0, 4).ConfigureAwait(false);
                var messageSize = BinaryPrimitives.ReadInt32LittleEndian(messageSizeBytes);
                EnsureMessageSizeIsValid(messageSize);
                var inputBufferChunkSource = new InputBufferChunkSource(BsonChunkPool.Default);
                var buffer = ByteBufferFactory.Create(inputBufferChunkSource, messageSize);
                buffer.Length = messageSize;
                buffer.SetBytes(0, messageSizeBytes, 0, 4);
                await _stream.ReadBytesAsync(operationContext, buffer, 4, messageSize - 4).ConfigureAwait(false);
                _lastUsedAtUtc = DateTime.UtcNow;
                buffer.MakeReadOnly();
                return buffer;
            }
            catch (Exception ex)
            {
                var wrappedException = WrapExceptionIfRequired(ex, "receiving a message from the server");
                ConnectionFailed(wrappedException ?? ex);
                if (wrappedException == null) { throw; } else { throw wrappedException; }
            }
        }

        private async Task<IByteBuffer> ReceiveBufferAsync(OperationContext operationContext, int responseTo)
        {
            using (var receiveLockRequest = new SemaphoreSlimRequest(_receiveLock, operationContext.RemainingTimeout, operationContext.CancellationToken))
            {
                var messageTask = _dropbox.GetMessageAsync(responseTo);
                try
                {
                    await Task.WhenAny(messageTask, receiveLockRequest.Task).ConfigureAwait(false);
                    if (messageTask.IsCompleted)
                    {
                        return _dropbox.RemoveMessage(responseTo); // also propagates exception if any
                    }

                    await receiveLockRequest.Task.ConfigureAwait(false); // propagate exceptions
                    while (true)
                    {
                        try
                        {
                            var buffer = await ReceiveBufferAsync(operationContext).ConfigureAwait(false);
                            _dropbox.AddMessage(buffer);
                        }
                        catch (Exception ex)
                        {
                            _dropbox.AddException(ex);
                        }

                        if (messageTask.IsCompleted)
                        {
                            return _dropbox.RemoveMessage(responseTo); // also propagates exception if any
                        }

                        operationContext.ThrowIfTimedOutOrCanceled();
                    }
                }
                catch
                {
                    var ignored = messageTask.ContinueWith(
                        t => { _dropbox.RemoveMessage(responseTo).Dispose(); },
                        TaskContinuationOptions.OnlyOnRanToCompletion);
                    throw;
                }
            }
        }

        public ResponseMessage ReceiveMessage(
            OperationContext operationContext,
            int responseTo,
            IMessageEncoderSelector encoderSelector,
            MessageEncoderSettings messageEncoderSettings)
        {
            Ensure.IsNotNull(encoderSelector, nameof(encoderSelector));
            ThrowIfCancelledOrDisposedOrNotOpen(operationContext);

            var helper = new ReceiveMessageHelper(this, responseTo, messageEncoderSettings, _compressorSource);
            try
            {
                helper.ReceivingMessage();
                using (var buffer = ReceiveBuffer(operationContext, responseTo))
                {
                    var message = helper.DecodeMessage(operationContext, buffer, encoderSelector);
                    helper.ReceivedMessage(buffer, message);
                    return message;
                }
            }
            catch (Exception ex)
            {
                helper.FailedReceivingMessage(ex);
                ThrowOperationCanceledExceptionIfRequired(ex);
                throw;
            }
        }

        public async Task<ResponseMessage> ReceiveMessageAsync(OperationContext operationContext, int responseTo,
            IMessageEncoderSelector encoderSelector,
            MessageEncoderSettings messageEncoderSettings)
        {
            Ensure.IsNotNull(encoderSelector, nameof(encoderSelector));
            ThrowIfCancelledOrDisposedOrNotOpen(operationContext);

            var helper = new ReceiveMessageHelper(this, responseTo, messageEncoderSettings, _compressorSource);
            try
            {
                helper.ReceivingMessage();
                using (var buffer = await ReceiveBufferAsync(operationContext, responseTo).ConfigureAwait(false))
                {
                    var message = helper.DecodeMessage(operationContext, buffer, encoderSelector);
                    helper.ReceivedMessage(buffer, message);
                    return message;
                }
            }
            catch (Exception ex)
            {
                helper.FailedReceivingMessage(ex);
                ThrowOperationCanceledExceptionIfRequired(ex);
                throw;
            }
        }

        private void SendBuffer(OperationContext operationContext, IByteBuffer buffer)
        {
            _sendLock.Wait(operationContext.RemainingTimeout, operationContext.CancellationToken);
            try
            {
                if (_state.Value == State.Failed)
                {
                    throw new MongoConnectionClosedException(_connectionId);
                }

                try
                {
                    _stream.WriteBytes(operationContext, buffer, 0, buffer.Length);
                    _lastUsedAtUtc = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    var wrappedException = WrapExceptionIfRequired(ex, "sending a message to the server");
                    ConnectionFailed(wrappedException ?? ex);
                    if (wrappedException == null) { throw; } else { throw wrappedException; }
                }
            }
            finally
            {
                _sendLock.Release();
            }
        }

        private async Task SendBufferAsync(OperationContext operationContext, IByteBuffer buffer)
        {
            await _sendLock.WaitAsync(operationContext.RemainingTimeout, operationContext.CancellationToken).ConfigureAwait(false);
            try
            {
                if (_state.Value == State.Failed)
                {
                    throw new MongoConnectionClosedException(_connectionId);
                }

                try
                {
                    await _stream.WriteBytesAsync(operationContext, buffer, 0, buffer.Length).ConfigureAwait(false);
                    _lastUsedAtUtc = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    var wrappedException = WrapExceptionIfRequired(ex, "sending a message to the server");
                    ConnectionFailed(wrappedException ?? ex);
                    if (wrappedException == null) { throw; } else { throw wrappedException; }
                }
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public void SendMessage(OperationContext operationContext, RequestMessage message, MessageEncoderSettings messageEncoderSettings)
        {
            Ensure.IsNotNull(message, nameof(message));
            ThrowIfCancelledOrDisposedOrNotOpen(operationContext);

            var helper = new SendMessageHelper(this, message, messageEncoderSettings);
            try
            {
                helper.EncodingMessage();
                using (var uncompressedBuffer = helper.EncodeMessage(operationContext, out var sentMessage))
                {
                    helper.SendingMessage(uncompressedBuffer);
                    int sentLength;
                    if (ShouldBeCompressed(sentMessage))
                    {
                        using (var compressedBuffer = CompressMessage(sentMessage, uncompressedBuffer, messageEncoderSettings))
                        {
                            SendBuffer(operationContext, compressedBuffer);
                            sentLength = compressedBuffer.Length;
                        }
                    }
                    else
                    {
                        SendBuffer(operationContext, uncompressedBuffer);
                        sentLength = uncompressedBuffer.Length;
                    }
                    helper.SentMessage(sentLength);
                }
            }
            catch (Exception ex)
            {
                helper.FailedSendingMessage(ex);
                ThrowOperationCanceledExceptionIfRequired(ex);
                throw;
            }
        }

        public async Task SendMessageAsync(OperationContext operationContext, RequestMessage message, MessageEncoderSettings messageEncoderSettings)
        {
            Ensure.IsNotNull(message, nameof(message));
            ThrowIfCancelledOrDisposedOrNotOpen(operationContext);

            var helper = new SendMessageHelper(this, message, messageEncoderSettings);
            try
            {
                helper.EncodingMessage();
                using (var uncompressedBuffer = helper.EncodeMessage(operationContext, out var sentMessage))
                {
                    helper.SendingMessage(uncompressedBuffer);
                    int sentLength;
                    if (ShouldBeCompressed(sentMessage))
                    {
                        using (var compressedBuffer = CompressMessage(sentMessage, uncompressedBuffer, messageEncoderSettings))
                        {
                            await SendBufferAsync(operationContext, compressedBuffer).ConfigureAwait(false);
                            sentLength = compressedBuffer.Length;
                        }
                    }
                    else
                    {
                        await SendBufferAsync(operationContext, uncompressedBuffer).ConfigureAwait(false);
                        sentLength = uncompressedBuffer.Length;
                    }
                    helper.SentMessage(sentLength);
                }
            }
            catch (Exception ex)
            {
                helper.FailedSendingMessage(ex);
                ThrowOperationCanceledExceptionIfRequired(ex);
                throw;
            }
        }

        public void SetReadTimeout(TimeSpan timeout)
        {
            ThrowIfDisposed();
            _stream.ReadTimeout = (int)timeout.TotalMilliseconds;
        }

        // private methods
        private bool ShouldBeCompressed(RequestMessage message)
        {
            return _sendCompressorType.HasValue && message.MayBeCompressed;
        }

        private CompressorType? ChooseSendCompressorTypeIfAny(ConnectionDescription connectionDescription)
        {
            var availableCompressors = connectionDescription.AvailableCompressors;
            return availableCompressors.Count > 0 ? (CompressorType?)availableCompressors[0] : null;
        }

        private IByteBuffer CompressMessage(
            RequestMessage message,
            IByteBuffer uncompressedBuffer,
            MessageEncoderSettings messageEncoderSettings)
        {
            var outputBufferChunkSource = new OutputBufferChunkSource(BsonChunkPool.Default);
            var compressedBuffer = new MultiChunkBuffer(outputBufferChunkSource);

            using (var uncompressedStream = new ByteBufferStream(uncompressedBuffer, ownsBuffer: false))
            using (var compressedStream = new ByteBufferStream(compressedBuffer, ownsBuffer: false))
            {
                var uncompressedMessageLength = uncompressedStream.ReadInt32();
                uncompressedStream.Position -= 4;

                using (var uncompressedMessageSlice = uncompressedBuffer.GetSlice((int)uncompressedStream.Position, uncompressedMessageLength))
                using (var uncompressedMessageStream = new ByteBufferStream(uncompressedMessageSlice, ownsBuffer: false))
                {
                    if (message.MayBeCompressed)
                    {
                        CompressMessage(message, uncompressedMessageStream, compressedStream, messageEncoderSettings);
                    }
                    else
                    {
                        uncompressedMessageStream.EfficientCopyTo(compressedStream);
                    }
                }

                compressedBuffer.Length = (int)compressedStream.Length;
            }

            return compressedBuffer;
        }

        private void CompressMessage(
            RequestMessage message,
            ByteBufferStream uncompressedMessageStream,
            ByteBufferStream compressedStream,
            MessageEncoderSettings messageEncoderSettings)
        {
            var compressedMessage = new CompressedMessage(message, uncompressedMessageStream, _sendCompressorType.Value);
            var compressedMessageEncoderFactory = new BinaryMessageEncoderFactory(compressedStream, messageEncoderSettings, _compressorSource);
            var compressedMessageEncoder = compressedMessageEncoderFactory.GetCompressedMessageEncoder(null);
            compressedMessageEncoder.WriteMessage(compressedMessage);
        }

        private void ThrowIfCancelledOrDisposed(OperationContext operationContext)
        {
            operationContext.ThrowIfTimedOutOrCanceled();
            ThrowIfDisposed();
        }

        private void ThrowIfCancelledOrDisposedOrNotOpen(OperationContext operationContext)
        {
            ThrowIfCancelledOrDisposed(operationContext);
            if (_state.Value == State.Failed)
            {
                throw new MongoConnectionClosedException(_connectionId);
            }
            if (_state.Value != State.Open && _state.Value != State.Initializing)
            {
                throw new InvalidOperationException("The connection must be opened before it can be used.");
            }
        }

        private void ThrowIfDisposed()
        {
            if (_state.Value == State.Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        private Exception WrapExceptionIfRequired(Exception ex, string action)
        {
            if (
                ex is ThreadAbortException ||
                ex is StackOverflowException ||
                ex is MongoAuthenticationException ||
                ex is OutOfMemoryException ||
                ex is OperationCanceledException ||
                ex is ObjectDisposedException)
            {
                return null;
            }
            else
            {
                var message = string.Format("An exception occurred while {0}.", action);
                return new MongoConnectionException(_connectionId, message, ex);
            }
        }

        private void ThrowOperationCanceledExceptionIfRequired(Exception exception)
        {
            if (exception is ObjectDisposedException objectDisposedException)
            {
                // We expect two cases here:
                //      objectDisposedException.ObjectName == GetType().Name
                //      objectDisposedException.Message == "The semaphore has been disposed."
                // but since the last one is language-specific, the only option we have is avoiding any additional conditions for ObjectDisposedException
                // TODO: this logic should be reviewed in the scope of https://jira.mongodb.org/browse/CSHARP-3165
                throw new OperationCanceledException($"The {nameof(BinaryConnection)} operation has been cancelled.", exception);
            }
        }

        // nested classes
        private class Dropbox
        {
            private readonly ConcurrentDictionary<int, TaskCompletionSource<IByteBuffer>> _messages = new ConcurrentDictionary<int, TaskCompletionSource<IByteBuffer>>();

            // public methods
            public void AddException(Exception exception)
            {
                foreach (var taskCompletionSource in _messages.Values)
                {
                    taskCompletionSource.TrySetException(exception); // has no effect on already completed tasks
                }
            }

            public void AddMessage(IByteBuffer message)
            {
                var responseTo = GetResponseTo(message);
                var tcs = _messages.GetOrAdd(responseTo, x => new TaskCompletionSource<IByteBuffer>());
                tcs.TrySetResult(message);
            }

            public Task<IByteBuffer> GetMessageAsync(int responseTo)
            {
                var tcs = _messages.GetOrAdd(responseTo, _ => new TaskCompletionSource<IByteBuffer>());
                return tcs.Task;
            }

            public IByteBuffer RemoveMessage(int responseTo)
            {
                TaskCompletionSource<IByteBuffer> tcs;
                _messages.TryRemove(responseTo, out tcs);
                return tcs.Task.GetAwaiter().GetResult(); // RemoveMessage is only called when Task is complete
            }

            // private methods
            private int GetResponseTo(IByteBuffer message)
            {
                var backingBytes = message.AccessBackingBytes(8);
                return BinaryPrimitives.ReadInt32LittleEndian(new ReadOnlySpan<byte>(backingBytes.Array, backingBytes.Offset, 4));
            }
        }

        private class OpenConnectionHelper
        {
            private readonly BinaryConnection _connection;
            private Stopwatch _stopwatch;

            public OpenConnectionHelper(BinaryConnection connection)
            {
                _connection = connection;
            }

            public void FailedOpeningConnection(Exception wrappedException)
            {
                if (!_connection._state.TryChange(State.Connecting, State.Failed) && !_connection._state.TryChange(State.Initializing, State.Failed))
                {
                    var currentState = _connection._state.Value;
                    if (currentState != State.Failed && currentState != State.Disposed)
                    {
                        throw new InvalidOperationException($"Invalid BinaryConnection state transition from {currentState} to Failed.");
                    }
                }

                _connection._eventLogger.LogAndPublish(new ConnectionOpeningFailedEvent(_connection.ConnectionId, _connection._settings, wrappedException, EventContext.OperationId));
            }

            public void InitializingConnection()
            {
                if (!_connection._state.TryChange(State.Connecting, State.Initializing))
                {
                    var currentState = _connection._state.Value;
                    if (currentState == State.Disposed)
                    {
                        throw new ObjectDisposedException(typeof(BinaryConnection).Name);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Invalid BinaryConnection state transition from {currentState} to Initializing.");
                    }
                }
            }

            public void OpenedConnection()
            {
                _stopwatch.Stop();
                _connection._connectionId = _connection._description.ConnectionId;

                if (!_connection._state.TryChange(State.Initializing, State.Open))
                {
                    var currentState = _connection._state.Value;
                    if (currentState == State.Disposed)
                    {
                        throw new ObjectDisposedException(typeof(BinaryConnection).Name);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Invalid BinaryConnection state transition from {currentState} to Open.");
                    }
                }

                _connection._eventLogger.LogAndPublish(new ConnectionOpenedEvent(_connection.ConnectionId, _connection._settings, _stopwatch.Elapsed, EventContext.OperationId));
            }

            public void OpeningConnection()
            {
                _connection._eventLogger.LogAndPublish(new ConnectionOpeningEvent(_connection.ConnectionId, _connection._settings, EventContext.OperationId));

                _stopwatch = Stopwatch.StartNew();
            }
        }

        private class ReceiveMessageHelper
        {
            private readonly ICompressorSource _compressorSource;
            private readonly BinaryConnection _connection;
            private TimeSpan _deserializationDuration;
            private readonly MessageEncoderSettings _messageEncoderSettings;
            private TimeSpan _networkDuration;
            private int _responseTo;
            private Stopwatch _stopwatch;

            public ReceiveMessageHelper(BinaryConnection connection, int responseTo, MessageEncoderSettings messageEncoderSettings, ICompressorSource compressorSource)
            {
                _compressorSource = compressorSource;
                _connection = connection;
                _responseTo = responseTo;
                _messageEncoderSettings = messageEncoderSettings;
            }

            public ResponseMessage DecodeMessage(OperationContext operationContext, IByteBuffer buffer, IMessageEncoderSelector encoderSelector)
            {
                operationContext.ThrowIfTimedOutOrCanceled();

                _stopwatch.Stop();
                _networkDuration = _stopwatch.Elapsed;

                ResponseMessage message;
                _stopwatch.Restart();
                using (var stream = new ByteBufferStream(buffer, ownsBuffer: false))
                {
                    var encoderFactory = new BinaryMessageEncoderFactory(stream, _messageEncoderSettings, _compressorSource);

                    var opcode = PeekOpcode(stream);
                    if (opcode == Opcode.Compressed)
                    {
                        var compresedMessageEncoder = encoderFactory.GetCompressedMessageEncoder(encoderSelector);
                        var compressedMessage = (CompressedMessage)compresedMessageEncoder.ReadMessage();
                        message = (ResponseMessage)compressedMessage.OriginalMessage;
                    }
                    else
                    {
                        var encoder = encoderSelector.GetEncoder(encoderFactory);
                        message = (ResponseMessage)encoder.ReadMessage();
                    }
                }
                _stopwatch.Stop();
                _deserializationDuration = _stopwatch.Elapsed;

                return message;
            }

            public void FailedReceivingMessage(Exception exception)
            {
                if (_connection._commandEventHelper.ShouldCallErrorReceiving)
                {
                    _connection._commandEventHelper.ErrorReceiving(_responseTo, _connection._connectionId, _connection.Description?.ServiceId, exception, _connection.IsInitializing);
                }

                _connection._eventLogger.LogAndPublish(new ConnectionReceivingMessageFailedEvent(_connection.ConnectionId, _responseTo, exception, EventContext.OperationId));
            }

            public void ReceivedMessage(IByteBuffer buffer, ResponseMessage message)
            {
                if (_connection._commandEventHelper.ShouldCallAfterReceiving)
                {
                    _connection._commandEventHelper.AfterReceiving(message, buffer, _connection._connectionId, _connection.Description?.ServiceId, _messageEncoderSettings, _connection.IsInitializing);
                }

                _connection._eventLogger.LogAndPublish(new ConnectionReceivedMessageEvent(_connection.ConnectionId, _responseTo, buffer.Length, _networkDuration, _deserializationDuration, EventContext.OperationId));
            }

            public void ReceivingMessage()
            {
                _connection._eventLogger.LogAndPublish(new ConnectionReceivingMessageEvent(_connection.ConnectionId, _responseTo, EventContext.OperationId));

                _stopwatch = Stopwatch.StartNew();
            }

            private Opcode PeekOpcode(BsonStream stream)
            {
                var savedPosition = stream.Position;
                stream.Position += 12;
                var opcode = (Opcode)stream.ReadInt32();
                stream.Position = savedPosition;
                return opcode;
            }
        }

        private class SendMessageHelper
        {
            private readonly Stopwatch _commandStopwatch;
            private readonly BinaryConnection _connection;
            private readonly MessageEncoderSettings _messageEncoderSettings;
            private readonly RequestMessage _message;
            private TimeSpan _serializationDuration;
            private Stopwatch _networkStopwatch;

            public SendMessageHelper(BinaryConnection connection, RequestMessage message, MessageEncoderSettings messageEncoderSettings)
            {
                _connection = connection;
                _message = message;
                _messageEncoderSettings = messageEncoderSettings;

                _commandStopwatch = Stopwatch.StartNew();
            }

            public IByteBuffer EncodeMessage(OperationContext operationContext, out RequestMessage sentMessage)
            {
                sentMessage = null;
                operationContext.ThrowIfTimedOutOrCanceled();

                var serializationStopwatch = Stopwatch.StartNew();
                var outputBufferChunkSource = new OutputBufferChunkSource(BsonChunkPool.Default);
                var buffer = new MultiChunkBuffer(outputBufferChunkSource);
                using (var stream = new ByteBufferStream(buffer, ownsBuffer: false))
                {
                    var encoderFactory = new BinaryMessageEncoderFactory(stream, _messageEncoderSettings, compressorSource: null);

                    var encoder = _message.GetEncoder(encoderFactory);
                    encoder.WriteMessage(_message);
                    _message.WasSent = true;
                    sentMessage = _message;

                    // Encoding messages includes serializing the
                    // documents, so encoding message could be expensive
                    // and worthy of us honoring cancellation here.
                    operationContext.ThrowIfTimedOutOrCanceled();

                    buffer.Length = (int)stream.Length;
                    buffer.MakeReadOnly();
                }
                serializationStopwatch.Stop();
                _serializationDuration = serializationStopwatch.Elapsed;

                return buffer;
            }

            public void EncodingMessage()
            {
                _connection._eventLogger.LogAndPublish(new ConnectionSendingMessagesEvent(_connection.ConnectionId, _message.RequestId, EventContext.OperationId));
            }

            public void FailedSendingMessage(Exception ex)
            {
                if (_connection._commandEventHelper.ShouldCallErrorSending)
                {
                    _connection._commandEventHelper.ErrorSending(_message, _connection._connectionId, _connection._description?.ServiceId, ex, _connection.IsInitializing);
                }

                _connection._eventLogger.LogAndPublish(new ConnectionSendingMessagesFailedEvent(_connection.ConnectionId, _message.RequestId, ex, EventContext.OperationId));
            }

            public void SendingMessage(IByteBuffer buffer)
            {
                if (_connection._commandEventHelper.ShouldCallBeforeSending)
                {
                    _connection._commandEventHelper.BeforeSending(_message, _connection.ConnectionId, _connection.Description?.ServiceId, buffer, _messageEncoderSettings, _commandStopwatch, _connection.IsInitializing);
                }

                _networkStopwatch = Stopwatch.StartNew();
            }

            public void SentMessage(int bufferLength)
            {
                _networkStopwatch.Stop();
                var networkDuration = _networkStopwatch.Elapsed;

                if (_connection._commandEventHelper.ShouldCallAfterSending)
                {
                    _connection._commandEventHelper.AfterSending(_message, _connection._connectionId, _connection.Description?.ServiceId, _connection.IsInitializing);
                }

                _connection._eventLogger.LogAndPublish(new ConnectionSentMessagesEvent(_connection.ConnectionId, _message.RequestId, bufferLength, networkDuration, _serializationDuration, EventContext.OperationId));
            }
        }

        private static class State
        {
            // note: the numeric values matter because sometimes we compare their magnitudes
            public static int Initial = 0;
            public static int Connecting = 1;
            public static int Initializing = 2;
            public static int Open = 3;
            public static int Failed = 4;
            public static int Disposed = 5;
        }
    }
}
