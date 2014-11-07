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
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Represents a connection wrapper.
    /// </summary>
    public abstract class ConnectionWrapper : IConnection
    {
        // fields
        private bool _disposed;
        private readonly IConnection _wrapped;

        // constructors
        public ConnectionWrapper(IConnection wrapped)
        {
            _wrapped = Ensure.IsNotNull(wrapped, "wrapped");
        }

        // properties
        public ConnectionId ConnectionId
        {
            get { return _wrapped.ConnectionId; }
        }

        public bool Disposed
        {
            get { return _disposed; }
            protected set { _disposed = value;  }
        }

        public virtual ConnectionDescription Description
        {
            get { return _wrapped.Description; }
        }

        public virtual EndPoint EndPoint
        {
            get { return _wrapped.EndPoint; }
        }

        public virtual bool IsExpired
        {
            get { return _wrapped.IsExpired; }
        }

        public virtual ConnectionSettings Settings
        {
            get { return _wrapped.Settings; }
        }

        public IConnection Wrapped
        {
            get { return _wrapped; }
        }

        // methods
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _wrapped.Dispose();
            }
            _disposed = true;
        }

        public virtual Task OpenAsync()
        {
            ThrowIfDisposed();
            return _wrapped.OpenAsync();
        }

        public virtual Task<ReplyMessage<TDocument>> ReceiveMessageAsync<TDocument>(int responseTo, IBsonSerializer<TDocument> serializer, MessageEncoderSettings messageEncoderSettings, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return _wrapped.ReceiveMessageAsync(responseTo, serializer, messageEncoderSettings, cancellationToken);
        }

        public virtual Task SendMessagesAsync(IEnumerable<RequestMessage> messages, MessageEncoderSettings messageEncoderSettings, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return _wrapped.SendMessagesAsync(messages, messageEncoderSettings, cancellationToken);
        }

        protected void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}
