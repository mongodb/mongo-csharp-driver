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

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.DefaultSerializer;
using MongoDB.Bson.Serialization;

namespace MongoDB.BsonUnitTests.DefaultSerializer {
    [TestFixture]
    public class ByteArraySerializerTests {
        private class C {
            public byte[] BA { get; set; }
        }

        [Test]
        public void TestNull() {
            var c = new C { BA = null };
            var json = c.ToJson();
            var expected = "{ 'BA' : null }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsInstanceOf<C>(rehydrated);
            Assert.AreEqual(null, rehydrated.BA);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestEmpty() {
            var c = new C { BA = new byte[0] };
            var json = c.ToJson();
            var expected = "{ 'BA' : { '$binary' : '', '$type' : '00' } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsInstanceOf<C>(rehydrated);
            Assert.IsTrue(c.BA.SequenceEqual(rehydrated.BA));
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestLengthOne() {
            var c = new C { BA = new byte[] { 1 } };
            var json = c.ToJson();
            var expected = "{ 'BA' : { '$binary' : 'AQ==', '$type' : '00' } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsInstanceOf<C>(rehydrated);
            Assert.IsTrue(c.BA.SequenceEqual(rehydrated.BA));
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestLengthTwo() {
            var c = new C { BA = new byte[] { 1, 2 } };
            var json = c.ToJson();
            var expected = "{ 'BA' : { '$binary' : 'AQI=', '$type' : '00' } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsInstanceOf<C>(rehydrated);
            Assert.IsTrue(c.BA.SequenceEqual(rehydrated.BA));
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestLengthNine() {
            var c = new C { BA = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 } };
            var json = c.ToJson();
            var expected = "{ 'BA' : { '$binary' : 'AQIDBAUGBwgJ', '$type' : '00' } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsInstanceOf<C>(rehydrated);
            Assert.IsTrue(c.BA.SequenceEqual(rehydrated.BA));
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class ByteSerializerTests {
        public class TestClass {
            public byte V { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                V = byte.MinValue
            };
            var json = obj.ToJson();
            var expected = "{ 'V' : 0 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                V = 0
            };
            var json = obj.ToJson();
            var expected = "{ 'V' : 0 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                V = 1
            };
            var json = obj.ToJson();
            var expected = "{ 'V' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                V = byte.MaxValue
            };
            var json = obj.ToJson();
            var expected = "{ 'V' : 255 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class CharSerializerTests {
        public class TestClass {
            [BsonRepresentation(BsonType.Int32)]
            public char I { get; set; }
            [BsonRepresentation(BsonType.String)]
            public char S { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                I = char.MinValue,
                S = char.MinValue
            };
            var json = obj.ToJson();
            var expected = "{ 'I' : 0, 'S' : '\\u0000' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        public void TestZero() {
            var obj = new TestClass {
                I = (char) 0,
                S = (char) 0
            };
            var json = obj.ToJson();
            var expected = "{ 'I' : 0, 'S' : '\\u0000' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                I = (char) 1,
                S = (char) 1
            };
            var json = obj.ToJson();
            var expected = "{ 'I' : 1, 'S' : '\\u0001' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestA() {
            var obj = new TestClass {
                I = 'A',
                S = 'A'
            };
            var json = obj.ToJson();
            var expected = "{ 'I' : #, 'S' : 'A' }".Replace("#", ((int) 'A').ToString()).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                I = char.MaxValue,
                S = char.MaxValue
            };
            var json = obj.ToJson();
            var expected = "{ 'I' : #, 'S' : '\\uffff' }".Replace("#", ((int) char.MaxValue).ToString()).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    // TODO: CultureInfoSerializeTests

    [TestFixture]
    public class DateTimeOffsetSerializerTests {
        public class TestClass {
            [BsonRepresentation(BsonType.Array)]
            public DateTimeOffset A { get; set; }
            [BsonRepresentation(BsonType.String)]
            public DateTimeOffset S { get; set; }
        }

        // TODO: more DateTimeOffset tests

        [Test]
        public void TestSerializeDateTimeOffset() {
            var value = new DateTimeOffset(new DateTime(2010, 10, 8, 11, 29, 0), TimeSpan.FromHours(-4));
            var obj = new TestClass {
                A = value,
                S = value
            };
            var json = obj.ToJson();
            var expected = "{ 'A' : #A, 'S' : '2010-10-08T11:29:00-04:00' }";
            expected = expected.Replace("#A", string.Format("[{0}, {1}]", value.DateTime.Ticks, value.Offset.TotalMinutes));
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    // TODO: DecimalSerializerTests

    [TestFixture]
    public class Int16SerializerTests {
        public class TestClass {
            public short V { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                V = short.MinValue
            };
            var json = obj.ToJson();
            var expected = ("{ 'V' : -32768 }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMinusOne() {
            var obj = new TestClass {
                V = -1
            };
            var json = obj.ToJson();
            var expected = ("{ 'V' : -1 }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                V = 0
            };
            var json = obj.ToJson();
            var expected = ("{ 'V' : 0 }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                V = 1
            };
            var json = obj.ToJson();
            var expected = ("{ 'V' : 1 }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                V = short.MaxValue
            };
            var json = obj.ToJson();
            var expected = ("{ 'V' : 32767 }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class SByteSerializerTests {
        public class TestClass {
            public sbyte V { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                V = sbyte.MinValue
            };
            var json = obj.ToJson();
            var expected = ("{ 'V' : -128 }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMinusOne() {
            var obj = new TestClass {
                V = -1
            };
            var json = obj.ToJson();
            var expected = ("{ 'V' : -1 }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                V = 0
            };
            var json = obj.ToJson();
            var expected = ("{ 'V' : 0 }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                V = 1
            };
            var json = obj.ToJson();
            var expected = ("{ 'V' : 1 }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                V = sbyte.MaxValue
            };
            var json = obj.ToJson();
            var expected = ("{ 'V' : 127 }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class SingleSerializerTests {
        public class TestClass {
            public float V { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                V = float.MinValue
            };
            var json = obj.ToJson();
            var expected = ("{ 'V' : # }").Replace("#", double.MinValue.ToString()).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMinusOne() {
            var obj = new TestClass {
                V = -1.0F
            };
            var json = obj.ToJson();
            var expected = ("{ 'V' : -1 }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                V = 0.0F
            };
            var json = obj.ToJson();
            var expected = ("{ 'V' : 0 }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                V = 1.0F
            };
            var json = obj.ToJson();
            var expected = ("{ 'V' : 1 }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                V = float.MaxValue
            };
            var json = obj.ToJson();
            var expected = ("{ 'V' : # }").Replace("#", double.MaxValue.ToString()).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestNaN() {
            var obj = new TestClass {
                V = float.NaN
            };
            var json = obj.ToJson();
            var expected = ("{ 'V' : NaN }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestNegativeInfinity() {
            var obj = new TestClass {
                V = float.NegativeInfinity
            };
            var json = obj.ToJson();
            var expected = ("{ 'V' : -Infinity }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestPositiveInfinity() {
            var obj = new TestClass {
                V = float.PositiveInfinity
            };
            var json = obj.ToJson();
            var expected = ("{ 'V' : Infinity }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class TimeSpanSerializerTests {
        public class TestClass {
            [BsonRepresentation(BsonType.Int64)]
            public TimeSpan L { get; set; }
            [BsonRepresentation(BsonType.String)]
            public TimeSpan S { get; set; }
        }

        [Test]
        public void TestMinValue() {
            var obj = new TestClass {
                L = TimeSpan.MinValue,
                S = TimeSpan.MinValue
            };
            var json = obj.ToJson();
            var expected = "{ 'L' : #L, 'S' : '#S' }";
            expected = expected.Replace("#L", TimeSpan.MinValue.Ticks.ToString());
            expected = expected.Replace("#S", TimeSpan.MinValue.ToString());
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMinusOneMinute() {
            var obj = new TestClass {
                L = TimeSpan.FromMinutes(-1),
                S = TimeSpan.FromMinutes(-1)
            };
            var json = obj.ToJson();
            var expected = "{ 'L' : #L, 'S' : '-00:01:00' }";
            expected = expected.Replace("#L", TimeSpan.FromMinutes(-1).Ticks.ToString());
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMinusOneSecond() {
            var obj = new TestClass {
                L = TimeSpan.FromSeconds(-1),
                S = TimeSpan.FromSeconds(-1)
            };
            var json = obj.ToJson();
            var expected = "{ 'L' : #L, 'S' : '-00:00:01' }";
            expected = expected.Replace("#L", TimeSpan.FromSeconds(-1).Ticks.ToString());
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                L = TimeSpan.Zero,
                S = TimeSpan.Zero
            };
            var json = obj.ToJson();
            var expected = "{ 'L' : #L, 'S' : '00:00:00' }";
            expected = expected.Replace("#L", TimeSpan.Zero.Ticks.ToString());
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOneSecond() {
            var obj = new TestClass {
                L = TimeSpan.FromSeconds(1),
                S = TimeSpan.FromSeconds(1)
            };
            var json = obj.ToJson();
            var expected = "{ 'L' : #L, 'S' : '00:00:01' }";
            expected = expected.Replace("#L", TimeSpan.FromSeconds(1).Ticks.ToString());
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOneMinute() {
            var obj = new TestClass {
                L = TimeSpan.FromMinutes(1),
                S = TimeSpan.FromMinutes(1)
            };
            var json = obj.ToJson();
            var expected = "{ 'L' : #L, 'S' : '00:01:00' }";
            expected = expected.Replace("#L", TimeSpan.FromMinutes(1).Ticks.ToString());
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMaxValue() {
            var obj = new TestClass {
                L = TimeSpan.MaxValue,
                S = TimeSpan.MaxValue
            };
            var json = obj.ToJson();
            var expected = "{ 'L' : #L, 'S' : '#S' }";
            expected = expected.Replace("#L", TimeSpan.MaxValue.Ticks.ToString());
            expected = expected.Replace("#S", TimeSpan.MaxValue.ToString());
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class UInt16SerializerTests {
        public class TestClass {
            public ushort V { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                V = ushort.MinValue
            };
            var json = obj.ToJson();
            var expected = "{ 'V' : 0 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                V = 0
            };
            var json = obj.ToJson();
            var expected = "{ 'V' : 0 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                V = 1
            };
            var json = obj.ToJson();
            var expected = "{ 'V' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                V = ushort.MaxValue
            };
            var json = obj.ToJson();
            var expected = "{ 'V' : 65535 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class UInt32SerializerTests {
        public class TestClass {
            public uint V { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                V = uint.MinValue
            };
            var json = obj.ToJson();
            var expected = "{ 'V' : 0 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                V = 0
            };
            var json = obj.ToJson();
            var expected = "{ 'V' : 0 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                V = 1
            };
            var json = obj.ToJson();
            var expected = "{ 'V' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOverflow() {
            var obj = new TestClass {
                V = 4000000000
            };
            var json = obj.ToJson();
            var expected = "{ 'V' : -294967296 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.AreEqual(4000000000, rehydrated.V);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                V = uint.MaxValue
            };
            var json = obj.ToJson();
            var expected = "{ 'V' : -1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.AreEqual(obj.V, rehydrated.V);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class UInt64SerializerTests {
        public class TestClass {
            public ulong V { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                V = ulong.MinValue
            };
            var json = obj.ToJson();
            var expected = "{ 'V' : 0 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                V = 0
            };
            var json = obj.ToJson();
            var expected = "{ 'V' : 0 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                V = 1
            };
            var json = obj.ToJson();
            var expected = "{ 'V' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                V = ulong.MaxValue
            };
            var json = obj.ToJson();
            var expected = "{ 'V' : -1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
