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
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Examples.Aws
{
    /// <summary>
    /// Atlas preconditions for local run:
    /// 1. Configure AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY, and optionally AWS_SESSION_TOKEN. If used, AWS_SESSION_TOKEN should be regenerated periodically.
    /// You may use `Command line or programmatic access` page on awsapps.com
    /// 2. Configure credentials folder here: c:\Users\{user}\.aws\
    /// 3. Get your arn via `get-caller-identity`:
    /// ./aws sts get-caller-identity
    ///{
    ///    "UserId": "%ID%:[user@example.com]",
    ///    "Account": "%ID_VALUE%",
    ///    "Arn": "arn:aws:sts::%ID_VALUE%:assumed-role/%ROLE_NAME%/[user@example.com]"
    /// }
    /// pay attention on %ROLE_NAME%.
    /// 4. list all roles via:.
    /// $ ./aws iam list-roles
    /// {
    ///     "Roles": [
    ///     {
    ///             "Path": "..",
    ///             "RoleName": "%ROLE_NAME%",
    ///             "Arn": "arn:aws...:
    ///        ...
    ///  in the provided roles, search for a record with a RoleName equal to %ROLE_NAME% and record his arn.
    ///  5. In your atlas cluster, create a new user with AWS authentication and set AWS IAM Role ARN from #4.
    ///  6. Then configure a MongoClient in the same way as it's done in these examples with MONGODB-AWS auth credentials.
    ///
    /// Additional notes:
    /// 1. To work with authentications that are not based on env vars credentials configuration, make sure that AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY and AWS_SESSION_TOKEN are not set
    /// 2. To work with aws profile make sure that env variable AWS_PROFILE has appropriate value if the used aws profile is not default
    /// 3. To work with ECS container credentials make sure that AWS_CONTAINER_CREDENTIALS_RELATIVE_URI or AWS_CONTAINER_CREDENTIALS_FULL_URI has appropriate value
    /// 4. To work with EC2 container credentials from EC2 instance metadata make sure a test is launched on EC2 env and AWS_CONTAINER_CREDENTIALS_* is not set
    /// 5. To work with Aws WebIdentityToken make sure that AWS_WEB_IDENTITY_TOKEN_FILE, AWS_ROLE_ARN and AWS_ROLE_SESSION_NAME are configured
    /// </summary>
    public class AwsAuthenticationExamples
    {
        private static readonly string __connectionStringHosts = "<host_address>";

        [Fact]
        public void ConnectionStringAuthConfiguration()
        {
            // the test uses env variables only to initialize userName, password and awsSessionToken
            RequireEnvironment
                .Check()
                .EnvironmentVariable("AWS_ACCESS_KEY_ID")
                .EnvironmentVariable("AWS_SECRET_ACCESS_KEY")
                .EnvironmentVariable("AWS_SESSION_TOKEN");

            // Start explicit aws credentials configuring via connection string
            var username = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
            var password = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
            var awsSessionToken = Environment.GetEnvironmentVariable("AWS_SESSION_TOKEN");

            // AWS_SESSION_TOKEN is optional. If you do not need to specify an AWS session token, omit the authMechanismProperties parameter and his value.
            var connectionString = $"mongodb+srv://{username}:{password}@{__connectionStringHosts}?authSource=$external&authMechanism=MONGODB-AWS&authMechanismProperties=AWS_SESSION_TOKEN:{awsSessionToken}";
            var mongoClientSettings = MongoClientSettings.FromConnectionString(connectionString);
            var client = new MongoClient(mongoClientSettings);
            // End explicit aws credentials configuring via connection string

            RunHello(client);
        }

        [Fact]
        public void ConnectionStringAuthConfiguration_with_auto_fetching_env_variables()
        {
            // the test uses env variables fetched inside the driver behind the scene.
            // Auto fetching can happen not only from env variables, but adding only a check for them as a simplest guard
            RequireEnvironment
                .Check()
                .EnvironmentVariable("AWS_ACCESS_KEY_ID")
                .EnvironmentVariable("AWS_SECRET_ACCESS_KEY")
                .EnvironmentVariable("AWS_SESSION_TOKEN");  // optional

            // Start aws authentication configuring via connection string with implicit credentials fetching
            var connectionString = $"mongodb+srv://{__connectionStringHosts}?authSource=$external&authMechanism=MONGODB-AWS";
            var mongoClientSettings = MongoClientSettings.FromConnectionString(connectionString);
            var client = new MongoClient(mongoClientSettings);
            // End aws authentication configuring via connection string with implicit credentials fetching

            RunHello(client);
        }


        [Fact]
        public void MongoClientSettingsAuthConfiguration()
        {
            // the test uses env variable only to initialize userName, password and awsSessionToken
            RequireEnvironment
                .Check()
                .EnvironmentVariable("AWS_ACCESS_KEY_ID")
                .EnvironmentVariable("AWS_SECRET_ACCESS_KEY")
                .EnvironmentVariable("AWS_SESSION_TOKEN");

            // Start explicit aws credentials configuring via MongoCredential
            var username = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
            var password = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
            var awsSessionToken = Environment.GetEnvironmentVariable("AWS_SESSION_TOKEN");

            // AWS_SESSION_TOKEN is optional. If you do not need to specify an AWS session token, omit calling WithMechanismProperty method.
            var awsCredentials = new MongoCredential("MONGODB-AWS", new MongoExternalIdentity(username), new PasswordEvidence(password))
                .WithMechanismProperty("AWS_SESSION_TOKEN", awsSessionToken);

            var mongoClientSettings = MongoClientSettings.FromConnectionString($"mongodb+srv://{username}:{password}@{__connectionStringHosts}");
            mongoClientSettings.Credential = awsCredentials;
            mongoClientSettings.ServerApi = new ServerApi(ServerApiVersion.V1, strict: true);
            var client = new MongoClient(mongoClientSettings);
            // End explicit aws credentials configuring via MongoCredential

            RunHello(client);
        }

        [Fact]
        public void MongoClientSettingsAuthConfiguration_with_auto_fetching_env_variables()
        {
            // the test uses env variables fetched inside the driver behind the scene.
            // Auto fetching can happen not only from env variables, but adding only a check for them as a simplest guard
            RequireEnvironment
                .Check()
                .EnvironmentVariable("AWS_ACCESS_KEY_ID")
                .EnvironmentVariable("AWS_SECRET_ACCESS_KEY")
                .EnvironmentVariable("AWS_SESSION_TOKEN");  // optional

            // Start aws authentication configuring via MongoClientSettings with implicit credentials fetching
            var awsCredentials = new MongoCredential("MONGODB-AWS", new MongoExternalAwsIdentity(), new ExternalEvidence());

            var mongoClientSettings = MongoClientSettings.FromConnectionString($"mongodb+srv://{__connectionStringHosts}");
            mongoClientSettings.Credential = awsCredentials;
            mongoClientSettings.ServerApi = new ServerApi(ServerApiVersion.V1, strict: true);
            var client = new MongoClient(mongoClientSettings);
            // End aws authentication configuring via MongoClientSettings with implicit credentials fetching

            RunHello(client);
        }

        // private methods
        private void RunHello(IMongoClient client)
        {
            client.GetDatabase(DatabaseNamespace.Admin.DatabaseName).RunCommand<BsonDocument>("{ hello : 1 }");
        }
    }
}
