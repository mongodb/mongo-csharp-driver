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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class IMongoCollectionExtensionsTests
    {
        [Theory]
        [ParameterAttributeData]
        public void Aggregate_should_call_collection_AggregateAsync_with_correct_options(
            [Values(false, true)] bool async)
        {
            var mockSubject = CreateMockSubject();
            var subject = mockSubject.Object;

            var expectedPipeline = new BsonDocument[]
            { 
                BsonDocument.Parse("{ $match: { x: 2 } }"),
                BsonDocument.Parse("{ $project : { Age : \"$Age\", Name : { $concat : [\"$firstName\", \" \", \"$lastName\"] }, _id : 0 } }"),
                BsonDocument.Parse("{ $group : { _id : \"$Age\", Name : { \"$first\" : \"$Name\" } } }"),
                BsonDocument.Parse("{ $project : { _id: 1 } }")
            };

            var collation = new Collation("en_US");
            var fluent = subject.Aggregate(new AggregateOptions
                {
                    AllowDiskUse = true,
                    BatchSize = 10,
                    Collation = collation,
                    MaxTime = TimeSpan.FromSeconds(3),
                    UseCursor = false
                })
                .Match("{x: 2}")
                .Project("{ Age : \"$Age\", Name : { $concat : [\"$firstName\", \" \", \"$lastName\"] }, _id : 0 }")
                .Group("{ _id : \"$Age\", Name : { \"$first\" : \"$Name\" } }")
                .Project("{ _id: 1 }");

            PipelineDefinition<Person, BsonDocument> actualPipeline = null;
            AggregateOptions actualOptions = null;

            if (async)
            {
                mockSubject
                    .Setup(s => s.AggregateAsync(It.IsAny<PipelineDefinition<Person, BsonDocument>>(), It.IsAny<AggregateOptions>(), It.IsAny<CancellationToken>()))
                    .Callback((PipelineDefinition<Person, BsonDocument> pipeline, AggregateOptions options, CancellationToken cancellationToken) =>
                    {
                        actualPipeline = pipeline;
                        actualOptions = options;
                    })
                    .Returns(Task.FromResult(new Mock<IAsyncCursor<BsonDocument>>().Object));

                fluent.ToCursorAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                mockSubject
                    .Setup(s => s.Aggregate(It.IsAny<PipelineDefinition<Person, BsonDocument>>(), It.IsAny<AggregateOptions>(), It.IsAny<CancellationToken>()))
                    .Callback((PipelineDefinition<Person, BsonDocument> pipeline, AggregateOptions options, CancellationToken cancellationToken) =>
                    {
                        actualPipeline = pipeline;
                        actualOptions = options;
                    })
                    .Returns(new Mock<IAsyncCursor<BsonDocument>>().Object);

                fluent.ToCursor(CancellationToken.None);
            }

            var inputSerializer = subject.DocumentSerializer;
            var serializerRegistry = subject.Settings.SerializerRegistry;
            actualPipeline.Render(inputSerializer, serializerRegistry).Documents.Should().Equal(expectedPipeline);
            actualOptions.AllowDiskUse.Should().Be(fluent.Options.AllowDiskUse);
            actualOptions.BatchSize.Should().Be(fluent.Options.BatchSize);
            actualOptions.Collation.Should().BeSameAs(collation);
            actualOptions.MaxTime.Should().Be(fluent.Options.MaxTime);
            actualOptions.UseCursor.Should().Be(fluent.Options.UseCursor);
        }

        [Theory]
        [ParameterAttributeData]
        public void Count_with_an_expression_should_call_collection_with_the_correct_filter(
            [Values(false, true)] bool async)
        {
            var mockSubject = CreateMockSubject();
            var subject = mockSubject.Object;

            if (async)
            {
                subject.CountAsync(x => x.FirstName == "Jack");
                mockSubject.Verify(s => s.CountAsync(It.IsAny<ExpressionFilterDefinition<Person>>(), null, default(CancellationToken)), Times.Once);
            }
            else
            {
                subject.Count(x => x.FirstName == "Jack");
                mockSubject.Verify(s => s.Count(It.IsAny<ExpressionFilterDefinition<Person>>(), null, default(CancellationToken)), Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void DeleteMany_with_an_expression_should_call_collection_with_the_correct_filter(
            [Values(false, true)] bool async)
        {
            var mockSubject = CreateMockSubject();
            var subject = mockSubject.Object;

            if (async)
            {
                subject.DeleteManyAsync(x => x.FirstName == "Jack");
                mockSubject.Verify(s => s.DeleteManyAsync(It.IsAny<ExpressionFilterDefinition<Person>>(), It.IsAny<DeleteOptions>(), default(CancellationToken)), Times.Once);
            }
            else
            {
                subject.DeleteMany(x => x.FirstName == "Jack");
                mockSubject.Verify(s => s.DeleteMany(It.IsAny<ExpressionFilterDefinition<Person>>(), It.IsAny<DeleteOptions>(), default(CancellationToken)), Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void DeleteOne_with_an_expression_should_call_collection_with_the_correct_filter(
            [Values(false, true)] bool async)
        {
            var mockSubject = CreateMockSubject();
            var subject = mockSubject.Object;

            if (async)
            {
                subject.DeleteOneAsync(x => x.FirstName == "Jack");
                mockSubject.Verify(s => s.DeleteOneAsync(It.IsAny<ExpressionFilterDefinition<Person>>(), It.IsAny<DeleteOptions>(), default(CancellationToken)), Times.Once);
            }
            else
            {
                subject.DeleteOne(x => x.FirstName == "Jack");
                mockSubject.Verify(s => s.DeleteOne(It.IsAny<ExpressionFilterDefinition<Person>>(), It.IsAny<DeleteOptions>(), default(CancellationToken)), Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Distinct_with_an_expression_should_call_collection_with_the_correct_filter(
            [Values(false, true)] bool async)
        {
            var mockSubject = CreateMockSubject();
            var subject = mockSubject.Object;

            if (async)
            {
                subject.DistinctAsync(x => x.LastName, x => x.FirstName == "Jack");
                mockSubject.Verify(
                    s => s.DistinctAsync(It.IsAny<ExpressionFieldDefinition<Person, string>>(), It.IsAny<ExpressionFilterDefinition<Person>>(), null, default(CancellationToken)),
                    Times.Once);
            }
            else
            {
                subject.Distinct(x => x.LastName, x => x.FirstName == "Jack");
                mockSubject.Verify(
                    s => s.Distinct(It.IsAny<ExpressionFieldDefinition<Person, string>>(), It.IsAny<ExpressionFilterDefinition<Person>>(), null, default(CancellationToken)),
                    Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void ToCursor_should_call_collection_Find_with_correct_options(
            [Values(false, true)] bool async)
        {
            var mockSubject = CreateMockSubject();
            var subject = mockSubject.Object;
            var filter = BsonDocument.Parse("{x:1}");
            var projection = BsonDocument.Parse("{y:1}");
            var sort = BsonDocument.Parse("{a:1}");
            var options = new FindOptions
            {
                AllowPartialResults = true,
                BatchSize = 20,
                Collation = new Collation("en_US"),
                Comment = "funny",
                CursorType = CursorType.TailableAwait,
                MaxAwaitTime = TimeSpan.FromSeconds(4),
                MaxTime = TimeSpan.FromSeconds(3),
                Modifiers = BsonDocument.Parse("{$snapshot: true}"),
                NoCursorTimeout = true,
                OplogReplay = true
            };

            var fluent = subject.Find(filter, options)
                .Project(projection)
                .Sort(sort)
                .Limit(30)
                .Skip(40);

            FilterDefinition<Person> actualFilter = null;
            FindOptions<Person, BsonDocument> actualOptions = null;

            if (async)
            {
                mockSubject
                    .Setup(s => s.FindAsync(It.IsAny<FilterDefinition<Person>>(), It.IsAny<FindOptions<Person, BsonDocument>>(), It.IsAny<CancellationToken>()))
                    .Callback((FilterDefinition<Person> filterArg, FindOptions<Person, BsonDocument> optionsArg, CancellationToken cancellationToken) =>
                    {
                        actualFilter = filterArg;
                        actualOptions = optionsArg;
                    })
                    .Returns(Task.FromResult(new Mock<IAsyncCursor<BsonDocument>>().Object));

                fluent.ToCursorAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                mockSubject
                    .Setup(s => s.FindSync(It.IsAny<FilterDefinition<Person>>(), It.IsAny<FindOptions<Person, BsonDocument>>(), It.IsAny<CancellationToken>()))
                    .Callback((FilterDefinition<Person> filterArg, FindOptions<Person, BsonDocument> optionsArg, CancellationToken cancellationToken) =>
                    {
                        actualFilter = filterArg;
                        actualOptions = optionsArg;
                    })
                    .Returns(new Mock<IAsyncCursor<BsonDocument>>().Object);

                fluent.ToCursor(CancellationToken.None);
            }

            ((BsonDocumentFilterDefinition<Person>)actualFilter).Document.Should().Be(filter);
            actualOptions.AllowPartialResults.Should().Be(fluent.Options.AllowPartialResults);
            actualOptions.Collation.Should().BeSameAs(fluent.Options.Collation);
            actualOptions.BatchSize.Should().Be(fluent.Options.BatchSize);
            actualOptions.Comment.Should().Be(fluent.Options.Comment);
            actualOptions.CursorType.Should().Be(fluent.Options.CursorType);
            actualOptions.Limit.Should().Be(fluent.Options.Limit);
            actualOptions.MaxAwaitTime.Should().Be(fluent.Options.MaxAwaitTime);
            actualOptions.MaxTime.Should().Be(fluent.Options.MaxTime);
            actualOptions.Modifiers.Should().Be(fluent.Options.Modifiers);
            actualOptions.NoCursorTimeout.Should().Be(fluent.Options.NoCursorTimeout);
            actualOptions.OplogReplay.Should().Be(fluent.Options.OplogReplay);
            actualOptions.Projection.Should().Be(fluent.Options.Projection);
            actualOptions.Skip.Should().Be(fluent.Options.Skip);
            actualOptions.Sort.Should().Be(fluent.Options.Sort);
        }

        [Theory]
        [ParameterAttributeData]
        public void ToCursor_with_an_expression_should_call_collection_FindAsync_with_correct_options(
            [Values(false, true)] bool async)
        {
            var mockSubject = CreateMockSubject();
            var subject = mockSubject.Object;
            var filter = BsonDocument.Parse("{Age:1}");
            var projection = BsonDocument.Parse("{y:1}");
            var sort = BsonDocument.Parse("{a:1}");
            var options = new FindOptions
            {
                AllowPartialResults = true,
                BatchSize = 20,
                Collation = new Collation("en_US"),
                Comment = "funny",
                CursorType = CursorType.TailableAwait,
                MaxAwaitTime = TimeSpan.FromSeconds(4),
                MaxTime = TimeSpan.FromSeconds(3),
                Modifiers = BsonDocument.Parse("{$snapshot: true}"),
                NoCursorTimeout = true,
                OplogReplay = true
            };

            var fluent = subject.Find(x => x.Age == 1, options)
                .Project(projection)
                .Sort(sort)
                .Limit(30)
                .Skip(40);

            FilterDefinition<Person> actualFilter = null;
            FindOptions<Person, BsonDocument> actualOptions = null;

            if (async)
            {
                mockSubject
                    .Setup(s => s.FindAsync(It.IsAny<FilterDefinition<Person>>(), It.IsAny<FindOptions<Person, BsonDocument>>(), It.IsAny<CancellationToken>()))
                    .Callback((FilterDefinition<Person> filterArg, FindOptions<Person, BsonDocument> optionsArg, CancellationToken cancellationToken) =>
                    {
                        actualFilter = filterArg;
                        actualOptions = optionsArg;
                    })
                    .Returns(Task.FromResult(new Mock<IAsyncCursor<BsonDocument>>().Object));

                fluent.ToCursorAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                mockSubject
                    .Setup(s => s.FindSync(It.IsAny<FilterDefinition<Person>>(), It.IsAny<FindOptions<Person, BsonDocument>>(), It.IsAny<CancellationToken>()))
                    .Callback((FilterDefinition<Person> filterArg, FindOptions<Person, BsonDocument> optionsArg, CancellationToken cancellationToken) =>
                    {
                        actualFilter = filterArg;
                        actualOptions = optionsArg;
                    })
                    .Returns(new Mock<IAsyncCursor<BsonDocument>>().Object);

                fluent.ToCursor(CancellationToken.None);
            }

            actualFilter.Should().BeOfType<ExpressionFilterDefinition<Person>>();
            actualFilter.Render(subject.DocumentSerializer, subject.Settings.SerializerRegistry).Should().Be(filter);
            actualOptions.AllowPartialResults.Should().Be(fluent.Options.AllowPartialResults);
            actualOptions.BatchSize.Should().Be(fluent.Options.BatchSize);
            actualOptions.Collation.Should().BeSameAs(fluent.Options.Collation);
            actualOptions.Comment.Should().Be(fluent.Options.Comment);
            actualOptions.CursorType.Should().Be(fluent.Options.CursorType);
            actualOptions.Limit.Should().Be(fluent.Options.Limit);
            actualOptions.MaxAwaitTime.Should().Be(fluent.Options.MaxAwaitTime);
            actualOptions.MaxTime.Should().Be(fluent.Options.MaxTime);
            actualOptions.Modifiers.Should().Be(fluent.Options.Modifiers);
            actualOptions.NoCursorTimeout.Should().Be(fluent.Options.NoCursorTimeout);
            actualOptions.OplogReplay.Should().Be(fluent.Options.OplogReplay);
            actualOptions.Projection.Should().Be(fluent.Options.Projection);
            actualOptions.Skip.Should().Be(fluent.Options.Skip);
            actualOptions.Sort.Should().Be(fluent.Options.Sort);
        }

        [Theory]
        [ParameterAttributeData]
        public void FindOneAndDelete_with_an_expression_should_call_collection_with_the_correct_filter(
            [Values(false, true)] bool async)
        {
            var mockSubject = CreateMockSubject();
            var subject = mockSubject.Object;

            if (async)
            {
                subject.FindOneAndDeleteAsync(x => x.FirstName == "Jack");

                mockSubject.Verify(s => s.FindOneAndDeleteAsync<Person>(It.IsAny<ExpressionFilterDefinition<Person>>(), null, default(CancellationToken)), Times.Once);
            }
            else
            {
                subject.FindOneAndDelete(x => x.FirstName == "Jack");

                mockSubject.Verify(s => s.FindOneAndDelete<Person>(It.IsAny<ExpressionFilterDefinition<Person>>(), null, default(CancellationToken)), Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void FindOneAndDelete_with_an_expression_and_result_options_should_call_collection_with_the_correct_filter(
            [Values(false, true)] bool async)
        {
            var mockSubject = CreateMockSubject();
            var subject = mockSubject.Object;
            var options = new FindOneAndDeleteOptions<Person, BsonDocument>();

            if (async)
            {
                subject.FindOneAndDeleteAsync(x => x.FirstName == "Jack", options);

                mockSubject.Verify(s => s.FindOneAndDeleteAsync<BsonDocument>(It.IsAny<ExpressionFilterDefinition<Person>>(), options, default(CancellationToken)), Times.Once);
            }
            else
            {
                subject.FindOneAndDelete(x => x.FirstName == "Jack", options);

                mockSubject.Verify(s => s.FindOneAndDelete<BsonDocument>(It.IsAny<ExpressionFilterDefinition<Person>>(), options, default(CancellationToken)), Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void FindOneAndReplace_with_an_expression_should_call_collection_with_the_correct_filter(
            [Values(false, true)] bool async)
        {
            var mockSubject = CreateMockSubject();
            var subject = mockSubject.Object;
            var replacement = new Person();

            if (async)
            {
                subject.FindOneAndReplaceAsync(x => x.FirstName == "Jack", replacement);

                mockSubject.Verify(
                    s => s.FindOneAndReplaceAsync<Person>(It.IsAny<ExpressionFilterDefinition<Person>>(), replacement, null, default(CancellationToken)),
                    Times.Once);
            }
            else
            {
                subject.FindOneAndReplace(x => x.FirstName == "Jack", replacement);

                mockSubject.Verify(
                    s => s.FindOneAndReplace<Person>(It.IsAny<ExpressionFilterDefinition<Person>>(), replacement, null, default(CancellationToken)),
                    Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void FindOneAndReplaceAsync_with_an_expression_and_result_options_should_call_collection_with_the_correct_filter(
            [Values(false, true)] bool async)
        {
            var mockSubject = CreateMockSubject();
            var subject = mockSubject.Object;
            var replacement = new Person();
            var options = new FindOneAndReplaceOptions<Person, BsonDocument>();

            if (async)
            {
                subject.FindOneAndReplaceAsync(x => x.FirstName == "Jack", replacement, options);

                mockSubject.Verify(
                    s => s.FindOneAndReplaceAsync<BsonDocument>(It.IsAny<ExpressionFilterDefinition<Person>>(), replacement, options, default(CancellationToken)),
                    Times.Once);
            }
            else
            {
                subject.FindOneAndReplace(x => x.FirstName == "Jack", replacement, options);

                mockSubject.Verify(
                    s => s.FindOneAndReplace<BsonDocument>(It.IsAny<ExpressionFilterDefinition<Person>>(), replacement, options, default(CancellationToken)),
                    Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void FindOneAndUpdateAsync_with_an_expression_should_call_collection_with_the_correct_filter(
            [Values(false, true)] bool async)
        {
            var mockSubject = CreateMockSubject();
            var subject = mockSubject.Object;
            var update = new BsonDocument();

            if (async)
            {
                subject.FindOneAndUpdateAsync(x => x.FirstName == "Jack", update);

                mockSubject.Verify(
                    s => s.FindOneAndUpdateAsync<Person>(
                        It.IsAny<ExpressionFilterDefinition<Person>>(),
                        It.Is<BsonDocumentUpdateDefinition<Person>>(x => x.Document == update),
                        null,
                        default(CancellationToken)),
                    Times.Once);
            }
            else
            {
                subject.FindOneAndUpdate(x => x.FirstName == "Jack", update);

                mockSubject.Verify(
                    s => s.FindOneAndUpdate<Person>(
                        It.IsAny<ExpressionFilterDefinition<Person>>(),
                        It.Is<BsonDocumentUpdateDefinition<Person>>(x => x.Document == update),
                        null,
                        default(CancellationToken)),
                    Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void FindOneAndUpdateAsync_with_an_expression_and_result_options_should_call_collection_with_the_correct_filter(
            [Values(false, true)] bool async)
        {
            var mockSubject = CreateMockSubject();
            var subject = mockSubject.Object;
            var update = new BsonDocument();
            var options = new FindOneAndUpdateOptions<Person, BsonDocument>();

            if (async)
            {
                subject.FindOneAndUpdateAsync(x => x.FirstName == "Jack", update, options);

                mockSubject.Verify(
                    s => s.FindOneAndUpdateAsync<BsonDocument>(
                        It.IsAny<ExpressionFilterDefinition<Person>>(),
                        It.Is<BsonDocumentUpdateDefinition<Person>>(x => x.Document == update),
                        options,
                        default(CancellationToken)),
                    Times.Once);
            }
            else
            {
                subject.FindOneAndUpdate(x => x.FirstName == "Jack", update, options);

                mockSubject.Verify(
                    s => s.FindOneAndUpdate<BsonDocument>(
                        It.IsAny<ExpressionFilterDefinition<Person>>(),
                        It.Is<BsonDocumentUpdateDefinition<Person>>(x => x.Document == update),
                        options,
                        default(CancellationToken)),
                    Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void ReplaceOneAsync_with_an_expression_should_call_collection_with_the_correct_filter(
            [Values(false, true)] bool async)
        {
            var mockSubject = CreateMockSubject();
            var subject = mockSubject.Object;
            var replacement = new Person();

            if (async)
            {
                subject.ReplaceOneAsync(x => x.FirstName == "Jack", replacement);

                mockSubject.Verify(s => s.ReplaceOneAsync(It.IsAny<ExpressionFilterDefinition<Person>>(), replacement, null, default(CancellationToken)), Times.Once);
            }
            else
            {
                subject.ReplaceOne(x => x.FirstName == "Jack", replacement);

                mockSubject.Verify(s => s.ReplaceOne(It.IsAny<ExpressionFilterDefinition<Person>>(), replacement, null, default(CancellationToken)), Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void UpdateManyAsync_with_an_expression_should_call_collection_with_the_correct_filter(
            [Values(false, true)] bool async)
        {
            var mockSubject = CreateMockSubject();
            var subject = mockSubject.Object;
            var update = new BsonDocument();

            if (async)
            {
                subject.UpdateManyAsync(x => x.FirstName == "Jack", update);

                mockSubject.Verify(
                    s => s.UpdateManyAsync(
                        It.IsAny<ExpressionFilterDefinition<Person>>(),
                        It.Is<BsonDocumentUpdateDefinition<Person>>(x => x.Document == update),
                        null,
                        default(CancellationToken)),
                    Times.Once);
            }
            else
            {
                subject.UpdateMany(x => x.FirstName == "Jack", update);

                mockSubject.Verify(
                    s => s.UpdateMany(
                        It.IsAny<ExpressionFilterDefinition<Person>>(),
                        It.Is<BsonDocumentUpdateDefinition<Person>>(x => x.Document == update),
                        null,
                        default(CancellationToken)),
                    Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void UpdateOneAsync_with_an_expression_should_call_collection_with_the_correct_filter(
            [Values(false, true)] bool async)
        {
            var mockSubject = CreateMockSubject();
            var subject = mockSubject.Object;
            var update = new BsonDocument();

            if (async)
            {
                subject.UpdateOneAsync(x => x.FirstName == "Jack", update);

                mockSubject.Verify(
                    s => s.UpdateOneAsync(
                    It.IsAny<ExpressionFilterDefinition<Person>>(),
                        It.Is<BsonDocumentUpdateDefinition<Person>>(x => x.Document == update),
                        null,
                        default(CancellationToken)),
                    Times.Once);
            }
            else
            {
                subject.UpdateOne(x => x.FirstName == "Jack", update);

                mockSubject.Verify(
                    s => s.UpdateOne(
                        It.IsAny<ExpressionFilterDefinition<Person>>(),
                        It.Is<BsonDocumentUpdateDefinition<Person>>(x => x.Document == update),
                        null,
                        default(CancellationToken)),
                    Times.Once);
            }
        }

        private bool Matches(object o, BsonDocument doc)
        {
            return o.ToBsonDocument().Equals(doc);
        }

        private Mock<IMongoCollection<Person>> CreateMockSubject()
        {
            var settings = new MongoCollectionSettings();
            var mockSubject = new Mock<IMongoCollection<Person>> { DefaultValue = DefaultValue.Mock };
            mockSubject.SetupGet(s => s.DocumentSerializer).Returns(settings.SerializerRegistry.GetSerializer<Person>());
            mockSubject.SetupGet(s => s.Settings).Returns(settings);
            return mockSubject;
        }

        public class Person
        {
            public string FirstName;
            public string LastName;
            public int Age;
        }
    }
}
