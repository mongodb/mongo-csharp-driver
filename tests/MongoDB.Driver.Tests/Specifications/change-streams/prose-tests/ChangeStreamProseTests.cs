/* Copyright 2010-present MongoDB Inc.
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

using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests.Specifications.change_streams.prose_tests
{
    public class ChangeStreamProseTests : LoggableTestClass
    {
        public ChangeStreamProseTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public void ChangeStream_events_should_be_split_using_changeStreamSplitLargeEvent()
        {
            RequireServer.Check()
                .Supports(Feature.ChangeStreamSplitEventStage)
                .ClusterTypes(ClusterType.ReplicaSet, ClusterType.Sharded);

            const int dataLength = 10 * 1024 * 1024;

            var collectionName = DriverTestConfiguration.CollectionNamespace.CollectionName;
            using var client = DriverTestConfiguration.CreateMongoClient();
            var db = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            db.DropCollection(collectionName);
            db.CreateCollection(collectionName, new() { ChangeStreamPreAndPostImagesOptions = new ChangeStreamPreAndPostImagesOptions() { Enabled = true } });
            var collection = db.GetCollection<BsonDocument>(collectionName);

            collection.InsertOne(new BsonDocument("value", new string('q', dataLength)));

            var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>().ChangeStreamSplitLargeEvent();
            var changeStreamCursor = collection.Watch(pipeline, new() { FullDocumentBeforeChange = ChangeStreamFullDocumentBeforeChangeOption.Required });

            var update = Builders<BsonDocument>.Update.Set("value", new string('z', dataLength));
            collection.UpdateOne(Builders<BsonDocument>.Filter.Empty, update);

            var events = changeStreamCursor.ToEnumerable().Take(2).ToArray();
            events[0].SplitEvent.Fragment.Should().Be(1);
            events[0].SplitEvent.Of.Should().Be(2);
            events[1].SplitEvent.Fragment.Should().Be(2);
            events[1].SplitEvent.Of.Should().Be(2);
        }
    }
}
