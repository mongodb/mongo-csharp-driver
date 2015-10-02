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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    [TestFixture]
    public class ListCollectionsOperationTests : OperationTestBase
    {
        // setup method
        public override void TestFixtureSetUp()
        {
            base.TestFixtureSetUp();
            _databaseNamespace = CoreTestConfiguration.GetDatabaseNamespaceForTestFixture();
        }

        // test methods
        [Test]
        public void constructor_should_initialize_subject()
        {
            var subject = new ListCollectionsOperation(_databaseNamespace, _messageEncoderSettings);

            subject.DatabaseNamespace.Should().BeSameAs(_databaseNamespace);
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);
            subject.Filter.Should().BeNull();
        }

        [Test]
        public void constructor_should_throw_when_databaseNamespace_is_null()
        {
            Action action = () => new ListCollectionsOperation(null, _messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("databaseNamespace");
        }

        [Test]
        public void constructor_should_throw_when_messageEncoderSettings_is_null()
        {
            Action action = () => new ListCollectionsOperation(_databaseNamespace, null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("messageEncoderSettings");
        }

        [Test]
        public void Filter_get_and_set_should_work()
        {
            var subject = new ListCollectionsOperation(_databaseNamespace, _messageEncoderSettings);
            var filter = new BsonDocument("name", "abc");

            subject.Filter = filter;
            var result = subject.Filter;

            result.Should().BeSameAs(filter);
        }

        [Test]
        [RequiresServer("EnsureCollectionsExist")]
        public void Execute_should_return_the_expected_result(
            [Values(false, true)]
            bool async)
        {
            var subject = new ListCollectionsOperation(_databaseNamespace, _messageEncoderSettings);
            var expectedNames = new[] { "regular", "capped" };

            var result = ExecuteOperation(subject, async);
            var list = ReadCursorToEnd(result, async);

            list.Count.Should().BeGreaterThan(0);
            list.Select(c => c["name"].AsString).Where(n => n != "system.indexes").Should().BeEquivalentTo(expectedNames);
        }

        [TestCase("{ name : \"regular\" }", "regular", false)]
        [TestCase("{ name : \"regular\" }", "regular", true)]
        [TestCase("{ \"options.capped\" : true }", "capped", false)]
        [TestCase("{ \"options.capped\" : true }", "capped", true)]
        [RequiresServer("EnsureCollectionsExist")]
        public void Execute_should_return_the_expected_result_when_filter_is_used(string filterString, string expectedName, bool async)
        {
            var filter = BsonDocument.Parse(filterString);
            var subject = new ListCollectionsOperation(_databaseNamespace, _messageEncoderSettings)
            {
                Filter = filter
            };

            var result = ExecuteOperation(subject, async);
            var list = ReadCursorToEnd(result, async);

            list.Should().HaveCount(1);
            list[0]["name"].AsString.Should().Be(expectedName);
        }

        [Test]
        [RequiresServer]
        public void Execute_should_return_the_expected_result_when_the_database_does_not_exist(
            [Values(false, true)]
            bool async)
        {
            var databaseNamespace = new DatabaseNamespace(_databaseNamespace.DatabaseName + "-not");
            var subject = new ListCollectionsOperation(databaseNamespace, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);
            var list = ReadCursorToEnd(result, async);

            list.Should().HaveCount(0);
        }

        [Test]
        [RequiresServer(VersionLessThan = "2.7.0")]
        public void Execute_should_throw_when_filter_name_is_not_a_string_and_connected_to_older_server(
            [Values(false, true)]
            bool async)
        {
            var filter = new BsonDocument("name", new BsonRegularExpression("^abc"));
            var subject = new ListCollectionsOperation(_databaseNamespace, _messageEncoderSettings)
            {
                Filter = filter
            };

            Action action = () => ExecuteOperation(subject, async);

            action.ShouldThrow<NotSupportedException>();
        }

        // helper methods
        private void CreateCappedCollection()
        {
            var collectionNamespace = new CollectionNamespace(_databaseNamespace, "capped");
            var createCollectionOperation = new CreateCollectionOperation(collectionNamespace, _messageEncoderSettings)
            {
                Capped = true,
                MaxSize = 10000
            };
            ExecuteOperation(createCollectionOperation);

            CreateIndexAndInsertData(collectionNamespace);
        }

        private void CreateIndexAndInsertData(CollectionNamespace collectionNamespace)
        {
            var createIndexRequests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) };
            var createIndexOperation = new CreateIndexesOperation(collectionNamespace, createIndexRequests, _messageEncoderSettings);
            ExecuteOperation(createIndexOperation);

            var insertRequests = new[] { new InsertRequest(new BsonDocument("x", 1)) };
            var insertOperation = new BulkInsertOperation(collectionNamespace, insertRequests, _messageEncoderSettings);
            ExecuteOperation(insertOperation);
        }

        private void CreateRegularCollection()
        {
            var collectionNamespace = new CollectionNamespace(_databaseNamespace, "regular");
            var createCollectionOperation = new CreateCollectionOperation(collectionNamespace, _messageEncoderSettings);
            ExecuteOperation(createCollectionOperation);

            CreateIndexAndInsertData(collectionNamespace);
        }

        private void EnsureCollectionsExist()
        {
            RunOncePerFixture(() =>
            {
                DropDatabase();
                CreateRegularCollection();
                CreateCappedCollection();
            });
        }
    }
}
