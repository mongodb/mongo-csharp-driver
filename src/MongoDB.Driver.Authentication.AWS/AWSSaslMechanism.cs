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
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver.Authentication.AWS.CredentialsSources;
using MongoDB.Driver.Authentication.AWS.SaslSteps;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Authentication.AWS
{
    internal sealed class AWSSaslMechanism : ISaslMechanism
    {
        public static AWSSaslMechanism Create(SaslContext context)
            => Create(context, RandomByteGenerator.Instance, SystemClock.Instance);

        public static AWSSaslMechanism Create(SaslContext context, IRandomByteGenerator randomByteGenerator, IClock clock)
        {
            Ensure.IsNotNull(context, nameof(context));
            Ensure.IsNotNull(randomByteGenerator, nameof(randomByteGenerator));
            Ensure.IsNotNull(clock, nameof(clock));
            if (context.Mechanism != MechanismName)
            {
                throw new InvalidOperationException($"Unexpected authentication mechanism: {context.Mechanism}");
            }

            if (context.Identity.Source != "$external")
            {
                throw new ArgumentException("MONGODB-AWS authentication may only use the $external source.", nameof(context.Identity.Source));
            }

            string password = null;
            if (context.IdentityEvidence is PasswordEvidence passwordEvidence)
            {
                password = passwordEvidence.ToInsecureString();
            }

            var awsCredentials = CreateAwsCredentialsFromMongoCredentials(context.Identity.Username, password, context.MechanismProperties);
            IAWSCredentialsSource credentialsSource = awsCredentials != null ? new AWSInstanceCredentialsSource(awsCredentials) : AWSFallbackCredentialsSource.Instance;

            return new AWSSaslMechanism(credentialsSource, randomByteGenerator, clock);
        }

        private static AWSCredentials CreateAwsCredentialsFromMongoCredentials(string username, string password, IEnumerable<KeyValuePair<string, object>> properties)
        {
            ValidateMechanismProperties(properties);
            var sessionToken = ExtractSessionTokenFromMechanismProperties(properties);

            if (username == null && password == null && sessionToken == null)
            {
                return null;
            }
            if (password != null && username == null)
            {
                throw new InvalidOperationException("When using MONGODB-AWS authentication if a password is provided via settings then a username must be provided also.");
            }
            if (username != null && password == null)
            {
                throw new InvalidOperationException("When using MONGODB-AWS authentication if a username is provided via settings then a password must be provided also.");
            }
            if (sessionToken != null && (username == null || password == null))
            {
                throw new InvalidOperationException("When using MONGODB-AWS authentication if a session token is provided via settings then a username and password must be provided also.");
            }

            return new AWSCredentials(accessKeyId: username, secretAccessKey: password, sessionToken);
        }

        private static string ExtractSessionTokenFromMechanismProperties(IEnumerable<KeyValuePair<string, object>> properties)
        {
            if (properties != null)
            {
                foreach (var pair in properties)
                {
                    if (pair.Key.ToUpperInvariant() == "AWS_SESSION_TOKEN")
                    {
                        return (string)pair.Value;
                    }
                }
            }

            return null;
        }

        private static void ValidateMechanismProperties(IEnumerable<KeyValuePair<string, object>> properties)
        {
            if (properties != null)
            {
                foreach (var pair in properties)
                {
                    if (pair.Key.ToUpperInvariant() != "AWS_SESSION_TOKEN")
                    {
                        throw new ArgumentException($"Unknown AWS property '{pair.Key}'.", nameof(properties));
                    }
                }
            }
        }

        public const int ClientNonceLength = 32;
        public const string MechanismName = "MONGODB-AWS";

        private readonly IClock _clock;
        private readonly IAWSCredentialsSource _credentialsSource;
        private readonly IRandomByteGenerator _randomByteGenerator;

        private AWSSaslMechanism(IAWSCredentialsSource credentialsSource, IRandomByteGenerator randomByteGenerator, IClock clock)
        {
            _credentialsSource = credentialsSource;
            _randomByteGenerator = randomByteGenerator;
            _clock = clock;
        }

        public string DatabaseName => "$external";

        public string Name => MechanismName;

        public ISaslStep CreateSpeculativeAuthenticationStep() => null;

        public BsonDocument CustomizeSaslStartCommand(BsonDocument startCommand) => startCommand;

        public ISaslStep Initialize(SaslConversation conversation, ConnectionDescription description)
            => new AWSFirstSaslStep(_credentialsSource, _randomByteGenerator, _clock);

        public void OnReAuthenticationRequired()
        {
        }

        public bool TryHandleAuthenticationException(MongoException exception, ISaslStep step, SaslConversation conversation, ConnectionDescription description, out ISaslStep nextStep)
        {
            _credentialsSource.ResetCache();
            nextStep = null;
            return false;
        }
    }
}
