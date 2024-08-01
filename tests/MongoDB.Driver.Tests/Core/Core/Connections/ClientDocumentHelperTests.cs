/* Copyright 2016-present MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Tests;
using MongoDB.TestHelpers.XunitExtensions;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Connections
{
    public class ClientDocumentHelperTests
    {
        [Fact]
        public void CreateClientDocument_should_return_expected_result()
        {
            ClientDocumentHelper.Initialize();
            var result = ClientDocumentHelper.CreateClientDocument(null, null);

            var names = result.Names.ToList();
            names.Count.Should().Be(3);
            names[0].Should().Be("driver");
            names[1].Should().Be("os");
            names[2].Should().Be("platform");
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateClientDocument_with_args_should_return_expected_result(
            [Values(null, "app1", "app2")]
            string applicationName,
            [Values("{ name : 'mongo-csharp-driver', version : '3.6.0' }", "{ name : 'mongo-csharp-driver', version : '3.6.1' }")]
            string driverDocumentString,
            [Values(
                "{ type : 'Windows', name : 'Windows 10', architecture : 'x86_64', version : '10.0' }",
                "{ type : 'Windows', name : 'Windows 10', architecture : 'x86_64', version : '10.1' }")]
            string osDocumentString,
            [Values("net45", "net46")]
            string platformString,
            [Values(null, "aws.lambda", "versel")]
            string env,
            [Values(null, "libName;", "libName;1.0.0")]
            string libraryInfoString)
        {
            var driverDocument = BsonDocument.Parse(driverDocumentString);
            var osDocument = BsonDocument.Parse(osDocumentString);
            var (libraryInfo, driverDocumentStringCombined) = ParselibraryInfo();

            var envDocument = env != null ? new BsonDocument("name", env) : null;
            var result = ClientDocumentHelper.CreateClientDocument(applicationName, driverDocument, osDocument, platformString, envDocument, libraryInfo);

            var applicationNameElement = applicationName == null ? null : $"application : {{ name : '{applicationName}' }},";
            var envElement = envDocument == null ? null : $", env : {{ name : '{env}' }}";
            var expectedResult = $"{{ {applicationNameElement} driver : {driverDocumentStringCombined}, os : {osDocumentString}, platform : '{platformString}'{envElement} }}";
            result.Should().Be(expectedResult);

            (LibraryInfo libraryInfo, string DriverDocument) ParselibraryInfo()
            {
                if (libraryInfoString == null)
                {
                    return (null, driverDocumentString);
                }

                var parts = libraryInfoString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                var name = parts[0];
                var version = parts.Length > 1 ? parts[1] : null;

                var driverDocumentCombined = new BsonDocument()
                {
                    { "name", $"{driverDocument["name"]}|{name}" },
                    { "version", version != null ? $"{driverDocument["version"]}|{version}" : driverDocument["version"] }
                };

                return new(new(name, version), driverDocumentCombined.ToString());
            }
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("lib1", "1.0")]
        [InlineData("lib2", null)]
        public void CreateClientDocument_with_library_info_should_return_expected_result(string libName, string libVersion)
        {
            var expectedDriverName = libName == null ? "mongo-csharp-driver" : $"mongo-csharp-driver|{libName}";

            var libraryInfo = libName != null ? new LibraryInfo(libName, libVersion) : null;

            ClientDocumentHelper.Initialize();
            var driverDocument = ClientDocumentHelper.CreateClientDocument(null, libraryInfo)["driver"];

            driverDocument["name"].AsString.Should().Be(expectedDriverName);

            if (libVersion != null)
            {
                driverDocument["version"].AsString.Should().EndWith(libVersion);
            }
            else
            {
                driverDocument["version"].AsString.Should().NotContain("|");
            }
        }

        [Fact]
        public void CreateDriverDocument_should_return_expected_result()
        {
            var result = ClientDocumentHelper.CreateDriverDocument();

            result["name"].AsString.Should().Be("mongo-csharp-driver");
            result["version"].BsonType.Should().Be(BsonType.String);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateDriverDocument_with_args_should_return_expected_result(
            [Values("3.6.0", "3.6.1")]
            string driverVersion)
        {
            var result = ClientDocumentHelper.CreateDriverDocument(driverVersion);

            result.Should().Be($"{{ name : 'mongo-csharp-driver', version : '{driverVersion}' }}");
        }

        [Theory]
        [InlineData(true, true, "{ name : 'vercel', container : { \"runtime\" : \"docker\", \"orchestrator\" : \"kubernetes\" }}")]
        [InlineData(false, true, "{ name : 'vercel', container : { \"orchestrator\" : \"kubernetes\" }}")]
        [InlineData(true, false, "{ name : 'vercel', container : { \"runtime\" : \"docker\" }}")]
        [InlineData(false, false, "{ name : 'vercel' }")]
        public void CreateEnvDocument_should_return_expected_result(bool isDockerToBeDetected, bool isKubernetesToBeDetected, string expected)
        {
            var fileSystemProviderMock  = new Mock<IFileSystemProvider>();
            var environmentVariableProviderMock = new Mock<IEnvironmentVariableProvider>();

            environmentVariableProviderMock.Setup(env => env.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST")).Returns(isKubernetesToBeDetected ? "dummy" : null);
            environmentVariableProviderMock.Setup(env => env.GetEnvironmentVariable("VERCEL")).Returns("dummy");

            fileSystemProviderMock.Setup(fileSystem => fileSystem.File.Exists("/.dockerenv")).Returns(isDockerToBeDetected);

            using var __ = new ClientDocumentHelperProvidersSetter(environmentVariableProviderMock.Object, fileSystemProviderMock.Object);

            var result = ClientDocumentHelper.CreateEnvDocument();
            result.Should().Be(expected);
        }

        [Fact]
        public void CreateOSDocument_should_return_expected_result()
        {
            var result = ClientDocumentHelper.CreateOSDocument();

            var names = result.Names.ToList();
            names.Should().Contain("type");
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateOSDocument_with_args_should_return_expected_result(
            [Values("Windows", "Linux")]
            string osType,
            [Values("Windows 10", "macOS")]
            string osName,
            [Values("x86_32", "x86_64")]
            string architecture,
            [Values("8.1", "10.0")]
            string osVersion)
        {
            var result = ClientDocumentHelper.CreateOSDocument(osType, osName, architecture, osVersion);

            result.Should().Be($"{{ type : '{osType}', name : '{osName}', architecture : '{architecture}', version : '{osVersion}' }}");
        }

        [Fact]
        public void GetPlatformString_should_return_expected_result()
        {
            var result = ClientDocumentHelper.GetPlatformString();

            result.Should().NotBeNull();
        }

        [Theory]
        [ParameterAttributeData]
        public void RemoveOneOptionalField_should_return_expected_result(
            [Range(0, 5)]
            int timesCalled)
        {
            var clientDocument = CreateClientDocument();

            var result = clientDocument;
            for (var i = 0; i < timesCalled; i++)
            {
                result = ClientDocumentHelper.RemoveOneOptionalField(result);
            }

            var expectedResult = CreateClientDocument();
            if (timesCalled < 5)
            {
                var optionalFieldNames = new[] { "!env.name", "!os.type", "env.name", "platform"};
                for (var i = 0; i < timesCalled; i++)
                {
                    var dottedFieldName = optionalFieldNames[i];
                    RemoveField(expectedResult, dottedFieldName);
                }
            }
            else
            {
                expectedResult = null;
            }
            result.Should().Be(expectedResult);
        }

        const string awsEnv = "AWS_EXECUTION_ENV=AWS_Lambda_java8";
        const string azureEnv = "FUNCTIONS_WORKER_RUNTIME";
        const string gcpEnv = "K_SERVICE";
        const string vercelEnv = "VERCEL";

        const string awsLambdaName = "aws.lambda";
        const string azureFuncName = "azure.func";
        const string gcpFuncName = "gcp.func";
        const string vercelName = "vercel";

        [Theory]
        [ParameterAttributeData]
        public void Prefer_vercel_over_aws_env_name_when_both_specified(
            [Values(awsEnv, azureEnv, gcpEnv, vercelEnv)] string left,
            [Values(awsEnv, azureEnv, gcpEnv, vercelEnv)] string right)
        {
            var variableLeftParts = left.Split('=');
            var variableRightParts = right.Split('=');

            var environmentVariableProviderMock = new Mock<IEnvironmentVariableProvider>();

            environmentVariableProviderMock
                .Setup(env => env.GetEnvironmentVariable(variableLeftParts[0])).Returns(variableLeftParts.Length > 1 ? variableLeftParts[1] : "dummy");

            environmentVariableProviderMock
                .Setup(env => env.GetEnvironmentVariable(variableRightParts[0])).Returns(variableRightParts.Length > 1 ? variableRightParts[1] : "dummy");

            using var __ = new ClientDocumentHelperProvidersSetter(environmentVariableProviderMock.Object);

            var clientEnvDocument = ClientDocumentHelper.CreateEnvDocument();
            if (left == right)
            {
                var expectedName = left switch
                {
                    awsEnv => awsLambdaName,
                    azureEnv => azureFuncName,
                    gcpEnv => gcpFuncName,
                    vercelEnv => vercelName,
                    _ => throw new Exception($"Unexpected env {left}."),
                };
                clientEnvDocument["name"].Should().Be(BsonValue.Create(expectedName));
            }
            else if ((left == awsEnv && right == vercelEnv) || (left == vercelEnv && right == awsEnv)) // exception
            {
                clientEnvDocument["name"].Should().Be(BsonValue.Create(vercelName));
            }
            else
            {
                clientEnvDocument.Should().BeNull();
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void RemoveOptionalFieldsUntilDocumentIsLessThan512Bytes_should_return_expected_result(
            [Range(-1, 3)]
            int largeOptionalFieldNameIndex)
        {
            var optionalFieldNames = new[] { "env.region", "os.version", "env.name", "platform" };
            var removingOrder = new[] { "!env.name", "!os.type", "env.name", "platform", };
            var clientDocument = CreateClientDocument();
            if (largeOptionalFieldNameIndex != -1)
            {
                var largeOptionalFieldName = optionalFieldNames[largeOptionalFieldNameIndex];
                SetField(clientDocument, largeOptionalFieldName, new string('x', 512));
            }

            var result = ClientDocumentHelper.RemoveOptionalFieldsUntilDocumentIsLessThan512Bytes(clientDocument);

            var expectedResult = CreateClientDocument();
            if (largeOptionalFieldNameIndex != -1)
            {
                for (var i = 0; i <= largeOptionalFieldNameIndex; i++)
                {
                    RemoveField(expectedResult, removingOrder[i]);
                }
            }
            result.Should().Be(expectedResult);
        }

        // private methods
        private BsonDocument CreateClientDocument() =>
            new BsonDocument
            {
                { "application", new BsonDocument("name", "app") },
                {
                    "driver",
                    new BsonDocument
                    {
                        { "name", "mongo-csharp-driver" },
                        { "version", "3.6.x" }
                    }
                },
                {
                    "os",
                    new BsonDocument
                    {
                        { "type", "Windows" },
                        { "name", "Windows 10.0" },
                        { "architecture", "x86_64" },
                        { "version", "10.0" }
                    }
                },
                { "platform", ".NET Framework" },
                {
                    "env",
                    new BsonDocument
                    {
                        { "name", "aws.lambda" },
                        { "region", "us-east-2" }
                    }
                }
            };

        private BsonDocument NavigateDots(BsonDocument document, string dottedFieldName, out string fieldName)
        {
            fieldName = dottedFieldName;
            while (fieldName.Contains("."))
            {
                var dotIndex = fieldName.IndexOf('.');
                var prefixName = fieldName.Substring(0, dotIndex);
                document = document[prefixName].AsBsonDocument;
                fieldName = fieldName.Substring(dotIndex + 1);
            }
            return document;
        }

        private void RemoveField(BsonDocument document, string dottedFieldName)
        {
            string fieldName;
            var leaveOnlyMode = dottedFieldName[0] == '!';
            if (leaveOnlyMode)
            {
                dottedFieldName = dottedFieldName.TrimStart('!');
            }
            var initialDocument = document;
            document = NavigateDots(document, dottedFieldName, out fieldName);
            if (leaveOnlyMode)
            {
                RemoveAll(document, protectedField: fieldName);
            }
            else
            {
                document.Remove(fieldName);
                if (document.ElementCount == 0)
                {
                    var dotIndex = dottedFieldName.IndexOf('.');
                    if (dotIndex != -1)
                    {
                        var prefix = dottedFieldName.Substring(0, dotIndex);
                        initialDocument.Remove(prefix);
                    }
                }
            }

            static void RemoveAll(BsonDocument document, string protectedField = null)
            {
                for (int i = document.ElementCount - 1; i >= 0; i--)
                {
                    var element = document.GetElement(i);
                    if (protectedField == null || element.Name != protectedField)
                    {
                        document.RemoveElement(element);
                    }
                }
            }
        }

        private void SetField(BsonDocument document, string dottedFieldName, BsonValue value)
        {
            string fieldName;
            document = NavigateDots(document, dottedFieldName, out fieldName);
            document[fieldName] = value;
        }
    }
}
