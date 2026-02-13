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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.TestHelpers
{
    internal sealed class MockConnection : IConnectionHandle
    {
        // fields
        private ConnectionId _connectionId;
        private readonly ConnectionSettings _connectionSettings;
        private bool? _isExpired;
        private readonly TaskCompletionSource<bool> _isExpiredTaskCompletionSource;
        private DateTime _lastUsedAtUtc;
        private DateTime _openedAtUtc;
        private readonly Queue<ActionQueueItem> _replyActions;
        private readonly List<RequestMessage> _sentMessages;

        private readonly Action<ConnectionOpeningEvent> _openingEventHandler;
        private readonly Action<ConnectionOpenedEvent> _openedEventHandler;
        private readonly Action<ConnectionClosingEvent> _closingEventHandler;
        private readonly Action<ConnectionClosedEvent> _closedEventHandler;

        // constructors
        public MockConnection()
            : this(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017)))
        {
        }

        public MockConnection(ServerId serverId) : this(serverId, new ConnectionSettings(), null)
        {
        }

        public MockConnection(ServerId serverId, ConnectionSettings connectionSettings, IEventSubscriber eventSubscriber, TaskCompletionSource<bool> isExpiredTaskCompletionSource = null)
            : this(new ConnectionId(serverId), connectionSettings, eventSubscriber, isExpiredTaskCompletionSource)
        {
        }

        public MockConnection(ConnectionId connectionId, ConnectionSettings connectionSettings, IEventSubscriber eventSubscriber, TaskCompletionSource<bool> isExpiredTaskCompletionSource = null)
        {
            _replyActions = new Queue<ActionQueueItem>();
            _sentMessages = new List<RequestMessage>();
            _connectionSettings = connectionSettings;
            _connectionId = connectionId;
            _isExpiredTaskCompletionSource = isExpiredTaskCompletionSource;

            if (eventSubscriber != null)
            {
                eventSubscriber.TryGetEventHandler(out _openingEventHandler);
                eventSubscriber.TryGetEventHandler(out _openedEventHandler);
                eventSubscriber.TryGetEventHandler(out _closingEventHandler);
                eventSubscriber.TryGetEventHandler(out _closedEventHandler);
            }
        }

        // properties
        public ConnectionId ConnectionId
        {
            get { return _connectionId; }
        }

        public ConnectionDescription Description { get; set; }

        public EndPoint EndPoint
        {
            get { return _connectionId.ServerId.EndPoint; }
        }

        public int Generation
        {
            get => throw new NotSupportedException();
        }

        public bool IsExpired
        {
            get
            {
                if (_isExpired.HasValue)
                {
                    return _isExpired.Value;
                }
                else if (_isExpiredTaskCompletionSource != null)
                {
                    return _isExpiredTaskCompletionSource.Task.IsCompleted;
                }
                else
                {
                    var now = DateTime.UtcNow;

                    // connection has been alive for too long
                    if (_connectionSettings.MaxLifeTime.TotalMilliseconds > -1 && now > _openedAtUtc.Add(_connectionSettings.MaxLifeTime))
                    {
                        _isExpired = true;
                        return true;
                    }

                    // connection has been idle for too long
                    if (_connectionSettings.MaxIdleTime.TotalMilliseconds > -1 && now > _lastUsedAtUtc.Add(_connectionSettings.MaxIdleTime))
                    {
                        _isExpired = true;
                        return true;
                    }

                    // NOTE: Binary connection also contains the following condition:
                    // return _state.Value > State.Open;
                    // which returns true if the connection is Failed or Disposed.
                    // For that target we use _isExpired field.
                    return false;
                }
            }
            set => _isExpired = value;
        }

        public ConnectionSettings Settings => _connectionSettings;

        // methods
        public void Dispose()
        {
            _closingEventHandler?.Invoke(new ConnectionClosingEvent(_connectionId, EventContext.OperationId));
            IsExpired = true;
            _closedEventHandler?.Invoke(new ConnectionClosedEvent(_connectionId, TimeSpan.FromTicks(1), EventContext.OperationId));
        }

        public void EnqueueCommandResponseMessage(Exception exception)
        {
            _replyActions.Enqueue(new ActionQueueItem(message: null, exception: exception));
        }

        public void EnqueueCommandResponseMessage(CommandResponseMessage replyMessage)
        {
            _replyActions.Enqueue(new ActionQueueItem(replyMessage));
        }

        public void EnqueueCommandResponseMessage(CommandResponseMessage replyMessage, TimeSpan? delay)
        {
            _replyActions.Enqueue(new ActionQueueItem(replyMessage, delay: delay));
        }

        public void EnqueueReplyMessage(Exception exception)
        {
            _replyActions.Enqueue(new ActionQueueItem(message: null, exception: exception));
        }

        public void EnqueueReplyMessage<TDocument>(ReplyMessage<TDocument> replyMessage)
        {
            _replyActions.Enqueue(new ActionQueueItem(replyMessage));
        }

        public IConnectionHandle Fork()
        {
            return this;
        }

        public List<RequestMessage> GetSentMessages()
        {
            return _sentMessages;
        }

        public void Open(OperationContext operationContext)
        {
            _openingEventHandler?.Invoke(new ConnectionOpeningEvent(_connectionId, _connectionSettings, null));

            _openedAtUtc = DateTime.UtcNow;
            // _lastUsedAtUtc is set in the SendBuffer method in the BinaryConnection
            // which is one from methods called inside Open
            _lastUsedAtUtc = DateTime.UtcNow;

            _openedEventHandler?.Invoke(new ConnectionOpenedEvent(_connectionId, _connectionSettings, TimeSpan.FromTicks(1), null));
        }

        public Task OpenAsync(OperationContext operationContext)
        {
            _openingEventHandler?.Invoke(new ConnectionOpeningEvent(_connectionId, _connectionSettings, null));

            _openedAtUtc = DateTime.UtcNow;
            // _lastUsedAtUtc is set in the SendBufferAsync method in the BinaryConnection
            // which is one from methods called inside OpenAsync
            _lastUsedAtUtc = DateTime.UtcNow;

            _openedEventHandler?.Invoke(new ConnectionOpenedEvent(_connectionId, _connectionSettings, TimeSpan.FromTicks(1), null));

            return Task.CompletedTask;
        }

        public void Reauthenticate(OperationContext operationContext)
            => _replyActions.Dequeue().GetEffectiveMessage();

        public Task ReauthenticateAsync(OperationContext operationContext)
            => _replyActions.Dequeue().GetEffectiveMessageAsync();

        public ResponseMessage ReceiveMessage(OperationContext operationContext, int responseTo, IMessageEncoderSelector encoderSelector, MessageEncoderSettings messageEncoderSettings)
        {
            var action = _replyActions.Dequeue();
            return (ResponseMessage)action.GetEffectiveMessage();
        }

        public async Task<ResponseMessage> ReceiveMessageAsync(OperationContext operationContext, int responseTo, IMessageEncoderSelector encoderSelector, MessageEncoderSettings messageEncoderSettings)
        {
            var action = _replyActions.Dequeue();
            return (ResponseMessage)await action.GetEffectiveMessageAsync().ConfigureAwait(false);
        }

        public void SendMessage(OperationContext operationContext, RequestMessage message, MessageEncoderSettings messageEncoderSettings)
        {
            _sentMessages.Add(message);
        }

        public Task SendMessageAsync(OperationContext operationContext, RequestMessage message, MessageEncoderSettings messageEncoderSettings)
        {
            _sentMessages.Add(message);
            return Task.CompletedTask;
        }

        public void CompleteCommandWithException(Exception exception)
        {
            // No-op for mock
        }

        // nested type
        private class ActionQueueItem
        {
            public ActionQueueItem(MongoDBMessage message, Exception exception = null, TimeSpan? delay = null)
            {
                Ensure.That(
                    (message != null || exception != null) &&
                    (message == null || exception == null),
                    "At least one a message or an exception is required and they are mutually exclusive.");

                Message = message;
                Exception = exception;
                Delay = delay;
            }

            public MongoDBMessage Message { get; }
            public Exception Exception { get; }
            public TimeSpan? Delay { get; }

            public MongoDBMessage GetEffectiveMessage()
            {
                ThrowIfException();
                DelayIfRequired();
                return Message;
            }

            public async Task<MongoDBMessage> GetEffectiveMessageAsync()
            {
                ThrowIfException();
                await DelayIfRequiredAsync().ConfigureAwait(false);
                return Message;
            }

            private void ThrowIfException()
            {
                if (Exception != null)
                {
                    throw Exception;
                }
            }

            private void DelayIfRequired()
            {
                if (Delay.HasValue)
                {
                    Thread.Sleep(Delay.Value);
                }
            }

            private async Task DelayIfRequiredAsync()
            {
                if (Delay.HasValue)
                {
                    await Task.Delay(Delay.Value).ConfigureAwait(false);
                }
            }
        }
    }
}
