/* Copyright 2010-2014 MongoDB Inc.
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
using MongoDB.Bson.Serialization.IdGenerators;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for BsonDocuments.
    /// </summary>
    public class BsonDocumentSerializer : BsonValueSerializerBase<BsonDocument>, IBsonDocumentSerializer, IBsonIdProvider
    {
        // private static fields
        private static BsonDocumentSerializer __instance = new BsonDocumentSerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the BsonDocumentSerializer class.
        /// </summary>
        public BsonDocumentSerializer()
            : base(BsonType.Document)
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the BsonDocumentSerializer class.
        /// </summary>
        public static BsonDocumentSerializer Instance
        {
            get { return __instance; }
        }

        // public methods
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>An object.</returns>
        protected override BsonDocument DeserializeValue(BsonDeserializationContext context)
        {
            var bsonReader = context.Reader;

            bsonReader.ReadStartDocument();
            var document = new BsonDocument(allowDuplicateNames: context.AllowDuplicateElementNames);
            while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
            {
                var name = bsonReader.ReadName();
                var value = context.DeserializeWithChildContext(BsonValueSerializer.Instance);
                document.Add(name, value);
            }
            bsonReader.ReadEndDocument();

            return document;
        }

        /// <summary>
        /// Gets the document Id.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="id">The Id.</param>
        /// <param name="idNominalType">The nominal type of the Id.</param>
        /// <param name="idGenerator">The IdGenerator for the Id type.</param>
        /// <returns>True if the document has an Id.</returns>
        public bool GetDocumentId(
            object document,
            out object id,
            out Type idNominalType,
            out IIdGenerator idGenerator)
        {
            var bsonDocument = (BsonDocument)document;

            BsonElement idElement;
            if (bsonDocument.TryGetElement("_id", out idElement))
            {
                id = idElement.Value;
                idGenerator = BsonSerializer.LookupIdGenerator(id.GetType());

                if (idGenerator == null)
                {
                    var idBinaryData = id as BsonBinaryData;
                    if (idBinaryData != null && (idBinaryData.SubType == BsonBinarySubType.UuidLegacy || idBinaryData.SubType == BsonBinarySubType.UuidStandard))
                    {
                        idGenerator = BsonBinaryDataGuidGenerator.GetInstance(idBinaryData.GuidRepresentation);
                    }
                }
            }
            else
            {
                id = null;
                idGenerator = BsonObjectIdGenerator.Instance;
            }

            idNominalType = typeof(BsonValue);
            return true;
        }

        /// <summary>
        /// Gets the serialization info for a member.
        /// </summary>
        /// <param name="memberName">The member name.</param>
        /// <returns>
        /// The serialization info for the member.
        /// </returns>
        public BsonSerializationInfo GetMemberSerializationInfo(string memberName)
        {
            return new BsonSerializationInfo(
                memberName,
                BsonValueSerializer.Instance,
                typeof(BsonValue));
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The object.</param>
        protected override void SerializeValue(BsonSerializationContext context, BsonDocument value)
        {
            var bsonWriter = context.Writer;
            bsonWriter.WriteStartDocument();

            BsonElement idElement = null;
            if (context.SerializeIdFirst && value.TryGetElement("_id", out idElement))
            {
                bsonWriter.WriteName(idElement.Name);
                context.SerializeWithChildContext(BsonValueSerializer.Instance, idElement.Value);
            }

            foreach (var element in value)
            {
                // if serializeIdFirst is false then idElement will be null and no elements will be skipped
                if (!object.ReferenceEquals(element, idElement))
                {
                    bsonWriter.WriteName(element.Name);
                    context.SerializeWithChildContext(BsonValueSerializer.Instance, element.Value);
                }
            }

            bsonWriter.WriteEndDocument();
        }

        /// <summary>
        /// Sets the document Id.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="id">The Id.</param>
        public void SetDocumentId(object document, object id)
        {
            if (document == null)
            {
                throw new ArgumentNullException("document");
            }
            if (id == null)
            {
                throw new ArgumentNullException("id");
            }

            var bsonDocument = (BsonDocument)document;
            var idBsonValue = id as BsonValue;
            if (idBsonValue == null)
            {
                idBsonValue = BsonValue.Create(id); // be helpful and provide automatic conversion to BsonValue if necessary
            }

            BsonElement idElement;
            if (bsonDocument.TryGetElement("_id", out idElement))
            {
                idElement.Value = idBsonValue;
            }
            else
            {
                bsonDocument.InsertAt(0, new BsonElement("_id", idBsonValue));
            }
        }
    }
}
