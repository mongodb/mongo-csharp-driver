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

namespace MongoDB.Driver.Authentication.Gssapi
{
    internal sealed class GssapiInitializeSaslStep : ISaslStep
    {
        private readonly string _authorizationId;
        private readonly ISecurityContext _context;

        public GssapiInitializeSaslStep(ISecurityContext context, string authorizationId)
        {
            _context = context;
            _authorizationId = authorizationId;
        }

        public (byte[] BytesToSendToServer, ISaslStep NextStep) Execute(
            SaslConversation conversation,
            byte[] bytesReceivedFromServer,
            CancellationToken cancellationToken)
        {
            byte[] bytesToSendToServer;
            try
            {
                bytesToSendToServer = _context.Next(bytesReceivedFromServer) ?? Array.Empty<byte>();
            }
            catch (GssapiException ex)
            {
                throw new MongoAuthenticationException(conversation.ConnectionId, "Unable to initialize security context", ex);
            }

            ISaslStep nextStep = _context.IsInitialized ? new GssapiNegotiateSaslStep(_context, _authorizationId) : new GssapiInitializeSaslStep(_context, _authorizationId);
            return (bytesToSendToServer, nextStep);
        }

        public Task<(byte[] BytesToSendToServer, ISaslStep NextStep)> ExecuteAsync(
            SaslConversation conversation,
            byte[] bytesReceivedFromServer,
            CancellationToken cancellationToken)
            => Task.FromResult(Execute(conversation, bytesReceivedFromServer, cancellationToken));
    }
}
