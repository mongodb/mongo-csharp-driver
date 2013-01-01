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
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Jira.CSharp522
{
    [TestFixture]
    public class CSharp522Tests
    {
        public class C
        {
            public int Id { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
        }

        private MongoCollection<C> _collection;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            _collection = Configuration.GetTestCollection<C>();
            _collection.Drop();
        }

        [Test]
        public void Test()
        {
            _collection.RemoveAll();
            _collection.Insert(new C { Id = 1, X = 1, Y = 2 });
            _collection.Insert(new C { Id = 2, X = 1, Y = 2 });
            _collection.Insert(new C { Id = 3, X = 2, Y = 3 });

            var query = _collection.AsQueryable()
                .Select(d => new { d.X, d.Y })
                .Distinct();
            var ex = Assert.Throws<NotSupportedException>(() => { query.ToList(); });
            var message = "Distinct is only supported for a single field. Projections used with Distinct must resolve to a single field in the document.";
            Assert.AreEqual(message, ex.Message);
        }
    }
}
