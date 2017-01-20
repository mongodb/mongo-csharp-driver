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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class DatabaseExistsOperationTests : OperationTestBase
    {
        public DatabaseExistsOperationTests()
        {
            // override this database and collection using special ones for this...
            _databaseNamespace = new DatabaseNamespace("DatabaseExistsOperationTests");
            _collectionNamespace = new CollectionNamespace(_databaseNamespace, "DatabaseExistsOperationTests");
        }

        [Fact]
        public void Constructor_should_throw_when_database_namespace_is_null()
        {
            Action action = () => new DatabaseExistsOperation(null, _messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_message_encoder_settings_is_null()
        {
            Action action = () => new DatabaseExistsOperation(_databaseNamespace, null);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_initialize_subject()
        {
            var subject = new DatabaseExistsOperation(_databaseNamespace, _messageEncoderSettings);

            subject.DatabaseNamespace.DatabaseName.Should().Be(_databaseNamespace.DatabaseName);
            subject.MessageEncoderSettings.Should().BeEquivalentTo(_messageEncoderSettings);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_true_when_database_exists(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            try
            {
                Insert(BsonDocument.Parse("{x:1}")); // ensure database exists

                var subject = new DatabaseExistsOperation(_databaseNamespace, _messageEncoderSettings);

                var result = ExecuteOperation(subject, async);

                result.Should().BeTrue();
            }
            finally
            {
                try { DropDatabase(); } catch { }
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_false_when_database_does_not_exist(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            DropDatabase();
            var subject = new DatabaseExistsOperation(_databaseNamespace, _messageEncoderSettings);

            try
            {
                var result = ExecuteOperation(subject, async);

                result.Should().BeFalse();
            }
            finally
            {
                try { DropDatabase(); } catch { }
            }
        }
    }
}
