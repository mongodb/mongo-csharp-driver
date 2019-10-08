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
using System.Linq.Expressions;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Tests.Linq;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class IMongoCollectionExtensionsTests
    {
        [Theory]
        [ParameterAttributeData]
        public void Aggregate_should_return_expected_result(
            [Values(false, true)] bool usingSession)
        {
            var collection = CreateMockCollection().Object;
            var session = usingSession ? new Mock<IClientSessionHandle>().Object : null;
            var options = new AggregateOptions();

            IAggregateFluent<Person> result;
            if (usingSession)
            {
                result = collection.Aggregate(session, options);
            }
            else
            {
                result = collection.Aggregate(options);
            }

            var fluent = result.Should().BeOfType<CollectionAggregateFluent<Person, Person>>().Subject;
            fluent._collection().Should().BeSameAs(collection);
            fluent._options().Should().BeSameAs(options);
            fluent._pipeline().Should().BeOfType<EmptyPipelineDefinition<Person>>();
            fluent._session().Should().BeSameAs(session);
        }

        [Fact]
        public void AsQueryable_should_return_expected_result()
        {
            var collection = CreateMockCollection().Object;
            var options = new AggregateOptions();

            var result = collection.AsQueryable(options);

            var queryable = result.Should().BeOfType<MongoQueryableImpl<Person, Person>>().Subject;
            var provider = queryable.Provider.Should().BeOfType<MongoQueryProviderImpl<Person>>().Subject;
            provider._collection().Should().BeSameAs(collection);
            provider._options().Should().BeSameAs(options);
        }

        [Theory]
        [ParameterAttributeData]
        public void Count_should_call_collection_with_expected_arguments(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var mockCollection = CreateMockCollection();
            var collection = mockCollection.Object;
            var session = new Mock<IClientSessionHandle>().Object;
            var filterExpression = (Expression<Func<Person, bool>>)(x => x.FirstName == "Jack");
            var options = new CountOptions();
            var cancellationToken = new CancellationTokenSource().Token;

            if (usingSession)
            {
                if (async)
                {
#pragma warning disable 618
                    IMongoCollectionExtensions.CountAsync(collection, session, filterExpression, options, cancellationToken);
                    mockCollection.Verify(s => s.CountAsync(session, It.IsAny<ExpressionFilterDefinition<Person>>(), options, cancellationToken), Times.Once);
#pragma warning restore
                }
                else
                {
#pragma warning disable 618
                    IMongoCollectionExtensions.Count(collection, session, filterExpression, options, cancellationToken);
                    mockCollection.Verify(s => s.Count(session, It.IsAny<ExpressionFilterDefinition<Person>>(), options, cancellationToken), Times.Once);
#pragma warning restore
                }
            }
            else
            {
                if (async)
                {
#pragma warning disable 618
                    IMongoCollectionExtensions.CountAsync(collection, filterExpression, options, cancellationToken);
                    mockCollection.Verify(s => s.CountAsync(It.IsAny<ExpressionFilterDefinition<Person>>(), options, cancellationToken), Times.Once);
#pragma warning restore
                }
                else
                {
#pragma warning disable 618
                    IMongoCollectionExtensions.Count(collection, filterExpression, options, cancellationToken);
                    mockCollection.Verify(s => s.Count(It.IsAny<ExpressionFilterDefinition<Person>>(), options, cancellationToken), Times.Once);
#pragma warning restore
                }
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void CountDocuments_should_call_collection_with_expected_arguments(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var mockCollection = CreateMockCollection();
            var collection = mockCollection.Object;
            var session = new Mock<IClientSessionHandle>().Object;
            var filterExpression = (Expression<Func<Person, bool>>)(x => x.FirstName == "Jack");
            var options = new CountOptions();
            var cancellationToken = new CancellationTokenSource().Token;

            if (usingSession)
            {
                if (async)
                {
                    IMongoCollectionExtensions.CountDocumentsAsync(collection, session, filterExpression, options, cancellationToken);
                    mockCollection.Verify(s => s.CountDocumentsAsync(session, It.IsAny<ExpressionFilterDefinition<Person>>(), options, cancellationToken), Times.Once);
                }
                else
                {
                    IMongoCollectionExtensions.CountDocuments(collection, session, filterExpression, options, cancellationToken);
                    mockCollection.Verify(s => s.CountDocuments(session, It.IsAny<ExpressionFilterDefinition<Person>>(), options, cancellationToken), Times.Once);
                }
            }
            else
            {
                if (async)
                {
                    IMongoCollectionExtensions.CountDocumentsAsync(collection, filterExpression, options, cancellationToken);
                    mockCollection.Verify(s => s.CountDocumentsAsync(It.IsAny<ExpressionFilterDefinition<Person>>(), options, cancellationToken), Times.Once);
                }
                else
                {
                    IMongoCollectionExtensions.CountDocuments(collection, filterExpression, options, cancellationToken);
                    mockCollection.Verify(s => s.CountDocuments(It.IsAny<ExpressionFilterDefinition<Person>>(), options, cancellationToken), Times.Once);
                }
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void DeleteMany_should_call_collection_with_expected_arguments(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool usingOptions,
            [Values(false, true)] bool async)
        {
            var mockCollection = CreateMockCollection();
            var collection = mockCollection.Object;
            var session = new Mock<IClientSessionHandle>().Object;
            var filterExpression = (Expression<Func<Person, bool>>)(x => x.FirstName == "Jack");
            var options = new DeleteOptions();
            var cancellationToken = new CancellationTokenSource().Token;

            if (usingSession)
            {
                if (async)
                {
                    IMongoCollectionExtensions.DeleteManyAsync(collection, session, filterExpression, options, cancellationToken);
                    mockCollection.Verify(s => s.DeleteManyAsync(session, It.IsAny<ExpressionFilterDefinition<Person>>(), options, cancellationToken), Times.Once);
                }
                else
                {
                    IMongoCollectionExtensions.DeleteMany(collection, session, filterExpression, options, cancellationToken);
                    mockCollection.Verify(s => s.DeleteMany(session, It.IsAny<ExpressionFilterDefinition<Person>>(), options, cancellationToken), Times.Once);
                }
            }
            else
            {
                if (usingOptions)
                {
                    if (async)
                    {
                        IMongoCollectionExtensions.DeleteManyAsync(collection, filterExpression, options, cancellationToken);
                        mockCollection.Verify(s => s.DeleteManyAsync(It.IsAny<ExpressionFilterDefinition<Person>>(), options, cancellationToken), Times.Once);
                    }
                    else
                    {
                        IMongoCollectionExtensions.DeleteMany(collection, filterExpression, options, cancellationToken);
                        mockCollection.Verify(s => s.DeleteMany(It.IsAny<ExpressionFilterDefinition<Person>>(), options, cancellationToken), Times.Once);
                    }
                }
                else
                {
                    if (async)
                    {
                        IMongoCollectionExtensions.DeleteManyAsync(collection, filterExpression, cancellationToken);
                        mockCollection.Verify(s => s.DeleteManyAsync(It.IsAny<ExpressionFilterDefinition<Person>>(), cancellationToken), Times.Once);
                    }
                    else
                    {
                        IMongoCollectionExtensions.DeleteMany(collection, filterExpression, cancellationToken);
                        mockCollection.Verify(s => s.DeleteMany(It.IsAny<ExpressionFilterDefinition<Person>>(), cancellationToken), Times.Once);
                    }
                }
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void DeleteOne_should_call_collection_with_expected_arguments(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool usingOptions,
            [Values(false, true)] bool async)
        {
            var mockCollection = CreateMockCollection();
            var collection = mockCollection.Object;
            var session = new Mock<IClientSessionHandle>().Object;
            var filterExpression = (Expression<Func<Person, bool>>)(x => x.FirstName == "Jack");
            var options = new DeleteOptions();
            var cancellationToken = new CancellationTokenSource().Token;

            if (usingSession)
            {
                if (async)
                {
                    IMongoCollectionExtensions.DeleteOneAsync(collection, session, filterExpression, options, cancellationToken);
                    mockCollection.Verify(s => s.DeleteOneAsync(session, It.IsAny<ExpressionFilterDefinition<Person>>(), options, cancellationToken), Times.Once);
                }
                else
                {
                    IMongoCollectionExtensions.DeleteOne(collection, session, filterExpression, options, cancellationToken);
                    mockCollection.Verify(s => s.DeleteOne(session, It.IsAny<ExpressionFilterDefinition<Person>>(), options, cancellationToken), Times.Once);
                }
            }
            else
            {
                if (usingOptions)
                {
                    if (async)
                    {
                        IMongoCollectionExtensions.DeleteOneAsync(collection, filterExpression, options, cancellationToken);
                        mockCollection.Verify(s => s.DeleteOneAsync(It.IsAny<ExpressionFilterDefinition<Person>>(), options, cancellationToken), Times.Once);
                    }
                    else
                    {
                        IMongoCollectionExtensions.DeleteOne(collection, filterExpression, options, cancellationToken);
                        mockCollection.Verify(s => s.DeleteOne(It.IsAny<ExpressionFilterDefinition<Person>>(), options, cancellationToken), Times.Once);
                    }
                }
                else
                {
                    if (async)
                    {
                        IMongoCollectionExtensions.DeleteOneAsync(collection, filterExpression, cancellationToken);
                        mockCollection.Verify(s => s.DeleteOneAsync(It.IsAny<ExpressionFilterDefinition<Person>>(), cancellationToken), Times.Once);
                    }
                    else
                    {
                        IMongoCollectionExtensions.DeleteOne(collection, filterExpression, cancellationToken);
                        mockCollection.Verify(s => s.DeleteOne(It.IsAny<ExpressionFilterDefinition<Person>>(), cancellationToken), Times.Once);
                    }
                }
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Distinct_should_call_collection_with_expected_arguments(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool usingFieldExpression,
            [Values(false, true)] bool usingFilterExpression,
            [Values(false, true)] bool async)
        {
            var mockCollection = CreateMockCollection();
            var collection = mockCollection.Object;
            var session = new Mock<IClientSessionHandle>().Object;
            var fieldDefinition = (FieldDefinition<Person, string>)"LastName";
            var fieldExpression = (Expression<Func<Person, string>>)(x => x.LastName);
            var filterDefinition = Builders<Person>.Filter.Eq(x => x.FirstName, "Jack");
            var filterExpression = (Expression<Func<Person, bool>>)(x => x.FirstName == "Jack");
            var options = new DistinctOptions();
            var cancellationToken = new CancellationTokenSource().Token;

            if (usingSession)
            {
                if (usingFieldExpression)
                {
                    if (usingFilterExpression)
                    {
                        if (async)
                        {
                            collection.DistinctAsync(session, fieldExpression, filterExpression, options, cancellationToken);
                            mockCollection.Verify(
                                s => s.DistinctAsync(session, It.IsAny<ExpressionFieldDefinition<Person, string>>(), It.IsAny<ExpressionFilterDefinition<Person>>(), options, cancellationToken),
                                Times.Once);
                        }
                        else
                        {
                            collection.Distinct(session, fieldExpression, filterExpression, options, cancellationToken);
                            mockCollection.Verify(
                                s => s.Distinct(session, It.IsAny<ExpressionFieldDefinition<Person, string>>(), It.IsAny<ExpressionFilterDefinition<Person>>(), options, cancellationToken),
                                Times.Once);
                        }
                    }
                    else
                    {
                        if (async)
                        {
                            collection.DistinctAsync(session, fieldExpression, filterDefinition, options, cancellationToken);
                            mockCollection.Verify(
                                s => s.DistinctAsync(session, It.IsAny<ExpressionFieldDefinition<Person, string>>(), filterDefinition, options, cancellationToken),
                                Times.Once);
                        }
                        else
                        {
                            collection.Distinct(session, fieldExpression, filterDefinition, options, cancellationToken);
                            mockCollection.Verify(
                                s => s.Distinct(session, It.IsAny<ExpressionFieldDefinition<Person, string>>(), filterDefinition, options, cancellationToken),
                                Times.Once);
                        }
                    }
                }
                else
                {
                    if (usingFilterExpression)
                    {
                        if (async)
                        {
                            IMongoCollectionExtensions.DistinctAsync(collection, session, fieldDefinition, filterExpression, options, cancellationToken);
                            mockCollection.Verify(
                                s => s.DistinctAsync(session, fieldDefinition, It.IsAny<ExpressionFilterDefinition<Person>>(), options, cancellationToken),
                                Times.Once);
                        }
                        else
                        {
                            IMongoCollectionExtensions.Distinct(collection, session, fieldDefinition, filterExpression, options, cancellationToken);
                            mockCollection.Verify(
                                s => s.Distinct(session, fieldDefinition, It.IsAny<ExpressionFilterDefinition<Person>>(), options, cancellationToken),
                                Times.Once);
                        }
                    }
                    else
                    {
                        // no extensions methods for these combinations
                    }
                }
            }
            else
            {
                if (usingFieldExpression)
                {
                    if (usingFilterExpression)
                    {
                        if (async)
                        {
                            collection.DistinctAsync(fieldExpression, filterExpression, options, cancellationToken);
                            mockCollection.Verify(
                                s => s.DistinctAsync(It.IsAny<ExpressionFieldDefinition<Person, string>>(), It.IsAny<ExpressionFilterDefinition<Person>>(), options, cancellationToken),
                                Times.Once);
                        }
                        else
                        {
                            collection.Distinct(fieldExpression, filterExpression, options, cancellationToken);
                            mockCollection.Verify(
                                s => s.Distinct(It.IsAny<ExpressionFieldDefinition<Person, string>>(), It.IsAny<ExpressionFilterDefinition<Person>>(), options, cancellationToken),
                                Times.Once);
                        }
                    }
                    else
                    {
                        if (async)
                        {
                            collection.DistinctAsync(fieldExpression, filterDefinition, options, cancellationToken);
                            mockCollection.Verify(
                                s => s.DistinctAsync(It.IsAny<ExpressionFieldDefinition<Person, string>>(), filterDefinition, options, cancellationToken),
                                Times.Once);
                        }
                        else
                        {
                            collection.Distinct(fieldExpression, filterDefinition, options, cancellationToken);
                            mockCollection.Verify(
                                s => s.Distinct(It.IsAny<ExpressionFieldDefinition<Person, string>>(), filterDefinition, options, cancellationToken),
                                Times.Once);
                        }
                    }
                }
                else
                {
                    if (usingFilterExpression)
                    {
                        if (async)
                        {
                            IMongoCollectionExtensions.DistinctAsync(collection, fieldDefinition, filterExpression, options, cancellationToken);
                            mockCollection.Verify(
                                s => s.DistinctAsync(fieldDefinition, It.IsAny<ExpressionFilterDefinition<Person>>(), options, cancellationToken),
                                Times.Once);
                        }
                        else
                        {
                            IMongoCollectionExtensions.Distinct(collection, fieldDefinition, filterExpression, options, cancellationToken);
                            mockCollection.Verify(
                                s => s.Distinct(fieldDefinition, It.IsAny<ExpressionFilterDefinition<Person>>(), options, cancellationToken),
                                Times.Once);
                        }
                    }
                    else
                    {
                        // no extension methods for these combinations
                    }
                }
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Find_should_return_expected_result(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool usingFilterExpression)
        {
            var mockCollection = CreateMockCollection();
            var collection = mockCollection.Object;
            var session = usingSession ? new Mock<IClientSessionHandle>().Object : null;
            var filterDefinition = Builders<Person>.Filter.Eq("FirstName", "Jack");
            var filterExpression = (Expression<Func<Person, bool>>)(x => x.FirstName == "Jack");
            var options = new FindOptions
            {
                AllowPartialResults = true,
                BatchSize = 123,
                Collation = new Collation("en-us"),
                Comment = "comment",
                CursorType = CursorType.Tailable,
                MaxAwaitTime = TimeSpan.FromSeconds(1),
                MaxTime = TimeSpan.FromSeconds(2),
                Modifiers = new BsonDocument("modifier", 1),
                NoCursorTimeout = true,
                OplogReplay = true
            };

            FindFluent<Person, Person> fluent;
            if (usingSession)
            {
                if (usingFilterExpression)
                {
                    var result = collection.Find(session, filterExpression, options);
                    fluent = result.Should().BeOfType<FindFluent<Person, Person>>().Subject;
                    fluent._filter().Should().BeOfType<ExpressionFilterDefinition<Person>>();
                }
                else
                {
                    var result = collection.Find(session, filterDefinition, options);
                    fluent = result.Should().BeOfType<FindFluent<Person, Person>>().Subject;
                    fluent._filter().Should().BeSameAs(filterDefinition);
                }
            }
            else
            {
                if (usingFilterExpression)
                {
                    var result = collection.Find(filterExpression, options);
                    fluent = result.Should().BeOfType<FindFluent<Person, Person>>().Subject;
                    fluent._filter().Should().BeOfType<ExpressionFilterDefinition<Person>>();
                }
                else
                {
                    var result = collection.Find(filterDefinition, options);
                    fluent = result.Should().BeOfType<FindFluent<Person, Person>>().Subject;
                    fluent._filter().Should().BeSameAs(filterDefinition);
                }
            }

            fluent._collection().Should().BeSameAs(collection);
            fluent._session().Should().BeSameAs(session);

            var actualOptions = fluent._options();
            actualOptions.AllowPartialResults.Should().Be(options.AllowPartialResults);
            actualOptions.BatchSize.Should().Be(options.BatchSize);
            actualOptions.Collation.Should().Be(options.Collation);
            actualOptions.Comment.Should().Be(options.Comment);
            actualOptions.CursorType.Should().Be(options.CursorType);
            actualOptions.MaxAwaitTime.Should().Be(options.MaxAwaitTime);
            actualOptions.MaxTime.Should().Be(options.MaxTime);
            actualOptions.Modifiers.Should().Be(options.Modifiers);
            actualOptions.NoCursorTimeout.Should().Be(options.NoCursorTimeout);
            actualOptions.OplogReplay.Should().Be(options.OplogReplay);
        }

        [Theory]
        [ParameterAttributeData]
        public void FindSync_should_call_collection_FindSync_with_expected_arguments(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool usingFilterExpression,
            [Values(false, true)] bool async)
        {
            var mockCollection = CreateMockCollection();
            var collection = mockCollection.Object;
            var session = new Mock<IClientSessionHandle>().Object;
            var filterDefinition = Builders<Person>.Filter.Eq("FirstName", "Jack");
            var filterExpression = (Expression<Func<Person, bool>>)(x => x.FirstName == "Jack");
            var options = new FindOptions<Person>(); // no projection
            var cancellationToken = new CancellationTokenSource().Token;

            if (usingSession)
            {
                if (usingFilterExpression)
                {
                    if (async)
                    {
                        IMongoCollectionExtensions.FindAsync(collection, session, filterExpression, options, cancellationToken);
                        mockCollection.Verify(m => m.FindAsync(session, It.IsAny<ExpressionFilterDefinition<Person>>(), options, cancellationToken), Times.Once);
                    }
                    else
                    {
                        IMongoCollectionExtensions.FindSync(collection, session, filterExpression, options, cancellationToken);
                        mockCollection.Verify(m => m.FindSync(session, It.IsAny<ExpressionFilterDefinition<Person>>(), options, cancellationToken), Times.Once);
                    }
                }
                else
                {
                    if (async)
                    {
                        IMongoCollectionExtensions.FindAsync(collection, session, filterDefinition, options, cancellationToken);
                        mockCollection.Verify(m => m.FindAsync(session, filterDefinition, options, cancellationToken), Times.Once);
                    }
                    else
                    {
                        IMongoCollectionExtensions.FindSync(collection, session, filterDefinition, options, cancellationToken);
                        mockCollection.Verify(m => m.FindSync(session, filterDefinition, options, cancellationToken), Times.Once);
                    }
                }
            }
            else
            {
                if (usingFilterExpression)
                {
                    if (async)
                    {
                        IMongoCollectionExtensions.FindAsync(collection, filterExpression, options, cancellationToken);
                        mockCollection.Verify(m => m.FindAsync(It.IsAny<ExpressionFilterDefinition<Person>>(), options, cancellationToken), Times.Once);
                    }
                    else
                    {
                        IMongoCollectionExtensions.FindSync(collection, filterExpression, options, cancellationToken);
                        mockCollection.Verify(m => m.FindSync(It.IsAny<ExpressionFilterDefinition<Person>>(), options, cancellationToken), Times.Once);
                    }
                }
                else
                {
                    if (async)
                    {
                        IMongoCollectionExtensions.FindAsync(collection, filterDefinition, options, cancellationToken);
                        mockCollection.Verify(m => m.FindAsync(filterDefinition, options, cancellationToken), Times.Once);
                    }
                    else
                    {
                        IMongoCollectionExtensions.FindSync(collection, filterDefinition, options, cancellationToken);
                        mockCollection.Verify(m => m.FindSync(filterDefinition, options, cancellationToken), Times.Once);
                    }
                }
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void FindOneAndDelete_should_call_collection_with_expected_arguments(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool usingFilterExpression,
            [Values(false, true)] bool usingProjection,
            [Values(false, true)] bool async)
        {
            var mockCollection = CreateMockCollection();
            var collection = mockCollection.Object;
            var session = new Mock<IClientSessionHandle>().Object;
            var filterDefinition = Builders<Person>.Filter.Eq("LastName", "Jack");
            var filterExpression = (Expression<Func<Person, bool>>)(x => x.FirstName == "Jack");
            var options = new FindOneAndDeleteOptions<Person>();
            var optionsWithProjection = new FindOneAndDeleteOptions<Person, BsonDocument>();
            var cancellationToken = new CancellationTokenSource().Token;

            if (usingSession)
            {
                if (usingFilterExpression)
                {
                    if (usingProjection)
                    {
                        if (async)
                        {
                            IMongoCollectionExtensions.FindOneAndDeleteAsync(collection, session, filterExpression, optionsWithProjection, cancellationToken);
                            mockCollection.Verify(m => m.FindOneAndDeleteAsync(session, It.IsAny<ExpressionFilterDefinition<Person>>(), optionsWithProjection, cancellationToken), Times.Once);
                        }
                        else
                        {
                            IMongoCollectionExtensions.FindOneAndDelete(collection, session, filterExpression, optionsWithProjection, cancellationToken);
                            mockCollection.Verify(m => m.FindOneAndDelete(session, It.IsAny<ExpressionFilterDefinition<Person>>(), optionsWithProjection, cancellationToken), Times.Once);
                        }
                    }
                    else
                    {
                        if (async)
                        {
                            IMongoCollectionExtensions.FindOneAndDeleteAsync<Person>(collection, session, filterExpression, options, cancellationToken);
                            mockCollection.Verify(m => m.FindOneAndDeleteAsync(session, It.IsAny<ExpressionFilterDefinition<Person>>(), options, cancellationToken), Times.Once);
                        }
                        else
                        {
                            IMongoCollectionExtensions.FindOneAndDelete<Person>(collection, session, filterExpression, options, cancellationToken);
                            mockCollection.Verify(m => m.FindOneAndDelete(session, It.IsAny<ExpressionFilterDefinition<Person>>(), options, cancellationToken), Times.Once);
                        }
                    }
                }
                else
                {
                    if (usingProjection)
                    {
                        // no extension methods for these combinations
                    }
                    else
                    {
                        if (async)
                        {
                            IMongoCollectionExtensions.FindOneAndDeleteAsync(collection, session, filterDefinition, options, cancellationToken);
                            mockCollection.Verify(m => m.FindOneAndDeleteAsync(session, filterDefinition, options, cancellationToken), Times.Once);
                        }
                        else
                        {
                            IMongoCollectionExtensions.FindOneAndDelete(collection, session, filterDefinition, options, cancellationToken);
                            mockCollection.Verify(m => m.FindOneAndDelete(session, filterDefinition, options, cancellationToken), Times.Once);
                        }
                    }
                }
            }
            else
            {
                if (usingFilterExpression)
                {
                    if (usingProjection)
                    {
                        if (async)
                        {
                            IMongoCollectionExtensions.FindOneAndDeleteAsync(collection, filterExpression, optionsWithProjection, cancellationToken);
                            mockCollection.Verify(m => m.FindOneAndDeleteAsync(It.IsAny<ExpressionFilterDefinition<Person>>(), optionsWithProjection, cancellationToken), Times.Once);
                        }
                        else
                        {
                            IMongoCollectionExtensions.FindOneAndDelete(collection, filterExpression, optionsWithProjection, cancellationToken);
                            mockCollection.Verify(m => m.FindOneAndDelete(It.IsAny<ExpressionFilterDefinition<Person>>(), optionsWithProjection, cancellationToken), Times.Once);
                        }
                    }
                    else
                    {
                        if (async)
                        {
                            IMongoCollectionExtensions.FindOneAndDeleteAsync<Person>(collection, filterExpression, options, cancellationToken);
                            mockCollection.Verify(m => m.FindOneAndDeleteAsync(It.IsAny<ExpressionFilterDefinition<Person>>(), options, cancellationToken), Times.Once);
                        }
                        else
                        {
                            IMongoCollectionExtensions.FindOneAndDelete<Person>(collection, filterExpression, options, cancellationToken);
                            mockCollection.Verify(m => m.FindOneAndDelete(It.IsAny<ExpressionFilterDefinition<Person>>(), options, cancellationToken), Times.Once);
                        }
                    }
                }
                else
                {
                    if (usingProjection)
                    {
                        // no extension methods for these combinations
                    }
                    else
                    {
                        if (async)
                        {
                            IMongoCollectionExtensions.FindOneAndDeleteAsync(collection, filterDefinition, options, cancellationToken);
                            mockCollection.Verify(m => m.FindOneAndDeleteAsync(filterDefinition, options, cancellationToken), Times.Once);
                        }
                        else
                        {
                            IMongoCollectionExtensions.FindOneAndDelete(collection, filterDefinition, options, cancellationToken);
                            mockCollection.Verify(m => m.FindOneAndDelete(filterDefinition, options, cancellationToken), Times.Once);
                        }
                    }
                }
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void FindOneAndReplace_should_call_collection_with_expected_arguments(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool usingFilterExpression,
            [Values(false, true)] bool usingProjection,
            [Values(false, true)] bool async)
        {
            var mockCollection = CreateMockCollection();
            var collection = mockCollection.Object;
            var session = new Mock<IClientSessionHandle>().Object;
            var filterDefinition = Builders<Person>.Filter.Eq("FirstName", "Jack");
            var filterExpression = (Expression<Func<Person, bool>>)(x => x.FirstName == "Jack");
            var replacement = new Person();
            var options = new FindOneAndReplaceOptions<Person>();
            var optionsWithProjection = new FindOneAndReplaceOptions<Person, BsonDocument>();
            var cancellationToken = new CancellationTokenSource().Token;

            if (usingSession)
            {
                if (usingFilterExpression)
                {
                    if (usingProjection)
                    {
                        if (async)
                        {
                            IMongoCollectionExtensions.FindOneAndReplaceAsync(collection, session, filterExpression, replacement, optionsWithProjection, cancellationToken);
                            mockCollection.Verify(m => m.FindOneAndReplaceAsync(session, It.IsAny<ExpressionFilterDefinition<Person>>(), replacement, optionsWithProjection, cancellationToken), Times.Once);
                        }
                        else
                        {
                            IMongoCollectionExtensions.FindOneAndReplace(collection, session, filterExpression, replacement, optionsWithProjection, cancellationToken);
                            mockCollection.Verify(m => m.FindOneAndReplace(session, It.IsAny<ExpressionFilterDefinition<Person>>(), replacement, optionsWithProjection, cancellationToken), Times.Once);
                        }
                    }
                    else
                    {
                        if (async)
                        {
                            IMongoCollectionExtensions.FindOneAndReplaceAsync(collection, session, filterExpression, replacement, options, cancellationToken);
                            mockCollection.Verify(m => m.FindOneAndReplaceAsync(session, It.IsAny<ExpressionFilterDefinition<Person>>(), replacement, options, cancellationToken), Times.Once);
                        }
                        else
                        {
                            IMongoCollectionExtensions.FindOneAndReplace<Person>(collection, session, filterExpression, replacement, options, cancellationToken);
                            mockCollection.Verify(m => m.FindOneAndReplace(session, It.IsAny<ExpressionFilterDefinition<Person>>(), replacement, options, cancellationToken), Times.Once);
                        }
                    }
                }
                else
                {
                    if (usingProjection)
                    {
                        // no extension methods for these combinations
                    }
                    else
                    {
                        if (async)
                        {
                            IMongoCollectionExtensions.FindOneAndReplaceAsync(collection, session, filterDefinition, replacement, options, cancellationToken);
                            mockCollection.Verify(m => m.FindOneAndReplaceAsync(session, filterDefinition, replacement, options, cancellationToken), Times.Once);
                        }
                        else
                        {
                            IMongoCollectionExtensions.FindOneAndReplace(collection, session, filterDefinition, replacement, options, cancellationToken);
                            mockCollection.Verify(m => m.FindOneAndReplace(session, filterDefinition, replacement, options, cancellationToken), Times.Once);
                        }
                    }
                }
            }
            else
            {
                if (usingFilterExpression)
                {
                    if (usingProjection)
                    {
                        if (async)
                        {
                            IMongoCollectionExtensions.FindOneAndReplaceAsync(collection, filterExpression, replacement, optionsWithProjection, cancellationToken);
                            mockCollection.Verify(m => m.FindOneAndReplaceAsync(It.IsAny<ExpressionFilterDefinition<Person>>(), replacement, optionsWithProjection, cancellationToken), Times.Once);
                        }
                        else
                        {
                            IMongoCollectionExtensions.FindOneAndReplace(collection, filterExpression, replacement, optionsWithProjection, cancellationToken);
                            mockCollection.Verify(m => m.FindOneAndReplace(It.IsAny<ExpressionFilterDefinition<Person>>(), replacement, optionsWithProjection, cancellationToken), Times.Once);
                        }
                    }
                    else
                    {
                        if (async)
                        {
                            IMongoCollectionExtensions.FindOneAndReplaceAsync<Person>(collection, filterExpression, replacement, options, cancellationToken);
                            mockCollection.Verify(m => m.FindOneAndReplaceAsync(It.IsAny<ExpressionFilterDefinition<Person>>(), replacement, options, cancellationToken), Times.Once);
                        }
                        else
                        {
                            IMongoCollectionExtensions.FindOneAndReplace<Person>(collection, filterExpression, replacement, options, cancellationToken);
                            mockCollection.Verify(m => m.FindOneAndReplace(It.IsAny<ExpressionFilterDefinition<Person>>(), replacement, options, cancellationToken), Times.Once);
                        }
                    }
                }
                else
                {
                    if (usingProjection)
                    {
                        // no extension methods for these combinations
                    }
                    else
                    {
                        if (async)
                        {
                            IMongoCollectionExtensions.FindOneAndReplaceAsync(collection, filterDefinition, replacement, options, cancellationToken);
                            mockCollection.Verify(m => m.FindOneAndReplaceAsync(filterDefinition, replacement, options, cancellationToken), Times.Once);
                        }
                        else
                        {
                            IMongoCollectionExtensions.FindOneAndReplace(collection, filterDefinition, replacement, options, cancellationToken);
                            mockCollection.Verify(m => m.FindOneAndReplace(filterDefinition, replacement, options, cancellationToken), Times.Once);
                        }
                    }
                }
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void FindOneAndUpdate_should_call_collection_with_expected_arguments(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool usingProjection,
            [Values(false, true)] bool usingFilterExpression,
            [Values(false, true)] bool async)
        {
            var mockCollection = CreateMockCollection();
            var collection = mockCollection.Object;
            var session = new Mock<IClientSessionHandle>().Object;
            var filterDefinition = Builders<Person>.Filter.Eq("FirstName", "Jack");
            var filterExpression = (Expression<Func<Person, bool>>)(x => x.FirstName == "Jack");
            var update = Builders<Person>.Update.Set("FirstName", "John");
            var options = new FindOneAndUpdateOptions<Person>();
            var optionsWithProjection = new FindOneAndUpdateOptions<Person, BsonDocument>();
            var cancellationToken = new CancellationTokenSource().Token;

            if (usingSession)
            {
                if (usingProjection)
                {
                    if (usingFilterExpression)
                    {
                        if (async)
                        {
                            IMongoCollectionExtensions.FindOneAndUpdateAsync(collection, session, filterExpression, update, optionsWithProjection, cancellationToken);
                            mockCollection.Verify(m => m.FindOneAndUpdateAsync(session, It.IsAny<ExpressionFilterDefinition<Person>>(), update, optionsWithProjection, cancellationToken), Times.Once);
                        }
                        else
                        {
                            IMongoCollectionExtensions.FindOneAndUpdate(collection, session, filterExpression, update, optionsWithProjection, cancellationToken);
                            mockCollection.Verify(m => m.FindOneAndUpdate(session, It.IsAny<ExpressionFilterDefinition<Person>>(), update, optionsWithProjection, cancellationToken), Times.Once);
                        }
                    }
                    else
                    {
                        // no extension methods for these combinations
                    }
                }
                else
                {
                    if (usingFilterExpression)
                    {
                        if (async)
                        {
                            IMongoCollectionExtensions.FindOneAndUpdateAsync<Person>(collection, session, filterExpression, update, options, cancellationToken);
                            mockCollection.Verify(m => m.FindOneAndUpdateAsync(session, It.IsAny<ExpressionFilterDefinition<Person>>(), update, options, cancellationToken), Times.Once);
                        }
                        else
                        {
                            IMongoCollectionExtensions.FindOneAndUpdate<Person>(collection, session, filterExpression, update, options, cancellationToken);
                            mockCollection.Verify(m => m.FindOneAndUpdate(session, It.IsAny<ExpressionFilterDefinition<Person>>(), update, options, cancellationToken), Times.Once);
                        }
                    }
                    else
                    {
                        if (async)
                        {
                            IMongoCollectionExtensions.FindOneAndUpdateAsync(collection, session, filterDefinition, update, options, cancellationToken);
                            mockCollection.Verify(m => m.FindOneAndUpdateAsync(session, filterDefinition, update, options, cancellationToken), Times.Once);
                        }
                        else
                        {
                            IMongoCollectionExtensions.FindOneAndUpdate(collection, session, filterDefinition, update, options, cancellationToken);
                            mockCollection.Verify(m => m.FindOneAndUpdate(session, filterDefinition, update, options, cancellationToken), Times.Once);
                        }
                    }
                }
            }
            else
            {
                if (usingProjection)
                {
                    if (usingFilterExpression)
                    {
                        if (async)
                        {
                            IMongoCollectionExtensions.FindOneAndUpdateAsync(collection, filterExpression, update, optionsWithProjection, cancellationToken);
                            mockCollection.Verify(m => m.FindOneAndUpdateAsync(It.IsAny<ExpressionFilterDefinition<Person>>(), update, optionsWithProjection, cancellationToken), Times.Once);
                        }
                        else
                        {
                            IMongoCollectionExtensions.FindOneAndUpdate(collection, filterExpression, update, optionsWithProjection, cancellationToken);
                            mockCollection.Verify(m => m.FindOneAndUpdate(It.IsAny<ExpressionFilterDefinition<Person>>(), update, optionsWithProjection, cancellationToken), Times.Once);
                        }
                    }
                    else
                    {
                        // no extension methods for these combinations
                    }
                }
                else
                {
                    if (usingFilterExpression)
                    {
                        if (async)
                        {
                            IMongoCollectionExtensions.FindOneAndUpdateAsync<Person>(collection, filterExpression, update, options, cancellationToken);
                            mockCollection.Verify(m => m.FindOneAndUpdateAsync(It.IsAny<ExpressionFilterDefinition<Person>>(), update, options, cancellationToken), Times.Once);
                        }
                        else
                        {
                            IMongoCollectionExtensions.FindOneAndUpdate<Person>(collection, filterExpression, update, options, cancellationToken);
                            mockCollection.Verify(m => m.FindOneAndUpdate(It.IsAny<ExpressionFilterDefinition<Person>>(), update, options, cancellationToken), Times.Once);
                        }
                    }
                    else
                    {
                        if (async)
                        {
                            IMongoCollectionExtensions.FindOneAndUpdateAsync(collection, filterDefinition, update, options, cancellationToken);
                            mockCollection.Verify(m => m.FindOneAndUpdateAsync(filterDefinition, update, options, cancellationToken), Times.Once);
                        }
                        else
                        {
                            IMongoCollectionExtensions.FindOneAndUpdate(collection, filterDefinition, update, options, cancellationToken);
                            mockCollection.Verify(m => m.FindOneAndUpdate(filterDefinition, update, options, cancellationToken), Times.Once);
                        }
                    }
                }
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void ReplaceOne_should_call_collection_with_expected_arguments(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var mockCollection = CreateMockCollection();
            var collection = mockCollection.Object;
            var session = new Mock<IClientSessionHandle>().Object;
            var filterExpression = (Expression<Func<Person, bool>>)(x => x.FirstName == "Jack");
            var replacement = new Person();
            var cancellationToken = new CancellationTokenSource().Token;

            assertReplaceOne();

            var replaceOptions = new ReplaceOptions();
            assertReplaceOneWithReplaceOptions(replaceOptions);

            var updateOptions = new UpdateOptions();
            assertReplaceOneWithUpdateOptions(updateOptions);

            void assertReplaceOne()
            {
                if (usingSession)
                {
                    if (async)
                    {
                        IMongoCollectionExtensions.ReplaceOneAsync(collection, session, filterExpression, replacement, cancellationToken: cancellationToken);
                        mockCollection.Verify(m => m.ReplaceOneAsync(session, It.IsAny<ExpressionFilterDefinition<Person>>(), replacement, (ReplaceOptions)null, cancellationToken), Times.Once);
                    }
                    else
                    {
                        IMongoCollectionExtensions.ReplaceOne(collection, session, filterExpression, replacement, cancellationToken: cancellationToken);
                        mockCollection.Verify(m => m.ReplaceOne(session, It.IsAny<ExpressionFilterDefinition<Person>>(), replacement, (ReplaceOptions)null, cancellationToken), Times.Once);
                    }
                }
                else
                {
                    if (async)
                    {
                        IMongoCollectionExtensions.ReplaceOneAsync(collection, filterExpression, replacement, cancellationToken: cancellationToken);
                        mockCollection.Verify(m => m.ReplaceOneAsync(It.IsAny<ExpressionFilterDefinition<Person>>(), replacement, (ReplaceOptions)null, cancellationToken), Times.Once);
                    }
                    else
                    {
                        IMongoCollectionExtensions.ReplaceOne(collection, filterExpression, replacement, cancellationToken: cancellationToken);
                        mockCollection.Verify(m => m.ReplaceOne(It.IsAny<ExpressionFilterDefinition<Person>>(), replacement, (ReplaceOptions)null, cancellationToken), Times.Once);
                    }
                }
            }

            void assertReplaceOneWithReplaceOptions(ReplaceOptions options)
            {
                if (usingSession)
                {
                    if (async)
                    {
                        IMongoCollectionExtensions.ReplaceOneAsync(collection, session, filterExpression, replacement, options, cancellationToken);
                        mockCollection.Verify(m => m.ReplaceOneAsync(session, It.IsAny<ExpressionFilterDefinition<Person>>(), replacement, options, cancellationToken), Times.Once);
                    }
                    else
                    {
                        IMongoCollectionExtensions.ReplaceOne(collection, session, filterExpression, replacement, options, cancellationToken);
                        mockCollection.Verify(m => m.ReplaceOne(session, It.IsAny<ExpressionFilterDefinition<Person>>(), replacement, options, cancellationToken), Times.Once);
                    }
                }
                else
                {
                    if (async)
                    {
                        IMongoCollectionExtensions.ReplaceOneAsync(collection, filterExpression, replacement, options, cancellationToken);
                        mockCollection.Verify(m => m.ReplaceOneAsync(It.IsAny<ExpressionFilterDefinition<Person>>(), replacement, options, cancellationToken), Times.Once);
                    }
                    else
                    {
                        IMongoCollectionExtensions.ReplaceOne(collection, filterExpression, replacement, options, cancellationToken);
                        mockCollection.Verify(m => m.ReplaceOne(It.IsAny<ExpressionFilterDefinition<Person>>(), replacement, options, cancellationToken), Times.Once);
                    }
                }
            }

            void assertReplaceOneWithUpdateOptions(UpdateOptions options)
            {
                if (usingSession)
                {
                    if (async)
                    {
#pragma warning disable 618
                        IMongoCollectionExtensions.ReplaceOneAsync(collection, session, filterExpression, replacement, options, cancellationToken);
                        mockCollection.Verify(m => m.ReplaceOneAsync(session, It.IsAny<ExpressionFilterDefinition<Person>>(), replacement, options, cancellationToken), Times.Once);
#pragma warning restore 618
                    }
                    else
                    {
#pragma warning disable 618
                        IMongoCollectionExtensions.ReplaceOne(collection, session, filterExpression, replacement, options, cancellationToken);
                        mockCollection.Verify(m => m.ReplaceOne(session, It.IsAny<ExpressionFilterDefinition<Person>>(), replacement, options, cancellationToken), Times.Once);
#pragma warning restore 618
                    }
                }
                else
                {
                    if (async)
                    {
#pragma warning disable 618
                        IMongoCollectionExtensions.ReplaceOneAsync(collection, filterExpression, replacement, options, cancellationToken);
                        mockCollection.Verify(m => m.ReplaceOneAsync(It.IsAny<ExpressionFilterDefinition<Person>>(), replacement, options, cancellationToken), Times.Once);
#pragma warning restore 618
                    }
                    else
                    {
#pragma warning disable 618
                        IMongoCollectionExtensions.ReplaceOne(collection, filterExpression, replacement, options, cancellationToken);
                        mockCollection.Verify(m => m.ReplaceOne(It.IsAny<ExpressionFilterDefinition<Person>>(), replacement, options, cancellationToken), Times.Once);
#pragma warning restore 618
                    }
                }
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void UpdateMany_should_call_collection_with_expected_arguments(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var mockCollection = CreateMockCollection();
            var collection = mockCollection.Object;
            var session = new Mock<IClientSessionHandle>().Object;
            var filterExpression = (Expression<Func<Person, bool>>)(x => x.FirstName == "Jack");
            var update = Builders<Person>.Update.Set("FirstName", "John");
            var options = new UpdateOptions();
            var cancellationToken = new CancellationTokenSource().Token;

            if (usingSession)
            {
                if (async)
                {
                    IMongoCollectionExtensions.UpdateManyAsync(collection, session, filterExpression, update, options, cancellationToken);
                    mockCollection.Verify(m => m.UpdateManyAsync(session, It.IsAny<ExpressionFilterDefinition<Person>>(), update, options, cancellationToken));
                }
                else
                {
                    IMongoCollectionExtensions.UpdateMany(collection, session, filterExpression, update, options, cancellationToken);
                    mockCollection.Verify(m => m.UpdateMany(session, It.IsAny<ExpressionFilterDefinition<Person>>(), update, options, cancellationToken));
                }
            }
            else
            {
                if (async)
                {
                    IMongoCollectionExtensions.UpdateManyAsync(collection, filterExpression, update, options, cancellationToken);
                    mockCollection.Verify(m => m.UpdateManyAsync(It.IsAny<ExpressionFilterDefinition<Person>>(), update, options, cancellationToken));
                }
                else
                {
                    IMongoCollectionExtensions.UpdateMany(collection, filterExpression, update, options, cancellationToken);
                    mockCollection.Verify(m => m.UpdateMany(It.IsAny<ExpressionFilterDefinition<Person>>(), update, options, cancellationToken));
                }
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void UpdateOne_should_call_collection_with_expected_arguments(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var mockCollection = CreateMockCollection();
            var collection = mockCollection.Object;
            var session = new Mock<IClientSessionHandle>().Object;
            var filterExpression = (Expression<Func<Person, bool>>)(x => x.FirstName == "Jack");
            var update = Builders<Person>.Update.Set("FirstName", "John");
            var options = new UpdateOptions();
            var cancellationToken = new CancellationTokenSource().Token;

            if (usingSession)
            {
                if (async)
                {
                    IMongoCollectionExtensions.UpdateOneAsync(collection, session, filterExpression, update, options, cancellationToken);
                    mockCollection.Verify(m => m.UpdateOneAsync(session, It.IsAny<ExpressionFilterDefinition<Person>>(), update, options, cancellationToken));
                }
                else
                {
                    IMongoCollectionExtensions.UpdateOne(collection, session, filterExpression, update, options, cancellationToken);
                    mockCollection.Verify(m => m.UpdateOne(session, It.IsAny<ExpressionFilterDefinition<Person>>(), update, options, cancellationToken));
                }
            }
            else
            {
                if (async)
                {
                    IMongoCollectionExtensions.UpdateOneAsync(collection, filterExpression, update, options, cancellationToken);
                    mockCollection.Verify(m => m.UpdateOneAsync(It.IsAny<ExpressionFilterDefinition<Person>>(), update, options, cancellationToken));
                }
                else
                {
                    IMongoCollectionExtensions.UpdateOne(collection, filterExpression, update, options, cancellationToken);
                    mockCollection.Verify(m => m.UpdateOne(It.IsAny<ExpressionFilterDefinition<Person>>(), update, options, cancellationToken));
                }
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Watch_should_call_collection_with_expected_arguments(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var mockCollection = CreateMockCollection();
            var collection = mockCollection.Object;
            var session = new Mock<IClientSessionHandle>().Object;
            var options = new ChangeStreamOptions();
            var cancellationToken = new CancellationTokenSource().Token;

            if (usingSession)
            {
                if (async)
                {
                    collection.WatchAsync(session, options, cancellationToken);
                    mockCollection.Verify(m => m.WatchAsync(session, It.IsAny<EmptyPipelineDefinition<ChangeStreamDocument<Person>>>(), options, cancellationToken), Times.Once);
                }
                else
                {
                    collection.Watch(session, options, cancellationToken);
                    mockCollection.Verify(m => m.Watch(session, It.IsAny<EmptyPipelineDefinition<ChangeStreamDocument<Person>>>(), options, cancellationToken), Times.Once);
                }
            }
            else
            {
                if (async)
                {
                    collection.WatchAsync(options, cancellationToken);
                    mockCollection.Verify(m => m.WatchAsync(It.IsAny<EmptyPipelineDefinition<ChangeStreamDocument<Person>>>(), options, cancellationToken), Times.Once);
                }
                else
                {
                    collection.Watch(options, cancellationToken);
                    mockCollection.Verify(m => m.Watch(It.IsAny<EmptyPipelineDefinition<ChangeStreamDocument<Person>>>(), options, cancellationToken), Times.Once);
                }
            }
        }

        private Mock<IMongoCollection<Person>> CreateMockCollection()
        {
            var settings = new MongoCollectionSettings();
            var mockCollection = new Mock<IMongoCollection<Person>> { DefaultValue = DefaultValue.Mock };
            mockCollection.SetupGet(s => s.DocumentSerializer).Returns(settings.SerializerRegistry.GetSerializer<Person>());
            mockCollection.SetupGet(s => s.Settings).Returns(settings);
            return mockCollection;
        }

        public class Person
        {
            public string FirstName;
            public string LastName;
            public int Age;
        }
    }
}
