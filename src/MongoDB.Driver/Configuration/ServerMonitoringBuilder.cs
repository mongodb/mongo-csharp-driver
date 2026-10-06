/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Configuration;

/// <summary>
/// Configures how a <see cref="MongoClient"/> discovers and tracks the state of each server in the
/// topology (SDAM monitoring).
/// </summary>
public sealed class ServerMonitoringBuilder
{
    internal ServerMonitoringBuilder()
    {
        HeartbeatInterval = ServerSettings.DefaultHeartbeatInterval;
        HeartbeatTimeout = ServerSettings.DefaultHeartbeatTimeout;
        Mode = ServerSettings.DefaultServerMonitoringMode;
    }

    /// <summary>
    /// Gets or sets the interval between checks of each server's state. Must be greater than zero.
    /// Defaults to <see cref="ServerSettings.DefaultHeartbeatInterval"/> (10 seconds).
    /// </summary>
    public TimeSpan HeartbeatInterval
    {
        get;
        set => field = Ensure.IsGreaterThanZero(value, nameof(HeartbeatInterval));
    }

    /// <summary>
    /// Gets or sets how long a single server check may take before it is considered to have failed.
    /// Must be infinite, or greater than zero. Defaults to
    /// <see cref="ServerSettings.DefaultHeartbeatTimeout"/> (infinite, meaning the connect timeout applies).
    /// </summary>
    public TimeSpan HeartbeatTimeout
    {
        get;
        set => field = Ensure.IsInfiniteOrGreaterThanZero(value, nameof(HeartbeatTimeout));
    }

    /// <summary>
    /// Gets or sets whether servers are monitored with the streaming or polling protocol. Defaults to
    /// <see cref="ServerMonitoringMode.Auto"/>, which uses streaming except when the client detects that
    /// it is running inside a FaaS environment.
    /// </summary>
    public ServerMonitoringMode Mode { get; set; }
}
