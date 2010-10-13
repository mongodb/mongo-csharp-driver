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
using System.Linq;
using System.Text;
using NUnit.Framework;

using MongoDB.BsonLibrary.Serialization;

namespace MongoDB.BsonLibrary.UnitTests.Serialization.PropertySerializers {
    [TestFixture]
    public class GenericEnumerablePropertySerializerTests {
        public class Address {
            public string Street { get; set; }
            public string City { get; set; }
            public string State { get; set; }
            public int Zip { get; set; }
        }

        public class TestClass {
            public IList<Address> Addresses { get; set; }
        }

        [Test]
        public void TestMin() {
            var obj = new TestClass {
                Addresses = new List<Address>() {
                    new Address { Street = "123 Main", City = "Smithtown", State = "PA", Zip = 12345 },
                    new Address { Street = "456 First", City = "Johnstown", State = "MD", Zip = 45678 }
                }
            };
            var json = obj.ToJson();
            //var expected = "{ 'C' : 0, 'F' : { '_t' : 'System.Byte', 'v' : 0 } }".Replace("'", "\"");
            //Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
