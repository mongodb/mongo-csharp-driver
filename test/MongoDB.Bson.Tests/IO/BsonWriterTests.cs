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

using System;
using System.IO;
using MongoDB.Bson.IO;
using NUnit.Framework;

namespace MongoDB.Bson.Tests.IO
{
    [TestFixture]
    public class BsonWriterTests
    {
        [Test]
        public void TestWriteNameThrowsWhenValueContainsNulls()
        {
            using (var stream = new MemoryStream())
            using (var bsonWriter = new BsonBinaryWriter(stream, BsonBinaryWriterSettings.Defaults))
            {
                Assert.Throws<BsonSerializationException>(() => { bsonWriter.WriteName("a\0b"); });
            }
        }

        [Test]
        public void TestWriteNameThrowsWhenValueIsNull()
        {
            using (var stream = new MemoryStream())
            using (var bsonWriter = new BsonBinaryWriter(stream, BsonBinaryWriterSettings.Defaults))
            {
                Assert.Throws<ArgumentNullException>(() => { bsonWriter.WriteName(null); });
            }
        }
    }
}
