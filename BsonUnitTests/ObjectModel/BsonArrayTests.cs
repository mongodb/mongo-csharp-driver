/* Copyright 2010-2012 10gen Inc.
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

using MongoDB.Bson;
using MongoDB.Bson.IO;

namespace MongoDB.BsonUnitTests
{
    [TestFixture]
    public class BsonArrayTests
    {
        [Test]
        public void TestCapacity()
        {
            var array = new BsonArray(4);
            Assert.AreEqual(4, array.Capacity);
            array.Capacity = 8;
            Assert.AreEqual(8, array.Capacity);
        }

        [Test]
        public void TestCreateBooleanArray()
        {
            var values = new Boolean[] { true, false };
            var array = BsonArray.Create(values);
            Assert.AreEqual(2, array.Count);
            Assert.IsInstanceOf<BsonBoolean>(array[0]);
            Assert.IsInstanceOf<BsonBoolean>(array[1]);
            Assert.AreEqual(true, array[0].AsBoolean);
            Assert.AreEqual(false, array[1].AsBoolean);
        }

        [Test]
        public void TestCreateBsonValueArray()
        {
            var values = new BsonValue[] { true, 1, 1.5 };
            var array = BsonArray.Create(values);
            Assert.AreEqual(3, array.Count);
            Assert.IsInstanceOf<BsonBoolean>(array[0]);
            Assert.IsInstanceOf<BsonInt32>(array[1]);
            Assert.IsInstanceOf<BsonDouble>(array[2]);
            Assert.AreEqual(true, array[0].AsBoolean);
            Assert.AreEqual(1, array[1].AsInt32);
            Assert.AreEqual(1.5, array[2].AsDouble);
        }

        [Test]
        public void TestCreateDateTimeArray()
        {
            var value1 = DateTime.SpecifyKind(new DateTime(2011, 1, 18), DateTimeKind.Utc);
            var value2 = DateTime.SpecifyKind(new DateTime(2011, 1, 19), DateTimeKind.Utc);
            var values = new DateTime[] { value1, value2 };
            var array = BsonArray.Create(values);
            Assert.AreEqual(2, array.Count);
            Assert.IsInstanceOf<BsonDateTime>(array[0]);
            Assert.IsInstanceOf<BsonDateTime>(array[1]);
            Assert.AreEqual(value1, array[0].AsDateTime);
            Assert.AreEqual(value2, array[1].AsDateTime);
        }

        [Test]
        public void TestCreateDoubleArray()
        {
            var values = new double[] { 1.5, 2.5 };
            var array = BsonArray.Create(values);
            Assert.AreEqual(2, array.Count);
            Assert.IsInstanceOf<BsonDouble>(array[0]);
            Assert.IsInstanceOf<BsonDouble>(array[1]);
            Assert.AreEqual(1.5, array[0].AsDouble);
            Assert.AreEqual(2.5, array[1].AsDouble);
        }

        [Test]
        public void TestCreateInt32Array()
        {
            var values = new int[] { 1, 2 };
            var array = BsonArray.Create(values);
            Assert.AreEqual(2, array.Count);
            Assert.IsInstanceOf<BsonInt32>(array[0]);
            Assert.IsInstanceOf<BsonInt32>(array[1]);
            Assert.AreEqual(1, array[0].AsInt32);
            Assert.AreEqual(2, array[1].AsInt32);
        }

        [Test]
        public void TestCreateInt64Array()
        {
            var values = new long[] { 1, 2 };
            var array = BsonArray.Create(values);
            Assert.AreEqual(2, array.Count);
            Assert.IsInstanceOf<BsonInt64>(array[0]);
            Assert.IsInstanceOf<BsonInt64>(array[1]);
            Assert.AreEqual(1, array[0].AsInt64);
            Assert.AreEqual(2, array[1].AsInt64);
        }

        [Test]
        public void TestCreateObjectArray()
        {
            var values = new object[] { true, 1, 1.5 };
            var array = BsonArray.Create(values);
            Assert.AreEqual(3, array.Count);
            Assert.IsInstanceOf<BsonBoolean>(array[0]);
            Assert.IsInstanceOf<BsonInt32>(array[1]);
            Assert.IsInstanceOf<BsonDouble>(array[2]);
            Assert.AreEqual(true, array[0].AsBoolean);
            Assert.AreEqual(1, array[1].AsInt32);
            Assert.AreEqual(1.5, array[2].AsDouble);
        }

        [Test]
        public void TestCreateObjectIdArray()
        {
            var value1 = ObjectId.GenerateNewId();
            var value2 = ObjectId.GenerateNewId();
            var values = new ObjectId[] { value1, value2 };
            var array = BsonArray.Create(values);
            Assert.AreEqual(2, array.Count);
            Assert.IsInstanceOf<BsonObjectId>(array[0]);
            Assert.IsInstanceOf<BsonObjectId>(array[1]);
            Assert.AreEqual(value1, array[0].AsObjectId);
            Assert.AreEqual(value2, array[1].AsObjectId);
        }

        [Test]
        public void TestCreateStringArray()
        {
            var values = new string[] { "a", "b" };
            var array = BsonArray.Create(values);
            Assert.AreEqual(2, array.Count);
            Assert.IsInstanceOf<BsonString>(array[0]);
            Assert.IsInstanceOf<BsonString>(array[1]);
            Assert.AreEqual("a", array[0].AsString);
            Assert.AreEqual("b", array[1].AsString);
        }

        [Test]
        public void TestCreateFromObject()
        {
            var values = (object)new object[]
            {
                1,
                1.5
            };
            var array = BsonArray.Create(values);
            Assert.AreEqual(2, array.Count);
            Assert.IsInstanceOf<BsonInt32>(array[0]);
            Assert.IsInstanceOf<BsonDouble>(array[1]);
            Assert.AreEqual(1, array[0].AsInt32);
            Assert.AreEqual(1.5, array[1].AsDouble);
        }
    }
}
