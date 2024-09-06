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
        // private fields
        private readonly object _document;
        private readonly IBsonSerializer _documentSerializer;

        // constructors
        public Type0CommandMessageSection(object document, IBsonSerializer documentSerializer)
        {
            Ensure.IsNotNull((object)document, nameof(document));
            _document = document;
            _documentSerializer = Ensure.IsNotNull(documentSerializer, nameof(documentSerializer));
        }

        // public properties
        public object Document => _document;
        public IBsonSerializer DocumentSerializer => _documentSerializer;
        public override PayloadType PayloadType => PayloadType.Type0;
    }

    internal sealed class Type0CommandMessageSection<TDocument> : Type0CommandMessageSection
    {
        // private fields
        private readonly TDocument _document;
        private readonly IBsonSerializer<TDocument> _documentSerializer;

        // constructors
        public Type0CommandMessageSection(TDocument document, IBsonSerializer<TDocument> documentSerializer)
            : base(document, documentSerializer)
        {
            Ensure.IsNotNull((object)document, nameof(document));
            _document = document;
            _documentSerializer = Ensure.IsNotNull(documentSerializer, nameof(documentSerializer));
        }

        // public properties
        public new TDocument Document => _document;
        public new IBsonSerializer<TDocument> DocumentSerializer => _documentSerializer;
    }

    internal abstract class Type1CommandMessageSection : CommandMessageSection
    {
        // private fields
        private readonly IBatchableSource<object> _documents;
        private readonly IBsonSerializer _documentSerializer;
        private readonly IElementNameValidator _elementNameValidator;
        private readonly string _identifier;
        private readonly int? _maxBatchCount;
        private readonly int? _maxDocumentSize;

        // constructors
        public Type1CommandMessageSection(
            string identifier,
            IBatchableSource<object> documents,
            IBsonSerializer documentSerializer,
            IElementNameValidator elementNameValidator,
            int? maxBatchCount,
            int? maxDocumentSize)
        {
            _identifier = Ensure.IsNotNull(identifier, nameof(identifier));
            _documents = Ensure.IsNotNull(documents, nameof(documents));
            _documentSerializer = Ensure.IsNotNull(documentSerializer, nameof(documentSerializer));
            _elementNameValidator = Ensure.IsNotNull(elementNameValidator, nameof(elementNameValidator));
            _maxBatchCount = Ensure.IsNullOrGreaterThanZero(maxBatchCount, nameof(maxBatchCount));
            _maxDocumentSize = Ensure.IsNullOrGreaterThanZero(maxDocumentSize, nameof(maxDocumentSize));
        }

        // public properties
        public IBatchableSource<object> Documents => _documents;
        public IBsonSerializer DocumentSerializer => _documentSerializer;
        public abstract Type DocumentType { get; }
        public IElementNameValidator ElementNameValidator => _elementNameValidator;
        public string Identifier => _identifier;
        public int? MaxBatchCount => _maxBatchCount;
        public int? MaxDocumentSize => _maxDocumentSize;
        public override PayloadType PayloadType => PayloadType.Type1;
    }

    internal sealed class Type1CommandMessageSection<TDocument> : Type1CommandMessageSection where TDocument : class
    {
        // private fields
        private readonly IBatchableSource<TDocument> _documents;
        private readonly IBsonSerializer<TDocument> _documentSerializer;

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
            _documents = Ensure.IsNotNull(documents, nameof(documents));
            _documentSerializer = Ensure.IsNotNull(documentSerializer, nameof(documentSerializer));
        }

        // public properties
        public new IBatchableSource<TDocument> Documents => _documents;
        public new IBsonSerializer<TDocument> DocumentSerializer => _documentSerializer;
        public override Type DocumentType => typeof(TDocument);
    }
}
