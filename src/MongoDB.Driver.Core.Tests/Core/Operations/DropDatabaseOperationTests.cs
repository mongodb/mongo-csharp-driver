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
    public class DropDatabaseOperationTests
    {
        // fields
        private DatabaseNamespace _databaseNamespace;
        private MessageEncoderSettings _messageEncoderSettings;

        // setup methods
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            _databaseNamespace = CoreTestConfiguration.GetDatabaseNamespaceForTestFixture();
            _messageEncoderSettings = CoreTestConfiguration.MessageEncoderSettings;
        }

        // test methods
        [Test]
        public void constructor_should_initialize_subject()
        {
            var subject = new DropDatabaseOperation(_databaseNamespace, _messageEncoderSettings);

            subject.DatabaseNamespace.Should().BeSameAs(_databaseNamespace);
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);
        }

        [Test]
        public void constructor_should_throw_when_databaseNamespace_is_null()
        {
            Action action = () => { new DropDatabaseOperation(null, _messageEncoderSettings); };

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("databaseNamespace");
        }

        [Test]
        public void CreateCommand_should_return_expected_result()
        {
            var subject = new DropDatabaseOperation(_databaseNamespace, _messageEncoderSettings);
            var expectedResult = new BsonDocument
            {
                { "dropDatabase", 1 }
            };

            var result = subject.CreateCommand();

            result.Should().Be(expectedResult);
        }

        [Test]
        [RequiresServer]
        public void Execute_should_return_expected_result(
            [Values(false, true)]
            bool async)
        {
            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                EnsureDatabaseExists(binding);
                var subject = new DropDatabaseOperation(_databaseNamespace, _messageEncoderSettings);

                var result = ExecuteOperation(subject, binding, async);

                result["ok"].ToBoolean().Should().BeTrue();
            }
        }

        [Test]
        public void Execute_should_throw_when_binding_is_null(
            [Values(false, true)]
            bool async)
        {
            var subject = new DropDatabaseOperation(_databaseNamespace, _messageEncoderSettings);

            Action action = () => ExecuteOperation(subject, null, async);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("binding");
        }

        [Test]
        public void DatabaseNamespace_get_should_return_expected_result()
        {
            var subject = new DropDatabaseOperation(_databaseNamespace, _messageEncoderSettings);

            var result = subject.DatabaseNamespace;

            result.Should().BeSameAs(_databaseNamespace);
        }

        [Test]
        public void MessageEncoderSettings_get_should_return_expected_result()
        {
            var subject = new DropDatabaseOperation(_databaseNamespace, _messageEncoderSettings);

            var result = subject.MessageEncoderSettings;

            result.Should().BeSameAs(_messageEncoderSettings);
        }

        // helper methods
        private void EnsureDatabaseExists(IWriteBinding binding)
        {
            var collectionNamespace = new CollectionNamespace(_databaseNamespace, "test");
            var requests = new[] { new InsertRequest(new BsonDocument()) };
            var insertOperation = new BulkInsertOperation(collectionNamespace, requests, _messageEncoderSettings);
            insertOperation.Execute(binding, CancellationToken.None);
        }

        private TResult ExecuteOperation<TResult>(IWriteOperation<TResult> operation, IReadWriteBinding binding, bool async)
        {
            if (async)
            {
                return operation.ExecuteAsync(binding, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                return operation.Execute(binding, CancellationToken.None);
            }
        }
    }
}
