/* Copyright 2010-2016 MongoDB Inc.
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
using Xunit;

namespace MongoDB.Bson.Tests
{
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

        [Fact]
        public void TestAddElement()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.Equal(false, wrapper.IsMaterialized);
            wrapper.Add(new BsonElement("x", 1));
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(1, wrapper.ElementCount);
            Assert.Equal("x", wrapper.GetElement(0).Name);
            Assert.Equal(new BsonInt32(1), wrapper[0]);
        }

        [Fact]
        public void TestAddNameValue()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.Equal(false, wrapper.IsMaterialized);
            wrapper.Add("x", 1);
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(1, wrapper.ElementCount);
            Assert.Equal("x", wrapper.GetElement(0).Name);
            Assert.Equal(new BsonInt32(1), wrapper[0]);
        }

        [Fact]
        public void TestAddNameValueWithCondition()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.Equal(false, wrapper.IsMaterialized);
            wrapper.Add("x", 1, false);
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(0, wrapper.ElementCount);

            wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.Equal(false, wrapper.IsMaterialized);
            wrapper.Add("x", 1, true);
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(1, wrapper.ElementCount);
            Assert.Equal("x", wrapper.GetElement(0).Name);
            Assert.Equal(new BsonInt32(1), wrapper[0]);
        }

        [Fact]
        public void TestAddNameValueFactoryWithCondition()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.Equal(false, wrapper.IsMaterialized);
            wrapper.Add("x", () => 1, false);
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(0, wrapper.ElementCount);

            wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.Equal(false, wrapper.IsMaterialized);
            wrapper.Add("x", () => 1, true);
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(1, wrapper.ElementCount);
            Assert.Equal("x", wrapper.GetElement(0).Name);
            Assert.Equal(new BsonInt32(1), wrapper[0]);
        }

        [Fact]
        public void TestAddRangeDictionary()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.Equal(false, wrapper.IsMaterialized);
            wrapper.AddRange(new Dictionary<string, object>());
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(0, wrapper.ElementCount);

            wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.Equal(false, wrapper.IsMaterialized);
            wrapper.AddRange(new Dictionary<string, object> { { "x", 1 } });
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(1, wrapper.ElementCount);
            Assert.Equal("x", wrapper.GetElement(0).Name);
            Assert.Equal(new BsonInt32(1), wrapper[0]);
        }

        [Fact]
        public void TestAddRangeElements()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.Equal(false, wrapper.IsMaterialized);
            wrapper.AddRange(new BsonElement[0]);
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(0, wrapper.ElementCount);

            wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.Equal(false, wrapper.IsMaterialized);
            wrapper.AddRange(new[] { new BsonElement("x", 1) });
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(1, wrapper.ElementCount);
            Assert.Equal("x", wrapper.GetElement(0).Name);
            Assert.Equal(new BsonInt32(1), wrapper[0]);
        }

        [Fact]
        public void TestAddRangeIDictionary()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.Equal(false, wrapper.IsMaterialized);
            wrapper.AddRange(new Dictionary<string, object>());
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(0, wrapper.ElementCount);

            wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.Equal(false, wrapper.IsMaterialized);
            wrapper.AddRange((IDictionary)(new Dictionary<string, object> { { "x", 1 } }));
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(1, wrapper.ElementCount);
            Assert.Equal("x", wrapper.GetElement(0).Name);
            Assert.Equal(new BsonInt32(1), wrapper[0]);
        }

        [Fact]
        public void TestAddRangeKeyValuePairs()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.Equal(false, wrapper.IsMaterialized);
            wrapper.AddRange(new KeyValuePair<string, object>[0]);
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(0, wrapper.ElementCount);

            wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.Equal(false, wrapper.IsMaterialized);
            wrapper.AddRange(new[] { new KeyValuePair<string, object>("x", 1) });
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(1, wrapper.ElementCount);
            Assert.Equal("x", wrapper.GetElement(0).Name);
            Assert.Equal(new BsonInt32(1), wrapper[0]);
        }

        [Fact]
        public void TestClear()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.Equal(false, wrapper.IsMaterialized);
            wrapper.Clear();
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(0, wrapper.ElementCount);
        }

        [Fact]
        public void TestClone()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.Equal(false, wrapper.IsMaterialized);
            var clonedWrapper = (BsonDocumentWrapper)wrapper.Clone();
            Assert.Equal(false, wrapper.IsMaterialized);
            Assert.Equal(false, clonedWrapper.IsMaterialized);
            Assert.Same(wrapper.Wrapped, clonedWrapper.Wrapped);
            Assert.Same(wrapper.Serializer, clonedWrapper.Serializer);
            Assert.Equal(wrapper, clonedWrapper);
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(true, clonedWrapper.IsMaterialized);

            wrapper = new BsonDocumentWrapper(new BsonDocument());
            wrapper["x"] = 1;
            Assert.Equal(true, wrapper.IsMaterialized);
            var clonedDocument = wrapper.Clone();
            Assert.IsType<BsonDocument>(clonedDocument);
            Assert.StrictEqual(wrapper, clonedDocument);
        }

        [Fact]
        public void TestCompareToBsonDocument()
        {
            var other = new BsonDocument("x", 1);
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.Equal(false, wrapper.IsMaterialized);
            var result = wrapper.CompareTo(other);
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(0, result);
        }

        [Fact]
        public void TestCompareToBsonValue()
        {
            var other = new BsonInt32(1);
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.Equal(false, wrapper.IsMaterialized);
            var result = wrapper.CompareTo(other);
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(1, result);
        }

        [Fact]
        public void TestConstructorWithObject()
        {
            var c = CreateC();

            var wrapper = new BsonDocumentWrapper(c);
            Assert.Same(UndiscriminatedActualTypeSerializer<object>.Instance, wrapper.Serializer);
            Assert.Same(c, wrapper.Wrapped);
            Assert.Equal(false, wrapper.IsMaterialized);

            wrapper = new BsonDocumentWrapper(null);
            Assert.Same(UndiscriminatedActualTypeSerializer<object>.Instance, wrapper.Serializer);
            Assert.Same(null, wrapper.Wrapped);
            Assert.Equal(false, wrapper.IsMaterialized);
        }

        [Fact]
        public void TestConstructorWithObjectAndSerializer()
        {
            var c = CreateC();
            var serializer = new CSerializer();

            var wrapper = new BsonDocumentWrapper(c, serializer);
            Assert.Same(serializer, wrapper.Serializer);
            Assert.Same(c, wrapper.Wrapped);
            Assert.Equal(false, wrapper.IsMaterialized);

            wrapper = new BsonDocumentWrapper(null, serializer);
            Assert.Same(serializer, wrapper.Serializer);
            Assert.Same(null, wrapper.Wrapped);
            Assert.Equal(false, wrapper.IsMaterialized);

            Assert.Throws<ArgumentNullException>(() => new BsonDocumentWrapper(c, null));
            new BsonDocumentWrapper(c, serializer);
        }

        [Fact]
        public void TestContains()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.Equal(false, wrapper.IsMaterialized);
            var result = wrapper.Contains("x");
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(true, result);
        }

        [Fact]
        public void TestContainsValue()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.Equal(false, wrapper.IsMaterialized);
            var result = wrapper.ContainsValue(1);
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(true, result);
        }

        [Fact]
        public void TestCreateGenericWithObject()
        {
            var c = CreateC();

            var wrapper = BsonDocumentWrapper.Create<C>(c);
            Assert.Same(BsonSerializer.LookupSerializer(typeof(C)), wrapper.Serializer);
            Assert.Same(c, wrapper.Wrapped);
            Assert.Equal(false, wrapper.IsMaterialized);

            wrapper = BsonDocumentWrapper.Create<C>(null);
            Assert.Same(BsonSerializer.LookupSerializer(typeof(C)), wrapper.Serializer);
            Assert.Same(null, wrapper.Wrapped);
            Assert.Equal(false, wrapper.IsMaterialized);
        }

        [Fact]
        public void TestCreateWithNominalTypeAndObject()
        {
            var c = CreateC();

            var wrapper = BsonDocumentWrapper.Create(typeof(C), c);
            Assert.Same(BsonSerializer.LookupSerializer(typeof(C)), wrapper.Serializer);
            Assert.Same(c, wrapper.Wrapped);
            Assert.Equal(false, wrapper.IsMaterialized);

            wrapper = BsonDocumentWrapper.Create(typeof(C), null);
            Assert.Same(BsonSerializer.LookupSerializer(typeof(C)), wrapper.Serializer);
            Assert.Same(null, wrapper.Wrapped);
            Assert.Equal(false, wrapper.IsMaterialized);

            Assert.Throws<ArgumentNullException>(() => BsonDocumentWrapper.Create(null, c));
        }

        [Fact]
        public void TestCreateMultipleGeneric()
        {
            var c = new C { X = 1 };

            var wrappers = BsonDocumentWrapper.CreateMultiple<C>(new[] { c, null }).ToArray();
            Assert.Equal(2, wrappers.Length);

            var wrapper1 = wrappers[0];
            Assert.Same(BsonSerializer.LookupSerializer(typeof(C)), wrapper1.Serializer);
            Assert.Same(c, wrapper1.Wrapped);
            Assert.Equal(false, wrapper1.IsMaterialized);

            var wrapper2 = wrappers[1];
            Assert.Same(BsonSerializer.LookupSerializer(typeof(C)), wrapper2.Serializer);
            Assert.Same(null, wrapper2.Wrapped);
            Assert.Equal(false, wrapper2.IsMaterialized);

            Assert.Throws<ArgumentNullException>(() => BsonDocumentWrapper.CreateMultiple<C>(null));
        }

        [Fact]
        public void TestCreateMultiple()
        {
            var c = new C { X = 1 };

            var wrappers = BsonDocumentWrapper.CreateMultiple(typeof(C), new[] { c, null }).ToArray();
            Assert.Equal(2, wrappers.Length);

            var wrapper1 = wrappers[0];
            Assert.Same(BsonSerializer.LookupSerializer(typeof(C)), wrapper1.Serializer);
            Assert.Same(c, wrapper1.Wrapped);
            Assert.Equal(false, wrapper1.IsMaterialized);

            var wrapper2 = wrappers[1];
            Assert.Same(BsonSerializer.LookupSerializer(typeof(C)), wrapper2.Serializer);
            Assert.Same(null, wrapper2.Wrapped);
            Assert.Equal(false, wrapper2.IsMaterialized);

            Assert.Throws<ArgumentNullException>(() => BsonDocumentWrapper.CreateMultiple(null, new[] { c, null }));
            Assert.Throws<ArgumentNullException>(() => BsonDocumentWrapper.CreateMultiple(typeof(C), null));
        }

        [Fact]
        public void TestDeepClone()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.Equal(false, wrapper.IsMaterialized);
            var deepClone = wrapper.DeepClone();
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.IsType<BsonDocument>(deepClone);
            Assert.StrictEqual(wrapper, deepClone);
        }

        [Fact]
        public void TestElementCount()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.Equal(false, wrapper.IsMaterialized);
            Assert.Equal(0, wrapper.ElementCount);
            Assert.Equal(true, wrapper.IsMaterialized);

            wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.Equal(false, wrapper.IsMaterialized);
            Assert.Equal(1, wrapper.ElementCount);
            Assert.Equal(true, wrapper.IsMaterialized);
        }

        [Fact]
        public void TestElements()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.Equal(false, wrapper.IsMaterialized);
            var elements = wrapper.Elements.ToArray();
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(0, elements.Length);

            wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.Equal(false, wrapper.IsMaterialized);
            elements = wrapper.Elements.ToArray();
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(1, elements.Length);
            Assert.Equal("x", elements[0].Name);
            Assert.Equal(new BsonInt32(1), elements[0].Value);
        }

        [Fact]
        public void TestEquals()
        {
            var other = new BsonDocument("x", 1);
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.Equal(false, wrapper.IsMaterialized);
            var result = wrapper.Equals(other);
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(true, result);
        }

        [Fact]
        public void TestGetElement()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.Equal(false, wrapper.IsMaterialized);
            var element = wrapper.GetElement(0);
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal("x", element.Name);
            Assert.Equal(new BsonInt32(1), element.Value);
        }

        [Fact]
        public void TestGetElementByName()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.Equal(false, wrapper.IsMaterialized);
            var element = wrapper.GetElement("x");
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal("x", element.Name);
            Assert.Equal(new BsonInt32(1), element.Value);
        }

        [Fact]
        public void TestGetEnumerator()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.Equal(false, wrapper.IsMaterialized);
            var enumerator = wrapper.GetEnumerator();
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("x", enumerator.Current.Name);
            Assert.Equal(new BsonInt32(1), enumerator.Current.Value);
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void TestGetHashCode()
        {
            var wrapper1 = new BsonDocumentWrapper(new BsonDocument("x", 1));
            var wrapper2 = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.Equal(false, wrapper1.IsMaterialized);
            Assert.Equal(false, wrapper2.IsMaterialized);
            var hashCode1 = wrapper1.GetHashCode();
            var hashCode2 = wrapper2.GetHashCode();
            Assert.Equal(true, wrapper1.IsMaterialized);
            Assert.Equal(true, wrapper2.IsMaterialized);
            Assert.Equal(hashCode1, hashCode2);
        }

        [Fact]
        public void TestGetValue()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.Equal(false, wrapper.IsMaterialized);
            var value = wrapper.GetValue(0);
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(new BsonInt32(1), value);
        }

        [Fact]
        public void TestGetValueByName()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.Equal(false, wrapper.IsMaterialized);
            var value = wrapper.GetValue("x");
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(new BsonInt32(1), value);
        }

        [Fact]
        public void TestGetValueByNameWithDefaultValue()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.Equal(false, wrapper.IsMaterialized);
            var x = wrapper.GetValue("x", 2);
            var y = wrapper.GetValue("y", 2);
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(new BsonInt32(1), x);
            Assert.Equal(new BsonInt32(2), y);
        }

        [Fact]
        public void TestIndexer()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.Equal(false, wrapper.IsMaterialized);
            var value = wrapper[0];
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(new BsonInt32(1), value);

            wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.Equal(false, wrapper.IsMaterialized);
            wrapper[0] = 2;
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(1, wrapper.ElementCount);
            Assert.Equal(new BsonInt32(2), wrapper[0]);
        }

        [Fact]
        public void TestIndexerByName()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.Equal(false, wrapper.IsMaterialized);
            var value = wrapper["x"];
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(new BsonInt32(1), value);

            wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.Equal(false, wrapper.IsMaterialized);
            wrapper["x"] = 1;
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(1, wrapper.ElementCount);
            Assert.Equal("x", wrapper.GetElement(0).Name);
            Assert.Equal(new BsonInt32(1), wrapper[0]);
        }

        [Fact]
        public void TestInsertAt()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.Equal(false, wrapper.IsMaterialized);
            wrapper.InsertAt(0, new BsonElement("x", 1));
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(1, wrapper.ElementCount);
            Assert.Equal("x", wrapper.GetElement(0).Name);
            Assert.Equal(new BsonInt32(1), wrapper[0]);
        }

        [Fact]
        public void TestMerge()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.Equal(false, wrapper.IsMaterialized);
            var result = wrapper.Merge(new BsonDocument("x", 2));
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Same(wrapper, result);
            Assert.Equal(1, wrapper.ElementCount);
            Assert.Equal("x", wrapper.GetElement(0).Name);
            Assert.Equal(new BsonInt32(1), wrapper[0]);
        }

        [Fact]
        public void TestMergeOverwrite()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.Equal(false, wrapper.IsMaterialized);
            var result = wrapper.Merge(new BsonDocument("x", 2), true);
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Same(wrapper, result);
            Assert.Equal(1, wrapper.ElementCount);
            Assert.Equal("x", wrapper.GetElement(0).Name);
            Assert.Equal(new BsonInt32(2), wrapper[0]);
        }

        [Fact]
        public void TestNames()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.Equal(false, wrapper.IsMaterialized);
            var names = wrapper.Names.ToArray();
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(0, names.Length);

            wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.Equal(false, wrapper.IsMaterialized);
            names = wrapper.Names.ToArray();
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(1, names.Length);
            Assert.Equal("x", names[0]);
        }

        [Fact]
        public void TestRawValues()
        {
#pragma warning disable 618
            var wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.Equal(false, wrapper.IsMaterialized);
            var values = wrapper.RawValues.ToArray();
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(0, values.Length);

            wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.Equal(false, wrapper.IsMaterialized);
            values = wrapper.RawValues.ToArray();
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(1, values.Length);
            Assert.Equal(1, values[0]);
#pragma warning restore
        }

        [Fact]
        public void TestRemove()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument { { "x", 1 }, { "y", 2 } });
            Assert.Equal(false, wrapper.IsMaterialized);
            wrapper.Remove("y");
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(1, wrapper.ElementCount);
            Assert.Equal("x", wrapper.GetElement(0).Name);
            Assert.Equal(new BsonInt32(1), wrapper[0]);
        }

        [Fact]
        public void TestRemoveAt()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument { { "x", 1 }, { "y", 2 } });
            Assert.Equal(false, wrapper.IsMaterialized);
            wrapper.RemoveAt(1);
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(1, wrapper.ElementCount);
            Assert.Equal("x", wrapper.GetElement(0).Name);
            Assert.Equal(new BsonInt32(1), wrapper[0]);
        }

        [Fact]
        public void TestRemoveElement()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument { { "x", 1 }, { "y", 2 } });
            Assert.Equal(false, wrapper.IsMaterialized);
            wrapper.RemoveElement(new BsonElement("y", 2));
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(1, wrapper.ElementCount);
            Assert.Equal("x", wrapper.GetElement(0).Name);
            Assert.Equal(new BsonInt32(1), wrapper[0]);
        }

        [Fact]
        public void TestSet()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.Equal(false, wrapper.IsMaterialized);
            wrapper.Set(0, 2);
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(1, wrapper.ElementCount);
            Assert.Equal("x", wrapper.GetElement(0).Name);
            Assert.Equal(new BsonInt32(2), wrapper[0]);
        }

        [Fact]
        public void TestSetByName()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.Equal(false, wrapper.IsMaterialized);
            wrapper.Set("x", 2);
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(1, wrapper.ElementCount);
            Assert.Equal("x", wrapper.GetElement(0).Name);
            Assert.Equal(new BsonInt32(2), wrapper[0]);
        }

        [Fact]
        public void TestSetElement()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.Equal(false, wrapper.IsMaterialized);
            wrapper.SetElement(0, new BsonElement("x", 2));
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(1, wrapper.ElementCount);
            Assert.Equal("x", wrapper.GetElement(0).Name);
            Assert.Equal(new BsonInt32(2), wrapper[0]);
        }

        [Fact]
        public void TestSetElementByName()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.Equal(false, wrapper.IsMaterialized);
            wrapper.SetElement(new BsonElement("x", 2));
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(1, wrapper.ElementCount);
            Assert.Equal("x", wrapper.GetElement(0).Name);
            Assert.Equal(new BsonInt32(2), wrapper[0]);
        }

        [Fact]
        public void TestToDictionary()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument { { "x", 1 }, { "y", 2 } });
            Assert.Equal(false, wrapper.IsMaterialized);
            var dictionary = wrapper.ToDictionary();
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(2, dictionary.Count);
            Assert.Equal(1, dictionary["x"]);
            Assert.Equal(2, dictionary["y"]);
        }

        [Fact]
        public void TestToHashtable()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument { { "x", 1 }, { "y", 2 } });
            Assert.Equal(false, wrapper.IsMaterialized);
            var hashtable = wrapper.ToHashtable();
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(2, hashtable.Count);
            Assert.Equal(1, hashtable["x"]);
            Assert.Equal(2, hashtable["y"]);
        }

        [Fact]
        public void TestToString()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.Equal(false, wrapper.IsMaterialized);
            var expected = "{ 'x' : 1 }".Replace("'", "\"");
            var s = wrapper.ToString();
            Assert.Equal(false, wrapper.IsMaterialized); // ToString just serializes, doesn't materialize
            Assert.Equal(expected, s);
        }

        [Fact]
        public void TestTryGetElement()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.Equal(false, wrapper.IsMaterialized);
            BsonElement element;
            var result = wrapper.TryGetElement("x", out element);
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(true, result);
            Assert.Equal("x", element.Name);
            Assert.Equal(new BsonInt32(1), element.Value);
        }

        [Fact]
        public void TestTryGetValue()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.Equal(false, wrapper.IsMaterialized);
            BsonValue value;
            var result = wrapper.TryGetValue("x", out value);
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(true, result);
            Assert.Equal(new BsonInt32(1), value);
        }

        [Fact]
        public void TestValues()
        {
            var wrapper = new BsonDocumentWrapper(new BsonDocument());
            Assert.Equal(false, wrapper.IsMaterialized);
            var values = wrapper.Values.ToArray();
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(0, values.Length);

            wrapper = new BsonDocumentWrapper(new BsonDocument("x", 1));
            Assert.Equal(false, wrapper.IsMaterialized);
            values = wrapper.Values.ToArray();
            Assert.Equal(true, wrapper.IsMaterialized);
            Assert.Equal(1, values.Length);
            Assert.Equal(new BsonInt32(1), values[0]);
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
