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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    [TestFixture]
    public class DistinctOperationTests : OperationTestBase
    {
        private string _fieldName;
        private IBsonSerializer<int> _valueSerializer;

        public override void TestFixtureSetUp()
        {
            base.TestFixtureSetUp();

            _fieldName = "y";
            _valueSerializer = new Int32Serializer();
        }

        [Test]
        public void Constructor_should_throw_when_collection_namespace_is_null()
        {
            Action act = () => new DistinctOperation<int>(null, _valueSerializer, _fieldName, _messageEncoderSettings);

            act.ShouldThrow<ArgumentException>();
        }

        [Test]
        public void Constructor_should_throw_when_value_serializer_is_null()
        {
            Action act = () => new DistinctOperation<int>(_collectionNamespace, null, _fieldName, _messageEncoderSettings);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_field_name_is_null()
        {
            Action act = () => new DistinctOperation<int>(_collectionNamespace, _valueSerializer, null, _messageEncoderSettings);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_message_encoder_settings_is_null()
        {
            Action act = () => new DistinctOperation<int>(_collectionNamespace, _valueSerializer, _fieldName, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        [Category("ReadConcern")]
        public void CreateCommand_should_create_the_correct_command(
            [Values("3.0.0", "3.2.0")] string serverVersion,
            [Values(null, ReadConcernLevel.Local, ReadConcernLevel.Majority)] ReadConcernLevel? readConcernLevel)
        {
            var semanticServerVersion = SemanticVersion.Parse(serverVersion);
            var subject = new DistinctOperation<int>(_collectionNamespace, _valueSerializer, _fieldName, _messageEncoderSettings)
            {
                Filter = new BsonDocument("x", 1),
                MaxTime = TimeSpan.FromSeconds(20),
                ReadConcern = new ReadConcern(readConcernLevel)
            };

            var expectedResult = new BsonDocument
            {
                { "distinct", _collectionNamespace.CollectionName },
                { "key", _fieldName },
                { "query", BsonDocument.Parse("{ x: 1 }") },
                { "maxTimeMS", 20000 }
            };

            if (!subject.ReadConcern.IsServerDefault)
            {
                expectedResult["readConcern"] = subject.ReadConcern.ToBsonDocument();
            }

            if (!subject.ReadConcern.IsSupported(semanticServerVersion))
            {
                Action act = () => subject.CreateCommand(semanticServerVersion);
                act.ShouldThrow<MongoClientException>();
            }
            else
            {
                var result = subject.CreateCommand(semanticServerVersion);
                result.Should().Be(expectedResult);
            }
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public void Execute_should_return_the_correct_results(
            [Values(false, true)]
            bool async)
        {
            var subject = new DistinctOperation<int>(_collectionNamespace, _valueSerializer, _fieldName, _messageEncoderSettings)
            {
                Filter = BsonDocument.Parse("{ _id : { $gt : 2 } }"),
                MaxTime = TimeSpan.FromSeconds(20),
            };

            var cursor = ExecuteOperation(subject, async);
            var result = ReadCursorToEnd(cursor, async);

            result.Should().HaveCount(2);
            result.Should().OnlyHaveUniqueItems();
            result.Should().Contain(new[] { 2, 3 });
        }

        private void EnsureTestData()
        {
            RunOncePerFixture(() =>
            {
                DropCollection();
                Insert(
                    new BsonDocument("_id", 1).Add("y", 1),
                    new BsonDocument("_id", 2).Add("y", 1),
                    new BsonDocument("_id", 3).Add("y", 2),
                    new BsonDocument("_id", 4).Add("y", 2),
                    new BsonDocument("_id", 5).Add("y", 3));
            });
        }
    }
}
