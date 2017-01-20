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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using FluentAssertions;
using Moq;
using Xunit;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using MongoDB.Bson.TestHelpers.XunitExtensions;

namespace MongoDB.Driver.Tests
{
    public class IAggregateFluentExtensionsTests
    {
        // public methods
        [Theory]
        [ParameterAttributeData]
        public void First_should_add_limit_and_call_ToCursor(
            [Values(false, true)] bool async)
        {
            var mockSubject1 = new Mock<IAggregateFluent<Person>>();
            var mockSubject2 = new Mock<IAggregateFluent<Person>>();
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
        public void First_should_throw_when_aggregate_is_null(
            [Values(false, true)] bool async)
        {
            IAggregateFluent<Person> subject = null;

            Action action;
            if (async)
            {
                action = () => subject.FirstAsync().GetAwaiter().GetResult();
            }
            else
            {
                action = () => subject.First();
            }

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("aggregate");
        }

        [Theory]
        [ParameterAttributeData]
        public void FirstOrDefault_should_add_limit_and_call_ToCursor(
            [Values(false, true)] bool async)
        {
            var mockSubject1 = new Mock<IAggregateFluent<Person>>();
            var mockSubject2 = new Mock<IAggregateFluent<Person>>();
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
        public void FirstOrDefault_should_throw_when_aggregate_is_null(
            [Values(false, true)] bool async)
        {
            IAggregateFluent<Person> subject = null;

            Action action;
            if (async)
            {
                action = () => subject.FirstOrDefaultAsync().GetAwaiter().GetResult();
            }
            else
            {
                action = () => subject.FirstOrDefault();
            }

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("aggregate");
        }

        [Fact]
        public void Group_should_generate_the_correct_group_when_a_result_type_is_not_specified()
        {
            var subject = CreateSubject()
                .Group("{_id: \"$Tags\" }");

            var expectedGroup = BsonDocument.Parse("{$group: {_id: '$Tags'}}");

            AssertLast(subject, expectedGroup);
        }

        [Fact]
        public void Group_should_generate_the_correct_document_using_expressions()
        {
            var subject = CreateSubject()
                .Group(x => x.Age, g => new { Name = g.Select(x => x.FirstName + " " + x.LastName).First() });

            var expectedGroup = BsonDocument.Parse("{$group: {_id: '$Age', Name: {'$first': { '$concat': ['$FirstName', ' ', '$LastName']}}}}");

            AssertLast(subject, expectedGroup);
        }

        [Fact]
        public void Lookup_should_generate_the_correct_group_when_using_BsonDocument()
        {
            var subject = CreateSubject()
                .Lookup("from", "local", "foreign", "as");

            var expectedLookup = BsonDocument.Parse("{$lookup: { from: 'from', localField: 'local', foreignField: 'foreign', as: 'as' } }");

            AssertLast(subject, expectedLookup);
        }

        [Fact]
        public void Lookup_should_generate_the_correct_group_when_using_lambdas()
        {
            var subject = CreateSubject()
                .Lookup<Person, NameMeaning, LookedUpPerson>(
                    CreateCollection<NameMeaning>(),
                    x => x.FirstName,
                    x => x.Name,
                    x => x.Meanings);

            var expectedLookup = BsonDocument.Parse("{$lookup: { from: 'NameMeaning', localField: 'FirstName', foreignField: 'Name', as: 'Meanings' } }");

            AssertLast(subject, expectedLookup);
        }

        [Fact]
        public void Match_should_generate_the_correct_match()
        {
            var subject = CreateSubject()
                .Match(x => x.Age > 20);

            var expectedMatch = BsonDocument.Parse("{$match: {Age: {$gt: 20}}}");

            AssertLast(subject, expectedMatch);
        }

        [Fact]
        public void Project_should_generate_the_correct_document_when_a_result_type_is_not_specified()
        {
            var subject = CreateSubject()
                .Project(BsonDocument.Parse("{ Awesome: \"$Tags\" }"));

            var expectedProject = BsonDocument.Parse("{$project: {Awesome: '$Tags'}}");

            AssertLast(subject, expectedProject);
        }

        [Fact]
        public void Project_should_generate_the_correct_document_using_expressions()
        {
            var subject = CreateSubject()
                .Project(x => new { Name = x.FirstName + " " + x.LastName });

            var expectedProject = BsonDocument.Parse("{$project: {Name: {'$concat': ['$FirstName', ' ', '$LastName']}, _id: 0}}");

            AssertLast(subject, expectedProject);
        }

        [Fact]
        public void ReplaceRoot_should_generate_the_correct_stage()
        {
            var subject = CreateSubject()
                .ReplaceRoot(x => x.PhoneNumber);

            var expectedStage = BsonDocument.Parse("{ $replaceRoot : { newRoot:  '$PhoneNumber' } }");

            AssertLast(subject, expectedStage);
        }

        [Fact]
        public void ReplaceRoot_should_generate_the_correct_stage_with_anonymous_class()
        {
            var subject = CreateSubject()
                .ReplaceRoot(x => new { Name = x.FirstName + " " + x.LastName });

            var expectedStage = BsonDocument.Parse("{ $replaceRoot : { newRoot:  { Name : { $concat : [ '$FirstName', ' ', '$LastName' ] } } } }");

            AssertLast(subject, expectedStage);
        }

        [Theory]
        [ParameterAttributeData]
        public void Single_should_add_limit_and_call_ToCursor(
           [Values(false, true)] bool async)
        {
            var mockSubject1 = new Mock<IAggregateFluent<Person>>();
            var mockSubject2 = new Mock<IAggregateFluent<Person>>();
            var mockCursor = new Mock<IAsyncCursor<Person>>();
            var firstBatch = new Person[]
            {
                new Person { FirstName = "John" }
            };
            var cancellationToken = new CancellationTokenSource().Token;

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
        public void Single_should_throw_when_aggregate_is_null(
            [Values(false, true)] bool async)
        {
            IAggregateFluent<Person> subject = null;

            Action action;
            if (async)
            {
                action = () => subject.SingleAsync().GetAwaiter().GetResult();
            }
            else
            {
                action = () => subject.Single();
            }

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("aggregate");
        }

        [Theory]
        [ParameterAttributeData]
        public void SingleOrDefault_should_add_limit_and_call_ToCursor(
            [Values(false, true)] bool async)
        {
            var mockSubject1 = new Mock<IAggregateFluent<Person>>();
            var mockSubject2 = new Mock<IAggregateFluent<Person>>();
            var mockCursor = new Mock<IAsyncCursor<Person>>();
            var firstBatch = new Person[]
            {
                new Person { FirstName = "John" }
            };
            var cancellationToken = new CancellationTokenSource().Token;

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
        public void SingleOrDefault_should_throw_when_aggregate_is_null(
            [Values(false, true)] bool async)
        {
            IAggregateFluent<Person> subject = null;

            Action action;
            if (async)
            {
                action = () => subject.SingleOrDefaultAsync().GetAwaiter().GetResult();
            }
            else
            {
                action = () => subject.SingleOrDefault();
            }

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("aggregate");
        }

        [Fact]
        public void SortBy_should_generate_the_correct_sort()
        {
            var subject = CreateSubject()
                .SortBy(x => x.FirstName);

            var expectedSort = BsonDocument.Parse("{$sort: {FirstName: 1}}");

            AssertLast(subject, expectedSort);
        }

        [Fact]
        public void SortBy_ThenBy_should_generate_the_correct_sort()
        {
            var subject = CreateSubject()
                .SortBy(x => x.FirstName)
                .ThenBy(x => x.LastName);

            var expectedSort = BsonDocument.Parse("{$sort: {FirstName: 1, LastName: 1}}");

            AssertLast(subject, expectedSort);
        }

        [Fact]
        public void SortBy_ThenByDescending_should_generate_the_correct_sort()
        {
            var subject = CreateSubject()
                .SortBy(x => x.FirstName)
                .ThenByDescending(x => x.LastName);

            var expectedSort = BsonDocument.Parse("{$sort: {FirstName: 1, LastName: -1}}");

            AssertLast(subject, expectedSort);
        }

        [Fact]
        public void SortBy_ThenBy_ThenBy_should_generate_the_correct_sort()
        {
            var subject = CreateSubject()
                .SortBy(x => x.FirstName)
                .ThenBy(x => x.LastName)
                .ThenBy(x => x.Age);

            var expectedSort = BsonDocument.Parse("{$sort: {FirstName: 1, LastName: 1, Age: 1}}");

            AssertLast(subject, expectedSort);
        }

        [Fact]
        public void SortByCount_should_generate_the_correct_stage()
        {
            var subject = CreateSubject()
                .SortByCount(x => x.Age);

            var expectedStage = BsonDocument.Parse("{ $sortByCount : '$Age' }");

            AssertLast(subject, expectedStage);
        }

        [Fact]
        public void SortByDescending_should_generate_the_correct_sort()
        {
            var subject = CreateSubject()
                .SortByDescending(x => x.FirstName);

            var expectedSort = BsonDocument.Parse("{$sort: {FirstName: -1}}");

            AssertLast(subject, expectedSort);
        }

        [Fact]
        public void SortByDescending_ThenBy_should_generate_the_correct_sort()
        {
            var subject = CreateSubject()
                .SortByDescending(x => x.FirstName)
                .ThenBy(x => x.LastName);

            var expectedSort = BsonDocument.Parse("{$sort: {FirstName: -1, LastName: 1}}");

            AssertLast(subject, expectedSort);
        }

        [Fact]
        public void SortByDescending_ThenByDescending_should_generate_the_correct_sort()
        {
            var subject = CreateSubject()
                .SortByDescending(x => x.FirstName)
                .ThenByDescending(x => x.LastName);

            var expectedSort = BsonDocument.Parse("{$sort: {FirstName: -1, LastName: -1}}");

            AssertLast(subject, expectedSort);
        }

        [Fact]
        public void Unwind_with_expression_to_BsonDocument_should_generate_the_correct_unwind()
        {
            var subject = CreateSubject()
                .Unwind(x => x.Age);

            var expectedUnwind = BsonDocument.Parse("{$unwind: '$Age'}");

            AssertLast(subject, expectedUnwind);
        }

        [Fact]
        public void Unwind_with_expression_to_new_result_should_generate_the_correct_unwind()
        {
            var subject = CreateSubject()
                .Unwind<Person, BsonDocument>(x => x.Age);

            var expectedUnwind = BsonDocument.Parse("{$unwind: '$Age'}");

            AssertLast(subject, expectedUnwind);
        }

        [Fact]
        public void Unwind_should_generate_the_correct_unwind()
        {
            var subject = CreateSubject()
                .Unwind("Age");

            var expectedUnwind = BsonDocument.Parse("{$unwind: '$Age'}");

            AssertLast(subject, expectedUnwind);
        }

        [Fact]
        public void Unwind_to_new_result_with_a_serializer_should_generate_the_correct_unwind()
        {
            var subject = CreateSubject()
                .Unwind("Age", new AggregateUnwindOptions<BsonDocument> { ResultSerializer = BsonDocumentSerializer.Instance });

            var expectedUnwind = BsonDocument.Parse("{$unwind: '$Age'}");

            AssertLast(subject, expectedUnwind);
        }

        [Fact]
        public void Unwind_with_options_where_no_options_are_set()
        {
            var subject = CreateSubject()
                .Unwind("Age", new AggregateUnwindOptions<BsonDocument>());

            var expectedUnwind = BsonDocument.Parse("{$unwind: '$Age'}");

            AssertLast(subject, expectedUnwind);
        }

        [Fact]
        public void Unwind_with_options_with_preserveNullAndEmptyArrays_set()
        {
            var subject = CreateSubject()
                .Unwind("Age", new AggregateUnwindOptions<BsonDocument> { PreserveNullAndEmptyArrays = true });

            var expectedUnwind = BsonDocument.Parse("{$unwind: { path: '$Age', preserveNullAndEmptyArrays: true } }");

            AssertLast(subject, expectedUnwind);
        }

        [Fact]
        public void Unwind_with_options_with_includeArrayIndex_set()
        {
            var subject = CreateSubject()
                .Unwind("Age", new AggregateUnwindOptions<BsonDocument> { IncludeArrayIndex = "AgeIndex" });

            var expectedUnwind = BsonDocument.Parse("{$unwind: { path: '$Age', includeArrayIndex: 'AgeIndex' } }");

            AssertLast(subject, expectedUnwind);
        }

        [Fact]
        public void Unwind_with_options_with_includeArrayIndex_set_and_preserveNullAndEmptyArrays_set()
        {
            var subject = CreateSubject()
                .Unwind("Age", new AggregateUnwindOptions<BsonDocument>
                {
                    IncludeArrayIndex = "AgeIndex",
                    PreserveNullAndEmptyArrays = true
                });

            var expectedUnwind = BsonDocument.Parse("{$unwind: { path: '$Age', preserveNullAndEmptyArrays: true, includeArrayIndex: 'AgeIndex' } }");

            AssertLast(subject, expectedUnwind);
        }

        // private methods
        private void AssertLast<TDocument>(IAggregateFluent<TDocument> fluent, BsonDocument expectedLast)
        {
            var pipeline = new PipelineStagePipelineDefinition<Person, TDocument>(fluent.Stages);
            var renderedPipeline = pipeline.Render(BsonSerializer.SerializerRegistry.GetSerializer<Person>(), BsonSerializer.SerializerRegistry);

            var last = renderedPipeline.Documents.Last();
            Assert.Equal(expectedLast, last);
        }

        private IAggregateFluent<Person> CreateSubject(CancellationToken cancellationToken = default(CancellationToken))
        {
            var collection = CreateCollection<Person>();
            return new AggregateFluent<Person, Person>(collection, new EmptyPipelineDefinition<Person>(), new AggregateOptions());
        }

        private IMongoCollection<T> CreateCollection<T>()
        {
            var mockDatabase = new Mock<IMongoDatabase>();
            SetupDatabaseGetCollectionMethod<BsonDocument>(mockDatabase);

            var settings = new MongoCollectionSettings();
            var mockCollection = new Mock<IMongoCollection<T>>();
            mockCollection.SetupGet(c => c.CollectionNamespace).Returns(new CollectionNamespace(new DatabaseNamespace("test"), typeof(T).Name));
            mockCollection.SetupGet(c => c.Database).Returns(mockDatabase.Object);
            mockCollection.SetupGet(c => c.DocumentSerializer).Returns(settings.SerializerRegistry.GetSerializer<T>());
            mockCollection.SetupGet(c => c.Settings).Returns(settings);
            return mockCollection.Object;
        }

        private void SetupDatabaseGetCollectionMethod<TDocument>(Mock<IMongoDatabase> mockDatabase)
        {
            mockDatabase
                .Setup(d => d.GetCollection<TDocument>(It.IsAny<string>(), It.IsAny<MongoCollectionSettings>()))
                .Returns((string collectionName, MongoCollectionSettings settings) =>
                {
                    var mockCollection = new Mock<IMongoCollection<TDocument>>();
                    mockCollection.SetupGet(c => c.CollectionNamespace).Returns(new CollectionNamespace(new DatabaseNamespace("test"), collectionName));
                    return mockCollection.Object;
                });
        }

        private IAsyncCursor<Person> CreateCursor(params Person[] persons)
        {
            var firstBatch = persons ?? new Person[0];

            return new AsyncCursor<Person>(
                new Mock<IChannelSource>().Object,
                new CollectionNamespace(new DatabaseNamespace("foo"), "bar"),
                new BsonDocument(),
                firstBatch,
                0,
                null,
                null,
                BsonSerializer.LookupSerializer<Person>(),
                new MessageEncoderSettings(),
                null);
        }

        // nested types
        public class Person
        {
            public string FirstName;
            public string LastName;
            public int Age;
            public PhoneNumber PhoneNumber;
        }

        public class PhoneNumber
        {
            public int AreaCode;
            public int Number;
        }

        public class NameMeaning
        {
            public string Name;
            public string Definition;
        }

        public class LookedUpPerson
        {
            public string FirstName;
            public string LastName;
            public int Age;
            public IEnumerable<NameMeaning> Meanings;
        }
    }
}
