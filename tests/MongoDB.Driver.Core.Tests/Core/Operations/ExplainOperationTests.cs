/* Copyright 2013-2016 MongoDB Inc.
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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class ExplainOperationTests : OperationTestBase
    {
        private BsonDocument _command;

        public ExplainOperationTests()
        {
            _command = new BsonDocument
            {
                { "count", _collectionNamespace.CollectionName }
            };
        }

        [Fact]
        public void Constructor_should_throw_when_collection_namespace_is_null()
        {
            Action action = () => new ExplainOperation(null, _command, _messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_command_is_null()
        {
            Action action = () => new ExplainOperation(_databaseNamespace, null, _messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_message_encoder_settings_is_null()
        {
            Action action = () => new ExplainOperation(_databaseNamespace, _command, null);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_initialize_subject()
        {
            var subject = new ExplainOperation(_databaseNamespace, _command, _messageEncoderSettings);

            subject.DatabaseNamespace.Should().Be(_databaseNamespace);
            subject.Command.Should().Be(_command);
            subject.MessageEncoderSettings.Should().BeEquivalentTo(_messageEncoderSettings);
            subject.Verbosity.Should().Be(ExplainVerbosity.QueryPlanner);
        }

        [Theory]
        [InlineData(ExplainVerbosity.AllPlansExecution, "allPlansExecution")]
        [InlineData(ExplainVerbosity.ExecutionStats, "executionStats")]
        [InlineData(ExplainVerbosity.QueryPlanner, "queryPlanner")]
        public void CreateCommand_should_return_expected_result(ExplainVerbosity verbosity, string verbosityString)
        {
            var subject = new ExplainOperation(_databaseNamespace, _command, _messageEncoderSettings)
            {
                Verbosity = verbosity
            };

            var expectedResult = new BsonDocument
            {
                { "explain", _command },
                { "verbosity", verbosityString }
            };

            var result = subject.CreateCommand();

            result.Should().Be(expectedResult);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_not_throw_when_collection_does_not_exist(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.ExplainCommand);
            EnsureCollectionExists();
            var subject = new ExplainOperation(_databaseNamespace, _command, _messageEncoderSettings);

            var result = ExecuteOperation((IReadOperation<BsonDocument>)subject, async);

            result.Should().NotBeNull();
        }

        private void EnsureCollectionExists()
        {
            Insert(BsonDocument.Parse("{x: 1}"));
        }
    }
}
