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
using MongoDB.Bson.Serialization.Serializers;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Tests
{
    [TestFixture]
    public class IAggregateFluentExtensionsTests
    {
        [Test]
        public void Group_should_generate_the_correct_group_when_a_result_type_is_not_specified()
        {
            var subject = CreateSubject()
                .Group(new { _id = "$Tags" });

            var expectedGroup = BsonDocument.Parse("{$group: {_id: '$Tags'}}");

            Assert.AreEqual(expectedGroup, subject.Pipeline.Last());
        }

        [Test]
        public void Match_should_generate_the_correct_match()
        {
            var subject = CreateSubject()
                .Match(x => x.Age > 20);

            var expectedMatch = BsonDocument.Parse("{$match: {Age: {$gt: 20}}}");

            Assert.AreEqual(expectedMatch, subject.Pipeline.Last());
        }

        [Test]
        public void Project_should_generate_the_correct_group_when_a_result_type_is_not_specified()
        {
            var subject = CreateSubject()
                .Project(new { Awesome = "$Tags" });

            var expectedProject = BsonDocument.Parse("{$project: {Awesome: '$Tags'}}");

            Assert.AreEqual(expectedProject, subject.Pipeline.Last());
        }

        [Test]
        public void SortBy_should_generate_the_correct_sort()
        {
            var subject = CreateSubject()
                .SortBy(x => x.FirstName);

            var expectedSort = BsonDocument.Parse("{$sort: {FirstName: 1}}");

            Assert.AreEqual(expectedSort, subject.Pipeline.Last());
        }

        [Test]
        public void SortBy_ThenBy_should_generate_the_correct_sort()
        {
            var subject = CreateSubject()
                .SortBy(x => x.FirstName)
                .ThenBy(x => x.LastName);

            var expectedSort = BsonDocument.Parse("{$sort: {FirstName: 1, LastName: 1}}");

            Assert.AreEqual(expectedSort, subject.Pipeline.Last());
        }

        [Test]
        public void SortBy_ThenByDescending_should_generate_the_correct_sort()
        {
            var subject = CreateSubject()
                .SortBy(x => x.FirstName)
                .ThenByDescending(x => x.LastName);

            var expectedSort = BsonDocument.Parse("{$sort: {FirstName: 1, LastName: -1}}");

            Assert.AreEqual(expectedSort, subject.Pipeline.Last());
        }

        [Test]
        public void SortBy_ThenBy_ThenBy_should_generate_the_correct_sort()
        {
            var subject = CreateSubject()
                .SortBy(x => x.FirstName)
                .ThenBy(x => x.LastName)
                .ThenBy(x => x.Age);

            var expectedSort = BsonDocument.Parse("{$sort: {FirstName: 1, LastName: 1, Age: 1}}");

            Assert.AreEqual(expectedSort, subject.Pipeline.Last());
        }

        [Test]
        public void SortByDescending_should_generate_the_correct_sort()
        {
            var subject = CreateSubject()
                .SortByDescending(x => x.FirstName);

            var expectedSort = BsonDocument.Parse("{$sort: {FirstName: -1}}");

            Assert.AreEqual(expectedSort, subject.Pipeline.Last());
        }

        [Test]
        public void SortByDescending_ThenBy_should_generate_the_correct_sort()
        {
            var subject = CreateSubject()
                .SortByDescending(x => x.FirstName)
                .ThenBy(x => x.LastName);

            var expectedSort = BsonDocument.Parse("{$sort: {FirstName: -1, LastName: 1}}");

            Assert.AreEqual(expectedSort, subject.Pipeline.Last());
        }

        [Test]
        public void SortByDescending_ThenByDescending_should_generate_the_correct_sort()
        {
            var subject = CreateSubject();
            subject.SortByDescending(x => x.FirstName).ThenByDescending(x => x.LastName);

            var expectedSort = BsonDocument.Parse("{$sort: {FirstName: -1, LastName: -1}}");

            Assert.AreEqual(expectedSort, subject.Pipeline.Last());
        }

        [Test]
        public void Unwind_with_expression_to_BsonDocument_should_generate_the_correct_unwind()
        {
            var subject = CreateSubject()
                .Unwind(x => x.Age);

            var expectedUnwind = BsonDocument.Parse("{$unwind: '$Age'}");

            Assert.AreEqual(expectedUnwind, subject.Pipeline.Last());
        }

        [Test]
        public void Unwind_should_generate_the_correct_unwind()
        {
            var subject = CreateSubject()
                .Unwind("$Age");

            var expectedUnwind = BsonDocument.Parse("{$unwind: '$Age'}");

            Assert.AreEqual(expectedUnwind, subject.Pipeline.Last());
        }

        [Test]
        public void Unwind_with_expression_to_new_result_should_generate_the_correct_unwind()
        {
            var subject = CreateSubject()
                .Unwind("$Age", BsonDocumentSerializer.Instance);

            var expectedUnwind = BsonDocument.Parse("{$unwind: '$Age'}");

            Assert.AreEqual(expectedUnwind, subject.Pipeline.Last());
            Assert.AreSame(BsonDocumentSerializer.Instance, subject.ResultSerializer);
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
