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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests
{
    [TestFixture]
    public class LazyBsonDocumentTests
    {
        [Test]
        public void TestAddBsonElement()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.Add(new BsonElement("z", 3));
                Assert.AreEqual(3, lazyBsonDocument["z"].AsInt32);
            }
        }

        [Test]
        public void TestAddBsonElementsArray()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
#pragma warning disable 618
                lazyBsonDocument.Add(new[] { new BsonElement("z", 3), new BsonElement("q", 4) });
#pragma warning restore
                Assert.AreEqual(3, lazyBsonDocument["z"].AsInt32);
                Assert.AreEqual(4, lazyBsonDocument["q"].AsInt32);
            }
        }

        [Test]
        public void TestAddBsonElementsIEnumerable()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
#pragma warning disable 618
                lazyBsonDocument.Add((IEnumerable<BsonElement>)new[] { new BsonElement("z", 3), new BsonElement("q", 4) });
#pragma warning restore
                Assert.AreEqual(3, lazyBsonDocument["z"].AsInt32);
                Assert.AreEqual(4, lazyBsonDocument["q"].AsInt32);
            }
        }

        [Test]
        public void TestAddDictionary()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
#pragma warning disable 618
                lazyBsonDocument.Add(new Dictionary<string, object> { { "z", 3 } });
#pragma warning restore
                Assert.AreEqual(3, lazyBsonDocument["z"].AsInt32);
            }
        }

        [Test]
        public void TestAddDictionaryWithKeys()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
#pragma warning disable 618
                lazyBsonDocument.Add(new Dictionary<string, object> { { "z", 3 }, { "q", 4 } }, new[] { "z" });
#pragma warning restore
                Assert.AreEqual(3, lazyBsonDocument["z"].AsInt32);
                Assert.IsFalse(lazyBsonDocument.Contains("q"));
            }
        }

        [Test]
        public void TestAddGenericIDictionary()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
#pragma warning disable 618
                lazyBsonDocument.Add((IDictionary<string, object>)new Dictionary<string, object> { { "z", 3 } });
#pragma warning restore
                Assert.AreEqual(3, lazyBsonDocument["z"].AsInt32);
            }
        }

        [Test]
        public void TestAddGenericIDictionaryWithKeys()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
#pragma warning disable 618
                lazyBsonDocument.Add((IDictionary<string, object>)new Dictionary<string, object> { { "z", 3 }, { "q", 4 } }, new[] { "z" });
#pragma warning restore
                Assert.AreEqual(3, lazyBsonDocument["z"].AsInt32);
                Assert.IsFalse(lazyBsonDocument.Contains("q"));
            }
        }

        [Test]
        public void TestAddIDictionary()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
#pragma warning disable 618
                lazyBsonDocument.Add((IDictionary)new Dictionary<string, object> { { "z", 3 } });
#pragma warning restore
                Assert.AreEqual(3, lazyBsonDocument["z"].AsInt32);
            }
        }

        [Test]
        public void TestAddIDictionaryWithKeys()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
#pragma warning disable 618
                lazyBsonDocument.Add((IDictionary)new Dictionary<string, object> { { "z", 3 }, { "q", 4 } }, new[] { "z" });
