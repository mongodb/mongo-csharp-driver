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
using System.IO;
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
        private readonly AsyncDropbox<int, InboundDropboxEntry> _inboundDropbox = new AsyncDropbox<int, InboundDropboxEntry>();
        private ConnectionDescription _description;
        private DateTime _lastUsedAtUtc;
        private readonly IMessageListener _listener;
        private readonly object _openLock = new object();
        private DateTime _openedAtUtc;
        private Task _openTask;
        private readonly AsyncQueue<OutboundQueueEntry> _outboundQueue = new AsyncQueue<OutboundQueueEntry>();
        private readonly ServerId _serverId;
        private readonly ConnectionSettings _settings;
        private readonly InterlockedInt32 _state;
        private Stream _stream;
        private readonly IStreamFactory _streamFactory;

        // constructors
        public BinaryConnection(ServerId serverId, EndPoint endPoint, ConnectionSettings settings, IStreamFactory streamFactory, IConnectionInitializer connectionInitializer, IMessageListener listener)
        {
            _serverId = Ensure.IsNotNull(serverId, "serverId");
            _endPoint = Ensure.IsNotNull(endPoint, "endPoint");
            _settings = Ensure.IsNotNull(settings, "settings");
            _streamFactory = Ensure.IsNotNull(streamFactory, "streamFactory");
            _connectionInitializer = Ensure.IsNotNull(connectionInitializer, "connectionInitializer");
            _listener = listener;

            _backgroundTaskCancellationTokenSource = new CancellationTokenSource();
            _backgroundTaskCancellationToken = _backgroundTaskCancellationTokenSource.Token;

            _connectionId = new ConnectionId(_serverId);
            _state = new InterlockedInt32(State.Initial);
        }

        // properties
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

                //connection has been alive for too long
                if (_settings.MaxLifeTime.TotalMilliseconds > -1 && now > _openedAtUtc.Add(_settings.MaxLifeTime))
                {
                    return true;
                }

                // connection has been idle for too long
                if (_settings.MaxIdleTime.TotalMilliseconds > -1 && now > _lastUsedAtUtc.Add(_settings.MaxIdleTime))
                {
                    return true;
                }

                return _state.Value == State.Disposed;
            }
        }

        public ConnectionSettings Settings
        {
            get { return _settings; }
        }

        // methods
        public void ConnectionFailed(Exception ex)
        {
            // TODO: what?
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing && _state.TryChange(State.Disposed))
            {
                _backgroundTaskCancellationTokenSource.Cancel();
                _backgroundTaskCancellationTokenSource.Dispose();
            }
        }

        private void OnSentMessages(List<RequestMessage> messages, Exception ex)
        {
            if (_listener != null)
            {
                foreach (var message in messages)
                {
                    var args = new SentMessageEventArgs(_endPoint, _connectionId, message, ex);
                    _listener.SentMessage(args);
                }
            }
        }

        public Task OpenAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            Ensure.IsInfiniteOrGreaterThanOrEqualToZero(timeout, "timeout");

            ThrowIfDisposed();

            lock (_openLock)
            {
                if (_state.TryChange(State.Initial, State.Connecting))
                {
                    _openedAtUtc = DateTime.UtcNow;
                    _openTask = OpenAsyncHelper(timeout, cancellationToken);
                }
                return _openTask;
            }
        }

        private async Task OpenAsyncHelper(TimeSpan timeout, CancellationToken cancellationToken)
        {
            var slidingTimeout = new SlidingTimeout(timeout);
            _stream = await _streamFactory.CreateStreamAsync(_endPoint, slidingTimeout, cancellationToken);
            _state.TryChange(State.Initializing);
            StartBackgroundTasks();
            _description = await _connectionInitializer.InitializeConnectionAsync(this, _serverId, slidingTimeout, cancellationToken);
            _connectionId = _description.ConnectionId;
            _state.TryChange(State.Open);
        }

        private async Task ReceiveBackgroundTask()
        {
            try
            {
                while (true)
                {
                    _backgroundTaskCancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var messageSizeBytes = new byte[4];
                        await _stream.FillBufferAsync(messageSizeBytes, 0, 4, _backgroundTaskCancellationToken);
                        var messageSize = BitConverter.ToInt32(messageSizeBytes, 0);
                        var buffer = ByteBufferFactory.Create(BsonChunkPool.Default, messageSize);
                        buffer.WriteBytes(0, messageSizeBytes, 0, 4);
                        await _stream.FillBufferAsync(buffer, 4, messageSize - 4, _backgroundTaskCancellationToken);
                        var responseToBytes = new byte[4];
                        buffer.ReadBytes(8, responseToBytes, 0, 4);
                        var responseTo = BitConverter.ToInt32(responseToBytes, 0);
                        _inboundDropbox.Post(responseTo, new InboundDropboxEntry(buffer));
                    }
                    catch (Exception ex)
                    {
                        ConnectionFailed(ex);
                        throw;
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // ignore TaskCanceledException
            }
        }

        public async Task<ReplyMessage<TDocument>> ReceiveMessageAsync<TDocument>(int responseTo, IBsonSerializer<TDocument> serializer, TimeSpan timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(serializer, "serializer");
            Ensure.IsInfiniteOrGreaterThanOrEqualToZero(timeout, "timeout");
            ThrowIfDisposedOrNotOpen();

            _lastUsedAtUtc = DateTime.UtcNow;
            var slidingTimeout = new SlidingTimeout(timeout);
            var entry = await _inboundDropbox.ReceiveAsync(responseTo, slidingTimeout, cancellationToken);

            ReplyMessage<TDocument> reply;
            if (entry.Buffer != null)
            {
                using (var buffer = entry.Buffer)
                using (var stream = new ByteBufferStream(buffer, ownsByteBuffer: false))
                {
                    var readerSettings = BsonBinaryReaderSettings.Defaults; // TODO: where are reader settings supposed to come from?
                    var binaryReader = new BsonBinaryReader(stream, readerSettings);
                    var encoderFactory = new BinaryMessageEncoderFactory(binaryReader);
                    var encoder = encoderFactory.GetReplyMessageEncoder<TDocument>(serializer);
                    reply = encoder.ReadMessage();
                }
            }
            else
            {
                reply = (ReplyMessage<TDocument>)entry.Reply;
            }

            ReplyMessage<TDocument> substituteReply = null;
            if (_listener != null)
            {
                var args = new ReceivedMessageEventArgs(_endPoint, _connectionId, reply);
                _listener.ReceivedMessage(args);
                substituteReply = (ReplyMessage<TDocument>)args.SubstituteReply;
            }

            return substituteReply ?? reply;
        }

        private async Task SendBackgroundTask()
        {
            try
            {
                while (true)
                {
                    _backgroundTaskCancellationToken.ThrowIfCancellationRequested();

                    var entry = await _outboundQueue.DequeueAsync();
                    try
                    {
                        await _stream.WriteBufferAsync(entry.Buffer, 0, entry.Buffer.Length, _backgroundTaskCancellationToken);
                        entry.TaskCompletionSource.TrySetResult(true);
                    }
                    catch (Exception ex)
                    {
                        entry.TaskCompletionSource.TrySetException(ex);
                        ConnectionFailed(ex);
                        throw;
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // ignore TaskCanceledException
            }
        }

        public async Task SendMessagesAsync(IEnumerable<RequestMessage> messages, TimeSpan timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(messages, "messages");
            Ensure.IsInfiniteOrGreaterThanOrEqualToZero(timeout, "timeout");
            ThrowIfDisposedOrNotOpen();

            _lastUsedAtUtc = DateTime.UtcNow;
            var slidingTimeout = new SlidingTimeout(timeout);
            var sentMessages = new List<RequestMessage>();
            var substituteReplies = new Dictionary<int, ReplyMessage>();

            Exception exception = null;
            using (var buffer = new MultiChunkBuffer(BsonChunkPool.Default))
            {
                using (var stream = new ByteBufferStream(buffer, ownsByteBuffer: false))
                {
                    var writerSettings = BsonBinaryWriterSettings.Defaults;
                    var binaryWriter = new BsonBinaryWriter(stream, writerSettings);
                    var encoderFactory = new BinaryMessageEncoderFactory(binaryWriter);
                    foreach (var message in messages)
                    {
                        RequestMessage substituteMessage = null;
                        ReplyMessage substituteReply = null;
                        if (_listener != null)
                        {
                            var args = new SendingMessageEventArgs(_endPoint, _connectionId, message);
                            _listener.SendingMessage(args);
                            substituteMessage = args.SubstituteMessage;
                            substituteReply = args.SubstituteReply;
                        }

                        var actualMessage = substituteMessage ?? message;
                        sentMessages.Add(actualMessage);

                        if (substituteReply == null)
                        {
                            var encoder = actualMessage.GetEncoder(encoderFactory);
                            encoder.WriteMessage(actualMessage);
                        }
                        else
                        {
                            substituteReplies.Add(message.RequestId, substituteReply);
                        }
                    }
                    buffer.Length = (int)stream.Length;
                }

                if (buffer.Length > 0)
                {
                    try
                    {
                        var entry = new OutboundQueueEntry(buffer, cancellationToken);
                        _outboundQueue.Enqueue(entry);
                        await entry.Task;
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }
                }
            }

            OnSentMessages(sentMessages, exception);
            if (exception != null)
            {
                throw exception;
            }

            foreach (var requestId in substituteReplies.Keys)
            {
                _inboundDropbox.Post(requestId, new InboundDropboxEntry(substituteReplies[requestId]));
            }
        }

        private void StartBackgroundTasks()
        {
            SendBackgroundTask().LogUnobservedExceptions();
            ReceiveBackgroundTask().LogUnobservedExceptions();
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
            public static int Disposed = 4;
        }

        private class InboundDropboxEntry
        {
            // fields
            private readonly IByteBuffer _buffer;
            private readonly ReplyMessage _reply;

            // constructors
            public InboundDropboxEntry(IByteBuffer buffer)
            {
                _buffer = buffer;
            }

            public InboundDropboxEntry(ReplyMessage reply)
            {
                _reply = reply;
            }

            // properties
            public IByteBuffer Buffer
            {
                get { return _buffer; }
            }

            public ReplyMessage Reply
            {
                get { return _reply; }
            }
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
