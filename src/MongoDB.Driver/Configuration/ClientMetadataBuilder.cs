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

using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Configuration;

/// <summary>
/// Configures the metadata a <see cref="MongoClient"/> reports about itself to the server during the
/// handshake. Most applications only set <see cref="ApplicationName"/>, which identifies the application
/// in server logs, in profiler output, and in the Atlas UI.
/// </summary>
public sealed class ClientMetadataBuilder
{
    internal ClientMetadataBuilder()
    {
    }

    /// <summary>
    /// Gets or sets the application name reported to the server during the handshake and surfaced in
    /// server logs and profiler output. Must be at most 128 bytes when encoded as UTF-8.
    /// The default value is <c>null</c>.
    /// </summary>
    public string ApplicationName
    {
        get;
        set => field = ApplicationNameHelper.EnsureApplicationNameIsValid(value, nameof(ApplicationName));
    }

    /// <summary>
    /// Gets or sets information about a library built on top of the .NET driver, appended to the driver
    /// metadata sent during the handshake. Intended for ODMs and framework integrations rather than
    /// applications. The default value is <c>null</c>.
    /// </summary>
    public LibraryInfo LibraryInfo { get; set; }
}
