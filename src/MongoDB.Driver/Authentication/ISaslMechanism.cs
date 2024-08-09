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

using MongoDB.Bson;
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver.Authentication
{
    /// <summary>
    /// Represents a SASL mechanism.
    /// </summary>
    public interface ISaslMechanism
    {
        /// <summary>
        /// Gets the name of the mechanism.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the name of the database.
        /// </summary>
        public string DatabaseName { get; }

        /// <summary>
        /// Creates speculative authentication step if supported.
        /// </summary>
        /// <returns>Speculative authenticate step if supported by mechanism, otherwise <value>null</value>.</returns>
        public ISaslStep CreateSpeculativeAuthenticationStep();

        /// <summary>
        /// Optionally customizes SASL start command.
        /// </summary>
        /// <param name="startCommand">Sasl Start Command</param>
        /// <returns>Mutated command</returns>
        public BsonDocument CustomizeSaslStartCommand(BsonDocument startCommand);

        /// <summary>
        /// Method called when server returns 391 error (ReauthenticationRequired), so auth mechanism can clear cache or perform another activity to reset mechanism internal state.
        /// </summary>
        public void OnReAuthenticationRequired();

        /// <summary>
        /// Initializes the SASL conversation for the connection.
        /// </summary>
        /// <param name="conversation">The SASL conversation.</param>
        /// <param name="description">The connection description.</param>
        /// <returns>The initial SASL step.</returns>
        public ISaslStep Initialize(SaslConversation conversation, ConnectionDescription description);

        /// <summary>
        /// Tries to handle the authentication exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="step">The step caused the exception.</param>
        /// <param name="conversation">The SASL conversation.</param>
        /// <param name="description">The connection description.</param>
        /// <param name="nextStep">Next step to continue authentication with.</param>
        /// <returns><value>true</value> if the exception was handled and authentication can be continued with <paramref name="nextStep"/>; otherwise <value>false</value></returns>
        public bool TryHandleAuthenticationException(
            MongoException exception,
            ISaslStep step,
            SaslConversation conversation,
            ConnectionDescription description,
            out ISaslStep nextStep);
    }
}

