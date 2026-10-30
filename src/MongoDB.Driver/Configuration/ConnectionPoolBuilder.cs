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
using System.Threading;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Configuration;

/// <summary>
/// Configures the connection pool of a <see cref="MongoClient"/>.
/// </summary>
public sealed class ConnectionPoolBuilder
{
    internal ConnectionPoolBuilder()
    {
        MaintenanceInterval = TimeSpan.FromMinutes(1);
        MaxConnecting = MongoInternalDefaults.ConnectionPool.MaxConnecting;
        MaxConnectionIdleTime = MongoDefaults.MaxConnectionIdleTime;
        MaxConnectionLifeTime = MongoDefaults.MaxConnectionLifeTime;
        MaxSize = MongoDefaults.MaxConnectionPoolSize;
        MinSize = MongoDefaults.MinConnectionPoolSize;
        WaitQueueTimeout = MongoDefaults.WaitQueueTimeout;
    }

    /// <summary>
    /// Gets or sets the interval at which the pool prunes idle and expired connections and tops up
    /// to <see cref="MinSize"/>. <see cref="Timeout.InfiniteTimeSpan"/> disables the
    /// maintenance thread. Must be infinite, or greater than or equal to zero. The default value is 1 minute.
    /// </summary>
    public TimeSpan MaintenanceInterval
    {
        get;
        set => field = Ensure.IsInfiniteOrGreaterThanOrEqualToZero(value, nameof(MaintenanceInterval));
    }

    /// <summary>
    /// Gets or sets the maximum number of connections the pool may be establishing concurrently.
    /// Must be greater than zero. Defaults to <c>MongoInternalDefaults.ConnectionPool.MaxConnecting</c> (2).
    /// </summary>
    public int MaxConnecting
    {
        get;
        set => field = Ensure.IsGreaterThanZero(value, nameof(MaxConnecting));
    }

    /// <summary>
    /// Gets or sets the maximum time a connection may remain idle in the pool before it is closed.
    /// Must be greater than or equal to zero. Defaults to <see cref="MongoDefaults.MaxConnectionIdleTime"/> (10 minutes).
    /// </summary>
    public TimeSpan MaxConnectionIdleTime
    {
        get;
        set => field = Ensure.IsGreaterThanOrEqualToZero(value, nameof(MaxConnectionIdleTime));
    }

    /// <summary>
    /// Gets or sets the maximum time a connection may remain in the pool before it is closed.
    /// Must be greater than zero. Defaults to <see cref="MongoDefaults.MaxConnectionLifeTime"/> (30 minutes).
    /// </summary>
    public TimeSpan MaxConnectionLifeTime
    {
        get;
        set => field = Ensure.IsGreaterThanZero(value, nameof(MaxConnectionLifeTime));
    }

    /// <summary>
    /// Gets or sets the maximum number of connections the pool may contain. Must be greater than zero, and
    /// greater than or equal to <see cref="MinSize"/> when the client is built.
    /// Defaults to <see cref="MongoDefaults.MaxConnectionPoolSize"/> (100).
    /// </summary>
    public int MaxSize
    {
        get;
        set => field = Ensure.IsGreaterThanZero(value, nameof(MaxSize));
    }

    /// <summary>
    /// Gets or sets the minimum number of connections the pool maintains. Must be greater than or equal to
    /// zero, and less than or equal to <see cref="MaxSize"/> when the client is built.
    /// Defaults to <see cref="MongoDefaults.MinConnectionPoolSize"/> (0).
    /// </summary>
    public int MinSize
    {
        get;
        set => field = Ensure.IsGreaterThanOrEqualToZero(value, nameof(MinSize));
    }

    /// <summary>
    /// Gets or sets the maximum time a thread waits for a connection to become available.
    /// Must be infinite, or greater than or equal to zero. Defaults to <see cref="MongoDefaults.WaitQueueTimeout"/> (2 minutes).
    /// </summary>
    public TimeSpan WaitQueueTimeout
    {
        get;
        set => field = Ensure.IsInfiniteOrGreaterThanOrEqualToZero(value, nameof(WaitQueueTimeout));
    }
}
