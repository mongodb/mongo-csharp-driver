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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Linq
{
    [TestFixture]
    public class SelectDictionaryTests
    {
        private class C
        {
            public ObjectId Id { get; set; }

            [BsonDictionaryOptions(DictionaryRepresentation.Document)]
            public IDictionary<string, int> D { get; set; } // serialized as { D : { x : 1, ... } }
            [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
            public IDictionary<string, int> E { get; set; } // serialized as { E : [{ k : "x", v : 1 }, ...] }
            [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
            public IDictionary<string, int> F { get; set; } // serialized as { F : [["x", 1], ... ] }
            [BsonDictionaryOptions(DictionaryRepresentation.Dynamic)]
            public IDictionary<string, int> G { get; set; } // serialized form depends on actual key values

            [BsonDictionaryOptions(DictionaryRepresentation.Document)]
            public IDictionary H { get; set; } // serialized as { H : { x : 1, ... } }
            [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
            public IDictionary I { get; set; } // serialized as { I : [{ k : "x", v : 1 }, ...] }
            [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
            public IDictionary J { get; set; } // serialized as { J : [["x", 1], ... ] }
            [BsonDictionaryOptions(DictionaryRepresentation.Dynamic)]
            public IDictionary K { get; set; } // serialized form depends on actual key values
        }

        private MongoServer _server;
        private MongoDatabase _database;
        private MongoCollection _collection;

        [TestFixtureSetUp]
        public void Setup()
        {
            _server = Configuration.TestServer;
            _database = Configuration.TestDatabase;
            _collection = Configuration.GetTestCollection<C>();

            var de = new Dictionary<string, int>();
            var dx = new Dictionary<string, int>() { { "x", 1 } };
            var dy = new Dictionary<string, int>() { { "y", 1 } };

            var he = new Hashtable();
            var hx = new Hashtable { { "x", 1 } };
            var hy = new Hashtable { { "y", 1 } };

            _collection.Drop();
            _collection.Insert(new C { D = null, E = null, F = null, G = null, H = null, I = null, J = null, K = null });
            _collection.Insert(new C { D = de, E = de, F = de, G = de, H = he, I = he, J = he, K = he });
            _collection.Insert(new C { D = dx, E = dx, F = dx, G = dx, H = hx, I = hx, J = hx, K = hx });
            _collection.Insert(new C { D = dy, E = dy, F = dy, G = dy, H = hy, I = hy, J = hy, K = hy });
        }

        [Test]
        public void TestWhereDContainsKeyX()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.D.ContainsKey("x")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => c.D.ContainsKey(\"x\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"D.x\" : { \"$exists\" : true } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, query.ToList().Count());
        }

        [Test]
        public void TestWhereDContainsKeyZ()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.D.ContainsKey("z")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => c.D.ContainsKey(\"z\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"D.z\" : { \"$exists\" : true } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(0, query.ToList().Count());
        }

        [Test]
        public void TestWhereEContainsKeyX()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.E.ContainsKey("x")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => c.E.ContainsKey(\"x\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"E.k\" : \"x\" }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, query.ToList().Count());
        }

        [Test]
        public void TestWhereEContainsKeyZ()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.E.ContainsKey("z")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => c.E.ContainsKey(\"z\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"E.k\" : \"z\" }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(0, query.ToList().Count());
        }

        [Test]
        public void TestWhereFContainsKeyX()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.F.ContainsKey("x")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => c.F.ContainsKey(\"x\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            var ex = Assert.Throws<NotSupportedException>(() => { selectQuery.BuildQuery(); });
            Assert.AreEqual("ContainsKey in a LINQ query is only supported for DictionaryRepresentation ArrayOfDocuments or Document, not ArrayOfArrays.", ex.Message);
        }

        [Test]
        public void TestWhereGContainsKeyX()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.G.ContainsKey("x")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => c.G.ContainsKey(\"x\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            var ex = Assert.Throws<NotSupportedException>(() => { selectQuery.BuildQuery(); });
            Assert.AreEqual("ContainsKey in a LINQ query is only supported for DictionaryRepresentation ArrayOfDocuments or Document, not Dynamic.", ex.Message);
        }

        [Test]
        public void TestWhereHContainsKeyX()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.H.Contains("x")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => c.H.Contains(\"x\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"H.x\" : { \"$exists\" : true } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, query.ToList().Count());
        }

        [Test]
        public void TestWhereHContainsKeyZ()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.H.Contains("z")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => c.H.Contains(\"z\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"H.z\" : { \"$exists\" : true } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(0, query.ToList().Count());
        }

        [Test]
        public void TestWhereIContainsKeyX()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.I.Contains("x")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => c.I.Contains(\"x\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"I.k\" : \"x\" }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, query.ToList().Count());
        }

        [Test]
        public void TestWhereIContainsKeyZ()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.I.Contains("z")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => c.I.Contains(\"z\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"I.k\" : \"z\" }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(0, query.ToList().Count());
        }

        [Test]
        public void TestWhereJContainsKeyX()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.J.Contains("x")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => c.J.Contains(\"x\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            var ex = Assert.Throws<NotSupportedException>(() => { selectQuery.BuildQuery(); });
            Assert.AreEqual("Contains in a LINQ query is only supported for DictionaryRepresentation ArrayOfDocuments or Document, not ArrayOfArrays.", ex.Message);
        }

        [Test]
        public void TestWhereKContainsKeyX()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.K.Contains("x")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => c.K.Contains(\"x\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            var ex = Assert.Throws<NotSupportedException>(() => { selectQuery.BuildQuery(); });
            Assert.AreEqual("Contains in a LINQ query is only supported for DictionaryRepresentation ArrayOfDocuments or Document, not Dynamic.", ex.Message);
        }
    }
}
