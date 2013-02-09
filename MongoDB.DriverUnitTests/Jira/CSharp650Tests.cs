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

namespace MongoDB.DriverUnitTests.Jira.CSharp650
{
    [TestFixture]
    public class CSharp650Tests
    {
        private readonly MongoCollection<BsonDocument> _collection = Configuration.TestCollection;

        public class C
        {
            public string _id { get; set; }
            public Score[] Scores { get; set; }
        }

        public class Score
        {
            public int Value;
        }

        [SetUp]
        public void SetUp()
        {
            _collection.RemoveAll();
            SaveNewDocument("id1", 1, 2, 3);
            SaveNewDocument("id2", 2, 3, 4);
            SaveNewDocument("id3", 3, 4, 5);
        }

        private void SaveNewDocument(string id, params int[] scores)
        {
            _collection.Save(new C
              {
                  _id = id,
                  Scores = scores.Select(s => new Score {Value = s}).ToArray(),
              });
        }

        [Test]
        public void QueryWhereAllShouldWork()
        {
            var query = from c in _collection.AsQueryable<C>()
                where c.Scores.All(t => t.Value > 1)
                select c._id;

            Assert.That(query, Is.EquivalentTo(new[] {"id2", "id3"}));
        }

        [Test]
        public void QueryAllShouldWork()
        {
            var statement = _collection.AsQueryable<C>().All(c => c.Scores.Any(s => s.Value == 2));
            Assert.False(statement);
            statement = _collection.AsQueryable<C>().All(c => c.Scores.Any(s => s.Value == 3));
            Assert.True(statement);
        }
    }
}