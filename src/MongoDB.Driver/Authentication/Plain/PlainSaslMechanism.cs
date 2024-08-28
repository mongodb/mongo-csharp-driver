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

namespace MongoDB.Driver.Authentication.Plain
{
    internal sealed class PlainSaslMechanism : ISaslMechanism
    {
        public const string MechanismName = "PLAIN";

        public static PlainSaslMechanism Create(SaslContext context)
        {
            Ensure.IsNotNull(context, nameof(context));
            if (context.Mechanism != MechanismName)
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
                throw new NotSupportedException($"{MechanismName} auth mechanism require password.");
            }

            return new PlainSaslMechanism(credential);
        }

        private readonly UsernamePasswordCredential _credential;

        private PlainSaslMechanism(UsernamePasswordCredential credential)
        {
            _credential = Ensure.IsNotNull(credential, nameof(credential));
        }

        public string DatabaseName => _credential.Source;

        public string Name => MechanismName;

        public ISaslStep CreateSpeculativeAuthenticationStep() => null;

        public BsonDocument CustomizeSaslStartCommand(BsonDocument startCommand) => startCommand;

        public ISaslStep Initialize(SaslConversation conversation, ConnectionDescription description)
            => new PlainSaslStep(_credential);

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
