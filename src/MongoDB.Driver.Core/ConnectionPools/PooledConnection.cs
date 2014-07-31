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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages;

namespace MongoDB.Driver.Core.ConnectionPools
{
    /// <summary>
    /// Represents a pooled connection.
    /// </summary>
    internal class PooledConnection : ConnectionWrapper
    {
        // fields
        private DateTime _createdAt;
        private DateTime _lastUsedAt;
        private object _lock = new object();
        private int _referenceCount;
        private readonly IRootConnection _rootConnection;

        // constructors
        public PooledConnection(IRootConnection rootConnection)
            : base(rootConnection)
        {
            _rootConnection = Ensure.IsNotNull(rootConnection, "rootConnection");
            _createdAt = DateTime.UtcNow;
            _lastUsedAt = _createdAt;
        }

        // properties
        public DateTime CreatedAt
        {
            get
            {
                ThrowIfDisposed();
                return _createdAt;
            }
        }

        public DateTime LastUsedAt
        {
            get
            {
                ThrowIfDisposed();
                return _lastUsedAt;
            }
        }

        internal new IRootConnection Wrapped
        {
            get { return _rootConnection; }
        }

        // methods
        public void DecrementReferenceCount()
        {
            Interlocked.Decrement(ref _referenceCount);
        }

        public void IncrementReferenceCount()
        {
            Interlocked.Increment(ref _referenceCount);
        }

        public override Task<ReplyMessage<TDocument>> ReceiveMessageAsync<TDocument>(int responseTo, IBsonSerializer<TDocument> serializer, TimeSpan timeout, CancellationToken cancellationToken)
        {
            _lastUsedAt = DateTime.UtcNow;
            return base.ReceiveMessageAsync<TDocument>(responseTo, serializer, timeout, cancellationToken);
        }

        public override Task SendMessagesAsync(IEnumerable<RequestMessage> messages, TimeSpan timeout, CancellationToken cancellationToken)
        {
            _lastUsedAt = DateTime.UtcNow;
            return base.SendMessagesAsync(messages, timeout, cancellationToken);
        }
    }
}
