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

using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Tests;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class CursorTests
    {
        [Theory]
        [InlineData(0, true)]
        [InlineData(1, false)]
        public void Constructor_should_call_Dispose_on_channelSource_if_cursorId_is_zero(int cursorId, bool shouldCallDispose)
        {
            var mockChannelSource = new Mock<IChannelSource>();
            new AsyncCursor<BsonDocument>(
                mockChannelSource.Object,
                new CollectionNamespace("databaseName", "collectionName"),
                new BsonDocument(), // query
                new BsonDocument[0], // firstBatch
                cursorId,
                null, // batchSize
                null, // limit
                BsonDocumentSerializer.Instance,
                null); // messageEncoderSettings

            mockChannelSource.Verify(s => s.Dispose(), Times.Exactly(shouldCallDispose ? 1 : 0));
        }

        // seems like a good place to put this
        [Fact]
        public void Kill_Cursor_should_actually_kill_the_cursor()
        {
            MongoDatabase database = LegacyTestConfiguration.Database;
            MongoCollection<BsonDocument> collection = database.GetCollection(GetType().Name);

            // insert 1000 documents into a collection
            for (int i = 0; i < 1000; i++)
            {
                collection.Insert(new BsonDocument("x", i));
            }

//            do a "find" on the collection and iterate the cursor past the first document
//            store the cursor id in a variable
            var cursor = collection.Find(Query.EQ("x", 1));
            cursor = collection.Find(Query.EQ("x", 2));
//            assert the cursor id is nonzero - do we have cursor ID?
            cursor.Should().NotBeNull();
//            kill the cursor (explicitly, or by letting it go out of scope, depending on the driver)
//            assert that the driver receives a server reply to "killCursors" with "ok: 1", an empty "cursorsNotFound" array, an empty "cursorsAlive" array, an empty "cursorsUnknown" array, and a "cursorsKilled" array with one element equal to the cursor id

        }
    }
}
