/* Copyright 2010-present MongoDB Inc.
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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.GenericEnumerable
{
    public class Address
    {
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public int Zip { get; set; }
    }

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

        [Fact]
        public void TestNull()
        {
            var obj = new TestClass { Addresses = null };
            var json = obj.ToJson();
            var expected = "{ 'Addresses' : null }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
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
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsType<HashSet<Address>>(rehydrated.Addresses);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    public class IEnumerableSerializerTests
    {
        public class TestClass
        {
            public IEnumerable<Address> Addresses { get; set; }
        }

        [Fact]
        public void TestNull()
        {
            var obj = new TestClass { Addresses = null };
            var json = obj.ToJson();
            var expected = "{ 'Addresses' : null }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
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
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsType<List<Address>>(rehydrated.Addresses);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    public class IListSerializerTests
    {
        public class TestClass
        {
            public IList<Address> Addresses { get; set; }
        }

        [Fact]
        public void TestNull()
        {
            var obj = new TestClass { Addresses = null };
            var json = obj.ToJson();
            var expected = "{ 'Addresses' : null }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestEmpty()
        {
            var obj = new TestClass
            {
                Addresses = new List<Address>()
            };
            var json = obj.ToJson();
            var expected = "{ 'Addresses' : [] }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsType<List<Address>>(rehydrated.Addresses);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
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
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsType<List<Address>>(rehydrated.Addresses);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
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
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsType<List<Address>>(rehydrated.Addresses);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    public class ICustomCollectionSerializerTests
    {
        public interface IAddressCollection : ICollection<Address>
        {
        }

        public class AddressCollection : IAddressCollection
        {
            private readonly List<Address> data = new();
            public int Count => data.Count;
            public bool IsReadOnly => false;
            public void Add(Address item) => data.Add(item);
            public void Clear() => data.Clear();
            public bool Contains(Address item) => data.Contains(item);
            public void CopyTo(Address[] array, int arrayIndex) => data.CopyTo(array, arrayIndex);
            public IEnumerator<Address> GetEnumerator() => data.GetEnumerator();
            public bool Remove(Address item) => data.Remove(item);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public class TestClass
        {
            public IAddressCollection Addresses { get; set; }
        }

        [Fact]
        public void TestNull()
        {
            var obj = new TestClass { Addresses = null };
            var json = obj.ToJson();
            var expected = "{ 'Addresses' : null }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestEmpty()
        {
            var obj = new TestClass
            {
                Addresses = new AddressCollection()
            };
            var json = obj.ToJson();
            var expected = "{ 'Addresses' : { '_t' : 'AddressCollection', '_v' : [] } }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsType<AddressCollection>(rehydrated.Addresses);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestOneAddress()
        {
            var obj = new TestClass
            {
                Addresses = new AddressCollection
                {
                    new Address { Street = "123 Main", City = "Smithtown", State = "PA", Zip = 12345 }
                }
            };
            var json = obj.ToJson();
            var expected = "{ 'Addresses' : { '_t' : 'AddressCollection', '_v' : [#A1] } }";
            expected = expected.Replace("#A1", "{ 'Street' : '123 Main', 'City' : 'Smithtown', 'State' : 'PA', 'Zip' : 12345 }");
            expected = expected.Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsType<AddressCollection>(rehydrated.Addresses);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestTwoAddresses()
        {
            var obj = new TestClass
            {
                Addresses = new AddressCollection
                {
                    new Address { Street = "123 Main", City = "Smithtown", State = "PA", Zip = 12345 },
                    new Address { Street = "456 First", City = "Johnstown", State = "MD", Zip = 45678 }
                }
            };
            var json = obj.ToJson();
            var expected = "{ 'Addresses' : { '_t' : 'AddressCollection', '_v' : [#A1, #A2] } }";
            expected = expected.Replace("#A1", "{ 'Street' : '123 Main', 'City' : 'Smithtown', 'State' : 'PA', 'Zip' : 12345 }");
            expected = expected.Replace("#A2", "{ 'Street' : '456 First', 'City' : 'Johnstown', 'State' : 'MD', 'Zip' : 45678 }");
            expected = expected.Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsType<AddressCollection>(rehydrated.Addresses);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
