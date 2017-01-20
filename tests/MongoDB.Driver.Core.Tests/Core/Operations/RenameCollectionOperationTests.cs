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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class RenameCollectionOperationTests : OperationTestBase
    {
        // fields
        private CollectionNamespace _newCollectionNamespace;

        // constructors
        public RenameCollectionOperationTests()
        {
            _databaseNamespace = CoreTestConfiguration.GetDatabaseNamespaceForTestClass(typeof(RenameCollectionOperationTests));
            _collectionNamespace = new CollectionNamespace(_databaseNamespace, "old");
            _newCollectionNamespace = new CollectionNamespace(_databaseNamespace, "new");
        }

        // test methods
        [Fact]
        public void CollectionNamespace_get_should_return_expected_result()
        {
            var subject = new RenameCollectionOperation(_collectionNamespace, _newCollectionNamespace, _messageEncoderSettings);

            var result = subject.CollectionNamespace;

            result.Should().BeSameAs(_collectionNamespace);
        }

        [Fact]
        public void constructor_should_initialize_subject()
        {
            var subject = new RenameCollectionOperation(_collectionNamespace, _newCollectionNamespace, _messageEncoderSettings);

            subject.CollectionNamespace.Should().BeSameAs(_collectionNamespace);
            subject.NewCollectionNamespace.Should().BeSameAs(_newCollectionNamespace);
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);
            subject.DropTarget.Should().NotHaveValue();
        }

        [Fact]
        public void constructor_should_throw_when_collectionNamespace_is_null()
        {
            Action action = () => new RenameCollectionOperation(null, _newCollectionNamespace, _messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("collectionNamespace");
        }

        [Fact]
        public void constructor_should_throw_when_newCollectionNamespace_is_null()
        {
            Action action = () => new RenameCollectionOperation(_collectionNamespace, null, _messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("newCollectionNamespace");
        }

        [Fact]
        public void CreateCommand_should_return_expected_result()
        {
            var subject = new RenameCollectionOperation(_collectionNamespace, _newCollectionNamespace, _messageEncoderSettings);
            var expectedResult = new BsonDocument
            {
                { "renameCollection", _collectionNamespace.FullName },
                { "to", _newCollectionNamespace.FullName }
            };

            var result = subject.CreateCommand(null);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_dropTarget_is_provided(
            [Values(false, true)]
            bool dropTarget)
        {
            var subject = new RenameCollectionOperation(_collectionNamespace, _newCollectionNamespace, _messageEncoderSettings)
            {
                DropTarget = dropTarget
            };
            var expectedResult = new BsonDocument
            {
                { "renameCollection", _collectionNamespace.FullName },
                { "to", _newCollectionNamespace.FullName },
                { "dropTarget", dropTarget }
            };

            var result = subject.CreateCommand(null);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_WriteConcern_is_set(
            [Values(null, 1, 2)]
            int? w,
            [Values(false, true)]
            bool isWriteConcernSupported)
        {
            var writeConcern = w.HasValue ? new WriteConcern(w.Value) : null;
            var subject = new RenameCollectionOperation(_collectionNamespace, _newCollectionNamespace, _messageEncoderSettings)
            {
                WriteConcern = writeConcern
            };
            var serverVersion = Feature.CommandsThatWriteAcceptWriteConcern.SupportedOrNotSupportedVersion(isWriteConcernSupported);

            var result = subject.CreateCommand(serverVersion);

            var expectedResult = new BsonDocument
            {
                { "renameCollection", _collectionNamespace.FullName },
                { "to", _newCollectionNamespace.FullName },
                { "writeConcern", () => writeConcern.ToBsonDocument(), writeConcern != null && isWriteConcernSupported }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void DropTarget_should_work(
            [Values(false, true)]
            bool dropTarget)
        {
            var subject = new RenameCollectionOperation(_collectionNamespace, _newCollectionNamespace, _messageEncoderSettings);

            subject.DropTarget = dropTarget;
            var result = subject.DropTarget;

            result.Should().Be(dropTarget);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureCollectionExists(_collectionNamespace, async);
            EnsureCollectionDoesNotExist(_newCollectionNamespace, async);
            var subject = new RenameCollectionOperation(_collectionNamespace, _newCollectionNamespace, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

            result["ok"].ToBoolean().Should().BeTrue();
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_dropTarget_is_true_and_newCollectionNamespace_exists(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureCollectionExists(_collectionNamespace, async);
            EnsureCollectionExists(_newCollectionNamespace, async);
            var subject = new RenameCollectionOperation(_collectionNamespace, _newCollectionNamespace, _messageEncoderSettings)
            {
                DropTarget = true
            };

            var result = ExecuteOperation(subject, async);

            result["ok"].ToBoolean().Should().BeTrue();
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_dropTarget_is_false_and_newCollectionNamespace_exists(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            var subject = new RenameCollectionOperation(_collectionNamespace, _newCollectionNamespace, _messageEncoderSettings)
            {
                DropTarget = false
            };
            EnsureCollectionExists(_collectionNamespace, async);
            EnsureCollectionExists(_newCollectionNamespace, async);

            Action action = () => ExecuteOperation(subject, async);

            action.ShouldThrow<MongoCommandException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_binding_is_null(
            [Values(false, true)]
            bool async)
        {
            var subject = new RenameCollectionOperation(_collectionNamespace, _newCollectionNamespace, _messageEncoderSettings);

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
            EnsureCollectionExists(_collectionNamespace, async);
            EnsureCollectionDoesNotExist(_newCollectionNamespace, async);
            var subject = new RenameCollectionOperation(_collectionNamespace, _newCollectionNamespace, _messageEncoderSettings)
            {
                WriteConcern = new WriteConcern(9)
            };

            var exception = Record.Exception(() => ExecuteOperation(subject, async));

            exception.Should().BeOfType<MongoWriteConcernException>();
        }

        [Fact]
        public void MessageEncoderSettings_get_should_return_expected_result()
        {
            var subject = new RenameCollectionOperation(_collectionNamespace, _newCollectionNamespace, _messageEncoderSettings);

            var result = subject.MessageEncoderSettings;

            result.Should().BeSameAs(_messageEncoderSettings);
        }

        [Fact]
        public void NewCollectionNamespace_get_should_return_expected_result()
        {
            var subject = new RenameCollectionOperation(_collectionNamespace, _newCollectionNamespace, _messageEncoderSettings);

            var result = subject.NewCollectionNamespace;

            result.Should().BeSameAs(_newCollectionNamespace);
        }

        [Theory]
        [ParameterAttributeData]
        public void WriteConcern_get_and_set_should_work(
            [Values(null, 1, 2)]
            int? w)
        {
            var subject = new RenameCollectionOperation(_collectionNamespace, _newCollectionNamespace, _messageEncoderSettings);
            var value = w.HasValue ? new WriteConcern(w.Value) : null;

            subject.WriteConcern = value;
            var result = subject.WriteConcern;

            result.Should().BeSameAs(value);
        }

        // helper methods
        private void EnsureCollectionDoesNotExist(CollectionNamespace collectionNamespace, bool async)
        {
            var operation = new DropCollectionOperation(collectionNamespace, _messageEncoderSettings);
            ExecuteOperation(operation, async);
        }

        private void EnsureCollectionExists(CollectionNamespace collectionNamespace, bool async)
        {
            var document = new BsonDocument("_id", ObjectId.GenerateNewId());
            var insertRequests = new[] { new InsertRequest(document) };
            var insertOperation = new BulkInsertOperation(collectionNamespace, insertRequests, _messageEncoderSettings);
            ExecuteOperation(insertOperation, async);
        }
    }
}
