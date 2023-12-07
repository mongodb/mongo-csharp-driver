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
using System.Threading.Tasks;

namespace MongoDB.Driver.Core.Authentication
{
    /// <summary>
    /// Represents a last SASL step.
    /// </summary>
    internal sealed class NoTransitionClientLastSaslStep : SaslAuthenticator.ISaslStep
    {
        private readonly byte[] _bytesToSendToServer;

        /// <summary>
        /// Initializes a new instance of the <see cref="NoTransitionClientLastSaslStep"/> class.
        /// </summary>
        public NoTransitionClientLastSaslStep(byte[] bytesToSendToServer)
        {
            _bytesToSendToServer = bytesToSendToServer;
        }

        /// <inheritdoc/>
        public byte[] BytesToSendToServer => _bytesToSendToServer;

        /// <inheritdoc/>
        public bool IsComplete => false;

        /// <inheritdoc/>
        public SaslAuthenticator.ISaslStep Transition(SaslAuthenticator.SaslConversation conversation, byte[] bytesReceivedFromServer)
        {
            if (bytesReceivedFromServer?.Length > 0)
            {
                // should not be reached
                throw new InvalidOperationException($"Received an additional {bytesReceivedFromServer} bytes from the server in last SASL response that was not expected.");
            }

            return new SaslAuthenticator.CompletedStep();
        }

        public Task<SaslAuthenticator.ISaslStep> TransitionAsync(SaslAuthenticator.SaslConversation conversation, byte[] bytesReceivedFromServer, CancellationToken cancellationToken)
            => Task.FromResult(Transition(conversation, bytesReceivedFromServer));
    }
}
