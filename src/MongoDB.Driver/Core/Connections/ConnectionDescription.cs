/* Copyright 2013-present MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Driver.Core.Compression;
using MongoDB.Driver.Core.Misc;
using MongoDB.Shared;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Represents information describing a connection.
    /// </summary>
    public sealed class ConnectionDescription : IEquatable<ConnectionDescription>
    {
        // fields
        private readonly IReadOnlyList<CompressorType> _compressors;
        private readonly ConnectionId _connectionId;
        private readonly HelloResult _helloResult;
        private readonly int _maxBatchCount;
        private readonly int _maxDocumentSize;
        private readonly int _maxMessageSize;
        private readonly int _maxWireVersion;
        private readonly int _minWireVersion;
        private readonly SemanticVersion _serverVersion;
        private readonly ObjectId? _serviceId;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionDescription"/> class.
        /// </summary>
        /// <param name="connectionId">The connection identifier.</param>
        /// <param name="helloResult">The hello result.</param>
        public ConnectionDescription(ConnectionId connectionId, HelloResult helloResult)
        {
            _connectionId = Ensure.IsNotNull(connectionId, nameof(connectionId));
            _helloResult = Ensure.IsNotNull(helloResult, nameof(helloResult));

            _compressors = Ensure.IsNotNull(_helloResult.Compressions, "compressions");
            _maxBatchCount = helloResult.MaxBatchCount;
            _maxDocumentSize = helloResult.MaxDocumentSize;
            _maxMessageSize = helloResult.MaxMessageSize;
            _maxWireVersion = helloResult.MaxWireVersion;
            _minWireVersion = helloResult.MinWireVersion;
            _serviceId = helloResult.ServiceId;
            _serverVersion = WireVersion.ToServerVersion(_maxWireVersion);
        }

        // properties
        /// <summary>
        /// Gets the available compressors.
        /// </summary>
        public IReadOnlyList<CompressorType> AvailableCompressors
        {
            get { return _compressors; }
        }

        /// <summary>
        /// Gets the connection identifier.
        /// </summary>
        /// <value>
        /// The connection identifier.
        /// </value>
        public ConnectionId ConnectionId
        {
            get { return _connectionId; }
        }

        /// <summary>
        /// Gets the hello result.
        /// </summary>
        /// <value>
        /// The hello result.
        /// </value>
        public HelloResult HelloResult
        {
            get { return _helloResult; }
        }

        /// <summary>
        /// Gets the maximum number of documents in a batch.
        /// </summary>
        /// <value>
        /// The maximum number of documents in a batch.
        /// </value>
        public int MaxBatchCount
        {
            get { return _maxBatchCount; }
        }

        /// <summary>
        /// Gets the maximum size of a document.
        /// </summary>
        /// <value>
        /// The maximum size of a document.
        /// </value>
        public int MaxDocumentSize
        {
            get { return _maxDocumentSize; }
        }

        /// <summary>
        /// Gets the maximum size of a message.
        /// </summary>
        /// <value>
        /// The maximum size of a message.
        /// </value>
        public int MaxMessageSize
        {
            get { return _maxMessageSize; }
        }

        /// <summary>
        /// Gets the maximum size of a wire document.
        /// </summary>
        /// <value>
        /// The maximum size of a wire document.
        /// </value>
        public int MaxWireDocumentSize
        {
            get { return _maxDocumentSize + 16 * 1024; }
        }

        /// <summary>
        /// Gets the maximum wire version.
        /// </summary>
        /// <value>
        /// The maximum wire version.
        /// </value>
        public int MaxWireVersion
        {
            get { return _maxWireVersion; }
        }

        /// <summary>
        /// Gets the minimum wire version.
        /// </summary>
        /// <value>
        /// The minimum wire version.
        /// </value>
        public int MinWireVersion
        {
            get { return _minWireVersion; }
        }

        /// <summary>
        /// Gets the server version.
        /// </summary>
        /// <value>
        /// The server version.
        /// </value>
        [Obsolete("Use MaxWireVersion instead.")]
        public SemanticVersion ServerVersion
        {
            get { return _serverVersion; }
        }

        /// <summary>
        /// Gets the service identifier.
        /// </summary>
        /// <value>
        /// The service identifier.
        /// </value>
        public ObjectId? ServiceId
        {
            get { return _serviceId; }
        }

        // methods
        /// <inheritdoc/>
        public bool Equals(ConnectionDescription other)
        {
            if (other == null)
            {
                return false;
            }

            return
                _connectionId.StructurallyEquals(other._connectionId) &&
                _helloResult.Equals(other._helloResult);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as ConnectionDescription);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return new Hasher()
                .Hash(_connectionId)
                .Hash(_helloResult)
                .GetHashCode();
        }

        /// <summary>
        /// Returns a new instance of ConnectionDescription with a different connection identifier.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>A connection description.</returns>
        public ConnectionDescription WithConnectionId(ConnectionId value)
        {
            return _connectionId.StructurallyEquals(value) ? this : new ConnectionDescription(value, _helloResult);
        }
    }
}
