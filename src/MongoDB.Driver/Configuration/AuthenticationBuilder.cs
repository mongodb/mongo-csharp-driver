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

namespace MongoDB.Driver.Configuration;

/// <summary>
/// Configures how a <see cref="MongoClient"/> authenticates with the server.
/// </summary>
/// <remarks>
/// Most applications set the credential through one of the mechanism specific extension methods in
/// <see cref="MongoClientBuilderExtensions"/> rather than by assigning <see cref="Credential"/> directly.
/// A client authenticates with a single credential, so each of those methods, like assigning
/// <see cref="Credential"/>, replaces whatever credential was configured before: the last one wins.
/// </remarks>
public sealed class AuthenticationBuilder
{
    internal AuthenticationBuilder()
    {
    }

    /// <summary>
    /// Gets or sets the credential used to authenticate with the server. The default value is
    /// <c>null</c>, which connects without authenticating.
    /// </summary>
    public MongoCredential Credential { get; set; }
}
