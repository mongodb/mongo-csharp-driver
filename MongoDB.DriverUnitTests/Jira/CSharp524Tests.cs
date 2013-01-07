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

using System;
using System.Linq;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Jira.CSharp524
{
    [TestFixture]
    public class CSharp524Tests
    {
        public class C
        {
            public int Id { get; set; }
            public int X { get; set; }
        }

        private MongoCollection<C> _collection;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            _collection = Configuration.GetTestCollection<C>();
            _collection.Drop();
        }

        [Test]
        public void TestDistinctMustBeLastOperator()
        {
            _collection.RemoveAll();
            _collection.Insert(new C { Id = 1, X = 1 });
            _collection.Insert(new C { Id = 2, X = 2 });

            var query = _collection.AsQueryable()
                .Select(d => d.X)
                .Distinct();

            var result = query.AsEnumerable().OrderBy(x => x).ToList();
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(1, result[0]);
            Assert.AreEqual(2, result[1]);

            var ex = Assert.Throws<NotSupportedException>(() => { query.Count(); });
            var message = "No further operators may follow Distinct in a LINQ query.";
            Assert.AreEqual(message, ex.Message);
        }
    }
}
