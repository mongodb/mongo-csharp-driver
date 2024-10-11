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
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.WireProtocol.Messages
{
    internal enum PayloadType
    {
        Type0 = 0,
        Type1 = 1
    }

    internal abstract class CommandMessageSection
    {
        public abstract PayloadType PayloadType { get; }
    }

    internal abstract class Type0CommandMessageSection : CommandMessageSection
    {
        // constructors
        public Type0CommandMessageSection(object document, IBsonSerializer documentSerializer)
        {
            Ensure.IsNotNull((object)document, nameof(document));
            Document = document;
            DocumentSerializer = Ensure.IsNotNull(documentSerializer, nameof(documentSerializer));
        }

        // public properties
        public object Document { get; }
        public IBsonSerializer DocumentSerializer { get; }
        public override PayloadType PayloadType => PayloadType.Type0;
    }

    internal sealed class Type0CommandMessageSection<TDocument> : Type0CommandMessageSection
    {
        // constructors
        public Type0CommandMessageSection(TDocument document, IBsonSerializer<TDocument> documentSerializer)
            : base(document, documentSerializer)
        {
            Ensure.IsNotNull((object)document, nameof(document));
            Document = document;
            DocumentSerializer = Ensure.IsNotNull(documentSerializer, nameof(documentSerializer));
        }

        // public properties
        public new TDocument Document { get; }
        public new IBsonSerializer<TDocument> DocumentSerializer { get; }
    }

    internal abstract class BatchableCommandMessageSection : CommandMessageSection
    {
        protected BatchableCommandMessageSection(
            IBatchableSource<object> documents,
            int? maxBatchCount,
            int? maxDocumentSize)
        {
            Documents = Ensure.IsNotNull(documents, nameof(documents));
            MaxBatchCount = Ensure.IsNullOrGreaterThanZero(maxBatchCount, nameof(maxBatchCount));
            MaxDocumentSize = Ensure.IsNullOrGreaterThanZero(maxDocumentSize, nameof(maxDocumentSize));
        }

        public IBatchableSource<object> Documents { get; }
        public int? MaxBatchCount { get; }
        public int? MaxDocumentSize { get; }
        public override PayloadType PayloadType => PayloadType.Type1;
    }

    internal abstract class Type1CommandMessageSection : BatchableCommandMessageSection
    {
        // constructors
        public Type1CommandMessageSection(
            string identifier,
            IBatchableSource<object> documents,
            IBsonSerializer documentSerializer,
            IElementNameValidator elementNameValidator,
            int? maxBatchCount,
            int? maxDocumentSize)
        : base(documents, maxBatchCount, maxDocumentSize)
        {
            Identifier = Ensure.IsNotNull(identifier, nameof(identifier));
            DocumentSerializer = Ensure.IsNotNull(documentSerializer, nameof(documentSerializer));
            ElementNameValidator = Ensure.IsNotNull(elementNameValidator, nameof(elementNameValidator));
        }

        // public properties
        public IBsonSerializer DocumentSerializer { get; }
        public abstract Type DocumentType { get; }
        public IElementNameValidator ElementNameValidator { get; }
        public string Identifier { get; }
    }

    internal sealed class Type1CommandMessageSection<TDocument> : Type1CommandMessageSection where TDocument : class
    {
        // constructors
        public Type1CommandMessageSection(
            string identifier,
            IBatchableSource<TDocument> documents,
            IBsonSerializer<TDocument> documentSerializer,
            IElementNameValidator elementNameValidator,
            int? maxBatchCount,
            int? maxDocumentSize)
            : base(identifier, documents, documentSerializer, elementNameValidator, maxBatchCount, maxDocumentSize)
        {
            Documents = Ensure.IsNotNull(documents, nameof(documents));
            DocumentSerializer = Ensure.IsNotNull(documentSerializer, nameof(documentSerializer));
        }

        // public properties
        public new IBatchableSource<TDocument> Documents { get; }
        public new IBsonSerializer<TDocument> DocumentSerializer { get; }
        public override Type DocumentType => typeof(TDocument);
    }

    internal sealed class ClientBulkWriteOpsCommandMessageSection : BatchableCommandMessageSection
    {
        public ClientBulkWriteOpsCommandMessageSection(
            IBatchableSource<BulkWriteModel> operations,
            Dictionary<int, BsonValue> idsMap,
            int? maxBatchCount,
            int? maxDocumentSize,
            RenderArgs<BsonDocument> renderArgs)
        : base(operations, maxBatchCount, maxDocumentSize)
            {
                Documents = operations;
                IdsMap = idsMap;
                RenderArgs = renderArgs;
            }

        public Dictionary<int, BsonValue> IdsMap { get; }
        public new IBatchableSource<BulkWriteModel> Documents { get; }
        public RenderArgs<BsonDocument> RenderArgs { get; }
    }
}
