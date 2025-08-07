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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Jira;

public class CSharp5697Tests : IntegrationTest<CSharp5697Tests.ClassFixture>
{
    public CSharp5697Tests(ClassFixture fixture)
        : base(fixture, server => server.Supports(Feature.ClientBulkWrite))
    {
    }

    [Theory]
    [ParameterAttributeData]
    public async Task ClientBulkWrite_supports_complex_id([Values(true, false)] bool async)
    {
        var id = async ? "1" : "2";
        var options = new ClientBulkWriteOptions { VerboseResult = true };
        BulkWriteModel[] models =
        [
            new BulkWriteInsertOneModel<Document>(
                Fixture.Collection.CollectionNamespace,
                new Document(new DocumentId(id)))
        ];

        var result = async ?
            await Fixture.Client.BulkWriteAsync(models, options) :
            Fixture.Client.BulkWrite(models, options);

        result.InsertResults[0].DocumentId.Should().BeOfType<DocumentId>()
            .Subject.Key.Should().Be(id);
    }

    public class Document
    {
        public Document(DocumentId id)
        {
            Id = id;
        }

        public DocumentId Id { get; }
    }

    public class DocumentId
    {
        public DocumentId(string key)
        {
            Key = key;
        }

        public string Key { get; }
    }

    public sealed class DocumentIdSerializer : IBsonSerializer<DocumentId>
    {
        public Type ValueType => typeof(DocumentId);

        public DocumentId Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
            => new DocumentId(context.Reader.ReadString());

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DocumentId value)
            => context.Writer.WriteString(value.Key);

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            var id = (DocumentId)value;
            Serialize(context, args, id);
        }

        object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
            => Deserialize(context, args);
    }

    public class ClassFixture : MongoCollectionFixture<Document>
    {
        public ClassFixture()
        {
            BsonSerializer.RegisterSerializer( new DocumentIdSerializer());
        }

        protected override IEnumerable<Document> InitialData => null;
    }
}

