/* Copyright 2010 10gen Inc.
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

namespace MongoDB.Driver {
    public static class MongoDefaults {
        #region public static fields
        private static TimeSpan connectTimeout = TimeSpan.FromSeconds(30);
        private static TimeSpan maxConnectionIdleTime = TimeSpan.FromMinutes(10);
        private static TimeSpan maxConnectionLifeTime = TimeSpan.FromMinutes(30);
        private static int maxConnectionPoolSize = 100;
        private static int maxMessageLength = 16000000; // 16MB (not 16 MiB!)
        private static int minConnectionPoolSize = 0;
        private static TimeSpan socketTimeout = TimeSpan.FromSeconds(30);
        private static int tcpReceiveBufferSize = 64 * 1024; // 64KiB (note: larger than 2MiB fails on Mac using Mono)
        private static int tcpSendBufferSize = 64 * 1024; // 64KiB (TODO: what is the optimum value for the buffers?)
        private static double waitQueueMultiple = 1; // default multiple of 1
        private static int waitQueueSize = 0; // use multiple by default
        private static TimeSpan waitQueueTimeout = TimeSpan.FromMilliseconds(500);
        #endregion

        #region public static properties
        public static TimeSpan ConnectTimeout {
            get { return connectTimeout; }
            set { connectTimeout = value; }
        }

        public static TimeSpan MaxConnectionIdleTime {
            get { return maxConnectionIdleTime; }
            set { maxConnectionIdleTime = value; }
        }

        public static TimeSpan MaxConnectionLifeTime {
            get { return maxConnectionLifeTime; }
            set { maxConnectionLifeTime = value; }
        }

        public static int MaxConnectionPoolSize {
            get { return maxConnectionPoolSize; }
            set { maxConnectionPoolSize = value; }
        }

        public static int MaxMessageLength {
            get { return maxMessageLength; }
            set { maxMessageLength = value; }
        }

        public static int MinConnectionPoolSize {
            get { return minConnectionPoolSize; }
            set { minConnectionPoolSize = value; }
        }

        public static TimeSpan SocketTimeout {
            get { return socketTimeout; }
            set { socketTimeout = value; }
        }

        public static int TcpReceiveBufferSize {
            get { return tcpReceiveBufferSize; }
            set { tcpReceiveBufferSize = value; }
        }

        public static int TcpSendBufferSize {
            get { return tcpSendBufferSize; }
            set { tcpSendBufferSize = value; }
        }

        public static double WaitQueueMultiple {
            get { return waitQueueMultiple; }
            set {
                waitQueueMultiple = value;
                waitQueueSize = 0;
            }
        }

        public static int WaitQueueSize {
            get { return waitQueueSize; }
            set {
                waitQueueMultiple = 0;
                waitQueueSize = value;
            }
        }

        public static TimeSpan WaitQueueTimeout {
            get { return waitQueueTimeout; }
            set { waitQueueTimeout = value; }
        }
        #endregion
    }
}
