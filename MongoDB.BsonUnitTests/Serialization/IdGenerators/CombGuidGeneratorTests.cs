/* Copyright 2010-2013 10gen Inc.
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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Serialization
{
    [TestFixture]
    public class CombGuidGeneratorTests
    {
        private CombGuidGenerator _generator = new CombGuidGenerator();

        [Test]
        public void TestNewCombGuid()
        {
            var guid = Guid.NewGuid();
            var timestamp = new DateTime(2013, 4, 2, 0, 0, 0, 500, DateTimeKind.Utc); // half a second past midnight
            var combGuid = _generator.NewCombGuid(guid, timestamp);

            var expectedDays = (short)(timestamp.Date - new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Days;
            var expectedTimeTicks = 150; // half a second in SQL Server resolution

            var bytes = combGuid.ToByteArray();
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes, 10, 2);
                Array.Reverse(bytes, 12, 4);
            }
            var days = BitConverter.ToInt16(bytes, 10);
            var timeTicks = BitConverter.ToInt32(bytes, 12);

            Assert.IsTrue(guid.ToByteArray().Take(10).SequenceEqual(bytes.Take(10))); // first 10 bytes are from the base Guid
            Assert.AreEqual(expectedDays, days);
            Assert.AreEqual(expectedTimeTicks, timeTicks);
        }

        [Test]
        public void TestIsEmpty()
        {
            Assert.IsTrue(_generator.IsEmpty(null));
            Assert.IsTrue(_generator.IsEmpty(Guid.Empty));
            Assert.IsFalse(_generator.IsEmpty(Guid.NewGuid()));
        }
    }
}
