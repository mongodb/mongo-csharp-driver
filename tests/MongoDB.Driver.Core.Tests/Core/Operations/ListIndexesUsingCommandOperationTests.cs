﻿/* Copyright 2013-present MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class ListIndexesUsingCommandOperationTests : OperationTestBase
    {
        // test methods
        [Fact]
        public void CollectionNamespace_get_should_return_expected_result()
        {
            var subject = new ListIndexesUsingCommandOperation(_collectionNamespace, _messageEncoderSettings);

            var result = subject.CollectionNamespace;

            result.Should().BeSameAs(_collectionNamespace);
        }

        [Fact]
        public void constructor_should_initialize_subject()
        {
            var subject = new ListIndexesUsingCommandOperation(_collectionNamespace, _messageEncoderSettings);

            subject.CollectionNamespace.Should().BeSameAs(_collectionNamespace);
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);
            subject.RetryRequested.Should().BeFalse();
        }

        [Fact]
        public void constructor_should_throw_when_collectionNamespace_is_null()
        {
            Action action = () => new ListIndexesUsingCommandOperation(null, _messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("collectionNamespace");
        }

        [Fact]
        public void BatchSize_get_and_set_should_work()
        {
            var subject = new ListIndexesUsingCommandOperation(_collectionNamespace, _messageEncoderSettings);
            int batchSize = 2;

            subject.BatchSize = batchSize;
            var result = subject.BatchSize;

            result.Should().Be(batchSize);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureCollectionExists(async);
            var subject = new ListIndexesUsingCommandOperation(_collectionNamespace, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);
            var list = ReadCursorToEnd(result, async);

            list.Select(index => index["name"].AsString).Should().BeEquivalentTo("_id_");
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_the_expected_result_when_batchSize_is_used([Values(false, true)] bool async)
        {
            RequireServer.Check();
            EnsureCollectionExists(async);
            int batchSize = 3;
            var subject = new ListIndexesUsingCommandOperation(_collectionNamespace, _messageEncoderSettings)
            {
                BatchSize = batchSize
            };

            using (var result = ExecuteOperation(subject, async) as AsyncCursor<BsonDocument>)
            {
                result._batchSize().Should().Be(batchSize);
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_collection_does_not_exist(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            DropCollection(async);
            var subject = new ListIndexesUsingCommandOperation(_collectionNamespace, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);
            var list = ReadCursorToEnd(result, async);

            list.Count.Should().Be(0);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_database_does_not_exist(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            DropDatabase(async);
            var subject = new ListIndexesUsingCommandOperation(_collectionNamespace, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);
            var list = ReadCursorToEnd(result, async);

            list.Count.Should().Be(0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_binding_is_null(
            [Values(false, true)]
            bool async)
        {
            var subject = new ListIndexesUsingCommandOperation(_collectionNamespace, _messageEncoderSettings);
            IReadBinding binding = null;

            Action action = () => ExecuteOperation(subject, binding, async);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("binding");
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_send_session_id_when_supported(
            [Values(false, true)] bool async)
        {
            RequireServer.Check();
            EnsureCollectionExists(async);
            var subject = new ListIndexesUsingCommandOperation(_collectionNamespace, _messageEncoderSettings);

            VerifySessionIdWasSentWhenSupported(subject, "listIndexes", async);
        }

        [Fact]
        public void MessageEncoderSettings_get_should_return_expected_result()
        {
            var subject = new ListIndexesUsingCommandOperation(_collectionNamespace, _messageEncoderSettings);

            var result = subject.MessageEncoderSettings;

            result.Should().BeSameAs(_messageEncoderSettings);
        }

        [Theory]
        [ParameterAttributeData]
        public void RetryRequested_get_should_return_expected_result(
            [Values(false, true)] bool value)
        {
            var subject = new ListIndexesUsingCommandOperation(_collectionNamespace, _messageEncoderSettings);

            subject.RetryRequested = value;
            var result = subject.RetryRequested;

            result.Should().Be(value);
        }

        // helper methods
        public void DropCollection(bool async)
        {
            var operation = new DropCollectionOperation(_collectionNamespace, _messageEncoderSettings);
            ExecuteOperation(operation, async);
        }

        public void DropDatabase(bool async)
        {
            var operation = new DropDatabaseOperation(_collectionNamespace.DatabaseNamespace, _messageEncoderSettings);
            ExecuteOperation(operation, async);
        }

        public void EnsureCollectionExists(bool async)
        {
            var requests = new[] { new InsertRequest(new BsonDocument("_id", ObjectId.GenerateNewId())) };
            var operation = new BulkInsertOperation(_collectionNamespace, requests, _messageEncoderSettings);
            ExecuteOperation(operation, async);
        }
    }
}
