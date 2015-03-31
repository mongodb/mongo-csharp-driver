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
    public class ListDatabasesOperationTests : OperationTestBase
    {
        // setup methods
        public override void TestFixtureSetUp()
        {
            _databaseNamespace = CoreTestConfiguration.GetDatabaseNamespaceForTestFixture();
        }

        // test methods
        [Test]
        public void constructor_should_initialize_subject()
        {
            var subject = new ListDatabasesOperation(_messageEncoderSettings);

            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);
        }

        [Test]
        public void CreateCommand_should_return_expected_result()
        {
            var subject = new ListDatabasesOperation(_messageEncoderSettings);
            var expectedResult = new BsonDocument
            {
                { "listDatabases", 1 }
            };

            var result = subject.CreateCommand();

            result.Should().Be(expectedResult);
        }

        [Test]
        [RequiresServer]
        public async Task ExecuteAsync_should_return_expected_result()
        {
            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                var subject = new ListDatabasesOperation(_messageEncoderSettings);
                EnsureDatabaseExists(binding);

                var result = await subject.ExecuteAsync(binding, CancellationToken.None);
                var list = await result.ToListAsync();

                list.Should().Contain(x => x["name"] == _databaseNamespace.DatabaseName);
            }
        }

        [Test]
        public void ExecuteAsync_should_throw_when_binding_is_null()
        {
            var subject = new ListDatabasesOperation(_messageEncoderSettings);

            Func<Task> action = () => subject.ExecuteAsync(null, CancellationToken.None);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("binding");
        }

        [Test]
        public void MessageEncoderSettings_get_should_return_expected_result()
        {
            var subject = new ListDatabasesOperation(_messageEncoderSettings);

            var result = subject.MessageEncoderSettings;

            result.Should().BeSameAs(_messageEncoderSettings);
        }

        // helper methods
        private void EnsureDatabaseExists(IWriteBinding binding)
        {
            var collectionNamespace = new CollectionNamespace(_databaseNamespace, "test");
            var requests = new[] { new InsertRequest(new BsonDocument()) };
            var insertOperation = new BulkInsertOperation(collectionNamespace, requests, _messageEncoderSettings);
            insertOperation.ExecuteAsync(binding, CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}
