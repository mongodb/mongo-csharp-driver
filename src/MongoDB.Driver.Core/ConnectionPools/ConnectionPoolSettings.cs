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

namespace MongoDB.Driver.Core.ConnectionPools
{
    /// <summary>
    /// Represents settings for a pool of connections.
    /// </summary>
    public class ConnectionPoolSettings
    {
        // fields
        private readonly TimeSpan _connectionMaxIdleTime;
        private readonly TimeSpan _connectionMaxLifeTime;
        private readonly TimeSpan _maintenanceInterval;
        private readonly int _maxConnections;

        // constructors
        public ConnectionPoolSettings()
        {
            _connectionMaxIdleTime = TimeSpan.FromMinutes(10);
            _connectionMaxLifeTime = TimeSpan.FromMinutes(30);
            _maintenanceInterval = TimeSpan.FromMinutes(1);
            _maxConnections = 10;
        }

        private ConnectionPoolSettings(
            TimeSpan connectionMaxIdleTime,
            TimeSpan connectionMaxLifeTime,
            TimeSpan maintenanceInterval,
            int maxConnections)
        {
            _connectionMaxIdleTime = connectionMaxIdleTime;
            _connectionMaxLifeTime = connectionMaxLifeTime;
            _maintenanceInterval = maintenanceInterval;
            _maxConnections = maxConnections;
        }

        // properties
        public TimeSpan ConnectionMaxIdleTime
        {
            get { return _connectionMaxIdleTime; }
        }

        public TimeSpan ConnectionMaxLifeTime
        {
            get { return _connectionMaxLifeTime; }
        }

        public TimeSpan MaintenanceInterval
        {
            get { return _maintenanceInterval; }
        }

        public int MaxConnections
        {
            get { return _maxConnections; }
        }

        // methods
        public ConnectionPoolSettings WithConnectionMaxIdleTime(TimeSpan value)
        {
            return (_connectionMaxIdleTime == value) ? this : new Builder(this) { _connectionMaxIdleTime = value }.Build();
        }

        public ConnectionPoolSettings WithConnectionMaxLifeTime(TimeSpan value)
        {
            return (_connectionMaxLifeTime == value) ? this : new Builder(this) { _connectionMaxLifeTime = value }.Build();
        }

        public ConnectionPoolSettings WithMaintenanceInterval(TimeSpan value)
        {
            return (_maintenanceInterval == value) ? this : new Builder(this) { _maintenanceInterval = value }.Build();
        }

        public ConnectionPoolSettings WithMaxConnections(int value)
        {
            return (_maxConnections == value) ? this : new Builder(this) { _maxConnections = value }.Build();
        }

        // nested types
        private struct Builder
        {
            // fields
            public TimeSpan _connectionMaxIdleTime;
            public TimeSpan _connectionMaxLifeTime;
            public TimeSpan _maintenanceInterval;
            public int _maxConnections;

            // constructors
            public Builder(ConnectionPoolSettings other)
            {
                _connectionMaxIdleTime = other._connectionMaxIdleTime;
                _connectionMaxLifeTime = other._connectionMaxLifeTime;
                _maintenanceInterval = other._maintenanceInterval;
                _maxConnections = other._maxConnections;
            }

            // methods
            public ConnectionPoolSettings Build()
            {
                return new ConnectionPoolSettings(
                    _connectionMaxIdleTime,
                    _connectionMaxLifeTime,
                    _maintenanceInterval,
                    _maxConnections);
            }
        }
    }
}
