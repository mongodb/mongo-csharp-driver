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
using MongoDB.Bson.IO;

namespace MongoDB.Driver.Authentication.Plain
{
    internal sealed class PlainSaslStep : ISaslStep
    {
        private readonly UsernamePasswordCredential _credential;

        public PlainSaslStep(UsernamePasswordCredential credential)
        {
            _credential = credential;
        }

        public (byte[] BytesToSendToServer, ISaslStep NextStep) Execute(
            SaslConversation conversation,
            byte[] bytesReceivedFromServer,
            CancellationToken cancellationToken)
            => (PreparePayload(), null);

        public Task<(byte[] BytesToSendToServer, ISaslStep NextStep)> ExecuteAsync(
            SaslConversation conversation,
            byte[] bytesReceivedFromServer,
            CancellationToken cancellationToken)
            => Task.FromResult(Execute(conversation, bytesReceivedFromServer, cancellationToken));

        private byte[] PreparePayload()
        {
            var dataString = $"\0{_credential.Username}\0{_credential.GetInsecurePassword()}";
            return Utf8Encodings.Strict.GetBytes(dataString);
        }
    }
}
