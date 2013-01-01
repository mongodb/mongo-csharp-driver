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
using MongoDB.Driver.Linq;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Jira.CSharp475
{
    [TestFixture]
    public class CSharp475Tests
    {
        public abstract class Base
        {
            public ObjectId Id { get; set; }
            public string A { get; set; }
        }

        public class T1 : Base
        {
            public string B { get; set; }
        }

        [Test]
        public void ProjectAfterOfTypeTest()
        {
            var server = Configuration.TestServer;
            var db = server.GetDatabase("csharp475");
            var collection = db.GetCollection<Base>("ProjectTest");
            collection.Drop();
            
            var t1 = new T1 { A = "T1.A", B = "T1.B" };
            collection.Insert(t1);

            var query = from t in collection.AsQueryable().OfType<T1>() select t.B;
            var results = query.ToList();
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0], Is.EqualTo("T1.B"));
        }
    }
}
