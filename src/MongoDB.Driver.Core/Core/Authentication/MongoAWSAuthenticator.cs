/* Copyright 2020–present MongoDB Inc.
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
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Authentication.External;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Authentication
{
    /// <summary>
    /// The Mongo AWS authenticator.
    /// </summary>
    public class MongoAWSAuthenticator : SaslAuthenticator
    {
        // constants
        private const int ClientNonceLength = 32;

        #region static
        // public static properties
        /// <summary>
        /// Gets the name of the mechanism.
        /// </summary>
        /// <value>
        /// The name of the mechanism.
        /// </value>
        public static string MechanismName
        {
            get { return "MONGODB-AWS"; }
        }

        // private static methods
        private static MongoAWSMechanism CreateMechanism(
            UsernamePasswordCredential credential,
            IEnumerable<KeyValuePair<string, string>> properties,
            IRandomByteGenerator randomByteGenerator,
            IExternalAuthenticationCredentialsProvider<AwsCredentials> externalAuthenticationCredentialsProvider,
            IClock clock)
        {
            if (credential.Source != "$external")
            {
                throw new ArgumentException("MONGODB-AWS authentication may only use the $external source.", nameof(credential));
            }

            return CreateMechanism(credential.Username, credential.Password, properties, randomByteGenerator, externalAuthenticationCredentialsProvider, clock);
        }

        private static MongoAWSMechanism CreateMechanism(
            string username,
            SecureString password,
            IEnumerable<KeyValuePair<string, string>> properties,
            IRandomByteGenerator randomByteGenerator,
            IExternalAuthenticationCredentialsProvider<AwsCredentials> externalAuthenticationCredentialsProvider,
            IClock clock)
        {
            var awsCredentials =
                CreateAwsCredentialsFromMongoCredentials(username, password, properties) ??
                externalAuthenticationCredentialsProvider.CreateCredentialsFromExternalSource();

            return new MongoAWSMechanism(awsCredentials, randomByteGenerator, clock);
        }

        private static AwsCredentials CreateAwsCredentialsFromMongoCredentials(string username, SecureString password, IEnumerable<KeyValuePair<string, string>> properties)
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

            return new AwsCredentials(accessKeyId: username, secretAccessKey: password, sessionToken);
        }

        private static string ExtractSessionTokenFromMechanismProperties(IEnumerable<KeyValuePair<string, string>> properties)
        {
            if (properties != null)
            {
                foreach (var pair in properties)
                {
                    if (pair.Key.ToUpperInvariant() == "AWS_SESSION_TOKEN")
                    {
                        return pair.Value;
                    }
                }
            }

            return null;
        }

        private static void ValidateMechanismProperties(IEnumerable<KeyValuePair<string, string>> properties)
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
        #endregion

        private readonly ICredentialsCache<AwsCredentials> _credentialsCache;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MongoAWSAuthenticator"/> class.
        /// </summary>
        /// <param name="credential">The credentials.</param>
        /// <param name="properties">The properties.</param>
        /// <param name="serverApi">The server API.</param>
        public MongoAWSAuthenticator(
            UsernamePasswordCredential credential,
            IEnumerable<KeyValuePair<string, string>> properties,
            ServerApi serverApi)
            : this(
                  credential,
                  properties,
                  new DefaultRandomByteGenerator(),
                  ExternalCredentialsAuthenticators.Instance.Aws,
                  SystemClock.Instance,
                  serverApi)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoAWSAuthenticator"/> class.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="properties">The properties.</param>
        /// <param name="serverApi">The server API.</param>
        public MongoAWSAuthenticator(
            string username,
            IEnumerable<KeyValuePair<string, string>> properties,
            ServerApi serverApi)
            : this(
                  username,
                  properties,
                  new DefaultRandomByteGenerator(),
                  ExternalCredentialsAuthenticators.Instance.Aws,
                  SystemClock.Instance,
                  serverApi)
        {
        }

        internal MongoAWSAuthenticator(
            UsernamePasswordCredential credential,
            IEnumerable<KeyValuePair<string, string>> properties,
            IRandomByteGenerator randomByteGenerator,
            IExternalAuthenticationCredentialsProvider<AwsCredentials> externalAuthenticationCredentialsProvider,
            IClock clock,
            ServerApi serverApi)
            : base(CreateMechanism(credential, properties, randomByteGenerator, externalAuthenticationCredentialsProvider, clock), serverApi)
        {
            _credentialsCache = externalAuthenticationCredentialsProvider as ICredentialsCache<AwsCredentials>; // can be null
        }

        internal MongoAWSAuthenticator(
            string username,
            IEnumerable<KeyValuePair<string, string>> properties,
            IRandomByteGenerator randomByteGenerator,
            IExternalAuthenticationCredentialsProvider<AwsCredentials> externalAuthenticationCredentialsProvider,
            IClock clock,
            ServerApi serverApi)
            : base(CreateMechanism(username, null, properties, randomByteGenerator, externalAuthenticationCredentialsProvider, clock), serverApi)
        {
            _credentialsCache = externalAuthenticationCredentialsProvider as ICredentialsCache<AwsCredentials>; // can be null
        }

        /// <inheritdoc/>
        public override string DatabaseName
        {
            get { return "$external"; }
        }

        /// <inheritdoc/>
        public override void Authenticate(IConnection connection, ConnectionDescription description, CancellationToken cancellationToken)
        {
            try
            {
                base.Authenticate(connection, description, cancellationToken);
            }
            catch
            {
                _credentialsCache?.Clear();
                throw;
            }
        }

        /// <inheritdoc/>
        public override async Task AuthenticateAsync(IConnection connection, ConnectionDescription description, CancellationToken cancellationToken)
        {
            try
            {
                await base.AuthenticateAsync(connection, description, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                _credentialsCache?.Clear();
                throw;
            }
        }

        // nested classes
        private class MongoAWSMechanism : ISaslMechanism
        {
            private readonly AwsCredentials _awsCredentials;
            private readonly IClock _clock;
            private readonly IRandomByteGenerator _randomByteGenerator;

            public MongoAWSMechanism(
                AwsCredentials awsCredentials,
                IRandomByteGenerator randomByteGenerator,
                IClock clock)
            {
                _awsCredentials = Ensure.IsNotNull(awsCredentials, nameof(awsCredentials));
                _randomByteGenerator = Ensure.IsNotNull(randomByteGenerator, nameof(randomByteGenerator));
                _clock = Ensure.IsNotNull(clock, nameof(clock));
            }

            public string Name
            {
                get { return MechanismName; }
            }

            public ISaslStep Initialize(IConnection connection, SaslConversation conversation, ConnectionDescription description)
            {
                Ensure.IsNotNull(connection, nameof(connection));
                Ensure.IsNotNull(description, nameof(description));

                var nonce = GenerateRandomBytes();

                var document = new BsonDocument
                {
                    { "r", nonce },
                    { "p", (int)'n' }
                };

                var clientMessageBytes = document.ToBson();

                return new ClientFirst(clientMessageBytes, nonce, _awsCredentials, _clock);
            }

            private byte[] GenerateRandomBytes()
            {
                return _randomByteGenerator.Generate(ClientNonceLength);
            }
        }

        private class ClientFirst : ISaslStep
        {
            private readonly AwsCredentials _awsCredentials;
            private readonly byte[] _bytesToSendToServer;
            private readonly IClock _clock;
            private readonly byte[] _nonce;

            public ClientFirst(
                byte[] bytesToSendToServer,
                byte[] nonce,
                AwsCredentials awsCredentials,
                IClock clock)
            {
                _bytesToSendToServer = bytesToSendToServer;
                _nonce = nonce;
                _awsCredentials = awsCredentials;
                _clock = clock;
            }

            public byte[] BytesToSendToServer
            {
                get { return _bytesToSendToServer; }
            }

            public bool IsComplete
            {
                get { return false; }
            }

            public ISaslStep Transition(SaslConversation conversation, byte[] bytesReceivedFromServer)
            {
                var serverFirstMessageDocument = BsonSerializer.Deserialize<BsonDocument>(bytesReceivedFromServer);
                var serverNonce = serverFirstMessageDocument["s"].AsByteArray;
                var host = serverFirstMessageDocument["h"].AsString;

                if (serverNonce.Length != ClientNonceLength * 2 || !serverNonce.Take(ClientNonceLength).SequenceEqual(_nonce))
                {
                    throw new MongoAuthenticationException(conversation.ConnectionId, "Server sent an invalid nonce.");
                }
                if (host.Length < 1 || host.Length > 255 || host.Contains(".."))
                {
                    throw new MongoAuthenticationException(conversation.ConnectionId, "Server returned an invalid sts host.");
                }
                var unexpectedNames = serverFirstMessageDocument.Names.Except(new[] { "h", "s" });
                if (unexpectedNames.Any())
                {
                    throw new MongoAuthenticationException(
                        conversation.ConnectionId,
                        $"Server returned unexpected fields: {string.Join(", ", unexpectedNames)}.");
                }

                AwsSignatureVersion4.CreateAuthorizationRequest(
                    _clock.UtcNow,
                    _awsCredentials.AccessKeyId,
                    _awsCredentials.SecretAccessKey,
                    _awsCredentials.SessionToken,
                    serverNonce,
                    host,
                    out var authorizationHeader,
                    out var timestamp);

                var document = new BsonDocument
                {
                    { "a", authorizationHeader },
                    { "d", timestamp },
                    { "t", _awsCredentials.SessionToken, _awsCredentials.SessionToken != null }
                };

                var clientSecondMessageBytes = document.ToBson();

                return new ClientLast(clientSecondMessageBytes);
            }
        }

        private class ClientLast : ISaslStep
        {
            private readonly byte[] _bytesToSendToServer;

            public ClientLast(byte[] bytesToSendToServer)
            {
                _bytesToSendToServer = bytesToSendToServer;
            }

            public byte[] BytesToSendToServer
            {
                get { return _bytesToSendToServer; }
            }

            public bool IsComplete
            {
                get { return false; }
            }

            public ISaslStep Transition(SaslConversation conversation, byte[] bytesReceivedFromServer)
            {
                return new CompletedStep();
            }
        }
    }
}
