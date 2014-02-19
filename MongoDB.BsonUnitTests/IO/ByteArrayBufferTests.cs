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

using System.IO;
using MongoDB.Bson.IO;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.IO
{
    [TestFixture]
    public class ByteArrayBufferTests
    {
        [Test]
        public void TestWriteBackingBytes()
        {
            var backingBytes = new byte[100];
            using (var buffer = new ByteArrayBuffer(backingBytes, 0, 0, false))
            {
                var segment = buffer.WriteBackingBytes(10);
                Assert.AreSame(backingBytes, segment.Array);
                Assert.AreEqual(0, segment.Offset);
                Assert.AreEqual(10, segment.Count);
                buffer.Position += 1;
                Assert.AreEqual(1, buffer.Position);
                Assert.AreEqual(1, buffer.Length);
            }
        }

        [Test]
        public void TestWriteByte()
        {
            var backingBytes = new byte[100];
            using (var buffer = new ByteArrayBuffer(backingBytes, 0, 0, false))
            {
                Assert.AreEqual(0, backingBytes[0]);
                Assert.AreEqual(0, backingBytes[1]);
                buffer.WriteByte(1);
                Assert.AreEqual(1, backingBytes[0]);
                Assert.AreEqual(0, backingBytes[1]);
                Assert.AreEqual(1, buffer.Position);
                Assert.AreEqual(1, buffer.Length);
            }
        }

        [Test]
        public void TestWriteBytes()
        {
            var bytes = new[] { (byte)1, (byte)2 };

            var backingBytes = new byte[100];
            using (var buffer = new ByteArrayBuffer(backingBytes, 0, 0, false))
            {
                Assert.AreEqual(0, backingBytes[0]);
                Assert.AreEqual(0, backingBytes[1]);
                Assert.AreEqual(0, backingBytes[2]);
                buffer.WriteBytes(bytes);
                Assert.AreEqual(1, backingBytes[0]);
                Assert.AreEqual(2, backingBytes[1]);
                Assert.AreEqual(0, backingBytes[2]);
                Assert.AreEqual(2, buffer.Position);
                Assert.AreEqual(2, buffer.Length);
            }
        }

        [Test]
        public void TestWriteBytesFromByteBuffer()
        {
            var bytes = new[] { (byte)1, (byte)2 };
            var source = new ByteArrayBuffer(bytes, 0, bytes.Length, true);

            var backingBytes = new byte[100];
            using (var buffer = new ByteArrayBuffer(backingBytes, 0, 0, false))
            {
                Assert.AreEqual(0, backingBytes[0]);
                Assert.AreEqual(0, backingBytes[1]);
                Assert.AreEqual(0, backingBytes[2]);
                buffer.WriteBytes(source);
                Assert.AreEqual(1, backingBytes[0]);
                Assert.AreEqual(2, backingBytes[1]);
                Assert.AreEqual(0, backingBytes[2]);
                Assert.AreEqual(2, buffer.Position);
                Assert.AreEqual(2, buffer.Length);
            }
        }

        [Test]
        public void TestWriteTo()
        {
            var backingBytes = new byte[100];
            using (var buffer = new ByteArrayBuffer(backingBytes, 0, 0, false))
            {
                buffer.WriteBytes(new[] { (byte)1, (byte)2 });

                using (var memoryStream = new MemoryStream())
                {
                    buffer.WriteTo(memoryStream);
                    Assert.AreEqual(2, memoryStream.Length);
                }
            }
        }
    }
}
