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
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver.Core.Authentication.Oidc
{
#pragma warning disable CS0618 // Type or member is obsolete
    internal sealed class OidcSaslMechanism : SaslAuthenticator.ISaslMechanism
#pragma warning restore CS0618 // Type or member is obsolete
    {
        private readonly IOidcCallbackAdapter _oidcCallback;
        private readonly string _principalName;
        private OidcCredentials _usedCredentials;

        public OidcSaslMechanism(IOidcCallbackAdapter oidcCallback, string principalName)
        {
            _oidcCallback = oidcCallback;
            _principalName = principalName;
        }

        public string Name => MongoOidcAuthenticator.MechanismName;

        public bool HasCachedCredentials => _oidcCallback.CachedCredentials != null;

#pragma warning disable CS0618 // Type or member is obsolete
        public SaslAuthenticator.ISaslStep Initialize(
            IConnection connection,
            SaslAuthenticator.SaslConversation conversation,
            ConnectionDescription description,
            CancellationToken cancellationToken)
        {
            var credentials = _oidcCallback.GetCredentials(new OidcCallbackParameters(1, _principalName), cancellationToken);
            return CreateNoTransitionClientLastSaslStep(credentials);
        }

        public async Task<SaslAuthenticator.ISaslStep> InitializeAsync(
            IConnection connection,
            SaslAuthenticator.SaslConversation conversation,
            ConnectionDescription description,
            CancellationToken cancellationToken)
        {
            var credentials = await _oidcCallback.GetCredentialsAsync(new OidcCallbackParameters(1, _principalName), cancellationToken);
            return CreateNoTransitionClientLastSaslStep(credentials);
        }

        public SaslAuthenticator.ISaslStep CreateSpeculativeAuthenticationStep(CancellationToken cancellationToken)
        {
            var cachedCredentials = _oidcCallback.CachedCredentials;
            if (cachedCredentials == null)
            {
                return null;
            }

            return CreateNoTransitionClientLastSaslStep(cachedCredentials);
        }

        public void ClearCache() => _oidcCallback.InvalidateCachedCredentials(_usedCredentials);

        private SaslAuthenticator.ISaslStep CreateNoTransitionClientLastSaslStep(OidcCredentials oidcCredentials)
        {
            if (oidcCredentials == null)
            {
                throw new InvalidOperationException("OIDC credentials have not been provided.");
            }

            _usedCredentials = oidcCredentials;
            return new NoTransitionClientLastSaslStep(new BsonDocument("jwt", oidcCredentials.AccessToken).ToBson());
        }
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
