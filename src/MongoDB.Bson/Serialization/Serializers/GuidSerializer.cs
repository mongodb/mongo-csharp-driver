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

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for Guids.
    /// </summary>
    public sealed class GuidSerializer : StructSerializerBase<Guid>, IRepresentationConfigurable<GuidSerializer>
    {
        #region static
        private static readonly GuidSerializer __standardInstance = new GuidSerializer(GuidRepresentation.Standard);

        /// <summary>
        /// Gets a cached instance of a GuidSerializer with Standard representation.
        /// </summary>
        public static GuidSerializer StandardInstance => __standardInstance;
        #endregion

        // private fields
        private readonly GuidRepresentation _guidRepresentation; // only relevant if _representation is Binary
        private readonly BsonType _representation;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="GuidSerializer"/> class.
        /// </summary>
        public GuidSerializer()
            : this(BsonType.Binary)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GuidSerializer"/> class.
        /// </summary>
        /// <param name="representation">The representation.</param>
        public GuidSerializer(BsonType representation)
        {
            switch (representation)
            {
                case BsonType.Binary:
                case BsonType.String:
                    break;

                default:
                    var message = string.Format("{0} is not a valid representation for a GuidSerializer.", representation);
                    throw new ArgumentException(message, nameof(representation));
            }

            _representation = representation;
            _guidRepresentation = GuidRepresentation.Unspecified;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GuidSerializer"/> class.
        /// </summary>
        /// <param name="guidRepresentation">The Guid representation.</param>
        public GuidSerializer(GuidRepresentation guidRepresentation)
        {
            _representation = BsonType.Binary;
            _guidRepresentation = guidRepresentation;
        }

        // public properties
        /// <summary>
        /// Gets the Guid representation.
        /// </summary>
        public GuidRepresentation GuidRepresentation => _guidRepresentation;

        /// <summary>
        /// Gets the representation.
        /// </summary>
        /// <value>
        /// The representation.
        /// </value>
        public BsonType Representation
        {
            get { return _representation; }
        }

        // public methods
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <param name="args">The deserialization args.</param>
        /// <returns>A deserialized value.</returns>
        public override Guid Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var bsonReader = context.Reader;

            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Binary:
                    if (_guidRepresentation == GuidRepresentation.Unspecified)
                    {
                        throw new BsonSerializationException("GuidSerializer cannot deserialize a Guid when GuidRepresentation is Unspecified.");
                    }
                    return bsonReader.ReadGuid(_guidRepresentation);

                case BsonType.String:
                    return new Guid(bsonReader.ReadString());

                default:
                    throw CreateCannotDeserializeFromBsonTypeException(bsonType);
            }
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                base.Equals(obj) &&
                obj is GuidSerializer other &&
                _guidRepresentation.Equals(other._guidRepresentation) &&
                _representation.Equals(other._representation);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => 0;

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="args">The serialization args.</param>
        /// <param name="value">The object.</param>
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Guid value)
        {
            var bsonWriter = context.Writer;

            switch (_representation)
            {
                case BsonType.Binary:
                    if (_guidRepresentation == GuidRepresentation.Unspecified)
                    {
                        throw new BsonSerializationException("GuidSerializer cannot serialize a Guid when GuidRepresentation is Unspecified.");
                    }
                    bsonWriter.WriteGuid(value, _guidRepresentation);
                    break;

                case BsonType.String:
                    bsonWriter.WriteString(value.ToString());
                    break;

                default:
                    var message = string.Format("'{0}' is not a valid Guid representation.", _representation);
                    throw new BsonSerializationException(message);
            }
        }

        /// <summary>
        /// Returns a serializer that has been reconfigured with the specified Guid representation.
        /// </summary>
        /// <param name="guidRepresentation">The GuidRepresentation.</param>
        /// <returns>The reconfigured serializer.</returns>
        public GuidSerializer WithGuidRepresentation(GuidRepresentation guidRepresentation)
        {
            return new GuidSerializer(guidRepresentation);
        }

        /// <summary>
        /// Returns a serializer that has been reconfigured with the specified representation.
        /// </summary>
        /// <param name="representation">The representation.</param>
        /// <returns>The reconfigured serializer.</returns>
        public GuidSerializer WithRepresentation(BsonType representation)
        {
            return new GuidSerializer(representation);
        }

        // explicit interface implementations
        IBsonSerializer IRepresentationConfigurable.WithRepresentation(BsonType representation)
        {
            return WithRepresentation(representation);
        }
    }
}
