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
using MongoDB.Shared;

namespace MongoDB.Driver.Core.Configuration;

/// <summary>
/// Tracing-related settings for MongoDB operations.
/// </summary>
public sealed class TracingOptions : IEquatable<TracingOptions>
{
    /// <summary>
    /// Gets or sets whether tracing is disabled for this client.
    /// When set to true, no OpenTelemetry activities will be created for this client's operations.
    /// Default is false (tracing enabled if configured via TracerProvider).
    /// </summary>
    public bool Disabled { get; set; }

    /// <summary>
    /// Gets or sets the maximum length of the db.query.text attribute.
    /// Default is 0 (attribute not added).
    /// </summary>
    public int QueryTextMaxLength { get; set; }

    internal TracingOptions Clone()
    {
        return new TracingOptions
        {
            QueryTextMaxLength = QueryTextMaxLength,
            Disabled = Disabled
        };
    }

    /// <summary>
    /// Determines whether two TracingOptions instances are equal.
    /// </summary>
    public static bool operator ==(TracingOptions lhs, TracingOptions rhs)
    {
        return object.Equals(lhs, rhs);
    }

    /// <summary>
    /// Determines whether two TracingOptions instances are not equal.
    /// </summary>
    public static bool operator !=(TracingOptions lhs, TracingOptions rhs)
    {
        return !(lhs == rhs);
    }

    /// <summary>
    /// Determines whether the specified TracingOptions is equal to this instance.
    /// </summary>
    public bool Equals(TracingOptions other)
    {
        return
            other != null &&
            QueryTextMaxLength == other.QueryTextMaxLength &&
            Disabled == other.Disabled;
    }

    /// <inheritdoc/>
    public override bool Equals(object obj) => Equals(obj as TracingOptions);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return new Hasher()
            .Hash(QueryTextMaxLength)
            .Hash(Disabled)
            .GetHashCode();
    }
}