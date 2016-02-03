/* Copyright 2010-2015 MongoDB Inc.
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
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Translators;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Tests
{
    [TestFixture]
    public class IFindFluentExtensionsTests
    {
        // public methods
        [Test]
        public void First_should_add_limit_and_call_ToCursor(
            [Values(false, true)] bool async)
        {
            var subject1 = Substitute.For<IFindFluent<Person, Person>>();
            var subject2 = Substitute.For<IFindFluent<Person, Person>>();
            var cursor = Substitute.For<IAsyncCursor<Person>>();
            var firstBatch = new Person[]
            {
                new Person { FirstName = "John" },
                new Person { FirstName = "Jane" }
            };
            var cancellationToken = new CancellationTokenSource().Token;

            subject1.Limit(1).Returns(subject2);
            cursor.Current.Returns(firstBatch);

            Person result;
            if (async)
            {
                subject2.ToCursorAsync(cancellationToken).Returns(Task.FromResult(cursor));
                cursor.MoveNextAsync(cancellationToken).Returns(Task.FromResult(true));

                result = subject1.FirstAsync(cancellationToken).GetAwaiter().GetResult();

            }
            else
            {
                subject2.ToCursor(cancellationToken).Returns(cursor);
                cursor.MoveNext(cancellationToken).Returns(true);

                result = subject1.First(cancellationToken);
            }

            result.FirstName = "John";
        }

        [Test]
        public void First_should_throw_when_find_is_null(
          [Values(false, true)] bool async)
        {
            IFindFluent<Person, Person> subject = null;

            Action action;
            if (async)
            {
                action = () => subject.FirstAsync().GetAwaiter().GetResult();
            }
            else
            {
                action = () => subject.First();
            }

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("find");
        }

        [Test]
        public void FirstOrDefault_should_add_limit_and_call_ToCursor(
           [Values(false, true)] bool async)
        {
            var subject1 = Substitute.For<IFindFluent<Person, Person>>();
            var subject2 = Substitute.For<IFindFluent<Person, Person>>();
            var cursor = Substitute.For<IAsyncCursor<Person>>();
            var firstBatch = new Person[]
            {
                new Person { FirstName = "John" },
                new Person { FirstName = "Jane" }
            };
            var cancellationToken = new CancellationTokenSource().Token;

            subject1.Limit(1).Returns(subject2);
            cursor.Current.Returns(firstBatch);

            Person result;
            if (async)
            {
                subject2.ToCursorAsync(cancellationToken).Returns(Task.FromResult(cursor));
                cursor.MoveNextAsync(cancellationToken).Returns(Task.FromResult(true));

                result = subject1.FirstOrDefaultAsync(cancellationToken).GetAwaiter().GetResult();

            }
            else
            {
                subject2.ToCursor(cancellationToken).Returns(cursor);
                cursor.MoveNext(cancellationToken).Returns(true);

                result = subject1.FirstOrDefault(cancellationToken);
            }

            result.FirstName = "John";
        }

        [Test]
        public void FirstOrDefault_should_throw_when_find_is_null(
            [Values(false, true)] bool async)
        {
            IFindFluent<Person, Person> subject = null;

            Action action;
            if (async)
            {
                action = () => subject.FirstOrDefaultAsync().GetAwaiter().GetResult();
            }
            else
            {
                action = () => subject.FirstOrDefault();
            }

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("find");
        }

        [Test]
        public void Project_should_generate_the_correct_fields_when_a_BsonDocument_is_used()
        {
            var subject = CreateSubject()
                .Project(BsonDocument.Parse("{_id: 1, Tags: 1}"));

            var expectedProjection = BsonDocument.Parse("{_id: 1, Tags: 1}");

            AssertProjection(subject, expectedProjection);
        }

        [Test]
        public void Project_should_generate_the_correct_fields_when_a_string_is_used()
        {
            var subject = CreateSubject()
                .Project("{_id: 1, Tags: 1}");

            var expectedProjection = BsonDocument.Parse("{_id: 1, Tags: 1}");

            AssertProjection(subject, expectedProjection);
        }

        [Test]
        public void Project_should_generate_the_correct_fields_and_assign_the_correct_result_serializer()
        {
            var subject = CreateSubject()
                .Project(x => x.FirstName + " " + x.LastName);

            var expectedProjection = BsonDocument.Parse("{FirstName: 1, LastName: 1, _id: 0}");

            AssertProjection(subject, expectedProjection);
        }

        [Test]
        public void Single_should_add_limit_and_call_ToCursor(
          [Values(false, true)] bool async)
        {
            var subject1 = Substitute.For<IFindFluent<Person, Person>>();
            var subject2 = Substitute.For<IFindFluent<Person, Person>>();
            var findOptions = new FindOptions<Person, Person>();
            var cursor = Substitute.For<IAsyncCursor<Person>>();
            var firstBatch = new Person[]
            {
                new Person { FirstName = "John" }
            };
            var cancellationToken = new CancellationTokenSource().Token;

            subject1.Options.Returns(findOptions);
            subject1.Limit(2).Returns(subject2);
            cursor.Current.Returns(firstBatch);

            Person result;
            if (async)
            {
                subject2.ToCursorAsync(cancellationToken).Returns(Task.FromResult(cursor));
                cursor.MoveNextAsync(cancellationToken).Returns(Task.FromResult(true));

                result = subject1.SingleAsync(cancellationToken).GetAwaiter().GetResult();

            }
            else
            {
                subject2.ToCursor(cancellationToken).Returns(cursor);
                cursor.MoveNext(cancellationToken).Returns(true);

                result = subject1.Single(cancellationToken);
            }

            result.FirstName = "John";
        }

        [Test]
        public void Single_should_throw_when_find_is_null(
            [Values(false, true)] bool async)
        {
            IFindFluent<Person, Person> subject = null;

            Action action;
            if (async)
            {
                action = () => subject.SingleAsync().GetAwaiter().GetResult();
            }
            else
            {
                action = () => subject.Single();
            }

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("find");
        }

        [Test]
        public void SingleOrDefault_should_add_limit_and_call_ToCursor(
            [Values(false, true)] bool async)
        {
            var subject1 = Substitute.For<IFindFluent<Person, Person>>();
            var subject2 = Substitute.For<IFindFluent<Person, Person>>();
            var findOptions = new FindOptions<Person, Person>();
            var cursor = Substitute.For<IAsyncCursor<Person>>();
            var firstBatch = new Person[]
            {
                new Person { FirstName = "John" }
            };
            var cancellationToken = new CancellationTokenSource().Token;

            subject1.Options.Returns(findOptions);
            subject1.Limit(2).Returns(subject2);
            cursor.Current.Returns(firstBatch);

            Person result;
            if (async)
            {
                subject2.ToCursorAsync(cancellationToken).Returns(Task.FromResult(cursor));
                cursor.MoveNextAsync(cancellationToken).Returns(Task.FromResult(true));

                result = subject1.SingleOrDefaultAsync(cancellationToken).GetAwaiter().GetResult();

            }
            else
            {
                subject2.ToCursor(cancellationToken).Returns(cursor);
                cursor.MoveNext(cancellationToken).Returns(true);

                result = subject1.SingleOrDefault(cancellationToken);
            }

            result.FirstName = "John";
        }

        [Test]
        public void SingleOrDefault_should_throw_when_find_is_null(
            [Values(false, true)] bool async)
        {
            IFindFluent<Person, Person> subject = null;

            Action action;
            if (async)
            {
                action = () => subject.SingleOrDefaultAsync().GetAwaiter().GetResult();
            }
            else
            {
                action = () => subject.SingleOrDefault();
            }

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("find");
        }

        [Test]
        public void SortBy_should_generate_the_correct_sort()
        {
            var subject = CreateSubject();
            subject.SortBy(x => x.FirstName);

            var expectedSort = BsonDocument.Parse("{FirstName: 1}");

            AssertSort(subject, expectedSort);
        }

        [Test]
        public void SortBy_ThenBy_should_generate_the_correct_sort()
        {
            var subject = CreateSubject();
            subject.SortBy(x => x.FirstName).ThenBy(x => x.LastName);

            var expectedSort = BsonDocument.Parse("{FirstName: 1, LastName: 1}");

            AssertSort(subject, expectedSort);
        }

        [Test]
        public void SortBy_ThenByDescending_should_generate_the_correct_sort()
        {
            var subject = CreateSubject();
            subject.SortBy(x => x.FirstName).ThenByDescending(x => x.LastName);

            var expectedSort = BsonDocument.Parse("{FirstName: 1, LastName: -1}");

            AssertSort(subject, expectedSort);
        }

        [Test]
        public void SortBy_ThenBy_ThenBy_should_generate_the_correct_sort()
        {
            var subject = CreateSubject();
            subject.SortBy(x => x.FirstName).ThenBy(x => x.LastName).ThenBy(x => x.Age);

            var expectedSort = BsonDocument.Parse("{FirstName: 1, LastName: 1, Age: 1}");

            AssertSort(subject, expectedSort);
        }

        [Test]
        public void SortByDescending_should_generate_the_correct_sort()
        {
            var subject = CreateSubject();
            subject.SortByDescending(x => x.FirstName);

            var expectedSort = BsonDocument.Parse("{FirstName: -1}");

            AssertSort(subject, expectedSort);
        }

        [Test]
        public void SortByDescending_ThenBy_should_generate_the_correct_sort()
        {
            var subject = CreateSubject();
            subject.SortByDescending(x => x.FirstName).ThenBy(x => x.LastName);

            var expectedSort = BsonDocument.Parse("{FirstName: -1, LastName: 1}");

            AssertSort(subject, expectedSort);
        }

        [Test]
        public void SortByDescending_ThenByDescending_should_generate_the_correct_sort()
        {
            var subject = CreateSubject();
            subject.SortByDescending(x => x.FirstName).ThenByDescending(x => x.LastName);

            var expectedSort = BsonDocument.Parse("{FirstName: -1, LastName: -1}");

            AssertSort(subject, expectedSort);
        }

        private static void AssertProjection<TResult>(IFindFluent<Person, TResult> subject, BsonDocument expectedProjection)
        {
            Assert.AreEqual(expectedProjection, subject.Options.Projection.Render(BsonSerializer.SerializerRegistry.GetSerializer<Person>(), BsonSerializer.SerializerRegistry).Document);
        }

        private static void AssertSort(IFindFluent<Person, Person> subject, BsonDocument expectedSort)
        {
            Assert.AreEqual(expectedSort, subject.Options.Sort.Render(BsonSerializer.SerializerRegistry.GetSerializer<Person>(), BsonSerializer.SerializerRegistry));
        }

        private IFindFluent<Person, Person> CreateSubject()
        {
            var settings = new MongoCollectionSettings();
            var collection = Substitute.For<IMongoCollection<Person>>();
            collection.Settings.Returns(settings);
            var options = new FindOptions<Person, Person>();
            var subject = new FindFluent<Person, Person>(collection, new BsonDocument(), options);

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
