/* Copyright 2010-2014 MongoDB Inc.
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
using System.Net;
using System.Threading;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents an instance of a MongoDB server host.
    /// </summary>
    public sealed class MongoServerInstance
    {
        // private static fields
        private static int __nextSequentialId;

        // public events
        /// <summary>
        /// Occurs when the value of the State property changes.
        /// </summary>
        public event EventHandler StateChanged;

        // private fields
        private readonly MongoServerSettings _settings;
        private readonly MongoConnectionPool _connectionPool;
        private readonly MongoServerAddress _address;
        private readonly int _sequentialId;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MongoServerInstance"/> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="address">The address.</param>
        internal MongoServerInstance(MongoServerSettings settings, MongoServerAddress address)
        {
            _settings = settings;
            _address = address;
            _sequentialId = Interlocked.Increment(ref __nextSequentialId);
            _connectionPool = new MongoConnectionPool(this);
        }

        // public properties
        /// <summary>
        /// Gets the instance type.
        /// </summary>
        public MongoServerInstanceType InstanceType
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        // public properties
        /// <summary>
        /// Gets the address of this server instance.
        /// </summary>
        public MongoServerAddress Address
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets the build info of this server instance.
        /// </summary>
        public MongoServerBuildInfo BuildInfo
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets the exception thrown the last time Connect was called (null if Connect did not throw an exception).
        /// </summary>
        public Exception ConnectException
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets the connection pool for this server instance.
        /// </summary>
        public MongoConnectionPool ConnectionPool
        {
            get { return _connectionPool; }
        }

        /// <summary>
        /// Gets whether this server instance is an arbiter instance.
        /// </summary>
        public bool IsArbiter
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets the result of the most recent ismaster command sent to this server instance.
        /// </summary>
        public IsMasterResult IsMasterResult
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets whether this server instance is a passive instance.
        /// </summary>
        public bool IsPassive
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets whether this server instance is a primary.
        /// </summary>
        public bool IsPrimary
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets whether this server instance is a secondary.
        /// </summary>
        public bool IsSecondary
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets the max document size for this server instance.
        /// </summary>
        public int MaxDocumentSize
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets the max message length for this server instance.
        /// </summary>
        public int MaxMessageLength
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets the maximum size of a wire document. Normally slightly larger than MaxDocumentSize.
        /// </summary>
        /// <value>
        /// The  maximum size of a wire document.
        /// </value>
        public int MaxWireDocumentSize
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets the maximum batch count for write operations.
        /// </summary>
        /// <value>
        /// The maximum batch count.
        /// </value>
        public int MaxBatchCount
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets the unique sequential Id for this server instance.
        /// </summary>
        public int SequentialId
        {
            get { return _sequentialId; }
        }

        /// <summary>
        /// Gets the server for this server instance.
        /// </summary>
        public MongoServerSettings Settings
        {
            get { return _settings; }
        }

        /// <summary>
        /// Gets the state of this server instance.
        /// </summary>
        public MongoServerState State
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        // public methods
        /// <summary>
        /// Gets the IP end point of this server instance.
        /// </summary>
        /// <returns>The IP end point of this server instance.</returns>
        public IPEndPoint GetIPEndPoint()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Checks whether the server is alive (throws an exception if not).
        /// </summary>
        public void Ping()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Refreshes the state as soon as possible.
        /// </summary>
        public void RefreshStateAsSoonAsPossible()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Checks whether this server instance supports a feature.
        /// </summary>
        /// <param name="featureId">The id of the feature.</param>
        /// <returns>True if this server instance supports the feature; otherwise, false.</returns>
        public bool Supports(FeatureId featureId)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Verifies the state of the server instance.
        /// </summary>
        public void VerifyState()
        {
            throw new NotImplementedException();
        }

        // private methods
        private void OnStateChanged()
        {
            var handler = StateChanged;
            if (handler != null)
            {
                try
                {
                    handler(this, EventArgs.Empty);
                }
                catch
                {
                    // ignore exceptions
                }
            }
        }
    }
}
