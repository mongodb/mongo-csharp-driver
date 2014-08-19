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

namespace MongoDB.Driver.Core.Configuration
{
    /// <summary>
    /// Represents settings for a pool of connections.
    /// </summary>
    public class ConnectionPoolSettings
    {
        // fields
        private readonly TimeSpan _maintenanceInterval;
        private readonly int _maxConnections;
        private readonly int _minConnections;
        private readonly int _waitQueueSize;
        private readonly TimeSpan _waitQueueTimeout;

        // constructors
        public ConnectionPoolSettings()
        {
            _maintenanceInterval = TimeSpan.FromMinutes(1);
            _maxConnections = 100;
            _minConnections = 0;
            _waitQueueSize = _maxConnections * 5;
            _waitQueueTimeout = TimeSpan.FromMinutes(2);
        }

        private ConnectionPoolSettings(
            TimeSpan maintenanceInterval,
            int maxConnections,
            int minConnections,
            int waitQueueSize,
            TimeSpan waitQueueTimeout)
        {
            _maintenanceInterval = maintenanceInterval;
            _maxConnections = maxConnections;
            _minConnections = minConnections;
            _waitQueueSize = waitQueueSize;
            _waitQueueTimeout = waitQueueTimeout;
        }

        // properties
        public TimeSpan MaintenanceInterval
        {
            get { return _maintenanceInterval; }
        }

        public int MaxConnections
        {
            get { return _maxConnections; }
        }

        public int MinConnections
        {
            get { return _minConnections; }
        }

        public int WaitQueueSize
        {
            get { return _waitQueueSize; }
        }

        public TimeSpan WaitQueueTimeout
        {
            get { return _waitQueueTimeout; }
        }

        // methods
        public ConnectionPoolSettings WithMaintenanceInterval(TimeSpan value)
        {
            return (_maintenanceInterval == value) ? this : new Builder(this) { _maintenanceInterval = value }.Build();
        }

        public ConnectionPoolSettings WithMaxConnections(int value)
        {
            return (_maxConnections == value) ? this : new Builder(this) { _maxConnections = value }.Build();
        }

        public ConnectionPoolSettings WithMinConnections(int value)
        {
            return (_minConnections == value) ? this : new Builder(this) { _minConnections = value }.Build();
        }

        public ConnectionPoolSettings WithWaitQueueSize(int value)
        {
            return (_waitQueueSize == value) ? this : new Builder(this) { _waitQueueSize = value }.Build();
        }

        public ConnectionPoolSettings WithWaitQueueTimeout(TimeSpan value)
        {
            return (_waitQueueTimeout == value) ? this : new Builder(this) { _waitQueueTimeout = value }.Build();
        }

        // nested types
        private struct Builder
        {
            // fields
            public TimeSpan _maintenanceInterval;
            public int _maxConnections;
            public int _minConnections;
            public int _waitQueueSize;
            public TimeSpan _waitQueueTimeout;

            // constructors
            public Builder(ConnectionPoolSettings other)
            {
                _maintenanceInterval = other._maintenanceInterval;
                _maxConnections = other._maxConnections;
                _minConnections = other._minConnections;
                _waitQueueSize = other._waitQueueSize;
                _waitQueueTimeout = other._waitQueueTimeout;
            }

            // methods
            public ConnectionPoolSettings Build()
            {
                return new ConnectionPoolSettings(
                    _maintenanceInterval,
                    _maxConnections,
                    _minConnections,
                    _waitQueueSize,
                    _waitQueueTimeout);
            }
        }
    }
}
