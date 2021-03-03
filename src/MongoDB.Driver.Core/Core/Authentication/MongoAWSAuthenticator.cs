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
using System.Net.Http;
using System.Security;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Shared;

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
            IClock clock)
        {
            if (credential.Source != "$external")
            {
                throw new ArgumentException("MONGODB-AWS authentication may only use the $external source.", nameof(credential));
            }

            return CreateMechanism(credential.Username, credential.Password, properties, randomByteGenerator, clock);
        }

        private static MongoAWSMechanism CreateMechanism(
            string username,
            SecureString password,
            IEnumerable<KeyValuePair<string, string>> properties,
            IRandomByteGenerator randomByteGenerator,
            IClock clock)
        {
            var awsCredentials =
                CreateAwsCredentialsFromMongoCredentials(username, password, properties) ??
                CreateAwsCredentialsFromEnvironmentVariables() ??
                CreateAwsCredentialsFromEcsResponse() ??
                CreateAwsCredentialsFromEc2Response();

            if (awsCredentials == null)
            {
                throw new InvalidOperationException("Unable to find credentials for MONGODB-AWS authentication.");
            }

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

        private static AwsCredentials CreateAwsCredentialsFromEnvironmentVariables()
        {
            var accessKeyId = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
            var secretAccessKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
            var sessionToken = Environment.GetEnvironmentVariable("AWS_SESSION_TOKEN");

            if (accessKeyId == null && secretAccessKey == null && sessionToken == null)
            {
                return null;
            }
            if (secretAccessKey != null && accessKeyId == null)
            {
                throw new InvalidOperationException("When using MONGODB-AWS authentication if a secret access key is provided via environment variables then an access key ID must be provided also.");
            }
            if (accessKeyId != null && secretAccessKey == null)
            {
                throw new InvalidOperationException("When using MONGODB-AWS authentication if an access key ID is provided via environment variables then a secret access key must be provided also.");
            }
            if (sessionToken != null && (accessKeyId == null || secretAccessKey == null))
            {
                throw new InvalidOperationException("When using MONGODB-AWS authentication if a session token is provided via environment variables then an access key ID and a secret access key must be provided also.");
            }

            return new AwsCredentials(accessKeyId, SecureStringHelper.ToSecureString(secretAccessKey), sessionToken);
        }

        private static AwsCredentials CreateAwsCredentialsFromEcsResponse()
        {
            var relativeUri = Environment.GetEnvironmentVariable("AWS_CONTAINER_CREDENTIALS_RELATIVE_URI");
            if (relativeUri == null)
            {
                return null;
            }

            var response = AwsHttpClientHelper.GetECSResponseAsync(relativeUri).GetAwaiter().GetResult();
            var parsedResponse = BsonDocument.Parse(response);
            var accessKeyId = parsedResponse.GetValue("AccessKeyId", null)?.AsString;
            var secretAccessKey = parsedResponse.GetValue("SecretAccessKey", null)?.AsString;
            var sessionToken = parsedResponse.GetValue("Token", null)?.AsString;

            return new AwsCredentials(accessKeyId, SecureStringHelper.ToSecureString(secretAccessKey), sessionToken);
        }

        private static AwsCredentials CreateAwsCredentialsFromEc2Response()
        {
            var response = AwsHttpClientHelper.GetEC2ResponseAsync().GetAwaiter().GetResult();
            var parsedResponse = BsonDocument.Parse(response);
            var accessKeyId = parsedResponse.GetValue("AccessKeyId", null)?.AsString;
            var secretAccessKey = parsedResponse.GetValue("SecretAccessKey", null)?.AsString;
            var sessionToken = parsedResponse.GetValue("Token", null)?.AsString;

            return new AwsCredentials(accessKeyId, SecureStringHelper.ToSecureString(secretAccessKey), sessionToken);
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

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MongoAWSAuthenticator"/> class.
        /// </summary>
        /// <param name="credential">The credentials.</param>
        /// <param name="properties">The properties.</param>
        [Obsolete("Use the newest overload instead.")]
        public MongoAWSAuthenticator(UsernamePasswordCredential credential, IEnumerable<KeyValuePair<string, string>> properties)
            : this(credential, properties, serverApi: null)
        {
        }

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
            : this(credential, properties, new DefaultRandomByteGenerator(), SystemClock.Instance, serverApi)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoAWSAuthenticator"/> class.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="properties">The properties.</param>
        [Obsolete("Use the newest overload instead.")]
        public MongoAWSAuthenticator(string username, IEnumerable<KeyValuePair<string, string>> properties)
            : this(username, properties, serverApi: null)
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
            : this(username, properties, new DefaultRandomByteGenerator(), SystemClock.Instance, serverApi)
        {
        }

        internal MongoAWSAuthenticator(
            UsernamePasswordCredential credential,
            IEnumerable<KeyValuePair<string, string>> properties,
            IRandomByteGenerator randomByteGenerator,
            IClock clock,
            ServerApi serverApi)
            : base(CreateMechanism(credential, properties, randomByteGenerator, clock), serverApi)
        {
        }

        internal MongoAWSAuthenticator(
            string username,
            IEnumerable<KeyValuePair<string, string>> properties,
            IRandomByteGenerator randomByteGenerator,
            IClock clock,
            ServerApi serverApi)
            : base(CreateMechanism(username, null, properties, randomByteGenerator, clock), serverApi)
        {
        }

        /// <inheritdoc/>
        public override string DatabaseName
        {
            get { return "$external"; }
        }

        // nested classes
        private class AwsCredentials
        {
            private readonly string _accessKeyId;
            private readonly SecureString _secretAccessKey;
            private readonly string _sessionToken;

            public AwsCredentials(string accessKeyId, SecureString secretAccessKey, string sessionToken)
            {
                _accessKeyId = Ensure.IsNotNull(accessKeyId, nameof(accessKeyId));
                _secretAccessKey = Ensure.IsNotNull(secretAccessKey, nameof(secretAccessKey));
                _sessionToken = sessionToken; // can be null
            }

            public string AccessKeyId => _accessKeyId;
            public SecureString SecretAccessKey => _secretAccessKey;
            public string SessionToken => _sessionToken;
        }

        private static class AwsHttpClientHelper
        {
            // private static
            private static readonly Uri __ec2BaseUri = new Uri("http://169.254.169.254");
            private static readonly Uri __ecsBaseUri = new Uri("http://169.254.170.2");
            private static readonly Lazy<HttpClient> __httpClientInstance = new Lazy<HttpClient>(() => new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            });

            public static async Task<string> GetEC2ResponseAsync()
            {
                var tokenRequest = CreateTokenRequest(__ec2BaseUri);
                var token = await GetHttpContentAsync(tokenRequest, "Failed to acquire EC2 token.").ConfigureAwait(false);

                var roleRequest = CreateRoleRequest(__ec2BaseUri, token);
                var roleName = await GetHttpContentAsync(roleRequest, "Failed to acquire EC2 role name.").ConfigureAwait(false);

                var credentialsRequest = CreateCredentialsRequest(__ec2BaseUri, roleName, token);
                var credentials = await GetHttpContentAsync(credentialsRequest, "Failed to acquire EC2 credentials.").ConfigureAwait(false);

                return credentials;
            }

            public static async Task<string> GetECSResponseAsync(string relativeUri)
            {
                var credentialsRequest = new HttpRequestMessage
                {
                    RequestUri = new Uri(__ecsBaseUri, relativeUri),
                    Method = HttpMethod.Get
                };

                return await GetHttpContentAsync(credentialsRequest, "Failed to acquire ECS credentials.").ConfigureAwait(false);
            }

            // private static methods
            private static HttpRequestMessage CreateCredentialsRequest(Uri baseUri, string roleName, string token)
            {
                var credentialsUri = new Uri(baseUri, "latest/meta-data/iam/security-credentials/");
                var credentialsRequest = new HttpRequestMessage
                {
                    RequestUri = new Uri(credentialsUri, roleName),
                    Method = HttpMethod.Get
                };
                credentialsRequest.Headers.Add("X-aws-ec2-metadata-token", token);

                return credentialsRequest;
            }

            private static HttpRequestMessage CreateRoleRequest(Uri baseUri, string token)
            {
                var roleRequest = new HttpRequestMessage
                {
                    RequestUri = new Uri(baseUri, "latest/meta-data/iam/security-credentials/"),
                    Method = HttpMethod.Get
                };
                roleRequest.Headers.Add("X-aws-ec2-metadata-token", token);

                return roleRequest;
            }

            private static HttpRequestMessage CreateTokenRequest(Uri baseUri)
            {
                var tokenRequest = new HttpRequestMessage
                {
                    RequestUri = new Uri(baseUri, "latest/api/token"),
                    Method = HttpMethod.Put,
                };
                tokenRequest.Headers.Add("X-aws-ec2-metadata-token-ttl-seconds", "30");

                return tokenRequest;
            }

            private static async Task<string> GetHttpContentAsync(HttpRequestMessage request, string exceptionMessage)
            {
                HttpResponseMessage response;
                try
                {
                    response = await __httpClientInstance.Value.SendAsync(request).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                }
                catch (Exception ex) when (ex is OperationCanceledException || ex is MongoClientException)
                {
                    throw new MongoClientException(exceptionMessage, ex);
                }

                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
        }

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
