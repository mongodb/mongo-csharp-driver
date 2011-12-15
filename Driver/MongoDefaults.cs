/* Copyright 2010-2011 10gen Inc.
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
        // public static fields
        private static bool assignIdOnInsert = true;
        private static TimeSpan connectTimeout = TimeSpan.FromSeconds(30);
        private static TimeSpan maxConnectionIdleTime = TimeSpan.FromMinutes(10);
        private static TimeSpan maxConnectionLifeTime = TimeSpan.FromMinutes(30);
        private static int maxConnectionPoolSize = 100;
        private static int maxMessageLength = 16000000; // 16MB (not 16 MiB!)
        private static int minConnectionPoolSize = 0;
        private static SafeMode safeMode = SafeMode.False;
        private static TimeSpan socketTimeout = TimeSpan.FromSeconds(30);
        private static int tcpReceiveBufferSize = 64 * 1024; // 64KiB (note: larger than 2MiB fails on Mac using Mono)
        private static int tcpSendBufferSize = 64 * 1024; // 64KiB (TODO: what is the optimum value for the buffers?)
        private static double waitQueueMultiple = 1.0; // default multiple of 1
        private static int waitQueueSize = 0; // use multiple by default
        private static TimeSpan waitQueueTimeout = TimeSpan.FromMilliseconds(500);

        // public static properties
        /// <summary>
        /// Gets or sets whether the driver should assign a value to empty Ids on Insert.
        /// </summary>
        public static bool AssignIdOnInsert
        {
            get { return assignIdOnInsert; }
            set { assignIdOnInsert = value; }
        }

        /// <summary>
        /// Gets the actual wait queue size (either WaitQueueSize or WaitQueueMultiple x MaxConnectionPoolSize).
        /// </summary>
        public static int ComputedWaitQueueSize
        {
            get
            {
                if (waitQueueMultiple == 0.0)
                {
                    return waitQueueSize;
                }
                else
                {
                    return (int)(waitQueueMultiple * maxConnectionPoolSize);
                }
            }
        }

        /// <summary>
        /// Gets or sets the connect timeout.
        /// </summary>
        public static TimeSpan ConnectTimeout
        {
            get { return connectTimeout; }
            set { connectTimeout = value; }
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
            get { return maxConnectionIdleTime; }
            set { maxConnectionIdleTime = value; }
        }

        /// <summary>
        /// Gets or sets the max connection life time.
        /// </summary>
        public static TimeSpan MaxConnectionLifeTime
        {
            get { return maxConnectionLifeTime; }
            set { maxConnectionLifeTime = value; }
        }

        /// <summary>
        /// Gets or sets the max connection pool size.
        /// </summary>
        public static int MaxConnectionPoolSize
        {
            get { return maxConnectionPoolSize; }
            set { maxConnectionPoolSize = value; }
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
            get { return maxMessageLength; }
            set { maxMessageLength = value; }
        }

        /// <summary>
        /// Gets or sets the min connection pool size.
        /// </summary>
        public static int MinConnectionPoolSize
        {
            get { return minConnectionPoolSize; }
            set { minConnectionPoolSize = value; }
        }

        /// <summary>
        /// Gets or sets the safe mode.
        /// </summary>
        public static SafeMode SafeMode
        {
            get { return safeMode; }
            set { safeMode = value; }
        }

        /// <summary>
        /// Gets or sets the socket timeout.
        /// </summary>
        public static TimeSpan SocketTimeout
        {
            get { return socketTimeout; }
            set { socketTimeout = value; }
        }

        /// <summary>
        /// Gets or sets the TCP receive buffer size.
        /// </summary>
        public static int TcpReceiveBufferSize
        {
            get { return tcpReceiveBufferSize; }
            set { tcpReceiveBufferSize = value; }
        }

        /// <summary>
        /// Gets or sets the TCP send buffer size.
        /// </summary>
        public static int TcpSendBufferSize
        {
            get { return tcpSendBufferSize; }
            set { tcpSendBufferSize = value; }
        }

        /// <summary>
        /// Gets or sets the wait queue multiple (the actual wait queue size will be WaitQueueMultiple x MaxConnectionPoolSize, see also WaitQueueSize).
        /// </summary>
        public static double WaitQueueMultiple
        {
            get { return waitQueueMultiple; }
            set
            {
                waitQueueMultiple = value;
                waitQueueSize = 0;
            }
        }

        /// <summary>
        /// Gets or sets the wait queue size (see also WaitQueueMultiple).
        /// </summary>
        public static int WaitQueueSize
        {
            get { return waitQueueSize; }
            set
            {
                waitQueueMultiple = 0;
                waitQueueSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the wait queue timeout.
        /// </summary>
        public static TimeSpan WaitQueueTimeout
        {
            get { return waitQueueTimeout; }
            set { waitQueueTimeout = value; }
        }
    }
}
