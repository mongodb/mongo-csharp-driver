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
    public class BooleanPropertySerializerTests {
        public class TestClass {
            static TestClass() {
                BsonClassMap.RegisterClassMap<TestClass>();
            }

            public bool Boolean { get; set; }
        }

        [Test]
        public void TestFalse() {
            var obj = new TestClass {
                Boolean = false
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'Boolean' : false }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestTrue() {
            var obj = new TestClass {
                Boolean = true
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'Boolean' : true }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }
    }

    [TestFixture]
    public class BytePropertySerializerTests {
        public class TestClass {
            static TestClass() {
                BsonClassMap.RegisterClassMap<TestClass>();
            }

            public byte Byte { get; set; }
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                Byte = 0
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'Byte' : 0 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                Byte = 1
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'Byte' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                Byte = byte.MaxValue
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'Byte' : 255 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }
    }

    [TestFixture]
    public class CharPropertySerializerTests {
        public class TestClass {
            static TestClass() {
                BsonClassMap.RegisterClassMap<TestClass>();
            }

            public char Char { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                Char = char.MinValue
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'Char' : 0 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        public void TestZero() {
            var obj = new TestClass {
                Char = (char) 0
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'Char' : 0 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                Char = (char) 1
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'Char' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                Char = char.MaxValue
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'Char' : 65535 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }
    }

    [TestFixture]
    public class DateTimePropertySerializerTests {
        public class TestClass {
            static TestClass() {
                BsonClassMap.RegisterClassMap<TestClass>();
            }

            public DateTime DateTime { get; set; }
        }

        [Test]
        public void TestMinLocal() {
            var obj = new TestClass {
                DateTime = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Local)
            };
            long milliseconds = (long) (obj.DateTime.ToUniversalTime() - Bson.UnixEpoch).TotalMilliseconds;
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'DateTime' : { '$date' : " + milliseconds.ToString() + " } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestMinUnspecified() {
            var obj = new TestClass {
                DateTime = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Unspecified)
            };
            long milliseconds = (long) (obj.DateTime.ToUniversalTime() - Bson.UnixEpoch).TotalMilliseconds;
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'DateTime' : { '$date' : " + milliseconds.ToString() + " } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestMinUtc() {
            var obj = new TestClass {
                DateTime = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc)
            };
            long milliseconds = (long) (obj.DateTime - Bson.UnixEpoch).TotalMilliseconds;
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'DateTime' : { '$date' : " + milliseconds.ToString() + " } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestMaxLocal() {
            var obj = new TestClass {
                DateTime = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Local)
            };
            long milliseconds = (long) (obj.DateTime.ToUniversalTime() - Bson.UnixEpoch).TotalMilliseconds;
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'DateTime' : { '$date' : " + milliseconds.ToString() + " } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestMaxUnspecified() {
            var obj = new TestClass {
                DateTime = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Unspecified)
            };
            long milliseconds = (long) (obj.DateTime.ToUniversalTime() - Bson.UnixEpoch).TotalMilliseconds;
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'DateTime' : { '$date' : " + milliseconds.ToString() + " } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestMaxUtc() {
            var obj = new TestClass {
                DateTime = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc)
            };
            long milliseconds = (long) (obj.DateTime - Bson.UnixEpoch).TotalMilliseconds;
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'DateTime' : { '$date' : " + milliseconds.ToString() + " } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestLocal() {
            var obj = new TestClass {
                DateTime = new DateTime(2010, 10, 08, 13, 30, 0, DateTimeKind.Local)
            };
            long milliseconds = (long) (obj.DateTime.ToUniversalTime() - Bson.UnixEpoch).TotalMilliseconds;
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'DateTime' : { '$date' : " + milliseconds.ToString() + " } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestUnspecified() {
            var obj = new TestClass {
                DateTime = new DateTime(2010, 10, 08, 13, 30, 0, DateTimeKind.Unspecified)
            };
            long milliseconds = (long) (obj.DateTime.ToUniversalTime() - Bson.UnixEpoch).TotalMilliseconds;
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'DateTime' : { '$date' : " + milliseconds.ToString() + " } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestUtc() {
            var obj = new TestClass {
                DateTime = new DateTime(2010, 10, 08, 13, 30, 0, DateTimeKind.Utc)
            };
            long milliseconds = (long) (obj.DateTime - Bson.UnixEpoch).TotalMilliseconds;
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'DateTime' : { '$date' : " + milliseconds.ToString() + " } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }
    }

    [TestFixture]
    public class DoublePropertySerializerTests {
        public class TestClass {
            static TestClass() {
                BsonClassMap.RegisterClassMap<TestClass>();
            }

            public double Double { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                Double = double.MinValue
            };
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'Double' : " + double.MinValue.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestMinusOne() {
            var obj = new TestClass {
                Double = -1.0
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'Double' : -1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                Double = 0.0
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'Double' : 0 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                Double = 1.0
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'Double' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                Double = double.MaxValue
            };
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'Double' : " + double.MaxValue.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestNaN() {
            var obj = new TestClass {
                Double = double.NaN
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'Double' : NaN }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestNegativeInfinity() {
            var obj = new TestClass {
                Double = double.NegativeInfinity
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'Double' : -Infinity }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestPositiveInfinity() {
            var obj = new TestClass {
                Double = double.PositiveInfinity
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'Double' : Infinity }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }
    }

    [TestFixture]
    public class GuidPropertySerializerTests {
        public class TestClass {
            static TestClass() {
                BsonClassMap.RegisterClassMap<TestClass>();
            }

            public Guid Guid { get; set; }
        }

        [Test]
        public void TestEmpty() {
            var obj = new TestClass {
                Guid = Guid.Empty
            };
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'Guid' : { '$binary' : 'AAAAAAAAAAAAAAAAAAAAAA==', '$type' : '03' } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestNew() {
            var obj = new TestClass {
                Guid = Guid.NewGuid()
            };
            var json = BsonUtils.ToJson(obj);
            var base64 = Convert.ToBase64String(obj.Guid.ToByteArray()).Replace("\\", "\\\\");
            var expected = ("{ 'Guid' : { '$binary' : '" + base64 + "', '$type' : '03' } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }
    }

    [TestFixture]
    public class Int16PropertySerializerTests {
        public class TestClass {
            static TestClass() {
                BsonClassMap.RegisterClassMap<TestClass>();
            }

            public short Int16 { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                Int16 = short.MinValue
            };
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'Int16' : " + short.MinValue.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestMinusOne() {
            var obj = new TestClass {
                Int16 = -1
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'Int16' : -1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                Int16 = 0
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'Int16' : 0 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                Int16 = 1
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'Int16' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                Int16 = short.MaxValue
            };
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'Int16' : " + short.MaxValue.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }
    }

    [TestFixture]
    public class Int32PropertySerializerTests {
        public class TestClass {
            static TestClass() {
                BsonClassMap.RegisterClassMap<TestClass>();
            }

            public int Int32 { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                Int32 = int.MinValue
            };
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'Int32' : " + int.MinValue.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestMinusOne() {
            var obj = new TestClass {
                Int32 = -1
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'Int32' : -1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                Int32 = 0
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'Int32' : 0 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                Int32 = 1
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'Int32' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                Int32 = int.MaxValue
            };
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'Int32' : " + int.MaxValue.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }
    }

    [TestFixture]
    public class Int64PropertySerializerTests {
        public class TestClass {
            static TestClass() {
                BsonClassMap.RegisterClassMap<TestClass>();
            }

            public long Int64 { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                Int64 = long.MinValue
            };
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'Int64' : " + long.MinValue.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestMinusOne() {
            var obj = new TestClass {
                Int64 = -1
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'Int64' : -1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                Int64 = 0
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'Int64' : 0 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                Int64 = 1
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'Int64' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                Int64 = long.MaxValue
            };
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'Int64' : " + long.MaxValue.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }
    }

    [TestFixture]
    public class SBytePropertySerializerTests {
        public class TestClass {
            static TestClass() {
                BsonClassMap.RegisterClassMap<TestClass>();
            }

            public sbyte SByte { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                SByte = sbyte.MinValue
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'SByte' : -128 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestMinusOne() {
            var obj = new TestClass {
                SByte = -1
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'SByte' : -1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                SByte = 0
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'SByte' : 0 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                SByte = 1
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'SByte' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                SByte = sbyte.MaxValue
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'SByte' : 127 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }
    }

    [TestFixture]
    public class SinglePropertySerializerTests {
        public class TestClass {
            static TestClass() {
                BsonClassMap.RegisterClassMap<TestClass>();
            }

            public float Single { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                Single = float.MinValue
            };
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'Single' : " + ((double) float.MinValue).ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = (TestClass) BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.AreEqual(rehydrated.Single, float.MinValue);
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestMinusOne() {
            var obj = new TestClass {
                Single = -1.0F
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'Single' : -1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                Single = 0.0F
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'Single' : 0 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                Single = 1.0F
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'Single' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                Single = float.MaxValue
            };
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'Single' : " + ((double) float.MaxValue).ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = (TestClass) BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.AreEqual(rehydrated.Single, float.MaxValue);
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestNaN() {
            var obj = new TestClass {
                Single = float.NaN
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'Single' : NaN }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestNegativeInfinity() {
            var obj = new TestClass {
                Single = float.NegativeInfinity
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'Single' : -Infinity }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestPositiveInfinity() {
            var obj = new TestClass {
                Single = float.PositiveInfinity
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'Single' : Infinity }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }
    }

    [TestFixture]
    public class StringPropertySerializerTests {
        public class TestClass {
            static TestClass() {
                BsonClassMap.RegisterClassMap<TestClass>();
            }

            public String String { get; set; }
        }

        [Test]
        public void TestNull() {
            var obj = new TestClass {
                String = null
            };
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'String' : null }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestEmpty() {
            var obj = new TestClass {
                String = String.Empty
            };
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'String' : '' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestHelloWorld() {
            var obj = new TestClass {
                String = "Hello World"
            };
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'String' : 'Hello World' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }
    }

    [TestFixture]
    public class UInt16PropertySerializerTests {
        public class TestClass {
            static TestClass() {
                BsonClassMap.RegisterClassMap<TestClass>();
            }

            public ushort UInt16 { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                UInt16 = ushort.MinValue
            };
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'UInt16' : " + ushort.MinValue.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                UInt16 = 0
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'UInt16' : 0 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                UInt16 = 1
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'UInt16' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                UInt16 = ushort.MaxValue
            };
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'UInt16' : " + ushort.MaxValue.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }
    }

    [TestFixture]
    public class UInt32PropertySerializerTests {
        public class TestClass {
            static TestClass() {
                BsonClassMap.RegisterClassMap<TestClass>();
            }

            public uint UInt32 { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                UInt32 = uint.MinValue
            };
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'UInt32' : " + uint.MinValue.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                UInt32 = 0
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'UInt32' : 0 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                UInt32 = 1
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'UInt32' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                UInt32 = uint.MaxValue
            };
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'UInt32' : -1 }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }
    }

    [TestFixture]
    public class UInt64PropertySerializerTests {
        public class TestClass {
            static TestClass() {
                BsonClassMap.RegisterClassMap<TestClass>();
            }

            public ulong UInt64 { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                UInt64 = ulong.MinValue
            };
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'UInt64' : " + ulong.MinValue.ToString() + " }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestZero() {
            var obj = new TestClass {
                UInt64 = 0
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'UInt64' : 0 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestOne() {
            var obj = new TestClass {
                UInt64 = 1
            };
            var json = BsonUtils.ToJson(obj);
            var expected = "{ 'UInt64' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }

        [Test]
        public void TestMax() {
            var obj = new TestClass {
                UInt64 = ulong.MaxValue
            };
            var json = BsonUtils.ToJson(obj);
            var expected = ("{ 'UInt64' : -1 }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = BsonUtils.ToBson(obj);
            var rehydrated = BsonSerializer.Deserialize(bson, typeof(TestClass));
            Assert.IsTrue(bson.SequenceEqual(BsonUtils.ToBson(rehydrated)));
        }
    }
}
