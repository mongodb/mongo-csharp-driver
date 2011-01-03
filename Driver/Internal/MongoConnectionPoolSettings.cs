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
using System.Net;
using System.Text;
using System.Threading;

namespace MongoDB.Driver.Internal {
    public class MongoConnectionPoolSettings {
        #region private fields
        private TimeSpan connectTimeout;
        private TimeSpan maxConnectionIdleTime;
        private TimeSpan maxConnectionLifeTime;
        private int maxConnectionPoolSize;
        private int minConnectionPoolSize;
        private TimeSpan socketTimeout;
        private int waitQueueSize;
        private TimeSpan waitQueueTimeout;
        #endregion

        #region constructors
        internal MongoConnectionPoolSettings(
            TimeSpan connectTimeout,
            TimeSpan maxConnectionIdleTime,
            TimeSpan maxConnectionLifeTime,
            int maxConnectionPoolSize,
            int minConnectionPoolSize,
            TimeSpan socketTimeout,
            int waitQueueSize,
            TimeSpan waitQueueTimeout
        ) {
            this.connectTimeout = connectTimeout;
            this.maxConnectionIdleTime = maxConnectionIdleTime;
            this.maxConnectionLifeTime = maxConnectionLifeTime;
            this.maxConnectionPoolSize = maxConnectionPoolSize;
            this.minConnectionPoolSize = minConnectionPoolSize;
            this.socketTimeout = socketTimeout;
            this.waitQueueSize = waitQueueSize;
            this.waitQueueTimeout = waitQueueTimeout;
        }
        #endregion

        #region public properties
        public TimeSpan ConnectTimeout {
            get { return connectTimeout; }
        }

        public TimeSpan MaxConnectionIdleTime {
            get { return maxConnectionIdleTime; }
        }

        public TimeSpan MaxConnectionLifeTime {
            get { return maxConnectionLifeTime; }
        }

        public int MaxConnectionPoolSize {
            get { return maxConnectionPoolSize; }
        }

        public int MinConnectionPoolSize {
            get { return minConnectionPoolSize; }
        }

        public TimeSpan SocketTimeout {
            get { return socketTimeout; }
        }

        public int WaitQueueSize {
            get { return waitQueueSize; }
        }

        public TimeSpan WaitQueueTimeout {
            get { return waitQueueTimeout; }
        }
        #endregion
    }
}
