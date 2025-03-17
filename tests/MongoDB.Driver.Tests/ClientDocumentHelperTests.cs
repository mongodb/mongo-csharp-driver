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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.TestHelpers.Core;
using MongoDB.TestHelpers.XunitExtensions;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class ClientDocumentHelperTests
    {
        private static readonly string __longAString = new string('a', 512);

        [Theory]
        [ParameterAttributeData]
        // Test that environment metadata is properly captured
        // https://github.com/mongodb/specifications/blob/master/source/mongodb-handshake/tests/README.md#test-1-test-that-environment-metadata-is-properly-captured
        public async Task Handhake_should_handle_faas_env_variables(
            [Values(
            // Valid AWS
            "{ 'env' : ['AWS_EXECUTION_ENV=AWS_Lambda_java8', 'AWS_REGION=us-east-2', 'AWS_LAMBDA_FUNCTION_MEMORY_SIZE=1024'], expected : { 'name' : 'aws.lambda', 'memory_mb' : 1024, 'region' : 'us-east-2' } }",
            // Invalid - AWS_EXECUTION_ENV should start with AWS_Lambda_
            "{ 'env' : ['AWS_EXECUTION_ENV=EC2', 'AWS_REGION=us-east-2'], expected : null }",
            // Valid Azure
            "{ 'env' : ['FUNCTIONS_WORKER_RUNTIME=node'], expected: { 'name' : 'azure.func' } }",
            // Valid GCP
            "{ 'env' : ['K_SERVICE=servicename', 'FUNCTION_MEMORY_MB=1024', 'FUNCTION_TIMEOUT_SEC=60', 'FUNCTION_REGION=us-central1'], expected: { 'name' : 'gcp.func', 'timeout_sec' : 60, 'memory_mb' : 1024, 'region' : 'us-central1' } }",
            // Valid VERCEL
            "{ 'env' : ['VERCEL=1', 'VERCEL_REGION=cdg1'], expected: { 'name' : 'vercel', 'region' : 'cdg1' } }",
            // Invalid - multiple providers
            "{ 'env' : ['AWS_EXECUTION_ENV=AWS_Lambda_java8', 'FUNCTIONS_WORKER_RUNTIME=node'], expected: null }",
            // Invalid - long string
            "{ 'env' : ['AWS_EXECUTION_ENV=AWS_Lambda_java8', 'AWS_REGION=#longA#'], expected: { 'name' : 'aws.lambda' } }",
            // Invalid - wrong types
            "{ 'env' : ['AWS_EXECUTION_ENV=AWS_Lambda_java8', 'AWS_LAMBDA_FUNCTION_MEMORY_SIZE=big'], expected: { 'name' : 'aws.lambda' } }",
            // Valid container and FaaS provider
            "{ 'env' : ['AWS_EXECUTION_ENV=AWS_Lambda_java8', 'AWS_REGION=us-east-2', 'AWS_LAMBDA_FUNCTION_MEMORY_SIZE=1024', 'KUBERNETES_SERVICE_HOST=1'], expected : { 'name' : 'aws.lambda', 'memory_mb' : 1024, 'region' : 'us-east-2', 'container' : { 'orchestrator' : 'kubernetes' } } }")]
            string environmentVariableDescription,
            [Values(false, true)] bool async)
        {
            RequireServer.Check().Authentication(authentication: false); // speculative authentication makes events asserting hard

            ClientDocumentHelperReflector.Initialize();

            var environmentVariableDescriptionDocument = BsonDocument.Parse(environmentVariableDescription);
            var expectedValue = environmentVariableDescriptionDocument["expected"];
            var env = environmentVariableDescriptionDocument["env"].AsBsonArray.Values.Select(v => v.AsString.Replace("#longA#", __longAString)).ToArray();
            var environmentVariableProviderMock = EnvironmentVariableProviderMock.Create(env);

            using var __ = new ClientDocumentHelperProvidersSetter(environmentVariableProviderMock.Object);

            var eventCapturer = new EventCapturer()
                .Capture<CommandStartedEvent>(e => e.CommandName == OppressiveLanguageConstants.LegacyHelloCommandName || e.CommandName == "hello");

            using (var client = DriverTestConfiguration.CreateMongoClient(eventCapturer))
            {
                var database = client.GetDatabase("db");
                if (async)
                {
                    _ = await database.RunCommandAsync<BsonDocument>("{ ping : 1 }");
                }
                else
                {
                    _ = database.RunCommand<BsonDocument>("{ ping : 1 }");
                }

                var command = eventCapturer.Next().Should().BeOfType<CommandStartedEvent>().Subject.Command["client"].AsBsonDocument;
                eventCapturer.Any().Should().BeFalse();
                if (expectedValue == BsonNull.Value)
                {
                    command.Should().NotContain("env");
                }
                else
                {
                    var expectedDocument = expectedValue.AsBsonDocument;
                    command["env"].AsBsonDocument.Should().Be(expectedDocument);
                }
            }
        }
    }

    internal static class ClientDocumentHelperReflector
    {
        public static void Initialize() => Reflector.InvokeStatic(typeof(ClientDocumentHelper), nameof(Initialize));
    }

    internal sealed class ClientDocumentHelperProvidersSetter : IDisposable
    {
        public ClientDocumentHelperProvidersSetter(IEnvironmentVariableProvider environmentVariableProvider, IFileSystemProvider fileSystemProvider = null)
        {
            if (environmentVariableProvider != null)
            {
                ClientDocumentHelper.SetEnvironmentVariableProvider(environmentVariableProvider);
            }

            if (fileSystemProvider != null)
            {
                ClientDocumentHelper.SetFileSystemProvider(fileSystemProvider);
            }
        }

        public void Dispose()
        {
            ClientDocumentHelper.SetEnvironmentVariableProvider(EnvironmentVariableProvider.Instance);
            ClientDocumentHelper.SetFileSystemProvider(FileSystemProvider.Instance);
        }
    }
}
