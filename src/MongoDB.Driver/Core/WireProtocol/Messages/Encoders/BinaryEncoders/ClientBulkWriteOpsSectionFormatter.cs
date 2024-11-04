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
    internal sealed class ClientBulkWriteOpsSectionFormatter : ICommandMessageSectionFormatter<ClientBulkWriteOpsCommandMessageSection>, IBulkWriteModelRenderer, IDisposable
    {
        private readonly long? _maxSize;
        private readonly Dictionary<string, int> _nsInfos;
        private MemoryStream _nsInfoMemoryStream;
        private BsonBinaryWriter _nsInfoWriter;
        private IBsonSerializerRegistry _serializerRegistry;
        private Dictionary<int, BsonValue> _idsMap;
        private int _currentIndex;

        public ClientBulkWriteOpsSectionFormatter(long? maxSize)
        {
            _maxSize = (maxSize ?? long.MaxValue) - 1000; // according to spec we should leave some extra space for further overhead
            if (_maxSize <= 0)
            {
                throw new InvalidOperationException("Section's size limit is too small.");
            }

            _nsInfos = new Dictionary<string, int>();
            _nsInfoMemoryStream = new MemoryStream();
            _nsInfoWriter = new BsonBinaryWriter(_nsInfoMemoryStream);
        }

        public void Dispose()
        {
            _nsInfoMemoryStream?.Dispose();
            _nsInfoMemoryStream = null;
            _nsInfoWriter?.Dispose();
            _nsInfoWriter = null;
        }

        public void FormatSection(ClientBulkWriteOpsCommandMessageSection section, IBsonWriter writer)
        {
            if (writer is not BsonBinaryWriter binaryWriter)
            {
                throw new ArgumentException("Writer must be an instance of BsonBinaryWriter.");
            }

            _serializerRegistry = BsonSerializer.SerializerRegistry;
            var serializationContext = BsonSerializationContext.CreateRoot(binaryWriter);
            _idsMap = section.IdsMap;
            var stream = binaryWriter.BsonStream;
            var startPosition = stream.Position;

            stream.WriteInt32(0); // size
            stream.WriteCString("ops");

            var batch = section.Documents;
            var maxDocumentSize = section.MaxDocumentSize ?? binaryWriter.Settings.MaxDocumentSize;
            binaryWriter.PushSettings(s => ((BsonBinaryWriterSettings)s).MaxDocumentSize = maxDocumentSize);
            binaryWriter.PushElementNameValidator(NoOpElementNameValidator.Instance);
            _nsInfoWriter.PushSettings(s => ((BsonBinaryWriterSettings)s).MaxDocumentSize = maxDocumentSize);
            try
            {
                var maxBatchCount = Math.Min(batch.Count, section.MaxBatchCount ?? int.MaxValue);
                var processedCount = maxBatchCount;
                for (var i = 0; i < maxBatchCount; i++)
                {
                    var documentStartPosition = stream.Position;
                    var model = batch.Items[batch.Offset + i];
                    _currentIndex = batch.Offset + i;

                    model.Render(section.RenderArgs, serializationContext, this);

                    var writtenSize = stream.Position - startPosition;
                    if (writtenSize > (_maxSize - _nsInfoWriter.Position))
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

        public void RenderDeleteMany<TDocument>(RenderArgs<BsonDocument> renderArgs, BsonSerializationContext serializationContext, BulkWriteDeleteManyModel<TDocument> model)
        {
            WriteStartModel(serializationContext, "delete", model);
            var documentSerializer = _serializerRegistry.GetSerializer<TDocument>();
            WriteFilter(serializationContext, renderArgs, model.Filter, documentSerializer);
            WriteBoolean(serializationContext, "multi", true);
            WriteHint(serializationContext, model.Hint);
            WriteCollation(serializationContext, model.Collation);
            WriteEndModel(serializationContext);
        }

        public void RenderDeleteOne<TDocument>(RenderArgs<BsonDocument> renderArgs, BsonSerializationContext serializationContext, BulkWriteDeleteOneModel<TDocument> model)
        {
            WriteStartModel(serializationContext, "delete", model);
            var documentSerializer = _serializerRegistry.GetSerializer<TDocument>();
            WriteFilter(serializationContext, renderArgs, model.Filter, documentSerializer);
            WriteBoolean(serializationContext, "multi", false);
            WriteHint(serializationContext, model.Hint);
            WriteCollation(serializationContext, model.Collation);
            WriteEndModel(serializationContext);
        }

        public void RenderInsertOne<TDocument>(RenderArgs<BsonDocument> renderArgs, BsonSerializationContext serializationContext, BulkWriteInsertOneModel<TDocument> model)
        {
            WriteStartModel(serializationContext, "insert", model);
            var documentSerializer = _serializerRegistry.GetSerializer<TDocument>();
            var documentId = documentSerializer.SetDocumentIdIfMissing(null, model.Document);
            _idsMap[_currentIndex] = BsonValue.Create(documentId);
            serializationContext.Writer.WriteName("document");
            documentSerializer.Serialize(serializationContext, model.Document);
            WriteEndModel(serializationContext);
        }

        public void RenderReplaceOne<TDocument>(RenderArgs<BsonDocument> renderArgs, BsonSerializationContext serializationContext, BulkWriteReplaceOneModel<TDocument> model)
        {
            WriteStartModel(serializationContext, "update", model);
            var documentSerializer = _serializerRegistry.GetSerializer<TDocument>();
            WriteFilter(serializationContext, renderArgs, model.Filter, documentSerializer);
            WriteUpdate(serializationContext, model.Replacement, documentSerializer, UpdateType.Replacement);
            if (model.IsUpsert)
            {
                WriteBoolean(serializationContext, "upsert", true);
            }

            WriteBoolean(serializationContext, "multi", false);
            WriteHint(serializationContext, model.Hint);
            WriteCollation(serializationContext, model.Collation);
            WriteSort(serializationContext, renderArgs, model.Sort, documentSerializer);
            WriteEndModel(serializationContext);
        }

        public void RenderUpdateMany<TDocument>(RenderArgs<BsonDocument> renderArgs, BsonSerializationContext serializationContext, BulkWriteUpdateManyModel<TDocument> model)
        {
            WriteStartModel(serializationContext, "update", model);
            var documentSerializer = _serializerRegistry.GetSerializer<TDocument>();
            WriteFilter(serializationContext, renderArgs, model.Filter, documentSerializer);
            WriteUpdate(serializationContext, renderArgs, model.Update, documentSerializer);
            if (model.IsUpsert)
            {
                WriteBoolean(serializationContext, "upsert", true);
            }

            WriteBoolean(serializationContext, "multi", true);
            WriteArrayFilters(serializationContext, model.ArrayFilters);
            WriteHint(serializationContext, model.Hint);
            WriteCollation(serializationContext, model.Collation);
            WriteEndModel(serializationContext);
        }

        public void RenderUpdateOne<TDocument>(RenderArgs<BsonDocument> renderArgs, BsonSerializationContext serializationContext, BulkWriteUpdateOneModel<TDocument> model)
        {
            WriteStartModel(serializationContext, "update", model);
            var documentSerializer = _serializerRegistry.GetSerializer<TDocument>();
            WriteFilter(serializationContext, renderArgs, model.Filter, documentSerializer);
            WriteUpdate(serializationContext, renderArgs, model.Update, documentSerializer);
            if (model.IsUpsert)
            {
                WriteBoolean(serializationContext, "upsert", true);
            }

            WriteBoolean(serializationContext, "multi", false);
            WriteArrayFilters(serializationContext, model.ArrayFilters);
            WriteHint(serializationContext, model.Hint);
            WriteCollation(serializationContext, model.Collation);
            WriteSort(serializationContext, renderArgs, model.Sort, documentSerializer);
            WriteEndModel(serializationContext);
        }

        private int GetNsInfoIndex(string nsName)
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

        private void WriteFilter<TDocument>(BsonSerializationContext serializationContext, RenderArgs<BsonDocument> renderArgs, FilterDefinition<TDocument> filterDefinition, IBsonSerializer<TDocument> documentSerializer)
        {
            serializationContext.Writer.WriteName("filter");
            var typedRenderArgs = renderArgs.WithNewDocumentType(documentSerializer);
            var filterDocument = filterDefinition.Render(typedRenderArgs);
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

        private void WriteSort<TDocument>(BsonSerializationContext serializationContext, RenderArgs<BsonDocument> renderArgs, SortDefinition<TDocument> sortDefinition, IBsonSerializer<TDocument> documentSerializer)
        {
            if (sortDefinition == null)
            {
                return;
            }

            serializationContext.Writer.WriteName("sort");
            var typedRenderArgs = renderArgs.WithNewDocumentType(documentSerializer);
            var sortDocument = sortDefinition.Render(typedRenderArgs);
            BsonDocumentSerializer.Instance.Serialize(serializationContext, sortDocument);
        }

        private void WriteStartModel(BsonSerializationContext serializationContext, string operationName, BulkWriteModel model)
        {
            var nsInfoIndex = GetNsInfoIndex(model.Namespace.FullName);

            var writer = serializationContext.Writer;
            writer.WriteStartDocument();
            writer.WriteName(operationName);
            writer.WriteInt32(nsInfoIndex);
        }

        private void WriteEndModel(BsonSerializationContext serializationContext)
        {
            serializationContext.Writer.WriteEndDocument();
        }

        private void WriteUpdate<TDocument>(BsonSerializationContext serializationContext, RenderArgs<BsonDocument> renderArgs, UpdateDefinition<TDocument> updateDefinition, IBsonSerializer<TDocument> documentSerializer)
        {
            var typedRenderArgs = renderArgs.WithNewDocumentType(documentSerializer);
            var filterDocument = updateDefinition.Render(typedRenderArgs);

            WriteUpdate(serializationContext, filterDocument, BsonValueSerializer.Instance, UpdateType.Update);
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
