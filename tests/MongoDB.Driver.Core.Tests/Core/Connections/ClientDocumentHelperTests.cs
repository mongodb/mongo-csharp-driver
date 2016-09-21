/* Copyright 2016 MongoDB Inc.
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
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Connections
{
    public class ClientDocumentHelperTests
    {
        [Fact]
        public void CreateClientDocument_should_return_expected_result()
        {
            var result = ClientDocumentHelper.CreateClientDocument(null);

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
            [Values("{ name : 'mongo-csharp-driver', version : '2.4.0' }", "{ name : 'mongo-csharp-driver', version : '2.4.1' }")]
            string driverDocumentString,
            [Values(
                "{ type : 'Windows', name : 'Windows 10', architecture : 'x86_64', version : '10.0' }",
                "{ type : 'Windows', name : 'Windows 10', architecture : 'x86_64', version : '10.1' }")]
            string osDocumentString,
            [Values("net45", "net46")]
            string platformString)
        {
            var driverDocument = BsonDocument.Parse(driverDocumentString);
            var osDocument = BsonDocument.Parse(osDocumentString);

            var result = ClientDocumentHelper.CreateClientDocument(applicationName, driverDocument, osDocument, platformString);

            var applicationNameElement = applicationName == null ? null : $"application : {{ name : '{applicationName}' }},";
            var expectedResult = $"{{ {applicationNameElement} driver : {driverDocumentString}, os : {osDocumentString}, platform : '{platformString}' }}";
            result.Should().Be(expectedResult);
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
            [Values("2.4.0", "2.4.1")]
            string driverVersion)
        {
            var result = ClientDocumentHelper.CreateDriverDocument(driverVersion);

            result.Should().Be($"{{ name : 'mongo-csharp-driver', version : '{driverVersion}' }}");
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
            [Range(0, 6)]
            int timesCalled)
        {
            var clientDocument = CreateClientDocument();

            var result = clientDocument;
            for (var i = 0; i < timesCalled; i++)
            {
                result = ClientDocumentHelper.RemoveOneOptionalField(result);
            }

            var expectedResult = CreateClientDocument();
            if (timesCalled < 6)
            {
                var optionalFieldNames = new[] { "application", "os.version", "os.architecture", "os.name", "platform" };
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

        [Theory]
        [ParameterAttributeData]
        public void RemoveOptionalFieldsUntilDocumentIsLessThan512Bytes_should_return_expected_result(
            [Range(-1, 4)]
            int largeOptionalFieldNameIndex)
        {
            var optionalFieldNames = new[] { "application", "os.version", "os.architecture", "os.name", "platform" };
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
                    RemoveField(expectedResult, optionalFieldNames[i]);
                }
            }
            result.Should().Be(expectedResult);
        }

        // private methods
        private BsonDocument CreateClientDocument()
        {
            return new BsonDocument
            {
                { "application", new BsonDocument("name", "app") },
                { "driver", new BsonDocument
                    {
                        { "name", "mongo-csharp-driver" },
                        { "version", "2.4.x" }
                    }
                },
                { "os", new BsonDocument
                    {
                        { "type", "Windows" },
                        { "name", "Windows 10.0" },
                        { "architecture", "x86_64" },
                        { "version", "10.0" }
                    }
                },
                { "platform", ".NET Framework" }
            };
        }

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
            document = NavigateDots(document, dottedFieldName, out fieldName);
            document.Remove(fieldName);
        }

        private void SetField(BsonDocument document, string dottedFieldName, BsonValue value)
        {
            string fieldName;
            document = NavigateDots(document, dottedFieldName, out fieldName);
            document[fieldName] = value;
        }
    }
}
