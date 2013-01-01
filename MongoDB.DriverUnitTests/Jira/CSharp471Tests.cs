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
using System.Linq;
using MongoDB.Driver.Linq;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Jira
{
    [TestFixture]
    public class CSharp471Tests
    {
        public class Base
        {
            public Guid Id { get; set; }

            public string A { get; set; }
        }

        public class T1 : Base
        {
            public string B { get; set; }
        }

        public class T2 : Base
        {
            public string C { get; set; }
        }

        [Test]
        public void CastTest()
        {
            var db = Configuration.TestDatabase;
            var collection = db.GetCollection<Base>("castTest");
            collection.Drop();

            var t1 = new T1 { Id = Guid.NewGuid(), A = "T1.A", B = "T1.B" };
            var t2 = new T2 { Id = Guid.NewGuid(), A = "T2.A" };
            collection.Insert(t1);
            collection.Insert(t2);

            var query = from t in collection.AsQueryable()
                        where t is T1 && ((T1)t).B == "T1.B" 
                        select t;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(collection, translatedQuery.Collection);
            Assert.AreSame(typeof(Base), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(Base t) => ((t is T1) && ((T1)t.B == \"T1.B\"))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"_t\" : \"T1\", \"B\" : \"T1.B\" }", selectQuery.BuildQuery().ToString());

            var results = query.ToList();
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0], Is.InstanceOf(typeof(T1)));
            Assert.That(results[0].A, Is.EqualTo("T1.A"));
        }
    }
}