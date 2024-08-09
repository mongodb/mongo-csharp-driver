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

using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver.Authentication.Gssapi
{
    internal sealed class GssapiNegotiateSaslStep : ISaslStep
    {
        private readonly string _authorizationId;
        private readonly ISecurityContext _context;

        public GssapiNegotiateSaslStep(ISecurityContext context, string authorizationId)
        {
            _context = context;
            _authorizationId = authorizationId;
        }

        public (byte[] BytesToSendToServer, ISaslStep NextStep) Execute(
            SaslConversation conversation,
            byte[] bytesReceivedFromServer,
            CancellationToken cancellationToken)
        {
            try
            {
                // NOTE: We simply check whether we can successfully decrypt the message,
                //       but don't do anything with the decrypted plaintext
                _ = _context.DecryptMessage(0, bytesReceivedFromServer);
            }
            catch (GssapiException ex)
            {
                throw new MongoAuthenticationException(conversation.ConnectionId, "Unable to decrypt message.", ex);
            }

            int length = 4;
            if (_authorizationId != null)
            {
                length += _authorizationId.Length;
            }

            bytesReceivedFromServer = new byte[length];
            bytesReceivedFromServer[0] = 0x1; // NO_PROTECTION
            bytesReceivedFromServer[1] = 0x0; // NO_PROTECTION
            bytesReceivedFromServer[2] = 0x0; // NO_PROTECTION
            bytesReceivedFromServer[3] = 0x0; // NO_PROTECTION

            if (_authorizationId != null)
            {
                var authorizationIdBytes = Encoding.UTF8.GetBytes(_authorizationId);
                authorizationIdBytes.CopyTo(bytesReceivedFromServer, 4);
            }

            byte[] bytesToSendToServer;
            try
            {
                bytesToSendToServer = _context.EncryptMessage(bytesReceivedFromServer);
            }
            catch (GssapiException ex)
            {
                throw new MongoAuthenticationException(conversation.ConnectionId, "Unable to encrypt message.", ex);
            }

            return (bytesToSendToServer, null);
        }

        public Task<(byte[] BytesToSendToServer, ISaslStep NextStep)> ExecuteAsync(
            SaslConversation conversation,
            byte[] bytesReceivedFromServer,
            CancellationToken cancellationToken)
            => Task.FromResult(Execute(conversation, bytesReceivedFromServer, cancellationToken));
    }
}
