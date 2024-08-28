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
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Authentication.ScramSha
{
    internal sealed class ScramShaFirstSaslStep : ISaslStep
    {
        private readonly IScramShaAlgorithm _algorithm;
        private readonly ScramCache _cache;
        private readonly UsernamePasswordCredential _credential;
        private readonly IRandomStringGenerator _randomStringGenerator;

        public ScramShaFirstSaslStep(IScramShaAlgorithm algorithm, UsernamePasswordCredential credential, IRandomStringGenerator randomStringGenerator, ScramCache cache)
        {
            _algorithm = algorithm;
            _credential = credential;
            _randomStringGenerator = randomStringGenerator;
            _cache = cache;
        }

        public (byte[] BytesToSendToServer, ISaslStep NextStep) Execute(
            SaslConversation conversation,
            byte[] bytesReceivedFromServer,
            CancellationToken cancellationToken)
        {
            const string gs2Header = "n,,";
            var username = "n=" + PrepUsername(_credential.Username);
            var r = GenerateRandomString();
            var nonce = "r=" + r;

            var clientFirstMessageBare = username + "," + nonce;
            var clientFirstMessage = gs2Header + clientFirstMessageBare;
            var clientFirstMessageBytes = Utf8Encodings.Strict.GetBytes(clientFirstMessage);

            var nextStep = new ScramShaSecondSaslStep(_algorithm, _credential, _cache, clientFirstMessageBare, r);
            return (clientFirstMessageBytes, nextStep);
        }

        public Task<(byte[] BytesToSendToServer, ISaslStep NextStep)> ExecuteAsync(
            SaslConversation conversation,
            byte[] bytesReceivedFromServer,
            CancellationToken cancellationToken)
            => Task.FromResult(Execute(conversation, bytesReceivedFromServer, cancellationToken));

        private string GenerateRandomString()
        {
            const string legalCharacters = "!\"#$%&'()*+-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";

            return _randomStringGenerator.Generate(20, legalCharacters);
        }

        private string PrepUsername(string username)
        {
            return username.Replace("=", "=3D").Replace(",", "=2C");
        }
    }
}
