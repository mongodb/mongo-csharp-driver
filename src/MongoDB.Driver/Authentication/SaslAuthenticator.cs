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
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol;

namespace MongoDB.Driver.Authentication
{
    internal sealed class SaslAuthenticator : IAuthenticator
    {
        public static bool TryCreate(
            string mechanism,
            IEnumerable<EndPoint> endPoints,
            MongoIdentity identity,
            MongoIdentityEvidence identityEvidence,
            IEnumerable<KeyValuePair<string, object>> mechanismProperties,
            ServerApi serverApi,
            out IAuthenticator authenticator)
        {
            var context = new SaslContext
            {
                Mechanism = mechanism,
                ClusterEndPoints = endPoints,
                Identity = identity,
                IdentityEvidence = identityEvidence,
                MechanismProperties = mechanismProperties
            };

            if (MongoClientSettings.Extensions.SaslMechanisms.TryCreate(context, out var saslMechanism))
            {
                authenticator = new SaslAuthenticator(saslMechanism, serverApi);
                return true;
            }

            authenticator = null;
            return false;
        }

        public const string SaslStartCommand = "saslStart";
        public const string SaslContinueCommand = "saslContinue";

        private readonly ServerApi _serverApi;
        private ISaslStep _speculativeContinueStep;

        internal SaslAuthenticator(ISaslMechanism mechanism, ServerApi serverApi)
        {
            Mechanism = Ensure.IsNotNull(mechanism, nameof(mechanism));
            _serverApi = serverApi; // can be null
        }

        public ISaslMechanism Mechanism { get; }

        public string Name => Mechanism.Name;

        public void Authenticate(IConnection connection, ConnectionDescription description, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(connection, nameof(connection));
            Ensure.IsNotNull(description, nameof(description));

            using (var conversation = new SaslConversation(description.ConnectionId, connection.EndPoint))
            {
                int? conversationId = null;
                if (TryUseSpeculativeAuthentication(description, out var result, out var currentStep))
                {
                    conversationId = result.GetValue("conversationId").AsInt32;
                }
                else
                {
                    currentStep = Mechanism.Initialize(conversation, description);
                }

                while (currentStep != null)
                {
                    var executionResult = currentStep.Execute(conversation, result?["payload"]?.AsByteArray, cancellationToken);
                    if (executionResult.BytesToSendToServer == null)
                    {
                        currentStep = executionResult.NextStep;
                        continue;
                    }

                    if (executionResult.BytesToSendToServer.Length == 0 && result?["done"] == true)
                    {
                        break;
                    }

                    var command = conversationId.HasValue
                        ? CreateContinueCommand(conversationId.Value, executionResult.BytesToSendToServer)
                        : CreateStartCommand(executionResult.BytesToSendToServer);
                    try
                    {
                        var protocol = CreateCommandProtocol(command);
                        result = protocol.Execute(connection, cancellationToken);
                        conversationId ??= result?.GetValue("conversationId").AsInt32;
                    }
                    catch (MongoException ex)
                    {
                        if (Mechanism.TryHandleAuthenticationException(ex, currentStep, conversation, description, out var nextStep))
                        {
                            currentStep = nextStep;
                            conversationId = null;
                            result = null;
                            continue;
                        }

                        throw CreateException(connection.ConnectionId, ex, command);
                    }

                    currentStep = executionResult.NextStep;
                }

                if (result?["done"] != true)
                {
                    throw new InvalidOperationException("SASL conversation was not completed properly.");
                }
            }
        }

