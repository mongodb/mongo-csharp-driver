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
using MongoDB.Driver.Core.Async;
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
        private readonly AsyncDropbox<int, IByteBuffer> _inboundDropbox;
        private ConnectionDescription _description;
        private DateTime _lastUsedAtUtc;
        private readonly IConnectionListener _listener;
        private DateTime _openedAtUtc;
        private readonly object _openLock = new object();
        private Task _openTask;
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
            _inboundDropbox = new AsyncDropbox<int, IByteBuffer>();
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
                foreach (var awaiter in _inboundDropbox.RemoveAllAwaiters())
                {
                    awaiter.TrySetException(exception);
                }

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
                StartBackgroundTasks();
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

                if (_listener != null)
                {
                    _listener.ConnectionErrorOpening(new ConnectionErrorOpeningEvent(_connectionId, _settings, ex));
                    _listener.ConnectionFailed(new ConnectionFailedEvent(_connectionId, ex));
                }

                throw;
            }
        }

        private async Task ReceiveBackgroundTask()
        {
            while (!_backgroundTaskCancellationToken.IsCancellationRequested)
            {
                try
                {
                    var messageSizeBytes = new byte[4];
                    await _stream.FillBufferAsync(messageSizeBytes, 0, 4, _backgroundTaskCancellationToken).ConfigureAwait(false);
                    var messageSize = BitConverter.ToInt32(messageSizeBytes, 0);
                    var buffer = ByteBufferFactory.Create(BsonChunkPool.Default, messageSize);
                    buffer.WriteBytes(0, messageSizeBytes, 0, 4);
                    await _stream.FillBufferAsync(buffer, 4, messageSize - 4, _backgroundTaskCancellationToken).ConfigureAwait(false);
                    _lastUsedAtUtc = DateTime.UtcNow;
                    var responseToBytes = new byte[4];
                    buffer.ReadBytes(8, responseToBytes, 0, 4);
                    var responseTo = BitConverter.ToInt32(responseToBytes, 0);
                    _inboundDropbox.Post(responseTo, buffer);
                }
                catch (Exception ex)
                {
                    ConnectionFailed(ex);
                    return;
                }
            }
        }

        public async Task<ReplyMessage<TDocument>> ReceiveMessageAsync<TDocument>(
            int responseTo,
            IBsonSerializer<TDocument> serializer,
            MessageEncoderSettings messageEncoderSettings,
            CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(serializer, "serializer");
            ThrowIfDisposedOrNotOpen();

            try
            {
                if (_listener != null)
                {
                    _listener.ConnectionBeforeReceivingMessage(new ConnectionBeforeReceivingMessageEvent(_connectionId, responseTo));
                }

                var stopwatch = Stopwatch.StartNew();
                var buffer = await _inboundDropbox.ReceiveAsync(responseTo, cancellationToken).ConfigureAwait(false);
                int length = buffer.Length;
                ReplyMessage<TDocument> reply;
                using (var stream = new ByteBufferStream(buffer, ownsByteBuffer: true))
                {
                    var encoderFactory = new BinaryMessageEncoderFactory(stream, messageEncoderSettings);
                    var encoder = encoderFactory.GetReplyMessageEncoder<TDocument>(serializer);
                    reply = (ReplyMessage<TDocument>)encoder.ReadMessage();
                }
                stopwatch.Stop();

                if (_listener != null)
                {
                    _listener.ConnectionAfterReceivingMessage<TDocument>(new ConnectionAfterReceivingMessageEvent<TDocument>(_connectionId, reply, length, stopwatch.Elapsed));
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
                    throw new MongoMessageNotSentException(_connectionId);
                }

                try
                {
                    // don't use the caller's cancellationToken because once we start writing a message we have to write the whole thing
                    await _stream.WriteBufferAsync(buffer, 0, buffer.Length, _backgroundTaskCancellationToken).ConfigureAwait(false);
                    _lastUsedAtUtc = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    ConnectionFailed(ex);
                    throw;
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
                using (var buffer = new MultiChunkBuffer(BsonChunkPool.Default))
                {
                    using (var stream = new ByteBufferStream(buffer, ownsByteBuffer: false))
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
                    await SendBufferAsync(buffer, cancellationToken);
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

        private void StartBackgroundTasks()
        {
            ReceiveBackgroundTask().ConfigureAwait(false);
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
                throw new MongoConnectionException(_connectionId, "Connection failed.");
            }
            if (_state.Value != State.Open && _state.Value != State.Initializing)
            {
                throw new InvalidOperationException("The connection must be opened before it can be used.");
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
    }
}
