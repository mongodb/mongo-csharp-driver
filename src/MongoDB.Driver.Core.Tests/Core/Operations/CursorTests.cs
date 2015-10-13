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

using System.Threading;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    [TestFixture]
    public class CursorTests
    {
        [TestCase(0, true)]
        [TestCase(1, false)]
        public void Constructor_should_call_Dispose_on_channelSource_if_cursorId_is_zero(int cursorId, bool shouldCallDispose)
        {
            var channelSource = Substitute.For<IChannelSource>();
            new AsyncCursor<BsonDocument>(
                channelSource,
                new CollectionNamespace("databaseName", "collectionName"),
                new BsonDocument(), // query
                new BsonDocument[0], // firstBatch
                cursorId,
                null, // batchSize
                null, // limit
                BsonDocumentSerializer.Instance,
                null); // messageEncoderSettings

            channelSource.Received(shouldCallDispose ? 1 : 0).Dispose();
        }
    }
}
