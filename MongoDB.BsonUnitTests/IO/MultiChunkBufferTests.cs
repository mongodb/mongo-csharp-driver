/* Copyright 2010-2014 MongoDB Inc.
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

using MongoDB.Bson.IO;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.IO
{
    [TestFixture]
    public class MultiChunkBufferTests
    {
        [Test]
        public void TestGetSingleChunkSlice()
        {
            var chunkSize = BsonChunkPool.Default.ChunkSize;
            var capacity = chunkSize * 3;
            using (var buffer = ByteBufferFactory.Create(BsonChunkPool.Default, capacity))
            {
                buffer.Length = capacity;
                buffer.MakeReadOnly();
                var slice = buffer.GetSlice(chunkSize, 1);
                Assert.IsInstanceOf<SingleChunkBuffer>(slice);
            }
        }

        [Test]
        public void TestGetMultipleChunkSlice()
        {
            var chunkSize = BsonChunkPool.Default.ChunkSize;
            var capacity = chunkSize * 3;
            using (var buffer = ByteBufferFactory.Create(BsonChunkPool.Default, capacity))
            {
                buffer.Length = capacity;
                buffer.MakeReadOnly();
                var slice = buffer.GetSlice(chunkSize, chunkSize + 1);
                Assert.IsInstanceOf<MultiChunkBuffer>(slice);
            }
        }
    }
}
