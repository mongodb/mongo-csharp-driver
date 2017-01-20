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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class GeoSearchOperationTests : OperationTestBase
    {
        [Fact]
        public void Constructor_should_throw_when_collection_namespace_is_null()
        {
            Action act = () => new GeoSearchOperation<BsonDocument>(null, 5, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_near_is_null()
        {
            Action act = () => new GeoSearchOperation<BsonDocument>(_collectionNamespace, null, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_result_serializer_is_null()
        {
            Action act = () => new GeoSearchOperation<BsonDocument>(_collectionNamespace, 5, null, _messageEncoderSettings);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_message_encoder_settings_is_null()
        {
            Action act = () => new GeoSearchOperation<BsonDocument>(_collectionNamespace, 5, BsonDocumentSerializer.Instance, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_create_the_correct_command(
            [Values("3.0.0", "3.2.0")] string serverVersionString,
            [Values(null, ReadConcernLevel.Local, ReadConcernLevel.Majority)] ReadConcernLevel? readConcernLevel)
        {
            var serverVersion = SemanticVersion.Parse(serverVersionString);
            var filter = new BsonDocument("x", 1);
            var limit = 10;
            var maxDistance = 30;
            var maxTime = TimeSpan.FromMilliseconds(50);
            var near = new BsonArray { 10, 20 };
            var readConcern = new ReadConcern(readConcernLevel);
            var subject = new GeoSearchOperation<BsonDocument>(_collectionNamespace, near, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Search = filter,
                Limit = limit,
                MaxDistance = maxDistance,
                MaxTime = maxTime,
                ReadConcern = readConcern
            };

            if (!readConcern.IsServerDefault && !Feature.ReadConcern.IsSupported(serverVersion))
            {
                var exception = Record.Exception(() => subject.CreateCommand(serverVersion));

                exception.Should().BeOfType<MongoClientException>();
            }
            else
            {
                var result = subject.CreateCommand(serverVersion);

                var expectedResult = new BsonDocument
                {
                    { "geoSearch", _collectionNamespace.CollectionName },
                    { "near", near },
                    { "limit", limit },
                    { "maxDistance", maxDistance },
                    { "search", filter },
                    { "maxTimeMS", maxTime.TotalMilliseconds },
                    { "readConcern", () => readConcern.ToBsonDocument(), !readConcern.IsServerDefault }
                };
                result.Should().Be(expectedResult);

            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet);
            EnsureTestData();
            var subject = new GeoSearchOperation<BsonDocument>(
                _collectionNamespace,
                new BsonArray { 1, 2 },
                BsonDocumentSerializer.Instance,
                _messageEncoderSettings)
            {
                MaxDistance = 20,
                Search = new BsonDocument("Type", "restaraunt")
            };

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
                    { "Location", new BsonArray { id, id + 1 } },
                    { "Type", "restaraunt" }
                }));
                CreateIndexes(new CreateIndexRequest(new BsonDocument("Location", "geoHaystack").Add("Type", 1))
                {
                    BucketSize = 10
                });
            });
        }
    }
}
