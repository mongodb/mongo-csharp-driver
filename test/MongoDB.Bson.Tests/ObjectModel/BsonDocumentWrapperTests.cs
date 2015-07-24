/* Copyright 2010-2014 MongoDB Inc.
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using NUnit.Framework;

namespace MongoDB.Bson.Tests
{
    [TestFixture]
    public class BsonDocumentWrapperTests
    {
        private class C
        {
            public int X { get; set; }
        }

        private class CSerializer : ClassSerializerBase<C>
        {
            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, C value)
            {
                var bsonWriter = context.Writer;
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteInt32("X", value.X);
                bsonWriter.WriteEndDocument();
            }
        }

        [Test]
        public void TestAddElement()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.AreEqual(false, wrapper.IsMaterialized);
            wrapper.Add(new BsonElement("x", 1));
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(1, wrapper.ElementCount);
            Assert.AreEqual("x", wrapper.GetElement(0).Name);
            Assert.AreEqual(new BsonInt32(1), wrapper[0]);
        }

        [Test]
        public void TestAddNameValue()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.AreEqual(false, wrapper.IsMaterialized);
            wrapper.Add("x", 1);
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(1, wrapper.ElementCount);
            Assert.AreEqual("x", wrapper.GetElement(0).Name);
            Assert.AreEqual(new BsonInt32(1), wrapper[0]);
        }

        [Test]
        public void TestAddNameValueWithCondition()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.AreEqual(false, wrapper.IsMaterialized);
            wrapper.Add("x", 1, false);
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(0, wrapper.ElementCount);

            wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.AreEqual(false, wrapper.IsMaterialized);
            wrapper.Add("x", 1, true);
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(1, wrapper.ElementCount);
            Assert.AreEqual("x", wrapper.GetElement(0).Name);
            Assert.AreEqual(new BsonInt32(1), wrapper[0]);
        }

        [Test]
        public void TestAddNameValueFactoryWithCondition()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.AreEqual(false, wrapper.IsMaterialized);
            wrapper.Add("x", () => 1, false);
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(0, wrapper.ElementCount);

            wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.AreEqual(false, wrapper.IsMaterialized);
            wrapper.Add("x", () => 1, true);
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(1, wrapper.ElementCount);
            Assert.AreEqual("x", wrapper.GetElement(0).Name);
            Assert.AreEqual(new BsonInt32(1), wrapper[0]);
        }

        [Test]
        public void TestAddRangeDictionary()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.AreEqual(false, wrapper.IsMaterialized);
            wrapper.AddRange(new Dictionary<string, object>());
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(0, wrapper.ElementCount);

            wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.AreEqual(false, wrapper.IsMaterialized);
            wrapper.AddRange(new Dictionary<string, object> { { "x", 1 } });
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(1, wrapper.ElementCount);
            Assert.AreEqual("x", wrapper.GetElement(0).Name);
            Assert.AreEqual(new BsonInt32(1), wrapper[0]);
        }

        [Test]
        public void TestAddRangeElements()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.AreEqual(false, wrapper.IsMaterialized);
            wrapper.AddRange(new BsonElement[0]);
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(0, wrapper.ElementCount);

            wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.AreEqual(false, wrapper.IsMaterialized);
            wrapper.AddRange(new[] { new BsonElement("x", 1) });
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(1, wrapper.ElementCount);
            Assert.AreEqual("x", wrapper.GetElement(0).Name);
            Assert.AreEqual(new BsonInt32(1), wrapper[0]);
        }

        [Test]
        public void TestAddRangeIDictionary()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.AreEqual(false, wrapper.IsMaterialized);
            wrapper.AddRange(new Dictionary<string, object>());
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(0, wrapper.ElementCount);

            wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.AreEqual(false, wrapper.IsMaterialized);
            wrapper.AddRange((IDictionary)(new Dictionary<string, object> { { "x", 1 } }));
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(1, wrapper.ElementCount);
            Assert.AreEqual("x", wrapper.GetElement(0).Name);
            Assert.AreEqual(new BsonInt32(1), wrapper[0]);
        }

        [Test]
        public void TestAddRangeKeyValuePairs()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.AreEqual(false, wrapper.IsMaterialized);
            wrapper.AddRange(new KeyValuePair<string, object>[0]);
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(0, wrapper.ElementCount);

            wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.AreEqual(false, wrapper.IsMaterialized);
            wrapper.AddRange(new[] { new KeyValuePair<string, object>("x", 1) });
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(1, wrapper.ElementCount);
            Assert.AreEqual("x", wrapper.GetElement(0).Name);
            Assert.AreEqual(new BsonInt32(1), wrapper[0]);
        }

        [Test]
        public void TestClear()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.AreEqual(false, wrapper.IsMaterialized);
            wrapper.Clear();
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(0, wrapper.ElementCount);
        }

        [Test]
        public void TestClone()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.AreEqual(false, wrapper.IsMaterialized);
            var clonedWrapper = (BsonDocumentWrapper)wrapper.Clone();
            Assert.AreEqual(false, wrapper.IsMaterialized);
            Assert.AreEqual(false, clonedWrapper.IsMaterialized);
            Assert.AreSame(wrapper.Wrapped, clonedWrapper.Wrapped);
            Assert.AreSame(wrapper.Serializer, clonedWrapper.Serializer);
            Assert.AreEqual(wrapper, clonedWrapper);
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(true, clonedWrapper.IsMaterialized);

            wrapper = new BsonDocumentWrapper(new BsonDocument());
            wrapper["x"] = 1;
            Assert.AreEqual(true, wrapper.IsMaterialized);
            var clonedDocument = wrapper.Clone();
            Assert.IsInstanceOf<BsonDocument>(clonedDocument);
            Assert.AreEqual(wrapper, clonedDocument);
        }

        [Test]
        public void TestCompareToBsonDocument()
        {
            var other = new BsonDocument("x", 1);
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.AreEqual(false, wrapper.IsMaterialized);
            var result = wrapper.CompareTo(other);
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(0, result);
        }

        [Test]
        public void TestCompareToBsonValue()
        {
            var other = new BsonInt32(1);
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.AreEqual(false, wrapper.IsMaterialized);
            var result = wrapper.CompareTo(other);
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(1, result);
        }

        [Test]
        public void TestConstructorWithObject()
        {
            var c = CreateC();

            var wrapper = new BsonDocumentWrapper(c);
            Assert.AreSame(UndiscriminatedActualTypeSerializer<object>.Instance, wrapper.Serializer);
            Assert.AreSame(c, wrapper.Wrapped);
            Assert.AreEqual(false, wrapper.IsMaterialized);

            wrapper = new BsonDocumentWrapper(null);
            Assert.AreSame(UndiscriminatedActualTypeSerializer<object>.Instance, wrapper.Serializer);
            Assert.AreSame(null, wrapper.Wrapped);
            Assert.AreEqual(false, wrapper.IsMaterialized);
        }

        [Test]
        public void TestConstructorWithObjectAndSerializer()
        {
            var c = CreateC();
            var serializer = new CSerializer();

            var wrapper = new BsonDocumentWrapper(c, serializer);
            Assert.AreSame(serializer, wrapper.Serializer);
            Assert.AreSame(c, wrapper.Wrapped);
            Assert.AreEqual(false, wrapper.IsMaterialized);

            wrapper = new BsonDocumentWrapper(null, serializer);
            Assert.AreSame(serializer, wrapper.Serializer);
            Assert.AreSame(null, wrapper.Wrapped);
            Assert.AreEqual(false, wrapper.IsMaterialized);

            Assert.Throws<ArgumentNullException>(() => new BsonDocumentWrapper(c, null));
            Assert.DoesNotThrow(() => new BsonDocumentWrapper(c, serializer));
        }

        [Test]
        public void TestContains()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.AreEqual(false, wrapper.IsMaterialized);
            var result = wrapper.Contains("x");
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(true, result);
        }

        [Test]
        public void TestContainsValue()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.AreEqual(false, wrapper.IsMaterialized);
            var result = wrapper.ContainsValue(1);
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(true, result);
        }

        [Test]
        public void TestCreateGenericWithObject()
        {
            var c = CreateC();

            var wrapper = BsonDocumentWrapper.Create<C>(c);
            Assert.AreSame(BsonSerializer.LookupSerializer(typeof(C)), wrapper.Serializer);
            Assert.AreSame(c, wrapper.Wrapped);
            Assert.AreEqual(false, wrapper.IsMaterialized);

            wrapper = BsonDocumentWrapper.Create<C>(null);
            Assert.AreSame(BsonSerializer.LookupSerializer(typeof(C)), wrapper.Serializer);
            Assert.AreSame(null, wrapper.Wrapped);
            Assert.AreEqual(false, wrapper.IsMaterialized);
        }

        [Test]
        public void TestCreateWithNominalTypeAndObject()
        {
            var c = CreateC();

            var wrapper = BsonDocumentWrapper.Create(typeof(C), c);
            Assert.AreSame(BsonSerializer.LookupSerializer(typeof(C)), wrapper.Serializer);
            Assert.AreSame(c, wrapper.Wrapped);
            Assert.AreEqual(false, wrapper.IsMaterialized);

            wrapper = BsonDocumentWrapper.Create(typeof(C), null);
            Assert.AreSame(BsonSerializer.LookupSerializer(typeof(C)), wrapper.Serializer);
            Assert.AreSame(null, wrapper.Wrapped);
            Assert.AreEqual(false, wrapper.IsMaterialized);

            Assert.Throws<ArgumentNullException>(() => BsonDocumentWrapper.Create(null, c));
        }

        [Test]
        public void TestCreateMultipleGeneric()
        {
            var c = new C { X = 1 };

            var wrappers = BsonDocumentWrapper.CreateMultiple<C>(new[] { c, null }).ToArray();
            Assert.AreEqual(2, wrappers.Length);

            var wrapper1 = wrappers[0];
            Assert.AreSame(BsonSerializer.LookupSerializer(typeof(C)), wrapper1.Serializer);
            Assert.AreSame(c, wrapper1.Wrapped);
            Assert.AreEqual(false, wrapper1.IsMaterialized);

            var wrapper2 = wrappers[1];
            Assert.AreSame(BsonSerializer.LookupSerializer(typeof(C)), wrapper2.Serializer);
            Assert.AreSame(null, wrapper2.Wrapped);
            Assert.AreEqual(false, wrapper2.IsMaterialized);

            Assert.Throws<ArgumentNullException>(() => BsonDocumentWrapper.CreateMultiple<C>(null));
        }

        [Test]
        public void TestCreateMultiple()
        {
            var c = new C { X = 1 };

            var wrappers = BsonDocumentWrapper.CreateMultiple(typeof(C), new[] { c, null }).ToArray();
            Assert.AreEqual(2, wrappers.Length);

            var wrapper1 = wrappers[0];
            Assert.AreSame(BsonSerializer.LookupSerializer(typeof(C)), wrapper1.Serializer);
            Assert.AreSame(c, wrapper1.Wrapped);
            Assert.AreEqual(false, wrapper1.IsMaterialized);

            var wrapper2 = wrappers[1];
            Assert.AreSame(BsonSerializer.LookupSerializer(typeof(C)), wrapper2.Serializer);
            Assert.AreSame(null, wrapper2.Wrapped);
            Assert.AreEqual(false, wrapper2.IsMaterialized);

            Assert.Throws<ArgumentNullException>(() => BsonDocumentWrapper.CreateMultiple(null, new[] { c, null }));
            Assert.Throws<ArgumentNullException>(() => BsonDocumentWrapper.CreateMultiple(typeof(C), null));
        }

        [Test]
        public void TestDeepClone()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.AreEqual(false, wrapper.IsMaterialized);
            var deepClone = wrapper.DeepClone();
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.IsInstanceOf<BsonDocument>(deepClone);
            Assert.AreEqual(wrapper, deepClone);
        }

        [Test]
        public void TestElementCount()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.AreEqual(false, wrapper.IsMaterialized);
            Assert.AreEqual(0, wrapper.ElementCount);
            Assert.AreEqual(true, wrapper.IsMaterialized);

            wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.AreEqual(false, wrapper.IsMaterialized);
            Assert.AreEqual(1, wrapper.ElementCount);
            Assert.AreEqual(true, wrapper.IsMaterialized);
        }

        [Test]
        public void TestElements()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.AreEqual(false, wrapper.IsMaterialized);
            var elements = wrapper.Elements.ToArray();
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(0, elements.Length);

            wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.AreEqual(false, wrapper.IsMaterialized);
            elements = wrapper.Elements.ToArray();
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(1, elements.Length);
            Assert.AreEqual("x", elements[0].Name);
            Assert.AreEqual(new BsonInt32(1), elements[0].Value);
        }

        [Test]
        public void TestEquals()
        {
            var other = new BsonDocument("x", 1);
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.AreEqual(false, wrapper.IsMaterialized);
            var result = wrapper.Equals(other);
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(true, result);
        }

        [Test]
        public void TestGetElement()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.AreEqual(false, wrapper.IsMaterialized);
            var element = wrapper.GetElement(0);
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual("x", element.Name);
            Assert.AreEqual(new BsonInt32(1), element.Value);
        }

        [Test]
        public void TestGetElementByName()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.AreEqual(false, wrapper.IsMaterialized);
            var element = wrapper.GetElement("x");
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual("x", element.Name);
            Assert.AreEqual(new BsonInt32(1), element.Value);
        }

        [Test]
        public void TestGetEnumerator()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.AreEqual(false, wrapper.IsMaterialized);
            var enumerator = wrapper.GetEnumerator();
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual("x", enumerator.Current.Name);
            Assert.AreEqual(new BsonInt32(1), enumerator.Current.Value);
            Assert.IsFalse(enumerator.MoveNext());
        }

        [Test]
        public void TestGetHashCode()
        {
            var wrapper1 = new BsonDocumentWrapper(new BsonDocument("x", 1));
            var wrapper2 = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.AreEqual(false, wrapper1.IsMaterialized);
            Assert.AreEqual(false, wrapper2.IsMaterialized);
            var hashCode1 = wrapper1.GetHashCode();
            var hashCode2 = wrapper2.GetHashCode();
            Assert.AreEqual(true, wrapper1.IsMaterialized);
            Assert.AreEqual(true, wrapper2.IsMaterialized);
            Assert.AreEqual(hashCode1, hashCode2);
        }

        [Test]
        public void TestGetValue()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.AreEqual(false, wrapper.IsMaterialized);
            var value = wrapper.GetValue(0);
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(new BsonInt32(1), value);
        }

        [Test]
        public void TestGetValueByName()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.AreEqual(false, wrapper.IsMaterialized);
            var value = wrapper.GetValue("x");
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(new BsonInt32(1), value);
        }

        [Test]
        public void TestGetValueByNameWithDefaultValue()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.AreEqual(false, wrapper.IsMaterialized);
            var x = wrapper.GetValue("x", 2);
            var y = wrapper.GetValue("y", 2);
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(new BsonInt32(1), x);
            Assert.AreEqual(new BsonInt32(2), y);
        }

        [Test]
        public void TestIndexer()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.AreEqual(false, wrapper.IsMaterialized);
            var value = wrapper[0];
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(new BsonInt32(1), value);

            wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.AreEqual(false, wrapper.IsMaterialized);
            wrapper[0] = 2;
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(1, wrapper.ElementCount);
            Assert.AreEqual(new BsonInt32(2), wrapper[0]);
        }

        [Test]
        public void TestIndexerByName()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.AreEqual(false, wrapper.IsMaterialized);
            var value = wrapper["x"];
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(new BsonInt32(1), value);

            wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.AreEqual(false, wrapper.IsMaterialized);
            wrapper["x"] = 1;
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(1, wrapper.ElementCount);
            Assert.AreEqual("x", wrapper.GetElement(0).Name);
            Assert.AreEqual(new BsonInt32(1), wrapper[0]);
        }

        [Test]
        public void TestInsertAt()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.AreEqual(false, wrapper.IsMaterialized);
            wrapper.InsertAt(0, new BsonElement("x", 1));
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(1, wrapper.ElementCount);
            Assert.AreEqual("x", wrapper.GetElement(0).Name);
            Assert.AreEqual(new BsonInt32(1), wrapper[0]);
        }

        [Test]
        public void TestMerge()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.AreEqual(false, wrapper.IsMaterialized);
            var result = wrapper.Merge(new BsonDocument("x", 2));
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreSame(wrapper, result);
            Assert.AreEqual(1, wrapper.ElementCount);
            Assert.AreEqual("x", wrapper.GetElement(0).Name);
            Assert.AreEqual(new BsonInt32(1), wrapper[0]);
        }

        [Test]
        public void TestMergeOverwrite()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.AreEqual(false, wrapper.IsMaterialized);
            var result = wrapper.Merge(new BsonDocument("x", 2), true);
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreSame(wrapper, result);
            Assert.AreEqual(1, wrapper.ElementCount);
            Assert.AreEqual("x", wrapper.GetElement(0).Name);
            Assert.AreEqual(new BsonInt32(2), wrapper[0]);
        }

        [Test]
        public void TestNames()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.AreEqual(false, wrapper.IsMaterialized);
            var names = wrapper.Names.ToArray();
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(0, names.Length);

            wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.AreEqual(false, wrapper.IsMaterialized);
            names = wrapper.Names.ToArray();
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(1, names.Length);
            Assert.AreEqual("x", names[0]);
        }

        [Test]
        public void TestRawValues()
        {
#pragma warning disable 618
            var wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.AreEqual(false, wrapper.IsMaterialized);
            var values = wrapper.RawValues.ToArray();
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(0, values.Length);

            wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.AreEqual(false, wrapper.IsMaterialized);
            values = wrapper.RawValues.ToArray();
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(1, values.Length);
            Assert.AreEqual(1, values[0]);
#pragma warning restore
        }

        [Test]
        public void TestRemove()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument { { "x", 1 }, { "y", 2 } });
            Assert.AreEqual(false, wrapper.IsMaterialized);
            wrapper.Remove("y");
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(1, wrapper.ElementCount);
            Assert.AreEqual("x", wrapper.GetElement(0).Name);
            Assert.AreEqual(new BsonInt32(1), wrapper[0]);
        }

        [Test]
        public void TestRemoveAt()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument { { "x", 1 }, { "y", 2 } });
            Assert.AreEqual(false, wrapper.IsMaterialized);
            wrapper.RemoveAt(1);
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(1, wrapper.ElementCount);
            Assert.AreEqual("x", wrapper.GetElement(0).Name);
            Assert.AreEqual(new BsonInt32(1), wrapper[0]);
        }

        [Test]
        public void TestRemoveElement()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument { { "x", 1 }, { "y", 2 } });
            Assert.AreEqual(false, wrapper.IsMaterialized);
            wrapper.RemoveElement(new BsonElement("y", 2));
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(1, wrapper.ElementCount);
            Assert.AreEqual("x", wrapper.GetElement(0).Name);
            Assert.AreEqual(new BsonInt32(1), wrapper[0]);
        }

        [Test]
        public void TestSet()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.AreEqual(false, wrapper.IsMaterialized);
            wrapper.Set(0, 2);
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(1, wrapper.ElementCount);
            Assert.AreEqual("x", wrapper.GetElement(0).Name);
            Assert.AreEqual(new BsonInt32(2), wrapper[0]);
        }

        [Test]
        public void TestSetByName()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.AreEqual(false, wrapper.IsMaterialized);
            wrapper.Set("x", 2);
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(1, wrapper.ElementCount);
            Assert.AreEqual("x", wrapper.GetElement(0).Name);
            Assert.AreEqual(new BsonInt32(2), wrapper[0]);
        }

        [Test]
        public void TestSetElement()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.AreEqual(false, wrapper.IsMaterialized);
            wrapper.SetElement(0, new BsonElement("x", 2));
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(1, wrapper.ElementCount);
            Assert.AreEqual("x", wrapper.GetElement(0).Name);
            Assert.AreEqual(new BsonInt32(2), wrapper[0]);
        }

        [Test]
        public void TestSetElementByName()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.AreEqual(false, wrapper.IsMaterialized);
            wrapper.SetElement(new BsonElement("x", 2));
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(1, wrapper.ElementCount);
            Assert.AreEqual("x", wrapper.GetElement(0).Name);
            Assert.AreEqual(new BsonInt32(2), wrapper[0]);
        }

        [Test]
        public void TestToDictionary()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument { { "x", 1 }, { "y", 2 } });
            Assert.AreEqual(false, wrapper.IsMaterialized);
            var dictionary = wrapper.ToDictionary();
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(2, dictionary.Count);
            Assert.AreEqual(1, dictionary["x"]);
            Assert.AreEqual(2, dictionary["y"]);
        }

        [Test]
        public void TestToHashtable()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument { { "x", 1 }, { "y", 2 } });
            Assert.AreEqual(false, wrapper.IsMaterialized);
            var hashtable = wrapper.ToHashtable();
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(2, hashtable.Count);
            Assert.AreEqual(1, hashtable["x"]);
            Assert.AreEqual(2, hashtable["y"]);
        }

        [Test]
        public void TestToString()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.AreEqual(false, wrapper.IsMaterialized);
            var expected = "{ 'x' : 1 }".Replace("'", "\"");
            var s = wrapper.ToString();
            Assert.AreEqual(false, wrapper.IsMaterialized); // ToString just serializes, doesn't materialize
            Assert.AreEqual(expected, s);
        }

        [Test]
        public void TestTryGetElement()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.AreEqual(false, wrapper.IsMaterialized);
            BsonElement element;
            var result = wrapper.TryGetElement("x", out element);
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(true, result);
            Assert.AreEqual("x", element.Name);
            Assert.AreEqual(new BsonInt32(1), element.Value);
        }

        [Test]
        public void TestTryGetValue()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.AreEqual(false, wrapper.IsMaterialized);
            BsonValue value;
            var result = wrapper.TryGetValue("x", out value);
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(true, result);
            Assert.AreEqual(new BsonInt32(1), value);
        }

        [Test]
        public void TestValues()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.AreEqual(false, wrapper.IsMaterialized);
            var values = wrapper.Values.ToArray();
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(0, values.Length);

            wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.AreEqual(false, wrapper.IsMaterialized);
            values = wrapper.Values.ToArray();
            Assert.AreEqual(true, wrapper.IsMaterialized);
            Assert.AreEqual(1, values.Length);
            Assert.AreEqual(new BsonInt32(1), values[0]);
        }

        // private methods
        private C CreateC()
        {
            return new C { X = 1 };
        }

        private BsonDocumentWrapper CreateWrapper()
        {
            var c = CreateC();
            return new BsonDocumentWrapper(c);
        }
    }
}
