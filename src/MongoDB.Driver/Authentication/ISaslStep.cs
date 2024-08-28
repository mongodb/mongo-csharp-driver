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

using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver.Authentication
{
    /// <summary>
    /// Represents a SASL step.
    /// </summary>
    public interface ISaslStep
    {
        // methods
        /// <summary>
        /// Executes the SASL step and create the next step.
        /// </summary>
        /// <param name="conversation">The SASL conversation.</param>
        /// <param name="bytesReceivedFromServer">The bytes received from server.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The next SASL step.</returns>
        (byte[] BytesToSendToServer, ISaslStep NextStep) Execute(SaslConversation conversation, byte[] bytesReceivedFromServer, CancellationToken cancellationToken);

        /// <summary>
        /// Executes the SASL step and create the next step.
        /// </summary>
        /// <param name="conversation">The SASL conversation.</param>
        /// <param name="bytesReceivedFromServer">The bytes received from server.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The next SASL step.</returns>
        Task<(byte[] BytesToSendToServer, ISaslStep NextStep)> ExecuteAsync(SaslConversation conversation, byte[] bytesReceivedFromServer, CancellationToken cancellationToken);
    }
}

