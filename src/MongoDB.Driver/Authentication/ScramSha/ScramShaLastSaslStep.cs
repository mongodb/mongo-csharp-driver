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
    internal sealed class ScramShaLastSaslStep : ISaslStep
    {
        private readonly byte[] _serverSignature64;

        public ScramShaLastSaslStep(byte[] serverSignature64)
        {
            _serverSignature64 = serverSignature64;
        }

        public (byte[] BytesToSendToServer, ISaslStep NextStep) Execute(
            SaslConversation conversation,
            byte[] bytesReceivedFromServer,
            CancellationToken cancellationToken)
        {
            var encoding = Utf8Encodings.Strict;
            var map = SaslMapParser.Parse(encoding.GetString(bytesReceivedFromServer));
            var serverSignature = Convert.FromBase64String(map['v']);

            if (!ConstantTimeEquals(_serverSignature64, serverSignature))
            {
                throw new MongoAuthenticationException(conversation.ConnectionId, message: "Server signature was invalid.");
            }

            return (Array.Empty<byte>(), null);
        }

        public Task<(byte[] BytesToSendToServer, ISaslStep NextStep)> ExecuteAsync(
            SaslConversation conversation,
            byte[] bytesReceivedFromServer,
            CancellationToken cancellationToken)
            => Task.FromResult(Execute(conversation, bytesReceivedFromServer, cancellationToken));

        private bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            var diff = a.Length ^ b.Length;
            for (var i = 0; i < a.Length && i < b.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }

            return diff == 0;
        }
    }
}