#pragma warning restore
                Assert.AreEqual(3, lazyBsonDocument["z"].AsInt32);
                Assert.IsFalse(lazyBsonDocument.Contains("q"));
            }
        }

        [Test]
        public void TestAddNameValue()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.Add("z", 3);
                Assert.AreEqual(3, lazyBsonDocument["z"].AsInt32);
            }
        }

        [Test]
        public void TestAddNameValueWithCondition()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.Add("z", 3, true);
                lazyBsonDocument.Add("q", 4, false);
                Assert.AreEqual(3, lazyBsonDocument["z"].AsInt32);
                Assert.IsFalse(lazyBsonDocument.Contains("q"));
            }
        }

        [Test]
        public void TestAddRangeDictionary()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.AddRange(new Dictionary<string, object> { { "z", 3 } });
                Assert.AreEqual(3, lazyBsonDocument["z"].AsInt32);
            }
        }

        [Test]
        public void TestAddRangeElements()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.AddRange(new[] { new BsonElement("z", 3) });
                Assert.AreEqual(3, lazyBsonDocument["z"].AsInt32);
            }
        }

        [Test]
        public void TestAddRangeIDictionary()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.AddRange((IDictionary)new Dictionary<string, object> { { "z", 3 } });
                Assert.AreEqual(3, lazyBsonDocument["z"].AsInt32);
            }
        }

        [Test]
        public void TestAddRangeKeyValuePairs()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.AddRange((IEnumerable<KeyValuePair<string, object>>)new Dictionary<string, object> { { "z", 3 } });
                Assert.AreEqual(3, lazyBsonDocument["z"].AsInt32);
            }
        }

        [Test]
        public void TestClear()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.Clear();
                Assert.AreEqual(0, lazyBsonDocument.ElementCount);
            }
        }

        [Test]
        public void TestClone()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                // the first time Clone will return a LazyBsonDocument, the second a BsonDocument
                using (var clone1 = (IDisposable)lazyBsonDocument.Clone())
                {
                    Assert.IsInstanceOf<LazyBsonDocument>(clone1);
                    Assert.AreEqual(lazyBsonDocument, clone1);
                }

                var clone2 = lazyBsonDocument.Clone();
                Assert.IsInstanceOf<BsonDocument>(clone2);
                Assert.AreEqual(lazyBsonDocument, clone2);
            }
        }

        [Test]
        public void TestCompareToBsonDocument()
        {
            var bsonDocument = new BsonDocument { { "a", 1 }, { "b", 2 } };
            var bson = bsonDocument.ToBson();

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                Assert.AreEqual(0, lazyBsonDocument.CompareTo(bsonDocument));
            }

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var clone = (BsonDocument)bsonDocument.Clone();
                clone["a"] = 0;
                Assert.AreEqual(1, lazyBsonDocument.CompareTo(clone));
            }

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var clone = (BsonDocument)bsonDocument.Clone();
                clone["a"] = 2;
                Assert.AreEqual(-1, lazyBsonDocument.CompareTo(clone));
            }

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var clone = (BsonDocument)bsonDocument.Clone();
                clone.SetElement(0, new BsonElement("c", 1)); // "a" < "c" when comparing names
                Assert.AreEqual(-1, lazyBsonDocument.CompareTo(clone));
            }

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var clone = (BsonDocument)bsonDocument.Clone();
                clone.Remove("b");
                Assert.AreEqual(1, lazyBsonDocument.CompareTo(clone));
            }

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var clone = (BsonDocument)bsonDocument.Clone();
                clone["c"] = 3;
                Assert.AreEqual(-1, lazyBsonDocument.CompareTo(clone));
            }
        }

        [Test]
        public void TestCompareToBsonValue()
        {
            var bsonDocument = new BsonDocument { { "a", 1 }, { "b", 2 } };
            var bson = bsonDocument.ToBson();

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                Assert.AreEqual(0, lazyBsonDocument.CompareTo((BsonValue)bsonDocument));
            }

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var clone = (BsonDocument)bsonDocument.Clone();
                clone["a"] = 0;
                Assert.AreEqual(1, lazyBsonDocument.CompareTo((BsonValue)clone));
            }

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var clone = (BsonDocument)bsonDocument.Clone();
                clone["a"] = 2;
                Assert.AreEqual(-1, lazyBsonDocument.CompareTo((BsonValue)clone));
            }

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var clone = (BsonDocument)bsonDocument.Clone();
                clone.SetElement(0, new BsonElement("c", 1)); // "a" < "c" when comparing names
                Assert.AreEqual(-1, lazyBsonDocument.CompareTo((BsonValue)clone));
            }

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var clone = (BsonDocument)bsonDocument.Clone();
                clone.Remove("b");
                Assert.AreEqual(1, lazyBsonDocument.CompareTo((BsonValue)clone));
            }

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var clone = (BsonDocument)bsonDocument.Clone();
                clone["c"] = 3;
                Assert.AreEqual(-1, lazyBsonDocument.CompareTo((BsonValue)clone));
            }
        }

        [Test]
        public void TestContains()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                Assert.AreEqual(true, lazyBsonDocument.Contains("x"));
                Assert.AreEqual(false, lazyBsonDocument.Contains("z"));
            }
        }

        [Test]
        public void TestContainsValue()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                Assert.AreEqual(true, lazyBsonDocument.ContainsValue(1));
                Assert.AreEqual(false, lazyBsonDocument.ContainsValue(3));
            }
        }

        [Test]
        public void TestDeepClone()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                // the first time DeepClone will return a LazyBsonDocument, the second a BsonDocument
                using (var clone1 = (IDisposable)lazyBsonDocument.DeepClone())
                {
                    Assert.IsInstanceOf<LazyBsonDocument>(clone1);
                    Assert.AreEqual(lazyBsonDocument, clone1);
                }

                var clone2 = lazyBsonDocument.DeepClone();
                Assert.IsInstanceOf<BsonDocument>(clone2);
                Assert.AreEqual(lazyBsonDocument, clone2);
            }
        }

        [Test]
        public void TestElementCount()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var count = lazyBsonDocument.ElementCount;
                Assert.AreEqual(2, count);
            }
        }

        [Test]
        public void TestElements()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var elements = lazyBsonDocument.Elements.ToArray();
                Assert.AreEqual(2, elements.Length);
                Assert.AreEqual("x", elements[0].Name);
                Assert.AreEqual(1, elements[0].Value.AsInt32);
                Assert.AreEqual("y", elements[1].Name);
                Assert.AreEqual(2, elements[1].Value.AsInt32);
            }
        }

        [Test]
        public void TestGetElementByIndex()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var elements = new[] { lazyBsonDocument.GetElement(0), lazyBsonDocument.GetElement(1) };
                Assert.AreEqual("x", elements[0].Name);
                Assert.AreEqual(1, elements[0].Value.AsInt32);
                Assert.AreEqual("y", elements[1].Name);
                Assert.AreEqual(2, elements[1].Value.AsInt32);

                Assert.Throws<ArgumentOutOfRangeException>(() => { lazyBsonDocument.GetElement(2); });
            }
        }

        [Test]
        public void TestGetElementByName()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var elements = new[] { lazyBsonDocument.GetElement("x"), lazyBsonDocument.GetElement("y") };
                Assert.AreEqual("x", elements[0].Name);
                Assert.AreEqual(1, elements[0].Value.AsInt32);
                Assert.AreEqual("y", elements[1].Name);
                Assert.AreEqual(2, elements[1].Value.AsInt32);

                Assert.Throws<KeyNotFoundException>(() => { lazyBsonDocument.GetElement("z"); });
            }
        }

        [Test]
        public void TestGetHashcode()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                Assert.AreEqual(bsonDocument.GetHashCode(), lazyBsonDocument.GetHashCode());
            }
        }

        [Test]
        public void TestGetValueByIndex()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var values = new[] { lazyBsonDocument.GetValue(0), lazyBsonDocument.GetValue(1) };
                Assert.AreEqual(1, values[0].AsInt32);
                Assert.AreEqual(2, values[1].AsInt32);

                Assert.Throws<ArgumentOutOfRangeException>(() => { lazyBsonDocument.GetValue(2); });
            }
        }

        [Test]
        public void TestGetValueByName()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var values = new[] { lazyBsonDocument.GetValue("x"), lazyBsonDocument.GetValue("y") };
                Assert.AreEqual(1, values[0].AsInt32);
                Assert.AreEqual(2, values[1].AsInt32);

                Assert.Throws<KeyNotFoundException>(() => { lazyBsonDocument.GetValue("z"); });
            }
        }

        [Test]
        public void TestGetValueByNameWithDefaultValue()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                Assert.AreEqual(1, lazyBsonDocument.GetValue("x", 3).AsInt32);
                Assert.AreEqual(3, lazyBsonDocument.GetValue("z", 3).AsInt32);
            }
        }

        [Test]
        public void TestIndexer()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument[0] = 3;
                Assert.AreEqual(3, lazyBsonDocument[0].AsInt32);
                Assert.AreEqual(2, lazyBsonDocument[1].AsInt32);

                Assert.Throws<ArgumentOutOfRangeException>(() => { lazyBsonDocument[2] = 3; });
            }
        }

        [Test]
        public void TestIndexerByName()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument["z"] = 3;
                Assert.AreEqual(1, lazyBsonDocument["x"].AsInt32);
                Assert.AreEqual(2, lazyBsonDocument["y"].AsInt32);
                Assert.AreEqual(3, lazyBsonDocument["z"].AsInt32);
            }
        }

        [Test]
        public void TestIndexerByNameWithDefaultValue()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument["z"] = 3;
