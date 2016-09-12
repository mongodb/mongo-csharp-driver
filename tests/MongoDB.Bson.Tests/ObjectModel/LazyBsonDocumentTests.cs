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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Bson.Tests
{
    public class LazyBsonDocumentTests
    {
        [Fact]
        public void TestAddBsonElement()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.Add(new BsonElement("z", 3));
                Assert.Equal(3, lazyBsonDocument["z"].AsInt32);
            }
        }

        [Fact]
        public void TestAddBsonElementsArray()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
#pragma warning disable 618
                lazyBsonDocument.Add(new[] { new BsonElement("z", 3), new BsonElement("q", 4) });
#pragma warning restore
                Assert.Equal(3, lazyBsonDocument["z"].AsInt32);
                Assert.Equal(4, lazyBsonDocument["q"].AsInt32);
            }
        }

        [Fact]
        public void TestAddBsonElementsIEnumerable()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
#pragma warning disable 618
                lazyBsonDocument.Add((IEnumerable<BsonElement>)new[] { new BsonElement("z", 3), new BsonElement("q", 4) });
#pragma warning restore
                Assert.Equal(3, lazyBsonDocument["z"].AsInt32);
                Assert.Equal(4, lazyBsonDocument["q"].AsInt32);
            }
        }

        [Fact]
        public void TestAddDictionary()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
#pragma warning disable 618
                lazyBsonDocument.Add(new Dictionary<string, object> { { "z", 3 } });
#pragma warning restore
                Assert.Equal(3, lazyBsonDocument["z"].AsInt32);
            }
        }

        [Fact]
        public void TestAddDictionaryWithKeys()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
#pragma warning disable 618
                lazyBsonDocument.Add(new Dictionary<string, object> { { "z", 3 }, { "q", 4 } }, new[] { "z" });
#pragma warning restore
                Assert.Equal(3, lazyBsonDocument["z"].AsInt32);
                Assert.False(lazyBsonDocument.Contains("q"));
            }
        }

        [Fact]
        public void TestAddGenericIDictionary()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
#pragma warning disable 618
                lazyBsonDocument.Add((IDictionary<string, object>)new Dictionary<string, object> { { "z", 3 } });
#pragma warning restore
                Assert.Equal(3, lazyBsonDocument["z"].AsInt32);
            }
        }

        [Fact]
        public void TestAddGenericIDictionaryWithKeys()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
#pragma warning disable 618
                lazyBsonDocument.Add((IDictionary<string, object>)new Dictionary<string, object> { { "z", 3 }, { "q", 4 } }, new[] { "z" });
#pragma warning restore
                Assert.Equal(3, lazyBsonDocument["z"].AsInt32);
                Assert.False(lazyBsonDocument.Contains("q"));
            }
        }

        [Fact]
        public void TestAddIDictionary()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
#pragma warning disable 618
                lazyBsonDocument.Add((IDictionary)new Dictionary<string, object> { { "z", 3 } });
#pragma warning restore
                Assert.Equal(3, lazyBsonDocument["z"].AsInt32);
            }
        }

        [Fact]
        public void TestAddIDictionaryWithKeys()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
#pragma warning disable 618
                lazyBsonDocument.Add((IDictionary)new Dictionary<string, object> { { "z", 3 }, { "q", 4 } }, new[] { "z" });
