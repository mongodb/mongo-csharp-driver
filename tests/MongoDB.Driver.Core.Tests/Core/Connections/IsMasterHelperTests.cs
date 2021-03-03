/* Copyright 2018–present MongoDB Inc.
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

using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using Xunit;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Compression;
using MongoDB.Driver.Core.Configuration;

namespace MongoDB.Driver.Core.Connections
{
    public class IsMasterHelperTests
    {
        [Theory]
        [ParameterAttributeData]
        public void AddClientDocumentToCommand_with_custom_document_should_return_expected_result(
            [Values("{ client : { driver : 'dotnet', version : '2.4.0' }, os : { type : 'Windows' } }")]
            string clientDocumentString)
        {
            var clientDocument = BsonDocument.Parse(clientDocumentString);
            var command = IsMasterHelper.CreateCommand();
            var result = IsMasterHelper.AddClientDocumentToCommand(command, clientDocument);

            result.Should().Be($"{{ isMaster : 1, client : {clientDocumentString} }}");
        }

        [Fact]
        public void AddClientDocumentToCommand_with_ConnectionInitializer_client_document_should_return_expected_result()
        {
            var command = IsMasterHelper.CreateCommand();
            var connectionInitializer = new ConnectionInitializer("test", new CompressorConfiguration[0], serverApi: null);
            var subjectClientDocument = (BsonDocument)Reflector.GetFieldValue(connectionInitializer, "_clientDocument");
            var result = IsMasterHelper.AddClientDocumentToCommand(command, subjectClientDocument);

            var names = result.Names.ToList();
            names.Count.Should().Be(2);
            names[0].Should().Be("isMaster");
            names[1].Should().Be("client");
            result[0].Should().Be(1);
            var clientDocument = result[1].AsBsonDocument;
            var clientDocumentNames = clientDocument.Names.ToList();
            clientDocumentNames.Count.Should().Be(4);
            clientDocumentNames[0].Should().Be("application");
            clientDocumentNames[1].Should().Be("driver");
            clientDocumentNames[2].Should().Be("os");
            clientDocumentNames[3].Should().Be("platform");
            clientDocument["application"]["name"].AsString.Should().Be("test");
            clientDocument["driver"]["name"].AsString.Should().Be("mongo-csharp-driver");
            clientDocument["driver"]["version"].BsonType.Should().Be(BsonType.String);
        }

        [Theory]
        [ParameterAttributeData]
        public void AddCompressorsToCommand_with_compressors_should_return_expected_result(
            [Values(
                new CompressorType[] { },
                new [] { CompressorType.Zlib },
                new [] { CompressorType.Snappy},
                new [] { CompressorType.Zlib, CompressorType.Snappy },
                new [] { CompressorType.ZStandard, CompressorType.Snappy })]
            CompressorType[] compressorsParameters)
        {
            var command = IsMasterHelper.CreateCommand();
            var compressors =
                compressorsParameters
                    .Select(c => new CompressorConfiguration(c))
                    .ToArray();
            var result = IsMasterHelper.AddCompressorsToCommand(command, compressors);

            var expectedCompressions = string.Join(",", compressorsParameters.Select(c => $"'{CompressorTypeMapper.ToServerName(c)}'"));
            result.Should().Be(BsonDocument.Parse($"{{ isMaster : 1, compression: [{expectedCompressions}] }}"));
        }
    }
}
