/* Copyright 2018-present MongoDB Inc.
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

using System.Collections.Generic;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;
using Xunit;

namespace MongoDB.Driver.Core.WireProtocol.Messages
{
    public class Type0CommandMessageSectionTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var document = new BsonDocument();
            var documentSerializer = new BsonDocumentSerializer();

            var result = (Type0CommandMessageSection)new Type0CommandMessageSection<BsonDocument>(document, documentSerializer);

            result.Document.Should().BeSameAs(document);
            result.DocumentSerializer.Should().BeSameAs(documentSerializer);
            result.PayloadType.Should().Be(PayloadType.Type0);
        }

        [Fact]
        public void Document_should_return_expected_result()
        {
            var document = new BsonDocument();
            var subject = CreateSubject(document: document);

            var result = subject.Document;

            result.Should().BeSameAs(document);
        }

        [Fact]
        public void DocumentSerializer_should_return_expected_result()
        {
            var documentSerializer = new BsonDocumentSerializer();
            var subject = CreateSubject(documentSerializer: documentSerializer);

            var result = subject.DocumentSerializer;

            result.Should().BeSameAs(documentSerializer);
        }

        [Fact]
        public void PayloadType_should_return_expected_result()
        {
            var subject = CreateSubject();

            var result = subject.PayloadType;

            result.Should().Be(PayloadType.Type0);
        }

        // private methods
        private Type0CommandMessageSection CreateSubject(
            BsonDocument document = null,
            IBsonSerializer<BsonDocument> documentSerializer = null)
        {
            document = document ?? new BsonDocument();
            documentSerializer = documentSerializer ?? new BsonDocumentSerializer();
            return new Type0CommandMessageSection<BsonDocument>(document, documentSerializer);
        }
    }

    public class Type0CommandMessageSectionTDocumentTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var document = new BsonDocument();
            var documentSerializer = new BsonDocumentSerializer();

            var result = new Type0CommandMessageSection<BsonDocument>(document, documentSerializer);

            result.Document.Should().BeSameAs(document);
            result.DocumentSerializer.Should().BeSameAs(documentSerializer);
            result.PayloadType.Should().Be(PayloadType.Type0);
        }

        [Fact]
        public void Document_should_return_expected_result()
        {
            var document = new BsonDocument();
            var subject = CreateSubject(document: document);

            var result = subject.Document;

            result.Should().BeSameAs(document);
        }

        [Fact]
        public void DocumentSerializer_should_return_expected_result()
        {
            var documentSerializer = new BsonDocumentSerializer();
            var subject = CreateSubject(documentSerializer: documentSerializer);

            var result = subject.DocumentSerializer;

            result.Should().BeSameAs(documentSerializer);
        }

        // private methods
        private Type0CommandMessageSection<BsonDocument> CreateSubject(
            BsonDocument document = null,
            IBsonSerializer<BsonDocument> documentSerializer = null)
        {
            document = document ?? new BsonDocument();
            documentSerializer = documentSerializer ?? BsonDocumentSerializer.Instance;
            return new Type0CommandMessageSection<BsonDocument>(document, documentSerializer);
        }
    }

    public class Type1CommandMessageSectionTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var identifier = "xyz";
            var documents = new BatchableSource<BsonDocument>(new List<BsonDocument>(), canBeSplit: false);
            var documentSerializer = new BsonDocumentSerializer();

            var result = (Type1CommandMessageSection)new Type1CommandMessageSection<BsonDocument>(identifier, documents, documentSerializer);

            result.Documents.Should().BeSameAs(documents);
            result.DocumentSerializer.Should().BeSameAs(documentSerializer);
            result.Identifier.Should().BeSameAs(identifier);
            result.PayloadType.Should().Be(PayloadType.Type1);
        }

        [Fact]
        public void Documents_should_return_expected_result()
        {
            var documents = new BatchableSource<BsonDocument>(new List<BsonDocument>(), canBeSplit: false);
            var subject = CreateSubject(documents: documents);

            var result = subject.Documents;

            result.Should().BeSameAs(documents);
        }

        [Fact]
        public void DocumentSerializer_should_return_expected_result()
        {
            var documentSerializer = new BsonDocumentSerializer();
            var subject = CreateSubject(documentSerializer: documentSerializer);

            var result = subject.DocumentSerializer;

            result.Should().BeSameAs(documentSerializer);
        }

        [Fact]
        public void Identifier_should_return_expected_result()
        {
            var identifier = "xyz";
            var subject = CreateSubject(identifier: identifier);

            var result = subject.Identifier;

            result.Should().BeSameAs(identifier);
        }

        [Fact]
        public void PayloadType_should_return_expected_result()
        {
            var subject = CreateSubject();

            var result = subject.PayloadType;

            result.Should().Be(PayloadType.Type1);
        }

        // private methods
        private Type1CommandMessageSection CreateSubject(
            string identifier = null,
            IBatchableSource<BsonDocument> documents = null,
            IBsonSerializer<BsonDocument> documentSerializer = null)
        {
            identifier = identifier ?? "identifier";
            documents = documents ?? new BatchableSource<BsonDocument>(new List<BsonDocument>(), canBeSplit: false);
            documentSerializer = documentSerializer ?? new BsonDocumentSerializer();
            return new Type1CommandMessageSection<BsonDocument>(identifier, documents, documentSerializer);
        }
    }

    public class Type1CommandMessageSectionTDocumentTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var identifier = "xyz";
            var documents = new BatchableSource<BsonDocument>(new List<BsonDocument>(), canBeSplit: false);
            var documentSerializer = new BsonDocumentSerializer();

            var result = new Type1CommandMessageSection<BsonDocument>(identifier, documents, documentSerializer);

            result.Documents.Should().BeSameAs(documents);
            result.DocumentSerializer.Should().BeSameAs(documentSerializer);
            result.Identifier.Should().BeSameAs(identifier);
            result.PayloadType.Should().Be(PayloadType.Type1);
        }

        [Fact]
        public void Documents_should_return_expected_result()
        {
            var documents = new BatchableSource<BsonDocument>(new List<BsonDocument>(), canBeSplit: false);
            var subject = CreateSubject(documents: documents);

            var result = subject.Documents;

            result.Should().BeSameAs(documents);
        }

        [Fact]
        public void DocumentSerializer_should_return_expected_result()
        {
            var documentSerializer = new BsonDocumentSerializer();
            var subject = CreateSubject(documentSerializer: documentSerializer);

            var result = subject.DocumentSerializer;

            result.Should().BeSameAs(documentSerializer);
        }

        [Fact]
        public void Identifier_should_return_expected_result()
        {
            var identifier = "xyz";
            var subject = CreateSubject(identifier: identifier);

            var result = subject.Identifier;

            result.Should().BeSameAs(identifier);
        }

        // private methods
        private Type1CommandMessageSection<BsonDocument> CreateSubject(
            string identifier = null,
            IBatchableSource<BsonDocument> documents = null,
            IBsonSerializer<BsonDocument> documentSerializer = null)
        {
            identifier = identifier ?? "identifier";
            documents = documents ?? new BatchableSource<BsonDocument>(new List<BsonDocument>(), canBeSplit: false);
            documentSerializer = documentSerializer ?? new BsonDocumentSerializer();
            return new Type1CommandMessageSection<BsonDocument>(identifier, documents, documentSerializer);
        }
    }
}
