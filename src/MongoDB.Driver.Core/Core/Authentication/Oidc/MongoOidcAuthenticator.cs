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
    internal sealed class OidcAuthenticationContext : IAuthenticationContext
    {
        public OidcAuthenticationContext(EndPoint endPoint, OidcCredentials usedCredentials)
        {
            CurrentEndPoint = Ensure.IsNotNull(endPoint, nameof(endPoint));
            UsedCredentials = usedCredentials;
        }

        public EndPoint CurrentEndPoint { get; }

        public OidcCredentials UsedCredentials { get; }
    }

    internal interface IOidcStepsFactory
    {
        ISaslStep CreateLastSaslStep(OidcCredentials oidcCredentials);
    }

    internal abstract class OidcSaslMechanism : SaslMechanismBase, IOidcStepsFactory, IWithAuthenticationContext
    {
        private readonly EndPoint _endPoint;

        public virtual ISaslStep CreateLastSaslStep(OidcCredentials oidcCredentials)
        {
            if (oidcCredentials == null)
            {
                throw new InvalidOperationException("OIDC credentials have not been provided.");
            }
            return new NoTransitionClientLast(new BsonDocument("jwt", oidcCredentials.AccessToken).ToBson());
        }

        public OidcSaslMechanism(EndPoint endPoint) => _endPoint = endPoint;

        public abstract ICredentialsCache<OidcCredentials> CredentialsCache { get; }

        public override string Name => MongoOidcAuthenticator.MechanismName;

        public abstract OidcCredentials UsedCredentials { get; }

        public IAuthenticationContext AuthenticationContext => new OidcAuthenticationContext(_endPoint, UsedCredentials);

        public abstract ISaslStep CreateSpeculativeAuthenticationSaslStep(CancellationToken cancellationToken);
        public virtual Task<ISaslStep> CreateSpeculativeAuthenticationSaslStepAsync(CancellationToken cancellationToken) => Task.FromResult(CreateSpeculativeAuthenticationSaslStep(cancellationToken));
        public abstract bool ShouldReauthenticateIfSaslError(IConnection connection, Exception ex);
    }

    /// <summary>
    /// The Mongo OIDC authenticator.
    /// </summary>
    internal sealed class MongoOidcAuthenticator : SaslAuthenticator, IWithAuthenticationContext
    {
        #region static
        public const string AllowedHostsMechanismPropertyName = "ALLOWED_HOSTS";
        public const string ProviderMechanismPropertyName = "PROVIDER_NAME";
        public const string MechanismName = "MONGODB-OIDC";
        public const string RequestCallbackMechanismPropertyName = "REQUEST_TOKEN_CALLBACK";
        public const string RefreshCallbackMechanismPropertyName = "REFRESH_TOKEN_CALLBACK";

        /// <summary>
        /// Create OIDC authenticator.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="principalName">The principalName.</param>
        /// <param name="properties">The properties.</param>
        /// <param name="context">The authentication context.</param>
        /// <param name="serverApi">The server API.</param>
        /// <returns>The oidc authenticator.</returns>
        public static MongoOidcAuthenticator CreateAuthenticator(
            string source,
            string principalName,
            IEnumerable<KeyValuePair<string, string>> properties,
            IAuthenticationContext context,
            ServerApi serverApi) =>
            CreateAuthenticator(source, principalName, properties, context, serverApi, ExternalCredentialsAuthenticators.Instance);

        private static MongoOidcAuthenticator CreateAuthenticator(
            string source,
            string principalName,
            IEnumerable<KeyValuePair<string, string>> properties,
            IAuthenticationContext context,
            ServerApi serverApi,
            IExternalCredentialsAuthenticators externalCredentialsAuthenticators) =>
        CreateAuthenticator(
            source,
            principalName,
            properties.Select(pair => new KeyValuePair<string, object>(pair.Key, pair.Value)),
            context,
            serverApi,
            externalCredentialsAuthenticators);

        /// <summary>
        /// Create OIDC authenticator.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="principalName">The principal name.</param>
        /// <param name="properties">The properties.</param>
        /// <param name="context">The authentication context.</param>
        /// <param name="serverApi">The server API.</param>
        /// <returns>The oidc authenticator.</returns>
        public static MongoOidcAuthenticator CreateAuthenticator(
            string source,
            string principalName,
            IEnumerable<KeyValuePair<string, object>> properties,
            IAuthenticationContext context,
            ServerApi serverApi) =>
            CreateAuthenticator(source, principalName, properties, context, serverApi, ExternalCredentialsAuthenticators.Instance);

        internal static MongoOidcAuthenticator CreateAuthenticator(
            string source,
            string principalName,
            IEnumerable<KeyValuePair<string, object>> properties,
            IAuthenticationContext context,
            ServerApi serverApi,
            IExternalCredentialsAuthenticators externalCredentialsAuthenticators)
        {
            Ensure.IsNotNull(context, nameof(context));
            var endPoint = Ensure.IsNotNull(context.CurrentEndPoint, nameof(context.CurrentEndPoint));
            Ensure.IsNotNull(externalCredentialsAuthenticators, nameof(externalCredentialsAuthenticators));

            if (source != "$external")
            {
                throw new ArgumentException("MONGODB-OIDC authentication may only use the $external source.", nameof(source));
            }

            var inputConfiguration = CreateInputConfiguration(endPoint, principalName, properties);

            OidcSaslMechanism mechanism;
            if (inputConfiguration.IsCallbackWorkflow)
            {
                var oidcAuthenticator = externalCredentialsAuthenticators.Oidc;
                var oidsCredentialsProvider = oidcAuthenticator.GetProvider(inputConfiguration);
                mechanism = new MongoOidcCallbackMechanism(inputConfiguration.PrincipalName, oidsCredentialsProvider, context);
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
                mechanism = new MongoOidcProviderMechanism(provider, endPoint);
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

                IEnumerable<string> allowedHostNames = null;
                string providerName = null;
                IOidcRequestCallbackProvider requestCallbackProvider = null;
                IOidcRefreshCallbackProvider refreshCallbackProvider = null;
                foreach (var authorizationProperty in properties)
                {
                    var value = authorizationProperty.Value;
                    switch (authorizationProperty.Key)
                    {
                        case AllowedHostsMechanismPropertyName:
                            {
                                allowedHostNames = value as IEnumerable<string>
                                    ?? throw new InvalidCastException(GetErrorMessage<IEnumerable<string>>(AllowedHostsMechanismPropertyName, value));
                            }
                            break;
                        case RequestCallbackMechanismPropertyName:
                            {
                                requestCallbackProvider = value as IOidcRequestCallbackProvider
                                    ?? throw new InvalidCastException(GetErrorMessage<IOidcRequestCallbackProvider>(RequestCallbackMechanismPropertyName, value));
                            }
                            break;
                        case RefreshCallbackMechanismPropertyName:
                            {
                                refreshCallbackProvider = value as IOidcRefreshCallbackProvider
                                    ?? throw new InvalidCastException(GetErrorMessage<IOidcRefreshCallbackProvider>(RefreshCallbackMechanismPropertyName, value));
                            }
                            break;
                        case ProviderMechanismPropertyName:
                            {
                                providerName = value as string
                                    ?? throw new InvalidCastException(GetErrorMessage<string>(ProviderMechanismPropertyName, value));
                            }
                            break;
                        default: throw new ArgumentException($"Unknown OIDC property '{authorizationProperty.Key}'.", nameof(authorizationProperty));
                    }
                }

                return new OidcInputConfiguration(endpoint, principalName, providerName, requestCallbackProvider, refreshCallbackProvider, allowedHostNames);
            }

            static string GetErrorMessage<TValue>(string propertyName, object value)
            {
                var messageEnd = typeof(TValue) == typeof(string) ? "be string" : $"inherit from {typeof(TValue).Name}";
                return $"The {propertyName} {value?.GetType()} must {messageEnd}.";
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

        public IAuthenticationContext AuthenticationContext => _mechanism.AuthenticationContext; 

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
