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
using MongoDB.Bson;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Authentication.Oidc
{
    internal sealed class OidcSaslMechanism : ISaslMechanism
    {
        public const string MechanismName = "MONGODB-OIDC";

        public static OidcSaslMechanism Create(SaslContext context)
            => Create(context, SystemClock.Instance, EnvironmentVariableProvider.Instance);

        public static OidcSaslMechanism Create(SaslContext context, IClock clock, IEnvironmentVariableProvider environmentVariableProvider)
        {
            Ensure.IsNotNull(clock, nameof(clock));
            Ensure.IsNotNull(environmentVariableProvider, nameof(environmentVariableProvider));

            var callbackAdapterFactory = OidcCallbackAdapterCachingFactory.Instance;
            if (clock != SystemClock.Instance || environmentVariableProvider != EnvironmentVariableProvider.Instance)
            {
                callbackAdapterFactory = new OidcCallbackAdapterCachingFactory(clock, environmentVariableProvider);
            }

            return Create(context, callbackAdapterFactory);
        }

        public static OidcSaslMechanism Create(
            SaslContext context,
            IOidcCallbackAdapterFactory callbackAdapterFactory)
        {
            Ensure.IsNotNull(context, nameof(context));
            Ensure.IsNotNull(callbackAdapterFactory, nameof(callbackAdapterFactory));
            if (context.Mechanism != MechanismName)
            {
                throw new InvalidOperationException($"Unexpected authentication mechanism: {context.Mechanism}");
            }

            if (context.Identity.Source != "$external")
            {
                throw new ArgumentException("MONGODB-OIDC authentication must use the $external authentication source.", nameof(context.Identity.Source));
            }

            if (context.IdentityEvidence is PasswordEvidence)
            {
                throw new NotSupportedException("OIDC authenticator cannot be constructed with password.");
            }

            var configuration = new OidcConfiguration(context.ClusterEndPoints, context.Identity.Username, context.MechanismProperties);
            var callbackAdapter = callbackAdapterFactory.Get(configuration);
            return new OidcSaslMechanism(callbackAdapter, configuration);
        }

        private readonly IOidcCallbackAdapter _oidcCallback;

        private OidcSaslMechanism(IOidcCallbackAdapter oidcCallback, OidcConfiguration configuration)
        {
            _oidcCallback = oidcCallback;
            Configuration = configuration;
        }

        public OidcConfiguration Configuration { get; }

        public string DatabaseName => "$external";

        public string Name => MechanismName;

        public ISaslStep CreateSpeculativeAuthenticationStep()
        {
            var oidcCredentials = _oidcCallback.CachedCredentials;

            if (oidcCredentials?.IsExpired != false)
            {
                return null;
            }

            return new OidcCachedCredentialsSaslStep(oidcCredentials);
        }

        public BsonDocument CustomizeSaslStartCommand(BsonDocument startCommand) => startCommand;

        public void Dispose()
        {
            _oidcCallback?.Dispose();
        }

        public ISaslStep Initialize(SaslConversation conversation, ConnectionDescription description)
        {
            var cachedCredentials = _oidcCallback.CachedCredentials;

            if (cachedCredentials != null)
            {
                return new OidcCachedCredentialsSaslStep(cachedCredentials);
            }

            return new OidcObtainCredentialsSaslStep(_oidcCallback, Configuration.PrincipalName);
        }

        public void OnReAuthenticationRequired()
            => _oidcCallback.InvalidateCachedCredentials();

        public bool TryHandleAuthenticationException(
            MongoException exception,
            ISaslStep step,
            SaslConversation conversation,
            ConnectionDescription description,
            out ISaslStep nextStep)
        {
            nextStep = null;
            if (!IsAuthenticationError(exception))
            {
                return false;
            }

            var oidcStep = (OidcSaslStep)step;
            _oidcCallback.InvalidateCachedCredentials(oidcStep.UsedCredentials);

            if (step is OidcCachedCredentialsSaslStep)
            {
                nextStep = new OidcObtainCredentialsSaslStep(_oidcCallback, Configuration.PrincipalName);
                return true;
            }

            return false;
        }

        private static bool IsAuthenticationError(MongoException ex)
            => ex is MongoCommandException mongoCommandException &&
               mongoCommandException.Code == (int)ServerErrorCode.AuthenticationFailed;
    }
}
