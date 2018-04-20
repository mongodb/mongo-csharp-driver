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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class DropIndexOperationTests : OperationTestBase
    {
        // test methods
        [Fact]
        public void CollectionNamespace_get_should_return_expected_result()
        {
            var indexName = "x_1";
            var subject = new DropIndexOperation(_collectionNamespace, indexName, _messageEncoderSettings);

            var result = subject.CollectionNamespace;

            result.Should().BeSameAs(_collectionNamespace);
        }

        [Fact]
        public void constructor_with_collectionNamespace_indexName_messageEncoderSettings_should_initialize_subject()
        {
            var indexName = "x_1";

            var subject = new DropIndexOperation(_collectionNamespace, indexName, _messageEncoderSettings);

            subject.CollectionNamespace.Should().BeSameAs(_collectionNamespace);
            subject.IndexName.Should().Be(indexName);
            subject.MaxTime.Should().NotHaveValue();
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);
        }

        [Fact]
        public void constructor_with_collectionNamespace_indexName_messageEncoderSettings_should_throw_when_collectionNamespace_is_null()
        {
            var indexName = "x_1";

            Action action = () => { new DropIndexOperation(null, indexName, _messageEncoderSettings); };

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("collectionNamespace");
        }

        [Fact]
        public void constructor_with_collectionNamespace_indexName_messageEncoderSettings_should_throw_when_indexName_is_null()
        {
            Action action = () => { new DropIndexOperation(_collectionNamespace, (string)null, _messageEncoderSettings); };

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("indexName");
        }

        [Fact]
        public void constructor_with_collectionNamespace_indexName_messageEncoderSettings_should_throw_when_messageEncoderSettings_is_null()
        {
            var indexName = "x_1";

            Action action = () => { new DropIndexOperation(_collectionNamespace, indexName, null); };

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("messageEncoderSettings");
        }

        [Fact]
        public void constructor_with_collectionNamespace_keys_messageEncoderSettings_should_initialize_subject()
        {
            var keys = new BsonDocument { { "x", 1 } };
            var expectedIndexName = "x_1";

            var subject = new DropIndexOperation(_collectionNamespace, keys, _messageEncoderSettings);

            subject.CollectionNamespace.Should().BeSameAs(_collectionNamespace);
            subject.IndexName.Should().Be(expectedIndexName);
            subject.MaxTime.Should().NotHaveValue();
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);
        }

        [Fact]
        public void constructor_with_collectionNamespace_keys_messageEncoderSettings_should_throw_when_collectionNamespace_is_null()
        {
            var keys = new BsonDocument { { "x", 1 } };

            Action action = () => { new DropIndexOperation(null, keys, _messageEncoderSettings); };

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("collectionNamespace");
        }

        [Fact]
        public void constructor_with_collectionNamespace_keys_messageEncoderSettings_should_throw_when_indexName_is_null()
        {
            Action action = () => { new DropIndexOperation(_collectionNamespace, (BsonDocument)null, _messageEncoderSettings); };

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("keys");
        }

        [Fact]
        public void constructor_with_collectionNamespace_keys_messageEncoderSettings_should_throw_when_messageEncoderSettings_is_null()
        {
            var keys = new BsonDocument { { "x", 1 } };

            Action action = () => { new DropIndexOperation(_collectionNamespace, keys, null); };

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("messageEncoderSettings");
        }

        [Fact]
        public void CreateCommand_should_return_expectedResult()
        {
            var indexName = "x_1";
            var subject = new DropIndexOperation(_collectionNamespace, indexName, _messageEncoderSettings);
            var expectedResult = new BsonDocument
            {
                { "dropIndexes", _collectionNamespace.CollectionName },
                { "index", indexName }
            };
            var session = OperationTestHelper.CreateSession();
            var connectionDescription = OperationTestHelper.CreateConnectionDescription();

            var result = subject.CreateCommand(session, connectionDescription);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(-10000, 0)]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(9999, 1)]
        [InlineData(10000, 1)]
        [InlineData(10001, 2)]
        public void CreateCommand_should_return_expected_result_when_MaxTime_is_Set(long maxTimeTicks, int expectedMaxTimeMS)
        {
            var indexName = "x_1";
            var maxTime = TimeSpan.FromTicks(maxTimeTicks);
            var subject = new DropIndexOperation(_collectionNamespace, indexName, _messageEncoderSettings);
            subject.MaxTime= maxTime;
            var expectedResult = new BsonDocument
            {
                { "dropIndexes", _collectionNamespace.CollectionName },
                { "index", indexName },
                {"maxTimeMS", expectedMaxTimeMS }
            };
            var session = OperationTestHelper.CreateSession();
            var connectionDescription = OperationTestHelper.CreateConnectionDescription();

            var result = subject.CreateCommand(session, connectionDescription);

            result.Should().Be(expectedResult);
            result["maxTimeMS"].BsonType.Should().Be(BsonType.Int32);
            
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expectedResult_when_WriteConcern_is_set(
            [Values(null, 1, 2)]
            int? w,
            [Values(false, true)]
            bool isWriteConcernSupported)
        {
            var indexName = "x_1";
            var writeConcern = w.HasValue ? new WriteConcern(w.Value) : null;
            var subject = new DropIndexOperation(_collectionNamespace, indexName, _messageEncoderSettings)
            {
                WriteConcern = writeConcern
            };
            var session = OperationTestHelper.CreateSession();
            var connectionDescription = OperationTestHelper.CreateConnectionDescription(serverVersion: Feature.CommandsThatWriteAcceptWriteConcern.SupportedOrNotSupportedVersion(isWriteConcernSupported));

            var result = subject.CreateCommand(session, connectionDescription);

            var expectedResult = new BsonDocument
            {
                { "dropIndexes", _collectionNamespace.CollectionName },
                { "index", indexName },
                { "writeConcern", () => writeConcern.ToBsonDocument(), writeConcern != null && isWriteConcernSupported }
            };
            result.Should().Be(expectedResult);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_not_throw_when_collection_does_not_exist(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            DropCollection();

            using (var binding = CreateReadWriteBinding())
            {
                var indexName = "x_1";
                var subject = new DropIndexOperation(_collectionNamespace, indexName, _messageEncoderSettings);

                ExecuteOperation(subject, async); // should not throw
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureIndexExists();
            var indexName = "x_1";
            var subject = new DropIndexOperation(_collectionNamespace, indexName, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

            result["ok"].ToBoolean().Should().BeTrue();
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_binding_is_null(
            [Values(false, true)]
            bool async)
        {
            var indexName = "x_1";
            var subject = new DropIndexOperation(_collectionNamespace, indexName, _messageEncoderSettings);

            Action action = () => ExecuteOperation(subject, null, async);
            var ex = action.ShouldThrow<ArgumentNullException>().Subject.Single();

            ex.ParamName.Should().Be("binding");
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_maxTime_is_exceeded(
            [Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.FailPoints).ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet);
            var indexName = "x_1";
            var subject = new DropIndexOperation(_collectionNamespace, indexName, _messageEncoderSettings) { MaxTime = TimeSpan.FromSeconds(9001) };

            using (var failPoint = FailPoint.ConfigureAlwaysOn(_cluster, _session, FailPointName.MaxTimeAlwaysTimeout))
            {
                var exception = Record.Exception(() => ExecuteOperation(subject, failPoint.Binding, async));

                exception.Should().BeOfType<MongoExecutionTimeoutException>();
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_a_write_concern_error_occurs(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.CommandsThatWriteAcceptWriteConcern).ClusterType(ClusterType.ReplicaSet);
            EnsureIndexExists();
            var indexName = "x_1";
            var subject = new DropIndexOperation(_collectionNamespace, indexName, _messageEncoderSettings)
            {
                WriteConcern = new WriteConcern(9)
            };

            var exception = Record.Exception(() => ExecuteOperation(subject, async));

            exception.Should().BeOfType<MongoWriteConcernException>();
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_send_session_id_when_supported(
            [Values(false, true)] bool async)
        {
            RequireServer.Check();
            EnsureIndexExists();
            var indexName = "x_1";
            var subject = new DropIndexOperation(_collectionNamespace, indexName, _messageEncoderSettings);

            VerifySessionIdWasSentWhenSupported(subject, "dropIndexes", async);
        }

        [Fact]
        public void IndexName_get_should_return_expected_result()
        {
            var indexName = "x_1";
            var subject = new DropIndexOperation(_collectionNamespace, indexName, _messageEncoderSettings);

            var result = subject.IndexName;

            result.Should().BeSameAs(indexName);
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxTime_get_and_set_should_work(
            [Values(null, -10000, 0, 1, 42, 9000, 10000, 10001)] int? maxTimeTicks)
        {
            var indexName = "x_1";
            var subject = new DropIndexOperation(_collectionNamespace, indexName, _messageEncoderSettings);
            var value = maxTimeTicks == null ? (TimeSpan?)null : TimeSpan.FromTicks(maxTimeTicks.Value);

            subject.MaxTime = value;
            var result = subject.MaxTime;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxTime_set_should_throw_when_value_is_invalid(
            [Values(-10001, -9999, -42, -1)] long maxTimeTicks)
        {
            var indexName = "x_1";
            var subject = new DropIndexOperation(_collectionNamespace, indexName, _messageEncoderSettings);
            var value = TimeSpan.FromTicks(maxTimeTicks);

            var exception = Record.Exception(() => subject.MaxTime = value);

            var e = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
            e.ParamName.Should().Be("value");
        }

        [Fact]
        public void MessageEncoderSettings_get_should_return_expected_result()
        {
            var indexName = "x_1";
            var subject = new DropIndexOperation(_collectionNamespace, indexName, _messageEncoderSettings);

            var result = subject.MessageEncoderSettings;

            result.Should().BeSameAs(_messageEncoderSettings);
        }
        
        [Theory]
        [ParameterAttributeData]
        public void WriteConcern_get_and_set_should_work(
            [Values(null, 1, 2)]
            int? w)
        {
            var indexName = "x_1";
            var subject = new DropIndexOperation(_collectionNamespace, indexName, _messageEncoderSettings);
            var value = w.HasValue ? new WriteConcern(w.Value) : null;

            subject.WriteConcern = value;
            var result = subject.WriteConcern;

            result.Should().BeSameAs(value);
        }

        // private methods
        private void EnsureIndexExists()
        {
            DropCollection();
            var keys = new BsonDocument("x", 1);
            var requests = new[] { new CreateIndexRequest(keys) };
            var createIndexOperation = new CreateIndexesOperation(_collectionNamespace, requests, _messageEncoderSettings);
            ExecuteOperation(createIndexOperation);
        }
    }
}
