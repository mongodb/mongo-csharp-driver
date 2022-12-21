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

namespace MongoDB.Driver.Core.Authentication.Sasl
{
    /// <summary>
    /// Represents a SASL step.
    /// </summary>
    internal interface ISaslStep
    {
        /// <summary>
        /// Gets the bytes to send to server.
        /// </summary>
        byte[] BytesToSendToServer { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is complete.
        /// </summary>
        bool IsComplete { get; }
        /// <summary>
        /// Transitions the SASL conversation to the next step.
        /// </summary>
        /// <param name="conversation">The SASL conversation.</param>
        /// <param name="bytesReceivedFromServer">The bytes received from server.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The next SASL step.</returns>
        ISaslStep Transition(SaslConversation conversation, byte[] bytesReceivedFromServer, CancellationToken cancellationToken = default);
        /// <summary>
        /// Transitions the SASL conversation to the next step.
        /// </summary>
        /// <param name="conversation">The SASL conversation.</param>
        /// <param name="bytesReceivedFromServer">The bytes received from server.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The next SASL step.</returns>
        Task<ISaslStep> TransitionAsync(SaslConversation conversation, byte[] bytesReceivedFromServer, CancellationToken cancellationToken = default);
    }

    internal abstract class SaslStepBase : ISaslStep
    {
        // properties
        /// <inheritdoc/>
        public abstract byte[] BytesToSendToServer { get; }
        /// <inheritdoc/>
        public abstract bool IsComplete { get; }

        // methods
        /// <inheritdoc/>
        public abstract ISaslStep Transition(SaslConversation conversation, byte[] bytesReceivedFromServer, CancellationToken cancellationToken = default);
        /// <inheritdoc/>
        public virtual Task<ISaslStep> TransitionAsync(SaslConversation conversation, byte[] bytesReceivedFromServer, CancellationToken cancellationToken = default) =>
            // call sync version by default
            Task.FromResult(Transition(conversation, bytesReceivedFromServer, cancellationToken));
    }

    /// <summary>
    /// Represents a completed SASL step.
    /// </summary>
    internal sealed class CompletedStep : SaslStepBase
    {
        // fields
        private readonly byte[] _bytesToSendToServer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="CompletedStep"/> class.
        /// </summary>
        public CompletedStep()
            : this(new byte[0])
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompletedStep"/> class.
        /// </summary>
        /// <param name="bytesToSendToServer">The bytes to send to server.</param>
        public CompletedStep(byte[] bytesToSendToServer)
        {
            _bytesToSendToServer = bytesToSendToServer;
        }

        // properties
        /// <inheritdoc/>
        public override byte[] BytesToSendToServer => _bytesToSendToServer;

        /// <inheritdoc/>
        public override bool IsComplete => true;

        /// <inheritdoc/>
        public override ISaslStep Transition(SaslConversation conversation, byte[] bytesReceivedFromServer, CancellationToken cancellationToken)
        {
            if (bytesReceivedFromServer.Length > 0)
            {
                // should not be reached
                throw new InvalidOperationException("Not all authentication response has been handled.");
            }

            throw new InvalidOperationException("Sasl conversation has completed.");
        }
    }

    /// <summary>
    /// Represents a last SASL step.
    /// </summary>
    internal sealed class NoTransitionClientLast : SaslStepBase
    {
        private readonly byte[] _bytesToSendToServer;

        /// <summary>
        /// Initializes a new instance of the <see cref="NoTransitionClientLast"/> class.
        /// </summary>
        public NoTransitionClientLast(byte[] bytesToSendToServer)
        {
            _bytesToSendToServer = bytesToSendToServer;
        }

        /// <inheritdoc/>
        public override byte[] BytesToSendToServer => _bytesToSendToServer;

        /// <inheritdoc/>
        public override bool IsComplete => false;

        /// <inheritdoc/>
        public override ISaslStep Transition(SaslConversation conversation, byte[] bytesReceivedFromServer, CancellationToken cancellationToken)
        {
            if (bytesReceivedFromServer.Length > 0)
            {
                // should not be reached
                throw new InvalidOperationException("Not all authentication response has been handled.");
            }

            return new CompletedStep();
        }
    }
}
