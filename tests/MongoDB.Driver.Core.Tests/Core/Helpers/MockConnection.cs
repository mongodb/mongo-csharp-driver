/* Copyright 2013-2015 MongoDB Inc.
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
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Helpers
{
    public class MockConnection : IConnection
    {
        // fields
        private ConnectionId _connectionId;
        private readonly Queue<MongoDBMessage> _replyMessages;
        private readonly List<RequestMessage> _sentMessages;

        // constructors
        public MockConnection()
            : this(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017)))
        {
        }

        public MockConnection(ServerId serverId)
        {
            _replyMessages = new Queue<MongoDBMessage>();
            _sentMessages = new List<RequestMessage>();
            Settings = new ConnectionSettings();
            _connectionId = new ConnectionId(serverId);
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

        public bool IsExpired { get; set; }

        public ConnectionSettings Settings { get; set; }

        // methods
        public void Dispose()
        {
            IsExpired = true;
        }

        public void EnqueueReplyMessage<TDocument>(ReplyMessage<TDocument> replyMessage)
        {
            _replyMessages.Enqueue(replyMessage);
        }

        public IConnection Fork()
        {
            return this;
        }

        public List<RequestMessage> GetSentMessages()
        {
            return _sentMessages;
        }

        public void Open(CancellationToken cancellationToken)
        {
        }

        public Task OpenAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<object>(null);
        }

        public ResponseMessage ReceiveMessage(int responseTo, IMessageEncoderSelector encoderSelector, MessageEncoderSettings messageEncoderSettings, CancellationToken cancellationToken)
        {
            return (ResponseMessage)_replyMessages.Dequeue();
        }

        public Task<ResponseMessage> ReceiveMessageAsync(int responseTo, IMessageEncoderSelector encoderSelector, MessageEncoderSettings messageEncoderSettings, CancellationToken cancellationToken)
        {
            return Task.FromResult((ResponseMessage)_replyMessages.Dequeue());
        }

        public void SendMessages(IEnumerable<RequestMessage> messages, MessageEncoderSettings messageEncoderSettings, CancellationToken cancellationToken)
        {
            _sentMessages.AddRange(messages);
        }

        public Task SendMessagesAsync(IEnumerable<RequestMessage> messages, MessageEncoderSettings messageEncoderSettings, CancellationToken cancellationToken)
        {
            _sentMessages.AddRange(messages);
            return Task.FromResult<object>(null);
        }
    }
}