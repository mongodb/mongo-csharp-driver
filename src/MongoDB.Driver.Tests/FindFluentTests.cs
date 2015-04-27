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

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Tests
{
    [TestFixture]
    public class FindFluentTests
    {
        private IMongoCollection<Person> _collection;

        [Test]
        public void CountAsync_should_not_throw_a_null_reference_exception()
        {
            var subject = CreateSubject();

            subject.CountAsync().GetAwaiter().GetResult();

            _collection.Received().CountAsync(
                subject.Filter,
                Arg.Any<CountOptions>(),
                Arg.Any<CancellationToken>());
        }

        [Test]
        public void ToString_should_return_the_correct_string()
        {
            var subject = CreateSubject();
            subject.Filter = new BsonDocument("Age", 20);
            subject.Options.Comment = "awesome";
            subject.Options.MaxTime = TimeSpan.FromSeconds(2);
            subject.Options.Modifiers = new BsonDocument
            {
                { "$explain", true },
                { "$hint", "ix_1" }
            };

            var find = subject
                .SortBy(x => x.LastName)
                .ThenByDescending(x => x.FirstName)
                .Skip(2)
                .Limit(10)
                .Project(x => x.FirstName + " " + x.LastName);

            var str = find.ToString();

            str.Should().Be(
                "find({ \"Age\" : 20 }, { \"FirstName\" : 1, \"LastName\" : 1, \"_id\" : 0 })" +
                ".sort({ \"LastName\" : 1, \"FirstName\" : -1 })" +
                ".skip(2)" +
                ".limit(10)" +
                ".maxTime(2000)" +
                "._addSpecial(\"$comment\", \"awesome\")" +
                "._addSpecial(\"$explain\", true)" +
                "._addSpecial(\"$hint\", \"ix_1\")");
        }

        private IFindFluent<Person, Person> CreateSubject()
        {
            var settings = new MongoCollectionSettings();
            _collection = Substitute.For<IMongoCollection<Person>>();
            _collection.DocumentSerializer.Returns(BsonSerializer.SerializerRegistry.GetSerializer<Person>());
            _collection.Settings.Returns(settings);
            var options = new FindOptions<Person, Person>();
            var subject = new FindFluent<Person, Person>(_collection, new BsonDocument(), options);

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