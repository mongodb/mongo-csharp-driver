/* Copyright 2013-present MongoDB Inc.
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
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class ExplainOperationTests : OperationTestBase
    {
        private IExplainableOperation _explainableOperation;

        public ExplainOperationTests()
        {
            var databaseNamespace = new DatabaseNamespace("test");
            var collectionNamespace = new CollectionNamespace(databaseNamespace, "test");
            var resultSerializer = BsonDocumentSerializer.Instance;
            var messageEncoderSettings = new MessageEncoderSettings();
            _explainableOperation = new FindOperation<BsonDocument>(collectionNamespace, resultSerializer, messageEncoderSettings);
        }

        [Fact]
        public void Constructor_should_throw_when_collection_namespace_is_null()
        {
            Action action = () => new ExplainOperation(null, _explainableOperation, _messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_explainableOperation_is_null()
        {
            Action action = () => new ExplainOperation(_databaseNamespace, null, _messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_message_encoder_settings_is_null()
        {
            Action action = () => new ExplainOperation(_databaseNamespace, _explainableOperation, null);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_initialize_subject()
        {
            var subject = new ExplainOperation(_databaseNamespace, _explainableOperation, _messageEncoderSettings);

            subject.DatabaseNamespace.Should().Be(_databaseNamespace);
            subject.ExplainableOperation.Should().Be(_explainableOperation);
            subject.MessageEncoderSettings.Should().BeEquivalentTo(_messageEncoderSettings);
            subject.Verbosity.Should().Be(ExplainVerbosity.QueryPlanner);
        }

        [Theory]
        [InlineData(ExplainVerbosity.AllPlansExecution, "allPlansExecution")]
        [InlineData(ExplainVerbosity.ExecutionStats, "executionStats")]
        [InlineData(ExplainVerbosity.QueryPlanner, "queryPlanner")]
        public void CreateCommand_should_return_expected_result(ExplainVerbosity verbosity, string verbosityString)
        {
            var subject = new ExplainOperation(_databaseNamespace, _explainableOperation, _messageEncoderSettings)
            {
                Verbosity = verbosity
            };

            var expectedResult = new BsonDocument
            {
                { "explain", BsonDocument.Parse("{ find : 'test' }") },
                { "verbosity", verbosityString }
            };

            var endPoint = new DnsEndPoint("localhost", 27017);
            var serverId = new ServerId(new ClusterId(), endPoint);
            var connectionId = new ConnectionId(serverId);
            var helloResult = new HelloResult(new BsonDocument { { "ok", 1 }, { "maxMessageSizeBytes", 48000000 } });
            var buildInfoResult = new BuildInfoResult(new BsonDocument { { "ok", 1 }, { "version", "3.6.0" } });
            var connectionDescription = new ConnectionDescription(connectionId, helloResult, buildInfoResult);
            var session = NoCoreSession.Instance;

            var result = subject.CreateExplainCommand(connectionDescription, session);

            result.Should().Be(expectedResult);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_not_throw_when_collection_does_not_exist(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureCollectionExists();
            var subject = new ExplainOperation(_databaseNamespace, _explainableOperation, _messageEncoderSettings);

            var result = ExecuteOperation((IReadOperation<BsonDocument>)subject, async);

            result.Should().NotBeNull();
        }

        private void EnsureCollectionExists()
        {
            Insert(BsonDocument.Parse("{x: 1}"));
        }
    }
}
