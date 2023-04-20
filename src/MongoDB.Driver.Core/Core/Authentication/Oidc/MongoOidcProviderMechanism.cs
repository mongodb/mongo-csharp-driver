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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Authentication.External;
using MongoDB.Driver.Core.Authentication.Sasl;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Authentication.Oidc
{
    internal sealed class MongoOidcProviderMechanism : OidcSaslMechanism
    {
        private readonly ICredentialsCache<OidcCredentials> _credentialsCache;
        private readonly IExternalAuthenticationCredentialsProvider<OidcCredentials> _providerWorkflowCredentialsProvider;

        public MongoOidcProviderMechanism(
            IExternalAuthenticationCredentialsProvider<OidcCredentials> providerWorkflowCredentialsProvider,
            EndPoint endPoint) : base(endPoint)
        {
            _providerWorkflowCredentialsProvider = Ensure.IsNotNull(providerWorkflowCredentialsProvider, nameof(providerWorkflowCredentialsProvider));
            _credentialsCache = _providerWorkflowCredentialsProvider as ICredentialsCache<OidcCredentials>;
        }

        public override ICredentialsCache<OidcCredentials> CredentialsCache => _credentialsCache;

        public override OidcCredentials UsedCredentials => null; /* only for callbacks workflow */

        public override bool ShouldReauthenticateIfSaslError(IConnection connection, Exception ex) => false; /* only for callbacks workflow */

        public override ISaslStep CreateSpeculativeAuthenticationSaslStep(CancellationToken cancellationToken) => Initialize(null, null, null, cancellationToken);
        public override Task<ISaslStep> CreateSpeculativeAuthenticationSaslStepAsync(CancellationToken cancellationToken) => InitializeAsync(null, null, null, cancellationToken);

        public override ISaslStep Initialize(IConnection connection, SaslConversation conversation, ConnectionDescription description, CancellationToken cancellationToken)
        {
            // only saslStart is supported with provider workflow
            var oidcCredentials = _providerWorkflowCredentialsProvider.CreateCredentialsFromExternalSource(cancellationToken);
            return CreateLastSaslStep(oidcCredentials);
        }

        public override async Task<ISaslStep> InitializeAsync(IConnection connection, SaslConversation conversation, ConnectionDescription description, CancellationToken cancellationToken)
        {
            // only saslStart is supported with provider workflow
            var oidcCredentials = await _providerWorkflowCredentialsProvider.CreateCredentialsFromExternalSourceAsync(cancellationToken).ConfigureAwait(false);
            return CreateLastSaslStep(oidcCredentials);
        }
    }
}
