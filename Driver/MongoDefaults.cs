/* Copyright 2010-2012 10gen Inc.
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
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using MongoDB.Bson;

namespace MongoDB.Driver
{
    /// <summary>
    /// Default values for various Mongo settings.
    /// </summary>
    public static class MongoDefaults
    {
        // private static fields
        private static bool __assignIdOnInsert = true;
        private static TimeSpan __connectTimeout = TimeSpan.FromSeconds(30);
        private static TimeSpan __maxConnectionIdleTime = TimeSpan.FromMinutes(10);
        private static TimeSpan __maxConnectionLifeTime = TimeSpan.FromMinutes(30);
        private static int __maxConnectionPoolSize = 100;
        private static int __maxMessageLength = 16000000; // 16MB (not 16 MiB!)
        private static int __minConnectionPoolSize = 0;
#pragma warning disable 612, 618
        private static SafeMode __safeMode = SafeMode.False;
#pragma warning restore
        private static TimeSpan __secondaryAcceptableLatency = TimeSpan.FromMilliseconds(15);
        private static TimeSpan __socketTimeout = TimeSpan.Zero; // use operating system default (presumably infinite)
        private static int __tcpReceiveBufferSize = 64 * 1024; // 64KiB (note: larger than 2MiB fails on Mac using Mono)
        private static int __tcpSendBufferSize = 64 * 1024; // 64KiB (TODO: what is the optimum value for the buffers?)
        private static double __waitQueueMultiple = 5.0; // default wait queue multiple is 5.0
        private static int __waitQueueSize = 0; // use multiple by default
        private static TimeSpan __waitQueueTimeout = TimeSpan.FromMinutes(2); // default wait queue timeout is 2 minutes

        // public static properties
        /// <summary>
        /// Gets or sets whether the driver should assign a value to empty Ids on Insert.
        /// </summary>
        public static bool AssignIdOnInsert
        {
            get { return __assignIdOnInsert; }
            set { __assignIdOnInsert = value; }
        }

        /// <summary>
        /// Gets the actual wait queue size (either WaitQueueSize or WaitQueueMultiple x MaxConnectionPoolSize).
        /// </summary>
        public static int ComputedWaitQueueSize
        {
            get
            {
                if (__waitQueueMultiple == 0.0)
                {
                    return __waitQueueSize;
                }
                else
                {
                    return (int)(__waitQueueMultiple * __maxConnectionPoolSize);
                }
            }
        }

        /// <summary>
        /// Gets or sets the connect timeout.
        /// </summary>
        public static TimeSpan ConnectTimeout
        {
            get { return __connectTimeout; }
            set { __connectTimeout = value; }
        }

        /// <summary>
        /// Gets or sets the representation to use for Guids (this is an alias for BsonDefaults.GuidRepresentation).
        /// </summary>
        public static GuidRepresentation GuidRepresentation
        {
            get { return BsonDefaults.GuidRepresentation; }
            set { BsonDefaults.GuidRepresentation = value; }
        }

        /// <summary>
        /// Gets or sets the max connection idle time.
        /// </summary>
        public static TimeSpan MaxConnectionIdleTime
        {
            get { return __maxConnectionIdleTime; }
            set { __maxConnectionIdleTime = value; }
        }

        /// <summary>
        /// Gets or sets the max connection life time.
        /// </summary>
        public static TimeSpan MaxConnectionLifeTime
        {
            get { return __maxConnectionLifeTime; }
            set { __maxConnectionLifeTime = value; }
        }

        /// <summary>
        /// Gets or sets the max connection pool size.
        /// </summary>
        public static int MaxConnectionPoolSize
        {
            get { return __maxConnectionPoolSize; }
            set { __maxConnectionPoolSize = value; }
        }

        /// <summary>
        /// Gets or sets the max document size (this is an alias for BsonDefaults.MaxDocumentSize).
        /// </summary>
        public static int MaxDocumentSize
        {
            get { return BsonDefaults.MaxDocumentSize; }
            set { BsonDefaults.MaxDocumentSize = value; }
        }

        /// <summary>
        /// Gets or sets the max message length.
        /// </summary>
        public static int MaxMessageLength
        {
            get { return __maxMessageLength; }
            set { __maxMessageLength = value; }
        }

        /// <summary>
        /// Gets or sets the min connection pool size.
        /// </summary>
        public static int MinConnectionPoolSize
        {
            get { return __minConnectionPoolSize; }
            set { __minConnectionPoolSize = value; }
        }

        /// <summary>
        /// Gets or sets the safe mode.
        /// </summary>
        [Obsolete("SafeMode has been replaced by WriteConcern and the default for WriteConcern is always Errors and is not configurable.")]
        public static SafeMode SafeMode
        {
            get { return __safeMode; }
            set { __safeMode = value; }
        }

        /// <summary>
        /// Gets or sets the default acceptable latency for considering a replica set member for inclusion in load balancing
        /// when using a read preference of Secondary, SecondaryPreferred, and Nearest.
        /// </summary>
        public static TimeSpan SecondaryAcceptableLatency
        {
            get { return __secondaryAcceptableLatency; }
            set { __secondaryAcceptableLatency = value; }
        }

        /// <summary>
        /// Gets or sets the socket timeout.
        /// </summary>
        public static TimeSpan SocketTimeout
        {
            get { return __socketTimeout; }
            set { __socketTimeout = value; }
        }

        /// <summary>
        /// Gets or sets the TCP receive buffer size.
        /// </summary>
        public static int TcpReceiveBufferSize
        {
            get { return __tcpReceiveBufferSize; }
            set { __tcpReceiveBufferSize = value; }
        }

        /// <summary>
        /// Gets or sets the TCP send buffer size.
        /// </summary>
        public static int TcpSendBufferSize
        {
            get { return __tcpSendBufferSize; }
            set { __tcpSendBufferSize = value; }
        }

        /// <summary>
        /// Gets or sets the wait queue multiple (the actual wait queue size will be WaitQueueMultiple x MaxConnectionPoolSize, see also WaitQueueSize).
        /// </summary>
        public static double WaitQueueMultiple
        {
            get { return __waitQueueMultiple; }
            set
            {
                __waitQueueMultiple = value;
                __waitQueueSize = 0;
            }
        }

        /// <summary>
        /// Gets or sets the wait queue size (see also WaitQueueMultiple).
        /// </summary>
        public static int WaitQueueSize
        {
            get { return __waitQueueSize; }
            set
            {
                __waitQueueMultiple = 0.0;
                __waitQueueSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the wait queue timeout.
        /// </summary>
        public static TimeSpan WaitQueueTimeout
        {
            get { return __waitQueueTimeout; }
            set { __waitQueueTimeout = value; }
        }
    }
}