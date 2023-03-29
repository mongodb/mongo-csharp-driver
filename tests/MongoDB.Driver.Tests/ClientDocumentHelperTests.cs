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
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class ClientDocumentHelperTests
    {
        [Theory]
        [ParameterAttributeData]
        public async Task Handhake_should_handle_faas_env_variables(
            [Values(
            // Valid AWS
            "{ 'AWS_EXECUTION_ENV' : 'AWS_Lambda_java8', 'AWS_REGION' : 'us-east-2', 'AWS_LAMBDA_FUNCTION_MEMORY_SIZE' : '1024', expected : { 'name' : 'aws.lambda', 'memory_mb' : 1024, 'region' : 'us-east-2' } }",
            // Valid Azure
            "{ 'FUNCTIONS_WORKER_RUNTIME' : 'node', expected: { 'name' : 'azure.func' } }",
            // Valid GCP
            "{ 'K_SERVICE' : 'servicename', 'FUNCTION_MEMORY_MB' : '1024', 'FUNCTION_TIMEOUT_SEC' : '60', 'FUNCTION_REGION' : 'us-central1', expected: { 'name' : 'gcp.func', 'timeout_sec' : 60, 'memory_mb' : 1024, 'region' : 'us-central1' } }",
            // Valid VERCEL
            "{ 'VERCEL' : '1', 'VERCEL_URL' : '*.vercel.app', 'VERCEL_REGION' : 'cdg1', expected: { 'name' : 'vercel', 'region' : 'cdg1', 'url' : '*.vercel.app' } }",
            // Invalid - multiple providers
            "{ 'AWS_EXECUTION_ENV' : 'AWS_Lambda_java8', 'FUNCTIONS_WORKER_RUNTIME' : 'node', expected: null }",
            // Invalid - long string
            "{ 'AWS_EXECUTION_ENV' : 'AWS_Lambda_java8', 'AWS_REGION' : '#longA#', expected: { 'name' : 'aws.lambda' } }",
            // Invalid - wrong types
            "{ 'AWS_EXECUTION_ENV' : 'AWS_Lambda_java8', 'AWS_LAMBDA_FUNCTION_MEMORY_SIZE' : 'big', expected: { 'name' : 'aws.lambda' } }")]
            string environmentVariableDescription,
            [Values(false, true)] bool async)
        {
            ClientDocumentHelperReflector.Initialize();

            var environmentVariableDescriptionDocument = BsonDocument.Parse(environmentVariableDescription);
            var expectedValue = environmentVariableDescriptionDocument["expected"];
            environmentVariableDescriptionDocument.Remove("expected");
            var descriptionElements = environmentVariableDescriptionDocument
                .Elements
                .ToList();
            descriptionElements
                // Ensure a test is not launched on actual env
                .ForEach(e => RequireEnvironment.Check().EnvironmentVariable(e.Name, isDefined: false));
            var bundleElements = descriptionElements.Select(e => new DisposableEnvironmentVariable(e.Name, GetValue(e.Value.ToString()))).ToList();

            var eventCapturer = new EventCapturer()
                .Capture<CommandStartedEvent>(e => e.CommandName == OppressiveLanguageConstants.LegacyHelloCommandName || e.CommandName == "hello");

            using (var bundle = new DisposableBundle(bundleElements))
            using (var client = DriverTestConfiguration.CreateDisposableClient(eventCapturer))
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

            string GetValue(string input) => input == "#longA#" ? new string('a', 512) : input;
        }

        // nested type
        private class DisposableEnvironmentVariable : IDisposable
        {
            private readonly string _initialValue;
            private readonly string _name;

            public DisposableEnvironmentVariable(string name, string value)
            {
                _name = name;
                _initialValue = Environment.GetEnvironmentVariable(name);
                Environment.SetEnvironmentVariable(name, value);
            }

            public void Dispose() => Environment.SetEnvironmentVariable(_name, _initialValue);
        }
    }

    internal static class ClientDocumentHelperReflector
    {
        public static void Initialize() => Reflector.InvokeStatic(typeof(ClientDocumentHelper), nameof(Initialize));
    }   
}
