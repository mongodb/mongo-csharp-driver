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
using FluentAssertions;
using MongoDB.Bson;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Tests
{
    [TestFixture]
    public class IMongoCollectionExtensionsTests
    {
        [Test]
        public void Aggregate_should_call_collection_AggregateAsync_with_correct_options(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();

            var expectedPipeline = new BsonDocument[]
            { 
                BsonDocument.Parse("{ $match: { x: 2 } }"),
                BsonDocument.Parse("{ $project : { Age : \"$Age\", Name : { $concat : [\"$firstName\", \" \", \"$lastName\"] }, _id : 0 } }"),
                BsonDocument.Parse("{ $group : { _id : \"$Age\", Name : { \"$first\" : \"$Name\" } } }"),
                BsonDocument.Parse("{ $project : { _id: 1 } }")
            };

            var fluent = subject.Aggregate(new AggregateOptions
            {
                AllowDiskUse = true,
                BatchSize = 10,
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
                subject.AggregateAsync(
                    Arg.Do<PipelineStagePipelineDefinition<Person, BsonDocument>>(x => actualPipeline = x),
                    Arg.Do<AggregateOptions>(x => actualOptions = x),
                    Arg.Any<CancellationToken>());

                fluent.ToCursorAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.Aggregate(
                    Arg.Do<PipelineStagePipelineDefinition<Person, BsonDocument>>(x => actualPipeline = x),
                    Arg.Do<AggregateOptions>(x => actualOptions = x),
                    Arg.Any<CancellationToken>());

                fluent.ToCursor(CancellationToken.None);
            }

            var inputSerializer = subject.DocumentSerializer;
            var serializerRegistry = subject.Settings.SerializerRegistry;
            actualPipeline.Render(inputSerializer, serializerRegistry).Documents.Should().Equal(expectedPipeline);
            actualOptions.AllowDiskUse.Should().Be(fluent.Options.AllowDiskUse);
            actualOptions.BatchSize.Should().Be(fluent.Options.BatchSize);
            actualOptions.MaxTime.Should().Be(fluent.Options.MaxTime);
            actualOptions.UseCursor.Should().Be(fluent.Options.UseCursor);
        }

        [Test]
        public void Count_with_an_expression_should_call_collection_with_the_correct_filter(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();

            if (async)
            {
                subject.CountAsync(x => x.FirstName == "Jack");
                subject.Received().CountAsync(Arg.Any<ExpressionFilterDefinition<Person>>(), null, default(CancellationToken));
            }
            else
            {
                subject.Count(x => x.FirstName == "Jack");
                subject.Received().Count(Arg.Any<ExpressionFilterDefinition<Person>>(), null, default(CancellationToken));
            }
        }

        [Test]
        public void DeleteMany_with_an_expression_should_call_collection_with_the_correct_filter(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();

            if (async)
            {
                subject.DeleteManyAsync(x => x.FirstName == "Jack");
                subject.Received().DeleteManyAsync(Arg.Any<ExpressionFilterDefinition<Person>>(), default(CancellationToken));
            }
            else
            {
                subject.DeleteMany(x => x.FirstName == "Jack");
                subject.Received().DeleteMany(Arg.Any<ExpressionFilterDefinition<Person>>(), default(CancellationToken));
            }
        }

        [Test]
        public void DeleteOne_with_an_expression_should_call_collection_with_the_correct_filter(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();

            if (async)
            {
                subject.DeleteOneAsync(x => x.FirstName == "Jack");
                subject.Received().DeleteOneAsync(Arg.Any<ExpressionFilterDefinition<Person>>(), default(CancellationToken));
            }
            else
            {
                subject.DeleteOne(x => x.FirstName == "Jack");
                subject.Received().DeleteOne(Arg.Any<ExpressionFilterDefinition<Person>>(), default(CancellationToken));
            }
        }

        [Test]
        public void Distinct_with_an_expression_should_call_collection_with_the_correct_filter(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();

            if (async)
            {
                subject.DistinctAsync(x => x.LastName, x => x.FirstName == "Jack");
                subject.Received().DistinctAsync(Arg.Any<ExpressionFieldDefinition<Person, string>>(), Arg.Any<ExpressionFilterDefinition<Person>>(), null, default(CancellationToken));
            }
            else
            {
                subject.Distinct(x => x.LastName, x => x.FirstName == "Jack");
                subject.Received().Distinct(Arg.Any<ExpressionFieldDefinition<Person, string>>(), Arg.Any<ExpressionFilterDefinition<Person>>(), null, default(CancellationToken));
            }
        }

        [Test]
        public void ToCursor_should_call_collection_Find_with_correct_options(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var filter = BsonDocument.Parse("{x:1}");
            var projection = BsonDocument.Parse("{y:1}");
            var sort = BsonDocument.Parse("{a:1}");
            var options = new FindOptions
            {
                AllowPartialResults = true,
                BatchSize = 20,
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
                subject.FindAsync(
                    Arg.Do<FilterDefinition<Person>>(x => actualFilter = x),
                    Arg.Do<FindOptions<Person, BsonDocument>>(x => actualOptions = x),
                    Arg.Any<CancellationToken>());

                fluent.ToCursorAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.FindSync(
                    Arg.Do<FilterDefinition<Person>>(x => actualFilter = x),
                    Arg.Do<FindOptions<Person, BsonDocument>>(x => actualOptions = x),
                    Arg.Any<CancellationToken>());

                fluent.ToCursor(CancellationToken.None);
            }

            ((BsonDocumentFilterDefinition<Person>)actualFilter).Document.Should().Be(filter);
            actualOptions.AllowPartialResults.Should().Be(fluent.Options.AllowPartialResults);
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

        [Test]
        public void ToCursor_with_an_expression_should_call_collection_FindAsync_with_correct_options(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var filter = BsonDocument.Parse("{Age:1}");
            var projection = BsonDocument.Parse("{y:1}");
            var sort = BsonDocument.Parse("{a:1}");
            var options = new FindOptions
            {
                AllowPartialResults = true,
                BatchSize = 20,
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
                subject.FindAsync(
                    Arg.Do<FilterDefinition<Person>>(x => actualFilter = x),
                    Arg.Do<FindOptions<Person, BsonDocument>>(x => actualOptions = x),
                    Arg.Any<CancellationToken>());

                fluent.ToCursorAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.FindSync(
                    Arg.Do<FilterDefinition<Person>>(x => actualFilter = x),
                    Arg.Do<FindOptions<Person, BsonDocument>>(x => actualOptions = x),
                    Arg.Any<CancellationToken>());

                fluent.ToCursor(CancellationToken.None);
            }

            actualFilter.Should().BeOfType<ExpressionFilterDefinition<Person>>();
            actualFilter.Render(subject.DocumentSerializer, subject.Settings.SerializerRegistry).Should().Be(filter);
            actualOptions.AllowPartialResults.Should().Be(fluent.Options.AllowPartialResults);
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

        [Test]
        public void FindOneAndDelete_with_an_expression_should_call_collection_with_the_correct_filter(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();

            if (async)
            {
                subject.FindOneAndDeleteAsync(x => x.FirstName == "Jack");

                subject.Received().FindOneAndDeleteAsync<Person>(Arg.Any<ExpressionFilterDefinition<Person>>(), null, default(CancellationToken));
            }
            else
            {
                subject.FindOneAndDelete(x => x.FirstName == "Jack");

                subject.Received().FindOneAndDelete<Person>(Arg.Any<ExpressionFilterDefinition<Person>>(), null, default(CancellationToken));
            }
        }

        [Test]
        public void FindOneAndDelete_with_an_expression_and_result_options_should_call_collection_with_the_correct_filter(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var options = new FindOneAndDeleteOptions<Person, BsonDocument>();

            if (async)
            {
                subject.FindOneAndDeleteAsync(x => x.FirstName == "Jack", options);

                subject.Received().FindOneAndDeleteAsync<BsonDocument>(Arg.Any<ExpressionFilterDefinition<Person>>(), options, default(CancellationToken));
            }
            else
            {
                subject.FindOneAndDelete(x => x.FirstName == "Jack", options);

                subject.Received().FindOneAndDelete<BsonDocument>(Arg.Any<ExpressionFilterDefinition<Person>>(), options, default(CancellationToken));
            }
        }

        [Test]
        public void FindOneAndReplace_with_an_expression_should_call_collection_with_the_correct_filter(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var replacement = new Person();

            if (async)
            {
                subject.FindOneAndReplaceAsync(x => x.FirstName == "Jack", replacement);

                subject.Received().FindOneAndReplaceAsync<Person>(Arg.Any<ExpressionFilterDefinition<Person>>(), replacement, null, default(CancellationToken));
            }
            else
            {
                subject.FindOneAndReplace(x => x.FirstName == "Jack", replacement);

                subject.Received().FindOneAndReplace<Person>(Arg.Any<ExpressionFilterDefinition<Person>>(), replacement, null, default(CancellationToken));
            }
        }

        [Test]
        public void FindOneAndReplaceAsync_with_an_expression_and_result_options_should_call_collection_with_the_correct_filter(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var replacement = new Person();
            var options = new FindOneAndReplaceOptions<Person, BsonDocument>();

            if (async)
            {
                subject.FindOneAndReplaceAsync(x => x.FirstName == "Jack", replacement, options);

                subject.Received().FindOneAndReplaceAsync<BsonDocument>(Arg.Any<ExpressionFilterDefinition<Person>>(), replacement, options, default(CancellationToken));
            }
            else
            {
                subject.FindOneAndReplace(x => x.FirstName == "Jack", replacement, options);

                subject.Received().FindOneAndReplace<BsonDocument>(Arg.Any<ExpressionFilterDefinition<Person>>(), replacement, options, default(CancellationToken));
            }
        }

        [Test]
        public void FindOneAndUpdateAsync_with_an_expression_should_call_collection_with_the_correct_filter(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var update = new BsonDocument();

            if (async)
            {
                subject.FindOneAndUpdateAsync(x => x.FirstName == "Jack", update);

                subject.Received().FindOneAndUpdateAsync<Person>(
                    Arg.Any<ExpressionFilterDefinition<Person>>(),
                    Arg.Is<BsonDocumentUpdateDefinition<Person>>(x => x.Document == update),
                    null,
                    default(CancellationToken));
            }
            else
            {
                subject.FindOneAndUpdate(x => x.FirstName == "Jack", update);

                subject.Received().FindOneAndUpdate<Person>(
                    Arg.Any<ExpressionFilterDefinition<Person>>(),
                    Arg.Is<BsonDocumentUpdateDefinition<Person>>(x => x.Document == update),
                    null,
                    default(CancellationToken));
            }
        }

        [Test]
        public void FindOneAndUpdateAsync_with_an_expression_and_result_options_should_call_collection_with_the_correct_filter(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var update = new BsonDocument();
            var options = new FindOneAndUpdateOptions<Person, BsonDocument>();

            if (async)
            {
                subject.FindOneAndUpdateAsync(x => x.FirstName == "Jack", update, options);

                subject.Received().FindOneAndUpdateAsync<BsonDocument>(
                    Arg.Any<ExpressionFilterDefinition<Person>>(),
                    Arg.Is<BsonDocumentUpdateDefinition<Person>>(x => x.Document == update),
                    options,
                    default(CancellationToken));
            }
            else
            {
                subject.FindOneAndUpdate(x => x.FirstName == "Jack", update, options);

                subject.Received().FindOneAndUpdate<BsonDocument>(
                    Arg.Any<ExpressionFilterDefinition<Person>>(),
                    Arg.Is<BsonDocumentUpdateDefinition<Person>>(x => x.Document == update),
                    options,
                    default(CancellationToken));
            }
        }

        [Test]
        public void ReplaceOneAsync_with_an_expression_should_call_collection_with_the_correct_filter(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var replacement = new Person();

            if (async)
            {
                subject.ReplaceOneAsync(x => x.FirstName == "Jack", replacement);

                subject.Received().ReplaceOneAsync(Arg.Any<ExpressionFilterDefinition<Person>>(), replacement, null, default(CancellationToken));
            }
            else
            {
                subject.ReplaceOne(x => x.FirstName == "Jack", replacement);

                subject.Received().ReplaceOne(Arg.Any<ExpressionFilterDefinition<Person>>(), replacement, null, default(CancellationToken));
            }
        }

        [Test]
        public void UpdateManyAsync_with_an_expression_should_call_collection_with_the_correct_filter(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var update = new BsonDocument();

            if (async)
            {
                subject.UpdateManyAsync(x => x.FirstName == "Jack", update);

                subject.Received().UpdateManyAsync(
                    Arg.Any<ExpressionFilterDefinition<Person>>(),
                    Arg.Is<BsonDocumentUpdateDefinition<Person>>(x => x.Document == update),
                    null,
                    default(CancellationToken));
            }
            else
            {
                subject.UpdateMany(x => x.FirstName == "Jack", update);

                subject.Received().UpdateMany(
                    Arg.Any<ExpressionFilterDefinition<Person>>(),
                    Arg.Is<BsonDocumentUpdateDefinition<Person>>(x => x.Document == update),
                    null,
                    default(CancellationToken));
            }
        }

        [Test]
        public void UpdateOneAsync_with_an_expression_should_call_collection_with_the_correct_filter(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var update = new BsonDocument();

            if (async)
            {
                subject.UpdateOneAsync(x => x.FirstName == "Jack", update);

                subject.Received().UpdateOneAsync(
                    Arg.Any<ExpressionFilterDefinition<Person>>(),
                    Arg.Is<BsonDocumentUpdateDefinition<Person>>(x => x.Document == update),
                    null,
                    default(CancellationToken));
            }
            else
            {
                subject.UpdateOne(x => x.FirstName == "Jack", update);

                subject.Received().UpdateOne(
                    Arg.Any<ExpressionFilterDefinition<Person>>(),
                    Arg.Is<BsonDocumentUpdateDefinition<Person>>(x => x.Document == update),
                    null,
                    default(CancellationToken));
            }
        }

        private bool Matches(object o, BsonDocument doc)
        {
            return o.ToBsonDocument().Equals(doc);
        }

        private IMongoCollection<Person> CreateSubject()
        {
            var settings = new MongoCollectionSettings();
            var subject = Substitute.For<IMongoCollection<Person>>();
            subject.DocumentSerializer.Returns(settings.SerializerRegistry.GetSerializer<Person>());
            subject.Settings.Returns(settings);

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
