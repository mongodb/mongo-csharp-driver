/* Copyright 2010-2014 MongoDB Inc.
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
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Tests;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver
{
    public class MongoCollectionImplTests
    {
        private MockOperationExecutor _operationExecutor;
        private MongoCollectionImpl<BsonDocument> _subject;

        [SetUp]
        public void Setup()
        {
            var settings = new MongoCollectionSettings();
            var dbSettings = new MongoDatabaseSettings();
            dbSettings.ApplyDefaultValues(new MongoServerSettings());
            settings.ApplyDefaultValues(dbSettings);
            _operationExecutor = new MockOperationExecutor();
            _subject = new MongoCollectionImpl<BsonDocument>(
                new CollectionNamespace("foo", "bar"),
                settings,
                Substitute.For<ICluster>(),
                _operationExecutor);
        }

        [Test]
        public void CollectionName_should_be_set()
        {
            _subject.CollectionNamespace.CollectionName.Should().Be("bar");
        }

        [Test]
        public void Settings_should_be_set()
        {
            _subject.Settings.Should().NotBeNull();
        }

        [Test]
        public async Task CountAsync_should_execute_the_CountOperation()
        {
            var model = new CountModel
            {
                Criteria = new BsonDocument("x", 1),
                Hint = "funny",
                Limit = 10,
                MaxTime = TimeSpan.FromSeconds(20),
                Skip = 30
            };
            await _subject.CountAsync(model, Timeout.InfiniteTimeSpan, CancellationToken.None);

            var call = _operationExecutor.GetReadCall<long>();

            call.Operation.Should().BeOfType<CountOperation>();
            var operation = (CountOperation)call.Operation;
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.Criteria.Should().Be((BsonDocument)model.Criteria);
            operation.Hint.Should().Be((string)model.Hint);
            operation.Limit.Should().Be(model.Limit);
            operation.MaxTime.Should().Be(model.MaxTime);
            operation.Skip.Should().Be(model.Skip);
        }

        [Test]
        public async Task DistinctAsync_should_execute_the_DistinctOperation()
        {
            var model = new DistinctModel<int>("a.b")
            {
                Criteria = new BsonDocument("x", 1),
                MaxTime = TimeSpan.FromSeconds(20),
            };

            await _subject.DistinctAsync(model, Timeout.InfiniteTimeSpan, CancellationToken.None);

            var call = _operationExecutor.GetReadCall<IReadOnlyList<int>>();

            call.Operation.Should().BeOfType<DistinctOperation<int>>();
            var operation = (DistinctOperation<int>)call.Operation;
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.FieldName.Should().Be("a.b");
            operation.Criteria.Should().Be((BsonDocument)model.Criteria);
            operation.MaxTime.Should().Be(model.MaxTime);
        }
    }
}