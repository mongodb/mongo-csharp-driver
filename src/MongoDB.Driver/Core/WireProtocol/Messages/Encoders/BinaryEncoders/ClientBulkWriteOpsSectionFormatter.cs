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
using System.IO;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.Operations.ElementNameValidators;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders
{
    internal sealed class ClientBulkWriteOpsSectionFormatter : ICommandMessageSectionFormatter<ClientBulkWriteOpsCommandMessageSection>, IBulkWriteModelVisitor, IDisposable
    {
        private readonly long? _maxSize;
        private readonly Dictionary<string, int> _nsInfos;
        private readonly BsonBinaryWriter _nsInfoWriter;
        private BsonSerializationContext _serializationContext;
        private IBsonSerializerRegistry _serializerRegistry;
        private RenderArgs<BsonDocument> _renderArgs;

        public ClientBulkWriteOpsSectionFormatter(long? maxSize)
        {
            _maxSize = (maxSize ?? long.MaxValue) - 1000; // according to spec we should leave some extra space for further overhead
            if (_maxSize <= 0)
            {
                throw new InvalidOperationException("Section's size limit is too small.");
            }

            _nsInfos = new Dictionary<string, int>();
            _nsInfoWriter = new BsonBinaryWriter(new MemoryStream());
        }

        public void Dispose() => _nsInfoWriter?.Dispose();

        public void FormatSection(ClientBulkWriteOpsCommandMessageSection section, IBsonWriter writer)
        {
            if (writer is not BsonBinaryWriter binaryWriter)
            {
                throw new ArgumentException("Writer must be an instance of BsonBinaryWriter.");
            }

            _renderArgs = section.RenderArgs;
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
                    if (writtenSize > (_maxSize - _nsInfoWriter.Position) && batch.CanBeSplit && i > 0)
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
                _nsInfoWriter.BaseStream.Seek(0, SeekOrigin.Begin);
                _nsInfoWriter.BaseStream.CopyTo(stream);
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

        public void VisitDeleteMany<TDocument>(BulkWriteDeleteManyModel<TDocument> deleteManyModel)
            where TDocument: class
            => WriteOperation("delete", deleteManyModel, (context, model) =>
            {
                var documentSerializer = _serializerRegistry.GetSerializer<TDocument>();
                WriteFilter(context, model.Filter, documentSerializer);
                WriteBoolean(context, "multi", true);
                WriteHint(context, model.Hint);
                WriteCollation(context, model.Collation);
            });

        public void VisitDeleteOne<TDocument>(BulkWriteDeleteOneModel<TDocument> deleteOneModel)
            where TDocument: class
            => WriteOperation("delete", deleteOneModel, (context, model) =>
            {
                var documentSerializer = _serializerRegistry.GetSerializer<TDocument>();
                WriteFilter(context, model.Filter, documentSerializer);
                WriteHint(context, model.Hint);
                WriteCollation(context, model.Collation);
            });

        public void VisitInsertOne<TDocument>(BulkWriteInsertOneModel<TDocument> insertOneModel)
            where TDocument: class
            => WriteOperation("insert", insertOneModel, (context, model) =>
            {
                var documentSerializer = _serializerRegistry.GetSerializer<TDocument>();
                documentSerializer.EnsureIdAssigned(null, model.Document);
                context.Writer.WriteName("document");
                documentSerializer.Serialize(_serializationContext, model.Document);
            });

        public void VisitReplaceOne<TDocument>(BulkWriteReplaceOneModel<TDocument> replaceOneModel)
            where TDocument: class
            => WriteOperation("update", replaceOneModel, (context, model) =>
            {
                var documentSerializer = _serializerRegistry.GetSerializer<TDocument>();
                WriteFilter(context, model.Filter, documentSerializer);
                WriteUpdate(context, model.Replacement, documentSerializer, UpdateType.Replacement);
                WriteBoolean(context, "upsert", model.IsUpsert);
                WriteHint(context, model.Hint);
                WriteCollation(context, model.Collation);
            });

        public void VisitUpdateMany<TDocument>(BulkWriteUpdateManyModel<TDocument> updateOneModel)
            where TDocument: class
            => WriteOperation("update", updateOneModel, (context, model) =>
            {
                var documentSerializer = _serializerRegistry.GetSerializer<TDocument>();
                WriteFilter(context, model.Filter, documentSerializer);
                WriteUpdate(context, model.Update, documentSerializer);
                WriteBoolean(context, "multi", true);
                WriteBoolean(context, "upsert", model.IsUpsert);
                WriteArrayFilters(context, model.ArrayFilters);
                WriteHint(context, model.Hint);
                WriteCollation(context, model.Collation);
            });

        public void VisitUpdateOne<TDocument>(BulkWriteUpdateOneModel<TDocument> updateManyModel)
            where TDocument: class
            => WriteOperation("update", updateManyModel, (context, model) =>
            {
                var documentSerializer = _serializerRegistry.GetSerializer<TDocument>();
                WriteFilter(context, model.Filter, documentSerializer);
                WriteUpdate(context, model.Update, documentSerializer);
                WriteBoolean(context, "upsert", model.IsUpsert);
                WriteArrayFilters(context, model.ArrayFilters);
                WriteHint(context, model.Hint);
                WriteCollation(context, model.Collation);
            });

        private int EnsureNsInfo(string nsName)
        {
            if (!_nsInfos.TryGetValue(nsName, out var index))
            {
                index = _nsInfos.Count;
                _nsInfos.Add(nsName, index);

                _nsInfoWriter.WriteStartDocument();
                _nsInfoWriter.WriteName("ns");
                _nsInfoWriter.WriteString(nsName);
                _nsInfoWriter.WriteEndDocument();
            }

            return index;
        }

        private void WriteArrayFilters(BsonSerializationContext serializationContext, IEnumerable<ArrayFilterDefinition> arrayFilters)
        {
            if (arrayFilters == null)
            {
                return;
            }

            serializationContext.Writer.WriteName("arrayFilters");
            serializationContext.Writer.WriteStartArray();
            foreach (var arrayFilter in arrayFilters)
            {
                var renderedArrayFilter = arrayFilter.Render(null, _serializerRegistry);
                BsonDocumentSerializer.Instance.Serialize(serializationContext, renderedArrayFilter);
            }
            serializationContext.Writer.WriteEndArray();
        }

        private void WriteBoolean(BsonSerializationContext serializationContext, string name, bool value)
        {
            var writer = serializationContext.Writer;
            writer.WriteName(name);
            writer.WriteBoolean(value);
        }

        private void WriteCollation(BsonSerializationContext serializationContext, Collation collation)
        {
            if (collation == null)
            {
                return;
            }

            serializationContext.Writer.WriteName("collation");
            var collationDocument = collation.ToBsonDocument();
            BsonDocumentSerializer.Instance.Serialize(serializationContext, collationDocument);
        }

        private void WriteFilter<TDocument>(BsonSerializationContext serializationContext, FilterDefinition<TDocument> filterDefinition, IBsonSerializer<TDocument> documentSerializer)
        {
            serializationContext.Writer.WriteName("filter");
            var renderArgs = _renderArgs.WithNewDocumentType(documentSerializer);
            var filterDocument = filterDefinition.Render(renderArgs);
            BsonDocumentSerializer.Instance.Serialize(serializationContext, filterDocument);
        }

        private void WriteHint(BsonSerializationContext serializationContext, BsonValue hint)
        {
            if (hint == null)
            {
                return;
            }

            serializationContext.Writer.WriteName("hint");
            BsonValueSerializer.Instance.Serialize(serializationContext, hint);
        }

        private void WriteOperation<TOperation>(string operationName, TOperation operation, Action<BsonSerializationContext, TOperation> operationBodyWriter)
            where TOperation : BulkWriteModel
        {
            var nsInfoIndex = EnsureNsInfo(operation.Namespace.FullName);

            var writer = _serializationContext.Writer;
            writer.WriteStartDocument();
            writer.WriteName(operationName);
            writer.WriteInt32(nsInfoIndex);
            operationBodyWriter(_serializationContext, operation);
            writer.WriteEndDocument();
        }



        private void WriteUpdate<TDocument>(BsonSerializationContext serializationContext, UpdateDefinition<TDocument> updateDefinition, IBsonSerializer<TDocument> documentSerializer)
        {
            var renderArgs = _renderArgs.WithNewDocumentType(documentSerializer);
            var filterDocument = updateDefinition.Render(renderArgs).AsBsonDocument;

            WriteUpdate(serializationContext, filterDocument, BsonDocumentSerializer.Instance, UpdateType.Update);
        }

        private void WriteUpdate<TDocument>(BsonSerializationContext serializationContext, TDocument updateDefinition, IBsonSerializer<TDocument> documentSerializer, UpdateType updateType)
        {
            serializationContext.Writer.WriteName("updateMods");
            serializationContext.Writer.PushElementNameValidator(ElementNameValidatorFactory.ForUpdateType(updateType));
            try
            {
                documentSerializer.Serialize(serializationContext, updateDefinition);
            }
            finally
            {
                serializationContext.Writer.PopElementNameValidator();
            }
        }
    }
}
