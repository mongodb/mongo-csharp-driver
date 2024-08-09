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
using MongoDB.Bson.IO;

namespace MongoDB.Driver.Authentication.ScramSha
{
    internal sealed class ScramShaSecondSaslStep : ISaslStep
    {
        private readonly IScramShaAlgorithm _algorithm;
        private readonly ScramCache _cache;
        private readonly string _clientFirstMessageBare;
        private readonly UsernamePasswordCredential _credential;
        private readonly string _rPrefix;

        public ScramShaSecondSaslStep(IScramShaAlgorithm algorithm, UsernamePasswordCredential credential, ScramCache cache, string clientFirstMessageBare, string rPrefix)
        {
            _algorithm = algorithm;
            _credential = credential;
            _cache = cache;
            _clientFirstMessageBare = clientFirstMessageBare;
            _rPrefix = rPrefix;
        }

        public (byte[] BytesToSendToServer, ISaslStep NextStep) Execute(
            SaslConversation conversation,
            byte[] bytesReceivedFromServer,
            CancellationToken cancellationToken)
        {
            var encoding = Utf8Encodings.Strict;
            var serverFirstMessage = encoding.GetString(bytesReceivedFromServer);
            var map = SaslMapParser.Parse(serverFirstMessage);

            var r = map['r'];
            if (!r.StartsWith(_rPrefix, StringComparison.Ordinal))
            {
                throw new MongoAuthenticationException(conversation.ConnectionId, message: "Server sent an invalid nonce.");
            }
            var s = map['s'];
            var i = map['i'];

            const string gs2Header = "n,,";
            var channelBinding = "c=" + Convert.ToBase64String(encoding.GetBytes(gs2Header));
            var nonce = "r=" + r;
            var clientFinalMessageWithoutProof = channelBinding + "," + nonce;

            var salt = Convert.FromBase64String(map['s']);
            var iterations = int.Parse(map['i']);

            byte[] clientKey;
            byte[] serverKey;

            var cacheKey = new ScramCacheKey(_credential.SaslPreppedPassword, salt, iterations);
            if (_cache.TryGet(cacheKey, out var cacheEntry))
            {
                clientKey = cacheEntry.ClientKey;
                serverKey = cacheEntry.ServerKey;
            }
            else
            {
                var saltedPassword = _algorithm.Hi(_credential, salt, iterations);
                clientKey = _algorithm.Hmac(encoding, saltedPassword, "Client Key");
                serverKey = _algorithm.Hmac(encoding, saltedPassword, "Server Key");
                _cache.Add(cacheKey, new ScramCacheEntry(clientKey, serverKey));
            }

            var storedKey = _algorithm.H(clientKey);
            var authMessage = _clientFirstMessageBare + "," + serverFirstMessage + "," + clientFinalMessageWithoutProof;
            var clientSignature = _algorithm.Hmac(encoding, storedKey, authMessage);
            var clientProof = XOR(clientKey, clientSignature);
            var serverSignature = _algorithm.Hmac(encoding, serverKey, authMessage);
            var proof = "p=" + Convert.ToBase64String(clientProof);
            var clientFinalMessage = clientFinalMessageWithoutProof + "," + proof;

            return(encoding.GetBytes(clientFinalMessage), new ScramShaLastSaslStep(serverSignature));
        }

        public Task<(byte[] BytesToSendToServer, ISaslStep NextStep)> ExecuteAsync(
            SaslConversation conversation,
            byte[] bytesReceivedFromServer,
            CancellationToken cancellationToken)
            => Task.FromResult(Execute(conversation, bytesReceivedFromServer, cancellationToken));

        private byte[] XOR(byte[] a, byte[] b)
        {
            var result = new byte[a.Length];
            for (int i = 0; i < a.Length; i++)
            {
                result[i] = (byte)(a[i] ^ b[i]);
            }

            return result;
        }
    }
}
