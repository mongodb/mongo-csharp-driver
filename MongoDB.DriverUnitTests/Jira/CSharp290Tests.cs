﻿/* Copyright 2010-2013 10gen Inc.
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

using System.Collections;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Jira.CSharp290
{
    [TestFixture]
    public class CSharp290Tests
    {
        private Dictionary<string, object> _dictionary = new Dictionary<string, object> { { "type", "Dictionary<string, object>" } };
        private Hashtable _hashtable = new Hashtable { { "type", "Hashtable" } };
        private IDictionary _idictionaryNonGeneric = new Hashtable() { { "type", "IDictionary" } };
        private IDictionary<string, object> _idictionary = new Dictionary<string, object> { { "type", "IDictionary<string, object>" } };

        [Test]
        public void TestBsonDocumentConstructor()
        {
            var document1 = new BsonDocument(_dictionary);
            var document2 = new BsonDocument(_hashtable);
            var document3 = new BsonDocument(_idictionaryNonGeneric);
            var document4 = new BsonDocument(_idictionary);

            Assert.AreEqual("Dictionary<string, object>", document1["type"].AsString);
            Assert.AreEqual("Hashtable", document2["type"].AsString);
            Assert.AreEqual("IDictionary", document3["type"].AsString);
            Assert.AreEqual("IDictionary<string, object>", document4["type"].AsString);
        }

        [Test]
        public void TestBsonDocumentAdd()
        {
            var document1 = new BsonDocument().AddRange(_dictionary);
            var document2 = new BsonDocument().AddRange(_hashtable);
            var document3 = new BsonDocument().AddRange(_idictionaryNonGeneric);
            var document4 = new BsonDocument().AddRange(_idictionary);

            Assert.AreEqual("Dictionary<string, object>", document1["type"].AsString);
            Assert.AreEqual("Hashtable", document2["type"].AsString);
            Assert.AreEqual("IDictionary", document3["type"].AsString);
            Assert.AreEqual("IDictionary<string, object>", document4["type"].AsString);
        }

        [Test]
        public void TestBsonTypeMapperMapToBsonValue()
        {
            var document1 = (BsonDocument)BsonTypeMapper.MapToBsonValue(_dictionary, BsonType.Document);
            var document2 = (BsonDocument)BsonTypeMapper.MapToBsonValue(_hashtable, BsonType.Document);
            var document3 = (BsonDocument)BsonTypeMapper.MapToBsonValue(_idictionaryNonGeneric, BsonType.Document);
            var document4 = (BsonDocument)BsonTypeMapper.MapToBsonValue(_idictionary, BsonType.Document);

            Assert.AreEqual("Dictionary<string, object>", document1["type"].AsString);
            Assert.AreEqual("Hashtable", document2["type"].AsString);
            Assert.AreEqual("IDictionary", document3["type"].AsString);
            Assert.AreEqual("IDictionary<string, object>", document4["type"].AsString);
        }

        [Test]
        public void TestBsonTypeMapperTryMapToBsonValue()
        {
            BsonValue document1, document2, document3, document4;
            Assert.IsTrue(BsonTypeMapper.TryMapToBsonValue(_dictionary, out document1));
            Assert.IsTrue(BsonTypeMapper.TryMapToBsonValue(_hashtable, out document2));
            Assert.IsTrue(BsonTypeMapper.TryMapToBsonValue(_idictionaryNonGeneric, out document3));
            Assert.IsTrue(BsonTypeMapper.TryMapToBsonValue(_idictionary, out document4));

            Assert.AreEqual("Dictionary<string, object>", ((BsonDocument)document1)["type"].AsString);
            Assert.AreEqual("Hashtable", ((BsonDocument)document2)["type"].AsString);
            Assert.AreEqual("IDictionary", ((BsonDocument)document3)["type"].AsString);
            Assert.AreEqual("IDictionary<string, object>", ((BsonDocument)document4)["type"].AsString);
        }

        [Test]
        public void TestCollectionOptionsDocumentConstructor()
        {
            var document1 = new CollectionOptionsDocument(_dictionary);
            var document2 = new CollectionOptionsDocument(_hashtable);
            var document3 = new CollectionOptionsDocument(_idictionaryNonGeneric);
            var document4 = new CollectionOptionsDocument(_idictionary);

            Assert.AreEqual("Dictionary<string, object>", document1["type"].AsString);
            Assert.AreEqual("Hashtable", document2["type"].AsString);
            Assert.AreEqual("IDictionary", document3["type"].AsString);
            Assert.AreEqual("IDictionary<string, object>", document4["type"].AsString);
        }

        [Test]
        public void TestCommandDocumentConstructor()
        {
            var document1 = new CommandDocument(_dictionary);
            var document2 = new CommandDocument(_hashtable);
            var document3 = new CommandDocument(_idictionaryNonGeneric);
            var document4 = new CommandDocument(_idictionary);

            Assert.AreEqual("Dictionary<string, object>", document1["type"].AsString);
            Assert.AreEqual("Hashtable", document2["type"].AsString);
            Assert.AreEqual("IDictionary", document3["type"].AsString);
            Assert.AreEqual("IDictionary<string, object>", document4["type"].AsString);
        }

        [Test]
        public void TestFieldsDocumentConstructor()
        {
            var document1 = new FieldsDocument(_dictionary);
            var document2 = new FieldsDocument(_hashtable);
            var document3 = new FieldsDocument(_idictionaryNonGeneric);
            var document4 = new FieldsDocument(_idictionary);

            Assert.AreEqual("Dictionary<string, object>", document1["type"].AsString);
            Assert.AreEqual("Hashtable", document2["type"].AsString);
            Assert.AreEqual("IDictionary", document3["type"].AsString);
            Assert.AreEqual("IDictionary<string, object>", document4["type"].AsString);
        }

#pragma warning disable 618
        [Test]
        public void TestGeoNearOptionsDocumentConstructor()
        {
            var document1 = new GeoNearOptionsDocument(_dictionary);
            var document2 = new GeoNearOptionsDocument(_hashtable);
            var document3 = new GeoNearOptionsDocument(_idictionaryNonGeneric);
            var document4 = new GeoNearOptionsDocument(_idictionary);

            Assert.AreEqual("Dictionary<string, object>", document1["type"].AsString);
            Assert.AreEqual("Hashtable", document2["type"].AsString);
            Assert.AreEqual("IDictionary", document3["type"].AsString);
            Assert.AreEqual("IDictionary<string, object>", document4["type"].AsString);
        }
#pragma warning restore

        [Test]
        public void TestGroupByDocumentConstructor()
        {
            var document1 = new GroupByDocument(_dictionary);
            var document2 = new GroupByDocument(_hashtable);
            var document3 = new GroupByDocument(_idictionaryNonGeneric);
            var document4 = new GroupByDocument(_idictionary);

            Assert.AreEqual("Dictionary<string, object>", document1["type"].AsString);
            Assert.AreEqual("Hashtable", document2["type"].AsString);
            Assert.AreEqual("IDictionary", document3["type"].AsString);
            Assert.AreEqual("IDictionary<string, object>", document4["type"].AsString);
        }

        [Test]
        public void TestIndexKeysDocumentConstructor()
        {
            var document1 = new IndexKeysDocument(_dictionary);
            var document2 = new IndexKeysDocument(_hashtable);
            var document3 = new IndexKeysDocument(_idictionaryNonGeneric);
            var document4 = new IndexKeysDocument(_idictionary);

            Assert.AreEqual("Dictionary<string, object>", document1["type"].AsString);
            Assert.AreEqual("Hashtable", document2["type"].AsString);
            Assert.AreEqual("IDictionary", document3["type"].AsString);
            Assert.AreEqual("IDictionary<string, object>", document4["type"].AsString);
        }

        [Test]
        public void TestIndexOptionsDocumentConstructor()
        {
            var document1 = new IndexOptionsDocument(_dictionary);
            var document2 = new IndexOptionsDocument(_hashtable);
            var document3 = new IndexOptionsDocument(_idictionaryNonGeneric);
            var document4 = new IndexOptionsDocument(_idictionary);

            Assert.AreEqual("Dictionary<string, object>", document1["type"].AsString);
            Assert.AreEqual("Hashtable", document2["type"].AsString);
            Assert.AreEqual("IDictionary", document3["type"].AsString);
            Assert.AreEqual("IDictionary<string, object>", document4["type"].AsString);
        }

#pragma warning disable 618
        [Test]
        public void TestMapReduceOptionsDocumentConstructor()
        {
            var document1 = new MapReduceOptionsDocument(_dictionary);
            var document2 = new MapReduceOptionsDocument(_hashtable);
            var document3 = new MapReduceOptionsDocument(_idictionaryNonGeneric);
            var document4 = new MapReduceOptionsDocument(_idictionary);

            Assert.AreEqual("Dictionary<string, object>", document1["type"].AsString);
            Assert.AreEqual("Hashtable", document2["type"].AsString);
            Assert.AreEqual("IDictionary", document3["type"].AsString);
            Assert.AreEqual("IDictionary<string, object>", document4["type"].AsString);
        }
#pragma warning restore

        [Test]
        public void TestQueryDocumentConstructor()
        {
            var document1 = new QueryDocument(_dictionary);
            var document2 = new QueryDocument(_hashtable);
            var document3 = new QueryDocument(_idictionaryNonGeneric);
            var document4 = new QueryDocument(_idictionary);

            Assert.AreEqual("Dictionary<string, object>", document1["type"].AsString);
            Assert.AreEqual("Hashtable", document2["type"].AsString);
            Assert.AreEqual("IDictionary", document3["type"].AsString);
            Assert.AreEqual("IDictionary<string, object>", document4["type"].AsString);
        }

        [Test]
        public void TestScopeDocumentConstructor()
        {
            var document1 = new ScopeDocument(_dictionary);
            var document2 = new ScopeDocument(_hashtable);
            var document3 = new ScopeDocument(_idictionaryNonGeneric);
            var document4 = new ScopeDocument(_idictionary);

            Assert.AreEqual("Dictionary<string, object>", document1["type"].AsString);
            Assert.AreEqual("Hashtable", document2["type"].AsString);
            Assert.AreEqual("IDictionary", document3["type"].AsString);
            Assert.AreEqual("IDictionary<string, object>", document4["type"].AsString);
        }

        [Test]
        public void TestSortByDocumentConstructor()
        {
            var document1 = new SortByDocument(_dictionary);
            var document2 = new SortByDocument(_hashtable);
            var document3 = new SortByDocument(_idictionaryNonGeneric);
            var document4 = new SortByDocument(_idictionary);

            Assert.AreEqual("Dictionary<string, object>", document1["type"].AsString);
            Assert.AreEqual("Hashtable", document2["type"].AsString);
            Assert.AreEqual("IDictionary", document3["type"].AsString);
            Assert.AreEqual("IDictionary<string, object>", document4["type"].AsString);
        }

        [Test]
        public void TestUpdateDocumentConstructor()
        {
            var document1 = new UpdateDocument(_dictionary);
            var document2 = new UpdateDocument(_hashtable);
            var document3 = new UpdateDocument(_idictionaryNonGeneric);
            var document4 = new UpdateDocument(_idictionary);

            Assert.AreEqual("Dictionary<string, object>", document1["type"].AsString);
            Assert.AreEqual("Hashtable", document2["type"].AsString);
            Assert.AreEqual("IDictionary", document3["type"].AsString);
            Assert.AreEqual("IDictionary<string, object>", document4["type"].AsString);
        }
    }
}
