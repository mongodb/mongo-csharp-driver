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
using Moq;
using Xunit;
using MongoDB.Bson.TestHelpers.XunitExtensions;

namespace MongoDB.Driver.Tests
{
    public class IFindFluentExtensionsTests
    {
        // public methods
        [Theory]
        [ParameterAttributeData]
        public void First_should_add_limit_and_call_ToCursor(
            [Values(false, true)] bool async)
        {
            var mockSubject1 = new Mock<IFindFluent<Person, Person>>();
            var mockSubject2 = new Mock<IFindFluent<Person, Person>>();
            var mockCursor = new Mock<IAsyncCursor<Person>>();
            var firstBatch = new Person[]
            {
                new Person { FirstName = "John" },
                new Person { FirstName = "Jane" }
            };
            var cancellationToken = new CancellationTokenSource().Token;

            mockSubject1.Setup(s => s.Limit(1)).Returns(mockSubject2.Object);
            mockCursor.SetupGet(c => c.Current).Returns(firstBatch);

            Person result;
            if (async)
            {
                mockSubject2.Setup(s => s.ToCursorAsync(cancellationToken)).Returns(Task.FromResult(mockCursor.Object));
                mockCursor.Setup(c => c.MoveNextAsync(cancellationToken)).Returns(Task.FromResult(true));

                result = mockSubject1.Object.FirstAsync(cancellationToken).GetAwaiter().GetResult();

            }
            else
            {
                mockSubject2.Setup(s => s.ToCursor(cancellationToken)).Returns(mockCursor.Object);
                mockCursor.Setup(c => c.MoveNext(cancellationToken)).Returns(true);

                result = mockSubject1.Object.First(cancellationToken);
            }

            result.FirstName.Should().Be("John");
        }

        [Theory]
        [ParameterAttributeData]
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

        [Theory]
        [ParameterAttributeData]
        public void FirstOrDefault_should_add_limit_and_call_ToCursor(
            [Values(false, true)] bool async)
        {
            var mockSubject1 = new Mock<IFindFluent<Person, Person>>();
            var mockSubject2 = new Mock<IFindFluent<Person, Person>>();
            var mockCursor = new Mock<IAsyncCursor<Person>>();
            var firstBatch = new Person[]
            {
                new Person { FirstName = "John" },
                new Person { FirstName = "Jane" }
            };
            var cancellationToken = new CancellationTokenSource().Token;

            mockSubject1.Setup(s => s.Limit(1)).Returns(mockSubject2.Object);
            mockCursor.SetupGet(c => c.Current).Returns(firstBatch);

            Person result;
            if (async)
            {
                mockSubject2.Setup(s => s.ToCursorAsync(cancellationToken)).Returns(Task.FromResult(mockCursor.Object));
                mockCursor.Setup(c => c.MoveNextAsync(cancellationToken)).Returns(Task.FromResult(true));

                result = mockSubject1.Object.FirstOrDefaultAsync(cancellationToken).GetAwaiter().GetResult();

            }
            else
            {
                mockSubject2.Setup(s => s.ToCursor(cancellationToken)).Returns(mockCursor.Object);
                mockCursor.Setup(c => c.MoveNext(cancellationToken)).Returns(true);

                result = mockSubject1.Object.FirstOrDefault(cancellationToken);
            }

            result.FirstName.Should().Be("John");
        }

        [Theory]
        [ParameterAttributeData]
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

        [Fact]
        public void Project_should_generate_the_correct_fields_when_a_BsonDocument_is_used()
        {
            var subject = CreateSubject()
                .Project(BsonDocument.Parse("{_id: 1, Tags: 1}"));

            var expectedProjection = BsonDocument.Parse("{_id: 1, Tags: 1}");

            AssertProjection(subject, expectedProjection);
        }

        [Fact]
        public void Project_should_generate_the_correct_fields_when_a_string_is_used()
        {
            var subject = CreateSubject()
                .Project("{_id: 1, Tags: 1}");

            var expectedProjection = BsonDocument.Parse("{_id: 1, Tags: 1}");

            AssertProjection(subject, expectedProjection);
        }

        [Fact]
        public void Project_should_generate_the_correct_fields_and_assign_the_correct_result_serializer()
        {
            var subject = CreateSubject()
                .Project(x => x.FirstName + " " + x.LastName);

            var expectedProjection = BsonDocument.Parse("{FirstName: 1, LastName: 1, _id: 0}");

            AssertProjection(subject, expectedProjection);
        }

        [Theory]
        [ParameterAttributeData]
        public void Single_should_add_limit_and_call_ToCursor(
            [Values(false, true)] bool async)
        {
            var mockSubject1 = new Mock<IFindFluent<Person, Person>>();
            var mockSubject2 = new Mock<IFindFluent<Person, Person>>();
            var findOptions = new FindOptions<Person, Person>();
            var mockCursor = new Mock<IAsyncCursor<Person>>();
            var firstBatch = new Person[]
            {
                new Person { FirstName = "John" }
            };
            var cancellationToken = new CancellationTokenSource().Token;

            mockSubject1.SetupGet(s => s.Options).Returns(findOptions);
            mockSubject1.Setup(s => s.Limit(2)).Returns(mockSubject2.Object);
            mockCursor.SetupGet(c => c.Current).Returns(firstBatch);

            Person result;
            if (async)
            {
                mockSubject2.Setup(s => s.ToCursorAsync(cancellationToken)).Returns(Task.FromResult(mockCursor.Object));
                mockCursor.Setup(c => c.MoveNextAsync(cancellationToken)).Returns(Task.FromResult(true));

                result = mockSubject1.Object.SingleAsync(cancellationToken).GetAwaiter().GetResult();
            }
            else
            {
                mockSubject2.Setup(s => s.ToCursor(cancellationToken)).Returns(mockCursor.Object);
                mockCursor.Setup(c => c.MoveNext(cancellationToken)).Returns(true);

                result = mockSubject1.Object.Single(cancellationToken);
            }

            result.FirstName.Should().Be("John");
        }

