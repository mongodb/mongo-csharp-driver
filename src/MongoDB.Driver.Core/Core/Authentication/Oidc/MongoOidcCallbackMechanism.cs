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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Authentication.External;
using MongoDB.Driver.Core.Authentication.Sasl;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;

namespace MongoDB.Driver.Core.Authentication.Oidc
{
    internal sealed class MongoOidcCallbackMechanism : OidcSaslMechanism
    {
        private readonly IOidcExternalAuthenticationCredentialsProvider _oidsCredentialsProvider;
        private readonly string _principalName;
        private OidcCredentials _usedCredentials;

        public MongoOidcCallbackMechanism(
            string principalName,
            IOidcExternalAuthenticationCredentialsProvider oidsCredentialsProvider,
            IAuthenticationContext context) : base(context.CurrentEndPoint)
        {
            _oidsCredentialsProvider = Ensure.IsNotNull(oidsCredentialsProvider, nameof(oidsCredentialsProvider));
            _principalName = principalName; // can be null

            // represents the last used credentials from previous Authenticate calls
            _usedCredentials = (context as OidcAuthenticationContext)?.UsedCredentials; // can be null. 
        }

        public override ICredentialsCache<OidcCredentials> CredentialsCache => _oidsCredentialsProvider;

        public override OidcCredentials UsedCredentials => _usedCredentials;

        public override bool ShouldReauthenticateIfSaslError(IConnection connection, Exception ex) =>
            ex is MongoAuthenticationException mongoAuthenticationException && // consider only server errors
            mongoAuthenticationException.AllowReauthentication &&
            RetryabilityHelper.IsReauthenticationRequested(mongoAuthenticationException);

        public override ISaslStep Initialize(IConnection connection, SaslConversation conversation, ConnectionDescription description, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(connection, nameof(connection));
            Ensure.IsNotNull(conversation, nameof(conversation));
            Ensure.IsNotNull(description, nameof(description));

            var stepResult = CreateSaslStartOrGetServerResponse(connection);
            if (stepResult.SaslStep != null)
            {
                return stepResult.SaslStep;
            }
            else
            {
                var oidcCredentials = _oidsCredentialsProvider.CreateCredentialsFromExternalSource(stepResult.InvalidCredentials, stepResult.IdpServerInfo, cancellationToken);
                return CreateLastSaslStep(oidcCredentials);
            }
        }

        public override async Task<ISaslStep> InitializeAsync(IConnection connection, SaslConversation conversation, ConnectionDescription description, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(connection, nameof(connection));
            Ensure.IsNotNull(conversation, nameof(conversation));
            Ensure.IsNotNull(description, nameof(description));

            var stepResult = CreateSaslStartOrGetServerResponse(connection);
            if (stepResult.SaslStep != null)
            {
                return stepResult.SaslStep;
            }
            else
            {
                var oidcCredentials = await _oidsCredentialsProvider.CreateCredentialsFromExternalSourceAsync(stepResult.InvalidCredentials, stepResult.IdpServerInfo, cancellationToken).ConfigureAwait(false);
                return CreateLastSaslStep(oidcCredentials);
            }
        }

        public override ISaslStep CreateSpeculativeAuthenticationSaslStep(CancellationToken cancellationToken) => CreateSaslStartOrGetServerResponse(connection: null).SaslStep;

        public override ISaslStep CreateLastSaslStep(OidcCredentials oidcCredentials)
        {
            _usedCredentials = oidcCredentials;  // track actually used credentials
            return base.CreateLastSaslStep(oidcCredentials);
        }

        // private methods
        private (ISaslStep SaslStep, BsonDocument IdpServerInfo, OidcCredentials InvalidCredentials) CreateSaslStartOrGetServerResponse(IConnection connection)
        {
            var cachedCredentials = _oidsCredentialsProvider.CachedCredentials;
            if (cachedCredentials != null)
            {
                var reauthenticationRequested = (connection?.IsInitialized).GetValueOrDefault();
                if (cachedCredentials.ShouldBeRefreshed || reauthenticationRequested)
                {
                    return (
                        SaslStep: null,
                        cachedCredentials.IdpServerInfo,
                        InvalidCredentials: reauthenticationRequested
                            ? _usedCredentials // force expiring used credentials for reauthentication, do nothing in the rest of cases
                            : null);
                }
                else
                {
                    var saslStep = CreateLastSaslStep(cachedCredentials);
                    return (SaslStep: saslStep, IdpServerInfo: null, InvalidCredentials: null);
                }
            }
            else
            {
                var document = new BsonDocument
                {
                    { "n", _principalName, _principalName != null }
                };

                var clientMessageBytes = document.ToBson();

                var saslStep = new CallbackWorkflowClientFirst(clientMessageBytes, _oidsCredentialsProvider, this);
                return (SaslStep: saslStep, IdpServerInfo: null, InvalidCredentials: null);
            }
        }

        // nested types
        private sealed class CallbackWorkflowClientFirst : SaslStepBase
        {
            private readonly byte[] _bytesToSendToServer;
            private readonly IOidcExternalAuthenticationCredentialsProvider _oidsCredentialsProvider;
            private readonly IOidcStepsFactory _oidcSaslStepFactory;

            public CallbackWorkflowClientFirst(
                byte[] bytesToSendToServer,
                IOidcExternalAuthenticationCredentialsProvider oidsCredentialsProvider,
                IOidcStepsFactory oidcSaslStepFactory)
            {
                _bytesToSendToServer = Ensure.IsNotNull(bytesToSendToServer, nameof(bytesToSendToServer));
                _oidsCredentialsProvider = Ensure.IsNotNull(oidsCredentialsProvider, nameof(oidsCredentialsProvider));
                _oidcSaslStepFactory = Ensure.IsNotNull(oidcSaslStepFactory, nameof(oidcSaslStepFactory));
            }

            public override byte[] BytesToSendToServer => _bytesToSendToServer;

            public override bool IsComplete => false;

            public override ISaslStep Transition(SaslConversation conversation, byte[] bytesReceivedFromServer, CancellationToken cancellationToken)
            {
                var serverFirstMessageDocument = BsonSerializer.Deserialize<BsonDocument>(bytesReceivedFromServer);
                var oidcCredentials = _oidsCredentialsProvider.CreateCredentialsFromExternalSource(invalidCredentials: null, serverFirstMessageDocument, cancellationToken);
                return _oidcSaslStepFactory.CreateLastSaslStep(oidcCredentials);
            }

            public override async Task<ISaslStep> TransitionAsync(SaslConversation conversation, byte[] bytesReceivedFromServer, CancellationToken cancellationToken)
            {
                var serverFirstMessageDocument = BsonSerializer.Deserialize<BsonDocument>(bytesReceivedFromServer);
                var oidcCredentials = await _oidsCredentialsProvider.CreateCredentialsFromExternalSourceAsync(invalidCredentials: null, serverFirstMessageDocument, cancellationToken).ConfigureAwait(false);
                return _oidcSaslStepFactory.CreateLastSaslStep(oidcCredentials);
            }
        }
    }
}
