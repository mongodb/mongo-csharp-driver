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

using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Serialization.GenericEnumerable
{
    public class Address
    {
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public int Zip { get; set; }
    }

    [TestFixture]
    public class HashSetSerializerTests
    {
        public class TestClass
        {
            static TestClass()
            {
                BsonClassMap.RegisterClassMap<TestClass>(cm => { cm.AutoMap(); });
            }

            public HashSet<Address> Addresses { get; set; }
        }

        [Test]
        public void TestNull()
        {
            var obj = new TestClass { Addresses = null };
            var json = obj.ToJson();
            var expected = "{ 'Addresses' : null }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerialization()
        {
            var obj = new TestClass
            {
                Addresses = new HashSet<Address>
                {
                    new Address { Street = "123 Main", City = "Smithtown", State = "PA", Zip = 12345 },
                    new Address { Street = "456 First", City = "Johnstown", State = "MD", Zip = 45678 }
                }
            };
            var json = obj.ToJson();
            var expected = "{ 'Addresses' : [#A1, #A2] }";
            expected = expected.Replace("#A1", "{ 'Street' : '123 Main', 'City' : 'Smithtown', 'State' : 'PA', 'Zip' : 12345 }");
            expected = expected.Replace("#A2", "{ 'Street' : '456 First', 'City' : 'Johnstown', 'State' : 'MD', 'Zip' : 45678 }");
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsInstanceOf<HashSet<Address>>(rehydrated.Addresses);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class IEnumerableSerializerTests
    {
        public class TestClass
        {
            public IEnumerable<Address> Addresses { get; set; }
        }

        [Test]
        public void TestNull()
        {
            var obj = new TestClass { Addresses = null };
            var json = obj.ToJson();
            var expected = "{ 'Addresses' : null }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerialization()
        {
            var obj = new TestClass
            {
                Addresses = new List<Address>
                {
                    new Address { Street = "123 Main", City = "Smithtown", State = "PA", Zip = 12345 },
                    new Address { Street = "456 First", City = "Johnstown", State = "MD", Zip = 45678 }
                }
            };
            var json = obj.ToJson();
            var expected = "{ 'Addresses' : [#A1, #A2] }";
            expected = expected.Replace("#A1", "{ 'Street' : '123 Main', 'City' : 'Smithtown', 'State' : 'PA', 'Zip' : 12345 }");
            expected = expected.Replace("#A2", "{ 'Street' : '456 First', 'City' : 'Johnstown', 'State' : 'MD', 'Zip' : 45678 }");
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsInstanceOf<List<Address>>(rehydrated.Addresses);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class IListSerializerTests
    {
        public class TestClass
        {
            public IList<Address> Addresses { get; set; }
        }

        [Test]
        public void TestNull()
        {
            var obj = new TestClass { Addresses = null };
            var json = obj.ToJson();
            var expected = "{ 'Addresses' : null }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestEmpty()
        {
            var obj = new TestClass
            {
                Addresses = new List<Address>()
            };
            var json = obj.ToJson();
            var expected = "{ 'Addresses' : [] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsInstanceOf<List<Address>>(rehydrated.Addresses);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOneAddress()
        {
            var obj = new TestClass
            {
                Addresses = new List<Address>
                {
                    new Address { Street = "123 Main", City = "Smithtown", State = "PA", Zip = 12345 }
                }
            };
            var json = obj.ToJson();
            var expected = "{ 'Addresses' : [#A1] }";
            expected = expected.Replace("#A1", "{ 'Street' : '123 Main', 'City' : 'Smithtown', 'State' : 'PA', 'Zip' : 12345 }");
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsInstanceOf<List<Address>>(rehydrated.Addresses);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestTwoAddresses()
        {
            var obj = new TestClass
            {
                Addresses = new List<Address>
                {
                    new Address { Street = "123 Main", City = "Smithtown", State = "PA", Zip = 12345 },
                    new Address { Street = "456 First", City = "Johnstown", State = "MD", Zip = 45678 }
                }
            };
            var json = obj.ToJson();
            var expected = "{ 'Addresses' : [#A1, #A2] }";
            expected = expected.Replace("#A1", "{ 'Street' : '123 Main', 'City' : 'Smithtown', 'State' : 'PA', 'Zip' : 12345 }");
            expected = expected.Replace("#A2", "{ 'Street' : '456 First', 'City' : 'Johnstown', 'State' : 'MD', 'Zip' : 45678 }");
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsInstanceOf<List<Address>>(rehydrated.Addresses);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