#pragma warning restore
                Assert.Equal(3, lazyBsonDocument["z"].AsInt32);
                Assert.False(lazyBsonDocument.Contains("q"));
            }
        }

        [Fact]
        public void TestAddNameValue()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.Add("z", 3);
                Assert.Equal(3, lazyBsonDocument["z"].AsInt32);
            }
        }

        [Fact]
        public void TestAddNameValueWithCondition()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.Add("z", 3, true);
                lazyBsonDocument.Add("q", 4, false);
                Assert.Equal(3, lazyBsonDocument["z"].AsInt32);
                Assert.False(lazyBsonDocument.Contains("q"));
            }
        }

        [Fact]
        public void TestAddRangeDictionary()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.AddRange(new Dictionary<string, object> { { "z", 3 } });
                Assert.Equal(3, lazyBsonDocument["z"].AsInt32);
            }
        }

        [Fact]
        public void TestAddRangeElements()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.AddRange(new[] { new BsonElement("z", 3) });
                Assert.Equal(3, lazyBsonDocument["z"].AsInt32);
            }
        }

        [Fact]
        public void TestAddRangeIDictionary()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.AddRange((IDictionary)new Dictionary<string, object> { { "z", 3 } });
                Assert.Equal(3, lazyBsonDocument["z"].AsInt32);
            }
        }

        [Fact]
        public void TestAddRangeKeyValuePairs()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.AddRange((IEnumerable<KeyValuePair<string, object>>)new Dictionary<string, object> { { "z", 3 } });
                Assert.Equal(3, lazyBsonDocument["z"].AsInt32);
            }
        }

        [Fact]
        public void TestClear()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.Clear();
                Assert.Equal(0, lazyBsonDocument.ElementCount);
            }
        }

        [Fact]
        public void TestClone()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                // the first time Clone will return a LazyBsonDocument, the second a BsonDocument
                using (var clone1 = (IDisposable)lazyBsonDocument.Clone())
                {
                    Assert.IsType<LazyBsonDocument>(clone1);
                    Assert.Equal(lazyBsonDocument, clone1);
                }

                var clone2 = lazyBsonDocument.Clone();
                Assert.IsType<BsonDocument>(clone2);
                Assert.StrictEqual(lazyBsonDocument, clone2);
            }
        }

        [Fact]
        public void TestCompareToBsonDocument()
        {
            var bsonDocument = new BsonDocument { { "a", 1 }, { "b", 2 } };
            var bson = bsonDocument.ToBson();

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                Assert.Equal(0, lazyBsonDocument.CompareTo(bsonDocument));
            }

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var clone = (BsonDocument)bsonDocument.Clone();
                clone["a"] = 0;
                Assert.Equal(1, lazyBsonDocument.CompareTo(clone));
            }

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var clone = (BsonDocument)bsonDocument.Clone();
                clone["a"] = 2;
                Assert.Equal(-1, lazyBsonDocument.CompareTo(clone));
            }

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var clone = (BsonDocument)bsonDocument.Clone();
                clone.SetElement(0, new BsonElement("c", 1)); // "a" < "c" when comparing names
                Assert.Equal(-1, lazyBsonDocument.CompareTo(clone));
            }

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var clone = (BsonDocument)bsonDocument.Clone();
                clone.Remove("b");
                Assert.Equal(1, lazyBsonDocument.CompareTo(clone));
            }

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var clone = (BsonDocument)bsonDocument.Clone();
                clone["c"] = 3;
                Assert.Equal(-1, lazyBsonDocument.CompareTo(clone));
            }
        }

        [Fact]
        public void TestCompareToBsonValue()
        {
            var bsonDocument = new BsonDocument { { "a", 1 }, { "b", 2 } };
            var bson = bsonDocument.ToBson();

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                Assert.Equal(0, lazyBsonDocument.CompareTo((BsonValue)bsonDocument));
            }

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var clone = (BsonDocument)bsonDocument.Clone();
                clone["a"] = 0;
                Assert.Equal(1, lazyBsonDocument.CompareTo((BsonValue)clone));
            }

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var clone = (BsonDocument)bsonDocument.Clone();
                clone["a"] = 2;
                Assert.Equal(-1, lazyBsonDocument.CompareTo((BsonValue)clone));
            }

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var clone = (BsonDocument)bsonDocument.Clone();
                clone.SetElement(0, new BsonElement("c", 1)); // "a" < "c" when comparing names
                Assert.Equal(-1, lazyBsonDocument.CompareTo((BsonValue)clone));
            }

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var clone = (BsonDocument)bsonDocument.Clone();
                clone.Remove("b");
                Assert.Equal(1, lazyBsonDocument.CompareTo((BsonValue)clone));
            }

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var clone = (BsonDocument)bsonDocument.Clone();
                clone["c"] = 3;
                Assert.Equal(-1, lazyBsonDocument.CompareTo((BsonValue)clone));
            }
        }

        [Fact]
        public void TestContains()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                Assert.Equal(true, lazyBsonDocument.Contains("x"));
                Assert.Equal(false, lazyBsonDocument.Contains("z"));
            }
        }

        [Fact]
        public void TestContainsValue()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                Assert.Equal(true, lazyBsonDocument.ContainsValue(1));
                Assert.Equal(false, lazyBsonDocument.ContainsValue(3));
            }
        }

        [Fact]
        public void TestDeepClone()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                // the first time DeepClone will return a LazyBsonDocument, the second a BsonDocument
                using (var clone1 = (IDisposable)lazyBsonDocument.DeepClone())
                {
                    Assert.IsType<LazyBsonDocument>(clone1);
                    Assert.Equal(lazyBsonDocument, clone1);
                }

                var clone2 = lazyBsonDocument.DeepClone();
                Assert.IsType<BsonDocument>(clone2);
                Assert.StrictEqual(lazyBsonDocument, clone2);
            }
        }

        [Fact]
        public void TestElementCount()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var count = lazyBsonDocument.ElementCount;
                Assert.Equal(2, count);
            }
        }

        [Fact]
        public void TestElements()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var elements = lazyBsonDocument.Elements.ToArray();
                Assert.Equal(2, elements.Length);
                Assert.Equal("x", elements[0].Name);
                Assert.Equal(1, elements[0].Value.AsInt32);
                Assert.Equal("y", elements[1].Name);
                Assert.Equal(2, elements[1].Value.AsInt32);
            }
        }

        [Fact]
        public void TestGetElementByIndex()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var elements = new[] { lazyBsonDocument.GetElement(0), lazyBsonDocument.GetElement(1) };
                Assert.Equal("x", elements[0].Name);
                Assert.Equal(1, elements[0].Value.AsInt32);
                Assert.Equal("y", elements[1].Name);
                Assert.Equal(2, elements[1].Value.AsInt32);

                Assert.Throws<ArgumentOutOfRangeException>(() => { lazyBsonDocument.GetElement(2); });
            }
        }

        [Fact]
        public void TestGetElementByName()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var elements = new[] { lazyBsonDocument.GetElement("x"), lazyBsonDocument.GetElement("y") };
                Assert.Equal("x", elements[0].Name);
                Assert.Equal(1, elements[0].Value.AsInt32);
                Assert.Equal("y", elements[1].Name);
                Assert.Equal(2, elements[1].Value.AsInt32);

                Assert.Throws<KeyNotFoundException>(() => { lazyBsonDocument.GetElement("z"); });
            }
        }

        [Fact]
        public void TestGetHashcode()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                Assert.Equal(bsonDocument.GetHashCode(), lazyBsonDocument.GetHashCode());
            }
        }

        [Fact]
        public void TestGetValueByIndex()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var values = new[] { lazyBsonDocument.GetValue(0), lazyBsonDocument.GetValue(1) };
                Assert.Equal(1, values[0].AsInt32);
                Assert.Equal(2, values[1].AsInt32);

                Assert.Throws<ArgumentOutOfRangeException>(() => { lazyBsonDocument.GetValue(2); });
            }
        }

        [Fact]
        public void TestGetValueByName()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var values = new[] { lazyBsonDocument.GetValue("x"), lazyBsonDocument.GetValue("y") };
                Assert.Equal(1, values[0].AsInt32);
                Assert.Equal(2, values[1].AsInt32);

                Assert.Throws<KeyNotFoundException>(() => { lazyBsonDocument.GetValue("z"); });
            }
        }

        [Fact]
        public void TestGetValueByNameWithDefaultValue()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                Assert.Equal(1, lazyBsonDocument.GetValue("x", 3).AsInt32);
                Assert.Equal(3, lazyBsonDocument.GetValue("z", 3).AsInt32);
            }
        }

        [Fact]
        public void TestIndexer()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument[0] = 3;
                Assert.Equal(3, lazyBsonDocument[0].AsInt32);
                Assert.Equal(2, lazyBsonDocument[1].AsInt32);

                Assert.Throws<ArgumentOutOfRangeException>(() => { lazyBsonDocument[2] = 3; });
            }
        }

        [Fact]
        public void TestIndexerByName()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument["z"] = 3;
                Assert.Equal(1, lazyBsonDocument["x"].AsInt32);
                Assert.Equal(2, lazyBsonDocument["y"].AsInt32);
                Assert.Equal(3, lazyBsonDocument["z"].AsInt32);
            }
        }

        [Fact]
        public void TestIndexerByNameWithDefaultValue()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument["z"] = 3;
