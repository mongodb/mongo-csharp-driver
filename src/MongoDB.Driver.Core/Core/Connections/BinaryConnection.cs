/* Copyright 2013-2014 MongoDB Inc.
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
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Represents a connection using the binary wire protocol over a binary stream.
    /// </summary>
    internal class BinaryConnection : IConnection
    {
        // fields
        private readonly CancellationToken _backgroundTaskCancellationToken;
        private readonly CancellationTokenSource _backgroundTaskCancellationTokenSource;
        private ConnectionId _connectionId;
        private readonly IConnectionInitializer _connectionInitializer;
        private EndPoint _endPoint;
        private ConnectionDescription _description;
        private DateTime _lastUsedAtUtc;
        private readonly IConnectionListener _listener;
        private DateTime _openedAtUtc;
        private readonly object _openLock = new object();
        private Task _openTask;
        private readonly ReceiveCoordinator _receiveCoordinator;
        private readonly SemaphoreSlim _sendLock;
        private readonly ConnectionSettings _settings;
        private readonly InterlockedInt32 _state;
        private Stream _stream;
        private readonly IStreamFactory _streamFactory;

        // constructors
        public BinaryConnection(ServerId serverId, EndPoint endPoint, ConnectionSettings settings, IStreamFactory streamFactory, IConnectionInitializer connectionInitializer, IConnectionListener listener)
        {
            Ensure.IsNotNull(serverId, "serverId");
            _endPoint = Ensure.IsNotNull(endPoint, "endPoint");
            _settings = Ensure.IsNotNull(settings, "settings");
            _streamFactory = Ensure.IsNotNull(streamFactory, "streamFactory");
            _connectionInitializer = Ensure.IsNotNull(connectionInitializer, "connectionInitializer");
            _listener = listener;

            _backgroundTaskCancellationTokenSource = new CancellationTokenSource();
            _backgroundTaskCancellationToken = _backgroundTaskCancellationTokenSource.Token;

            _connectionId = new ConnectionId(serverId);
            _receiveCoordinator = new ReceiveCoordinator();
            _sendLock = new SemaphoreSlim(1);
            _state = new InterlockedInt32(State.Initial);
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

        // methods
        private void ConnectionFailed(Exception exception)
        {
            if (_state.TryChange(State.Open, State.Failed))
            {
                if (_listener != null)
                {
                    _listener.ConnectionFailed(new ConnectionFailedEvent(_connectionId, exception));
                }
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
                    if (_listener != null)
                    {
                        _listener.ConnectionBeforeClosing(new ConnectionBeforeClosingEvent(_connectionId));
                    }

                    _backgroundTaskCancellationTokenSource.Cancel();
                    _backgroundTaskCancellationTokenSource.Dispose();
                    _sendLock.Dispose();

                    if (_stream != null)
                    {
                        try
                        {
                            _stream.Close();
                            _stream.Dispose();
                        }
                        catch
                        {
                            // eat this...
                        }
                    }

                    if (_listener != null)
                    {
                        _listener.ConnectionAfterClosing(new ConnectionAfterClosingEvent(_connectionId));
                    }
                }
            }
        }

        public Task OpenAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            lock (_openLock)
            {
                if (_state.TryChange(State.Initial, State.Connecting))
                {
                    _openedAtUtc = DateTime.UtcNow;
                    _openTask = OpenAsyncHelper(cancellationToken);
                }
                return _openTask;
            }
        }

        private async Task OpenAsyncHelper(CancellationToken cancellationToken)
        {
            if (_listener != null)
            {
                _listener.ConnectionBeforeOpening(new ConnectionBeforeOpeningEvent(_connectionId, _settings));
            }

            try
            {
                var stopwatch = Stopwatch.StartNew();
                _stream = await _streamFactory.CreateStreamAsync(_endPoint, cancellationToken).ConfigureAwait(false);
                _state.TryChange(State.Initializing);
                _description = await _connectionInitializer.InitializeConnectionAsync(this, cancellationToken).ConfigureAwait(false);
                stopwatch.Stop();
                _connectionId = _description.ConnectionId;
                _state.TryChange(State.Open);

                if (_listener != null)
                {
                    _listener.ConnectionAfterOpening(new ConnectionAfterOpeningEvent(_connectionId, _settings, stopwatch.Elapsed));
                }
            }
            catch (Exception ex)
            {
                _state.TryChange(State.Failed);

                var wrappedException =  WrapException(ex, "opening a connection to the server");

                if (_listener != null)
                {
                    _listener.ConnectionErrorOpening(new ConnectionErrorOpeningEvent(_connectionId, _settings, wrappedException));
                    _listener.ConnectionFailed(new ConnectionFailedEvent(_connectionId, wrappedException));
                }

                throw wrappedException;
            }
        }

        private async Task<IByteBuffer> ReceiveBufferAsync()
        {
            try
            {
                var messageSizeBytes = new byte[4];
                await _stream.ReadBytesAsync(messageSizeBytes, 0, 4, _backgroundTaskCancellationToken).ConfigureAwait(false);
                var messageSize = BitConverter.ToInt32(messageSizeBytes, 0);
                var inputBufferChunkSource = new InputBufferChunkSource(BsonChunkPool.Default);
                var buffer = ByteBufferFactory.Create(inputBufferChunkSource, messageSize);
                buffer.Length = messageSize;
                buffer.SetBytes(0, messageSizeBytes, 0, 4);
                await _stream.ReadBytesAsync(buffer, 4, messageSize - 4, _backgroundTaskCancellationToken).ConfigureAwait(false);
                _lastUsedAtUtc = DateTime.UtcNow;
                buffer.MakeReadOnly();
                return buffer;
            }
            catch (Exception ex)
            {
                var wrappedException = WrapException(ex, "receiving a message from the server");
                ConnectionFailed(wrappedException);
                throw wrappedException;
            }
        }

        private async Task<IByteBuffer> ReceiveBufferAsync(int responseTo, CancellationToken cancellationToken)
        {
            var instructions = await _receiveCoordinator.GetInstructionsAsync(responseTo, cancellationToken).ConfigureAwait(false);
            switch (instructions.Action)
            {
                case ReceiveCoordinatorAction.ReturnBuffer:
                    return instructions.Buffer;

                case ReceiveCoordinatorAction.AssumeReceiverRole:
                    try
                    {
                        while (true)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var buffer = await ReceiveBufferAsync().ConfigureAwait(false);
                            var segment = buffer.AccessBackingBytes(8);
                            var receivedResponseTo = BitConverter.ToInt32(segment.Array, segment.Offset);

                            if (receivedResponseTo == responseTo)
                            {
                                return buffer;
                            }
                            else
                            {
                                _receiveCoordinator.DispatchBuffer(receivedResponseTo, buffer);
                            }
                        }
                    }
                    finally
                    {
                        _receiveCoordinator.RelinquishReceiverRole();
                    }

                default:
                    throw new MongoInternalException("Invalid ReceiveCoordinatorAction.");
            }
        }

        public async Task<ResponseMessage> ReceiveMessageAsync(
            int responseTo,
            IMessageEncoderSelector encoderSelector,
            MessageEncoderSettings messageEncoderSettings,
            CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(encoderSelector, "encoderSelector");
            ThrowIfDisposedOrNotOpen();

            try
            {
                if (_listener != null)
                {
                    _listener.ConnectionBeforeReceivingMessage(new ConnectionBeforeReceivingMessageEvent(_connectionId, responseTo));
                }

                var stopwatch = Stopwatch.StartNew();
                ResponseMessage reply;
                int length;
                using (var buffer = await ReceiveBufferAsync(responseTo, cancellationToken).ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    length = buffer.Length;
                    using (var stream = new ByteBufferStream(buffer))
                    {
                        var encoderFactory = new BinaryMessageEncoderFactory(stream, messageEncoderSettings);
                        var encoder = encoderSelector.GetEncoder(encoderFactory);
                        reply = (ResponseMessage)encoder.ReadMessage();
                    }
                }
                stopwatch.Stop();

                if (_listener != null)
                {
                    _listener.ConnectionAfterReceivingMessage(new ConnectionAfterReceivingMessageEvent(_connectionId, reply, length, stopwatch.Elapsed));
                }

                return reply;
            }
            catch (Exception ex)
            {
                if (_listener != null)
                {
                    _listener.ConnectionErrorReceivingMessage(new ConnectionErrorReceivingMessageEvent(_connectionId, responseTo, ex));
                }

                throw;
            }
        }

        private async Task SendBufferAsync(IByteBuffer buffer, CancellationToken cancellationToken)
        {
            await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_state.Value == State.Failed)
                {
                    throw new MongoConnectionClosedException(_connectionId);
                }

                try
                {
                    // don't use the caller's cancellationToken because once we start writing a message we have to write the whole thing
                    await _stream.WriteBytesAsync(buffer, 0, buffer.Length, _backgroundTaskCancellationToken).ConfigureAwait(false);
                    _lastUsedAtUtc = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    var wrappedException = WrapException(ex, "sending a message to the server");
                    ConnectionFailed(wrappedException);
                    throw wrappedException;
                }
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public async Task SendMessagesAsync(IEnumerable<RequestMessage> messages, MessageEncoderSettings messageEncoderSettings, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(messages, "messages");
            ThrowIfDisposedOrNotOpen();

            var messagesToSend = messages.ToList();

            try
            {
                if (_listener != null)
                {
                    _listener.ConnectionBeforeSendingMessages(new ConnectionBeforeSendingMessagesEvent(_connectionId, messagesToSend));
                }

                cancellationToken.ThrowIfCancellationRequested();
                var outputBufferChunkSource = new OutputBufferChunkSource(BsonChunkPool.Default);
                using (var buffer = new MultiChunkBuffer(outputBufferChunkSource))
                {
                    using (var stream = new ByteBufferStream(buffer, ownsBuffer: false))
                    {
                        var encoderFactory = new BinaryMessageEncoderFactory(stream, messageEncoderSettings);
                        foreach (var message in messagesToSend)
                        {
                            if (message.ShouldBeSent == null || message.ShouldBeSent())
                            {
                                var encoder = message.GetEncoder(encoderFactory);
                                encoder.WriteMessage(message);
                                message.WasSent = true;
                            }

                            // Encoding messages includes serializing the
                            // documents, so encoding message could be expensive
                            // and worthy of us honoring cancellation here.
                            cancellationToken.ThrowIfCancellationRequested();
                        }
                        buffer.Length = (int)stream.Length;
                    }

                    var stopwatch = Stopwatch.StartNew();
                    await SendBufferAsync(buffer, cancellationToken).ConfigureAwait(false);
                    stopwatch.Stop();

                    if (_listener != null)
                    {
                        _listener.ConnectionAfterSendingMessages(new ConnectionAfterSendingMessagesEvent(_connectionId, messagesToSend, buffer.Length, stopwatch.Elapsed));
                    }
                }
            }
            catch (Exception ex)
            {
                if (_listener != null)
                {
                    _listener.ConnectionErrorSendingMessages(new ConnectionErrorSendingMessagesEvent(_connectionId, messagesToSend, ex));
                }

                throw;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_state.Value == State.Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        private void ThrowIfDisposedOrNotOpen()
        {
            ThrowIfDisposed();
            if (_state.Value == State.Failed)
            {
                throw new MongoConnectionClosedException(_connectionId);
            }
            if (_state.Value != State.Open && _state.Value != State.Initializing)
            {
                throw new InvalidOperationException("The connection must be opened before it can be used.");
            }
        }

        private Exception WrapException(Exception ex, string action)
        {
            if (ex is ThreadAbortException || ex is StackOverflowException || ex is OutOfMemoryException)
            {
                return ex;
            }
            else
            {
                var message = string.Format("An exception occurred while {0}.", action);
                return new MongoConnectionException(_connectionId, message, ex);
            }
        }

        // nested classes
        private static class State
        {
            public static int Initial = 0;
            public static int Connecting = 1;
            public static int Initializing = 2;
            public static int Open = 3;
            public static int Failed = 4;
            public static int Disposed = 5;
        }

        private enum ReceiveCoordinatorAction
        {
            ReturnBuffer,
            AssumeReceiverRole
        }

        private struct ReceiveCoordinatorInstructions
        {
            public ReceiveCoordinatorAction Action;
            public IByteBuffer Buffer;
        }

        private class ReceiveCoordinator
        {
            private readonly Dictionary<int, TaskCompletionSource<ReceiveCoordinatorInstructions>> _awaiters = new Dictionary<int, TaskCompletionSource<ReceiveCoordinatorInstructions>>();
            private readonly Dictionary<int, IByteBuffer> _buffers = new Dictionary<int, IByteBuffer>();
            private readonly object _lock = new object();
            private bool _receiverRoleAssigned;

            public void DispatchBuffer(int responseTo, IByteBuffer buffer)
            {
                TaskCompletionSource<ReceiveCoordinatorInstructions> awaiter = null;

                lock (_lock)
                {
                    if (_awaiters.TryGetValue(responseTo, out awaiter))
                    {
                        _awaiters.Remove(responseTo);
                    }
                    else
                    {
                        _buffers.Add(responseTo, buffer);
                    }
                }

                if (awaiter != null)
                {
                    var instructions = new ReceiveCoordinatorInstructions
                    {
                        Action = ReceiveCoordinatorAction.ReturnBuffer,
                        Buffer = buffer
                    };
                    if (!awaiter.TrySetResult(instructions))
                    {
                        buffer.Dispose();
                    }
                }
            }

            public async Task<ReceiveCoordinatorInstructions> GetInstructionsAsync(int responseTo, CancellationToken cancellationToken)
            {
                TaskCompletionSource<ReceiveCoordinatorInstructions> awaiter;

                lock (_lock)
                {
                    ReceiveCoordinatorInstructions instructions;

                    IByteBuffer buffer;
                    if (_buffers.TryGetValue(responseTo, out buffer))
                    {
                        _buffers.Remove(responseTo);
                        instructions = new ReceiveCoordinatorInstructions
                        {
                            Action = ReceiveCoordinatorAction.ReturnBuffer,
                            Buffer = buffer
                        };
                        return instructions;
                    }
                    else if (_receiverRoleAssigned)
                    {
                        awaiter = new TaskCompletionSource<ReceiveCoordinatorInstructions>();
                        _awaiters.Add(responseTo, awaiter);
                    }
                    else
                    {
                        _receiverRoleAssigned = true;
                        instructions = new ReceiveCoordinatorInstructions { Action = ReceiveCoordinatorAction.AssumeReceiverRole };
                        return instructions;
                    }
                }

                using (cancellationToken.Register(() => awaiter.TrySetCanceled(), useSynchronizationContext: false))
                {
                    return await awaiter.Task.ConfigureAwait(false);
                }
            }

            public void RelinquishReceiverRole()
            {
                TaskCompletionSource<ReceiveCoordinatorInstructions> awaiter = null;

                lock (_lock)
                {
                    if (_awaiters.Count > 0)
                    {
                        var pair = _awaiters.First();
                        _awaiters.Remove(pair.Key);
                        awaiter = pair.Value;
                    }
                    else
                    {
                        _receiverRoleAssigned = false;
                    }
                }

                if (awaiter != null)
                {
                    var instructions = new ReceiveCoordinatorInstructions { Action = ReceiveCoordinatorAction.AssumeReceiverRole };
                    awaiter.TrySetResult(instructions);
                }
            }
        }
    }
}