        [Theory]
        [ParameterAttributeData]
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

        [Theory]
        [ParameterAttributeData]
        public void SingleOrDefault_should_add_limit_and_call_ToCursor(
            [Values(false, true)] bool async)
        {
            var mockSubject1 = new Mock<IFindFluent<Person, Person>>();
            var mockSubject2 = new Mock<IFindFluent<Person, Person>>();
            var findOptions = new FindOptions<Person, Person>();
            var mockCursor = new Mock<IAsyncCursor<Person>>();
            var firstBatch = new Person[]
            {
                new Person { FirstName = "John" }
            };
            var cancellationToken = new CancellationTokenSource().Token;

            mockSubject1.SetupGet(s => s.Options).Returns(findOptions);
            mockSubject1.Setup(s => s.Limit(2)).Returns(mockSubject2.Object);
            mockCursor.SetupGet(c => c.Current).Returns(firstBatch);

            Person result;
            if (async)
            {
                mockSubject2.Setup(s => s.ToCursorAsync(cancellationToken)).Returns(Task.FromResult(mockCursor.Object));
                mockCursor.Setup(c => c.MoveNextAsync(cancellationToken)).Returns(Task.FromResult(true));

                result = mockSubject1.Object.SingleOrDefaultAsync(cancellationToken).GetAwaiter().GetResult();

            }
            else
            {
                mockSubject2.Setup(s => s.ToCursor(cancellationToken)).Returns(mockCursor.Object);
                mockCursor.Setup(c => c.MoveNext(cancellationToken)).Returns(true);

                result = mockSubject1.Object.SingleOrDefault(cancellationToken);
            }

            result.FirstName.Should().Be("John");
        }

        [Theory]
        [ParameterAttributeData]
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

        [Fact]
        public void SortBy_should_generate_the_correct_sort()
        {
            var subject = CreateSubject();
            subject.SortBy(x => x.FirstName);

            var expectedSort = BsonDocument.Parse("{FirstName: 1}");

            AssertSort(subject, expectedSort);
        }

        [Fact]
        public void SortBy_ThenBy_should_generate_the_correct_sort()
        {
            var subject = CreateSubject();
            subject.SortBy(x => x.FirstName).ThenBy(x => x.LastName);

            var expectedSort = BsonDocument.Parse("{FirstName: 1, LastName: 1}");

            AssertSort(subject, expectedSort);
        }

        [Fact]
        public void SortBy_ThenByDescending_should_generate_the_correct_sort()
        {
            var subject = CreateSubject();
            subject.SortBy(x => x.FirstName).ThenByDescending(x => x.LastName);

            var expectedSort = BsonDocument.Parse("{FirstName: 1, LastName: -1}");

            AssertSort(subject, expectedSort);
        }

        [Fact]
        public void SortBy_ThenBy_ThenBy_should_generate_the_correct_sort()
        {
            var subject = CreateSubject();
            subject.SortBy(x => x.FirstName).ThenBy(x => x.LastName).ThenBy(x => x.Age);

            var expectedSort = BsonDocument.Parse("{FirstName: 1, LastName: 1, Age: 1}");

            AssertSort(subject, expectedSort);
        }

        [Fact]
        public void SortByDescending_should_generate_the_correct_sort()
        {
            var subject = CreateSubject();
            subject.SortByDescending(x => x.FirstName);

            var expectedSort = BsonDocument.Parse("{FirstName: -1}");

            AssertSort(subject, expectedSort);
        }

        [Fact]
        public void SortByDescending_ThenBy_should_generate_the_correct_sort()
        {
            var subject = CreateSubject();
            subject.SortByDescending(x => x.FirstName).ThenBy(x => x.LastName);

            var expectedSort = BsonDocument.Parse("{FirstName: -1, LastName: 1}");

            AssertSort(subject, expectedSort);
        }

        [Fact]
        public void SortByDescending_ThenByDescending_should_generate_the_correct_sort()
        {
            var subject = CreateSubject();
            subject.SortByDescending(x => x.FirstName).ThenByDescending(x => x.LastName);

            var expectedSort = BsonDocument.Parse("{FirstName: -1, LastName: -1}");

            AssertSort(subject, expectedSort);
        }

        private static void AssertProjection<TResult>(IFindFluent<Person, TResult> subject, BsonDocument expectedProjection)
        {
            Assert.Equal(expectedProjection, subject.Options.Projection.Render(BsonSerializer.SerializerRegistry.GetSerializer<Person>(), BsonSerializer.SerializerRegistry).Document);
        }

        private static void AssertSort(IFindFluent<Person, Person> subject, BsonDocument expectedSort)
        {
            Assert.Equal(expectedSort, subject.Options.Sort.Render(BsonSerializer.SerializerRegistry.GetSerializer<Person>(), BsonSerializer.SerializerRegistry));
        }

        private IFindFluent<Person, Person> CreateSubject()
        {
            var settings = new MongoCollectionSettings();
            var mockCollection = new Mock<IMongoCollection<Person>>();
            mockCollection.SetupGet(c => c.Settings).Returns(settings);
            var options = new FindOptions<Person, Person>();
            return new FindFluent<Person, Person>(mockCollection.Object, new BsonDocument(), options);
        }

        public class Person
        {
            public string FirstName;
            public string LastName;
            public int Age;
        }
    }
}
