/* Copyright 2020-present MongoDB Inc.
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
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Communication.Security
{
    /// <summary>
    /// AWS.SDK notes:
    /// 1. run-aws-auth-test-with-regular-aws-credentials: only driver side logic
    /// 2. run-aws-auth-test-with-assume-role-credentials: only driver side logic
    /// The below use AWS.SDK calls
    /// 3. run-aws-auth-test-with-aws-credentials-as-environment-variables: EnvironmentVariablesAWSCredentials
    /// 4. run-aws-auth-test-with-aws-credentials-and-session-token-as-environment-variables: EnvironmentVariablesAWSCredentials
    /// 5. run-aws-auth-test-with-aws-EC2-credentials: DefaultInstanceProfileAWSCredentials.Instance (EC2)
    /// 6. run-aws-auth-test-with-aws-ECS-credentials: ECSTaskCredentials
    /// 7. run-aws-auth-test-with-aws-web-identity-credentials: AssumeRoleWithWebIdentityCredentials.FromEnvironmentVariables
    /// </summary>
    [Trait("Category", "Authentication")]
    [Trait("Category", "AwsMechanism")]
    public class AwsAuthenticationTests
    {
        [Fact]
        public void Aws_authentication_should_should_have_expected_result()
        {
            RequireEnvironment.Check().EnvironmentVariable("AWS_TESTS_ENABLED");

            using (var client = DriverTestConfiguration.CreateDisposableClient())
            {
                // test that a command that doesn't require auth completes normally
                var adminDatabase = client.GetDatabase("admin");
                var pingCommand = new BsonDocument("ping", 1);
                var pingResult = adminDatabase.RunCommand<BsonDocument>(pingCommand);

                // test that a command that does require auth completes normally
                var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
                var collection = database.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);
                var count = collection.CountDocuments(FilterDefinition<BsonDocument>.Empty);
            }
        }

        [Fact]
        public void Ecs_should_fill_AWS_CONTAINER_CREDENTIALS_RELATIVE_URI()
        {
            var isEcs = Environment.GetEnvironmentVariable("AWS_ECS_ENABLED") != null;
            var awsContainerUri = Environment.GetEnvironmentVariable("AWS_CONTAINER_CREDENTIALS_RELATIVE_URI") ?? Environment.GetEnvironmentVariable("AWS_CONTAINER_CREDENTIALS_FULL_URI");
            (awsContainerUri != null).Should().Be(isEcs);
        }

        [Fact]
        public void AwsSdk_should_support_all_required_handlers()
        {
            var credentialsGeneratorsDelegatesEnumerator = FallbackCredentialsFactory.CredentialsGenerators.GetEnumerator();

            // AppConfigAWSCredentials
            AWSCredentials credentials = null;
            if (Type.GetType("Amazon.Runtime.AppConfigAWSCredentials, AWSSDK.Core", throwOnError: false) != null) // app.config/web.config does not present on windows
            {
                var appConfigAWSCredentialsException = Record.Exception(() => RunTestCase());
                // app.config/web.config case is based on ConfigurationManager. This is not configured for this test
                appConfigAWSCredentialsException.Message.Should().Contain("The app.config/web.config files for the application did not contain credential information");
            }

            // AssumeRoleWithWebIdentityCredentials.FromEnvironmentVariables()
            var exception = Record.Exception(() => RunTestCase());
            if (Environment.GetEnvironmentVariable("AWS_WEB_IDENTITY_TOKEN_FILE") != null)
            {
                // aws-web-identity-credentials is configured
                exception.Should().BeNull();
                credentials.Should().BeOfType<AssumeRoleWithWebIdentityCredentials>();
            }
            else
            {
                // otherwise fail
                exception.Message.Should().Contain("webIdentityTokenFile");
            }

            // GetAWSCredentials (Profile)
            exception = Record.Exception(() => RunTestCase());
            if (IsWithAwsProfileOnMachine())
            {
                // current machine contains configured aws profile, which may include:
                // 1. BasicAWSCredentials (aws_access_key_id and aws_secret_access_key)
                // 2. SessionAWSCredentials (aws_access_key_id, aws_secret_access_key, aws_session_token)
                exception.Should().BeNull();
                credentials.Should().Match(x => x is BasicAWSCredentials || x is SessionAWSCredentials);
            }
            else
            {
                // otherwise fail
                exception.Message.Should().Contain("Credential").And.Subject.Should().Contain("profile");
            }

            // EnvironmentVariablesAWSCredentials
            exception = Record.Exception(() => RunTestCase());
            if (Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID") != null && Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY") != null)
            {
                // environment variables code path
                exception.Should().BeNull();
                credentials.Should().BeOfType<EnvironmentVariablesAWSCredentials>();
            }
            else
            {
                // otherwise fail
                exception.Message.Should().Contain("The environment variables").And.Subject.Contains("were not set with AWS credentials");
            }

            // ECSEC2CredentialsWrapper
            exception = Record.Exception(() => RunTestCase());
            if (Environment.GetEnvironmentVariable("AWS_CONTAINER_CREDENTIALS_RELATIVE_URI") != null || Environment.GetEnvironmentVariable("AWS_CONTAINER_CREDENTIALS_FULL_URI") != null)
            {
                exception.Should().BeNull();
                credentials.Should().BeOfType<ECSTaskCredentials>();
            }
            else
            {
                exception.Should().BeNull();
                credentials.GetType().Name.Should().Contain("DefaultInstanceProfileAWSCredentials"); // EC2 case
            }

            credentialsGeneratorsDelegatesEnumerator.MoveNext().Should().BeFalse(); // no more handlers

            bool IsWithAwsProfileOnMachine()
            {
                var credentialProfileChain = new CredentialProfileStoreChain();
                if (credentialProfileChain.TryGetProfile(Environment.GetEnvironmentVariable("AWS_PROFILE") ?? "default", out var profile))
                {
                    try
                    {
                        _ = profile.GetAWSCredentials(credentialProfileChain);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }

                return false;
            }

            void RunTestCase()
            {
                credentials = null;
                credentialsGeneratorsDelegatesEnumerator.MoveNext().Should().BeTrue();
                credentials = credentialsGeneratorsDelegatesEnumerator.Current();
            }
        }
    }
}
