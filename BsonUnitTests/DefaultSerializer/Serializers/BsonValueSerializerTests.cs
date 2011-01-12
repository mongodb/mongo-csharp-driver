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
    public class BsonArraySerializerTests {
        public class TestClass {
            public TestClass() { }

            public TestClass(
                BsonArray value
            ) {
                this.B = value;
                this.V = value;
            }

            public BsonValue B { get; set; }
            public BsonArray V { get; set; }
        }

        [Test]
        public void TestNull() {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "null").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestEmpty() {
            var obj = new TestClass(new BsonArray());
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "[]").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestNotEmpty() {
            var obj = new TestClass(new BsonArray { 1, 2 });
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "[1, 2]").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class BsonBinaryGuidSerializerTests {
        public class TestClass {
            public TestClass() { }

            public TestClass(
                BsonBinaryData value
            ) {
                this.B = value;
                this.V = value;
            }

            public BsonValue B { get; set; }
            public BsonBinaryData V { get; set; }
        }

        [Test]
        public void TestNull() {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "null").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestEmpty() {
            var obj = new TestClass(Guid.Empty);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '$binary' : 'AAAAAAAAAAAAAAAAAAAAAA==', '$type' : '03' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestNew() {
            var obj = new TestClass(Guid.NewGuid());
            var json = obj.ToJson();
            var base64 = Convert.ToBase64String(obj.V.Bytes).Replace("\\", "\\\\");
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '$binary' : '" + base64 + "', '$type' : '03' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class BsonBooleanSerializerTests {
        public class TestClass {
            public TestClass() { }

            public TestClass(
                BsonBoolean value
            ) {
                this.B = value;
                this.V = value;
            }

            public BsonValue B { get; set; }
            public BsonBoolean V { get; set; }
        }

        [Test]
        public void TestNull() {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "null").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestFalse() {
            var obj = new TestClass(false);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "false").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestTrue() {
            var obj = new TestClass(true);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "true").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class BsonDateTimeSerializerTests {
        public class TestClass {
            public TestClass() { }

            public TestClass(
                BsonDateTime value
            ) {
                this.B = value;
                this.V = value;
            }

            public BsonValue B { get; set; }
            public BsonDateTime V { get; set; }
        }

        [Test]
        public void TestNull() {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "null").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMinLocal() {
            var obj = new TestClass(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Local));
            long milliseconds = (long) (obj.V.Value.ToUniversalTime() - BsonConstants.UnixEpoch).TotalMilliseconds;
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '$date' : " + milliseconds.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMinUnspecified() {
            var obj = new TestClass(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Unspecified));
            long milliseconds = (long) (obj.V.Value.ToUniversalTime() - BsonConstants.UnixEpoch).TotalMilliseconds;
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '$date' : " + milliseconds.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMinUtc() {
            var obj = new TestClass(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc));
            long milliseconds = (long) (obj.V.Value - BsonConstants.UnixEpoch).TotalMilliseconds;
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '$date' : " + milliseconds.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMaxLocal() {
            var obj = new TestClass(DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Local));
            long milliseconds = (long) (obj.V.Value.ToUniversalTime() - BsonConstants.UnixEpoch).TotalMilliseconds;
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '$date' : " + milliseconds.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMaxUnspecified() {
            var obj = new TestClass(DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Unspecified));
            long milliseconds = (long) (obj.V.Value.ToUniversalTime() - BsonConstants.UnixEpoch).TotalMilliseconds;
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '$date' : " + milliseconds.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMaxUtc() {
            var obj = new TestClass(DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc));
            long milliseconds = (long) (obj.V.Value - BsonConstants.UnixEpoch).TotalMilliseconds;
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '$date' : " + milliseconds.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestLocal() {
            var obj = new TestClass(new DateTime(2010, 10, 08, 13, 30, 0, DateTimeKind.Local));
            long milliseconds = (long) (obj.V.Value.ToUniversalTime() - BsonConstants.UnixEpoch).TotalMilliseconds;
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '$date' : " + milliseconds.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestUnspecified() {
            var obj = new TestClass(new DateTime(2010, 10, 08, 13, 30, 0, DateTimeKind.Unspecified));
            long milliseconds = (long) (obj.V.Value.ToUniversalTime() - BsonConstants.UnixEpoch).TotalMilliseconds;
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '$date' : " + milliseconds.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestUtc() {
            var obj = new TestClass(new DateTime(2010, 10, 08, 13, 30, 0, DateTimeKind.Utc));
            long milliseconds = (long) (obj.V.Value - BsonConstants.UnixEpoch).TotalMilliseconds;
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '$date' : " + milliseconds.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class BsonDocumentSerializerTests {
        public class TestClass {
            public TestClass() { }

            public TestClass(
                BsonDocument value
            ) {
                this.B = value;
                this.V = value;
            }

            public BsonValue B { get; set; }
            public BsonDocument V { get; set; }
        }

        [Test]
        public void TestNull() {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "null").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestEmpty() {
            var obj = new TestClass(new BsonDocument());
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestNotEmpty() {
            var obj = new TestClass(
                new BsonDocument {
                    { "A", 1 },
                    { "B", 2 }
                }
            );
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ 'A' : 1, 'B' : 2 }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class BsonDocumentWrapperSerializerTests {
        public class TestClass {
            public TestClass() { }

            public TestClass(
                BsonDocumentWrapper value
            ) {
                this.B = value;
                this.V = value;
            }

            public BsonValue B { get; set; }
            public BsonDocumentWrapper V { get; set; }
        }

        [Test]
        public void TestNull() {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "null").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            Assert.Throws<InvalidOperationException>(() => BsonSerializer.Deserialize<TestClass>(bson));
        }

        [Test]
        public void TestEmpty() {
            var obj = new TestClass(new BsonDocumentWrapper(new BsonDocument()));
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            Assert.Throws<InvalidOperationException>(() => BsonSerializer.Deserialize<TestClass>(bson));
        }

        [Test]
        public void TestNotEmpty() {
            var obj = new TestClass(
                new BsonDocumentWrapper(
                    new BsonDocument {
                        { "A", 1 },
                        { "B", 2 }
                    }
                )
            );
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ 'A' : 1, 'B' : 2 }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            Assert.Throws<InvalidOperationException>(() => BsonSerializer.Deserialize<TestClass>(bson));
        }
    }

    [TestFixture]
    public class BsonDoubleSerializerTests {
        public class TestClass {
            public TestClass() { }

            public TestClass(
                BsonDouble value
            ) {
                this.B = value;
                this.V = value;
            }

            public BsonValue B { get; set; }
            public BsonDouble V { get; set; }
        }

        [Test]
        public void TestNull() {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "null").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass(double.MinValue);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", XmlConvert.ToString(double.MinValue)).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMinusOne() {
            var obj = new TestClass(-1.0);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "-1").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass(0.0);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "0").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass(1.0);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "1").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass(double.MaxValue);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", XmlConvert.ToString(double.MaxValue)).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestNaN() {
            var obj = new TestClass(double.NaN);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "NaN").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestNegativeInfinity() {
            var obj = new TestClass(double.NegativeInfinity);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "-INF").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestPositiveInfinity() {
            var obj = new TestClass(double.PositiveInfinity);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "INF").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class BsonInt32SerializerTests {
        public class TestClass {
            public TestClass() { }

            public TestClass(
                BsonInt32 value
            ) {
                this.B = value;
                this.V = value;
            }

            public BsonValue B { get; set; }
            public BsonInt32 V { get; set; }
        }

        [Test]
        public void TestNull() {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "null").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass(int.MinValue);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", int.MinValue.ToString()).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMinusOne() {
            var obj = new TestClass(-1);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "-1").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass(0);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "0").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass(1);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "1").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass(int.MaxValue);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", int.MaxValue.ToString()).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class BsonInt64SerializerTests {
        public class TestClass {
            public TestClass() { }

            public TestClass(
                BsonInt64 value
            ) {
                this.B = value;
                this.V = value;
            }

            public BsonValue B { get; set; }
            public BsonInt64 V { get; set; }
        }

        [Test]
        public void TestNull() {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "null").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass(long.MinValue);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", long.MinValue.ToString()).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMinusOne() {
            var obj = new TestClass(-1);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "-1").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass(0);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "0").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass(1);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "1").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass(long.MaxValue);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", long.MaxValue.ToString()).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class BsonJavaScriptSerializerTests {
        public class TestClass {
            public TestClass() { }

            public TestClass(
                BsonJavaScript value
            ) {
                this.B = value;
                this.V = value;
            }

            public BsonValue B { get; set; }
            public BsonJavaScript V { get; set; }
        }

        [Test]
        public void TestNull() {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "null").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestNotNull() {
            var obj = new TestClass("this.age === 21");
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '$code' : 'this.age === 21' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class BsonJavaScriptWithScopeSerializerTests {
        public class TestClass {
            public TestClass() { }

            public TestClass(
                BsonJavaScriptWithScope value
            ) {
                this.B = value;
                this.V = value;
            }

            public BsonValue B { get; set; }
            public BsonJavaScriptWithScope V { get; set; }
        }

        [Test]
        public void TestNull() {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "null").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestNotNull() {
            var scope = new BsonDocument("x", 21);
            var obj = new TestClass(new BsonJavaScriptWithScope("this.age === 21", scope));
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '$code' : 'this.age === 21', '$scope' : { 'x' : 21 } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class BsonMaxKeySerializerTests {
        public class TestClass {
            public TestClass() { }

            public TestClass(
                BsonMaxKey value
            ) {
                this.B = value;
                this.V = value;
            }

            public BsonValue B { get; set; }
            public BsonMaxKey V { get; set; }
        }

        [Test]
        public void TestNull() {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "null").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestValue() {
            var obj = new TestClass(BsonMaxKey.Value);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '$maxkey' : 1 }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.AreSame(obj.V, rehydrated.V);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class BsonMinKeySerializerTests {
        public class TestClass {
            public TestClass() { }

            public TestClass(
                BsonMinKey value
            ) {
                this.B = value;
                this.V = value;
            }

            public BsonValue B { get; set; }
            public BsonMinKey V { get; set; }
        }

        [Test]
        public void TestNull() {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "null").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestValue() {
            var obj = new TestClass(BsonMinKey.Value);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '$minkey' : 1 }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.AreSame(obj.V, rehydrated.V);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class BsonNullSerializerTests {
        public class TestClass {
            public TestClass() { }

            public TestClass(
                BsonNull value
            ) {
                this.B = value;
                this.V = value;
            }

            public BsonValue B { get; set; }
            public BsonNull V { get; set; }
        }

        [Test]
        public void TestNull() {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : null, 'V' : # }".Replace("#", "{ '$csharpnull' : true }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestValue() {
            var obj = new TestClass(BsonNull.Value);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "null").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.AreSame(obj.V, rehydrated.V);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class BsonObjectIdSerializerTests {
        public class TestClass {
            public TestClass() { }

            public TestClass(
                BsonObjectId value
            ) {
                this.B = value;
                this.V = value;
            }

            public BsonValue B { get; set; }
            public BsonObjectId V { get; set; }
        }

        [Test]
        public void TestNull() {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "null").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestNotNull() {
            var obj = new TestClass(new BsonObjectId(1, 2, 3, 4));
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '$oid' : '000000010000020003000004' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class BsonRegularExpressionSerializerTests {
        public class TestClass {
            public TestClass() { }

            public TestClass(
                BsonRegularExpression value
            ) {
                this.B = value;
                this.V = value;
            }

            public BsonValue B { get; set; }
            public BsonRegularExpression V { get; set; }
        }

        [Test]
        public void TestNull() {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "null").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestWithoutOptions() {
            var obj = new TestClass(new BsonRegularExpression("abc"));
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '$regex' : 'abc', '$options' : '' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestWithOptions() {
            var obj = new TestClass(new BsonRegularExpression("abc", "gim"));
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '$regex' : 'abc', '$options' : 'gim' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class BsonStringSerializerTests {
        public class TestClass {
            public TestClass() { }

            public TestClass(
                BsonString value
            ) {
                this.B = value;
                this.V = value;
            }

            public BsonValue B { get; set; }
            public BsonString V { get; set; }
        }

        [Test]
        public void TestNull() {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "null").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestEmpty() {
            var obj = new TestClass("");
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "''").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestHelloWorld() {
            var obj = new TestClass("Hello World");
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "'Hello World'").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class BsonSymbolSerializerTests {
        public class TestClass {
            public TestClass() { }

            public TestClass(
                BsonSymbol value
            ) {
                this.B = value;
                this.V = value;
                this.S = value;
            }

            public BsonValue B { get; set; }
            [BsonRepresentation(BsonType.Symbol)]
            public BsonSymbol V { get; set; }
            [BsonRepresentation(BsonType.String)]
            public BsonSymbol S { get; set; }
        }

        [Test]
        public void TestNull() {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : #, 'S' : # }".Replace("#", "null").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.AreSame(obj.V, rehydrated.V);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestEmpty() {
            var obj = new TestClass("");
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : #, 'S' : '' }".Replace("#", "{ '$symbol' : '' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.AreSame(obj.V, rehydrated.V);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestHelloWorld() {
            var obj = new TestClass("Hello World");
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : #, 'S' : 'Hello World' }".Replace("#", "{ '$symbol' : 'Hello World' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.AreSame(obj.V, rehydrated.V);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class BsonTimestampSerializerTests {
        public class TestClass {
            public TestClass() { }

            public TestClass(
                BsonTimestamp value
            ) {
                this.B = value;
                this.V = value;
            }

            public BsonValue B { get; set; }
            public BsonTimestamp V { get; set; }
        }

        [Test]
        public void TestNull() {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "null").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass(new BsonTimestamp(long.MinValue));
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '$timestamp' : " + long.MinValue.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMinusOne() {
            var obj = new TestClass(new BsonTimestamp(-1));
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '$timestamp' : -1 }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass(new BsonTimestamp(0));
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '$timestamp' : 0 }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass(new BsonTimestamp(1));
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '$timestamp' : 1 }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOneTwo() {
            var obj = new TestClass(new BsonTimestamp(1, 2));
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '$timestamp' : " + ((1L << 32) + 2).ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass(new BsonTimestamp(long.MaxValue));
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '$timestamp' : " + long.MaxValue.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
