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
using System.Net.Sockets;
using MongoDB.Bson;
using System.Runtime.Serialization;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a MongoDB connection exception.
    /// </summary>
    public class MongoConnectionException : MongoException
    {
        // fields
        private readonly ConnectionId _connectionId;
        private int? _generation = null;
        private ObjectId? _serviceId = null;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MongoConnectionException"/> class.
        /// </summary>
        /// <param name="connectionId">The connection identifier.</param>
        /// <param name="message">The error message.</param>
        public MongoConnectionException(ConnectionId connectionId, string message)
            : this(connectionId, message, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoConnectionException"/> class.
        /// </summary>
        /// <param name="connectionId">The connection identifier.</param>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public MongoConnectionException(ConnectionId connectionId, string message, Exception innerException)
            : base(message, innerException)
        {
            _connectionId = Ensure.IsNotNull(connectionId, nameof(connectionId));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoConnectionException"/> class.
        /// </summary>
        /// <param name="info">The SerializationInfo.</param>
        /// <param name="context">The StreamingContext.</param>
        public MongoConnectionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _connectionId = (ConnectionId)info.GetValue("_connectionId", typeof(ConnectionId));
        }

        // properties
        /// <summary>
        /// Gets the connection identifier.
        /// </summary>
        public ConnectionId ConnectionId
        {
            get { return _connectionId; }
        }

        /// <summary>
        /// Whether or not this exception contains a socket timeout exception.
        /// </summary>
        [Obsolete("Use ContainsTimeoutException instead.")]
        public bool ContainsSocketTimeoutException
        {
            get
            {
                for (var exception = InnerException; exception != null; exception = exception.InnerException)
                {
                    if (exception is SocketException socketException &&
                        socketException.SocketErrorCode == SocketError.TimedOut)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Whether or not this exception contains a timeout exception.
        /// </summary>
        public bool ContainsTimeoutException
        {
            get
            {
                for (var exception = InnerException; exception != null; exception = exception.InnerException)
                {
                    if (exception is SocketException socketException &&
                        socketException.SocketErrorCode == SocketError.TimedOut)
                    {
                        return true;
                    }

                    if (exception is TimeoutException)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Determines whether the exception is network error or no.
        /// </summary>
        public virtual bool IsNetworkException => true; // true in subclasses, only if they can be considered as a network error

        // methods
        /// <inheritdoc/>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("_connectionId", _connectionId);
        }

        // properties
        // TODO temporary property for propagating exception generation to server
        // Will be reconsider after SDAM spec error handling adjustments
        internal int? Generation
        {
            get { return _generation; }
            set
            {
                if (_generation != null)
                {
                    throw new InvalidOperationException("Generation is already set.");
                }

                _generation = value;
            }
        }

        /// <summary>
        /// A value for propagating a serviceId to the SDAM logic after handshake completed but before acquiring connection.
        /// </summary>
        internal ObjectId? ServiceId
        {
            get { return _serviceId; }
            set
            {
                if (_serviceId != null)
                {
                    throw new InvalidOperationException("Service Id is already set.");
                }

                _serviceId = value;
            }
        }
    }
}
