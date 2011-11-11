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
using System.Text;
using MongoDB.Bson.Serialization.Attributes;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDB.DriverUnitTests
{
    [TestFixture]
    public class MongoCollectionLinqTests
    {
        [TestFixtureSetUp]
        public void TestSetup()
        {

            var server = MongoServer.Create();
            var database = server["test"];


            var it = new MongoCollectionSettings<Document>(database, "Document");
            var col = database.GetCollection(it);

            col.Save(new Document() { ID = 1, Name = "Vlad" });
            col.Save(new Document() { ID = 2, Name = "John" });
            col.Save(new Document() { ID = 3, Name = "Jeff" });
            col.Save(new Document() { ID = 4, Name = "Fred" });
        }

        [TestFixtureTearDown]
        public void TestTearDown()
        {

            var server = MongoServer.Create();
            var database = server["test"];


            var it = new MongoCollectionSettings<Document>(database, "Document");
            database.GetCollection(it).RemoveAll();
        }

        [Test]
        public void TestBasic()
        {
            var server = MongoServer.Create();
            var database = server["test"];


            var it = new MongoCollectionSettings<Document>(database, "Document");


            var items = from item in database.GetCollection(it)
                        where item.ID == 1
                        select item;

            var count = Enumerable.Count(items);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void TestAdvancedWhere()
        {
            var server = MongoServer.Create();
            var database = server["test"];


            var it = new MongoCollectionSettings<Document>(database, "Document");

            var rnd = new Random();
            var items = from item in database.GetCollection(it)
                        where item.ID == rnd.Next(0, 4)
                        select item;

            var count = 0;
            foreach (var item in items)
            {
                count++;
            }

            Assert.AreEqual(1, count);
        }


        [Test]
        public void TestGreaterLess()
        {
            var server = MongoServer.Create();
            var database = server["test"];


            var it = new MongoCollectionSettings<Document>(database, "Document");


            Assert.AreEqual(3, (from item in database.GetCollection(it) where item.ID > 1 select item).Count());
            Assert.AreEqual(4, (from item in database.GetCollection(it) where item.ID >= 1 select item).Count());
            Assert.AreEqual(3, (from item in database.GetCollection(it) where item.ID < 4 select item).Count());
            Assert.AreEqual(4, (from item in database.GetCollection(it) where item.ID <= 4 select item).Count());

            Assert.AreEqual(3, (from item in database.GetCollection(it) where item.ID != 1 select item).Count());

        }


        [Test]
        public void TestMultipleParams()
        {
            var server = MongoServer.Create();
            var database = server["test"];


            var it = new MongoCollectionSettings<Document>(database, "Document");

            var items = from item in database.GetCollection(it)
                        where item.ID == 1 || item.ID == 2
                        select item;

            var count = 0;
            foreach (var item in items)
            {
                count++;
            }

            Assert.AreEqual(2, count);
        }
        [Test]
        public void TestFirst()
        {
            var server = MongoServer.Create();
            var database = server["test"];


            var it = new MongoCollectionSettings<Document>(database, "Document");
            var first = database.GetCollection(it).FirstOrDefault(k => k.ID == 1);
            Assert.IsNotNull(first);
            Assert.AreEqual("Vlad", first.Name);


            first = database.GetCollection(it).First(k => k.ID == 1);

            Assert.IsNotNull(first);
            Assert.AreEqual("Vlad", first.Name);

            var thrown = false;
            
            Assert.Throws<ArgumentNullException>(() => database.GetCollection(it).First(k => k.ID == 0));


            Assert.AreEqual("John", database.GetCollection(it).Skip(1).First().Name);
            
        }
        [Test]
        public void TestLast()
        {
            var server = MongoServer.Create();
            var database = server["test"];


            var it = new MongoCollectionSettings<Document>(database, "Document");
            var first = database.GetCollection(it).LastOrDefault(k => k.ID == 1);
            Assert.IsNotNull(first);
            Assert.AreEqual("Vlad", first.Name);


            first = database.GetCollection(it).Last();

            Assert.IsNotNull(first);
            Assert.AreEqual("Fred", first.Name);

           

            Assert.Throws<ArgumentNullException>(() => database.GetCollection(it).Last(k => k.ID == 0));
            Assert.Throws<InvalidOperationException>(() => database.GetCollection(it).Reverse().Last());

        }

        [Test]
        public void TestSort()
        {
            var server = MongoServer.Create();
            var database = server["test"];


            var it = new MongoCollectionSettings<Document>(database, "Document");


            var col = database.GetCollection(it);

            var iterator = col.OrderBy(k => k.Name).OrderByDescending(k => k.ID).GetEnumerator();
            Assert.AreEqual(true, iterator.MoveNext());
            Assert.AreEqual("Fred", iterator.Current.Name);

            Assert.AreEqual(true, iterator.MoveNext());
            Assert.AreEqual("Jeff", iterator.Current.Name);

            Assert.AreEqual(true, iterator.MoveNext());
            Assert.AreEqual("John", iterator.Current.Name);

            Assert.AreEqual(true, iterator.MoveNext());
            Assert.AreEqual("Vlad", iterator.Current.Name);

        }

        [Test]
        public void TestCount()
        {
            var server = MongoServer.Create();
            var database = server["test"];


            var it = new MongoCollectionSettings<Document>(database, "Document");


            var col = database.GetCollection(it);

            var query = (from k in col where k.Name == "Vlad" select k);


            Assert.AreEqual(1, query.Count());
            Assert.AreEqual(typeof(int), query.Count().GetType());

            Assert.AreEqual(1, query.LongCount());
            Assert.AreEqual(typeof(long), query.LongCount().GetType());

            Assert.AreEqual(1, query.LongCount(k => k.ID == 1));
            Assert.AreEqual(typeof(long), query.LongCount(k => k.ID == 1).GetType());
        }

        [Test]
        public void TestLimit()
        {
            var server = MongoServer.Create();
            var database = server["test"];


            var it = new MongoCollectionSettings<Document>(database, "Document");


            var col = database.GetCollection(it);

            var count = 0;
            foreach (var item in col.OrderByDescending(k => k.ID).Take(2))
            {
                count++;
            }

            Assert.AreEqual(2, count);
        }

        [Test]
        public void TestSkip()
        {
            var server = MongoServer.Create();
            var database = server["test"];


            var it = new MongoCollectionSettings<Document>(database, "Document");


            var col = database.GetCollection(it);


            var count = 0;
            foreach (var item in col.OrderByDescending(k => k.ID).Skip(2))
            {
                count++;
            }

            Assert.AreEqual(2, count);
        }


        [Test]
        public void TestReverse()
        {
            var server = MongoServer.Create();
            var database = server["test"];


            var it = new MongoCollectionSettings<Document>(database, "Document");


            var col = database.GetCollection(it);

            var last = col.Reverse().FirstOrDefault();
            Assert.AreEqual("Fred", last.Name);

        }



        [Test]
        public void TestSelect()
        {
            var server = MongoServer.Create();
            var database = server["test"];


            var it = new MongoCollectionSettings<Document>(database, "Document");


            var col = database.GetCollection(it);

            var sel = col.Select(k => new { It = k.ID + k.Name, ID = k.ID }).Reverse().Take(2).Where(k => k.ID == 4);
            var cnt = 0;
            foreach (var itm in sel)
            {
                cnt++;

            }

            Assert.AreEqual(1, cnt);



            cnt = 0;
            foreach (var itm in col.Where(k => k.ID == 1).Select(ConvertDoc).Select(s => s == "Vlad"))
            {
                cnt++;
            }

            Assert.AreEqual(1, cnt);
        }

        private string ConvertDoc(Document doc)
        {
            return doc.Name;
        }




        public class Document
        {
            public string Name
            {
                get;
                set;
            }
            [BsonId]
            public int ID
            {
                get;
                set;
            }

            public int GetRandom()
            {
                Random rnd = new Random();
                return rnd.Next(0, 4);
            }

        }



    }
}
