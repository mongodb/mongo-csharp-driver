/* Copyright 2010-present MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Tests.Linq.Linq3Implementation;
using MongoDB.TestHelpers.XunitExtensions;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class IFindFluentExtensionsTests : Linq3IntegrationTest
    {
        // public methods
        [Theory]
        [ParameterAttributeData]
        public void Any_should_add_projection_and_limit_and_return_expected_result(
            [Values(0, 1, 2)] int count,
            [Values(false, true)] bool async)
        {
            var expectedResult = count > 0;

            var mockSubject1 = new Mock<IFindFluent<Person, Person>>();
            var mockSubject2 = new Mock<IFindFluent<Person, BsonDocument>>();
            var mockSubject3 = new Mock<IFindFluent<Person, BsonDocument>>();
            var mockCursor = new Mock<IAsyncCursor<BsonDocument>>();
            var firstBatch = Enumerable.Range(0, count).Select(i => new BsonDocument("_id", i)).ToArray();
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            mockSubject1.Setup(s => s.Project(It.Is<BsonDocumentProjectionDefinition<Person, BsonDocument>>(p => p.Document["_id"].AsInt32 == 1))).Returns(mockSubject2.Object);
            mockSubject2.Setup(s => s.Limit(1)).Returns(mockSubject3.Object);
            mockCursor.SetupGet(c => c.Current).Returns(firstBatch);

            bool result;
            if (async)
            {
                mockSubject3.Setup(s => s.ToCursorAsync(cancellationToken)).Returns(Task.FromResult(mockCursor.Object));
                mockCursor.Setup(c => c.MoveNextAsync(cancellationToken)).Returns(Task.FromResult(true));

                result = mockSubject1.Object.AnyAsync(cancellationToken).GetAwaiter().GetResult();
            }
            else
            {
                mockSubject3.Setup(s => s.ToCursor(cancellationToken)).Returns(mockCursor.Object);
                mockCursor.Setup(c => c.MoveNext(cancellationToken)).Returns(true);

                result = mockSubject1.Object.Any(cancellationToken);
            }

            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void Any_should_throw_when_find_is_null(
            [Values(false, true)] bool async)
        {
            IFindFluent<Person, Person> subject = null;

            Action action;
            if (async)
            {
                action = () => subject.AnyAsync().GetAwaiter().GetResult();
            }
            else
            {
                action = () => subject.Any();
            }

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("find");
        }

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
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

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
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

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
            RequireServer.Check().Supports(Feature.FindProjectionExpressions);

            var subject = CreateSubject()
                .Project(x => x.FirstName + " " + x.LastName);

            var expectedProjection =
                BsonDocument.Parse("{ _v : { $concat : ['$FirstName', ' ', '$LastName'] }, _id : 0 }");

            AssertProjection(subject, expectedProjection);

            var results = subject.ToList();
            results.Should().Equal("John Doe");
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
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

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
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

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
            Assert.Equal(expectedProjection, subject.Options.Projection.Render(new(BsonSerializer.SerializerRegistry.GetSerializer<Person>(), BsonSerializer.SerializerRegistry, renderForFind: true)).Document);
        }

        private static void AssertSort(IFindFluent<Person, Person> subject, BsonDocument expectedSort)
        {
            Assert.Equal(expectedSort, subject.Options.Sort.Render(new(BsonSerializer.SerializerRegistry.GetSerializer<Person>(), BsonSerializer.SerializerRegistry)));
        }

        private IMongoCollection<Person> CreateCollection()
        {
            var collection = GetCollection<Person>();

            CreateCollection(
                collection,
                new Person { FirstName = "John", LastName = "Doe", Age = 21 });

            return collection;
        }

        private IFindFluent<Person, Person> CreateSubject()
        {
            var collection = CreateCollection();
            return collection.Find("{}");
        }

        public class Person
        {
            public string FirstName;
            public string LastName;
            public int Age;
        }
    }
}
