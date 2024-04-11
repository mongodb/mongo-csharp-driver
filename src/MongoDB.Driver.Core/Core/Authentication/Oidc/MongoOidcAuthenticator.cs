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
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Authentication.Oidc
{
    internal sealed class MongoOidcAuthenticator : SaslAuthenticator
    {
        #region static
        public const string MechanismName = "MONGODB-OIDC";

        public static MongoOidcAuthenticator CreateAuthenticator(
            string source,
            string principalName,
            IEnumerable<KeyValuePair<string, object>> properties,
            IReadOnlyList<EndPoint> endPoints,
            ServerApi serverApi)
            => CreateAuthenticator(
                source,
                principalName,
                properties,
                endPoints,
                serverApi,
                OidcCallbackAdapterCachingFactory.Instance);

        public static MongoOidcAuthenticator CreateAuthenticator(
            string source,
            string principalName,
            IEnumerable<KeyValuePair<string, object>> properties,
            IReadOnlyList<EndPoint> endPoints,
            ServerApi serverApi,
            IOidcCallbackAdapterFactory callbackAdapterFactory)
        {
            Ensure.IsNotNull(endPoints, nameof(endPoints));
            Ensure.IsNotNull(callbackAdapterFactory, nameof(callbackAdapterFactory));

            if (source != "$external")
            {
                throw new ArgumentException("MONGODB-OIDC authentication must use the $external authentication source.", nameof(source));
            }

            var configuration = new OidcConfiguration(endPoints, principalName, properties);
            var callbackAdapter = callbackAdapterFactory.Get(configuration);
            var mechanism = new OidcSaslMechanism(callbackAdapter, principalName);
            return new MongoOidcAuthenticator(mechanism, serverApi, configuration);
        }
        #endregion

        private MongoOidcAuthenticator(
            OidcSaslMechanism mechanism,
            ServerApi serverApi,
            OidcConfiguration configuration)
            : base(mechanism, serverApi)
        {
            OidcMechanism = mechanism;
            Configuration = configuration;
        }

        public override string DatabaseName => "$external";

        public OidcConfiguration Configuration { get; }

        private OidcSaslMechanism OidcMechanism { get; }

        public override void Authenticate(IConnection connection, ConnectionDescription description, CancellationToken cancellationToken)
        {
            // Capture the cache state to decide if we want retry on auth error or not.
            // Not the best solution, but let us not to introduce the retry logic into SaslAuthenticator to reduce affected areas for now.
            // Consider to move this code into SaslAuthenticator when retry logic will be applicable not only for Oidc Auth.
            var allowRetryOnAuthError = OidcMechanism.HasCachedCredentials;
            TryAuthenticate(allowRetryOnAuthError);

            void TryAuthenticate(bool retryOnFailure)
            {
                try
                {
                    base.Authenticate(connection, description, cancellationToken);
                }
                catch (Exception ex)
                {
                    ClearCredentialsCache();

                    if (retryOnFailure && ShouldReauthenticateIfSaslError(ex, connection))
                    {
                        Thread.Sleep(100);
                        TryAuthenticate(false);
                    }
                    else
                    {
                        throw UnwrapMongoAuthenticationException(ex);
                    }
                }
            }
        }

        public override Task AuthenticateAsync(IConnection connection, ConnectionDescription description, CancellationToken cancellationToken)
        {
            // Capture the cache state to decide if we want retry on auth error or not.
            // Not the best solution, but let us not to introduce the retry logic into SaslAuthenticator to reduce affected areas for now.
            // Consider to move this code into SaslAuthenticator when retry logic will be applicable not only for Oidc Auth.
            var allowRetryOnAuthError = OidcMechanism.HasCachedCredentials;
            return TryAuthenticateAsync(allowRetryOnAuthError);

            async Task TryAuthenticateAsync(bool retryOnFailure)
            {
                try
                {
                    await base.AuthenticateAsync(connection, description, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    ClearCredentialsCache();

                    if (retryOnFailure && ShouldReauthenticateIfSaslError(ex, connection))
                    {
                        await Task.Delay(100).ConfigureAwait(false);
                        await TryAuthenticateAsync(false).ConfigureAwait(false);
                    }
                    else
                    {
                        throw UnwrapMongoAuthenticationException(ex);
                    }
                }
            }
        }

        public override BsonDocument CustomizeInitialHelloCommand(BsonDocument helloCommand, CancellationToken cancellationToken)
        {
            var speculativeFirstStep = OidcMechanism.CreateSpeculativeAuthenticationStep(cancellationToken);
            if (speculativeFirstStep != null)
            {
                _speculativeFirstStep = speculativeFirstStep;
                var firstCommand = CreateStartCommand(speculativeFirstStep);
                firstCommand.Add("db", DatabaseName);
                helloCommand.Add("speculativeAuthenticate", firstCommand);
            }

            return helloCommand;
        }

        public void ClearCredentialsCache() => OidcMechanism.ClearCache();

        private static bool ShouldReauthenticateIfSaslError(Exception ex, IConnection connection)
        {
            return ex is MongoAuthenticationException authenticationException &&
                   authenticationException.InnerException is MongoCommandException mongoCommandException &&
                   mongoCommandException.Code == (int)ServerErrorCode.AuthenticationFailed &&
                   !connection.Description.IsInitialized();
        }

        private static Exception UnwrapMongoAuthenticationException(Exception ex)
        {
            if (ex is MongoAuthenticationException mongoAuthenticationException &&
                mongoAuthenticationException.InnerException != null)
            {
                return mongoAuthenticationException.InnerException;
            }

            return ex;
        }
    }
}
