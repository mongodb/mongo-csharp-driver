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

using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Configuration;

/// <summary>
/// Configures how a <see cref="MongoClient"/> translates .NET expression trees into MongoDB expressions.
/// These are the defaults for operations that do not specify their own translation options.
/// </summary>
public sealed class TranslationBuilder
{
    internal TranslationBuilder()
    {
    }

    /// <summary>
    /// Gets or sets the server version to target when translating expressions. The default value is
    /// <c>null</c>, which targets the newest supported behavior.
    /// </summary>
    public ServerVersion? CompatibilityLevel { get; set; }

    /// <summary>
    /// Gets or sets whether client side projections are enabled. The default value is <c>null</c>,
    /// which leaves the provider default in effect.
    /// </summary>
    public bool? EnableClientSideProjections { get; set; }
}
