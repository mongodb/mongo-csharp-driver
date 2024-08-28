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

using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver.Authentication.Oidc
{
    internal sealed class OidcObtainCredentialsSaslStep : OidcSaslStep, ISaslStep
    {
        private readonly IOidcCallbackAdapter _oidcCallback;
        private readonly string _principalName;

        public OidcObtainCredentialsSaslStep(IOidcCallbackAdapter oidcCallback, string principalName)
        {
            _oidcCallback = oidcCallback;
            _principalName = principalName;
        }

        public (byte[] BytesToSendToServer, ISaslStep NextStep) Execute(
            SaslConversation conversation,
            byte[] bytesReceivedFromServer,
            CancellationToken cancellationToken)
        {
            var credentials = _oidcCallback.GetCredentials(new OidcCallbackParameters(1, _principalName), cancellationToken);
            return PreparePayload(credentials);
        }

        public async Task<(byte[] BytesToSendToServer, ISaslStep NextStep)> ExecuteAsync(
            SaslConversation conversation,
            byte[] bytesReceivedFromServer,
            CancellationToken cancellationToken)
        {
            var credentials = await _oidcCallback.GetCredentialsAsync(new OidcCallbackParameters(1, _principalName), cancellationToken);
            return PreparePayload(credentials);
        }
    }
}
