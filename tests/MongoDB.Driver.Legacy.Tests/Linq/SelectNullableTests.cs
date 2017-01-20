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
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq
{
    public class SelectNullableTests
    {
        private enum E { None, A, B };

        private class C
        {
            public ObjectId Id { get; set; }
            [BsonElement("e")]
            [BsonRepresentation(BsonType.String)]
            public E? E { get; set; }
            [BsonElement("x")]
            public int? X { get; set; }
        }

        private static MongoCollection<C> __collection;
        private static Lazy<bool> __lazyOneTimeSetup = new Lazy<bool>(OneTimeSetup);

        public SelectNullableTests()
        {
            var _ = __lazyOneTimeSetup.Value;
        }

        private static bool OneTimeSetup()
        {
            __collection = LegacyTestConfiguration.GetCollection<C>();

            __collection.Drop();
            __collection.Insert(new C { E = null });
            __collection.Insert(new C { E = E.A });
            __collection.Insert(new C { E = E.B });
            __collection.Insert(new C { X = null });
            __collection.Insert(new C { X = 1 });
            __collection.Insert(new C { X = 2 });

            return true;
        }

        [SkippableFact]
        public void TestWhereEEqualsA()
        {
            RequireEnvironment.Check().EnvironmentVariable("MONO"); // Does not pass on Mono 3.2.5. Excluding for now.
            var query = from c in __collection.AsQueryable<C>()
                        where c.E == E.A
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => ((Nullable<Int32>)c.E == (Nullable<Int32>)1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"e\" : \"A\" }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereEEqualsNull()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.E == null
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.E == (Nullable<E>)null)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"e\" : null }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereXEquals1()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.X == 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.X == (Nullable<Int32>)1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"x\" : 1 }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereXEqualsNull()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.X == null
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.X == (Nullable<Int32>)null)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"x\" : null }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        private int Consume<T>(IQueryable<T> query)
        {
            var count = 0;
            foreach (var c in query)
            {
                count++;
            }
            return count;
        }
    }
}
