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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;

namespace MongoDB.DriverOnlineTests.Linq
{
    [TestFixture]
    public class MongoQueryProviderTests
    {
        private class C
        {
            public ObjectId Id { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
        }

        private MongoServer server;
        private MongoDatabase database;
        private MongoCollection collection;

        [TestFixtureSetUp]
        public void Setup()
        {
            server = MongoServer.Create("mongodb://localhost/?safe=true");
            server.Connect();
            database = server["onlinetests"];
            collection = database["linqtests"];
        }

        [Test]
        public void TestConstructor()
        {
            var provider = new MongoQueryProvider(collection);
        }

        [Test]
        public void TestCreateQuery()
        {
            var expression = collection.AsQueryable<C>().Expression;
            var provider = new MongoQueryProvider(collection);
            var query = provider.CreateQuery<C>(expression);
            Assert.AreSame(typeof(C), query.ElementType);
            Assert.AreSame(provider, query.Provider);
            Assert.AreSame(expression, query.Expression);
        }

        [Test]
        public void TestCreateQueryNonGeneric()
        {
            var expression = collection.AsQueryable<C>().Expression;
            var provider = new MongoQueryProvider(collection);
            var query = provider.CreateQuery(expression);
            Assert.AreSame(typeof(C), query.ElementType);
            Assert.AreSame(provider, query.Provider);
            Assert.AreSame(expression, query.Expression);
        }
    }
}
