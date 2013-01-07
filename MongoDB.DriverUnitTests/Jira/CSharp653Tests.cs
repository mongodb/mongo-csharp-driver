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

using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Jira.CSharp653
{
    [TestFixture]
    public class CSharp653Tests
    {
        public interface IEntity
        {
            string Name { get; set; }
        }

        public class Entity : IEntity
        {
            public Entity()
            {
            }

            public string Name { get; set; }
        }

        [Test]
        public void TestAndWithAll()
        {
            var all = new QueryDocument();
            var query = Query.And(all);
            var expected = "{ }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestAndWithAllOnLeft()
        {
            var all = new QueryDocument();
            var right = Query<Entity>.EQ(x => x.Name, "John");
            var query = Query.And(all, right);
            var expected = "{ 'Name' : 'John' }".Replace("'", "\"");
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestAndWithAllOnRight()
        {
            var left = Query<Entity>.EQ(x => x.Name, "John");
            var all = new QueryDocument();
            var query = Query.And(left, all);
            var expected = "{ 'Name' : 'John' }".Replace("'", "\"");
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestLinqCount()
        {
            var collection = Configuration.GetTestCollection<Entity>();
            if (collection.Exists()) { collection.Drop(); }

            for (int i = 0; i < 100; ++i)
            {
                var e = new Entity() { Name = "Name_" + i };
                collection.Insert(e.GetType(), e, WriteConcern.Acknowledged);
            }

            var query = (from e in collection.AsQueryable<Entity>()
                         where true || e.Name == "Name_22"
                         select e);
            var count = query.Count();

            Assert.AreEqual(100, count);
        }

        [Test]
        public void TestOrWithAll()
        {
            var all = new QueryDocument();
            var query = Query.Or(all);
            var expected = "{ }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestOrWithAllOnLeft()
        {
            var all = new QueryDocument();
            var right = Query<Entity>.EQ(x => x.Name, "John");
            var query = Query.Or(all, right);
            var expected = "{ }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestOrWithAllOnRight()
        {
            var left = Query<Entity>.EQ(x => x.Name, "John");
            var all = new QueryDocument();
            var query = Query.Or(left, all);
            var expected = "{ }";
            Assert.AreEqual(expected, query.ToJson());
        }
    }
}