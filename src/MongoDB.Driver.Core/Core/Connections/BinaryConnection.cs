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
        private readonly AsyncQueue<OutboundQueueEntry> _outboundQueue;
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

            _connectionId = new ConnectionId(serverId);
            _inboundDropbox = new AsyncDropbox<int, IByteBuffer>();
            _outboundQueue = new AsyncQueue<OutboundQueueEntry>();
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
                foreach (var entry in _outboundQueue.DequeueAll())
                {
                    entry.TaskCompletionSource.TrySetException(new MessageNotSentException(_connectionId));
                }

                foreach (var awaiter in _inboundDropbox.RemoveAllAwaiters())
                {
                    awaiter.TrySetException(exception);
                }

                if (_listener != null)
                {
                    _listener.ConnectionFailed(_connectionId, exception);
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
                        _listener.ConnectionBeforeClosing(_connectionId);
                    }

                    _backgroundTaskCancellationTokenSource.Cancel();
                    _backgroundTaskCancellationTokenSource.Dispose();
                    try
                    {
                        _stream.Close();
                        _stream.Dispose();
                    }
                    catch
                    {
                        // eat this...
                    }

                    if (_listener != null)
                    {
                        _listener.ConnectionAfterClosing(_connectionId);
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
                _listener.ConnectionBeforeOpening(_connectionId, _settings);
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
                    _listener.ConnectionAfterOpening(_connectionId, _settings, stopwatch.Elapsed);
                }
            }
            catch (Exception ex)
            {
                _state.TryChange(State.Failed);

                if (_listener != null)
                {
                    _listener.ConnectionErrorOpening(_connectionId, ex);
                    _listener.ConnectionFailed(_connectionId, ex);
                }

                throw;
            }
        }

        private async Task<bool> ReceiveBackgroundTask(CancellationToken cancellationToken)
        {
            try
            {
                var messageSizeBytes = new byte[4];
                await _stream.FillBufferAsync(messageSizeBytes, 0, 4, cancellationToken).ConfigureAwait(false);
                var messageSize = BitConverter.ToInt32(messageSizeBytes, 0);
                var buffer = ByteBufferFactory.Create(BsonChunkPool.Default, messageSize);
                buffer.WriteBytes(0, messageSizeBytes, 0, 4);
                await _stream.FillBufferAsync(buffer, 4, messageSize - 4, cancellationToken).ConfigureAwait(false);
                _lastUsedAtUtc = DateTime.UtcNow;
                var responseToBytes = new byte[4];
                buffer.ReadBytes(8, responseToBytes, 0, 4);
                var responseTo = BitConverter.ToInt32(responseToBytes, 0);
                _inboundDropbox.Post(responseTo, buffer);
                return true;
            }
            catch (Exception ex)
            {
                ConnectionFailed(ex);
                return false;
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
                    _listener.ConnectionBeforeReceivingMessage(_connectionId, responseTo);
                }

                var stopwatch = Stopwatch.StartNew();
                var buffer = await _inboundDropbox.ReceiveAsync(responseTo, cancellationToken).ConfigureAwait(false);
                int length = buffer.Length;
                ReplyMessage<TDocument> reply;
                using (var stream = new ByteBufferStream(buffer, ownsByteBuffer: true))
                {
                    var encoderFactory = new BinaryMessageEncoderFactory(stream, messageEncoderSettings);
                    var encoder = encoderFactory.GetReplyMessageEncoder<TDocument>(serializer);
                    reply = encoder.ReadMessage();
                }
                stopwatch.Stop();

                if (_listener != null)
                {
                    _listener.ConnectionAfterReceivingMessage<TDocument>(_connectionId, reply, length, stopwatch.Elapsed);
                }

                return reply;
            }
            catch (Exception ex)
            {
                if (_listener != null)
                {
                    _listener.ConnectionErrorReceivingMessage(_connectionId, responseTo, ex);
                }

                throw;
            }
        }

        private async Task<bool> SendBackgroundTask(CancellationToken cancellationToken)
        {
            var entry = await _outboundQueue.DequeueAsync().ConfigureAwait(false);
            // Before we might blow up the stream, let's see if we have been
            // asked to cancel. No reason to do work on the stream if the user
            // no longer cares.
            if (entry.CancellationToken.IsCancellationRequested)
            {
                entry.TaskCompletionSource.TrySetCanceled();
                return true; // means to not exit the background task...
            }
            try
            {
                // Don't use the entry's cancellation token because once we
                // start writing the message, we don't want to stop arbitrarily
                // and render the connection unusable at the whim of a user.
                await _stream.WriteBufferAsync(entry.Buffer, 0, entry.Buffer.Length, cancellationToken).ConfigureAwait(false);
                _lastUsedAtUtc = DateTime.UtcNow;
                entry.TaskCompletionSource.TrySetResult(true);
                return true;
            }
            catch (Exception ex)
            {
                // We have to kill the connection off here because we may have
                // partially written the bytes to the stream, leaving it
                // unusable.
                ConnectionFailed(ex);
                entry.TaskCompletionSource.TrySetException(ex);
                return false; // stop the loop, we are done!!!
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
                    _listener.ConnectionBeforeSendingMessages(_connectionId, messagesToSend);
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
                    var entry = new OutboundQueueEntry(buffer, cancellationToken);
                    _outboundQueue.Enqueue(entry);
                    await entry.Task.ConfigureAwait(false);
                    stopwatch.Stop();

                    if (_listener != null)
                    {
                        _listener.ConnectionAfterSendingMessages(_connectionId, messagesToSend, buffer.Length, stopwatch.Elapsed);
                    }
                }
            }
            catch (Exception ex)
            {
                if (_listener != null)
                {
                    _listener.ConnectionErrorSendingMessages(_connectionId, messagesToSend, ex);
                }

                throw;
            }
        }

        private void StartBackgroundTasks()
        {
            AsyncBackgroundTask.Start(SendBackgroundTask, TimeSpan.FromMilliseconds(0), _backgroundTaskCancellationTokenSource.Token)
                .HandleUnobservedException(ConnectionFailed);
            AsyncBackgroundTask.Start(ReceiveBackgroundTask, TimeSpan.FromMilliseconds(0), _backgroundTaskCancellationTokenSource.Token)
                .HandleUnobservedException(ConnectionFailed);
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

        private class OutboundQueueEntry
        {
            // fields
            private readonly IByteBuffer _buffer;
            private readonly CancellationToken _cancellationToken;
            private readonly TaskCompletionSource<bool> _taskCompletionSource;

            // constructors
            public OutboundQueueEntry(IByteBuffer buffer, CancellationToken cancellationToken)
            {
                _buffer = buffer;
                _cancellationToken = cancellationToken;
                _taskCompletionSource = new TaskCompletionSource<bool>();
            }

            // properties
            public CancellationToken CancellationToken
            {
                get { return _cancellationToken; }
            }

            public IByteBuffer Buffer
            {
                get { return _buffer; }
            }

            public Task Task
            {
                get { return _taskCompletionSource.Task; }
            }

            public TaskCompletionSource<bool> TaskCompletionSource
            {
                get { return _taskCompletionSource; }
            }
        }
    }
}
