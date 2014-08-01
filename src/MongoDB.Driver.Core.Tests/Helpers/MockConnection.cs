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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol.Messages;

namespace MongoDB.Driver.Core.Tests.Helpers
{
    public class MockConnection : IConnection
    {
        // fields
        private readonly Queue<MongoDBMessage> _replyMessages;
        private readonly List<RequestMessage> _sentMessages;

        // constructors
        public MockConnection(ServerId serverId)
        {
            _replyMessages = new Queue<MongoDBMessage>();
            _sentMessages = new List<RequestMessage>();
            Settings = new ConnectionSettings();
            ServerId = serverId;
        }

        // properties
        public ConnectionDescription Description { get; set; }

        public EndPoint EndPoint
        {
            get { return ServerId.EndPoint; }
        }

        public ServerId ServerId { get; private set; }

        public ConnectionSettings Settings { get; set; }

        // methods
        public void Dispose()
        {
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

        public Task OpenAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            return Task.FromResult<object>(null);
        }

        public Task<ReplyMessage<TDocument>> ReceiveMessageAsync<TDocument>(int responseTo, Bson.Serialization.IBsonSerializer<TDocument> serializer, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return Task.FromResult<ReplyMessage<TDocument>>((ReplyMessage<TDocument>)_replyMessages.Dequeue());
        }

        public Task SendMessagesAsync(IEnumerable<RequestMessage> messages, TimeSpan timeout, CancellationToken cancellationToken)
        {
            _sentMessages.AddRange(messages);
            return Task.FromResult<object>(null);
        }
    }
}