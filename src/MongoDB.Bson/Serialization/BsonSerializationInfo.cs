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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.IO;

namespace MongoDB.Bson.Serialization
{
    /// <summary>
    /// Represents the information needed to serialize a member.
    /// </summary>
    public class BsonSerializationInfo
    {
        #region static
        /// <summary>
        /// Creates a new instance of the BsonSerializationinfo class with an element path instead of an element name.
        /// </summary>
        /// <param name="elementPath">The element path.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="nominalType">The nominal type.</param>
        /// <returns>A BsonSerializationInfo.</returns>
        public static BsonSerializationInfo CreateWithPath(IEnumerable<string> elementPath, IBsonSerializer serializer, Type nominalType)
        {
            return new BsonSerializationInfo(elementPath.ToList(), serializer, nominalType);
        }
        #endregion

        // private fields
        // note: while _elementName could have been modeled with an _elementPath of length 1, treating this is a special case avoids some allocations
        private readonly string _elementName;
        private readonly IReadOnlyList<string> _elementPath;
        private readonly IBsonSerializer _serializer;
        private readonly Type _nominalType;

        // constructors
        /// <summary>
        /// Initializes a new instance of the BsonSerializationInfo class.
        /// </summary>
        /// <param name="elementName">The element name.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="nominalType">The nominal type.</param>
        public BsonSerializationInfo(string elementName, IBsonSerializer serializer, Type nominalType)
        {
            _elementName = elementName;
            _serializer = serializer;
            _nominalType = nominalType;
        }

        private BsonSerializationInfo(IReadOnlyList<string> elementPath, IBsonSerializer serializer, Type nominalType)
        {
            _elementPath = elementPath;
            _serializer = serializer;
            _nominalType = nominalType;
        }

        // public properties
        /// <summary>
        /// Gets the element name.
        /// </summary>
        public string ElementName
        {
            get
            {
                if (_elementPath != null)
                {
                    throw new InvalidOperationException("When ElementPath is not null you must use it instead.");
                }
                return _elementName;
            }
        }

        /// <summary>
        /// Gets element path.
        /// </summary>
        public IReadOnlyList<string> ElementPath
        {
            get { return _elementPath; }
        }

        /// <summary>
        /// Gets or sets the serializer.
        /// </summary>
        public IBsonSerializer Serializer
        {
            get { return _serializer; }
        }

        /// <summary>
        /// Gets or sets the nominal type.
        /// </summary>
        public Type NominalType
        {
            get { return _nominalType; }
        }

        /* DOMAIN-API Both DeserializeValue and SerializeValue should be changed to accept a domain as input.
         * DeserializeValue is only used by BsonDocumentBackedClass
         * SerializeValue is used by BsonDocumentBackedClass and also by GridFS buckets to serialize the Id field.
         *
         * BsonDocumentBackedClass is the base for a few classes in the driver (GridFSFileInfo, ChangeStreamPreAndPostImagesOptions, etc).
         * For those classes we can use the default domain, as no custom serialization is expected.
         *
         * GridFS buckets on the other hand allow the user to specify a custom Id serializer, which may require a custom domain.
         * But looking at the docs it seems that ObjectId is the preferred type for the Id field, and I suppose there won't need to be custom serialization.
         *
         * Thus for now I think it's acceptable to use the default domain in SerializeValue too, but we should revisit this decision at a later time.
         */

        //DOMAIN-API This method should be changed to accept a domain as input.
        /// <summary>
        /// Deserializes the value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>A deserialized value.</returns>
        public object DeserializeValue(BsonValue value)
        {
            var tempDocument = new BsonDocument("value", value);
            using (var reader = new BsonDocumentReader(tempDocument))
            {
                var context = BsonDeserializationContext.CreateRoot(reader, BsonSerializer.DefaultSerializationDomain);
                reader.ReadStartDocument();
                reader.ReadName("value");
                var deserializedValue = _serializer.Deserialize(context);
                reader.ReadEndDocument();
                return deserializedValue;
            }
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                GetType().Equals(obj.GetType()) &&
                obj is BsonSerializationInfo other &&
                object.Equals(_elementName, other._elementName) &&
                object.Equals(_elementPath, other._elementPath) &&
                object.Equals(_nominalType, other._nominalType) &&
                object.Equals(_serializer, other._serializer);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => 0;

        //DOMAIN-API This method should be changed to accept a domain as input.
        /// <summary>
        /// Serializes the value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The serialized value.</returns>
        public BsonValue SerializeValue(object value)
        {
            var tempDocument = new BsonDocument();
            using (var bsonWriter = new BsonDocumentWriter(tempDocument))
            {
                var context = BsonSerializationContext.CreateRoot(bsonWriter, BsonSerializer.DefaultSerializationDomain);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteName("value");
                _serializer.Serialize(context, value);
                bsonWriter.WriteEndDocument();
                return tempDocument[0];
            }
        }

        //DOMAIN-API This method should be probably removed, it's never used.
        /// <summary>
        /// Serializes the values.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <returns>The serialized values.</returns>
        public BsonArray SerializeValues(IEnumerable values)
        {
            var tempDocument = new BsonDocument();
            using (var bsonWriter = new BsonDocumentWriter(tempDocument))
            {
                //QUESTION Is it correct we only need a standard domain here?
                var context = BsonSerializationContext.CreateRoot(bsonWriter, BsonSerializer.DefaultSerializationDomain);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteName("values");
                bsonWriter.WriteStartArray();
                foreach (var value in values)
                {
                    _serializer.Serialize(context, value);
                }
                bsonWriter.WriteEndArray();
                bsonWriter.WriteEndDocument();

                return tempDocument[0].AsBsonArray;
            }
        }

        //DOMAIN-API This method should be probably removed, it's never used.
        /// <summary>
        /// Creates a new BsonSerializationInfo object using the elementName provided and copying all other attributes.
        /// </summary>
        /// <param name="elementName">Name of the element.</param>
        /// <returns>A new BsonSerializationInfo.</returns>
        public BsonSerializationInfo WithNewName(string elementName)
        {
            return new BsonSerializationInfo(
                elementName,
                _serializer,
                _nominalType);
        }
    }
}
