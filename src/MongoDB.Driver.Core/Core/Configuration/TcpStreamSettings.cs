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
using System.Net.Sockets;
using System.Threading;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Configuration
{
    /// <summary>
    /// Represents settings for a TCP stream.
    /// </summary>
    public class TcpStreamSettings
    {
        // fields
        private readonly AddressFamily _addressFamily;
        private readonly TimeSpan _connectTimeout;
        private readonly TimeSpan? _readTimeout;
        private readonly int _receiveBufferSize;
        private readonly int _sendBufferSize;
        private readonly TimeSpan? _writeTimeout;

        // constructors
        public TcpStreamSettings(
            Optional<AddressFamily> addressFamily = default(Optional<AddressFamily>),
            Optional<TimeSpan> connectTimeout = default(Optional<TimeSpan>),
            Optional<TimeSpan?> readTimeout = default(Optional<TimeSpan?>),
            Optional<int> receiveBufferSize = default(Optional<int>),
            Optional<int> sendBufferSize = default(Optional<int>),
            Optional<TimeSpan?> writeTimeout = default(Optional<TimeSpan?>))
        {
            _addressFamily = addressFamily.WithDefault(AddressFamily.InterNetwork);
            _connectTimeout = Ensure.IsInfiniteOrGreaterThanOrEqualToZero(connectTimeout.WithDefault(Timeout.InfiniteTimeSpan), "connectTimeout");
            _readTimeout = Ensure.IsNullOrInfiniteOrGreaterThanOrEqualToZero(readTimeout.WithDefault(null), "readTimeout");
            _receiveBufferSize = Ensure.IsGreaterThanZero(receiveBufferSize.WithDefault(64 * 1024), "receiveBufferSize");
            _sendBufferSize = Ensure.IsGreaterThanZero(sendBufferSize.WithDefault(64 * 1024), "sendBufferSize");
            _writeTimeout = Ensure.IsNullOrInfiniteOrGreaterThanOrEqualToZero(writeTimeout.WithDefault(null), "writeTimeout");
        }

        // properties
        public AddressFamily AddressFamily
        {
            get { return _addressFamily; }
        }

        public TimeSpan ConnectTimeout
        {
            get { return _connectTimeout; }
        }

        public TimeSpan? ReadTimeout
        {
            get { return _readTimeout; }
        }

        public int ReceiveBufferSize
        {
            get { return _receiveBufferSize; }
        }

        public int SendBufferSize
        {
            get { return _sendBufferSize; }
        }

        public TimeSpan? WriteTimeout
        {
            get { return _writeTimeout; }
        }

        // methods
        public TcpStreamSettings With(
            Optional<AddressFamily> addressFamily = default(Optional<AddressFamily>),
            Optional<TimeSpan> connectTimeout = default(Optional<TimeSpan>),
            Optional<TimeSpan?> readTimeout = default(Optional<TimeSpan?>),
            Optional<int> receiveBufferSize = default(Optional<int>),
            Optional<int> sendBufferSize = default(Optional<int>),
            Optional<TimeSpan?> writeTimeout = default(Optional<TimeSpan?>))
        {
            return new TcpStreamSettings(
                addressFamily: addressFamily.WithDefault(_addressFamily),
                connectTimeout: connectTimeout.WithDefault(_connectTimeout),
                readTimeout: readTimeout.WithDefault(_readTimeout),
                receiveBufferSize: receiveBufferSize.WithDefault(_receiveBufferSize),
                sendBufferSize: sendBufferSize.WithDefault(_sendBufferSize),
                writeTimeout: writeTimeout.WithDefault(_writeTimeout));
        }
    }
}
