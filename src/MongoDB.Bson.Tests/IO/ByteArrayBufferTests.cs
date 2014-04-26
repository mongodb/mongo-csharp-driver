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

namespace MongoDB.Bson.Tests.IO
{
    [TestFixture]
    public class ByteArrayBufferTests
    {
        [Test]
        public void TestAccessBackingBytes()
        {
            var backingBytes = new byte[100];
            using (var buffer = new ByteArrayBuffer(backingBytes, 10, 80, false))
            {
                var segment = buffer.AccessBackingBytes(20);
                Assert.AreSame(backingBytes, segment.Array);
                Assert.AreEqual(30, segment.Offset);
                Assert.AreEqual(60, segment.Count);
            }
        }

        [Test]
        public void TestWriteByte()
        {
            var backingBytes = new byte[100];
            using (var buffer = new ByteArrayBuffer(backingBytes, 10, 80, false))
            {
                Assert.AreEqual(0, backingBytes[30]);
                Assert.AreEqual(0, backingBytes[31]);
                buffer.WriteByte(20, 1);
                Assert.AreEqual(1, backingBytes[30]);
                Assert.AreEqual(0, backingBytes[31]);
            }
        }

        [Test]
        public void TestWriteBytes()
        {
            var bytes = new[] { (byte)1, (byte)2 };

            var backingBytes = new byte[100];
            using (var buffer = new ByteArrayBuffer(backingBytes, 10, 80, false))
            {
                Assert.AreEqual(0, backingBytes[30]);
                Assert.AreEqual(0, backingBytes[31]);
                Assert.AreEqual(0, backingBytes[32]);
                buffer.WriteBytes(20, bytes, 0, bytes.Length);
                Assert.AreEqual(1, backingBytes[30]);
                Assert.AreEqual(2, backingBytes[31]);
                Assert.AreEqual(0, backingBytes[32]);
            }
        }

        [Test]
        public void TestWriteTo()
        {
            var backingBytes = new byte[100];
            using (var buffer = new ByteArrayBuffer(backingBytes, 10, 80, false))
            {
                buffer.WriteBytes(0, new[] { (byte)1, (byte)2 }, 0, 2);
                buffer.Length = 2;

                using (var memoryStream = new MemoryStream())
                {
                    buffer.WriteTo(memoryStream);
                    Assert.AreEqual(2, memoryStream.Length);
                }
            }
        }
    }
}
