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
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq
{
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

            [BsonDictionaryOptions(DictionaryRepresentation.Document)]
            public IDictionary H { get; set; } // serialized as { H : { x : 1, ... } }
            [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
            public IDictionary I { get; set; } // serialized as { I : [{ k : "x", v : 1 }, ...] }
            [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
            public IDictionary J { get; set; } // serialized as { J : [["x", 1], ... ] }
        }

        private static MongoCollection __collection;
        private static Lazy<bool> __lazyOneTimeSetup = new Lazy<bool>(OneTimeSetup);

        public SelectDictionaryTests()
        {
            var _ = __lazyOneTimeSetup.Value;
        }

        private static bool OneTimeSetup()
        {
            __collection = LegacyTestConfiguration.GetCollection<C>();

            var de = new Dictionary<string, int>();
            var dx = new Dictionary<string, int>() { { "x", 1 } };
            var dy = new Dictionary<string, int>() { { "y", 1 } };

            var he = new Hashtable();
            var hx = new Hashtable { { "x", 1 } };
            var hy = new Hashtable { { "y", 1 } };

            __collection.Drop();
            __collection.Insert(new C { D = null, E = null, F = null, H = null, I = null, J = null });
            __collection.Insert(new C { D = de, E = de, F = de, H = he, I = he, J = he });
            __collection.Insert(new C { D = dx, E = dx, F = dx, H = hx, I = hx, J = hx });
            __collection.Insert(new C { D = dy, E = dy, F = dy, H = hy, I = hy, J = hy });

            return true;
        }

        [Fact]
        public void TestWhereDContainsKeyX()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.D.ContainsKey("x")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => c.D.ContainsKey(\"x\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"D.x\" : { \"$exists\" : true } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, query.ToList().Count());
        }

        [Fact]
        public void TestWhereDContainsKeyZ()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.D.ContainsKey("z")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => c.D.ContainsKey(\"z\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"D.z\" : { \"$exists\" : true } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(0, query.ToList().Count());
        }

        [Fact]
        public void TestWhereEContainsKeyX()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.E.ContainsKey("x")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => c.E.ContainsKey(\"x\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"E.k\" : \"x\" }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, query.ToList().Count());
        }

        [Fact]
        public void TestWhereEContainsKeyZ()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.E.ContainsKey("z")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => c.E.ContainsKey(\"z\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"E.k\" : \"z\" }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(0, query.ToList().Count());
        }

        [Fact]
        public void TestWhereFContainsKeyX()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.F.ContainsKey("x")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => c.F.ContainsKey(\"x\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            var ex = Assert.Throws<NotSupportedException>(() => { selectQuery.BuildQuery(); });
            Assert.Equal("ContainsKey in a LINQ query is only supported for DictionaryRepresentation ArrayOfDocuments or Document, not ArrayOfArrays.", ex.Message);
        }

        [Fact]
        public void TestWhereHContainsKeyX()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.H.Contains("x")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => c.H.Contains(\"x\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"H.x\" : { \"$exists\" : true } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, query.ToList().Count());
        }

        [Fact]
        public void TestWhereHContainsKeyZ()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.H.Contains("z")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => c.H.Contains(\"z\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"H.z\" : { \"$exists\" : true } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(0, query.ToList().Count());
        }

        [Fact]
        public void TestWhereIContainsKeyX()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.I.Contains("x")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => c.I.Contains(\"x\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"I.k\" : \"x\" }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, query.ToList().Count());
        }

        [Fact]
        public void TestWhereIContainsKeyZ()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.I.Contains("z")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => c.I.Contains(\"z\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"I.k\" : \"z\" }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(0, query.ToList().Count());
        }

        [Fact]
        public void TestWhereJContainsKeyX()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.J.Contains("x")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => c.J.Contains(\"x\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            var ex = Assert.Throws<NotSupportedException>(() => { selectQuery.BuildQuery(); });
            Assert.Equal("Contains in a LINQ query is only supported for DictionaryRepresentation ArrayOfDocuments or Document, not ArrayOfArrays.", ex.Message);
        }
    }
}
