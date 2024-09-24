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
using System.Collections.Generic;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders
{
    internal sealed class ClientBulkWriteOpsSectionFormatter : ICommandMessageSectionFormatter<ClientBulkWriteOpsCommandMessageSection>, IBulkWriteModelVisitor
    {
        private readonly long? _maxSize;
        private readonly List<string> _nsInfos;
        private BsonSerializationContext _serializationContext;
        private IBsonSerializerRegistry _serializerRegistry;

        public ClientBulkWriteOpsSectionFormatter(long? maxSize)
        {
            _maxSize = maxSize;
            _nsInfos = new List<string>();
        }

        public void FormatSection(ClientBulkWriteOpsCommandMessageSection section, IBsonWriter writer)
        {
            if (writer is not BsonBinaryWriter binaryWriter)
            {
                throw new ArgumentException("Writer must be an instance of BsonBinaryWriter");
            }

            _serializerRegistry = BsonSerializer.SerializerRegistry;
            _serializationContext = BsonSerializationContext.CreateRoot(binaryWriter);
            var stream = binaryWriter.BsonStream;
            var startPosition = stream.Position;

            stream.WriteInt32(0); // size
            stream.WriteCString("ops");

            var batch = section.Documents;
            var maxDocumentSize = section.MaxDocumentSize ?? binaryWriter.Settings.MaxDocumentSize;
            binaryWriter.PushSettings(s => ((BsonBinaryWriterSettings)s).MaxDocumentSize = maxDocumentSize);
            binaryWriter.PushElementNameValidator(NoOpElementNameValidator.Instance);
            try
            {
                var maxBatchCount = Math.Min(batch.Count, section.MaxBatchCount ?? int.MaxValue);
                var processedCount = maxBatchCount;
                for (var i = 0; i < maxBatchCount; i++)
                {
                    var documentStartPosition = stream.Position;
                    var document = batch.Items[batch.Offset + i];

                    document.Visit(this);

                    var writtenSize = stream.Position - startPosition;
                    if (writtenSize > _maxSize && batch.CanBeSplit && i > 0)
                    {
                        stream.Position = documentStartPosition;
                        stream.SetLength(documentStartPosition);
                        processedCount = i;
                        break;
                    }
                }
                batch.SetProcessedCount(processedCount);

                stream.BackpatchSize(startPosition);

                stream.WriteByte((byte)section.PayloadType);
                startPosition = stream.Position;

                stream.WriteInt32(0); // size
                stream.WriteCString("nsInfo");

                for (var i = 0; i < _nsInfos.Count; i++)
                {
                    writer.WriteStartDocument();
                    writer.WriteName("ns");
                    writer.WriteString(_nsInfos[i]);
                    writer.WriteEndDocument();
                }

                stream.BackpatchSize(startPosition);
            }
            finally
            {
                writer.PopElementNameValidator();
                writer.PopSettings();
            }
        }

        public void Visit(BulkWriteModel bulkWriteModel)
        {
        }

        public void VisitDeleteMany<TDocument>(BulkWriteDeleteManyModel<TDocument> deleteManyModel) => throw new System.NotImplementedException();

        public void VisitDeleteOne<TDocument>(BulkWriteDeleteOneModel<TDocument> deleteOneModel) => throw new System.NotImplementedException();

        public void VisitInsertOne<TDocument>(BulkWriteInsertOneModel<TDocument> insertOneModel)
        {
            var nsInfoIndex = EnsureNsInfo(insertOneModel.Namespace.FullName);

            var writer = _serializationContext.Writer;
            writer.WriteStartDocument();
            writer.WriteName("insert");
            writer.WriteInt32(nsInfoIndex);
            writer.WriteName("document");

            var documentSerializer = insertOneModel.Serializer ?? _serializerRegistry.GetSerializer<TDocument>();
            documentSerializer.Serialize(_serializationContext, insertOneModel.Document);

            writer.WriteEndDocument();
        }

        public void VisitReplaceOne<TDocument>(BulkWriteReplaceOneModel<TDocument> replaceOneModel) => throw new NotImplementedException();

        public void VisitUpdateMany<TDocument>(BulkWriteUpdateManyModel<TDocument> updateOneModel) => throw new System.NotImplementedException();

        public void VisitUpdateOne<TDocument>(BulkWriteUpdateOneModel<TDocument> updateManyModel) => throw new System.NotImplementedException();

        private int EnsureNsInfo(string nsName)
        {
            if (!_nsInfos.Contains(nsName))
            {
                _nsInfos.Add(nsName);
            }

            return _nsInfos.IndexOf(nsName);
        }
    }
}
