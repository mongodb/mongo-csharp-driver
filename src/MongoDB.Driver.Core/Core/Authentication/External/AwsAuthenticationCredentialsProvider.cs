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
using System.Net.Http;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Authentication.External
{
    internal class AwsCredentials : IExternalCredentials
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

        public BsonDocument GetKmsCredentials()
            => new BsonDocument
            {
                { "accessKeyId", _accessKeyId },
                { "secretAccessKey", SecureStringHelper.ToInsecureString(_secretAccessKey) },
                { "sessionToken", _sessionToken, _sessionToken != null }
            };
    }

    internal class AwsAuthenticationCredentialsProvider : IExternalAuthenticationCredentialsProvider<AwsCredentials>
    {
        private readonly AwsHttpClientHelper _awsHttpClientHelper;
        private readonly HttpClientHelper _httpClientHelper;

        public AwsAuthenticationCredentialsProvider(HttpClientHelper httpClientHelper)
        {
            _httpClientHelper = Ensure.IsNotNull(httpClientHelper, nameof(httpClientHelper));
            _awsHttpClientHelper = new AwsHttpClientHelper(_httpClientHelper);
        }

        public AwsCredentials CreateCredentialsFromExternalSource(CancellationToken cancellationToken) =>
            CreateCredentialsFromExternalSourceAsync(cancellationToken).GetAwaiter().GetResult();
        public async Task<AwsCredentials> CreateCredentialsFromExternalSourceAsync(CancellationToken cancellationToken)
        {
            return CreateAwsCredentialsFromEnvironmentVariables() ??
                (await CreateAwsCredentialsFromEcsResponseAsync(cancellationToken).ConfigureAwait(false)) ??
                (await CreateAwsCredentialsFromEc2ResponseAsync(cancellationToken).ConfigureAwait(false)) ??
                throw new InvalidOperationException($"Unable to find credentials for AWS authentication.");
        }

        private AwsCredentials CreateAwsCredentialsFromEnvironmentVariables()
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
                throw new InvalidOperationException($"When using AWS authentication if a secret access key is provided via environment variables then an access key ID must be provided also.");
            }
            if (accessKeyId != null && secretAccessKey == null)
            {
                throw new InvalidOperationException($"When using AWS authentication if an access key ID is provided via environment variables then a secret access key must be provided also.");
            }
            if (sessionToken != null && (accessKeyId == null || secretAccessKey == null))
            {
                throw new InvalidOperationException($"When using AWS authentication if a session token is provided via environment variables then an access key ID and a secret access key must be provided also.");
            }

            return new AwsCredentials(accessKeyId, SecureStringHelper.ToSecureString(secretAccessKey), sessionToken);
        }

        private async Task<AwsCredentials> CreateAwsCredentialsFromEcsResponseAsync(CancellationToken cancellationToken)
        {
            var relativeUri = Environment.GetEnvironmentVariable("AWS_CONTAINER_CREDENTIALS_RELATIVE_URI");
            if (relativeUri == null)
            {
                return null;
            }

            var response = await _awsHttpClientHelper.GetECSResponseAsync(relativeUri, cancellationToken).ConfigureAwait(false);
            return CreateAwsCreadentialsFromAwsResponse(response);
        }

        private async Task<AwsCredentials> CreateAwsCredentialsFromEc2ResponseAsync(CancellationToken cancellationToken)
        {
            var response = await _awsHttpClientHelper.GetEC2ResponseAsync(cancellationToken).ConfigureAwait(false);
            return CreateAwsCreadentialsFromAwsResponse(response);
        }

        private AwsCredentials CreateAwsCreadentialsFromAwsResponse(string awsResponse)
        {
            var parsedResponse = BsonDocument.Parse(awsResponse);
            var accessKeyId = parsedResponse.GetValue("AccessKeyId", null)?.AsString;
            var secretAccessKey = parsedResponse.GetValue("SecretAccessKey", null)?.AsString;
            var sessionToken = parsedResponse.GetValue("Token", null)?.AsString;

            return new AwsCredentials(accessKeyId, SecureStringHelper.ToSecureString(secretAccessKey), sessionToken);
        }

        // nested types
        private class AwsHttpClientHelper
        {
            // private static
            private static readonly Uri __ec2BaseUri = new Uri("http://169.254.169.254");
            private static readonly Uri __ecsBaseUri = new Uri("http://169.254.170.2");

            private readonly HttpClientHelper _httpClientHelper;

            public AwsHttpClientHelper(HttpClientHelper httpClientHelper) => _httpClientHelper = httpClientHelper;

            public async Task<string> GetEC2ResponseAsync(CancellationToken cancellationToken)
            {
                var tokenRequest = CreateTokenRequest(__ec2BaseUri);
                var token = await _httpClientHelper.GetHttpContentAsync(tokenRequest, "Failed to acquire EC2 token.", cancellationToken).ConfigureAwait(false);

                var roleRequest = CreateRoleRequest(__ec2BaseUri, token);
                var roleName = await _httpClientHelper.GetHttpContentAsync(roleRequest, "Failed to acquire EC2 role name.", cancellationToken).ConfigureAwait(false);

                var credentialsRequest = CreateCredentialsRequest(__ec2BaseUri, roleName, token);
                var credentials = await _httpClientHelper.GetHttpContentAsync(credentialsRequest, "Failed to acquire EC2 credentials.", cancellationToken).ConfigureAwait(false);

                return credentials;
            }

            public async Task<string> GetECSResponseAsync(string relativeUri, CancellationToken cancellationToken)
            {
                var credentialsRequest = new HttpRequestMessage
                {
                    RequestUri = new Uri(__ecsBaseUri, relativeUri),
                    Method = HttpMethod.Get
                };

                return await _httpClientHelper.GetHttpContentAsync(credentialsRequest, "Failed to acquire ECS credentials.", cancellationToken).ConfigureAwait(false);
            }

            // private static methods
            private HttpRequestMessage CreateCredentialsRequest(Uri baseUri, string roleName, string token)
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

            private HttpRequestMessage CreateRoleRequest(Uri baseUri, string token)
            {
                var roleRequest = new HttpRequestMessage
                {
                    RequestUri = new Uri(baseUri, "latest/meta-data/iam/security-credentials/"),
                    Method = HttpMethod.Get
                };
                roleRequest.Headers.Add("X-aws-ec2-metadata-token", token);

                return roleRequest;
            }

            private HttpRequestMessage CreateTokenRequest(Uri baseUri)
            {
                var tokenRequest = new HttpRequestMessage
                {
                    RequestUri = new Uri(baseUri, "latest/api/token"),
                    Method = HttpMethod.Put,
                };
                tokenRequest.Headers.Add("X-aws-ec2-metadata-token-ttl-seconds", "30");

                return tokenRequest;
            }
        }
    }
}
