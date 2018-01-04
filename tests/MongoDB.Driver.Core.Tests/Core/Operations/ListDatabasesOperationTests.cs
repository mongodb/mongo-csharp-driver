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
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class ListDatabasesOperationTests : OperationTestBase
    {
        // constructors
        public ListDatabasesOperationTests()
        {
            _databaseNamespace = CoreTestConfiguration.GetDatabaseNamespaceForTestClass(typeof(ListDatabasesOperationTests));
        }

        // test methods
        [Fact]
        public void constructor_should_initialize_subject()
        {
            var subject = new ListDatabasesOperation(_messageEncoderSettings);

            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);
        }

        [Fact]
        public void Filter_get_and_set_should_work()
        {
            var subject = new ListDatabasesOperation(_messageEncoderSettings);
            var filter = new BsonDocument("name", "abc");

            subject.Filter = filter;
            var result = subject.Filter;

            result.Should().BeSameAs(filter);
        }

        [Theory]
        [ParameterAttributeData]
        public void NameOnly_get_and_set_should_work(
            [Values(false,true)] bool nameOnly)
        {
            var subject = new ListDatabasesOperation(_messageEncoderSettings);
   
            subject.NameOnly = nameOnly;
            var result = subject.NameOnly;

            result.Should().Be(nameOnly);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result(
            [Values(null, "cake")] string filterString,
            [Values(null, false, true)] bool? nameOnly)
        {
            var filter = filterString != null
                ? BsonDocument.Parse($"{{ name : \"{filterString}\" }}")
                : null;

            var subject = new ListDatabasesOperation(_messageEncoderSettings)
            {
                NameOnly = nameOnly,
                Filter = filter
            };
            
            var expectedResult = new BsonDocument
            {
                { "listDatabases", 1 },
                { "filter", filter, filterString != null },
                { "nameOnly", nameOnly, nameOnly != null }
            };
            

            var result = subject.CreateCommand();

            result.Should().Be(expectedResult);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result(
            [Values(false, true)] bool async)
        {
            RequireServer.Check();
            var subject = new ListDatabasesOperation(_messageEncoderSettings);
            EnsureDatabaseExists(async);

            var result = ExecuteOperation(subject, async);
            var list = ReadCursorToEnd(result, async);

            list.Should().Contain(x => x["name"] == _databaseNamespace.DatabaseName);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_the_expected_result_when_filter_is_used(
            [Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.ListDatabasesFilter);
            
            var filterString = $"{{ name : \"{_databaseNamespace.DatabaseName}\" }}";
            var filter = BsonDocument.Parse(filterString);
            var subject = new ListDatabasesOperation(_messageEncoderSettings) { Filter = filter };
            EnsureDatabaseExists(async);

            var result = ExecuteOperation(subject, async);

            var databases = ReadCursorToEnd(result, async);
            databases.Should().HaveCount(1);
            databases[0]["name"].AsString.Should().Be(_databaseNamespace.DatabaseName);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_the_expected_result_when_nameOnly_is_used(
            [Values(false, true)] bool nameOnly,
            [Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.ListDatabasesNameOnlyOption);

            var subject = new ListDatabasesOperation(_messageEncoderSettings)
            {
                NameOnly = nameOnly
            };
            
            EnsureDatabaseExists(async);

            var result = ExecuteOperation(subject, async);
            var databases = ReadCursorToEnd(result, async);

            foreach (var database in databases)
            {
                database.Contains("name").Should().BeTrue();
                if (nameOnly)
                {
                    database.ElementCount.Should().Be(1);
                }
                else
                {
                    database.ElementCount.Should().BeGreaterThan(1);
                }       
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_binding_is_null(
            [Values(false, true)] bool async)
        {
            var subject = new ListDatabasesOperation(_messageEncoderSettings);
            IReadBinding binding = null;

            Action action = () => ExecuteOperation(subject, binding, async);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("binding");
        }

        [Fact]
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
