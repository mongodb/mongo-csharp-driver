/* Copyright 2010–present MongoDB Inc.
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

using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver.Core.Authentication.Sasl
{
    /// <summary>
    /// Represents a SASL mechanism.
    /// </summary>
    internal interface ISaslMechanism
    {
        // properties
        /// <summary>
        /// Gets the name of the mechanism.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        string Name { get; }

        // methods
        /// <summary>
        /// Initializes the mechanism.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="conversation">The SASL conversation.</param>
        /// <param name="description">The connection description.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The initial SASL step.</returns>
        ISaslStep Initialize(IConnection connection, SaslConversation conversation, ConnectionDescription description, CancellationToken cancellationToken);
        /// <summary>
        /// Initializes the mechanism.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="conversation">The SASL conversation.</param>
        /// <param name="description">The connection description.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The initial SASL step.</returns>
        Task<ISaslStep> InitializeAsync(IConnection connection, SaslConversation conversation, ConnectionDescription description, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Represents a base implementation for SASL mechanism.
    /// </summary>
    internal abstract class SaslMechanismBase : ISaslMechanism
    {
        /// <inheritdoc/>
        public abstract string Name { get; }
        /// <inheritdoc/>
        public abstract ISaslStep Initialize(IConnection connection, SaslConversation conversation, ConnectionDescription description, CancellationToken cancellationToken);
        /// <inheritdoc/>
        public virtual Task<ISaslStep> InitializeAsync(IConnection connection, SaslConversation conversation, ConnectionDescription description, CancellationToken cancellationToken) => Task.FromResult(Initialize(connection, conversation, description, cancellationToken));
    }
}