#pragma warning disable 618
                Assert.AreEqual(1, lazyBsonDocument["x", 4].AsInt32);
                Assert.AreEqual(2, lazyBsonDocument["y", 4].AsInt32);
                Assert.AreEqual(3, lazyBsonDocument["z", 4].AsInt32);
                Assert.AreEqual(4, lazyBsonDocument["q", 4].AsInt32);
#pragma warning restore
            }
        }

        [Test]
        public void TestInsertAt()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.InsertAt(0, new BsonElement("z", 3));
                var firstElement = lazyBsonDocument.GetElement(0);
                Assert.AreEqual("z", firstElement.Name);
                Assert.AreEqual(3, firstElement.Value.AsInt32);
            }
        }

        [Test]
        public void TestMerge()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.Merge(new BsonDocument { { "y", 3 }, { "z", 3 } });
                Assert.AreEqual(2, lazyBsonDocument["y"].AsInt32);
                Assert.AreEqual(3, lazyBsonDocument["z"].AsInt32);
            }
        }

        [Test]
        public void TestMergeWithOverWriteFalse()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.Merge(new BsonDocument { { "y", 3 }, { "z", 3 } }, false);
                Assert.AreEqual(2, lazyBsonDocument["y"].AsInt32);
                Assert.AreEqual(3, lazyBsonDocument["z"].AsInt32);
            }
        }

        [Test]
        public void TestMergeWithOverWriteTrue()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.Merge(new BsonDocument { { "y", 3 }, { "z", 3 } }, true);
                Assert.AreEqual(3, lazyBsonDocument["y"].AsInt32);
                Assert.AreEqual(3, lazyBsonDocument["z"].AsInt32);
            }
        }

        [Test]
        public void TestNames()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var names = lazyBsonDocument.Names.ToArray();
                Assert.AreEqual(2, names.Length);
                Assert.AreEqual("x", names[0]);
                Assert.AreEqual("y", names[1]);
            }
        }

        [Test]
        public void TestNestedLazyBsonArray()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "a", new BsonArray { 1, 2 } } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var nestedLazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                var nestedValues = nestedLazyBsonArray.Values.ToArray();
                Assert.AreEqual(1, lazyBsonDocument["x"].AsInt32);
                Assert.AreEqual(2, nestedValues.Length);
                Assert.AreEqual(1, nestedValues[0].AsInt32);
                Assert.AreEqual(2, nestedValues[1].AsInt32);
            }
        }

        [Test]
        public void TestNestedLazyBsonDocument()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "d", new BsonDocument { { "x", 1 }, { "y", 2 } } } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var nestedLazyBsonDocument = lazyBsonDocument["d"].AsBsonDocument;
                var nestedElements = nestedLazyBsonDocument.Elements.ToArray();
                Assert.AreEqual(1, lazyBsonDocument["x"].AsInt32);
                Assert.AreEqual(2, nestedElements.Length);
                Assert.AreEqual("x", nestedElements[0].Name);
                Assert.AreEqual(1, nestedElements[0].Value.AsInt32);
                Assert.AreEqual("y", nestedElements[1].Name);
                Assert.AreEqual(2, nestedElements[1].Value.AsInt32);
            }
        }

        [Test]
        public void TestRawValues()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
