/* Copyright 2013-2014 MongoDB Inc.
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
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    [TestFixture]
    public class ExplainOperationTests : OperationTestBase
    {
        private BsonDocument _command;

        public override void TestFixtureSetUp()
        {
            base.TestFixtureSetUp();

            _command = new BsonDocument
            {
                { "count", _collectionNamespace.CollectionName }
            };
        }

        [Test]
        public void Constructor_should_throw_when_collection_namespace_is_null()
        {
            Action action = () => new ExplainOperation(null, _command, _messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_command_is_null()
        {
            Action action = () => new ExplainOperation(_databaseNamespace, null, _messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_message_encoder_settings_is_null()
        {
            Action action = () => new ExplainOperation(_databaseNamespace, _command, null);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_initialize_subject()
        {
            var subject = new ExplainOperation(_databaseNamespace, _command, _messageEncoderSettings);

            subject.DatabaseNamespace.Should().Be(_databaseNamespace);
            subject.Command.Should().Be(_command);
            subject.MessageEncoderSettings.Should().BeEquivalentTo(_messageEncoderSettings);
            subject.Verbosity.Should().Be(ExplainVerbosity.QueryPlanner);
        }

        [Test]
        [TestCase(ExplainVerbosity.AllPlansExecution, "allPlansExecution")]
        [TestCase(ExplainVerbosity.ExecutionStats, "executionStats")]
        [TestCase(ExplainVerbosity.QueryPlanner, "queryPlanner")]
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

        [Test]
        [RequiresServer("EnsureCollectionExists", MinimumVersion = "2.7.6")]
        public void Execute_should_not_throw_when_collection_does_not_exist(
            [Values(false, true)]
            bool async)
        {
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
