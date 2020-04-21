/* Copyright 2017-present MongoDB Inc.
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
using MongoDB.Bson.TestHelpers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq
{
    public class MongoQueryProviderImplTests
    {
        [Theory]
        [ParameterAttributeData]
        public void constructor_should_initialize_instance(
            [Values(false, true)] bool withSession)
        {
            var collection = Mock.Of<IMongoCollection<BsonDocument>>();
            var session = withSession ? Mock.Of<IClientSessionHandle>() : null;
            var options = new AggregateOptions();

            var subject = new MongoQueryProviderImpl<BsonDocument>(collection, session, options);

            subject._collection().Should().BeSameAs(collection);
            subject._session().Should().BeSameAs(session);
            subject._options().Should().BeSameAs(options);
        }

        [Fact]
        public void constructor_should_throw_whe_collection_is_null()
        {
            var session = Mock.Of<IClientSessionHandle>();
            var options = new AggregateOptions();

            var exception = Record.Exception(() => new MongoQueryProviderImpl<BsonDocument>(collection: null, session, options));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("collection");
        }

        [Fact]
        public void constructor_should_throw_whe_options_is_null()
        {
            var collection = Mock.Of<IMongoCollection<BsonDocument>>();
            var session = Mock.Of<IClientSessionHandle>();

            var exception = Record.Exception(() => new MongoQueryProviderImpl<BsonDocument>(collection, session, options: null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("options");
        }

        [Theory]
        [ParameterAttributeData]
        public void ExecuteModel_should_call_Execute_with_expected_arguments(
            [Values(false, true)] bool withSession,
            [Values(false, true)] bool async)
        {
            var collection = Mock.Of<IMongoCollection<BsonDocument>>();
            var session = withSession ? Mock.Of<IClientSessionHandle>() : null;
            var options = new AggregateOptions();
            var subject = new MongoQueryProviderImpl<BsonDocument>(collection, session, options);
            var mockModel = new Mock<QueryableExecutionModel>();
            var cancellationToken = new CancellationToken();

            if (async)
            {
                subject.ExecuteModelAsync(mockModel.Object, cancellationToken).GetAwaiter().GetResult();

                mockModel.Verify(m => m.ExecuteAsync(collection, session, options, cancellationToken), Times.Once);
            }
            else
            {
                subject.ExecuteModel(mockModel.Object);

                mockModel.Verify(m => m.Execute(collection, session, options), Times.Once);
            }
        }
    }

    internal static class MongoQueryProviderImplReflector
    {
        public static IMongoCollection<TDocument> _collection<TDocument>(this MongoQueryProviderImpl<TDocument> obj)
            => (IMongoCollection<TDocument>)Reflector.GetFieldValue(obj, nameof(_collection));

        public static AggregateOptions _options<TDocument>(this MongoQueryProviderImpl<TDocument> obj)
            => (AggregateOptions)Reflector.GetFieldValue(obj, nameof(_options));

        public static IClientSessionHandle _session<TDocument>(this MongoQueryProviderImpl<TDocument> obj)
            => (IClientSessionHandle)Reflector.GetFieldValue(obj, nameof(_session));
    }
}
