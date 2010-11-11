﻿/* Copyright 2010 10gen Inc.
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

        static MongoDefaults() {

            // Mono doesn't support 4MB buffers (tested on Mac, unverified on Linux or Windows)
            Type t = Type.GetType ("Mono.Runtime");
            if (t != null) {
                TcpReceiveBufferSize = 2 * 1024 * 1024;
                TcpSendBufferSize = 2 * 1024 * 1024;
            }
        }

        #region public static fields
        private static TimeSpan connectTimeout = TimeSpan.FromSeconds(30);
        private static int maxMessageLength = 16 * 1024 * 1204; // 16MB
        private static int tcpReceiveBufferSize = 4 * 1024 * 1204; // 4MB
        private static int tcpSendBufferSize = 4 * 1024 * 1204; // 4MB
        #endregion

        #region public static properties
        public static TimeSpan ConnectTimeout {
            get { return connectTimeout; }
            set { connectTimeout = value; }
        }

        public static int MaxMessageLength {
            get { return maxMessageLength; }
            set { maxMessageLength = value; }
        }

        public static int TcpReceiveBufferSize {
            get { return tcpReceiveBufferSize; }
            set { tcpReceiveBufferSize = value; }
        }

        public static int TcpSendBufferSize {
            get { return tcpSendBufferSize; }
            set { tcpSendBufferSize = value; }
        }
        #endregion
    }
}
