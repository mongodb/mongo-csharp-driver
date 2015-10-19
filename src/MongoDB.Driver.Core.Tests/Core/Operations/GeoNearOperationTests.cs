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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    [TestFixture]
    public class GeoNearOperationTests : OperationTestBase
    {
        [Test]
        public void Constructor_should_throw_when_collection_namespace_is_null()
        {
            Action act = () => new GeoNearOperation<BsonDocument>(null, 5, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_near_is_null()
        {
            Action act = () => new GeoNearOperation<BsonDocument>(_collectionNamespace, null, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_result_serializer_is_null()
        {
            Action act = () => new GeoNearOperation<BsonDocument>(_collectionNamespace, 5, null, _messageEncoderSettings);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_message_encoder_settings_is_null()
        {
            Action act = () => new GeoNearOperation<BsonDocument>(_collectionNamespace, 5, BsonDocumentSerializer.Instance, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        [Category("ReadConcern")]
        public void CreateCommand_should_create_the_correct_command(
            [Values("3.0.0", "3.2.0")] string serverVersion,
            [Values(null, ReadConcernLevel.Local, ReadConcernLevel.Majority)] ReadConcernLevel? readConcernLevel)
        {
            var semanticServerVersion = SemanticVersion.Parse(serverVersion);
            var distanceMultiplier = 40;
            var filter = new BsonDocument("x", 1);
            var includeLocs = true;
            var limit = 10;
            var maxDistance = 30;
            var maxTime = TimeSpan.FromMilliseconds(50);
            var near = new BsonArray { 10, 20 };
            var spherical = true;
            var uniqueDocs = true;
            var subject = new GeoNearOperation<BsonDocument>(_collectionNamespace, near, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                DistanceMultiplier = distanceMultiplier,
                Filter = filter,
                IncludeLocs = includeLocs,
                Limit = limit,
                MaxDistance = maxDistance,
                MaxTime = maxTime,
                ReadConcern = new ReadConcern(readConcernLevel),
                Spherical = spherical,
                UniqueDocs = uniqueDocs
            };
            var expectedResult = new BsonDocument
            {
                { "geoNear", _collectionNamespace.CollectionName },
                { "near", near },
                { "limit", limit },
                { "maxDistance", maxDistance },
                { "query", filter },
                { "spherical", spherical },
                { "distanceMultiplier", distanceMultiplier },
                { "includeLocs", includeLocs },
                { "uniqueDocs", uniqueDocs },
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
            var subject = new GeoNearOperation<BsonDocument>(
                _collectionNamespace,
                new BsonArray { 1, 2 },
                BsonDocumentSerializer.Instance,
                _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

            result["results"].Should().NotBeNull();
        }

        // helper methods
        private void EnsureTestData()
        {
            RunOncePerFixture(() =>
            {
                DropCollection();
                Insert(Enumerable.Range(1, 5).Select(id => new BsonDocument
                {
                    { "_id", id },
                    { "Location", new BsonArray { id, id + 1 } }
                }));
                CreateIndexes(new CreateIndexRequest(new BsonDocument("Location", "2d")));
            });
        }
    }
}
