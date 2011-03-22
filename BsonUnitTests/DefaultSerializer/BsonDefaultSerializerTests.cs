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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.BsonUnitTests.Serialization {
    [TestFixture]
    public class BsonDefaultSerializerTests {
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
            private class DateOfBirthSerializer : BsonBaseSerializer {
                public override object Deserialize(
                    BsonReader bsonReader,
                    Type nominalType,
                    IBsonSerializationOptions options
                ) {
                    return XmlConvert.ToDateTime(bsonReader.ReadString(), XmlDateTimeSerializationMode.RoundtripKind);
                }

                public override void Serialize(
                    BsonWriter bsonWriter,
                    Type nominalType,
                    object value,
                    IBsonSerializationOptions options
                ) {
                    var dateTime = (DateTime) value;
                    bsonWriter.WriteString(dateTime.ToString("yyyy-MM-dd"));
                }
            }

            static Employee() {
                BsonClassMap.RegisterClassMap<Employee>(
                    cm => {
                        cm.MapIdProperty(e => e.EmployeeId);
                        cm.MapProperty(e => e.FirstName).SetElementName("fn");
                        cm.MapProperty(e => e.LastName).SetElementName("ln");
                        cm.MapProperty(e => e.DateOfBirth).SetElementName("dob").SetSerializer(new DateOfBirthSerializer());
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

        public class Order {
            public string Customer { get; set; }
            public OrderDetail[] OrderDetails { get; set; }
        }

        public class OrderDetail {
            public string Product { get; set; }
            public int Quantity { get; set; }
        }

        [Test]
        public void TestSerializeOrder() {
            var order = new Order {
                Customer = "John",
                OrderDetails = new[] {
                    new OrderDetail { Product = "Pen", Quantity = 1 },
                    new OrderDetail { Product = "Ruler", Quantity = 2 }
                }
            };
            var json = order.ToJson();
            var expected = "{ 'Customer' : 'John', 'OrderDetails' : # }";
            expected = expected.Replace("#", "[{ 'Product' : 'Pen', 'Quantity' : 1 }, { 'Product' : 'Ruler', 'Quantity' : 2 }]");
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = order.ToBson();
            var rehydrated = BsonSerializer.Deserialize<Order>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
