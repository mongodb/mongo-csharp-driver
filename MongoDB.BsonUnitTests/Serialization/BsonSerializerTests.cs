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

using System;
using System.ComponentModel;
using System.Linq;
using System.Xml;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Serialization
{
    [TestFixture]
    public class BsonSerializerTests
    {
        [Test]
        public void TestAnonymousClass()
        {
            var obj = new
            {
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

        public class Employee
        {
            private class DateOfBirthSerializer : BsonBaseSerializer
            {
                public override object Deserialize(BsonReader bsonReader, Type nominalType, Type actualType, IBsonSerializationOptions options)
                {
                    return XmlConvert.ToDateTime(bsonReader.ReadString(), XmlDateTimeSerializationMode.RoundtripKind);
                }

                public override void Serialize(BsonWriter bsonWriter, Type nominalType, object value, IBsonSerializationOptions options)
                {
                    var dateTime = (DateTime)value;
                    bsonWriter.WriteString(dateTime.ToString("yyyy-MM-dd"));
                }
            }

            static Employee()
            {
                BsonClassMap.RegisterClassMap<Employee>(cm =>
                {
                    cm.MapIdProperty(e => e.EmployeeId);
                    cm.MapProperty(e => e.FirstName).SetElementName("fn");
                    cm.MapProperty(e => e.LastName).SetElementName("ln");
                    cm.MapProperty(e => e.DateOfBirth).SetElementName("dob").SetSerializer(new DateOfBirthSerializer());
                    cm.MapProperty(e => e.Age).SetElementName("age");
                });
            }

            public ObjectId EmployeeId { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public DateTime DateOfBirth { get; set; }
            public int Age
            {
                get
                {
                    DateTime now = DateTime.Today;
                    int age = now.Year - DateOfBirth.Year;
                    if (DateOfBirth > now.AddYears(-age)) 
                        age--;

                    return age;
                }
            }
        }

        [Test]
        public void TestSerializeEmployee()
        {
            var employee = new Employee { FirstName = "John", LastName = "Smith", DateOfBirth = new DateTime(2001, 2, 3) };
            var json = employee.ToJson();

            var bson = employee.ToBson();
            var rehydrated = BsonSerializer.Deserialize<Employee>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        public class Account
        {
            public DateTimeOffset Opened { get; set; }
            public decimal Balance { get; set; }
        }

        [Test]
        public void TestSerializeAccount()
        {
            var account = new Account { Opened = DateTimeOffset.Now, Balance = 12345.67M };
            var json = account.ToJson();

            var bson = account.ToBson();
            var rehydrated = BsonSerializer.Deserialize<Account>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        public class Order
        {
            public string Customer { get; set; }
            public OrderDetail[] OrderDetails { get; set; }
        }

        public class OrderDetail
        {
            public string Product { get; set; }
            public int Quantity { get; set; }
        }

        [Test]
        public void TestSerializeOrder()
        {
            var order = new Order
            {
                Customer = "John",
                OrderDetails = new[]
                {
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

        public class InventoryItem : ISupportInitialize
        {
            [BsonIgnore]
            public bool WasBeginInitCalled;
            [BsonIgnore]
            public bool WasEndInitCalled;

            public int Price { get; set; }

            public void BeginInit()
            {
                WasBeginInitCalled = true;
            }

            public void EndInit()
            {
                WasEndInitCalled = true;
            }
        }

        [Test]
        public void TestSerializeInventoryItem()
        {
            var item = new InventoryItem { Price = 42 };
            var json = item.ToJson();

            var bson = item.ToBson();
            var rehydrated = BsonSerializer.Deserialize<InventoryItem>(bson);
            Assert.IsTrue(rehydrated.WasBeginInitCalled);
            Assert.IsTrue(rehydrated.WasEndInitCalled);
        }

        [BsonKnownTypes(typeof(B), typeof(C))]
        private class A
        { }

        private class B : A
        { }

        private class C : A
        { }

        [Test]
        public void TestLookupActualType()
        {
            var actualType = BsonSerializer.LookupActualType(typeof(A), BsonValue.Create("C"));

            Assert.AreEqual(typeof(C), actualType);
        }
    }
}
