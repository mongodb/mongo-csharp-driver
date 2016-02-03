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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    [TestFixture]
    public class CountOperationTests : OperationTestBase
    {
        [Test]
        public void Constructor_should_throw_when_collection_namespace_is_null()
        {
            Action act = () => new CountOperation(null, _messageEncoderSettings);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_message_encoder_settings_is_null()
        {
            Action act = () => new CountOperation(_collectionNamespace, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        [Category("ReadConcern")]
        public void CreateCommand_should_create_the_correct_command(
            [Values("3.0.0", "3.2.0")] string serverVersion,
            [Values(null, ReadConcernLevel.Local, ReadConcernLevel.Majority)] ReadConcernLevel? readConcernLevel)
        {
            var semanticServerVersion = SemanticVersion.Parse(serverVersion);
            var filter = new BsonDocument("x", 1);
            var hint = "funny";
            var limit = 10;
            var skip = 30;
            var maxTime = TimeSpan.FromSeconds(20);
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Filter = filter,
                Hint = hint,
                Limit = limit,
                MaxTime = maxTime,
                ReadConcern = new ReadConcern(readConcernLevel),
                Skip = skip
            };
            var expectedResult = new BsonDocument
            {
                { "count", _collectionNamespace.CollectionName },
                { "query", filter },
                { "limit", limit },
                { "skip", skip },
                { "hint", hint },
                { "maxTimeMS", maxTime.TotalMilliseconds }
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
        public void Execute_should_return_expected_result(
            [Values(false, true)]
            bool async)
        {
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

            result.Should().Be(5);
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public void Execute_should_return_expected_result_when_filter_is_provided(
            [Values(false, true)]
            bool async)
        {
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings);
            subject.Filter = BsonDocument.Parse("{ _id : { $gt : 1 } }");

            var result = ExecuteOperation(subject, async);

            result.Should().Be(4);
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public void Execute_should_return_expected_result_when_hint_is_provided(
            [Values(false, true)]
            bool async)
        {
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings);
            subject.Hint = BsonDocument.Parse("{ _id : 1 }");

            var result = ExecuteOperation(subject, async);

            result.Should().Be(5);
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public void Execute_should_return_expected_result_when_limit_is_provided(
            [Values(false, true)]
            bool async)
        {
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings);
            subject.Limit = 3;

            var result = ExecuteOperation(subject, async);

            result.Should().Be(3);
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public void Execute_should_return_expected_result_when_maxTime_is_provided(
            [Values(false, true)]
            bool async)
        {
            if (SupportedFeatures.AreFailPointsSupported(CoreTestConfiguration.ServerVersion))
            {
                // TODO: port FailPoint infrastructure from Driver.Tests to Core.Tests
            }
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public void Execute_should_return_expected_result_when_skip_is_provided(
            [Values(false, true)]
            bool async)
        {
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings);
            subject.Skip = 3;

            var result = ExecuteOperation(subject, async);

            result.Should().Be(2);
        }

        // helper methods
        private void EnsureTestData()
        {
            RunOncePerFixture(() =>
            {
                DropCollection();
                Insert(Enumerable.Range(1, 5).Select(id => new BsonDocument("_id", id)));

            });
        }
    }
}
