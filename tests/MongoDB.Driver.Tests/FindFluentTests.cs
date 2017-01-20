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
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
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
                var renderedProjection = options.Projection.Render(sourceSerializer, serializerRegistry);
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
            [Values(false, true)] bool async)
        {
            var findOptions = new FindOptions<Person, Person>
            {
                Collation = new Collation("en_US"),
                Limit = 1,
                MaxTime = TimeSpan.FromSeconds(1),
                Modifiers = new BsonDocument("$hint", "hint"),
                Skip = 2
            };
            var subject = CreateSubject(findOptions);

            Predicate<CountOptions> countOptionsPredicate = countOptions =>
            {
                return
                    countOptions.Collation == findOptions.Collation &&
                    countOptions.Hint == findOptions.Modifiers["$hint"].AsString &&
                    countOptions.Limit == findOptions.Limit &&
                    countOptions.MaxTime == findOptions.MaxTime &&
                    countOptions.Skip == findOptions.Skip;
            };

            if (async)
            {
                subject.CountAsync().GetAwaiter().GetResult();

                _mockCollection.Verify(
                    c => c.CountAsync(
                        subject.Filter,
                        It.Is<CountOptions>(o => countOptionsPredicate(o)),
                        It.IsAny<CancellationToken>()),
                    Times.Once);
            }
            else
            {
                subject.Count();

                _mockCollection.Verify(
                    c => c.Count(
                        subject.Filter,
                        It.Is<CountOptions>(o => countOptionsPredicate(o)),
                        It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        [Fact]
        public void ToString_should_return_the_correct_string()
        {
            var subject = CreateSubject();
            subject.Filter = new BsonDocument("Age", 20);
            subject.Options.Collation = new Collation("en_US");
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
                ".collation({ \"locale\" : \"en_US\" })" +
                ".sort({ \"LastName\" : 1, \"FirstName\" : -1 })" +
                ".skip(2)" +
                ".limit(10)" +
                ".maxTime(2000)" +
                "._addSpecial(\"$comment\", \"awesome\")" +
                "._addSpecial(\"$explain\", true)" +
                "._addSpecial(\"$hint\", \"ix_1\")");
        }

        private IFindFluent<Person, Person> CreateSubject(FindOptions<Person, Person> options = null)
        {
            var settings = new MongoCollectionSettings();
            _mockCollection = new Mock<IMongoCollection<Person>>();
            _mockCollection.SetupGet(c => c.DocumentSerializer).Returns(BsonSerializer.SerializerRegistry.GetSerializer<Person>());
            _mockCollection.SetupGet(c => c.Settings).Returns(settings);
            options = options ?? new FindOptions<Person, Person>();
            var subject = new FindFluent<Person, Person>(_mockCollection.Object, new BsonDocument(), options);

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