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
    public class ByteSerializerTests {
        public class TestClass {
            public byte C { get; set; }
            [BsonUseCompactRepresentation(false)]
            public byte F { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                C = byte.MinValue,
                F = byte.MinValue
            };
            var json = obj.ToJson();
            var expected = "{ 'C' : 0, 'F' : { '_t' : 'System.Byte', '_v' : 0 } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.AreEqual(rehydrated.C, rehydrated.F);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                C = 0,
                F = 0
            };
            var json = obj.ToJson();
            var expected = "{ 'C' : 0, 'F' : { '_t' : 'System.Byte', '_v' : 0 } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.AreEqual(rehydrated.C, rehydrated.F);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                C = 1,
                F = 1
            };
            var json = obj.ToJson();
            var expected = "{ 'C' : 1, 'F' : { '_t' : 'System.Byte', '_v' : 1 } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.AreEqual(rehydrated.C, rehydrated.F);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                C = byte.MaxValue,
                F = byte.MaxValue
            };
            var json = obj.ToJson();
            var expected = "{ 'C' : 255, 'F' : { '_t' : 'System.Byte', '_v' : 255 } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.AreEqual(rehydrated.C, rehydrated.F);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class CharSerializerTests {
        public class TestClass {
            public char C { get; set; }
            [BsonUseCompactRepresentation(false)]
            public char F { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                C = char.MinValue,
                F = char.MinValue
            };
            var json = obj.ToJson();
            var expected = "{ 'C' : '\\u0000', 'F' : { '_t' : 'System.Char', '_v' : '\\u0000' } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        public void TestZero() {
            var obj = new TestClass {
                C = (char) 0,
                F = (char) 0
            };
            var json = obj.ToJson();
            var expected = "{ 'C' : '\\u0000', 'F' : { '_t' : 'System.Char', '_v' : '\\u0000' } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                C = (char) 1,
                F = (char) 1
            };
            var json = obj.ToJson();
            var expected = "{ 'C' : '\\u0001', 'F' : { '_t' : 'System.Char', '_v' : '\\u0001' } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestA() {
            var obj = new TestClass {
                C = 'A',
                F = 'A'
            };
            var json = obj.ToJson();
            var expected = "{ 'C' : 'A', 'F' : { '_t' : 'System.Char', '_v' : 'A' } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                C = char.MaxValue,
                F = char.MaxValue
            };
            var json = obj.ToJson();
            var expected = "{ 'C' : '\\uffff', 'F' : { '_t' : 'System.Char', '_v' : '\\uffff' } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    // TODO: CultureInfoSerializeTests

    [TestFixture]
    public class DateTimeOffsetSerializerTests {
        public class TestClass {
            [BsonUseCompactRepresentation]
            public DateTimeOffset C { get; set; }
            public DateTimeOffset F { get; set; }
        }

        // TODO: more DateTimeOffset tests

        [Test]
        public void TestSerializeDateTimeOffset() {
            var value = new DateTimeOffset(new DateTime(2010, 10, 8, 11, 29, 0), TimeSpan.FromHours(-4));
            var obj = new TestClass {
                C = value,
                F = value
            };
            var json = obj.ToJson();
            var expected = "{ 'C' : #C, 'F' : #F }";
            expected = expected.Replace("#C", string.Format("[{0}, {1}]", value.DateTime.Ticks, value.Offset.TotalMinutes));
            expected = expected.Replace("#F", "{ '_t' : 'System.DateTimeOffset', 'dt' : '2010-10-08T11:29:00', 'o' : '-04:00' }");
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    // TODO: DecimalSerializerTests

    [TestFixture]
    public class Int16SerializerTests {
        public class TestClass {
            public short C { get; set; }
            [BsonUseCompactRepresentation(false)]
            public short F { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                C = short.MinValue,
                F = short.MinValue
            };
            var json = obj.ToJson();
            var expected = ("{ 'C' : -32768, 'F' : { '_t' : 'System.Int16', '_v' : -32768 } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMinusOne() {
            var obj = new TestClass {
                C = -1,
                F = -1
            };
            var json = obj.ToJson();
            var expected = ("{ 'C' : -1, 'F' : { '_t' : 'System.Int16', '_v' : -1 } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                C = 0,
                F = 0
            };
            var json = obj.ToJson();
            var expected = ("{ 'C' : 0, 'F' : { '_t' : 'System.Int16', '_v' : 0 } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                C = 1,
                F = 1
            };
            var json = obj.ToJson();
            var expected = ("{ 'C' : 1, 'F' : { '_t' : 'System.Int16', '_v' : 1 } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                C = short.MaxValue,
                F = short.MaxValue
            };
            var json = obj.ToJson();
            var expected = ("{ 'C' : 32767, 'F' : { '_t' : 'System.Int16', '_v' : 32767 } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class SByteSerializerTests {
        public class TestClass {
            public sbyte C { get; set; }
            [BsonUseCompactRepresentation(false)]
            public sbyte F { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                C = sbyte.MinValue,
                F = sbyte.MinValue
            };
            var json = obj.ToJson();
            var expected = ("{ 'C' : -128, 'F' : { '_t' : 'System.SByte', '_v' : -128 } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMinusOne() {
            var obj = new TestClass {
                C = -1,
                F = -1
            };
            var json = obj.ToJson();
            var expected = ("{ 'C' : -1, 'F' : { '_t' : 'System.SByte', '_v' : -1 } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                C = 0,
                F = 0
            };
            var json = obj.ToJson();
            var expected = ("{ 'C' : 0, 'F' : { '_t' : 'System.SByte', '_v' : 0 } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                C = 1,
                F = 1
            };
            var json = obj.ToJson();
            var expected = ("{ 'C' : 1, 'F' : { '_t' : 'System.SByte', '_v' : 1 } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                C = sbyte.MaxValue,
                F = sbyte.MaxValue
            };
            var json = obj.ToJson();
            var expected = ("{ 'C' : 127, 'F' : { '_t' : 'System.SByte', '_v' : 127 } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class SingleSerializerTests {
        public class TestClass {
            public float C { get; set; }
            [BsonUseCompactRepresentation(false)]
            public float F { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                C = float.MinValue,
                F = float.MinValue
            };
            var json = obj.ToJson();
            var expected = ("{ 'C' : #, 'F' : { '_t' : 'System.Single', '_v' : # } }").Replace("#", double.MinValue.ToString()).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMinusOne() {
            var obj = new TestClass {
                C = -1.0F,
                F = -1.0F
            };
            var json = obj.ToJson();
            var expected = ("{ 'C' : -1, 'F' : { '_t' : 'System.Single', '_v' : -1 } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                C = 0.0F,
                F = 0.0F
            };
            var json = obj.ToJson();
            var expected = ("{ 'C' : 0, 'F' : { '_t' : 'System.Single', '_v' : 0 } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                C = 1.0F,
                F = 1.0F
            };
            var json = obj.ToJson();
            var expected = ("{ 'C' : 1, 'F' : { '_t' : 'System.Single', '_v' : 1 } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                C = float.MaxValue,
                F = float.MaxValue
            };
            var json = obj.ToJson();
            var expected = ("{ 'C' : #, 'F' : { '_t' : 'System.Single', '_v' : # } }").Replace("#", double.MaxValue.ToString()).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestNaN() {
            var obj = new TestClass {
                C = float.NaN,
                F = float.NaN
            };
            var json = obj.ToJson();
            var expected = ("{ 'C' : NaN, 'F' : { '_t' : 'System.Single', '_v' : NaN } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestNegativeInfinity() {
            var obj = new TestClass {
                C = float.NegativeInfinity,
                F = float.NegativeInfinity
            };
            var json = obj.ToJson();
            var expected = ("{ 'C' : -Infinity, 'F' : { '_t' : 'System.Single', '_v' : -Infinity } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestPositiveInfinity() {
            var obj = new TestClass {
                C = float.PositiveInfinity,
                F = float.PositiveInfinity
            };
            var json = obj.ToJson();
            var expected = ("{ 'C' : Infinity, 'F' : { '_t' : 'System.Single', '_v' : Infinity } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class TimeSpanSerializerTests {
        public class TestClass {
            [BsonUseCompactRepresentation]
            public TimeSpan C { get; set; }
            public TimeSpan F { get; set; }
        }

        [Test]
        public void TestMinValue() {
            var obj = new TestClass {
                C = TimeSpan.MinValue,
                F = TimeSpan.MinValue
            };
            var json = obj.ToJson();
            var expected = "{ 'C' : #C, 'F' : { '_t' : 'System.TimeSpan', '_v' : '#F' } }";
            expected = expected.Replace("#C", TimeSpan.MinValue.Ticks.ToString());
            expected = expected.Replace("#F", TimeSpan.MinValue.ToString());
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMinusOneMinute() {
            var obj = new TestClass {
                C = TimeSpan.FromMinutes(-1),
                F = TimeSpan.FromMinutes(-1)
            };
            var json = obj.ToJson();
            var expected = "{ 'C' : #C, 'F' : { '_t' : 'System.TimeSpan', '_v' : '#F' } }";
            expected = expected.Replace("#C", TimeSpan.FromMinutes(-1).Ticks.ToString());
            expected = expected.Replace("#F", TimeSpan.FromMinutes(-1).ToString());
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMinusOneSecond() {
            var obj = new TestClass {
                C = TimeSpan.FromSeconds(-1),
                F = TimeSpan.FromSeconds(-1)
            };
            var json = obj.ToJson();
            var expected = "{ 'C' : #C, 'F' : { '_t' : 'System.TimeSpan', '_v' : '#F' } }";
            expected = expected.Replace("#C", TimeSpan.FromSeconds(-1).Ticks.ToString());
            expected = expected.Replace("#F", TimeSpan.FromSeconds(-1).ToString());
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                C = TimeSpan.Zero,
                F = TimeSpan.Zero
            };
            var json = obj.ToJson();
            var expected = "{ 'C' : #C, 'F' : { '_t' : 'System.TimeSpan', '_v' : '#F' } }";
            expected = expected.Replace("#C", TimeSpan.Zero.Ticks.ToString());
            expected = expected.Replace("#F", TimeSpan.Zero.ToString());
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOneSecond() {
            var obj = new TestClass {
                C = TimeSpan.FromSeconds(1),
                F = TimeSpan.FromSeconds(1)
            };
            var json = obj.ToJson();
            var expected = "{ 'C' : #C, 'F' : { '_t' : 'System.TimeSpan', '_v' : '#F' } }";
            expected = expected.Replace("#C", TimeSpan.FromSeconds(1).Ticks.ToString());
            expected = expected.Replace("#F", TimeSpan.FromSeconds(1).ToString());
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOneMinute() {
            var obj = new TestClass {
                C = TimeSpan.FromMinutes(1),
                F = TimeSpan.FromMinutes(1)
            };
            var json = obj.ToJson();
            var expected = "{ 'C' : #C, 'F' : { '_t' : 'System.TimeSpan', '_v' : '#F' } }";
            expected = expected.Replace("#C", TimeSpan.FromMinutes(1).Ticks.ToString());
            expected = expected.Replace("#F", TimeSpan.FromMinutes(1).ToString());
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMaxValue() {
            var obj = new TestClass {
                C = TimeSpan.MaxValue,
                F = TimeSpan.MaxValue
            };
            var json = obj.ToJson();
            var expected = "{ 'C' : #C, 'F' : { '_t' : 'System.TimeSpan', '_v' : '#F' } }";
            expected = expected.Replace("#C", TimeSpan.MaxValue.Ticks.ToString());
            expected = expected.Replace("#F", TimeSpan.MaxValue.ToString());
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class UInt16SerializerTests {
        public class TestClass {
            public ushort C { get; set; }
            [BsonUseCompactRepresentation(false)]
            public ushort F { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                C = ushort.MinValue,
                F = ushort.MinValue
            };
            var json = obj.ToJson();
            var expected = "{ 'C' : 0, 'F' : { '_t' : 'System.UInt16', '_v' : 0 } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                C = 0,
                F = 0
            };
            var json = obj.ToJson();
            var expected = "{ 'C' : 0, 'F' : { '_t' : 'System.UInt16', '_v' : 0 } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                C = 1,
                F = 1
            };
            var json = obj.ToJson();
            var expected = "{ 'C' : 1, 'F' : { '_t' : 'System.UInt16', '_v' : 1 } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                C = ushort.MaxValue,
                F = ushort.MaxValue
            };
            var json = obj.ToJson();
            var expected = "{ 'C' : 65535, 'F' : { '_t' : 'System.UInt16', '_v' : 65535 } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class UInt32SerializerTests {
        public class TestClass {
            public uint C { get; set; }
            [BsonUseCompactRepresentation(false)]
            public uint F { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                C = uint.MinValue,
                F = uint.MinValue
            };
            var json = obj.ToJson();
            var expected = "{ 'C' : 0, 'F' : { '_t' : 'System.UInt32', '_v' : 0 } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                C = 0,
                F = 0
            };
            var json = obj.ToJson();
            var expected = "{ 'C' : 0, 'F' : { '_t' : 'System.UInt32', '_v' : 0 } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                C = 1,
                F = 1
            };
            var json = obj.ToJson();
            var expected = "{ 'C' : 1, 'F' : { '_t' : 'System.UInt32', '_v' : 1 } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOverflow() {
            var obj = new TestClass {
                C = 4000000000,
                F = 4000000000
            };
            var json = obj.ToJson();
            var expected = "{ 'C' : -294967296, 'F' : { '_t' : 'System.UInt32', '_v' : -294967296 } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.AreEqual(4000000000, rehydrated.C);
            Assert.AreEqual(4000000000, rehydrated.F);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                C = uint.MaxValue,
                F = uint.MaxValue
            };
            var json = obj.ToJson();
            var expected = "{ 'C' : -1, 'F' : { '_t' : 'System.UInt32', '_v' : -1 } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.AreEqual(obj.C, rehydrated.C);
            Assert.AreEqual(obj.F, rehydrated.F);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class UInt64SerializerTests {
        public class TestClass {
            public ulong C { get; set; }
            [BsonUseCompactRepresentation(false)]
            public ulong F { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                C = ulong.MinValue,
                F = ulong.MinValue
            };
            var json = obj.ToJson();
            var expected = "{ 'C' : 0, 'F' : { '_t' : 'System.UInt64', '_v' : 0 } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                C = 0,
                F = 0
            };
            var json = obj.ToJson();
            var expected = "{ 'C' : 0, 'F' : { '_t' : 'System.UInt64', '_v' : 0 } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                C = 1,
                F = 1
            };
            var json = obj.ToJson();
            var expected = "{ 'C' : 1, 'F' : { '_t' : 'System.UInt64', '_v' : 1 } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                C = ulong.MaxValue,
                F = ulong.MaxValue
            };
            var json = obj.ToJson();
            var expected = "{ 'C' : -1, 'F' : { '_t' : 'System.UInt64', '_v' : -1 } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
