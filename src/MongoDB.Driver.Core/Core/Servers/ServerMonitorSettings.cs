/* Copyright 2020-present MongoDB Inc.
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
using System.Net;

namespace MongoDB.Driver.Core.Servers
{
    internal class ServerMonitorSettings
    {
        private readonly TimeSpan _connectTimeout;
        private readonly TimeSpan _heartbeatInterval;
        private readonly TimeSpan _minHeartbeatInterval;

        public ServerMonitorSettings(TimeSpan connectTimeout, TimeSpan heartbeatInterval, Optional<TimeSpan> minHeartbeatInterval = default)
        {
            _connectTimeout = connectTimeout;
            _heartbeatInterval = heartbeatInterval;
            _minHeartbeatInterval = minHeartbeatInterval.WithDefault(TimeSpan.FromMilliseconds(500));
        }

        public TimeSpan ConnectTimeout => _connectTimeout;
        public TimeSpan HeartbeatInterval => _heartbeatInterval;
        public TimeSpan MinHeartbeatInterval => _minHeartbeatInterval;
    }
}
