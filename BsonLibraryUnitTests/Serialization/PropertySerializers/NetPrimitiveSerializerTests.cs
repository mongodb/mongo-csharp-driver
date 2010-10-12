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
    public class BytePropertySerializerTests {
        public class TestClass {
            public byte Byte { get; set; }
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                Byte = 0
            };
            var json = obj.ToJson();
            var expected = "{ 'Byte' : 0 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                Byte = 1
            };
            var json = obj.ToJson();
            var expected = "{ 'Byte' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                Byte = byte.MaxValue
            };
            var json = obj.ToJson();
            var expected = "{ 'Byte' : 255 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class CharPropertySerializerTests {
        public class TestClass {
            public char Char { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                Char = char.MinValue
            };
            var json = obj.ToJson();
            var expected = "{ 'Char' : 0 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        public void TestZero() {
            var obj = new TestClass {
                Char = (char) 0
            };
            var json = obj.ToJson();
            var expected = "{ 'Char' : 0 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                Char = (char) 1
            };
            var json = obj.ToJson();
            var expected = "{ 'Char' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                Char = char.MaxValue
            };
            var json = obj.ToJson();
            var expected = "{ 'Char' : 65535 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class DateTimeOffsetPropertySerializerTests {
        public class TestClass {
            public DateTimeOffset DateTimeOffset { get; set; }
        }

        // TODO: more DateTimeOffset tests

        [Test]
        public void TestSerializeDateTimeOffset() {
            var dateTime = new DateTime(2010, 10, 8, 11, 29, 0);
            var obj = new TestClass { DateTimeOffset = new DateTimeOffset(dateTime, TimeSpan.FromHours(-4)) };
            var json = obj.ToJson();
            var expected = "{ 'DateTimeOffset' : { '_t' : 'System.DateTimeOffset', 'dt' : '2010-10-08T11:29:00', 'o' : '-04:00' } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    // TODO: DecimalPropertySerializerTests

    [TestFixture]
    public class Int16PropertySerializerTests {
        public class TestClass {
            public short Int16 { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                Int16 = short.MinValue
            };
            var json = obj.ToJson();
            var expected = ("{ 'Int16' : " + short.MinValue.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMinusOne() {
            var obj = new TestClass {
                Int16 = -1
            };
            var json = obj.ToJson();
            var expected = "{ 'Int16' : -1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                Int16 = 0
            };
            var json = obj.ToJson();
            var expected = "{ 'Int16' : 0 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                Int16 = 1
            };
            var json = obj.ToJson();
            var expected = "{ 'Int16' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                Int16 = short.MaxValue
            };
            var json = obj.ToJson();
            var expected = ("{ 'Int16' : " + short.MaxValue.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class SBytePropertySerializerTests {
        public class TestClass {
            public sbyte SByte { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                SByte = sbyte.MinValue
            };
            var json = obj.ToJson();
            var expected = "{ 'SByte' : -128 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMinusOne() {
            var obj = new TestClass {
                SByte = -1
            };
            var json = obj.ToJson();
            var expected = "{ 'SByte' : -1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                SByte = 0
            };
            var json = obj.ToJson();
            var expected = "{ 'SByte' : 0 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                SByte = 1
            };
            var json = obj.ToJson();
            var expected = "{ 'SByte' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                SByte = sbyte.MaxValue
            };
            var json = obj.ToJson();
            var expected = "{ 'SByte' : 127 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class SinglePropertySerializerTests {
        public class TestClass {
            public float Single { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                Single = float.MinValue
            };
            var json = obj.ToJson();
            var expected = ("{ 'Single' : " + ((double) float.MinValue).ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.AreEqual(rehydrated.Single, float.MinValue);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMinusOne() {
            var obj = new TestClass {
                Single = -1.0F
            };
            var json = obj.ToJson();
            var expected = "{ 'Single' : -1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                Single = 0.0F
            };
            var json = obj.ToJson();
            var expected = "{ 'Single' : 0 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                Single = 1.0F
            };
            var json = obj.ToJson();
            var expected = "{ 'Single' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                Single = float.MaxValue
            };
            var json = obj.ToJson();
            var expected = ("{ 'Single' : " + ((double) float.MaxValue).ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.AreEqual(rehydrated.Single, float.MaxValue);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestNaN() {
            var obj = new TestClass {
                Single = float.NaN
            };
            var json = obj.ToJson();
            var expected = "{ 'Single' : NaN }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestNegativeInfinity() {
            var obj = new TestClass {
                Single = float.NegativeInfinity
            };
            var json = obj.ToJson();
            var expected = "{ 'Single' : -Infinity }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestPositiveInfinity() {
            var obj = new TestClass {
                Single = float.PositiveInfinity
            };
            var json = obj.ToJson();
            var expected = "{ 'Single' : Infinity }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class TimeSpanPropertySerializerTests {
        public class TestClass {
            public TimeSpan P { get; set; }
        }

        [Test]
        public void TestMinValue() {
            var obj = new TestClass { P = TimeSpan.MinValue };
            var json = obj.ToJson();
            var expected = "{ 'P' : # }".Replace("#", "{ '_t' : 'System.TimeSpan', 'v' : " + TimeSpan.MinValue.Ticks.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMinusOneMinute() {
            var obj = new TestClass { P = TimeSpan.FromMinutes(-1) };
            var json = obj.ToJson();
            var expected = "{ 'P' : # }".Replace("#", "{ '_t' : 'System.TimeSpan', 'v' : " + TimeSpan.FromMinutes(-1).Ticks.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMinusOneSecond() {
            var obj = new TestClass { P = TimeSpan.FromSeconds(-1) };
            var json = obj.ToJson();
            var expected = "{ 'P' : # }".Replace("#", "{ '_t' : 'System.TimeSpan', 'v' : " + TimeSpan.FromSeconds(-1).Ticks.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass { P = TimeSpan.Zero };
            var json = obj.ToJson();
            var expected = "{ 'P' : # }".Replace("#", "{ '_t' : 'System.TimeSpan', 'v' : 0 }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOneSecond() {
            var obj = new TestClass { P = TimeSpan.FromSeconds(1) };
            var json = obj.ToJson();
            var expected = "{ 'P' : # }".Replace("#", "{ '_t' : 'System.TimeSpan', 'v' : " + TimeSpan.FromSeconds(1).Ticks.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOneMinute() {
            var obj = new TestClass { P = TimeSpan.FromMinutes(1) };
            var json = obj.ToJson();
            var expected = "{ 'P' : # }".Replace("#", "{ '_t' : 'System.TimeSpan', 'v' : " + TimeSpan.FromMinutes(1).Ticks.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMaxValue() {
            var obj = new TestClass { P = TimeSpan.MaxValue };
            var json = obj.ToJson();
            var expected = "{ 'P' : # }".Replace("#", "{ '_t' : 'System.TimeSpan', 'v' : " + TimeSpan.MaxValue.Ticks.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class UInt16PropertySerializerTests {
        public class TestClass {
            public ushort UInt16 { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                UInt16 = ushort.MinValue
            };
            var json = obj.ToJson();
            var expected = ("{ 'UInt16' : " + ushort.MinValue.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                UInt16 = 0
            };
            var json = obj.ToJson();
            var expected = "{ 'UInt16' : 0 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                UInt16 = 1
            };
            var json = obj.ToJson();
            var expected = "{ 'UInt16' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                UInt16 = ushort.MaxValue
            };
            var json = obj.ToJson();
            var expected = ("{ 'UInt16' : " + ushort.MaxValue.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class UInt32PropertySerializerTests {
        public class TestClass {
            public uint UInt32 { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                UInt32 = uint.MinValue
            };
            var json = obj.ToJson();
            var expected = ("{ 'UInt32' : " + uint.MinValue.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                UInt32 = 0
            };
            var json = obj.ToJson();
            var expected = "{ 'UInt32' : 0 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                UInt32 = 1
            };
            var json = obj.ToJson();
            var expected = "{ 'UInt32' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                UInt32 = uint.MaxValue
            };
            var json = obj.ToJson();
            var expected = ("{ 'UInt32' : -1 }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class UInt64PropertySerializerTests {
        public class TestClass {
            public ulong UInt64 { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                UInt64 = ulong.MinValue
            };
            var json = obj.ToJson();
            var expected = ("{ 'UInt64' : " + ulong.MinValue.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                UInt64 = 0
            };
            var json = obj.ToJson();
            var expected = "{ 'UInt64' : 0 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                UInt64 = 1
            };
            var json = obj.ToJson();
            var expected = "{ 'UInt64' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                UInt64 = ulong.MaxValue
            };
            var json = obj.ToJson();
            var expected = ("{ 'UInt64' : -1 }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
