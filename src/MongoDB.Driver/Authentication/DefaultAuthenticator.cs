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
using MongoDB.Driver.Authentication.ScramSha;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Authentication
{
    /// <summary>
    /// The default authenticator.
    /// If saslSupportedMechs is not present in the hello or legacy hello results for mechanism negotiation uses SCRAM-SHA-1.
    /// Else, uses SCRAM-SHA-256 if present in the list of mechanisms. Otherwise, uses
    /// SCRAM-SHA-1 the default, regardless of whether SCRAM-SHA-1 is in the list.
    /// </summary>
    internal sealed class DefaultAuthenticator : IAuthenticator
    {
        private readonly IEnumerable<EndPoint> _endPoints;
        private readonly MongoIdentity _identity;
        private readonly MongoIdentityEvidence _identityEvidence;
        private readonly ServerApi _serverApi;
        private IAuthenticator _speculativeAuthenticator;

        internal DefaultAuthenticator(
            MongoIdentity identity,
            MongoIdentityEvidence identityEvidence,
            IEnumerable<EndPoint> endPoints,
            ServerApi serverApi)
        {
            _endPoints = endPoints;
            _identity = Ensure.IsNotNull(identity, nameof(identity));
            _identityEvidence = identityEvidence;
            _serverApi = serverApi;
        }

        public string Name => "DEFAULT";

        public void Authenticate(IConnection connection, ConnectionDescription description, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(connection, nameof(connection));
            Ensure.IsNotNull(description, nameof(description));

            // If we don't have SaslSupportedMechs as part of the response, that means we didn't piggyback the initial
            // hello or legacy hello request and should query the server (provided that the server >= 4.0), merging results into
            // a new ConnectionDescription
            if (!description.HelloResult.HasSaslSupportedMechs
                && Feature.ScramSha256Authentication.IsSupported(description.MaxWireVersion))
            {
                var command = CustomizeInitialHelloCommand(HelloHelper.CreateCommand(_serverApi, loadBalanced: connection.Settings.LoadBalanced), cancellationToken);
                var helloProtocol = HelloHelper.CreateProtocol(command, _serverApi);
                var helloResult = HelloHelper.GetResult(connection, helloProtocol, cancellationToken);
                var mergedHelloResult = new HelloResult(description.HelloResult.Wrapped.Merge(helloResult.Wrapped));
                description = new ConnectionDescription(
                    description.ConnectionId,
                    mergedHelloResult);
            }

            var authenticator = GetOrCreateAuthenticator(connection, description);
            authenticator.Authenticate(connection, description, cancellationToken);
        }

        public async Task AuthenticateAsync(IConnection connection, ConnectionDescription description, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(connection, nameof(connection));
            Ensure.IsNotNull(description, nameof(description));

            // If we don't have SaslSupportedMechs as part of the response, that means we didn't piggyback the initial
            // hello or legacy hello request and should query the server (provided that the server >= 4.0), merging results into
            // a new ConnectionDescription
            if (!description.HelloResult.HasSaslSupportedMechs
                && Feature.ScramSha256Authentication.IsSupported(description.MaxWireVersion))
            {
                var command = CustomizeInitialHelloCommand(HelloHelper.CreateCommand(_serverApi, loadBalanced: connection.Settings.LoadBalanced), cancellationToken);
                var helloProtocol = HelloHelper.CreateProtocol(command, _serverApi);
                var helloResult = await HelloHelper.GetResultAsync(connection, helloProtocol, cancellationToken).ConfigureAwait(false);
                var mergedHelloResult = new HelloResult(description.HelloResult.Wrapped.Merge(helloResult.Wrapped));
                description = new ConnectionDescription(
                    description.ConnectionId,
                    mergedHelloResult);
            }

            var authenticator = GetOrCreateAuthenticator(connection, description);
            await authenticator.AuthenticateAsync(connection, description, cancellationToken).ConfigureAwait(false);
        }

        public BsonDocument CustomizeInitialHelloCommand(BsonDocument helloCommand, CancellationToken cancellationToken)
        {
            var saslSupportedMechs = CreateSaslSupportedMechsRequest(_identity);
            helloCommand = helloCommand.Merge(saslSupportedMechs);
            if (SaslAuthenticator.TryCreate(
                ScramShaSaslMechanism.ScramSha256MechanismName,
                _endPoints,
                _identity,
                _identityEvidence,
                null,
                _serverApi,
                out var authenticator))
            {
                _speculativeAuthenticator = authenticator;
                return _speculativeAuthenticator.CustomizeInitialHelloCommand(helloCommand, cancellationToken);
            }

            return helloCommand;
        }

        private static BsonDocument CreateSaslSupportedMechsRequest(MongoIdentity identity)
        {
            return new BsonDocument { { "saslSupportedMechs", $"{identity.Source}.{identity.Username}" } };
        }

        // see https://github.com/mongodb/specifications/blob/master/source/auth/auth.rst#defaults
        private IAuthenticator CreateAuthenticator(IConnection connection, ConnectionDescription description)
        {
            // If saslSupportedMechs is not present in the hello or legacy hello results for mechanism negotiation, then SCRAM-SHA-1 MUST be used
            var mechanismName = ScramShaSaslMechanism.ScramSha1MechanismName;
            // If a saslSupportedMechs field was present in the hello or legacy hello results for mechanism negotiation,
            // then it MUST be inspected to select a default mechanism.
            if (description.HelloResult.HasSaslSupportedMechs)
            {
                // If SCRAM-SHA-256 is present in the list of mechanisms, then it MUST be used as the default;
                // otherwise, SCRAM-SHA-1 MUST be used as the default, regardless of whether SCRAM-SHA-1 is in the list.
                if (description.HelloResult.SaslSupportedMechs.Contains("SCRAM-SHA-256"))
                {
                    mechanismName = ScramShaSaslMechanism.ScramSha256MechanismName;
                }
            }

            if (SaslAuthenticator.TryCreate(
                    mechanismName,
                    _endPoints,
                    _identity,
                    _identityEvidence,
                    null,
                    _serverApi,
                    out var authenticator))
            {
                return authenticator;
            }

            throw new InvalidOperationException("Cannot create authenticator for the connection.");
        }

        private IAuthenticator GetOrCreateAuthenticator(IConnection connection, ConnectionDescription description)
        {
            /* It is possible to have Hello["SpeculativeAuthenticate"] != null and for
             * _speculativeScramSha256Authenticator to be null in the case of multiple authenticators */
            var speculativeAuthenticateResult = description.HelloResult.SpeculativeAuthenticate;
            var canUseSpeculativeAuthenticator = _speculativeAuthenticator != null && speculativeAuthenticateResult != null;
            return canUseSpeculativeAuthenticator ? _speculativeAuthenticator : CreateAuthenticator(connection, description);
        }
    }
}
