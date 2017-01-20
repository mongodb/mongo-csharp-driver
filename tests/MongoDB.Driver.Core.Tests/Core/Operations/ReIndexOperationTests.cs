/* Copyright 2016 MongoDB Inc.
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
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class ReIndexOperationTests : OperationTestBase
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var subject = new ReIndexOperation(_collectionNamespace, _messageEncoderSettings);

            subject.CollectionNamespace.Should().BeSameAs(_collectionNamespace);
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);
            subject.WriteConcern.Should().BeNull();
        }

        [Fact]
        public void constructor_should_throw_when_collectionNamespace_is_null()
        {
            var exception = Record.Exception(() => new ReIndexOperation(null, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("collectionNamespace");
        }

        [Fact]
        public void constructor_should_throw_when_messageEncoderSettings_is_null()
        {
            var exception = Record.Exception(() => new ReIndexOperation(_collectionNamespace, null));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("messageEncoderSettings");
        }

        [Theory]
        [ParameterAttributeData]
        public void WriteConcern_get_and_set_should_work(
            [Values(null, 1, 2)]
            int? w)
        {
            var subject = new ReIndexOperation(_collectionNamespace, _messageEncoderSettings);
            var value = w.HasValue ? new WriteConcern(w.Value) : null;

            subject.WriteConcern = value;
            var result = subject.WriteConcern;

            result.Should().BeSameAs(value);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureCollectionExists();
            var subject = new ReIndexOperation(_collectionNamespace, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

            result["ok"].ToBoolean().Should().BeTrue();
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_binding_is_null(
            [Values(false, true)]
            bool async)
        {
            var subject = new ReIndexOperation(_collectionNamespace, _messageEncoderSettings);

            var exception = Record.Exception(() =>
            {
                if (async)
                {
                    subject.ExecuteAsync(null, CancellationToken.None).GetAwaiter().GetResult(); ;
                }
                else
                {
                    subject.Execute(null, CancellationToken.None);
                }
            });
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_a_write_concern_error_occurs(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.CommandsThatWriteAcceptWriteConcern).ClusterType(ClusterType.ReplicaSet);
            EnsureCollectionExists();
            var subject = new ReIndexOperation(_collectionNamespace, _messageEncoderSettings)
            {
                WriteConcern = new WriteConcern(9)
            };

            async = false;
            var exception = Record.Exception(() => ExecuteOperation(subject, async));

            exception.Should().BeOfType<MongoWriteConcernException>();
        }

        [Fact]
        public void CreateCommand_should_return_expected_result()
        {
            var subject = new ReIndexOperation(_collectionNamespace, _messageEncoderSettings);

            var result = subject.CreateCommand(null);

            var expectedResult = new BsonDocument
            {
                { "reIndex", _collectionNamespace.CollectionName }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_WriteConcern_is_set(
            [Values(null, 1, 2)]
            int? w,
            [Values(false, true)]
            bool isWriteConcernSupported)
        {
            var writeConcern = w.HasValue ? new WriteConcern(w.Value) : null;
            var subject = new ReIndexOperation(_collectionNamespace, _messageEncoderSettings)
            {
                WriteConcern = writeConcern
            };
            var serverVersion = Feature.CommandsThatWriteAcceptWriteConcern.SupportedOrNotSupportedVersion(isWriteConcernSupported);

            var result = subject.CreateCommand(serverVersion);

            var expectedResult = new BsonDocument
            {
                { "reIndex", _collectionNamespace.CollectionName },
                { "writeConcern", () => writeConcern.ToBsonDocument(), writeConcern != null && isWriteConcernSupported }
            };
            result.Should().Be(expectedResult);
        }

        // private methods
        public void EnsureCollectionExists()
        {
            var document = new BsonDocument("x", 1);
            var requests = new[] { new InsertRequest(document) };
            var operation = new BulkInsertOperation(_collectionNamespace, requests, _messageEncoderSettings);
            ExecuteOperation(operation);
        }
    }
}