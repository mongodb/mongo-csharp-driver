/* Copyright 2010-2014 MongoDB Inc.
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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Tests
{
    [TestFixture]
    public class IAggregateFluentExtensionsTests
    {
        [Test]
        public void SortBy_should_generate_the_correct_sort()
        {
            var subject = CreateSubject();
            subject.SortBy(x => x.FirstName);

            var expectedSort = BsonDocument.Parse("{FirstName: 1}");

            Assert.AreEqual(expectedSort, ((BsonDocument)subject.Pipeline.Last())["$sort"]);
        }

        [Test]
        public void SortBy_ThenBy_should_generate_the_correct_sort()
        {
            var subject = CreateSubject();
            subject.SortBy(x => x.FirstName).ThenBy(x => x.LastName);

            var expectedSort = BsonDocument.Parse("{FirstName: 1, LastName: 1}");

            Assert.AreEqual(expectedSort, ((BsonDocument)subject.Pipeline.Last())["$sort"]);
        }

        [Test]
        public void SortBy_ThenByDescending_should_generate_the_correct_sort()
        {
            var subject = CreateSubject();
            subject.SortBy(x => x.FirstName).ThenByDescending(x => x.LastName);

            var expectedSort = BsonDocument.Parse("{FirstName: 1, LastName: -1}");

            Assert.AreEqual(expectedSort, ((BsonDocument)subject.Pipeline.Last())["$sort"]);
        }

        [Test]
        public void SortBy_ThenBy_ThenBy_should_generate_the_correct_sort()
        {
            var subject = CreateSubject();
            subject.SortBy(x => x.FirstName).ThenBy(x => x.LastName).ThenBy(x => x.Age);

            var expectedSort = BsonDocument.Parse("{FirstName: 1, LastName: 1, Age: 1}");

            Assert.AreEqual(expectedSort, ((BsonDocument)subject.Pipeline.Last())["$sort"]);
        }

        [Test]
        public void SortByDescending_should_generate_the_correct_sort()
        {
            var subject = CreateSubject();
            subject.SortByDescending(x => x.FirstName);

            var expectedSort = BsonDocument.Parse("{FirstName: -1}");

            Assert.AreEqual(expectedSort, ((BsonDocument)subject.Pipeline.Last())["$sort"]);
        }

        [Test]
        public void SortByDescending_ThenBy_should_generate_the_correct_sort()
        {
            var subject = CreateSubject();
            subject.SortByDescending(x => x.FirstName).ThenBy(x => x.LastName);

            var expectedSort = BsonDocument.Parse("{FirstName: -1, LastName: 1}");

            Assert.AreEqual(expectedSort, ((BsonDocument)subject.Pipeline.Last())["$sort"]);
        }

        [Test]
        public void SortByDescending_ThenByDescending_should_generate_the_correct_sort()
        {
            var subject = CreateSubject();
            subject.SortByDescending(x => x.FirstName).ThenByDescending(x => x.LastName);

            var expectedSort = BsonDocument.Parse("{FirstName: -1, LastName: -1}");

            Assert.AreEqual(expectedSort, ((BsonDocument)subject.Pipeline.Last())["$sort"]);
        }

        private IAggregateFluent<Person, Person> CreateSubject()
        {
            var settings = new MongoCollectionSettings();
            var collection = Substitute.For<IMongoCollection<Person>>();
            collection.Settings.Returns(settings);
            var options = new AggregateOptions();
            var subject = new AggregateFluent<Person, Person>(collection, new List<object>(), options, settings.SerializerRegistry.GetSerializer<Person>());

            return subject;
        }

        public class Person
        {
            public string FirstName;
            public string LastName;
            public int Age;
        }
    }
}
