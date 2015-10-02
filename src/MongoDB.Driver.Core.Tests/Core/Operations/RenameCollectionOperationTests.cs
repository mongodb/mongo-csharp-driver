/* Copyright 2013-2015 MongoDB Inc.
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
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    [TestFixture]
    public class RenameCollectionOperationTests : OperationTestBase
    {
        // fields
        private CollectionNamespace _newCollectionNamespace;

        // setup methods
        public override void TestFixtureSetUp()
        {
            _databaseNamespace = CoreTestConfiguration.GetDatabaseNamespaceForTestFixture();
            _collectionNamespace = new CollectionNamespace(_databaseNamespace, "old");
            _newCollectionNamespace = new CollectionNamespace(_databaseNamespace, "new");
        }

        // test methods
        [Test]
        public void CollectionNamespace_get_should_return_expected_result()
        {
            var subject = new RenameCollectionOperation(_collectionNamespace, _newCollectionNamespace, _messageEncoderSettings);

            var result = subject.CollectionNamespace;

            result.Should().BeSameAs(_collectionNamespace);
        }

        [Test]
        public void constructor_should_initialize_subject()
        {
            var subject = new RenameCollectionOperation(_collectionNamespace, _newCollectionNamespace, _messageEncoderSettings);

            subject.CollectionNamespace.Should().BeSameAs(_collectionNamespace);
            subject.NewCollectionNamespace.Should().BeSameAs(_newCollectionNamespace);
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);
            subject.DropTarget.Should().NotHaveValue();
        }

        [Test]
        public void constructor_should_throw_when_collectionNamespace_is_null()
        {
            Action action = () => new RenameCollectionOperation(null, _newCollectionNamespace, _messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("collectionNamespace");
        }

        [Test]
        public void constructor_should_throw_when_newCollectionNamespace_is_null()
        {
            Action action = () => new RenameCollectionOperation(_collectionNamespace, null, _messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("newCollectionNamespace");
        }

        [Test]
        public void CreateCommand_should_return_expected_result()
        {
            var subject = new RenameCollectionOperation(_collectionNamespace, _newCollectionNamespace, _messageEncoderSettings);
            var expectedResult = new BsonDocument
            {
                { "renameCollection", _collectionNamespace.FullName },
                { "to", _newCollectionNamespace.FullName }
            };

            var result = subject.CreateCommand();

            result.Should().Be(expectedResult);
        }

        [Test]
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

            var result = subject.CreateCommand();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void DropTarget_should_work(
            [Values(false, true)]
            bool dropTarget)
        {
            var subject = new RenameCollectionOperation(_collectionNamespace, _newCollectionNamespace, _messageEncoderSettings);

            subject.DropTarget = dropTarget;
            var result = subject.DropTarget;

            result.Should().Be(dropTarget);
        }

        [Test]
        [RequiresServer]
        public void Execute_should_return_expected_result(
            [Values(false, true)]
            bool async)
        {
            EnsureCollectionExists(_collectionNamespace, async);
            EnsureCollectionDoesNotExist(_newCollectionNamespace, async);
            var subject = new RenameCollectionOperation(_collectionNamespace, _newCollectionNamespace, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

            result["ok"].ToBoolean().Should().BeTrue();
        }

        [Test]
        [RequiresServer]
        public void Execute_should_return_expected_result_when_dropTarget_is_true_and_newCollectionNamespace_exists(
            [Values(false, true)]
            bool async)
        {
            EnsureCollectionExists(_collectionNamespace, async);
            EnsureCollectionExists(_newCollectionNamespace, async);
            var subject = new RenameCollectionOperation(_collectionNamespace, _newCollectionNamespace, _messageEncoderSettings)
            {
                DropTarget = true
            };

            var result = ExecuteOperation(subject, async);

            result["ok"].ToBoolean().Should().BeTrue();
        }

        [Test]
        [RequiresServer]
        public void Execute_should_throw_when_dropTarget_is_false_and_newCollectionNamespace_exists(
            [Values(false, true)]
            bool async)
        {
            var subject = new RenameCollectionOperation(_collectionNamespace, _newCollectionNamespace, _messageEncoderSettings)
            {
                DropTarget = false
            };
            EnsureCollectionExists(_collectionNamespace, async);
            EnsureCollectionExists(_newCollectionNamespace, async);

            Action action = () => ExecuteOperation(subject, async);

            action.ShouldThrow<MongoCommandException>();
        }

        [Test]
        public void Execyte_should_throw_when_binding_is_null(
            [Values(false, true)]
            bool async)
        {
            var subject = new RenameCollectionOperation(_collectionNamespace, _newCollectionNamespace, _messageEncoderSettings);

            Action action = () => ExecuteOperation(subject, null, async);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("binding");
        }

        [Test]
        public void MessageEncoderSettings_get_should_return_expected_result()
        {
            var subject = new RenameCollectionOperation(_collectionNamespace, _newCollectionNamespace, _messageEncoderSettings);

            var result = subject.MessageEncoderSettings;

            result.Should().BeSameAs(_messageEncoderSettings);
        }

        [Test]
        public void NewCollectionNamespace_get_should_return_expected_result()
        {
            var subject = new RenameCollectionOperation(_collectionNamespace, _newCollectionNamespace, _messageEncoderSettings);

            var result = subject.NewCollectionNamespace;

            result.Should().BeSameAs(_newCollectionNamespace);
        }

        // helper methods
        private void EnsureCollectionDoesNotExist(CollectionNamespace collectionNamespace, bool async)
        {
            var operation = new DropCollectionOperation(collectionNamespace, _messageEncoderSettings);
            ExecuteOperation(operation, async);
        }

        private void EnsureCollectionExists(CollectionNamespace collectionNamespace, bool async)
        {
            try
            {
                var operation = new CreateCollectionOperation(collectionNamespace, _messageEncoderSettings);
                ExecuteOperation(operation, async);
            }
            catch (MongoCommandException ex)
            {
                if (ex.Message == "Command create failed: collection already exists.")
                {
                    return;
                }
                throw;
            }
        }
    }
}
