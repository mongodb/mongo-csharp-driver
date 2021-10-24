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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class DropDatabaseOperationTests : OperationTestBase
    {
        // test methods
        [Fact]
        public void constructor_should_initialize_subject()
        {
            var subject = new DropDatabaseOperation(_databaseNamespace, _messageEncoderSettings);

            subject.DatabaseNamespace.Should().BeSameAs(_databaseNamespace);
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);
        }

        [Fact]
        public void constructor_should_throw_when_databaseNamespace_is_null()
        {
            Action action = () => { new DropDatabaseOperation(null, _messageEncoderSettings); };

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("databaseNamespace");
        }

        [Fact]
        public void CreateCommand_should_return_expected_result()
        {
            var subject = new DropDatabaseOperation(_databaseNamespace, _messageEncoderSettings);
            var expectedResult = new BsonDocument
            {
                { "dropDatabase", 1 }
            };
            var session = OperationTestHelper.CreateSession();
            var connectionDescription = OperationTestHelper.CreateConnectionDescription();

            var result = subject.CreateCommand(session, connectionDescription);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_WriteConcern_is_set(
            [Values(null, 1, 2)]
            int? w)
        {
            var writeConcern = w.HasValue ? new WriteConcern(w.Value) : null;
            var subject = new DropDatabaseOperation(_databaseNamespace, _messageEncoderSettings)
            {
                WriteConcern = writeConcern
            };
            var session = OperationTestHelper.CreateSession();
            var connectionDescription = OperationTestHelper.CreateConnectionDescription(serverVersion: Feature.CommandsThatWriteAcceptWriteConcern.FirstSupportedVersion);

            var result = subject.CreateCommand(session, connectionDescription);

            var expectedResult = new BsonDocument
            {
                { "dropDatabase", 1 },
                { "writeConcern", () => writeConcern.ToBsonDocument(), writeConcern != null }
            };
            result.Should().Be(expectedResult);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureDatabaseExists();

            using (var binding = CreateReadWriteBinding())
            {
                var subject = new DropDatabaseOperation(_databaseNamespace, _messageEncoderSettings);

                var result = ExecuteOperation(subject, binding, async);

                result["ok"].ToBoolean().Should().BeTrue();
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_binding_is_null(
            [Values(false, true)]
            bool async)
        {
            var subject = new DropDatabaseOperation(_databaseNamespace, _messageEncoderSettings);

            Action action = () => ExecuteOperation(subject, null, async);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("binding");
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_a_write_concern_error_occurs(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.CommandsThatWriteAcceptWriteConcern).ClusterType(ClusterType.ReplicaSet);
            var subject = new DropDatabaseOperation(_databaseNamespace, _messageEncoderSettings)
            {
                WriteConcern = new WriteConcern(9)
            };

            var exception = Record.Exception(() =>
            {
                using (var binding = CreateReadWriteBinding())
                {
                    ExecuteOperation(subject, binding, async);
                }
            });

            exception.Should().BeOfType<MongoWriteConcernException>();
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_send_session_id_when_supported(
            [Values(false, true)] bool async)
        {
            RequireServer.Check();
            EnsureDatabaseExists();
            var subject = new DropDatabaseOperation(_databaseNamespace, _messageEncoderSettings);

            VerifySessionIdWasSentWhenSupported(subject, "dropDatabase", async);
        }

        [Fact]
        public void DatabaseNamespace_get_should_return_expected_result()
        {
            var subject = new DropDatabaseOperation(_databaseNamespace, _messageEncoderSettings);

            var result = subject.DatabaseNamespace;

            result.Should().BeSameAs(_databaseNamespace);
        }

        [Fact]
        public void MessageEncoderSettings_get_should_return_expected_result()
        {
            var subject = new DropDatabaseOperation(_databaseNamespace, _messageEncoderSettings);

            var result = subject.MessageEncoderSettings;

            result.Should().BeSameAs(_messageEncoderSettings);
        }

        [Theory]
        [ParameterAttributeData]
        public void WriteConcern_get_and_set_should_work(
            [Values(null, 1, 2)]
            int? w)
        {
            var subject = new DropDatabaseOperation(_databaseNamespace, _messageEncoderSettings);
            var value = w.HasValue ? new WriteConcern(w.Value) : null;

            subject.WriteConcern = value;
            var result = subject.WriteConcern;

            result.Should().BeSameAs(value);
        }
    }
}
