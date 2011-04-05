﻿/* Copyright 2010-2011 10gen Inc.
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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.BsonUnitTests.Serialization {
    [TestFixture]
    public class BitArraySerializerTests {
        public class TestClass {
            [BsonRepresentation(BsonType.Binary)]
            public BitArray B;
            [BsonRepresentation(BsonType.String)]
            public BitArray S;
        }

        [Test]
        public void TestNull() {
            var obj = new TestClass {
                B = null,
                S = null
            };
            var json = obj.ToJson();
            var expected = "{ 'B' : null, 'S' : null }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestLength0() {
            var obj = new TestClass {
                B = new BitArray(new bool[0]),
                S = new BitArray(new bool[0])
            };
            var json = obj.ToJson();
            var expected = "{ 'B' : BinData(0, ''), 'S' : '' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestLength1() {
            var obj = new TestClass {
                B = new BitArray(new[] { true }),
                S = new BitArray(new[] { true })
            };
            var json = obj.ToJson();
            var expected = "{ 'B' : { 'Length' : 1, 'Bytes' : BinData(0, 'AQ==') }, 'S' : '1' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(rehydrated.B[0]);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestLength2() {
            var obj = new TestClass {
                B = new BitArray(new[] { true, true }),
                S = new BitArray(new[] { true, true })
            };
            var json = obj.ToJson();
            var expected = "{ 'B' : { 'Length' : 2, 'Bytes' : BinData(0, 'Aw==') }, 'S' : '11' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(rehydrated.B[0]);
            Assert.IsTrue(rehydrated.B[1]);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestLength7() {
            var obj = new TestClass {
                B = new BitArray(new[] { true, false, true, false, true, false, true }),
                S = new BitArray(new[] { true, false, true, false, true, false, true })
            };
            var json = obj.ToJson();
            var expected = "{ 'B' : { 'Length' : 7, 'Bytes' : BinData(0, 'VQ==') }, 'S' : '1010101' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(rehydrated.B[0]);
            Assert.IsTrue(rehydrated.B[2]);
            Assert.IsTrue(rehydrated.B[4]);
            Assert.IsTrue(rehydrated.B[6]);
            Assert.IsFalse(rehydrated.B[1]);
            Assert.IsFalse(rehydrated.B[3]);
            Assert.IsFalse(rehydrated.B[5]);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestLength8() {
            var obj = new TestClass {
                B = new BitArray(new[] { true, false, true, false, true, false, true, false }),
                S = new BitArray(new[] { true, false, true, false, true, false, true, false })
            };
            var json = obj.ToJson();
            var expected = "{ 'B' : BinData(0, 'VQ=='), 'S' : '10101010' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(rehydrated.B[0]);
            Assert.IsTrue(rehydrated.B[2]);
            Assert.IsTrue(rehydrated.B[4]);
            Assert.IsTrue(rehydrated.B[6]);
            Assert.IsFalse(rehydrated.B[1]);
            Assert.IsFalse(rehydrated.B[3]);
            Assert.IsFalse(rehydrated.B[5]);
            Assert.IsFalse(rehydrated.B[7]);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestLength9() {
            var obj = new TestClass {
                B = new BitArray(new[] { true, false, true, false, true, false, true, false, true }),
                S = new BitArray(new[] { true, false, true, false, true, false, true, false, true })
            };
            var json = obj.ToJson();
            var expected = "{ 'B' : { 'Length' : 9, 'Bytes' : BinData(0, 'VQE=') }, 'S' : '101010101' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(rehydrated.B[0]);
            Assert.IsTrue(rehydrated.B[2]);
            Assert.IsTrue(rehydrated.B[4]);
            Assert.IsTrue(rehydrated.B[6]);
            Assert.IsTrue(rehydrated.B[8]);
            Assert.IsFalse(rehydrated.B[1]);
            Assert.IsFalse(rehydrated.B[3]);
            Assert.IsFalse(rehydrated.B[5]);
            Assert.IsFalse(rehydrated.B[7]);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class ByteArraySerializerTests {
        private class C {
            [BsonRepresentation(BsonType.Binary)]
            public byte[] B;
            [BsonRepresentation(BsonType.String)]
            public byte[] S;
        }

        [Test]
        public void TestNull() {
            var c = new C { B = null, S = null };
            var json = c.ToJson();
            var expected = "{ 'B' : null, 'S' : null }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsInstanceOf<C>(rehydrated);
            Assert.AreEqual(null, rehydrated.B);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestEmpty() {
            var c = new C { B = new byte[0], S = new byte[0] };
            var json = c.ToJson();
            var expected = "{ 'B' : BinData(0, ''), 'S' : '' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsInstanceOf<C>(rehydrated);
            Assert.IsTrue(c.B.SequenceEqual(rehydrated.B));
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestLengthOne() {
            var c = new C { B = new byte[] { 1 }, S = new byte[] { 1 } };
            var json = c.ToJson();
            var expected = "{ 'B' : BinData(0, 'AQ=='), 'S' : '01' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsInstanceOf<C>(rehydrated);
            Assert.IsTrue(c.B.SequenceEqual(rehydrated.B));
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestLengthTwo() {
            var c = new C { B = new byte[] { 1, 2 }, S = new byte[] { 1, 2 } };
            var json = c.ToJson();
            var expected = "{ 'B' : BinData(0, 'AQI='), 'S' : '0102' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsInstanceOf<C>(rehydrated);
            Assert.IsTrue(c.B.SequenceEqual(rehydrated.B));
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestLengthNine() {
            var c = new C {
                B = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 },
                S = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }
            };
            var json = c.ToJson();
            var expected = "{ 'B' : BinData(0, 'AQIDBAUGBwgJ'), 'S' : '010203040506070809' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsInstanceOf<C>(rehydrated);
            Assert.IsTrue(c.B.SequenceEqual(rehydrated.B));
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class ByteSerializerTests {
        public class TestClass {
            [BsonRepresentation(BsonType.Binary)]
            public byte B;
            [BsonRepresentation(BsonType.Int32)]
            public byte I;
            [BsonRepresentation(BsonType.Int64)]
            public byte L;
            [BsonRepresentation(BsonType.String)]
            public byte S;
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                B = byte.MinValue,
                I = byte.MinValue,
                L = byte.MinValue,
                S = byte.MinValue
            };
            var json = obj.ToJson();
            var expected = "{ 'B' : BinData(0, 'AA=='), 'I' : 0, 'L' : NumberLong(0), 'S' : '00' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                B = 0,
                I = 0,
                L = 0,
                S = 0
            };
            var json = obj.ToJson();
            var expected = "{ 'B' : BinData(0, 'AA=='), 'I' : 0, 'L' : NumberLong(0), 'S' : '00' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                B = 1,
                I = 1,
                L = 1,
                S = 1
            };
            var json = obj.ToJson();
            var expected = "{ 'B' : BinData(0, 'AQ=='), 'I' : 1, 'L' : NumberLong(1), 'S' : '01' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                B = byte.MaxValue,
                I = byte.MaxValue,
                L = byte.MaxValue,
                S = byte.MaxValue
            };
            var json = obj.ToJson();
            var expected = "{ 'B' : BinData(0, '/w=='), 'I' : 255, 'L' : NumberLong(255), 'S' : 'ff' }".Replace("'", "\"");
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

    [TestFixture]
    public class CultureInfoSerializerTests {
        public class TestClass {
            public CultureInfo V { get; set; }
        }

        [Test]
        public void TestNull() {
            var obj = new TestClass {
                V = null
            };
            var json = obj.ToJson();
            var expected = "{ 'V' : null }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestEnUs() {
            var obj = new TestClass {
                V = new CultureInfo("en-US")
            };
            var json = obj.ToJson();
            var expected = "{ 'V' : 'en-US' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }


        [Test]
        public void TestEnUsUseUserOverrideFalse() {
            var obj = new TestClass {
                V = new CultureInfo("en-US", false)
            };
            var json = obj.ToJson();
            var expected = "{ 'V' : { 'Name' : 'en-US', 'UseUserOverride' : false } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class DateTimeOffsetSerializerTests {
        public class TestClass {
            [BsonRepresentation(BsonType.Array)]
            public DateTimeOffset A { get; set; }
            [BsonRepresentation(BsonType.Document)]
            public DateTimeOffset D { get; set; }
            [BsonRepresentation(BsonType.String)]
            public DateTimeOffset S { get; set; }
        }

        // TODO: more DateTimeOffset tests

        [Test]
        public void TestSerializeDateTimeOffset() {
            var value = new DateTimeOffset(new DateTime(2010, 10, 8, 11, 29, 0), TimeSpan.FromHours(-4));
            var isoDate = value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.FFFZ");
            var obj = new TestClass {
                A = value,
                D = value,
                S = value
            };
            var json = obj.ToJson();
            var expected = "{ 'A' : #A, 'D' : #D, 'S' : '#S' }";
            expected = expected.Replace("#A", string.Format("[NumberLong('{0}'), {1}]", value.DateTime.Ticks, value.Offset.TotalMinutes));
            expected = expected.Replace("#D",
                "{ 'DateTime' : ISODate('#D'), 'Ticks' : NumberLong('#T'), 'Offset' : #O }"
                    .Replace("#D", isoDate)
                    .Replace("#T", value.DateTime.Ticks.ToString())
                    .Replace("#O", XmlConvert.ToString(value.Offset.TotalMinutes))
            );
            expected = expected.Replace("#S", "2010-10-08T11:29:00-04:00");
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class DecimalSerializerTests {
        public class TestClass {
            public decimal X;
            [BsonRepresentation(BsonType.Array)]
            public decimal A;
            [BsonRepresentation(BsonType.Double)]
            public decimal D;
            [BsonRepresentation(BsonType.Int32)]
            public decimal I;
            [BsonRepresentation(BsonType.Int64)]
            public decimal L;
            [BsonRepresentation(BsonType.String)]
            public decimal S;
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                X = decimal.MinValue,
                A = decimal.MinValue,
                D = decimal.MinValue,
                I = decimal.MinValue,
                L = decimal.MinValue,
                S = decimal.MinValue
            };
            var json = obj.ToJson();
            var expected = "{ 'X' : '#S', 'A' : [-1, -1, -1, -2147483648], 'D' : #D, 'I' : #I, 'L' : NumberLong('#L'), 'S' : '#S' }";
            expected = expected.Replace("#S", XmlConvert.ToString(decimal.MinValue));
            expected = expected.Replace("#D", "-1.7976931348623157E+308");
            expected = expected.Replace("#I", XmlConvert.ToString(int.MinValue));
            expected = expected.Replace("#L", XmlConvert.ToString(long.MinValue));
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                X = decimal.MaxValue,
                A = decimal.MaxValue,
                D = decimal.MaxValue,
                I = decimal.MaxValue,
                L = decimal.MaxValue,
                S = decimal.MaxValue
            };
            var json = obj.ToJson();
            var expected = "{ 'X' : '#S', 'A' : [-1, -1, -1, 0], 'D' : #D, 'I' : #I, 'L' : NumberLong('#L'), 'S' : '#S' }";
            expected = expected.Replace("#S", XmlConvert.ToString(decimal.MaxValue));
            expected = expected.Replace("#D", "1.7976931348623157E+308");
            expected = expected.Replace("#I", XmlConvert.ToString(int.MaxValue));
            expected = expected.Replace("#L", XmlConvert.ToString(long.MaxValue));
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMinusOne() {
            var obj = new TestClass {
                X = decimal.MinusOne,
                A = decimal.MinusOne,
                D = decimal.MinusOne,
                I = decimal.MinusOne,
                L = decimal.MinusOne,
                S = decimal.MinusOne
            };
            var json = obj.ToJson();
            var expected = "{ 'X' : '-1', 'A' : [1, 0, 0, -2147483648], 'D' : -1.0, 'I' : -1, 'L' : NumberLong(-1), 'S' : '-1' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                X = decimal.Zero,
                A = decimal.Zero,
                D = decimal.Zero,
                I = decimal.Zero,
                L = decimal.Zero,
                S = decimal.Zero
            };
            var json = obj.ToJson();
            var expected = "{ 'X' : '0', 'A' : [0, 0, 0, 0], 'D' : 0.0, 'I' : 0, 'L' : NumberLong(0), 'S' : '0' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                X = decimal.One,
                A = decimal.One,
                D = decimal.One,
                I = decimal.One,
                L = decimal.One,
                S = decimal.One
            };
            var json = obj.ToJson();
            var expected = "{ 'X' : '1', 'A' : [1, 0, 0, 0], 'D' : 1.0, 'I' : 1, 'L' : NumberLong(1), 'S' : '1' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOnePointThree() {
            var obj = new TestClass {
                X = 1.3m,
                A = 1.3m,
                D = 1.3m,
                I = 1.0m,
                L = 1.0m,
                S = 1.3m
            };
            var json = obj.ToJson();
            var expected = "{ 'X' : '1.3', 'A' : [13, 0, 0, 65536], 'D' : 1.3, 'I' : 1, 'L' : NumberLong(1), 'S' : '1.3' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOnePointFive() {
            var obj = new TestClass {
                X = 1.5m,
                A = 1.5m,
                D = 1.5m,
                I = 1.0m,
                L = 1.0m,
                S = 1.5m
            };
            var json = obj.ToJson();
            var expected = "{ 'X' : '1.5', 'A' : [15, 0, 0, 65536], 'D' : 1.5, 'I' : 1, 'L' : NumberLong(1), 'S' : '1.5' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class Int16SerializerTests {
        public class TestClass {
            [BsonRepresentation(BsonType.Double)]
            public short D;
            [BsonRepresentation(BsonType.Int32)]
            public short I;
            [BsonRepresentation(BsonType.Int64)]
            public short L;
            [BsonRepresentation(BsonType.String)]
            public short S;
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                D = short.MinValue,
                I = short.MinValue,
                L = short.MinValue,
                S = short.MinValue
            };
            var json = obj.ToJson();
            var expected = ("{ 'D' : -32768.0, 'I' : -32768, 'L' : NumberLong(-32768), 'S' : '-32768' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMinusOne() {
            var obj = new TestClass {
                D = -1,
                I = -1,
                L = -1,
                S = -1
            };
            var json = obj.ToJson();
            var expected = ("{ 'D' : -1.0, 'I' : -1, 'L' : NumberLong(-1), 'S' : '-1' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                D = 0,
                I = 0,
                L = 0,
                S = 0
            };
            var json = obj.ToJson();
            var expected = ("{ 'D' : 0.0, 'I' : 0, 'L' : NumberLong(0), 'S' : '0' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                D = 1,
                I = 1,
                L = 1,
                S = 1
            };
            var json = obj.ToJson();
            var expected = ("{ 'D' : 1.0, 'I' : 1, 'L' : NumberLong(1), 'S' : '1' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                D = short.MaxValue,
                I = short.MaxValue,
                L = short.MaxValue,
                S = short.MaxValue
            };
            var json = obj.ToJson();
            var expected = ("{ 'D' : 32767.0, 'I' : 32767, 'L' : NumberLong(32767), 'S' : '32767' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class SByteSerializerTests {
        public class TestClass {
            [BsonRepresentation(BsonType.Binary)]
            public sbyte B;
            [BsonRepresentation(BsonType.Int32)]
            public sbyte I;
            [BsonRepresentation(BsonType.Int64)]
            public sbyte L;
            [BsonRepresentation(BsonType.String)]
            public sbyte S;
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                B = sbyte.MinValue,
                I = sbyte.MinValue,
                L = sbyte.MinValue,
                S = sbyte.MinValue
            };
            var json = obj.ToJson();
            var expected = ("{ 'B' : BinData(0, 'gA=='), 'I' : -128, 'L' : NumberLong(-128), 'S' : '80' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMinusOne() {
            var obj = new TestClass {
                B = -1,
                I = -1,
                L = -1,
                S = -1
            };
            var json = obj.ToJson();
            var expected = ("{ 'B' : BinData(0, '/w=='), 'I' : -1, 'L' : NumberLong(-1), 'S' : 'ff' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                B = 0,
                I = 0,
                L = 0,
                S = 0
            };
            var json = obj.ToJson();
            var expected = ("{ 'B' : BinData(0, 'AA=='), 'I' : 0, 'L' : NumberLong(0), 'S' : '00' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                B = 1,
                I = 1,
                L = 1,
                S = 1
            };
            var json = obj.ToJson();
            var expected = ("{ 'B' : BinData(0, 'AQ=='), 'I' : 1, 'L' : NumberLong(1), 'S' : '01' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                B = sbyte.MaxValue,
                I = sbyte.MaxValue,
                L = sbyte.MaxValue,
                S = sbyte.MaxValue
            };
            var json = obj.ToJson();
            var expected = ("{ 'B' : BinData(0, 'fw=='), 'I' : 127, 'L' : NumberLong(127), 'S' : '7f' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class SingleSerializerTests {
        public class TestClass {
            [BsonRepresentation(BsonType.Double)]
            public float D;
            [BsonRepresentation(BsonType.Int32, AllowTruncation = true)]
            public float I;
            [BsonRepresentation(BsonType.Int64, AllowTruncation = true)]
            public float L;
            [BsonRepresentation(BsonType.String)]
            public float S;
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                D = float.MinValue,
                I = 0,
                L = 0,
                S = float.MinValue
            };
            var json = obj.ToJson();
            var expected = "{ 'D' : #D, 'I' : 0, 'L' : NumberLong(0), 'S' : '#S' }";
            expected = expected.Replace("#D", "-1.7976931348623157E+308");
            expected = expected.Replace("#S", "-3.40282347E+38");
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMinusOne() {
            var obj = new TestClass {
                D = -1.0f,
                I = -1.0f,
                L = -1.0f,
                S = -1.0f
            };
            var json = obj.ToJson();
            var expected = "{ 'D' : -1.0, 'I' : -1, 'L' : NumberLong(-1), 'S' : '-1' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                D = 0.0f,
                I = 0.0f,
                L = 0.0f,
                S = 0.0f
            };
            var json = obj.ToJson();
            var expected = "{ 'D' : 0.0, 'I' : 0, 'L' : NumberLong(0), 'S' : '0' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                D = 1.0f,
                I = 1.0f,
                L = 1.0f,
                S = 1.0f
            };
            var json = obj.ToJson();
            var expected = "{ 'D' : 1.0, 'I' : 1, 'L' : NumberLong(1), 'S' : '1' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOnePointFive() {
            var obj = new TestClass {
                D = 1.5f,
                I = 1.5f,
                L = 1.5f,
                S = 1.5f
            };
            var json = obj.ToJson();
            var expected = "{ 'D' : 1.5, 'I' : 1, 'L' : NumberLong(1), 'S' : '1.5' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                D = float.MaxValue,
                I = 0,
                L = 0,
                S = float.MaxValue
            };
            var json = obj.ToJson();
            var expected = "{ 'D' : #D, 'I' : 0, 'L' : NumberLong(0), 'S' : '#S' }";
            expected = expected.Replace("#D", "1.7976931348623157E+308");
            expected = expected.Replace("#S", "3.40282347E+38");
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestNaN() {
            var obj = new TestClass {
                D = float.NaN,
                I = 0,
                L = 0,
                S = float.NaN
            };
            var json = obj.ToJson();
            var expected = "{ 'D' : NaN, 'I' : 0, 'L' : NumberLong(0), 'S' : 'NaN' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestNegativeInfinity() {
            var obj = new TestClass {
                D = float.NegativeInfinity,
                I = 0,
                L = 0,
                S = float.NegativeInfinity
            };
            var json = obj.ToJson();
            var expected = "{ 'D' : -Infinity, 'I' : 0, 'L' : NumberLong(0), 'S' : '-Infinity' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestPositiveInfinity() {
            var obj = new TestClass {
                D = float.PositiveInfinity,
                I = 0,
                L = 0,
                S = float.PositiveInfinity
            };
            var json = obj.ToJson();
            var expected = "{ 'D' : Infinity, 'I' : 0, 'L' : NumberLong(0), 'S' : 'Infinity' }".Replace("'", "\"");
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
            var expected = "{ 'L' : NumberLong('#L'), 'S' : '#S' }";
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
            var expected = "{ 'L' : NumberLong(-600000000), 'S' : '-00:01:00' }".Replace("'", "\"");
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
            var expected = "{ 'L' : NumberLong(#L), 'S' : '-00:00:01' }";
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
            var expected = "{ 'L' : NumberLong(0), 'S' : '00:00:00' }".Replace("'", "\"");
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
            var expected = "{ 'L' : NumberLong(#L), 'S' : '00:00:01' }";
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
            var expected = "{ 'L' : NumberLong(600000000), 'S' : '00:01:00' }".Replace("'", "\"");
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
            var expected = "{ 'L' : NumberLong('#L'), 'S' : '#S' }";
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
            [BsonRepresentation(BsonType.Double)]
            public ushort D;
            [BsonRepresentation(BsonType.Int32)]
            public ushort I;
            [BsonRepresentation(BsonType.Int64)]
            public ushort L;
            [BsonRepresentation(BsonType.String)]
            public ushort S;
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                D = ushort.MinValue,
                I = ushort.MinValue,
                L = ushort.MinValue,
                S = ushort.MinValue
            };
            var json = obj.ToJson();
            var expected = "{ 'D' : 0.0, 'I' : 0, 'L' : NumberLong(0), 'S' : '0' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                D = 0,
                I = 0,
                L = 0,
                S = 0
            };
            var json = obj.ToJson();
            var expected = "{ 'D' : 0.0, 'I' : 0, 'L' : NumberLong(0), 'S' : '0' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                D = 1,
                I = 1,
                L = 1,
                S = 1
            };
            var json = obj.ToJson();
            var expected = "{ 'D' : 1.0, 'I' : 1, 'L' : NumberLong(1), 'S' : '1' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                D = ushort.MaxValue,
                I = ushort.MaxValue,
                L = ushort.MaxValue,
                S = ushort.MaxValue
            };
            var json = obj.ToJson();
            var expected = "{ 'D' : 65535.0, 'I' : 65535, 'L' : NumberLong(65535), 'S' : '65535' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class UInt32SerializerTests {
        public class TestClass {
            [BsonRepresentation(BsonType.Double)]
            public uint D;
            [BsonRepresentation(BsonType.Int32, AllowOverflow=true)]
            public uint I;
            [BsonRepresentation(BsonType.Int64)]
            public uint L;
            [BsonRepresentation(BsonType.String)]
            public uint S;
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                D = uint.MinValue,
                I = uint.MinValue,
                L = uint.MinValue,
                S = uint.MinValue
            };
            var json = obj.ToJson();
            var expected = "{ 'D' : 0.0, 'I' : 0, 'L' : NumberLong(0), 'S' : '0' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                D = 0,
                I = 0,
                L = 0,
                S = 0
            };
            var json = obj.ToJson();
            var expected = "{ 'D' : 0.0, 'I' : 0, 'L' : NumberLong(0), 'S' : '0' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                D = 1,
                I = 1,
                L = 1,
                S = 1
            };
            var json = obj.ToJson();
            var expected = "{ 'D' : 1.0, 'I' : 1, 'L' : NumberLong(1), 'S' : '1' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                D = uint.MaxValue,
                I = uint.MaxValue,
                L = uint.MaxValue,
                S = uint.MaxValue
            };
            var json = obj.ToJson();
            var expected = "{ 'D' : 4294967295.0, 'I' : -1, 'L' : NumberLong('4294967295'), 'S' : '4294967295' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class UInt64SerializerTests {
        public class TestClass {
            [BsonRepresentation(BsonType.Double)]
            public ulong D;
            [BsonRepresentation(BsonType.Int32)]
            public ulong I;
            [BsonRepresentation(BsonType.Int64, AllowOverflow=true)]
            public ulong L;
            [BsonRepresentation(BsonType.String)]
            public ulong S;
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                D = ulong.MinValue,
                I = ulong.MinValue,
                L = ulong.MinValue,
                S = ulong.MinValue
            };
            var json = obj.ToJson();
            var expected = "{ 'D' : 0.0, 'I' : 0, 'L' : NumberLong(0), 'S' : '0' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                D = 0,
                I = 0,
                L = 0,
                S = 0
            };
            var json = obj.ToJson();
            var expected = "{ 'D' : 0.0, 'I' : 0, 'L' : NumberLong(0), 'S' : '0' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                D = 1,
                I = 1,
                L = 1,
                S = 1
            };
            var json = obj.ToJson();
            var expected = "{ 'D' : 1.0, 'I' : 1, 'L' : NumberLong(1), 'S' : '1' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                D = 0,
                I = 0,
                L = ulong.MaxValue,
                S = ulong.MaxValue
            };
            var json = obj.ToJson();
            var expected = "{ 'D' : 0.0, 'I' : 0, 'L' : NumberLong(-1), 'S' : '18446744073709551615' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class UriSerializerTests {
        public class TestClass {
            public Uri V { get; set; }
        }

        [Test]
        public void TestNull() {
            var obj = new TestClass {
                V = null
            };
            var json = obj.ToJson();
            var expected = "{ 'V' : null }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestHttp() {
            var obj = new TestClass {
                V = new Uri("http://www.cnn.com")
            };
            var json = obj.ToJson();
            var expected = "{ 'V' : 'http://www.cnn.com/' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMongoDB() {
            var obj = new TestClass {
                V = new Uri("mongodb://localhost/?safe=true")
            };
            var json = obj.ToJson();
            var expected = "{ 'V' : 'mongodb://localhost/?safe=true' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class VersionSerializerTests {
        public class TestClass {
            [BsonRepresentation(BsonType.Document)]
            public Version D { get; set; }
            [BsonRepresentation(BsonType.String)]
            public Version S { get; set; }
        }

        [Test]
        public void TestNull() {
            var obj = new TestClass {
                D = null,
                S = null
            };
            var json = obj.ToJson();
            var expected = "{ 'D' : null, 'S' : null }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestVersion12() {
            var version = new Version(1, 2);
            var obj = new TestClass {
                D = version,
                S = version
            };
            var json = obj.ToJson();
            var expected = "{ 'D' : { 'Major' : 1, 'Minor' : 2 }, 'S' : '1.2' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestVersion123() {
            var version = new Version(1, 2, 3);
            var obj = new TestClass {
                D = version,
                S = version
            };
            var json = obj.ToJson();
            var expected = "{ 'D' : { 'Major' : 1, 'Minor' : 2, 'Build' : 3 }, 'S' : '1.2.3' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestVersion1234() {
            var version = new Version(1, 2, 3, 4);
            var obj = new TestClass {
                D = version,
                S = version
            };
            var json = obj.ToJson();
            var expected = "{ 'D' : { 'Major' : 1, 'Minor' : 2, 'Build' : 3, 'Revision' : 4 }, 'S' : '1.2.3.4' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
