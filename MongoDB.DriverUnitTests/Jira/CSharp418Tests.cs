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

using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Jira.CSharp418
{
    [TestFixture]
    public class CSharp418Tests
    {
        public class C
        {
            public ObjectId Id;
            public int X;
        }

        public class D : C
        {
            public int Y;
        }

        private MongoServer _server;
        private MongoDatabase _database;
        private MongoCollection<D> _collection;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            _server = Configuration.TestServer;
            _database = Configuration.TestDatabase;
            _collection = Configuration.GetTestCollection<D>();
        }

        [Test]
        public void TestQueryAgainstInheritedField()
        {
            _collection.Drop();
            _collection.Insert(new D { X = 1, Y = 2 });

            var query = from d in _collection.AsQueryable<D>()
                        where d.X == 1
                        select d;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(D), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(D d) => (d.X == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"X\" : 1 }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, query.ToList().Count);
        }
    }
}
