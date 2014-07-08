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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Represents settings for a TCP stream.
    /// </summary>
    public class TcpStreamSettings
    {
        // fields
        private readonly TimeSpan? _readTimeout;
        private readonly int _receiveBufferSize;
        private readonly int _sendBufferSize;
        private readonly TimeSpan? _writeTimeout;

        // constructors
        public TcpStreamSettings()
        {
            _receiveBufferSize = 64 * 1024;
            _sendBufferSize = 64 * 1024;
        }

        private TcpStreamSettings(
            TimeSpan? readTimeout,
            int receiveBufferSize,
            int sendBufferSize,
            TimeSpan? writeTimeout)
        {
            _readTimeout = readTimeout;
            _receiveBufferSize = receiveBufferSize;
            _sendBufferSize = sendBufferSize;
            _writeTimeout = writeTimeout;
        }

        // properties
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
        public TcpStreamSettings WithReadTimeout(TimeSpan? value)
        {
            Ensure.IsNullOrValidTimeout(value, "value");
            return _readTimeout == value ? this : new Builder(this) { _readTimeout = value }.Build();
        }

        public TcpStreamSettings WithReceiveBufferSize(int value)
        {
            Ensure.IsGreaterThanZero(value, "value");
            return _receiveBufferSize == value ? this : new Builder(this) { _receiveBufferSize = value }.Build();
        }

        public TcpStreamSettings WithSendBufferSize(int value)
        {
            Ensure.IsGreaterThanZero(value, "value");
            return _sendBufferSize == value ? this : new Builder(this) { _sendBufferSize = value }.Build();
        }

        public TcpStreamSettings WithWriteTimeout(TimeSpan? value)
        {
            Ensure.IsNullOrValidTimeout(value, "value");
            return _writeTimeout == value ? this : new Builder(this) { _writeTimeout = value }.Build();
        }

        // nested types
        private struct Builder
        {
            // fields
            public TimeSpan? _readTimeout;
            public int _receiveBufferSize;
            public int _sendBufferSize;
            public TimeSpan? _writeTimeout;

            // constructors
            public Builder(TcpStreamSettings other)
            {
                _readTimeout = other._readTimeout;
                _receiveBufferSize = other._receiveBufferSize;
                _sendBufferSize = other._sendBufferSize;
                _writeTimeout = other._writeTimeout;
            }

            // methods
            public TcpStreamSettings Build()
            {
                return new TcpStreamSettings(
                    _readTimeout,
                    _receiveBufferSize,
                    _sendBufferSize,
                    _writeTimeout);
            }
        }
    }
}
