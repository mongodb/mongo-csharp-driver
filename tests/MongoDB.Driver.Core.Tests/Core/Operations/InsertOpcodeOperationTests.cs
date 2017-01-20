/* Copyright 2013-2016 MongoDB Inc.
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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class InsertOpcodeOperationTests : OperationTestBase
    {
        private BatchableSource<BsonDocument> _documentSource;

        public InsertOpcodeOperationTests()
        {
            _documentSource = new BatchableSource<BsonDocument>(new[] 
            { 
                BsonDocument.Parse("{_id: 1, x: 1}")
            });
        }

        [Fact]
        public void Constructor_should_throw_when_collection_namespace_is_null()
        {
            Action act = () => new InsertOpcodeOperation<BsonDocument>(null, _documentSource, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_document_source_is_null()
        {
            Action act = () => new InsertOpcodeOperation<BsonDocument>(_collectionNamespace, null, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_serializer_is_null()
        {
            Action act = () => new InsertOpcodeOperation<BsonDocument>(_collectionNamespace, _documentSource, null, _messageEncoderSettings);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_message_encoder_settings_is_null()
        {
            Action act = () => new InsertOpcodeOperation<BsonDocument>(_collectionNamespace, _documentSource, BsonDocumentSerializer.Instance, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_initialize_object()
        {
            var subject = new InsertOpcodeOperation<BsonDocument>(_collectionNamespace, _documentSource, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.CollectionNamespace.FullName.Should().Be(_collectionNamespace.FullName);
            subject.DocumentSource.Should().NotBeNull();
            subject.Serializer.Should().BeSameAs(BsonDocumentSerializer.Instance);
            subject.MessageEncoderSettings.Should().BeEquivalentTo(_messageEncoderSettings);
        }

        [Fact]
        public void ContinueOnError_should_work()
        {
            var subject = new InsertOpcodeOperation<BsonDocument>(_collectionNamespace, _documentSource, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.ContinueOnError.Should().Be(false);

            subject.ContinueOnError = true;

            subject.ContinueOnError.Should().Be(true);
        }

        [Fact]
        public void MaxBatchCount_should_work()
        {
            var subject = new InsertOpcodeOperation<BsonDocument>(_collectionNamespace, _documentSource, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.MaxBatchCount.Should().Be(null);

            subject.MaxBatchCount = 20;

            subject.MaxBatchCount.Should().Be(20);
        }

        [Fact]
        public void MaxDocumentSize_should_work()
        {
            var subject = new InsertOpcodeOperation<BsonDocument>(_collectionNamespace, _documentSource, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.MaxDocumentSize.Should().Be(null);

            subject.MaxDocumentSize = 20;

            subject.MaxDocumentSize.Should().Be(20);
        }

        [Fact]
        public void MaxMessageSize_should_work()
        {
            var subject = new InsertOpcodeOperation<BsonDocument>(_collectionNamespace, _documentSource, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.MaxMessageSize.Should().Be(null);

            subject.MaxMessageSize = 20;

            subject.MaxMessageSize.Should().Be(20);
        }

        [Fact]
        public void WriteConcern_should_work()
        {
            var subject = new InsertOpcodeOperation<BsonDocument>(_collectionNamespace, _documentSource, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.WriteConcern.Should().Be(WriteConcern.Acknowledged);

            subject.WriteConcern = WriteConcern.W2;

            subject.WriteConcern.Should().Be(WriteConcern.W2);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_insert_a_single_document(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            DropCollection();
            var subject = new InsertOpcodeOperation<BsonDocument>(_collectionNamespace, _documentSource, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);
            result.Should().HaveCount(1);

            var list = ReadAllFromCollection(async);
            list.Should().HaveCount(1);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_insert_multiple_documents(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            DropCollection();
            var documentSource = new BatchableSource<BsonDocument>(new[] 
            {
                BsonDocument.Parse("{_id: 1, x: 1}"),
                BsonDocument.Parse("{_id: 2, x: 2}"),
                BsonDocument.Parse("{_id: 3, x: 3}"),
                BsonDocument.Parse("{_id: 4, x: 4}"),
            });
            var subject = new InsertOpcodeOperation<BsonDocument>(_collectionNamespace, documentSource, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);
            result.Should().HaveCount(1);

            var list = ReadAllFromCollection(async);
            list.Should().HaveCount(4);
        }
    }
}