        public async Task AuthenticateAsync(IConnection connection, ConnectionDescription description, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(connection, nameof(connection));
            Ensure.IsNotNull(description, nameof(description));

            using (var conversation = new SaslConversation(description.ConnectionId, connection.EndPoint))
            {
                int? conversationId = null;
                if (TryUseSpeculativeAuthentication(description, out var result, out var currentStep))
                {
                    conversationId = result.GetValue("conversationId").AsInt32;
                }
                else
                {
                    currentStep = Mechanism.Initialize(conversation, description);
                }

                while (currentStep != null)
                {
                    var executionResult = await currentStep.ExecuteAsync(conversation, result?["payload"]?.AsByteArray, cancellationToken).ConfigureAwait(false);
                    if (executionResult.BytesToSendToServer == null)
                    {
                        currentStep = executionResult.NextStep;
                        continue;
                    }

                    if (executionResult.BytesToSendToServer.Length == 0 && result?["done"] == true)
                    {
                        break;
                    }

                    var command = conversationId.HasValue
                        ? CreateContinueCommand(conversationId.Value, executionResult.BytesToSendToServer)
                        : CreateStartCommand(executionResult.BytesToSendToServer);
                    try
                    {
                        var protocol = CreateCommandProtocol(command);
                        result = await protocol.ExecuteAsync(connection, cancellationToken).ConfigureAwait(false);
                        conversationId ??= result?.GetValue("conversationId").AsInt32;
                    }
                    catch (MongoException ex)
                    {
                        if (Mechanism.TryHandleAuthenticationException(ex, currentStep, conversation, description, out var nextStep))
                        {
                            currentStep = nextStep;
                            conversationId = null;
                            result = null;
                            continue;
                        }

                        throw CreateException(connection.ConnectionId, ex, command);
                    }

                    currentStep = executionResult.NextStep;
                }

                if (result?["done"] != true)
                {
                    throw new InvalidOperationException("SASL conversation was not completed properly.");
                }
            }
        }

        public BsonDocument CustomizeInitialHelloCommand(BsonDocument helloCommand, CancellationToken cancellationToken)
        {
            var speculativeStep = Mechanism.CreateSpeculativeAuthenticationStep();
            if (speculativeStep != null)
            {
                (var bytesToSend, _speculativeContinueStep) = speculativeStep.Execute(null, null, cancellationToken);
                var firstCommand = CreateStartCommand(bytesToSend);
                firstCommand.Add("db", Mechanism.DatabaseName);
                helloCommand.Add("speculativeAuthenticate", firstCommand);
            }

            return helloCommand;
        }

        private bool TryUseSpeculativeAuthentication(ConnectionDescription description, out BsonDocument result, out ISaslStep step)
        {
            if (description.IsInitialized())
            {
                result = null;
                step = null;
                return false;
            }

            var speculativeAuthenticateResult = description.HelloResult.SpeculativeAuthenticate;
            if (speculativeAuthenticateResult != null)
            {
                result = speculativeAuthenticateResult;
                step = _speculativeContinueStep;
                return true;
            }

            result = null;
            step = null;
            return false;
        }

        private MongoAuthenticationException CreateException(ConnectionId connectionId, Exception ex, BsonDocument command)
        {
            // Do NOT echo the full command into exception message
            var message = $"Unable to authenticate using sasl protocol mechanism {Mechanism.Name}.";
            return new MongoAuthenticationException(connectionId, message, ex);
        }

        private CommandWireProtocol<BsonDocument> CreateCommandProtocol(BsonDocument command)
            => new CommandWireProtocol<BsonDocument>(
                databaseNamespace: new DatabaseNamespace(Mechanism.DatabaseName),
                command: command,
                secondaryOk: true,
                resultSerializer: BsonDocumentSerializer.Instance,
                messageEncoderSettings: null,
                serverApi: _serverApi);

        private BsonDocument CreateStartCommand(byte[] bytesToSendToServer)
        {
            var startCommand = new BsonDocument
            {
                { SaslStartCommand, 1 },
                { "mechanism",Mechanism.Name },
                { "payload", bytesToSendToServer }
            };
            Mechanism.CustomizeSaslStartCommand(startCommand);
            return startCommand;
        }

        private BsonDocument CreateContinueCommand(int conversationId, byte[] bytesToSendToServer)
            => new BsonDocument
            {
                { SaslContinueCommand, 1 },
                { "conversationId", conversationId },
                { "payload", bytesToSendToServer }
            };
    }
}
