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

namespace MongoDB.BsonLibrary.UnitTests.Serialization {
    [TestFixture]
    public class BsonClassMapSerializerTests {
        [Test]
        public void TestAnonymousClass() {
            var obj = new {
                I = 1,
                D = 1.1,
                S = "Hello"
            };
            var json = obj.ToJson();
            var expected = "{ 'I' : 1, 'D' : 1.1, 'S' : 'Hello' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            Assert.Throws<InvalidOperationException>(() => BsonSerializer.Deserialize(bson, obj.GetType()));
        }

        public class Employee {
            private class DateOfBirthSerializer : IBsonPropertySerializer {
                public Type PropertyType {
                    get { return typeof(DateTime); }
                }

                public void DeserializeProperty(
                    BsonReader bsonReader,
                    object obj,
                    BsonPropertyMap propertyMap
                ) {
                    var employee = (Employee) obj;
                    employee.DateOfBirth = DateTime.Parse(bsonReader.ReadString(propertyMap.ElementName));
                }

                public void SerializeProperty(
                    BsonWriter bsonWriter,
                    object obj,
                    BsonPropertyMap propertyMap
                ) {
                    var employee = (Employee) obj;
                    bsonWriter.WriteString(propertyMap.ElementName, employee.DateOfBirth.ToString("yyyy-MM-dd"));
                }
            }

            static Employee() {
                BsonClassMap.RegisterClassMap<Employee>(
                    cm => {
                        cm.MapId(e => e.EmployeeId);
                        cm.MapProperty(e => e.FirstName, "fn");
                        cm.MapProperty(e => e.LastName, "ln");
                        cm.MapProperty(e => e.DateOfBirth, "dob")
                            .SetPropertySerializer(new DateOfBirthSerializer());
                    }
                );
            }

            public ObjectId EmployeeId { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public DateTime DateOfBirth { get; set; }
        }

        [Test]
        public void TestSerializeEmployee() {
            var employee = new Employee { FirstName = "John", LastName = "Smith", DateOfBirth = new DateTime(2001, 2, 3) };
            var json = employee.ToJson();

            var bson = employee.ToBson();
            var rehydrated = BsonSerializer.Deserialize<Employee>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        public class Account {
            public DateTimeOffset Opened { get; set; }
            public decimal Balance { get; set; }
        }

        [Test]
        public void TestSerializeAccount() {
            var account = new Account { Opened = DateTimeOffset.Now, Balance = 12345.67M };
            var json = account.ToJson();

            var bson = account.ToBson();
            var rehydrated = BsonSerializer.Deserialize<Account>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
