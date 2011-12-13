/* Copyright 2010-2011 10gen Inc.
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
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Wrappers;

namespace MongoDB.DriverUnitTests.Jira.CSharp290
{
    [TestFixture]
    public class CSharp290Tests
    {
        private Dictionary<string, object> dictionary = new Dictionary<string, object> { { "type", "Dictionary<string, object>" } };
        private Hashtable hashtable = new Hashtable { { "type", "Hashtable" } };
        private IDictionary idictionaryNonGeneric = new Hashtable() { { "type", "IDictionary" } };
        private IDictionary<string, object> idictionary = new Dictionary<string, object> { { "type", "IDictionary<string, object>" } };

        [Test]
        public void TestBsonDocumentConstructor()
        {
            var document1 = new BsonDocument(dictionary);
            var document2 = new BsonDocument(hashtable);
            var document3 = new BsonDocument(idictionaryNonGeneric);
            var document4 = new BsonDocument(idictionary);

            Assert.AreEqual("Dictionary<string, object>", document1["type"].AsString);
            Assert.AreEqual("Hashtable", document2["type"].AsString);
            Assert.AreEqual("IDictionary", document3["type"].AsString);
            Assert.AreEqual("IDictionary<string, object>", document4["type"].AsString);
        }

        [Test]
        public void TestBsonDocumentAdd()
        {
            var document1 = new BsonDocument().Add(dictionary);
            var document2 = new BsonDocument().Add(hashtable);
            var document3 = new BsonDocument().Add(idictionaryNonGeneric);
            var document4 = new BsonDocument().Add(idictionary);

            Assert.AreEqual("Dictionary<string, object>", document1["type"].AsString);
            Assert.AreEqual("Hashtable", document2["type"].AsString);
            Assert.AreEqual("IDictionary", document3["type"].AsString);
            Assert.AreEqual("IDictionary<string, object>", document4["type"].AsString);
        }

        [Test]
        public void TestBsonTypeMapperMapToBsonValue()
        {
            var document1 = (BsonDocument)BsonTypeMapper.MapToBsonValue(dictionary, BsonType.Document);
            var document2 = (BsonDocument)BsonTypeMapper.MapToBsonValue(hashtable, BsonType.Document);
            var document3 = (BsonDocument)BsonTypeMapper.MapToBsonValue(idictionaryNonGeneric, BsonType.Document);
            var document4 = (BsonDocument)BsonTypeMapper.MapToBsonValue(idictionary, BsonType.Document);

            Assert.AreEqual("Dictionary<string, object>", document1["type"].AsString);
            Assert.AreEqual("Hashtable", document2["type"].AsString);
            Assert.AreEqual("IDictionary", document3["type"].AsString);
            Assert.AreEqual("IDictionary<string, object>", document4["type"].AsString);
        }

        [Test]
        public void TestBsonTypeMapperTryMapToBsonValue()
        {
            BsonValue document1, document2, document3, document4;
            Assert.IsTrue(BsonTypeMapper.TryMapToBsonValue(dictionary, out document1));
            Assert.IsTrue(BsonTypeMapper.TryMapToBsonValue(hashtable, out document2));
            Assert.IsTrue(BsonTypeMapper.TryMapToBsonValue(idictionaryNonGeneric, out document3));
            Assert.IsTrue(BsonTypeMapper.TryMapToBsonValue(idictionary, out document4));

            Assert.AreEqual("Dictionary<string, object>", ((BsonDocument)document1)["type"].AsString);
            Assert.AreEqual("Hashtable", ((BsonDocument)document2)["type"].AsString);
            Assert.AreEqual("IDictionary", ((BsonDocument)document3)["type"].AsString);
            Assert.AreEqual("IDictionary<string, object>", ((BsonDocument)document4)["type"].AsString);
        }

        [Test]
        public void TestCollectionOptionsDocumentConstructor()
        {
            var document1 = new CollectionOptionsDocument(dictionary);
            var document2 = new CollectionOptionsDocument(hashtable);
            var document3 = new CollectionOptionsDocument(idictionaryNonGeneric);
            var document4 = new CollectionOptionsDocument(idictionary);

            Assert.AreEqual("Dictionary<string, object>", document1["type"].AsString);
            Assert.AreEqual("Hashtable", document2["type"].AsString);
            Assert.AreEqual("IDictionary", document3["type"].AsString);
            Assert.AreEqual("IDictionary<string, object>", document4["type"].AsString);
        }

        [Test]
        public void TestCommandDocumentConstructor()
        {
            var document1 = new CommandDocument(dictionary);
            var document2 = new CommandDocument(hashtable);
            var document3 = new CommandDocument(idictionaryNonGeneric);
            var document4 = new CommandDocument(idictionary);

            Assert.AreEqual("Dictionary<string, object>", document1["type"].AsString);
            Assert.AreEqual("Hashtable", document2["type"].AsString);
            Assert.AreEqual("IDictionary", document3["type"].AsString);
            Assert.AreEqual("IDictionary<string, object>", document4["type"].AsString);
        }

        [Test]
        public void TestFieldsDocumentConstructor()
        {
            var document1 = new FieldsDocument(dictionary);
            var document2 = new FieldsDocument(hashtable);
            var document3 = new FieldsDocument(idictionaryNonGeneric);
            var document4 = new FieldsDocument(idictionary);

            Assert.AreEqual("Dictionary<string, object>", document1["type"].AsString);
            Assert.AreEqual("Hashtable", document2["type"].AsString);
            Assert.AreEqual("IDictionary", document3["type"].AsString);
            Assert.AreEqual("IDictionary<string, object>", document4["type"].AsString);
        }

        [Test]
        public void TestGeoNearOptionsDocumentConstructor()
        {
            var document1 = new GeoNearOptionsDocument(dictionary);
            var document2 = new GeoNearOptionsDocument(hashtable);
            var document3 = new GeoNearOptionsDocument(idictionaryNonGeneric);
            var document4 = new GeoNearOptionsDocument(idictionary);

            Assert.AreEqual("Dictionary<string, object>", document1["type"].AsString);
            Assert.AreEqual("Hashtable", document2["type"].AsString);
            Assert.AreEqual("IDictionary", document3["type"].AsString);
            Assert.AreEqual("IDictionary<string, object>", document4["type"].AsString);
        }

        [Test]
        public void TestGroupByDocumentConstructor()
        {
            var document1 = new GroupByDocument(dictionary);
            var document2 = new GroupByDocument(hashtable);
            var document3 = new GroupByDocument(idictionaryNonGeneric);
            var document4 = new GroupByDocument(idictionary);

            Assert.AreEqual("Dictionary<string, object>", document1["type"].AsString);
            Assert.AreEqual("Hashtable", document2["type"].AsString);
            Assert.AreEqual("IDictionary", document3["type"].AsString);
            Assert.AreEqual("IDictionary<string, object>", document4["type"].AsString);
        }

        [Test]
        public void TestIndexKeysDocumentConstructor()
        {
            var document1 = new IndexKeysDocument(dictionary);
            var document2 = new IndexKeysDocument(hashtable);
            var document3 = new IndexKeysDocument(idictionaryNonGeneric);
            var document4 = new IndexKeysDocument(idictionary);

            Assert.AreEqual("Dictionary<string, object>", document1["type"].AsString);
            Assert.AreEqual("Hashtable", document2["type"].AsString);
            Assert.AreEqual("IDictionary", document3["type"].AsString);
            Assert.AreEqual("IDictionary<string, object>", document4["type"].AsString);
        }

        [Test]
        public void TestIndexOptionsDocumentConstructor()
        {
            var document1 = new IndexOptionsDocument(dictionary);
            var document2 = new IndexOptionsDocument(hashtable);
            var document3 = new IndexOptionsDocument(idictionaryNonGeneric);
            var document4 = new IndexOptionsDocument(idictionary);

            Assert.AreEqual("Dictionary<string, object>", document1["type"].AsString);
            Assert.AreEqual("Hashtable", document2["type"].AsString);
            Assert.AreEqual("IDictionary", document3["type"].AsString);
            Assert.AreEqual("IDictionary<string, object>", document4["type"].AsString);
        }

        [Test]
        public void TestMapReduceOptionsDocumentConstructor()
        {
            var document1 = new MapReduceOptionsDocument(dictionary);
            var document2 = new MapReduceOptionsDocument(hashtable);
            var document3 = new MapReduceOptionsDocument(idictionaryNonGeneric);
            var document4 = new MapReduceOptionsDocument(idictionary);

            Assert.AreEqual("Dictionary<string, object>", document1["type"].AsString);
            Assert.AreEqual("Hashtable", document2["type"].AsString);
            Assert.AreEqual("IDictionary", document3["type"].AsString);
            Assert.AreEqual("IDictionary<string, object>", document4["type"].AsString);
        }

        [Test]
        public void TestQueryDocumentConstructor()
        {
            var document1 = new QueryDocument(dictionary);
            var document2 = new QueryDocument(hashtable);
            var document3 = new QueryDocument(idictionaryNonGeneric);
            var document4 = new QueryDocument(idictionary);

            Assert.AreEqual("Dictionary<string, object>", document1["type"].AsString);
            Assert.AreEqual("Hashtable", document2["type"].AsString);
            Assert.AreEqual("IDictionary", document3["type"].AsString);
            Assert.AreEqual("IDictionary<string, object>", document4["type"].AsString);
        }

        [Test]
        public void TestScopeDocumentConstructor()
        {
            var document1 = new ScopeDocument(dictionary);
            var document2 = new ScopeDocument(hashtable);
            var document3 = new ScopeDocument(idictionaryNonGeneric);
            var document4 = new ScopeDocument(idictionary);

            Assert.AreEqual("Dictionary<string, object>", document1["type"].AsString);
            Assert.AreEqual("Hashtable", document2["type"].AsString);
            Assert.AreEqual("IDictionary", document3["type"].AsString);
            Assert.AreEqual("IDictionary<string, object>", document4["type"].AsString);
        }

        [Test]
        public void TestSortByDocumentConstructor()
        {
            var document1 = new SortByDocument(dictionary);
            var document2 = new SortByDocument(hashtable);
            var document3 = new SortByDocument(idictionaryNonGeneric);
            var document4 = new SortByDocument(idictionary);

            Assert.AreEqual("Dictionary<string, object>", document1["type"].AsString);
            Assert.AreEqual("Hashtable", document2["type"].AsString);
            Assert.AreEqual("IDictionary", document3["type"].AsString);
            Assert.AreEqual("IDictionary<string, object>", document4["type"].AsString);
        }

        [Test]
        public void TestUpdateDocumentConstructor()
        {
            var document1 = new UpdateDocument(dictionary);
            var document2 = new UpdateDocument(hashtable);
            var document3 = new UpdateDocument(idictionaryNonGeneric);
            var document4 = new UpdateDocument(idictionary);

            Assert.AreEqual("Dictionary<string, object>", document1["type"].AsString);
            Assert.AreEqual("Hashtable", document2["type"].AsString);
            Assert.AreEqual("IDictionary", document3["type"].AsString);
            Assert.AreEqual("IDictionary<string, object>", document4["type"].AsString);
        }
    }
}
