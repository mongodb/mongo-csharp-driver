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
using System.Xml;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Bson.DefaultSerializer;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.BsonUnitTests.DefaultSerializer {
    [TestFixture]
    public class BooleanSerializerTests {
        public class TestClass {
            public bool N;
            [BsonRepresentation(BsonType.Boolean)]
            public bool B;
            [BsonRepresentation(BsonType.Double)]
            public bool D;
            [BsonRepresentation(BsonType.Int32)]
            public bool I;
            [BsonRepresentation(BsonType.Int64)]
            public bool L;
            [BsonRepresentation(BsonType.String)]
            public bool S;
        }

        [Test]
        public void TestFalse() {
            var obj = new TestClass {
                N = false, B = false, D = false, I = false, L = false, S = false
            };
            var json = obj.ToJson();
            var expected = "{ 'N' : false, 'B' : false, 'D' : 0, 'I' : 0, 'L' : 0, 'S' : 'false' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestTrue() {
            var obj = new TestClass {
                N = true, B = true, D = true, I = true, L = true, S = true
            };
            var json = obj.ToJson();
            var expected = "{ 'N' : true, 'B' : true, 'D' : 1, 'I' : 1, 'L' : 1, 'S' : 'true' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class DateTimeSerializerTests {
        public class TestClass {
            public DateTime Default;
            [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
            public DateTime Local;
            [BsonDateTimeOptions(Kind = DateTimeKind.Unspecified)]
            public DateTime Unspecified;
            [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
            public DateTime Utc;
            [BsonDateTimeOptions(Representation = BsonType.Int64)]
            public DateTime Ticks;
            [BsonDateTimeOptions(Representation = BsonType.String)]
            public DateTime String;
            [BsonDateTimeOptions(Representation = BsonType.String, DateOnly = true)]
            public DateTime DateOnlyString;
        }

        private static string expectedTemplate = 
            "{ 'Default' : #Default, 'Local' : #Local, 'Unspecified' : #Unspecified, 'Utc' : #Utc, 'Ticks' : #Ticks, 'String' : '#String', 'DateOnlyString' : '#DateOnlyString' }";

        [Test]
        public void TestMinLocal() {
            var minLocal = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Local);
            var obj = new TestClass {
                Default = minLocal,
                Local = minLocal,
                Unspecified = minLocal,
                Utc = minLocal,
                Ticks = minLocal,
                String = minLocal,
                DateOnlyString = minLocal.Date
            };
            var json = obj.ToJson();
            var expected = expectedTemplate;
            expected = expected.Replace("#Default", "{ '$date' : -62135596800000 }");
            expected = expected.Replace("#Local", "{ '$date' : -62135596800000 }");
            expected = expected.Replace("#Unspecified", "{ '$date' : -62135596800000 }");
            expected = expected.Replace("#Utc", "{ '$date' : -62135596800000 }");
            expected = expected.Replace("#Ticks", "0");
            expected = expected.Replace("#String", "0001-01-01T00:00:00Z");
            expected = expected.Replace("#DateOnlyString", "0001-01-01");
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.AreEqual(DateTime.MinValue, rehydrated.Default);
            Assert.AreEqual(DateTime.MinValue, rehydrated.Local);
            Assert.AreEqual(DateTime.MinValue, rehydrated.Unspecified);
            Assert.AreEqual(DateTime.MinValue, rehydrated.Utc);
            Assert.AreEqual(DateTime.MinValue, rehydrated.Ticks);
            Assert.AreEqual(DateTime.MinValue, rehydrated.String);
            Assert.AreEqual(DateTime.MinValue.Date, rehydrated.DateOnlyString);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.Default.Kind);
            Assert.AreEqual(DateTimeKind.Local, rehydrated.Local.Kind);
            Assert.AreEqual(DateTimeKind.Unspecified, rehydrated.Unspecified.Kind);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.Utc.Kind);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.Ticks.Kind);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.String.Kind);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.DateOnlyString.Kind);
        }

        [Test]
        public void TestMinUnspecified() {
            var minUnspecified = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Unspecified);
            var obj = new TestClass {
                Default = minUnspecified,
                Local = minUnspecified,
                Unspecified = minUnspecified,
                Utc = minUnspecified,
                Ticks = minUnspecified,
                String = minUnspecified,
                DateOnlyString = minUnspecified.Date
            };
            var json = obj.ToJson();
            var expected = expectedTemplate;
            expected = expected.Replace("#Default", "{ '$date' : -62135596800000 }");
            expected = expected.Replace("#Local", "{ '$date' : -62135596800000 }");
            expected = expected.Replace("#Unspecified", "{ '$date' : -62135596800000 }");
            expected = expected.Replace("#Utc", "{ '$date' : -62135596800000 }");
            expected = expected.Replace("#Ticks", "0");
            expected = expected.Replace("#String", "0001-01-01T00:00:00Z");
            expected = expected.Replace("#DateOnlyString", "0001-01-01");
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.AreEqual(DateTime.MinValue, rehydrated.Default);
            Assert.AreEqual(DateTime.MinValue, rehydrated.Local);
            Assert.AreEqual(DateTime.MinValue, rehydrated.Unspecified);
            Assert.AreEqual(DateTime.MinValue, rehydrated.Utc);
            Assert.AreEqual(DateTime.MinValue, rehydrated.Ticks);
            Assert.AreEqual(DateTime.MinValue, rehydrated.String);
            Assert.AreEqual(DateTime.MinValue.Date, rehydrated.DateOnlyString);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.Default.Kind);
            Assert.AreEqual(DateTimeKind.Local, rehydrated.Local.Kind);
            Assert.AreEqual(DateTimeKind.Unspecified, rehydrated.Unspecified.Kind);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.Utc.Kind);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.Ticks.Kind);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.String.Kind);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.DateOnlyString.Kind);
        }

        [Test]
        public void TestMinUtc() {
            var minUtc = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
            var obj = new TestClass {
                Default = minUtc,
                Local = minUtc,
                Unspecified = minUtc,
                Utc = minUtc,
                Ticks = minUtc,
                String = minUtc,
                DateOnlyString = minUtc.Date
            };
            var json = obj.ToJson();
            var expected = expectedTemplate;
            expected = expected.Replace("#Default", "{ '$date' : -62135596800000 }");
            expected = expected.Replace("#Local", "{ '$date' : -62135596800000 }");
            expected = expected.Replace("#Unspecified", "{ '$date' : -62135596800000 }");
            expected = expected.Replace("#Utc", "{ '$date' : -62135596800000 }");
            expected = expected.Replace("#Ticks", "0");
            expected = expected.Replace("#String", "0001-01-01T00:00:00Z");
            expected = expected.Replace("#DateOnlyString", "0001-01-01");
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.AreEqual(DateTime.MinValue, rehydrated.Default);
            Assert.AreEqual(DateTime.MinValue, rehydrated.Local);
            Assert.AreEqual(DateTime.MinValue, rehydrated.Unspecified);
            Assert.AreEqual(DateTime.MinValue, rehydrated.Utc);
            Assert.AreEqual(DateTime.MinValue, rehydrated.Ticks);
            Assert.AreEqual(DateTime.MinValue, rehydrated.String);
            Assert.AreEqual(DateTime.MinValue.Date, rehydrated.DateOnlyString);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.Default.Kind);
            Assert.AreEqual(DateTimeKind.Local, rehydrated.Local.Kind);
            Assert.AreEqual(DateTimeKind.Unspecified, rehydrated.Unspecified.Kind);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.Utc.Kind);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.Ticks.Kind);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.String.Kind);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.DateOnlyString.Kind);
        }

        [Test]
        public void TestMaxLocal() {
            var maxLocal = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Local);
            var obj = new TestClass {
                Default = maxLocal,
                Local = maxLocal,
                Unspecified = maxLocal,
                Utc = maxLocal,
                Ticks = maxLocal,
                String = maxLocal,
                DateOnlyString = maxLocal.Date
            };
            var json = obj.ToJson();
            var expected = expectedTemplate;
            expected = expected.Replace("#Default", "{ '$date' : 253402300800000 }");
            expected = expected.Replace("#Local", "{ '$date' : 253402300800000 }");
            expected = expected.Replace("#Unspecified", "{ '$date' : 253402300800000 }");
            expected = expected.Replace("#Utc", "{ '$date' : 253402300800000 }");
            expected = expected.Replace("#Ticks", "3155378975999999999");
            expected = expected.Replace("#String", "9999-12-31T23:59:59.9999999Z");
            expected = expected.Replace("#DateOnlyString", "9999-12-31");
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.AreEqual(DateTime.MaxValue, rehydrated.Default);
            Assert.AreEqual(DateTime.MaxValue, rehydrated.Local);
            Assert.AreEqual(DateTime.MaxValue, rehydrated.Unspecified);
            Assert.AreEqual(DateTime.MaxValue, rehydrated.Utc);
            Assert.AreEqual(DateTime.MaxValue, rehydrated.Ticks);
            Assert.AreEqual(DateTime.MaxValue, rehydrated.String);
            Assert.AreEqual(DateTime.MaxValue.Date, rehydrated.DateOnlyString);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.Default.Kind);
            Assert.AreEqual(DateTimeKind.Local, rehydrated.Local.Kind);
            Assert.AreEqual(DateTimeKind.Unspecified, rehydrated.Unspecified.Kind);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.Utc.Kind);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.Ticks.Kind);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.String.Kind);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.DateOnlyString.Kind);
        }

        [Test]
        public void TestMaxUnspecified() {
            var maxUnspecified = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Unspecified);
            var obj = new TestClass {
                Default = maxUnspecified,
                Local = maxUnspecified,
                Unspecified = maxUnspecified,
                Utc = maxUnspecified,
                Ticks = maxUnspecified,
                String = maxUnspecified,
                DateOnlyString = maxUnspecified.Date
            };
            var json = obj.ToJson();
            var expected = expectedTemplate;
            expected = expected.Replace("#Default", "{ '$date' : 253402300800000 }");
            expected = expected.Replace("#Local", "{ '$date' : 253402300800000 }");
            expected = expected.Replace("#Unspecified", "{ '$date' : 253402300800000 }");
            expected = expected.Replace("#Utc", "{ '$date' : 253402300800000 }");
            expected = expected.Replace("#Ticks", "3155378975999999999");
            expected = expected.Replace("#String", "9999-12-31T23:59:59.9999999Z");
            expected = expected.Replace("#DateOnlyString", "9999-12-31");
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.AreEqual(DateTime.MaxValue, rehydrated.Default);
            Assert.AreEqual(DateTime.MaxValue, rehydrated.Local);
            Assert.AreEqual(DateTime.MaxValue, rehydrated.Unspecified);
            Assert.AreEqual(DateTime.MaxValue, rehydrated.Utc);
            Assert.AreEqual(DateTime.MaxValue, rehydrated.Ticks);
            Assert.AreEqual(DateTime.MaxValue, rehydrated.String);
            Assert.AreEqual(DateTime.MaxValue.Date, rehydrated.DateOnlyString);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.Default.Kind);
            Assert.AreEqual(DateTimeKind.Local, rehydrated.Local.Kind);
            Assert.AreEqual(DateTimeKind.Unspecified, rehydrated.Unspecified.Kind);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.Utc.Kind);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.Ticks.Kind);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.String.Kind);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.DateOnlyString.Kind);
        }

        [Test]
        public void TestMaxUtc() {
            var maxUtc = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
            var obj = new TestClass {
                Default = maxUtc,
                Local = maxUtc,
                Unspecified = maxUtc,
                Utc = maxUtc,
                Ticks = maxUtc,
                String = maxUtc,
                DateOnlyString = maxUtc.Date
            };
            var json = obj.ToJson();
            var expected = expectedTemplate;
            expected = expected.Replace("#Default", "{ '$date' : 253402300800000 }");
            expected = expected.Replace("#Local", "{ '$date' : 253402300800000 }");
            expected = expected.Replace("#Unspecified", "{ '$date' : 253402300800000 }");
            expected = expected.Replace("#Utc", "{ '$date' : 253402300800000 }");
            expected = expected.Replace("#Ticks", "3155378975999999999");
            expected = expected.Replace("#String", "9999-12-31T23:59:59.9999999Z");
            expected = expected.Replace("#DateOnlyString", "9999-12-31");
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.AreEqual(DateTime.MaxValue, rehydrated.Default);
            Assert.AreEqual(DateTime.MaxValue, rehydrated.Local);
            Assert.AreEqual(DateTime.MaxValue, rehydrated.Unspecified);
            Assert.AreEqual(DateTime.MaxValue, rehydrated.Utc);
            Assert.AreEqual(DateTime.MaxValue, rehydrated.Ticks);
            Assert.AreEqual(DateTime.MaxValue, rehydrated.String);
            Assert.AreEqual(DateTime.MaxValue.Date, rehydrated.DateOnlyString);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.Default.Kind);
            Assert.AreEqual(DateTimeKind.Local, rehydrated.Local.Kind);
            Assert.AreEqual(DateTimeKind.Unspecified, rehydrated.Unspecified.Kind);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.Utc.Kind);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.Ticks.Kind);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.String.Kind);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.DateOnlyString.Kind);
        }

        [Test]
        public void TestLocal() {
            var local = DateTime.Now;
            var utc = local.ToUniversalTime();
            var obj = new TestClass {
                Default = local,
                Local = local,
                Unspecified = local,
                Utc = local,
                Ticks = local,
                String = local,
                DateOnlyString = local.Date
            };
            var json = obj.ToJson();
            var expected = expectedTemplate;
            var milliseconds = (long) (utc - BsonConstants.UnixEpoch).TotalMilliseconds;
            var utcJson = "{ '$date' : # }".Replace("#", milliseconds.ToString());
            expected = expected.Replace("#Default", utcJson);
            expected = expected.Replace("#Local", utcJson);
            expected = expected.Replace("#Unspecified", utcJson);
            expected = expected.Replace("#Utc", utcJson);
            expected = expected.Replace("#Ticks", utc.Ticks.ToString());
            expected = expected.Replace("#String", XmlConvert.ToString(local, XmlDateTimeSerializationMode.RoundtripKind));
            expected = expected.Replace("#DateOnlyString", local.Date.ToString("yyyy-MM-dd"));
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            var utcTruncated = BsonConstants.UnixEpoch.AddMilliseconds(milliseconds); // loss of precision
            var localTruncated = utcTruncated.ToLocalTime();
            Assert.AreEqual(utcTruncated, rehydrated.Default);
            Assert.AreEqual(localTruncated, rehydrated.Local);
            Assert.AreEqual(localTruncated, rehydrated.Unspecified);
            Assert.AreEqual(utcTruncated, rehydrated.Utc);
            Assert.AreEqual(utc, rehydrated.Ticks);
            Assert.AreEqual(utc, rehydrated.String);
            Assert.AreEqual(local.Date, rehydrated.DateOnlyString);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.Default.Kind);
            Assert.AreEqual(DateTimeKind.Local, rehydrated.Local.Kind);
            Assert.AreEqual(DateTimeKind.Unspecified, rehydrated.Unspecified.Kind);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.Utc.Kind);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.Ticks.Kind);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.String.Kind);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.DateOnlyString.Kind);
        }

        [Test]
        public void TestUnspecified() {
            var unspecified = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
            var utc = unspecified.ToUniversalTime();
            var obj = new TestClass {
                Default = unspecified,
                Local = unspecified,
                Unspecified = unspecified,
                Utc = unspecified,
                Ticks = unspecified,
                String = unspecified,
                DateOnlyString = unspecified.Date
            };
            var json = obj.ToJson();
            var expected = expectedTemplate;
            var milliseconds = (long) (utc - BsonConstants.UnixEpoch).TotalMilliseconds;
            var utcJson = "{ '$date' : # }".Replace("#", milliseconds.ToString());
            expected = expected.Replace("#Default", utcJson);
            expected = expected.Replace("#Local", utcJson);
            expected = expected.Replace("#Unspecified", utcJson);
            expected = expected.Replace("#Utc", utcJson);
            expected = expected.Replace("#Ticks", utc.Ticks.ToString());
            expected = expected.Replace("#String", XmlConvert.ToString(unspecified, XmlDateTimeSerializationMode.RoundtripKind));
            expected = expected.Replace("#DateOnlyString", unspecified.Date.ToString("yyyy-MM-dd"));
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            var utcTruncated = BsonConstants.UnixEpoch.AddMilliseconds(milliseconds); // loss of precision
            var localTruncated = utcTruncated.ToLocalTime();
            Assert.AreEqual(utcTruncated, rehydrated.Default);
            Assert.AreEqual(localTruncated, rehydrated.Local);
            Assert.AreEqual(localTruncated, rehydrated.Unspecified);
            Assert.AreEqual(utcTruncated, rehydrated.Utc);
            Assert.AreEqual(utc, rehydrated.Ticks);
            Assert.AreEqual(utc, rehydrated.String);
            Assert.AreEqual(unspecified.Date, rehydrated.DateOnlyString);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.Default.Kind);
            Assert.AreEqual(DateTimeKind.Local, rehydrated.Local.Kind);
            Assert.AreEqual(DateTimeKind.Unspecified, rehydrated.Unspecified.Kind);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.Utc.Kind);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.Ticks.Kind);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.String.Kind);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.DateOnlyString.Kind);
        }

        [Test]
        public void TestUtc() {
            var utc = DateTime.UtcNow;
            var obj = new TestClass {
                Default = utc,
                Local = utc,
                Unspecified = utc,
                Utc = utc,
                Ticks = utc,
                String = utc,
                DateOnlyString = utc.Date
            };
            var json = obj.ToJson();
            var expected = expectedTemplate;
            var milliseconds = (long) (utc - BsonConstants.UnixEpoch).TotalMilliseconds;
            var utcJson = "{ '$date' : # }".Replace("#", milliseconds.ToString());
            expected = expected.Replace("#Default", utcJson);
            expected = expected.Replace("#Local", utcJson);
            expected = expected.Replace("#Unspecified", utcJson);
            expected = expected.Replace("#Utc", utcJson);
            expected = expected.Replace("#Ticks", utc.Ticks.ToString());
            expected = expected.Replace("#String", XmlConvert.ToString(utc, XmlDateTimeSerializationMode.RoundtripKind));
            expected = expected.Replace("#DateOnlyString", utc.Date.ToString("yyyy-MM-dd"));
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            var utcTruncated = BsonConstants.UnixEpoch.AddMilliseconds(milliseconds); // loss of precision
            var localTruncated = utcTruncated.ToLocalTime();
            Assert.AreEqual(utcTruncated, rehydrated.Default);
            Assert.AreEqual(localTruncated, rehydrated.Local);
            Assert.AreEqual(localTruncated, rehydrated.Unspecified);
            Assert.AreEqual(utcTruncated, rehydrated.Utc);
            Assert.AreEqual(utc, rehydrated.Ticks);
            Assert.AreEqual(utc, rehydrated.String);
            Assert.AreEqual(utc.Date, rehydrated.DateOnlyString);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.Default.Kind);
            Assert.AreEqual(DateTimeKind.Local, rehydrated.Local.Kind);
            Assert.AreEqual(DateTimeKind.Unspecified, rehydrated.Unspecified.Kind);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.Utc.Kind);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.Ticks.Kind);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.String.Kind);
            Assert.AreEqual(DateTimeKind.Utc, rehydrated.DateOnlyString.Kind);
        }
    }

    [TestFixture]
    public class DoubleSerializerTests {
        public class TestClass {
            public double Double { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                Double = double.MinValue
            };
            var json = obj.ToJson();
            var expected = ("{ 'Double' : " + double.MinValue.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMinusOne() {
            var obj = new TestClass {
                Double = -1.0
            };
            var json = obj.ToJson();
            var expected = "{ 'Double' : -1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                Double = 0.0
            };
            var json = obj.ToJson();
            var expected = "{ 'Double' : 0 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                Double = 1.0
            };
            var json = obj.ToJson();
            var expected = "{ 'Double' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                Double = double.MaxValue
            };
            var json = obj.ToJson();
            var expected = ("{ 'Double' : " + double.MaxValue.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestNaN() {
            var obj = new TestClass {
                Double = double.NaN
            };
            var json = obj.ToJson();
            var expected = "{ 'Double' : NaN }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestNegativeInfinity() {
            var obj = new TestClass {
                Double = double.NegativeInfinity
            };
            var json = obj.ToJson();
            var expected = "{ 'Double' : -Infinity }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestPositiveInfinity() {
            var obj = new TestClass {
                Double = double.PositiveInfinity
            };
            var json = obj.ToJson();
            var expected = "{ 'Double' : Infinity }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class GuidSerializerTests {
        public class TestClass {
            public Guid Binary { get; set; }
            [BsonRepresentation(BsonType.String)]
            public Guid String { get; set; }
        }

        [Test]
        public void TestEmpty() {
            var guid = Guid.Empty;
            var obj = new TestClass {
                Binary = guid,
                String = guid
            };
            var json = obj.ToJson();
            var expected = "{ 'Binary' : #B, 'String' : #S }";
            expected = expected.Replace("#B", "{ '$binary' : 'AAAAAAAAAAAAAAAAAAAAAA==', '$type' : '03' }");
            expected = expected.Replace("#S", "'00000000-0000-0000-0000-000000000000'");
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestNew() {
            var guid = Guid.NewGuid();
            var obj = new TestClass {
                Binary = guid,
                String = guid
            };
            var json = obj.ToJson();
            var base64 = Convert.ToBase64String(obj.Binary.ToByteArray()).Replace("\\", "\\\\");
            var expected = "{ 'Binary' : #B, 'String' : #S }";
            expected = expected.Replace("#B", "{ '$binary' : '" + base64 + "', '$type' : '03' }");
            expected = expected.Replace("#S", "'" + guid.ToString() + "'");
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class Int32SerializerTests {
        public class TestClass {
            public int Int32 { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                Int32 = int.MinValue
            };
            var json = obj.ToJson();
            var expected = ("{ 'Int32' : " + int.MinValue.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMinusOne() {
            var obj = new TestClass {
                Int32 = -1
            };
            var json = obj.ToJson();
            var expected = "{ 'Int32' : -1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                Int32 = 0
            };
            var json = obj.ToJson();
            var expected = "{ 'Int32' : 0 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                Int32 = 1
            };
            var json = obj.ToJson();
            var expected = "{ 'Int32' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                Int32 = int.MaxValue
            };
            var json = obj.ToJson();
            var expected = ("{ 'Int32' : " + int.MaxValue.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class Int64SerializerTests {
        public class TestClass {
            public long Int64 { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                Int64 = long.MinValue
            };
            var json = obj.ToJson();
            var expected = ("{ 'Int64' : " + long.MinValue.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMinusOne() {
            var obj = new TestClass {
                Int64 = -1
            };
            var json = obj.ToJson();
            var expected = "{ 'Int64' : -1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                Int64 = 0
            };
            var json = obj.ToJson();
            var expected = "{ 'Int64' : 0 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                Int64 = 1
            };
            var json = obj.ToJson();
            var expected = "{ 'Int64' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                Int64 = long.MaxValue
            };
            var json = obj.ToJson();
            var expected = ("{ 'Int64' : " + long.MaxValue.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class ObjectIdSerializerTests {
        public class TestClass {
            public ObjectId ObjectId { get; set; }
            [BsonRepresentation(BsonType.String)]
            public ObjectId String { get; set; }
        }

        [Test]
        public void TestSerializer() {
            var objectId = new ObjectId(1, 2, 3, 4);
            var obj = new TestClass {
                ObjectId = objectId,
                String = objectId
            };
            var json = obj.ToJson();
            var expected = ("{ 'ObjectId' : #O, 'String' : #S }");
            expected = expected.Replace("#O", "{ '$oid' : '000000010000020003000004' }");
            expected = expected.Replace("#S", "'000000010000020003000004'");
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class StringSerializerTests {
        public class TestClass {
            public String String { get; set; }
        }

        [Test]
        public void TestNull() {
            var obj = new TestClass {
                String = null
            };
            var json = obj.ToJson();
            var expected = ("{ 'String' : null }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestEmpty() {
            var obj = new TestClass {
                String = String.Empty
            };
            var json = obj.ToJson();
            var expected = ("{ 'String' : '' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestHelloWorld() {
            var obj = new TestClass {
                String = "Hello World"
            };
            var json = obj.ToJson();
            var expected = ("{ 'String' : 'Hello World' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
