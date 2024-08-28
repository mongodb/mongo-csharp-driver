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
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver.Authentication.Gssapi
{
    internal sealed class GssapiFirstSaslStep : ISaslStep
    {
        private readonly string _serviceName;
        private readonly string _hostname;
        private readonly string _realm;
        private readonly string _username;
        private readonly SecureString _password;

        public GssapiFirstSaslStep(string serviceName, string hostname, string realm, string username, SecureString password)
        {
            _serviceName = serviceName;
            _hostname = hostname;
            _realm = realm;
            _username = username;
            _password = password;
        }

        public (byte[] BytesToSendToServer, ISaslStep NextStep) Execute(
            SaslConversation conversation,
            byte[] bytesReceivedFromServer,
            CancellationToken cancellationToken)
        {
            try
            {
                var context = SecurityContextFactory.InitializeSecurityContext(_serviceName, _hostname, _realm, _username, _password);
                conversation.RegisterItemForDisposal(context);
                var bytesToSendToServer = context.Next(null) ?? Array.Empty<byte>();

                return (bytesToSendToServer, new GssapiInitializeSaslStep(context, _username));
            }
            catch (GssapiException ex)
            {
                if (_password != null)
                {
                    throw new MongoAuthenticationException(conversation.ConnectionId, "Unable to initialize security context. Ensure the username and password are correct.", ex);
                }

                throw new MongoAuthenticationException(conversation.ConnectionId, "Unable to initialize security context.", ex);
            }
        }

        public Task<(byte[] BytesToSendToServer, ISaslStep NextStep)> ExecuteAsync(
            SaslConversation conversation,
            byte[] bytesReceivedFromServer,
            CancellationToken cancellationToken)
            => Task.FromResult(Execute(conversation, bytesReceivedFromServer, cancellationToken));
    }
}
