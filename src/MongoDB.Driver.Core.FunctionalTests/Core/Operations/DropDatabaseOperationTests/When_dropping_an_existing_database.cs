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

using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations.DropDatabaseOperationTests
{
    [TestFixture]
    public class When_dropping_an_existing_database : CollectionUsingSpecification
    {
        private DropDatabaseOperation _subject;
        private BsonDocument _result;

        protected override void Given()
        {
            // Ensure database exists
            var op = new BulkInsertOperation(
                new CollectionNamespace(DatabaseNamespace, "temp"), 
                new [] { new InsertRequest(new BsonDocument("x", 1)) },
                MessageEncoderSettings);

            ExecuteOperation(op);

            _subject = new DropDatabaseOperation(DatabaseNamespace, MessageEncoderSettings);
        }

        protected override void When()
        {
            _result = ExecuteOperation(_subject);
        }

        [Test]
        public void Ok_should_be_true()
        {
            _result["ok"].ToBoolean().Should().BeTrue();
        }

        [Test]
        public void The_database_should_no_longer_exist()
        {
            var op = new ListDatabaseNamesOperation(MessageEncoderSettings);
            var result = ExecuteOperation(op);
            result.Should().NotContain(DatabaseNamespace);
        }
    }
}
