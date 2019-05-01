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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class ListCollectionsUsingCommandOperationTests : OperationTestBase
    {
        // constructors
        public ListCollectionsUsingCommandOperationTests()
        {
            _databaseNamespace = CoreTestConfiguration.GetDatabaseNamespaceForTestClass(typeof(ListCollectionsUsingCommandOperationTests));
        }

        // test methods
        [Fact]
        public void constructor_should_initialize_subject()
        {
            var subject = new ListCollectionsUsingCommandOperation(_databaseNamespace, _messageEncoderSettings);

            subject.DatabaseNamespace.Should().BeSameAs(_databaseNamespace);
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);
            subject.Filter.Should().BeNull();
            subject.RetryRequested.Should().BeFalse();
        }

        [Fact]
        public void constructor_should_throw_when_databaseNamespace_is_null()
        {
            Action action = () => new ListCollectionsUsingCommandOperation(null, _messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("databaseNamespace");
        }

        [Fact]
        public void constructor_should_throw_when_messageEncoderSettings_is_null()
        {
            Action action = () => new ListCollectionsUsingCommandOperation(_databaseNamespace, null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("messageEncoderSettings");
        }

        [Fact]
        public void Filter_get_and_set_should_work()
        {
            var subject = new ListCollectionsUsingCommandOperation(_databaseNamespace, _messageEncoderSettings);
            var filter = new BsonDocument("name", "abc");

            subject.Filter = filter;
            var result = subject.Filter;

            result.Should().BeSameAs(filter);
        }

        [Theory]
        [ParameterAttributeData]
        public void NameOnly_get_and_set_should_work(
           [Values(null, false, true)] bool? nameOnly)
        {
            var subject = new ListCollectionsUsingCommandOperation(_databaseNamespace, _messageEncoderSettings);

            subject.NameOnly = nameOnly;
            var result = subject.NameOnly;

            result.Should().Be(nameOnly);
        }

        [Theory]
        [ParameterAttributeData]
        public void RetryRequested_get_and_set_should_work(
           [Values(false, true)] bool value)
        {
            var subject = new ListCollectionsUsingCommandOperation(_databaseNamespace, _messageEncoderSettings);

            subject.RetryRequested = value;
            var result = subject.RetryRequested;

            result.Should().Be(value);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_the_expected_result(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.ListCollectionsCommand);
            EnsureCollectionsExist();
            var subject = new ListCollectionsUsingCommandOperation(_databaseNamespace, _messageEncoderSettings);
            var expectedNames = new[] { "regular", "capped" };

            var result = ExecuteOperation(subject, async);
            var list = ReadCursorToEnd(result, async);

            list.Count.Should().BeGreaterThan(0);
            list.Select(c => c["name"].AsString).Where(n => n != "system.indexes").Should().BeEquivalentTo(expectedNames);
        }

        [SkippableTheory]
        [InlineData("{ name : \"regular\" }", "regular", false)]
        [InlineData("{ name : \"regular\" }", "regular", true)]
        [InlineData("{ \"options.capped\" : true }", "capped", false)]
        [InlineData("{ \"options.capped\" : true }", "capped", true)]
        public void Execute_should_return_the_expected_result_when_filter_is_used(string filterString, string expectedName, bool async)
        {
            RequireServer.Check().Supports(Feature.ListCollectionsCommand);
            EnsureCollectionsExist();
            var filter = BsonDocument.Parse(filterString);
            var subject = new ListCollectionsUsingCommandOperation(_databaseNamespace, _messageEncoderSettings)
            {
                Filter = filter
            };

            var result = ExecuteOperation(subject, async);
            var list = ReadCursorToEnd(result, async);

            list.Should().HaveCount(1);
            list[0]["name"].AsString.Should().Be(expectedName);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_the_expected_result_when_the_database_does_not_exist(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.ListCollectionsCommand);
            var databaseNamespace = new DatabaseNamespace(_databaseNamespace.DatabaseName + "-not");
            var subject = new ListCollectionsUsingCommandOperation(databaseNamespace, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);
            var list = ReadCursorToEnd(result, async);

            list.Should().HaveCount(0);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_send_session_id_when_supported(
            [Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.ListCollectionsCommand);
            EnsureCollectionsExist();
            var subject = new ListCollectionsUsingCommandOperation(_databaseNamespace, _messageEncoderSettings);
            var expectedNames = new[] { "regular", "capped" };

            VerifySessionIdWasSentWhenSupported(subject, "listCollections", async);
        }

        [Theory]
        [InlineData(null, null, "{ listCollections : 1 }")]
        [InlineData(null, false, "{ listCollections : 1, nameOnly : false }")]
        [InlineData(null, true, "{ listCollections : 1, nameOnly : true }")]
        [InlineData("{ x: 1 }", null, "{ listCollections : 1, filter : { x : 1 } }")]
        [InlineData("{ x: 1 }", false, "{ listCollections : 1, filter : { x : 1 }, nameOnly : false }")]
        [InlineData("{ x: 1 }", true, "{ listCollections : 1, filter : { x : 1 }, nameOnly : true }")]
        public void CreateCommand_should_return_expected_result(string filterString, bool? nameOnly, string expectedCommand)
        {
            var filter = filterString == null ? null : BsonDocument.Parse(filterString);
            var subject = new ListCollectionsUsingCommandOperation(_databaseNamespace, _messageEncoderSettings)
            {
                Filter = filter,
                NameOnly = nameOnly
            };

            var result = subject.CreateOperation();

            result.Command.Should().Be(expectedCommand);
            result.DatabaseNamespace.Should().BeSameAs(subject.DatabaseNamespace);
            result.ResultSerializer.Should().BeSameAs(BsonDocumentSerializer.Instance);
            result.MessageEncoderSettings.Should().BeSameAs(subject.MessageEncoderSettings);
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

    public static class ListCollectionsUsingCommandOperationReflector
    {
        public static ReadCommandOperation<BsonDocument> CreateOperation(this ListCollectionsUsingCommandOperation obj) => (ReadCommandOperation<BsonDocument>)Reflector.Invoke(obj, nameof(CreateOperation));
    }
}