#pragma warning disable 618
                Assert.Equal(1, lazyBsonDocument["x", 4].AsInt32);
                Assert.Equal(2, lazyBsonDocument["y", 4].AsInt32);
                Assert.Equal(3, lazyBsonDocument["z", 4].AsInt32);
                Assert.Equal(4, lazyBsonDocument["q", 4].AsInt32);
#pragma warning restore
            }
        }

        [Fact]
        public void TestInsertAt()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.InsertAt(0, new BsonElement("z", 3));
                var firstElement = lazyBsonDocument.GetElement(0);
                Assert.Equal("z", firstElement.Name);
                Assert.Equal(3, firstElement.Value.AsInt32);
            }
        }

        [Fact]
        public void TestMerge()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.Merge(new BsonDocument { { "y", 3 }, { "z", 3 } });
                Assert.Equal(2, lazyBsonDocument["y"].AsInt32);
                Assert.Equal(3, lazyBsonDocument["z"].AsInt32);
            }
        }

        [Fact]
        public void TestMergeWithOverWriteFalse()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.Merge(new BsonDocument { { "y", 3 }, { "z", 3 } }, false);
                Assert.Equal(2, lazyBsonDocument["y"].AsInt32);
                Assert.Equal(3, lazyBsonDocument["z"].AsInt32);
            }
        }

        [Fact]
        public void TestMergeWithOverWriteTrue()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.Merge(new BsonDocument { { "y", 3 }, { "z", 3 } }, true);
                Assert.Equal(3, lazyBsonDocument["y"].AsInt32);
                Assert.Equal(3, lazyBsonDocument["z"].AsInt32);
            }
        }

        [Fact]
        public void TestNames()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var names = lazyBsonDocument.Names.ToArray();
                Assert.Equal(2, names.Length);
                Assert.Equal("x", names[0]);
                Assert.Equal("y", names[1]);
            }
        }

        [Fact]
        public void TestNestedLazyBsonArray()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "a", new BsonArray { 1, 2 } } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var nestedLazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                var nestedValues = nestedLazyBsonArray.Values.ToArray();
                Assert.Equal(1, lazyBsonDocument["x"].AsInt32);
                Assert.Equal(2, nestedValues.Length);
                Assert.Equal(1, nestedValues[0].AsInt32);
                Assert.Equal(2, nestedValues[1].AsInt32);
            }
        }

        [Fact]
        public void TestNestedLazyBsonDocument()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "d", new BsonDocument { { "x", 1 }, { "y", 2 } } } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var nestedLazyBsonDocument = lazyBsonDocument["d"].AsBsonDocument;
                var nestedElements = nestedLazyBsonDocument.Elements.ToArray();
                Assert.Equal(1, lazyBsonDocument["x"].AsInt32);
                Assert.Equal(2, nestedElements.Length);
                Assert.Equal("x", nestedElements[0].Name);
                Assert.Equal(1, nestedElements[0].Value.AsInt32);
                Assert.Equal("y", nestedElements[1].Name);
                Assert.Equal(2, nestedElements[1].Value.AsInt32);
            }
        }

        [Fact]
        public void TestRawValues()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
