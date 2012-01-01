/* Copyright 2010-2012 10gen Inc.
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
    public class MongoQueryableTests
    {
        private class C
        {
            public ObjectId Id { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
        }

        private MongoServer _server;
        private MongoDatabase _database;
        private MongoCollection _collection;

        [TestFixtureSetUp]
        public void Setup()
        {
            _server = Configuration.TestServer;
            _server.Connect();
            _database = Configuration.TestDatabase;
            _collection = Configuration.TestCollection;
        }

        [Test]
        public void TestConstructorWithOneArgument()
        {
            var provider = new MongoQueryProvider(_collection);
            var iqueryable = (IQueryable)new MongoQueryable<C>(provider);
            Assert.AreSame(typeof(C), iqueryable.ElementType);
            Assert.AreSame(provider, iqueryable.Provider);
        }

        [Test]
        public void TestConstructorWithTwoArguments()
        {
            var queryable = _collection.AsQueryable<C>();
            var iqueryable = (IQueryable)new MongoQueryable<C>((MongoQueryProvider)queryable.Provider, queryable.Expression);
            Assert.AreSame(typeof(C), iqueryable.ElementType);
            Assert.AreSame(queryable.Provider, iqueryable.Provider);
            Assert.AreSame(queryable.Expression, iqueryable.Expression);
        }
    }
}
