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
using System.Reflection;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.TestHelpers.XunitExtensions;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class FindFluentTests
    {
        private Mock<IMongoCollection<Person>> _mockCollection;

        [Theory]
        [ParameterAttributeData]
        public void As_should_change_the_result_type(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();

            var result = subject.As<BsonDocument>();

            Predicate<FindOptions<Person, BsonDocument>> hasExpectedProjection = options =>
            {
                var serializerRegistry = BsonSerializer.SerializerRegistry;
                var sourceSerializer = serializerRegistry.GetSerializer<Person>();
                var renderedProjection = options.Projection.Render(new(sourceSerializer, serializerRegistry));
                return renderedProjection.Document == null && renderedProjection.ProjectionSerializer is BsonDocumentSerializer;
            };

            if (async)
            {
                result.ToCursorAsync().GetAwaiter().GetResult();

                _mockCollection.Verify(
                    c => c.FindAsync<BsonDocument>(
                        subject.Filter,
                        It.Is<FindOptions<Person, BsonDocument>>(options => hasExpectedProjection(options)),
                        CancellationToken.None),
                    Times.Once);
            }
            else
            {
                result.ToCursor();

                _mockCollection.Verify(
                    c => c.FindSync<BsonDocument>(
                        subject.Filter,
                        It.Is<FindOptions<Person, BsonDocument>>(options => hasExpectedProjection(options)),
                        CancellationToken.None),
                    Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Count_should_call_collection_Count(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var session = CreateSession(usingSession);
            var filter = new BsonDocumentFilterDefinition<Person>(new BsonDocument("filter", 1));
            var hint = new BsonDocument("hint", 1);
            var findOptions = new FindOptions<Person>
            {
                Collation = new Collation("en-us"),
                Hint = hint,
                Limit = 1,
                MaxTime = TimeSpan.FromSeconds(2),
                Skip = 3
            };
            var subject = CreateSubject(session: session, filter: filter, options: findOptions);
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            Predicate<CountOptions> matchesExpectedOptions = countOptions =>
                countOptions.Collation.Equals(findOptions.Collation) &&
                countOptions.Hint.Equals(hint) &&
                countOptions.Limit.Equals((long?)findOptions.Limit) &&
                countOptions.MaxTime.Equals(findOptions.MaxTime) &&
                countOptions.Skip.Equals((long?)findOptions.Skip);

            if (async)
            {
                if (usingSession)
                {
#pragma warning disable 618
                    subject.CountAsync(cancellationToken).GetAwaiter().GetResult();
                    _mockCollection.Verify(
                        m => m.CountAsync(
                            session,
                            filter,
                            It.Is<CountOptions>(o => matchesExpectedOptions(o)),
                            cancellationToken),
                        Times.Once);
#pragma warning restore
                }
                else
                {
#pragma warning disable 618
                    subject.CountAsync(cancellationToken).GetAwaiter().GetResult();
                    _mockCollection.Verify(
                        m => m.CountAsync(
                            filter,
                            It.Is<CountOptions>(o => matchesExpectedOptions(o)),
                            cancellationToken),
                        Times.Once);
                }
#pragma warning restore
            }
            else
            {
                if (usingSession)
                {
#pragma warning disable 618
                    subject.Count(cancellationToken);
                    _mockCollection.Verify(
                        m => m.Count(
                            session,
                            filter,
                            It.Is<CountOptions>(o => matchesExpectedOptions(o)),
                            cancellationToken),
                        Times.Once);
#pragma warning restore
                }
                else
                {
#pragma warning disable 618
                    subject.Count(cancellationToken);
                    _mockCollection.Verify(
                        m => m.Count(
                            filter,
                            It.Is<CountOptions>(o => matchesExpectedOptions(o)),
                            cancellationToken),
                        Times.Once);
#pragma warning restore
                }
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void CountDocuments_should_call_collection_CountDocuments(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var session = CreateSession(usingSession);
            var filter = new BsonDocumentFilterDefinition<Person>(new BsonDocument("filter", 1));
            var hint = new BsonDocument("hint", 1);
            var findOptions = new FindOptions<Person>
            {
                Collation = new Collation("en-us"),
                Hint = hint,
                Limit = 1,
                MaxTime = TimeSpan.FromSeconds(2),
                Skip = 3
            };
            var subject = CreateSubject(session: session, filter: filter, options: findOptions);
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            Predicate<CountOptions> matchesExpectedOptions = countOptions =>
                countOptions.Collation.Equals(findOptions.Collation) &&
                countOptions.Hint.Equals(hint) &&
                countOptions.Limit.Equals((long?)findOptions.Limit) &&
                countOptions.MaxTime.Equals(findOptions.MaxTime) &&
                countOptions.Skip.Equals((long?)findOptions.Skip);

            if (async)
            {
                if (usingSession)
                {
                    subject.CountDocumentsAsync(cancellationToken).GetAwaiter().GetResult();
                    _mockCollection.Verify(
                        m => m.CountDocumentsAsync(
                            session,
                            filter,
                            It.Is<CountOptions>(o => matchesExpectedOptions(o)),
                            cancellationToken),
                        Times.Once);
                }
                else
                {
                    subject.CountDocumentsAsync(cancellationToken).GetAwaiter().GetResult();
                    _mockCollection.Verify(
                        m => m.CountDocumentsAsync(
                            filter,
                            It.Is<CountOptions>(o => matchesExpectedOptions(o)),
                            cancellationToken),
                        Times.Once);
                }
            }
            else
            {
                if (usingSession)
                {
                    subject.CountDocuments(cancellationToken);
                    _mockCollection.Verify(
                        m => m.CountDocuments(
                            session,
                            filter,
                            It.Is<CountOptions>(o => matchesExpectedOptions(o)),
                            cancellationToken),
                        Times.Once);
                }
                else
                {
                    subject.CountDocuments(cancellationToken);
                    _mockCollection.Verify(
                        m => m.CountDocuments(
                            filter,
                            It.Is<CountOptions>(o => matchesExpectedOptions(o)),
                            cancellationToken),
                        Times.Once);
                }
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void ToCursor_should_call_collection_Find_with_expected_arguments(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var session = usingSession ? new Mock<IClientSessionHandle>().Object : null;
            var filter = Builders<Person>.Filter.Eq("_id", 1);
            var options = new FindOptions<Person, Person>();
            var subject = CreateSubject(session, filter, options);
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            if (async)
            {
                var _ = subject.ToCursorAsync(cancellationToken).GetAwaiter().GetResult();

                if (usingSession)
                {
                    _mockCollection.Verify(
                        collection => collection.FindAsync(
                            session,
                            filter,
                            options,
                            cancellationToken),
                    Times.Once);
                }
                else
                {
                    _mockCollection.Verify(
                       collection => collection.FindAsync(
                           filter,
                           options,
                           cancellationToken),
                   Times.Once);
                }
            }
            else
            {
                var _ = subject.ToCursor(cancellationToken);

                if (usingSession)
                {
                    _mockCollection.Verify(
                        collection => collection.FindSync(
                            session,
                            filter,
                            options,
                            cancellationToken),
                    Times.Once);
                }
                else
                {
                    _mockCollection.Verify(
                       collection => collection.FindSync(
                           filter,
                           options,
                           cancellationToken),
                   Times.Once);
                }
            }
        }

        [Fact]
        public void ToString_should_return_the_correct_string()
        {
            var subject = CreateSubject();
            subject.Filter = new BsonDocument("Age", 20);
            subject.Options.Collation = new Collation("en_US");
            subject.Options.Comment = "awesome";
            subject.Options.Hint = "x_3";
            subject.Options.Max = new BsonDocument("max", 5);
            subject.Options.MaxTime = TimeSpan.FromSeconds(2);
            subject.Options.Min = new BsonDocument("min", 2);
            subject.Options.ReturnKey = true;
            subject.Options.ShowRecordId = true;

            var find = subject
                .SortBy(x => x.LastName)
                .ThenByDescending(x => x.FirstName)
                .Skip(2)
                .Limit(10)
                .Project(x => x.FirstName + " " + x.LastName);

            var str = find.ToString();

            var expectedProjection =
                "{ \"_v\" : { \"$concat\" : [\"$FirstName\", \" \", \"$LastName\"] }, \"_id\" : 0 }";

            str.Should().Be(
                "find({ \"Age\" : 20 }, " + expectedProjection + ")" +
                ".collation({ \"locale\" : \"en_US\" })" +
                ".sort({ \"LastName\" : 1, \"FirstName\" : -1 })" +
                ".skip(2)" +
                ".limit(10)" +
                ".maxTime(2000)" +
                ".hint(x_3)" +
                ".max({ \"max\" : 5 })" +
                ".min({ \"min\" : 2 })" +
                ".returnKey(true)" +
                ".showRecordId(true)" +
                "._addSpecial(\"$comment\", \"awesome\")");
        }

        // private methods
        private IClientSessionHandle CreateSession(bool usingSession)
        {
            return usingSession ? Mock.Of<IClientSessionHandle>() : null;
        }

        private IFindFluent<Person, Person> CreateSubject(IClientSessionHandle session = null, FilterDefinition<Person> filter = null, FindOptions<Person, Person> options = null)
        {
            var clientSettings = new MongoClientSettings();
            var mockClient = new Mock<IMongoClient>();
            mockClient.SetupGet(c => c.Settings).Returns(clientSettings);

            var mockDatabase = new Mock<IMongoDatabase>();
            mockDatabase.SetupGet(d => d.Client).Returns(mockClient.Object);

            var collectionSettings = new MongoCollectionSettings();
            collectionSettings.SerializationDomain = BsonSerializer.CreateSerializationDomain();
            _mockCollection = new Mock<IMongoCollection<Person>>();
            _mockCollection.SetupGet(c => c.Database).Returns(mockDatabase.Object);
            _mockCollection.SetupGet(c => c.DocumentSerializer).Returns(BsonSerializer.SerializerRegistry.GetSerializer<Person>());
            _mockCollection.SetupGet(c => c.Settings).Returns(collectionSettings);
            filter = filter ?? new BsonDocument();
            options = options ?? new FindOptions<Person, Person>();
            var subject = new FindFluent<Person, Person>(session: session, collection: _mockCollection.Object, filter: filter, options: options);

            return subject;
        }

        public class Person
        {
            public string FirstName;
            public string LastName;
            public int Age;
        }
    }

    internal static class FindFluentReflector
    {
        public static IMongoCollection<TDocument> _collection<TDocument, TProjection>(this FindFluent<TDocument, TProjection> obj)
        {
            var fieldInfo = typeof(FindFluent<TDocument, TProjection>).GetField("_collection", BindingFlags.NonPublic | BindingFlags.Instance);
            return (IMongoCollection<TDocument>)fieldInfo.GetValue(obj);
        }

        public static FilterDefinition<TDocument> _filter<TDocument, TProjection>(this FindFluent<TDocument, TProjection> obj)
        {
            var fieldInfo = typeof(FindFluent<TDocument, TProjection>).GetField("_filter", BindingFlags.NonPublic | BindingFlags.Instance);
            return (FilterDefinition<TDocument>)fieldInfo.GetValue(obj);
        }

        public static FindOptions<TDocument, TProjection> _options<TDocument, TProjection>(this FindFluent<TDocument, TProjection> obj)
        {
            var fieldInfo = typeof(FindFluent<TDocument, TProjection>).GetField("_options", BindingFlags.NonPublic | BindingFlags.Instance);
            return (FindOptions<TDocument, TProjection>)fieldInfo.GetValue(obj);
        }

        public static IClientSessionHandle _session<TDocument, TProjection>(this FindFluent<TDocument, TProjection> obj)
        {
            var fieldInfo = typeof(FindFluent<TDocument, TProjection>).GetField("_session", BindingFlags.NonPublic | BindingFlags.Instance);
            return (IClientSessionHandle)fieldInfo.GetValue(obj);
        }
    }
}
