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
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;

namespace MongoDB.Driver.Configuration;

/// <summary>
/// Configures how a <see cref="MongoClient"/> executes operations: server selection, the read preference,
/// concerns, retry behavior and API version applied to operations that do not specify their own.
/// </summary>
public sealed class OperationsBuilder
{
    internal OperationsBuilder()
    {
        LocalThreshold = MongoDefaults.LocalThreshold;
        MaxAdaptiveRetries = RetryabilityHelper.OperationRetryBackpressureConstants.DefaultMaxRetries;
        ReadConcern = ReadConcern.Default;
        ReadPreference = ReadPreference.Primary;
        RetryReads = true;
        RetryWrites = true;
        ServerSelectionTimeout = MongoDefaults.ServerSelectionTimeout;
        WriteConcern = WriteConcern.Acknowledged;
    }

    /// <summary>
    /// Gets or sets whether overload retargeting is enabled. The default value is <c>false</c>.
    /// </summary>
    /// <remarks>This option requires MongoDB Atlas Server Version 9.0 and above.</remarks>
    public bool EnableOverloadRetargeting { get; set; }

    /// <summary>
    /// Gets or sets the window, measured from the round trip time of the fastest suitable server, within
    /// which other servers are still considered eligible for selection. Must be infinite, or greater than
    /// or equal to zero. Defaults to <see cref="MongoDefaults.LocalThreshold"/> (15 milliseconds).
    /// </summary>
    public TimeSpan LocalThreshold
    {
        get;
        set => field = Ensure.IsInfiniteOrGreaterThanOrEqualToZero(value, nameof(LocalThreshold));
    }

    /// <summary>
    /// Gets or sets the maximum number of adaptive retries for overload errors. Must be greater than or
    /// equal to zero. The default value is 2.
    /// </summary>
    /// <remarks>This option requires MongoDB Atlas Server Version 9.0 and above.</remarks>
    public int MaxAdaptiveRetries
    {
        get;
        set => field = Ensure.IsGreaterThanOrEqualToZero(value, nameof(MaxAdaptiveRetries));
    }

    /// <summary>
    /// Gets or sets a custom server selector that runs after the selector chosen for the operation (read
    /// preference or writable server) and before the latency window (<see cref="LocalThreshold"/>) is applied.
    /// Defaults to <c>null</c>, which means no additional selection.
    /// </summary>
    /// <remarks>
    /// The same instance is used for every server selection made by the client, possibly from several threads
    /// at once, so it must be thread-safe.
    /// </remarks>
    public IServerSelector PostServerSelector { get; set; }

    /// <summary>
    /// Gets or sets a custom server selector that runs before the selector chosen for the operation (read
    /// preference or writable server). Defaults to <c>null</c>, which means no additional selection.
    /// </summary>
    /// <remarks>
    /// The same instance is used for every server selection made by the client, possibly from several threads
    /// at once, so it must be thread-safe.
    /// </remarks>
    public IServerSelector PreServerSelector { get; set; }

    /// <summary>
    /// Gets or sets the default read concern used by operations that do not specify their own.
    /// Must not be null. Defaults to <see cref="ReadConcern.Default"/>.
    /// </summary>
    public ReadConcern ReadConcern
    {
        get;
        set => field = Ensure.IsNotNull(value, nameof(ReadConcern));
    }

    /// <summary>
    /// Gets or sets the default read preference used by operations that do not specify their own.
    /// Must not be null. Defaults to <see cref="ReadPreference.Primary"/>.
    /// </summary>
    public ReadPreference ReadPreference
    {
        get;
        set => field = Ensure.IsNotNull(value, nameof(ReadPreference));
    }

    /// <summary>
    /// Gets or sets whether reads are retried once after a retryable error. The default value is <c>true</c>.
    /// </summary>
    public bool RetryReads { get; set; }

    /// <summary>
    /// Gets or sets whether writes are retried once after a retryable error. The default value is <c>true</c>.
    /// </summary>
    public bool RetryWrites { get; set; }

    /// <summary>
    /// Gets or sets the server API version to declare on every command. The default value is <c>null</c>,
    /// which does not declare an API version.
    /// </summary>
    public ServerApi ServerApi { get; set; }

    /// <summary>
    /// Gets or sets how long the client waits for a suitable server to become available before failing
    /// the operation. Must be greater than or equal to zero. Defaults to
    /// <see cref="MongoDefaults.ServerSelectionTimeout"/> (30 seconds).
    /// </summary>
    public TimeSpan ServerSelectionTimeout
    {
        get;
        set => field = Ensure.IsGreaterThanOrEqualToZero(value, nameof(ServerSelectionTimeout));
    }

    /// <summary>
    /// Gets or sets the default write concern used by operations that do not specify their own.
    /// Must not be null. Defaults to <see cref="WriteConcern.Acknowledged"/>.
    /// </summary>
    public WriteConcern WriteConcern
    {
        get;
        set => field = Ensure.IsNotNull(value, nameof(WriteConcern));
    }
}