#pragma warning disable 618
                var lazyValues = lazyBsonDocument.RawValues.ToArray();
#pragma warning restore
                Assert.AreEqual(2, lazyValues.Length);
                Assert.AreEqual(1, lazyValues[0]);
                Assert.AreEqual(2, lazyValues[1]);
            }
        }

        [Test]
        public void TestRemove()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.Remove("y");
                Assert.AreEqual(1, lazyBsonDocument.ElementCount);
                Assert.IsTrue(lazyBsonDocument.Contains("x"));
                Assert.IsFalse(lazyBsonDocument.Contains("y"));
            }
        }

        [Test]
        public void TestRemoveAt()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.RemoveAt(0);
                Assert.AreEqual(1, lazyBsonDocument.ElementCount);
                Assert.IsFalse(lazyBsonDocument.Contains("x"));
                Assert.IsTrue(lazyBsonDocument.Contains("y"));
            }
        }

        [Test]
        public void TestRemoveElement()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.RemoveElement(new BsonElement("x", 1));
                Assert.AreEqual(1, lazyBsonDocument.ElementCount);
                Assert.IsFalse(lazyBsonDocument.Contains("x"));
                Assert.IsTrue(lazyBsonDocument.Contains("y"));
            }
        }

        [Test]
        public void TestSetByIndex()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.Set(0, 3);
                Assert.AreEqual(2, lazyBsonDocument.ElementCount);
                Assert.AreEqual(3, lazyBsonDocument[0].AsInt32);
            }
        }

        [Test]
        public void TestSetByNameExisting()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.Set("x", 3);
                Assert.AreEqual(2, lazyBsonDocument.ElementCount);
                Assert.AreEqual(3, lazyBsonDocument[0].AsInt32);
            }
        }

        [Test]
        public void TestSetByNameNew()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.Set("z", 3);
                Assert.AreEqual(3, lazyBsonDocument.ElementCount);
                Assert.AreEqual("z", lazyBsonDocument.GetElement(2).Name);
                Assert.AreEqual(3, lazyBsonDocument[2].AsInt32);
            }
        }

        [Test]
        public void TestSetElementByIndex()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.SetElement(0, new BsonElement("x", 3));
                Assert.AreEqual(2, lazyBsonDocument.ElementCount);
                Assert.AreEqual(3, lazyBsonDocument[0].AsInt32);
            }
        }

        [Test]
        public void TestSetElementExisting()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.SetElement(new BsonElement("x", 3));
                Assert.AreEqual(2, lazyBsonDocument.ElementCount);
                Assert.AreEqual(3, lazyBsonDocument[0].AsInt32);
            }
        }

        [Test]
        public void TestSetElementNew()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.SetElement(new BsonElement("z", 3));
                Assert.AreEqual(3, lazyBsonDocument.ElementCount);
                Assert.AreEqual("z", lazyBsonDocument.GetElement(2).Name);
                Assert.AreEqual(3, lazyBsonDocument[2].AsInt32);
            }
        }

        [Test]
        public void TestSetByName()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.Set("x", 3);
                Assert.AreEqual(2, lazyBsonDocument.ElementCount);
                Assert.AreEqual(3, lazyBsonDocument[0].AsInt32);
            }
        }

        [Test]
        public void TestTryGetElement()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                BsonElement element;
                Assert.IsTrue(lazyBsonDocument.TryGetElement("x", out element));
                Assert.AreEqual("x", element.Name);
                Assert.AreEqual(1, element.Value.AsInt32);
            }
        }

        [Test]
        public void TestTryGetValue()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                BsonValue value;
                Assert.IsTrue(lazyBsonDocument.TryGetValue("x", out value));
                Assert.AreEqual(1, value.AsInt32);
            }
        }

        [Test]
        public void TestValues()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyValues = lazyBsonDocument.Values.ToArray();
                Assert.AreEqual(2, lazyValues.Length);
                Assert.AreEqual(1, lazyValues[0].AsInt32);
                Assert.AreEqual(2, lazyValues[1].AsInt32);
            }
        }

        [Test]
        public void TestLargeDocumentDeserialization()
        {
            var bsonDocument = new BsonDocument { { "stringfield", "A" } };
            var noOfDoubleFields = 200000;
            for (var i = 0; i < noOfDoubleFields; i++)
            {
                bsonDocument.Add("doublefield_"+i, i*1.0);
            }
            var bson = bsonDocument.ToBson();
            BsonDefaults.MaxDocumentSize = 4 * 1024 * 1024;
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                Assert.AreEqual(noOfDoubleFields + 1, lazyBsonDocument.ElementCount);
            }
        }
    }
}
