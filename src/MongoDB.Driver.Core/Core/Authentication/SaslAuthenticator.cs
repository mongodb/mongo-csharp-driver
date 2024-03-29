/* Copyright 2015-present MongoDB Inc.
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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol;

namespace MongoDB.Driver.Core.Authentication
{
    /// <summary>
    /// Base class for a SASL authenticator.
    /// </summary>
    [Obsolete("This class will be removed in later release.")]
    public abstract class SaslAuthenticator : IAuthenticator
    {
        /// <summary>
        /// The SASL start command.
        /// </summary>
        public const string SaslStartCommand = "saslStart";
        /// <summary>
        /// The SASL continue command.
        /// </summary>
        public const string SaslContinueCommand = "saslContinue";

        // fields
        private protected readonly ISaslMechanism _mechanism;
        private protected readonly ServerApi _serverApi;
        private protected ISaslStep _speculativeFirstStep;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SaslAuthenticator"/> class.
        /// </summary>
        /// <param name="mechanism">The mechanism.</param>
        [Obsolete("Use the newest overload instead.")]
        protected SaslAuthenticator(ISaslMechanism mechanism)
            : this(mechanism, serverApi: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SaslAuthenticator"/> class.
        /// </summary>
        /// <param name="mechanism">The mechanism.</param>
        /// <param name="serverApi">The server API.</param>
        protected SaslAuthenticator(ISaslMechanism mechanism, ServerApi serverApi)
        {
            _mechanism = Ensure.IsNotNull(mechanism, nameof(mechanism));
            _serverApi = serverApi; // can be null
        }

        // properties
        /// <inheritdoc/>
        public string Name => _mechanism.Name;

        /// <summary>
        /// Gets the name of the database.
        /// </summary>
        /// <value>
        /// The name of the database.
        /// </value>
        public abstract string DatabaseName { get; }

        // methods
        /// <inheritdoc/>
        public virtual void Authenticate(IConnection connection, ConnectionDescription description, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(connection, nameof(connection));
            Ensure.IsNotNull(description, nameof(description));

            using (var conversation = new SaslConversation(description.ConnectionId))
            {
                ISaslStep currentStep;
                int? conversationId = null;

                if (TryGetSpeculativeFirstStep(description, out currentStep, out var result))
                {
                    currentStep = Transition(conversation, currentStep, result);
                    conversationId = result.GetValue("conversationId").AsInt32;
                }
                else
                {
                    currentStep = _mechanism.Initialize(connection, conversation, description, cancellationToken);
                }

                while (currentStep != null)
                {
                    var command = conversationId.HasValue
                        ? CreateContinueCommand(currentStep, conversationId.Value)
                        : CreateStartCommand(currentStep);
                    try
                    {
                        var protocol = CreateCommandProtocol(command);
                        result = protocol.Execute(connection, cancellationToken);
                        conversationId ??= result?.GetValue("conversationId").AsInt32;
                    }
                    catch (MongoException ex)
                    {
                        throw CreateException(connection.ConnectionId, ex, command);
                    }

                    currentStep = Transition(conversation, currentStep, result);
                }
            }
        }

        /// <inheritdoc/>
        public virtual async Task AuthenticateAsync(IConnection connection, ConnectionDescription description, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(connection, nameof(connection));
            Ensure.IsNotNull(description, nameof(description));

            using (var conversation = new SaslConversation(description.ConnectionId))
            {
                ISaslStep currentStep;
                int? conversationId = null;

                if (TryGetSpeculativeFirstStep(description, out currentStep, out var result))
                {
                    currentStep = await TransitionAsync(conversation, currentStep, result, cancellationToken).ConfigureAwait(false);
                    conversationId = result.GetValue("conversationId").AsInt32;
                }
                else
                {
                    currentStep = await _mechanism.InitializeAsync(connection, conversation, description, cancellationToken).ConfigureAwait(false);
                }

                while (currentStep != null)
                {
                    var command = conversationId.HasValue
                        ? CreateContinueCommand(currentStep, conversationId.Value)
                        : CreateStartCommand(currentStep);
                    try
                    {
                        var protocol = CreateCommandProtocol(command);
                        result = await protocol.ExecuteAsync(connection, cancellationToken).ConfigureAwait(false);
                        conversationId ??= result?.GetValue("conversationId").AsInt32;
                    }
                    catch (MongoException ex)
                    {
                        throw CreateException(connection.ConnectionId, ex, command);
                    }

                    currentStep = await TransitionAsync(conversation, currentStep, result, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <inheritdoc/>
        public virtual BsonDocument CustomizeInitialHelloCommand(BsonDocument helloCommand, CancellationToken cancellationToken)
            => helloCommand;

        /// <summary>
        /// Determines whether saslStart should be skipped.
        /// </summary>
        /// <param name="description">The connection description.</param>
        /// <param name="firstStep">The first sasl step.</param>
        /// <param name="result">The result.</param>
        /// <returns>A flag whether saslStart command can be skipped.</returns>
        private protected bool TryGetSpeculativeFirstStep(ConnectionDescription description, out ISaslStep firstStep, out BsonDocument result)
        {
            var speculativeAuthenticateResult = description.HelloResult.SpeculativeAuthenticate;
            if (_speculativeFirstStep != null && speculativeAuthenticateResult != null)
            {
                result = speculativeAuthenticateResult;
                firstStep = _speculativeFirstStep;
                return true;
            }
            else
            {
                result = null;
                firstStep = null;
                return false;
            }
        }

        private protected virtual MongoAuthenticationException CreateException(ConnectionId connectionId, Exception ex, BsonDocument command)
        {
            // Do NOT echo the full command into exception message
            var message = $"Unable to authenticate using sasl protocol mechanism {Name}.";
            return new MongoAuthenticationException(connectionId, message, ex);
        }

        private CommandWireProtocol<BsonDocument> CreateCommandProtocol(BsonDocument command)
            => new CommandWireProtocol<BsonDocument>(
                databaseNamespace: new DatabaseNamespace(DatabaseName),
                command: command,
                secondaryOk: true,
                resultSerializer: BsonDocumentSerializer.Instance,
                messageEncoderSettings: null,
                serverApi: _serverApi);

        private protected virtual BsonDocument CreateStartCommand(ISaslStep currentStep)
            => new BsonDocument
            {
                { SaslStartCommand, 1 },
                { "mechanism", _mechanism.Name },
                { "payload", currentStep.BytesToSendToServer }
            };


        private BsonDocument CreateContinueCommand(ISaslStep currentStep, int conversationId)
            => new BsonDocument
            {
                { SaslContinueCommand, 1 },
                { "conversationId", conversationId },
                { "payload", currentStep.BytesToSendToServer }
            };

        private bool IsCompleted(ISaslStep currentStep, BsonDocument result) => currentStep.IsComplete && result.GetValue("done", false).ToBoolean();

        private ISaslStep Transition(
            SaslConversation conversation,
            ISaslStep currentStep,
            BsonDocument result)
        {
            // we might be done here if the client is not expecting a reply from the server
            if (IsCompleted(currentStep, result))
            {
                return null;
            }

            currentStep = currentStep.Transition(conversation, result["payload"].AsByteArray);

            // we might be done here if the client had some final verification it needed to do
            if (IsCompleted(currentStep, result))
            {
                return null;
            }

            return currentStep;
        }

        private async Task<ISaslStep> TransitionAsync(
            SaslConversation conversation,
            ISaslStep currentStep,
            BsonDocument result,
            CancellationToken cancellationToken)
        {
            // we might be done here if the client is not expecting a reply from the server
            if (IsCompleted(currentStep, result))
            {
                return null;
            }

            currentStep = await currentStep.TransitionAsync(conversation, result["payload"].AsByteArray, cancellationToken).ConfigureAwait(false);

            // we might be done here if the client had some final verification it needed to do
            if (IsCompleted(currentStep, result))
            {
                return null;
            }

            return currentStep;
        }

        /// <summary>
        /// Represents a SASL conversation.
        /// </summary>
        protected internal sealed class SaslConversation : IDisposable
        {
            // fields
            private readonly ConnectionId _connectionId;
            private readonly List<IDisposable> _itemsNeedingDisposal;
            private bool _isDisposed;

            // constructors
            /// <summary>
            /// Initializes a new instance of the <see cref="SaslConversation"/> class.
            /// </summary>
            /// <param name="connectionId">The connection identifier.</param>
            public SaslConversation(ConnectionId connectionId)
            {
                _connectionId = connectionId;
                _itemsNeedingDisposal = new List<IDisposable>();
            }

            // properties
            /// <summary>
            /// Gets the connection identifier.
            /// </summary>
            /// <value>
            /// The connection identifier.
            /// </value>
            public ConnectionId ConnectionId
            {
                get { return _connectionId; }
            }

            /// <summary>
            /// Registers the item for disposal.
            /// </summary>
            /// <param name="item">The disposable item.</param>
            public void RegisterItemForDisposal(IDisposable item)
            {
                Ensure.IsNotNull(item, nameof(item));
                _itemsNeedingDisposal.Add(item);
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                if (!_isDisposed)
                {
                    for (int i = _itemsNeedingDisposal.Count - 1; i >= 0; i--)
                    {
                        _itemsNeedingDisposal[i].Dispose();
                    }

                    _itemsNeedingDisposal.Clear();
                    _isDisposed = true;
                }
            }
        }

        /// <summary>
        /// Represents a SASL mechanism.
        /// </summary>
        protected internal interface ISaslMechanism
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
        /// Represents a SASL step.
        /// </summary>
        protected internal interface ISaslStep
        {
            // properties
            /// <summary>
            /// Gets the bytes to send to server.
            /// </summary>
            /// <value>
            /// The bytes to send to server.
            /// </value>
            byte[] BytesToSendToServer { get; }

            /// <summary>
            /// Gets a value indicating whether this instance is complete.
            /// </summary>
            /// <value>
            /// <c>true</c> if this instance is complete; otherwise, <c>false</c>.
            /// </value>
            bool IsComplete { get; }

            // methods
            /// <summary>
            /// Transitions the SASL conversation to the next step.
            /// </summary>
            /// <param name="conversation">The SASL conversation.</param>
            /// <param name="bytesReceivedFromServer">The bytes received from server.</param>
            /// <returns>The next SASL step.</returns>
            ISaslStep Transition(SaslConversation conversation, byte[] bytesReceivedFromServer);

            /// <summary>
            /// Transitions the SASL conversation to the next step.
            /// </summary>
            /// <param name="conversation">The SASL conversation.</param>
            /// <param name="bytesReceivedFromServer">The bytes received from server.</param>
            /// <param name="cancellationToken">The cancellation token.</param>
            /// <returns>The next SASL step.</returns>
            Task<ISaslStep> TransitionAsync(SaslConversation conversation, byte[] bytesReceivedFromServer, CancellationToken cancellationToken);
        }

        /// <summary>
        /// Represents a completed SASL step.
        /// </summary>
        protected internal class CompletedStep : ISaslStep
        {
            // fields
            private readonly byte[] _bytesToSendToServer;

            // constructors
            /// <summary>
            /// Initializes a new instance of the <see cref="CompletedStep"/> class.
            /// </summary>
            public CompletedStep()
                : this(Array.Empty<byte>())
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
            public byte[] BytesToSendToServer
            {
                get { return _bytesToSendToServer; }
            }

            /// <inheritdoc/>
            public bool IsComplete
            {
                get { return true; }
            }

            /// <inheritdoc/>
            public ISaslStep Transition(SaslConversation conversation, byte[] bytesReceivedFromServer)
            {
                if (bytesReceivedFromServer?.Length > 0)
                {
                    // should not be reached
                    throw new InvalidOperationException("Not all authentication response has been handled.");
                }

                throw new InvalidOperationException("Sasl conversation has completed.");
            }

            /// <inheritdoc/>
            public Task<ISaslStep> TransitionAsync(SaslConversation conversation, byte[] bytesReceivedFromServer, CancellationToken cancellationToken)
                => Task.FromResult(Transition(conversation, bytesReceivedFromServer));
        }
    }
}
