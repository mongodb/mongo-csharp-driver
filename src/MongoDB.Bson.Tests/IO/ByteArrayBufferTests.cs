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
            using (var buffer = new ByteArrayBuffer(backingBytes))
            {
                var segment = buffer.AccessBackingBytes(20);
                Assert.AreSame(backingBytes, segment.Array);
                Assert.AreEqual(20, segment.Offset);
                Assert.AreEqual(80, segment.Count);
            }
        }

        [Test]
        public void TestWriteByte()
        {
            var backingBytes = new byte[100];
            using (var buffer = new ByteArrayBuffer(backingBytes))
            {
                Assert.AreEqual(0, backingBytes[20]);
                Assert.AreEqual(0, backingBytes[21]);
                buffer.SetByte(20, 1);
                Assert.AreEqual(1, backingBytes[20]);
                Assert.AreEqual(0, backingBytes[21]);
            }
        }

        [Test]
        public void TestWriteBytes()
        {
            var bytes = new[] { (byte)1, (byte)2 };

            var backingBytes = new byte[100];
            using (var buffer = new ByteArrayBuffer(backingBytes))
            {
                Assert.AreEqual(0, backingBytes[20]);
                Assert.AreEqual(0, backingBytes[21]);
                Assert.AreEqual(0, backingBytes[22]);
                buffer.SetBytes(20, bytes, 0, bytes.Length);
                Assert.AreEqual(1, backingBytes[20]);
                Assert.AreEqual(2, backingBytes[21]);
                Assert.AreEqual(0, backingBytes[22]);
            }
        }
    }
}
