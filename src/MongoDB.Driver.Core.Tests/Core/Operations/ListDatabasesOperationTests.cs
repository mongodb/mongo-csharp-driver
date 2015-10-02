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
        public void Execute_should_return_expected_result(
            [Values(false, true)]
            bool async)
        {
            var subject = new ListDatabasesOperation(_messageEncoderSettings);
            EnsureDatabaseExists(async);

            var result = ExecuteOperation(subject, async);
            var list = ReadCursorToEnd(result, async);

            list.Should().Contain(x => x["name"] == _databaseNamespace.DatabaseName);
        }

        [Test]
        public void Execute_should_throw_when_binding_is_null(
            [Values(false, true)]
            bool async)
        {
            var subject = new ListDatabasesOperation(_messageEncoderSettings);

            Action action = () => ExecuteOperation(subject, null, async);

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
        private void EnsureDatabaseExists(bool async)
        {
            var collectionNamespace = new CollectionNamespace(_databaseNamespace, "test");
            var requests = new[] { new InsertRequest(new BsonDocument()) };
            var insertOperation = new BulkInsertOperation(collectionNamespace, requests, _messageEncoderSettings);
            ExecuteOperation(insertOperation, async);
        }
    }
}
