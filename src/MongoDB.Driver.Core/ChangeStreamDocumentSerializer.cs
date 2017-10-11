/* Copyright 2017 MongoDB Inc.
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

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;
using System;

namespace MongoDB.Driver
{

    /// <summary>
    /// A serializer for ChangeStreamDocument instances.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public class ChangeStreamDocumentSerializer<TDocument> : SealedClassSerializerBase<ChangeStreamDocument<TDocument>>
    {
        #region static
        // private static fields
        private static readonly IBsonSerializer<ChangeStreamOperationType> __operationTypeSerializer = new ChangeStreamOperationTypeSerializer();
        private readonly ChangeStreamUpdateDescriptionSerializer __updateDescriptionSerializer = new ChangeStreamUpdateDescriptionSerializer();
        #endregion

        // private fields
        private readonly IBsonSerializer<TDocument> _documentSerializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeStreamDocumentSerializer{TDocument}"/> class.
        /// </summary>
        /// <param name="documentSerializer">The document serializer.</param>
        public ChangeStreamDocumentSerializer(
            IBsonSerializer<TDocument> documentSerializer)
        {
            _documentSerializer = Ensure.IsNotNull(documentSerializer, nameof(documentSerializer));
        }

        // public methods
        /// <inheritdoc />
        protected override ChangeStreamDocument<TDocument> DeserializeValue(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;

            CollectionNamespace collectionNamespace = null;
            BsonDocument documentKey = null;
            TDocument fullDocument = default(TDocument);
            BsonDocument resumeToken = null;
            ChangeStreamOperationType? operationType = null;
            ChangeStreamUpdateDescription updateDescription = null;

            reader.ReadStartDocument();
            while (reader.ReadBsonType() != 0)
            {
                var fieldName = reader.ReadName();
                switch (fieldName)
                {
                    case "_id":
                        resumeToken = BsonDocumentSerializer.Instance.Deserialize(context);
                        break;

                    case "ns":
                        collectionNamespace = DeserializeCollectionNamespace(reader);
                        break;

                    case "documentKey":
                        documentKey = BsonDocumentSerializer.Instance.Deserialize(context);
                        break;

                    case "fullDocument":
                        if (reader.CurrentBsonType == BsonType.Null)
                        {
                            reader.ReadNull();
                            fullDocument = default(TDocument);
                        }
                        else
                        {
                            fullDocument = _documentSerializer.Deserialize(context);
                        }
                        break;

                    case "operationType":
                        operationType = __operationTypeSerializer.Deserialize(context);
                        break;

                    case "updateDescription":
                        updateDescription = __updateDescriptionSerializer.Deserialize(context);
                        break;

                    default:
                        throw new FormatException($"Invalid field name: \"{fieldName}\".");
                }
            }
            reader.ReadEndDocument();

            return new ChangeStreamDocument<TDocument>(
                resumeToken,
                operationType.Value,
                collectionNamespace,
                documentKey,
                updateDescription,
                fullDocument);
        }

        /// <inheritdoc />
        protected override void SerializeValue(BsonSerializationContext context, BsonSerializationArgs args, ChangeStreamDocument<TDocument> value)
        {
            var writer = context.Writer;
            writer.WriteStartDocument();
            writer.WriteName("_id");
            BsonDocumentSerializer.Instance.Serialize(context, value.ResumeToken);
            writer.WriteName("operationType");
            __operationTypeSerializer.Serialize(context, value.OperationType);
            if (value.CollectionNamespace != null)
            {
                writer.WriteName("ns");
                SerializeCollectionNamespace(writer, value.CollectionNamespace);
            }
            if (value.DocumentKey != null)
            {
                writer.WriteName("documentKey");
                BsonDocumentSerializer.Instance.Serialize(context, value.DocumentKey);
            }
            if (value.UpdateDescription != null)
            {
                writer.WriteName("updateDescription");
                __updateDescriptionSerializer.Serialize(context, value.UpdateDescription);
            }
            if (value.FullDocument != null)
            {
                writer.WriteName("fullDocument");
                _documentSerializer.Serialize(context, value.FullDocument);
            }
            writer.WriteEndDocument();
        }

        // private methods
        private CollectionNamespace DeserializeCollectionNamespace(IBsonReader reader)
        {
            string collectionName = null;
            string databaseName = null;

            reader.ReadStartDocument();
            while (reader.ReadBsonType() != 0)
            {
                var fieldName = reader.ReadName();
                switch (fieldName)
                {
                    case "db":
                        databaseName = reader.ReadString();
                        break;

                    case "coll":
                        collectionName = reader.ReadString();
                        break;

                    default:
                        throw new FormatException($"Invalid field name: \"{fieldName}\".");
                }
            }
            reader.ReadEndDocument();

            var databaseNamespace = new DatabaseNamespace(databaseName);
            return new CollectionNamespace(databaseNamespace, collectionName);
        }

        private void SerializeCollectionNamespace(IBsonWriter writer, CollectionNamespace value)
        {
            writer.WriteStartDocument();
            writer.WriteName("db");
            writer.WriteString(value.DatabaseNamespace.DatabaseName);
            writer.WriteName("coll");
            writer.WriteString(value.CollectionName);
            writer.WriteEndDocument();
        }
    }
}
