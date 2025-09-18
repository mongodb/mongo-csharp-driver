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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Authentication.AWS.CredentialsSources;

namespace MongoDB.Driver.Authentication.AWS.SaslSteps
{
    internal sealed class AWSLastSaslStep : ISaslStep
    {
        private static readonly ISet<string> __serverResponseExpectedNames = new HashSet<string>(new []{ "h", "s" });

        private readonly IClock _clock;
        private readonly IAWSCredentialsSource _credentialsSource;
        private readonly byte[] _nonce;

        public AWSLastSaslStep(byte[] nonce, IAWSCredentialsSource credentialsSource, IClock clock)
        {
            _nonce = nonce;
            _credentialsSource = credentialsSource;
            _clock = clock;
        }

        public (byte[] BytesToSendToServer, ISaslStep NextStep) Execute(
            SaslConversation conversation,
            byte[] bytesReceivedFromServer,
            CancellationToken cancellationToken)
        {
            ParseServerResponse(conversation, bytesReceivedFromServer, out var serverNonce, out var host);
            var credentials = _credentialsSource.GetCredentials(cancellationToken);
            return (PreparePayload(credentials, serverNonce, host), null);
        }

        public async Task<(byte[] BytesToSendToServer, ISaslStep NextStep)> ExecuteAsync(
            SaslConversation conversation,
            byte[] bytesReceivedFromServer,
            CancellationToken cancellationToken)
        {
            ParseServerResponse(conversation, bytesReceivedFromServer, out var serverNonce, out var host);
            var credentials = await _credentialsSource.GetCredentialsAsync(cancellationToken).ConfigureAwait(false);
            return (PreparePayload(credentials, serverNonce, host), null);
        }

        private byte[] PreparePayload(AWSCredentials credentials, byte[] serverNonce, string host)
        {
            AWSSignatureVersion4.CreateAuthorizationRequest(
                _clock.UtcNow,
                credentials.AccessKeyId,
                credentials.SecretAccessKey,
                credentials.SessionToken,
                serverNonce,
                host,
                out var authorizationHeader,
                out var timestamp);

            var document = new BsonDocument
            {
                { "a", authorizationHeader },
                { "d", timestamp },
                { "t", credentials.SessionToken, credentials.SessionToken != null }
            };

            return document.ToBson();
        }

        private void ParseServerResponse(SaslConversation conversation, byte[] bytesReceivedFromServer, out byte[] serverNonce, out string host)
        {
            var serverFirstMessageDocument = BsonSerializer.Deserialize<BsonDocument>(bytesReceivedFromServer);
            if (serverFirstMessageDocument.Names.Any(n => !__serverResponseExpectedNames.Contains(n)))
            {
                var unexpectedNames = serverFirstMessageDocument.Names.Except(__serverResponseExpectedNames);

                throw new MongoAuthenticationException(
                    conversation.ConnectionId,
                    $"Server returned unexpected fields: {string.Join(", ", unexpectedNames)}.");
            }

            serverNonce = serverFirstMessageDocument["s"].AsByteArray;
            host = serverFirstMessageDocument["h"].AsString;

            if (serverNonce.Length != AWSSaslMechanism.ClientNonceLength * 2 || !serverNonce.Take(AWSSaslMechanism.ClientNonceLength).SequenceEqual(_nonce))
            {
                throw new MongoAuthenticationException(conversation.ConnectionId, "Server sent an invalid nonce.");
            }

            if (host.Length < 1 || host.Length > 255 || host.Contains(".."))
            {
                throw new MongoAuthenticationException(conversation.ConnectionId, "Server returned an invalid sts host.");
            }
        }
    }
}
