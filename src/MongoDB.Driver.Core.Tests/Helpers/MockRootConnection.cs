using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.WireProtocol.Messages;

namespace MongoDB.Driver.Core.Tests.Helpers
{
    public class MockRootConnection : IRootConnection
    {
        private Queue<object> _replyMessages;

        public MockRootConnection()
        {
            _replyMessages = new Queue<object>();
        }

        public ConnectionDescription Description { get; set; }

        public DnsEndPoint EndPoint { get; set; }

        public int PendingResponseCount { get; set; }

        public ConnectionSettings Settings { get; set; }

        public void Dispose()
        {
        }

        public IConnection Fork()
        {
            return this;
        }

        public Task OpenAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            return Task.FromResult<object>(null);
        }

        public void SetConnectionDescription(ConnectionDescription description)
        {
            Description = description;
        }

        public Task<ReplyMessage<TDocument>> ReceiveMessageAsync<TDocument>(int responseTo, Bson.Serialization.IBsonSerializer<TDocument> serializer, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return Task.FromResult<ReplyMessage<TDocument>>((ReplyMessage<TDocument>)_replyMessages.Dequeue());
        }

        public Task SendMessagesAsync(IEnumerable<RequestMessage> messages, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return Task.FromResult<object>(null);
        }

        public void AddReplyMessage<TDocument>(ReplyMessage<TDocument> replyMessage)
        {
            _replyMessages.Enqueue(replyMessage);
        }
    }
}
