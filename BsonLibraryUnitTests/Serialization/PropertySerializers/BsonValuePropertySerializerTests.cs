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
    public class BsonBinaryGuidPropertySerializerTests {
        public class TestClass {
            public BsonBinaryData BsonBinary { get; set; }
        }

        [Test]
        public void TestEmpty() {
            var obj = new TestClass {
                BsonBinary = Guid.Empty
            };
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'BsonBinary' : { '$binary' : 'AAAAAAAAAAAAAAAAAAAAAA==', '$type' : '03' } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestNew() {
            var obj = new TestClass {
                BsonBinary = Guid.NewGuid()
            };
            var json = BsonUtils.ToJson(obj);
            var base64 = Convert.ToBase64String(obj.BsonBinary.Bytes).Replace("\\", "\\\\");
            var expected = ("{ 'BsonBinary' : { '$binary' : '" + base64 + "', '$type' : '03' } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }
    }

    [TestFixture]
    public class BsonBooleanPropertySerializerTests {
        public class TestClass {
            public BsonBoolean BsonBoolean { get; set; }
        }

        [Test]
        public void TestFalse() {
            var obj = new TestClass {
                BsonBoolean = false
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'BsonBoolean' : false }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestTrue() {
            var obj = new TestClass {
                BsonBoolean = true
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'BsonBoolean' : true }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }
    }
    [TestFixture]
    public class BsonDateTimePropertySerializerTests {
        public class TestClass {
            public BsonDateTime BsonDateTime { get; set; }
        }

        [Test]
        public void TestMinLocal() {
            var obj = new TestClass {
                BsonDateTime = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Local)
            };
            long milliseconds = (long) (obj.BsonDateTime.Value.ToUniversalTime() - Bson.UnixEpoch).TotalMilliseconds;
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'BsonDateTime' : { '$date' : " + milliseconds.ToString() + " } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestMinUnspecified() {
            var obj = new TestClass {
                BsonDateTime = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Unspecified)
            };
            long milliseconds = (long) (obj.BsonDateTime.Value.ToUniversalTime() - Bson.UnixEpoch).TotalMilliseconds;
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'BsonDateTime' : { '$date' : " + milliseconds.ToString() + " } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestMinUtc() {
            var obj = new TestClass {
                BsonDateTime = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc)
            };
            long milliseconds = (long) (obj.BsonDateTime.Value - Bson.UnixEpoch).TotalMilliseconds;
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'BsonDateTime' : { '$date' : " + milliseconds.ToString() + " } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestMaxLocal() {
            var obj = new TestClass {
                BsonDateTime = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Local)
            };
            long milliseconds = (long) (obj.BsonDateTime.Value.ToUniversalTime() - Bson.UnixEpoch).TotalMilliseconds;
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'BsonDateTime' : { '$date' : " + milliseconds.ToString() + " } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestMaxUnspecified() {
            var obj = new TestClass {
                BsonDateTime = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Unspecified)
            };
            long milliseconds = (long) (obj.BsonDateTime.Value.ToUniversalTime() - Bson.UnixEpoch).TotalMilliseconds;
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'BsonDateTime' : { '$date' : " + milliseconds.ToString() + " } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestMaxUtc() {
            var obj = new TestClass {
                BsonDateTime = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc)
            };
            long milliseconds = (long) (obj.BsonDateTime.Value - Bson.UnixEpoch).TotalMilliseconds;
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'BsonDateTime' : { '$date' : " + milliseconds.ToString() + " } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestLocal() {
            var obj = new TestClass {
                BsonDateTime = new DateTime(2010, 10, 08, 13, 30, 0, DateTimeKind.Local)
            };
            long milliseconds = (long) (obj.BsonDateTime.Value.ToUniversalTime() - Bson.UnixEpoch).TotalMilliseconds;
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'BsonDateTime' : { '$date' : " + milliseconds.ToString() + " } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestUnspecified() {
            var obj = new TestClass {
                BsonDateTime = new DateTime(2010, 10, 08, 13, 30, 0, DateTimeKind.Unspecified)
            };
            long milliseconds = (long) (obj.BsonDateTime.Value.ToUniversalTime() - Bson.UnixEpoch).TotalMilliseconds;
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'BsonDateTime' : { '$date' : " + milliseconds.ToString() + " } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestUtc() {
            var obj = new TestClass {
                BsonDateTime = new DateTime(2010, 10, 08, 13, 30, 0, DateTimeKind.Utc)
            };
            long milliseconds = (long) (obj.BsonDateTime.Value - Bson.UnixEpoch).TotalMilliseconds;
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'BsonDateTime' : { '$date' : " + milliseconds.ToString() + " } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }
    }

    [TestFixture]
    public class BsonDoublePropertySerializerTests {
        public class TestClass {
            public BsonDouble BsonDouble { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                BsonDouble = double.MinValue
            };
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'BsonDouble' : " + double.MinValue.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestMinusOne() {
            var obj = new TestClass {
                BsonDouble = -1.0
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'BsonDouble' : -1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                BsonDouble = 0.0
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'BsonDouble' : 0 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                BsonDouble = 1.0
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'BsonDouble' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                BsonDouble = double.MaxValue
            };
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'BsonDouble' : " + double.MaxValue.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestNaN() {
            var obj = new TestClass {
                BsonDouble = double.NaN
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'BsonDouble' : NaN }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestNegativeInfinity() {
            var obj = new TestClass {
                BsonDouble = double.NegativeInfinity
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'BsonDouble' : -Infinity }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestPositiveInfinity() {
            var obj = new TestClass {
                BsonDouble = double.PositiveInfinity
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'BsonDouble' : Infinity }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }
    }

    [TestFixture]
    public class BsonInt32PropertySerializerTests {
        public class TestClass {
            public BsonInt32 BsonInt32 { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                BsonInt32 = int.MinValue
            };
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'BsonInt32' : " + int.MinValue.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestMinusOne() {
            var obj = new TestClass {
                BsonInt32 = -1
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'BsonInt32' : -1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                BsonInt32 = 0
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'BsonInt32' : 0 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                BsonInt32 = 1
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'BsonInt32' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                BsonInt32 = int.MaxValue
            };
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'BsonInt32' : " + int.MaxValue.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }
    }

    [TestFixture]
    public class BsonInt64PropertySerializerTests {
        public class TestClass {
            public BsonInt64 BsonInt64 { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                BsonInt64 = long.MinValue
            };
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'BsonInt64' : " + long.MinValue.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestMinusOne() {
            var obj = new TestClass {
                BsonInt64 = -1
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'BsonInt64' : -1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                BsonInt64 = 0
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'BsonInt64' : 0 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                BsonInt64 = 1
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'BsonInt64' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                BsonInt64 = long.MaxValue
            };
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'BsonInt64' : " + long.MaxValue.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }
    }

    [TestFixture]
    public class BsonStringPropertySerializerTests {
        public class TestClass {
            public BsonString BsonString { get; set; }
        }

        [Test]
        public void TestNull() {
            var obj = new TestClass {
                BsonString = null
            };
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'BsonString' : null }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestEmpty() {
            var obj = new TestClass {
                BsonString = BsonString.Empty
            };
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'BsonString' : '' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestHelloWorld() {
            var obj = new TestClass {
                BsonString = "Hello World"
            };
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'BsonString' : 'Hello World' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }
    }

    [TestFixture]
    public class BsonSymbolPropertySerializerTests {
        public class TestClass {
            public BsonSymbol BsonSymbol { get; set; }
        }

        [Test]
        public void TestNull() {
            var obj = new TestClass {
                BsonSymbol = null
            };
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'BsonSymbol' : null }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = (TestClass) BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.AreSame(obj.BsonSymbol, rehydrated.BsonSymbol);
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestEmpty() {
            var obj = new TestClass {
                BsonSymbol = String.Empty
            };
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'BsonSymbol' : { '$symbol' : '' } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = (TestClass) BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.AreSame(obj.BsonSymbol, rehydrated.BsonSymbol);
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestHelloWorld() {
            var obj = new TestClass {
                BsonSymbol = "Hello World"
            };
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'BsonSymbol' : { '$symbol' : 'Hello World' } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = (TestClass) BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.AreSame(obj.BsonSymbol, rehydrated.BsonSymbol);
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }
    }
}
