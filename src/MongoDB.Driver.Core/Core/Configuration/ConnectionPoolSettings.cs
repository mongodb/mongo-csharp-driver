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
using MongoDB.Driver.Core.Misc;

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
        public ConnectionPoolSettings(
            Optional<TimeSpan> maintenanceInterval = default(Optional<TimeSpan>),
            Optional<int> maxConnections = default(Optional<int>),
            Optional<int> minConnections = default(Optional<int>),
            Optional<int> waitQueueSize = default(Optional<int>),
            Optional<TimeSpan> waitQueueTimeout = default(Optional<TimeSpan>))
        {
            _maintenanceInterval = Ensure.IsInfiniteOrGreaterThanOrEqualToZero(maintenanceInterval.WithDefault(TimeSpan.FromMinutes(1)), "maintenanceInterval");
            _maxConnections = Ensure.IsGreaterThanZero(maxConnections.WithDefault(100), "maxConnections");
            _minConnections = Ensure.IsGreaterThanOrEqualToZero(minConnections.WithDefault(0), "minConnections");
            _waitQueueSize = Ensure.IsGreaterThanOrEqualToZero(waitQueueSize.WithDefault(_maxConnections * 5), "waitQueueSize");
            _waitQueueTimeout = Ensure.IsInfiniteOrGreaterThanOrEqualToZero(waitQueueTimeout.WithDefault(TimeSpan.FromMinutes(2)), "waitQueueTimeout");
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
        public ConnectionPoolSettings With (
            Optional<TimeSpan> maintenanceInterval = default(Optional<TimeSpan>),
            Optional<int> maxConnections = default(Optional<int>),
            Optional<int> minConnections = default(Optional<int>),
            Optional<int> waitQueueSize = default(Optional<int>),
            Optional<TimeSpan> waitQueueTimeout = default(Optional<TimeSpan>))
        {
            return new ConnectionPoolSettings(
                maintenanceInterval: maintenanceInterval.WithDefault(_maintenanceInterval),
                maxConnections: maxConnections.WithDefault(_maxConnections),
                minConnections: minConnections.WithDefault(_minConnections),
                waitQueueSize: waitQueueSize.WithDefault(_waitQueueSize),
                waitQueueTimeout: waitQueueTimeout.WithDefault(_waitQueueTimeout));
        }
    }
}
