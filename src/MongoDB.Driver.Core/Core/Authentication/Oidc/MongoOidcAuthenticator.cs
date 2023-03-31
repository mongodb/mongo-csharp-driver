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
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Authentication.External;
using MongoDB.Driver.Core.Authentication.Sasl;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Authentication.Oidc
{
    internal abstract class OidcSaslMechanism : SaslMechanismBase
    {
        public static ISaslStep CreateLastSaslStep(IExternalCredentials oidcCredentials)
        {
            if (oidcCredentials == null)
            {
                throw new InvalidOperationException("OIDC credentials have not been provided.");
            }
            return new NoTransitionClientLast(new BsonDocument("jwt", oidcCredentials.AccessToken).ToBson());
        }

        public abstract ICredentialsCache<OidcCredentials> CredentialsCache { get; }

        public override string Name => MongoOidcAuthenticator.MechanismName;

        public abstract ISaslStep CreateSpeculativeAuthenticationSaslStep(CancellationToken cancellationToken);
        public virtual Task<ISaslStep> CreateSpeculativeAuthenticationSaslStepAsync(CancellationToken cancellationToken) => Task.FromResult(CreateSpeculativeAuthenticationSaslStep(cancellationToken));
        public abstract bool ShouldReauthenticateIfSaslError(IConnection connection, Exception ex);
    }

    /// <summary>
    /// The Mongo OIDC authenticator.
    /// </summary>
    internal sealed class MongoOidcAuthenticator : SaslAuthenticator
    {
        #region static
        /// <summary>
        /// Provider name mechanism authorization property.
        /// </summary>
        public const string ProviderName = "PROVIDER_NAME";
        /// <summary>
        /// Mechanism name authorization property.
        /// </summary>
        public const string MechanismName = "MONGODB-OIDC";
        /// <summary>
        /// Request callback mechanism authorization property.
        /// </summary>
        public const string RequestCallbackName = "REQUEST_TOKEN_CALLBACK";
        /// <summary>
        /// Refresh callback mechanism authorization property.
        /// </summary>
        public const string RefreshCallbackName = "REFRESH_TOKEN_CALLBACK";

        /// <summary>
        /// Create OIDC authenticator.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="principalName">The principalName.</param>
        /// <param name="properties">The properties.</param>
        /// <param name="endPoint">The endpoint.</param>
        /// <param name="serverApi">The server API.</param>
        /// <returns>The oidc authenticator.</returns>
        public static MongoOidcAuthenticator CreateAuthenticator(
            string source,
            string principalName,
            IEnumerable<KeyValuePair<string, string>> properties,
            EndPoint endPoint,
            ServerApi serverApi) =>
            CreateAuthenticator(source, principalName, properties, endPoint, serverApi, ExternalCredentialsAuthenticators.Instance);

        internal static MongoOidcAuthenticator CreateAuthenticator(
            string source,
            string principalName,
            IEnumerable<KeyValuePair<string, string>> properties,
            EndPoint endpoint,
            ServerApi serverApi,
            IExternalCredentialsAuthenticators externalCredentialsAuthenticators) =>
        CreateAuthenticator(
            source,
            principalName,
            properties.Select(pair => new KeyValuePair<string, object>(pair.Key, pair.Value)),
            endpoint,
            serverApi,
            externalCredentialsAuthenticators);

        /// <summary>
        /// Create OIDC authenticator.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="principalName">The principal name.</param>
        /// <param name="properties">The properties.</param>
        /// <param name="endpoint">The current endpoint.</param>
        /// <param name="serverApi">The server API.</param>
        /// <returns>The oidc authenticator.</returns>
        public static MongoOidcAuthenticator CreateAuthenticator(
            string source,
            string principalName,
            IEnumerable<KeyValuePair<string, object>> properties,
            EndPoint endpoint,
            ServerApi serverApi) =>
            CreateAuthenticator(source, principalName, properties, endpoint, serverApi, ExternalCredentialsAuthenticators.Instance);

        internal static MongoOidcAuthenticator CreateAuthenticator(
            string source,
            string principalName,
            IEnumerable<KeyValuePair<string, object>> properties,
            EndPoint endpoint,
            ServerApi serverApi,
            IExternalCredentialsAuthenticators externalCredentialsAuthenticators)
        {
            Ensure.IsNotNull(endpoint, nameof(endpoint));
            Ensure.IsNotNull(externalCredentialsAuthenticators, nameof(externalCredentialsAuthenticators));

            if (source != "$external")
            {
                throw new ArgumentException("MONGODB-OIDC authentication may only use the $external source.", nameof(source));
            }

            var inputConfiguration = CreateInputConfiguration(endpoint, principalName, properties);

            OidcSaslMechanism mechanism;
            if (inputConfiguration.IsCallbackWorkflow)
            {
                var oidsCredentialsProvider = externalCredentialsAuthenticators.Oidc.GetProvider(inputConfiguration);
                mechanism = new MongoOidcCallbackMechanism(inputConfiguration.PrincipalName, oidsCredentialsProvider);
            }
            else
            {
                var providerName = Ensure.IsNotNull(inputConfiguration.ProviderName, nameof(inputConfiguration.ProviderName));
                IExternalAuthenticationCredentialsProvider<OidcCredentials> provider = providerName switch
                {
                    "aws" => new OidcAuthenticationCredentialsProviderAdapter<OidcCredentials>(externalCredentialsAuthenticators.AwsForOidc),
                    "azure" => new OidcAuthenticationCredentialsProviderAdapter<AzureCredentials>(externalCredentialsAuthenticators.Azure),
                    "gcp" => new OidcAuthenticationCredentialsProviderAdapter<GcpCredentials>(externalCredentialsAuthenticators.Gcp),
                    _ => throw new NotSupportedException($"Not supported provider name: {providerName} for OIDC authentication.")
                };
                mechanism = new MongoOidcProviderMechanism(provider);
            }
            return new MongoOidcAuthenticator(mechanism, serverApi);

            static OidcInputConfiguration CreateInputConfiguration(
                EndPoint endpoint,
                string principalName,
                IEnumerable<KeyValuePair<string, object>> properties)
            {
                if (properties == null)
                {
                    return new OidcInputConfiguration(endpoint, principalName);
                }

                string providerName = null;
                IRequestCallbackProvider requestCallbackProvider = null;
                IRefreshCallbackProvider refreshCallbackProvider = null;
                foreach (var authorizationProperty in properties)
                {
                    var value = authorizationProperty.Value;
                    switch (authorizationProperty.Key)
                    {
                        case RequestCallbackName:
                            {
                                requestCallbackProvider = value is IRequestCallbackProvider requestProvider
                                    ? requestProvider
                                    : throw new InvalidCastException($"The OIDC request callback must be inherited from {nameof(IRequestCallbackProvider)}, but was {value.GetType().FullName}.");
                            }
                            break;
                        case RefreshCallbackName:
                            {
                                refreshCallbackProvider = value is IRefreshCallbackProvider refreshProvider
                                    ? refreshProvider
                                    : throw new InvalidCastException($"The OIDC refresh callback must be inherited from {nameof(IRefreshCallbackProvider)}, but was {value.GetType().FullName}.");
                            }
                            break;
                        case ProviderName: providerName = value.ToString(); break;
                        default: throw new ArgumentException($"Unknown OIDC property '{authorizationProperty.Key}'.", nameof(authorizationProperty));
                    }
                }

                return new OidcInputConfiguration(endpoint, principalName, providerName, requestCallbackProvider, refreshCallbackProvider);
            }
        }
        #endregion

        private readonly new OidcSaslMechanism _mechanism;

        private MongoOidcAuthenticator(
            OidcSaslMechanism mechanism,
            ServerApi serverApi)
            : base(mechanism, serverApi)
        {
            _mechanism = Ensure.IsNotNull(mechanism, nameof(mechanism));
        }

        /// <summary>
        /// The database name.
        /// </summary>
        public override string DatabaseName => "$external";

        /// <inheritdoc/>
        public override void Authenticate(IConnection connection, ConnectionDescription description, CancellationToken cancellationToken)
        {
            try
            {
                base.Authenticate(connection, description, cancellationToken);
            }
            catch (Exception ex)
            {
                ClearCredentials();
                if (_mechanism.ShouldReauthenticateIfSaslError(connection, ex))
                {
                    base.Authenticate(connection, description, cancellationToken);
                }
                else
                {
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public override async Task AuthenticateAsync(IConnection connection, ConnectionDescription description, CancellationToken cancellationToken)
        {
            try
            {
                await base.AuthenticateAsync(connection, description, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ClearCredentials();
                if (_mechanism.ShouldReauthenticateIfSaslError(connection, ex))
                {
                    await base.AuthenticateAsync(connection, description, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public override BsonDocument CustomizeInitialHelloCommand(BsonDocument helloCommand, CancellationToken cancellationToken)
        {
            _speculativeFirstStep = _mechanism.CreateSpeculativeAuthenticationSaslStep(cancellationToken);
            if (_speculativeFirstStep != null)
            {
                var firstCommand = CreateStartCommand(_speculativeFirstStep);
                firstCommand.Add("db", DatabaseName);
                helloCommand.Add("speculativeAuthenticate", firstCommand);
            }
            return helloCommand;
        }

        // protected methods
        private protected override MongoAuthenticationException CreateException(ConnectionId connectionId, Exception ex, BsonDocument command)
        {
            var originalException = base.CreateException(connectionId, ex, command);
            if (_mechanism is MongoOidcCallbackMechanism)
            {
                var payload = BsonSerializer.Deserialize<BsonDocument>(command["payload"].AsByteArray);
                // if no jwt, then cached credentials are not involved
                var allowReauthenticationAfterError = payload.Contains("jwt");
                return new MongoAuthenticationException(connectionId, originalException.Message, ex, allowReauthenticationAfterError);
            }
            else
            {
                return originalException;
            }
        }

        // private methods
        private void ClearCredentials() => _mechanism?.CredentialsCache.Clear();
    }
}
