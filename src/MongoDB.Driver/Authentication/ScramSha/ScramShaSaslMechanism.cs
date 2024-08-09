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
using MongoDB.Bson;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Authentication.ScramSha
{
    internal sealed class ScramShaSaslMechanism : ISaslMechanism
    {
        public const string ScramSha1MechanismName = "SCRAM-SHA-1";
        public const string ScramSha256MechanismName = "SCRAM-SHA-256";

        public static ScramShaSaslMechanism CreateScramSha1Mechanism(SaslContext context)
            => CreateScramSha1Mechanism(context, DefaultRandomStringGenerator.Instance);

        internal static ScramShaSaslMechanism CreateScramSha1Mechanism(SaslContext context, IRandomStringGenerator randomStringGenerator)
            => Create(context, ScramSha1MechanismName, new ScramSha1Algorithm(), randomStringGenerator);

        public static ScramShaSaslMechanism CreateScramSha256Mechanism(SaslContext context)
            => CreateScramSha256Mechanism(context, DefaultRandomStringGenerator.Instance);

        internal static ScramShaSaslMechanism CreateScramSha256Mechanism(SaslContext context, IRandomStringGenerator randomStringGenerator)
            => Create(context, ScramSha256MechanismName, new ScramSha256Algorithm(), randomStringGenerator);

        private static ScramShaSaslMechanism Create(
            SaslContext context,
            string mechanismName,
            IScramShaAlgorithm algorithm,
            IRandomStringGenerator randomStringGenerator)
        {
            Ensure.IsNotNull(context, nameof(context));
            Ensure.IsNotNull(randomStringGenerator, nameof(randomStringGenerator));
            if (context.Mechanism != mechanismName)
            {
                throw new InvalidOperationException($"Unexpected authentication mechanism: {context.Mechanism}");
            }

            UsernamePasswordCredential credential;
            if (context.IdentityEvidence is PasswordEvidence passwordEvidence)
            {
                credential = new UsernamePasswordCredential(
                    context.Identity.Source,
                    context.Identity.Username,
                    passwordEvidence.ToInsecureString());
            }
            else
            {
                throw new NotSupportedException($"{mechanismName} auth mechanism require password.");
            }

            return new ScramShaSaslMechanism(mechanismName, algorithm, credential, randomStringGenerator, new ScramCache());
        }

        private readonly IScramShaAlgorithm _algorithm;
        private readonly ScramCache _cache;
        private readonly UsernamePasswordCredential _credential;
        private readonly IRandomStringGenerator _randomStringGenerator;

        private ScramShaSaslMechanism(
            string mechanismName,
            IScramShaAlgorithm algorithm,
            UsernamePasswordCredential credential,
            IRandomStringGenerator randomStringGenerator,
            ScramCache cache)
        {
            Name = mechanismName;
            _algorithm = algorithm;
            _credential = credential;
            _randomStringGenerator = randomStringGenerator;
            _cache = cache;
        }

        public string DatabaseName => _credential.Source;

        public string Name { get; }

        public ISaslStep CreateSpeculativeAuthenticationStep()
            => Initialize(null, null);

        public BsonDocument CustomizeSaslStartCommand(BsonDocument startCommand)
        {
            startCommand.Add("options", new BsonDocument("skipEmptyExchange", true));
            return startCommand;
        }

        public ISaslStep Initialize(SaslConversation conversation, ConnectionDescription description)
            => new ScramShaFirstSaslStep(_algorithm, _credential, _randomStringGenerator, _cache);

        public void OnReAuthenticationRequired()
        {
        }

        public bool TryHandleAuthenticationException(
            MongoException exception,
            ISaslStep step,
            SaslConversation conversation,
            ConnectionDescription description,
            out ISaslStep nextStep)
        {
            nextStep = null;
            return false;
        }
    }
}
