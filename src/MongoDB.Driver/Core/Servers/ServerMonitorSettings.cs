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

namespace MongoDB.Driver.Core.Servers
{
    internal class ServerMonitorSettings
    {
        public ServerMonitorSettings(TimeSpan connectTimeout, TimeSpan heartbeatInterval, TimeSpan heartbeatTimeout, Optional<TimeSpan> minHeartbeatInterval = default, Optional<ServerMonitoringMode> serverMonitoringMode = default)
        {
            ConnectTimeout = connectTimeout;
            HeartbeatInterval = heartbeatInterval;
            HeartbeatTimeout = heartbeatTimeout;
            MinHeartbeatInterval = minHeartbeatInterval.WithDefault(TimeSpan.FromMilliseconds(500));
            ServerMonitoringMode = serverMonitoringMode.WithDefault(ServerMonitoringMode.Auto);
        }

        public TimeSpan ConnectTimeout { get; }
        public TimeSpan HeartbeatInterval { get; }
        public TimeSpan HeartbeatTimeout { get; }
        public TimeSpan MinHeartbeatInterval { get; }
        public ServerMonitoringMode ServerMonitoringMode { get; }
    }
}