#pragma warning disable 618
                var lazyValues = lazyBsonDocument.RawValues.ToArray();
#pragma warning restore
                Assert.Equal(2, lazyValues.Length);
                Assert.Equal(1, lazyValues[0]);
                Assert.Equal(2, lazyValues[1]);
            }
        }

        [Fact]
        public void TestRemove()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.Remove("y");
                Assert.Equal(1, lazyBsonDocument.ElementCount);
                Assert.True(lazyBsonDocument.Contains("x"));
                Assert.False(lazyBsonDocument.Contains("y"));
            }
        }

        [Fact]
        public void TestRemoveAt()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.RemoveAt(0);
                Assert.Equal(1, lazyBsonDocument.ElementCount);
                Assert.False(lazyBsonDocument.Contains("x"));
                Assert.True(lazyBsonDocument.Contains("y"));
            }
        }

        [Fact]
        public void TestRemoveElement()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.RemoveElement(new BsonElement("x", 1));
                Assert.Equal(1, lazyBsonDocument.ElementCount);
                Assert.False(lazyBsonDocument.Contains("x"));
                Assert.True(lazyBsonDocument.Contains("y"));
            }
        }

        [Fact]
        public void TestSetByIndex()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.Set(0, 3);
                Assert.Equal(2, lazyBsonDocument.ElementCount);
                Assert.Equal(3, lazyBsonDocument[0].AsInt32);
            }
        }

        [Fact]
        public void TestSetByNameExisting()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.Set("x", 3);
                Assert.Equal(2, lazyBsonDocument.ElementCount);
                Assert.Equal(3, lazyBsonDocument[0].AsInt32);
            }
        }

        [Fact]
        public void TestSetByNameNew()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.Set("z", 3);
                Assert.Equal(3, lazyBsonDocument.ElementCount);
                Assert.Equal("z", lazyBsonDocument.GetElement(2).Name);
                Assert.Equal(3, lazyBsonDocument[2].AsInt32);
            }
        }

        [Fact]
        public void TestSetElementByIndex()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.SetElement(0, new BsonElement("x", 3));
                Assert.Equal(2, lazyBsonDocument.ElementCount);
                Assert.Equal(3, lazyBsonDocument[0].AsInt32);
            }
        }

        [Fact]
        public void TestSetElementExisting()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.SetElement(new BsonElement("x", 3));
                Assert.Equal(2, lazyBsonDocument.ElementCount);
                Assert.Equal(3, lazyBsonDocument[0].AsInt32);
            }
        }

        [Fact]
        public void TestSetElementNew()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.SetElement(new BsonElement("z", 3));
                Assert.Equal(3, lazyBsonDocument.ElementCount);
                Assert.Equal("z", lazyBsonDocument.GetElement(2).Name);
                Assert.Equal(3, lazyBsonDocument[2].AsInt32);
            }
        }

        [Fact]
        public void TestSetByName()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                lazyBsonDocument.Set("x", 3);
                Assert.Equal(2, lazyBsonDocument.ElementCount);
                Assert.Equal(3, lazyBsonDocument[0].AsInt32);
            }
        }

        [Fact]
        public void TestTryGetElement()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                BsonElement element;
                Assert.True(lazyBsonDocument.TryGetElement("x", out element));
                Assert.Equal("x", element.Name);
                Assert.Equal(1, element.Value.AsInt32);
            }
        }

        [Fact]
        public void TestTryGetValue()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                BsonValue value;
                Assert.True(lazyBsonDocument.TryGetValue("x", out value));
                Assert.Equal(1, value.AsInt32);
            }
        }

        [Fact]
        public void TestValues()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyValues = lazyBsonDocument.Values.ToArray();
                Assert.Equal(2, lazyValues.Length);
                Assert.Equal(1, lazyValues[0].AsInt32);
                Assert.Equal(2, lazyValues[1].AsInt32);
            }
        }

        [SkippableFact]
        public void TestLargeDocumentDeserialization()
        {
            RequireProcess.Check().Bits(64);

            var bsonDocument = new BsonDocument { { "stringfield", "A" } };
            var noOfDoubleFields = 200000;
            for (var i = 0; i < noOfDoubleFields; i++)
            {
                bsonDocument.Add("doublefield_"+i, i*1.0);
            }
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                Assert.Equal(noOfDoubleFields + 1, lazyBsonDocument.ElementCount);
            }
        }
    }
}
