/* Copyright 2010 10gen Inc.
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

using MongoDB.BsonLibrary.IO;
using MongoDB.BsonLibrary.Serialization;

namespace MongoDB.BsonLibrary.UnitTests.Serialization.PropertySerializers {
    [TestFixture]
    public class DateTimeOffsetPropertySerializerTests {
        public class TestClass {
            static TestClass() {
                BsonClassMap.RegisterClassMap<TestClass>(
                    cm => {
                        cm.MapProperty(e => e.DateTimeOffset, "dto");
                    }
                );
            }

            public DateTimeOffset DateTimeOffset { get; set; }
        }

        [Test]
        public void TestSerializeDateTimeOffset() {
            var dateTime = new DateTime(2010, 10, 8, 11, 29, 0);
            var obj = new TestClass { DateTimeOffset = new DateTimeOffset(dateTime, TimeSpan.FromHours(-4)) };
            var json = obj.ToJson();
            var expected = "{ 'dto' : { '_t' : 'System.DateTimeOffset', 'dt' : '2010-10-08T11:29:00', 'o' : '-04:00' } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
