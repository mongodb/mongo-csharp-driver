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
using MongoDB.Bson;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests
{
    [TestFixture]
    public class BsonDocumentWrapperTests
    {
        private class C
        {
            public int X { get; set; }
        }

        private C _c = new C { X = 1 };

        [Test]
        public void TestConstructorWithObject()
        {
            var wrapper = new BsonDocumentWrapper(_c);
            var expected = "{ \"X\" : 1 }";
            Assert.AreEqual(expected, wrapper.ToJson());
        }

        [Test]
        public void TestConstructorWithNullObject()
        {
            var wrapper = new BsonDocumentWrapper(null);
            var expected = "null";
            Assert.AreEqual(expected, wrapper.ToJson());
        }

        [Test]
        public void TestConstructorWithNominalTypeAndObject()
        {
            var wrapper = new BsonDocumentWrapper(typeof(C), _c);
            var expected = "{ \"X\" : 1 }";
            Assert.AreEqual(expected, wrapper.ToJson());
        }

        [Test]
        public void TestConstructorWithNominalTypeAndNullObject()
        {
            var wrapper = new BsonDocumentWrapper(typeof(C), null);
            var expected = "null";
            Assert.AreEqual(expected, wrapper.ToJson());
        }

        [Test]
        public void TestConstructorWithNullNominalTypeAndObject()
        {
            Assert.Throws<ArgumentNullException>(() => { var wrapper = new BsonDocumentWrapper(null, _c); });
        }

        [Test]
        public void TestConstructorWithNominalTypeAndObjectAndIsUpdateDocument()
        {
            var wrapper = new BsonDocumentWrapper(typeof(C), _c, false);
            var expected = "{ \"X\" : 1 }";
            Assert.AreEqual(expected, wrapper.ToJson());
        }

        [Test]
        public void TestConstructorWithNominalTypeAndNullObjectAndIsUpdateDocument()
        {
            var wrapper = new BsonDocumentWrapper(typeof(C), null, false);
            var expected = "null";
            Assert.AreEqual(expected, wrapper.ToJson());
        }

        [Test]
        public void TestConstructorWithNullNominalTypeAndObjectAndIsUpdateDocument()
        {
            Assert.Throws<ArgumentNullException>(() => { var wrapper = new BsonDocumentWrapper(null, _c, false); });
        }

        [Test]
        public void TestCreateGenericWithValue()
        {
            var wrapper = BsonDocumentWrapper.Create<C>(_c);
            var expected = "{ \"X\" : 1 }";
            Assert.AreEqual(expected, wrapper.ToJson());
        }

        [Test]
        public void TestCreateGenericWithNullValue()
        {
            var wrapper = BsonDocumentWrapper.Create<C>(null);
            var expected = "null";
            Assert.AreEqual(expected, wrapper.ToJson());
        }

        [Test]
        public void TestCreateGenericWithValueAndIsUpdateDocument()
        {
            var wrapper = BsonDocumentWrapper.Create<C>(_c, false);
            var expected = "{ \"X\" : 1 }";
            Assert.AreEqual(expected, wrapper.ToJson());
        }

        [Test]
        public void TestCreateGenericWithNullValueAndIsUpdateDocument()
        {
            var wrapper = BsonDocumentWrapper.Create<C>(null, false);
            var expected = "null";
            Assert.AreEqual(expected, wrapper.ToJson());
        }

        [Test]
        public void TestCreateWithNominalTypeAndValue()
        {
            var wrapper = BsonDocumentWrapper.Create(typeof(C), _c);
            var expected = "{ \"X\" : 1 }";
            Assert.AreEqual(expected, wrapper.ToJson());
        }

        [Test]
        public void TestCreateWithNominalTypeAndNullValue()
        {
            var wrapper = BsonDocumentWrapper.Create(typeof(C), null);
            var expected = "null";
            Assert.AreEqual(expected, wrapper.ToJson());
        }

        [Test]
        public void TestCreateWithNullNominalTypeAndValue()
        {
            Assert.Throws<ArgumentNullException>(() => { var wrapper = BsonDocumentWrapper.Create(null, _c); });
        }

        [Test]
        public void TestCreateWithNominalTypeAndValueAndIsUpdateDocument()
        {
            var wrapper = BsonDocumentWrapper.Create(typeof(C), _c, false);
            var expected = "{ \"X\" : 1 }";
            Assert.AreEqual(expected, wrapper.ToJson());
        }

        [Test]
        public void TestCreateWithNominalTypeAndNullValueAndIsUpdateDocument()
        {
            var wrapper = BsonDocumentWrapper.Create(typeof(C), null, false);
            var expected = "null";
            Assert.AreEqual(expected, wrapper.ToJson());
        }

        [Test]
        public void TestCreateWithNullNominalTypeAndValueAndIsUpdateDocument()
        {
            Assert.Throws<ArgumentNullException>(() => { var wrapper = BsonDocumentWrapper.Create(null, _c, false); });
        }

        [Test]
        public void TestCreateMultipleGenericWithValues()
        {
            var wrappers = BsonDocumentWrapper.CreateMultiple<C>(new C[] { _c, null });
            var expected = "[{ \"X\" : 1 }, null]";
            Assert.AreEqual(expected, wrappers.ToJson());
        }

        [Test]
        public void TestCreateMultipleGenericWithNullValues()
        {
            Assert.Throws<ArgumentNullException>(() => { var wrappers = BsonDocumentWrapper.CreateMultiple<C>(null); });
        }

        [Test]
        public void TestCreateMultipleWithNominalTypeAndValues()
        {
            var wrappers = BsonDocumentWrapper.CreateMultiple(typeof(C), new C[] { _c, null });
            var expected = "[{ \"X\" : 1 }, null]";
            Assert.AreEqual(expected, wrappers.ToJson());
        }

        [Test]
        public void TestCreateMultipleWithNullNominalTypeAndValues()
        {
            Assert.Throws<ArgumentNullException>(() => { var wrappers = BsonDocumentWrapper.CreateMultiple(null, new C[] { _c, null }); });
        }

        [Test]
        public void TestCreateMultipleWithNominalTypeAndNullValues()
        {
            Assert.Throws<ArgumentNullException>(() => { var wrappers = BsonDocumentWrapper.CreateMultiple(typeof(C), null); });
        }
    }
}
