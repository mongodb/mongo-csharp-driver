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
using MongoDB.Bson;
using MongoDB.Driver.Authentication.AWS.CredentialsSources;

namespace MongoDB.Driver.Authentication.AWS.SaslSteps
{
    internal sealed class AWSFirstSaslStep : ISaslStep
    {
        private readonly IClock _clock;
        private readonly IAWSCredentialsSource _credentialsSource;
        private readonly IRandomByteGenerator _randomByteGenerator;

        public AWSFirstSaslStep(IAWSCredentialsSource credentialsSource, IRandomByteGenerator randomByteGenerator, IClock clock)
        {
            _credentialsSource = credentialsSource;
            _randomByteGenerator = randomByteGenerator;
            _clock = clock;
        }

        public (byte[] BytesToSendToServer, ISaslStep NextStep) Execute(
            SaslConversation conversation,
            byte[] bytesReceivedFromServer,
            CancellationToken cancellationToken)
        {
            var nonce = _randomByteGenerator.Generate(AWSSaslMechanism.ClientNonceLength);
            var document = new BsonDocument
            {
                { "r", nonce },
                { "p", (int)'n' }
            };

            return (document.ToBson(), new AWSLastSaslStep(nonce, _credentialsSource, _clock));
        }

        public Task<(byte[] BytesToSendToServer, ISaslStep NextStep)> ExecuteAsync(
            SaslConversation conversation,
            byte[] bytesReceivedFromServer,
            CancellationToken cancellationToken)
            => Task.FromResult(Execute(conversation, bytesReceivedFromServer, cancellationToken));
    }
}
