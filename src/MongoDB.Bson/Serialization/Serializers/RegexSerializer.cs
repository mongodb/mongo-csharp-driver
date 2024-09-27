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
using System.Text.RegularExpressions;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for Regex.
    /// </summary>
    public sealed class RegexSerializer : SealedClassSerializerBase<Regex>, IRepresentationConfigurable<RegexSerializer>
    {
        #region static
        private static readonly RegexSerializer __regularExpressionInstance = new RegexSerializer(BsonType.RegularExpression);

        /// <summary>
        /// Gets a cached instance of a RegexSerializer with RegularExpression representation.
        /// </summary>
        public static RegexSerializer RegularExpressionInstance => __regularExpressionInstance;
        #endregion

        // private fields
        private readonly BsonType _representation;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="RegexSerializer"/> class.
        /// </summary>
        public RegexSerializer()
            : this(BsonType.RegularExpression)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RegexSerializer"/> class.
        /// </summary>
        /// <param name="representation">The representation.</param>
        public RegexSerializer(BsonType representation)
        {
            switch (representation)
            {
                case BsonType.RegularExpression:
                case BsonType.String:
                    break;

                default:
                    var message = string.Format("{0} is not a valid representation for an RegexSerializer.", representation);
                    throw new ArgumentException(message);
            }

            _representation = representation;
        }

        // public properties
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
        public override Regex Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;

            var bsonType = reader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Null:
                    reader.ReadNull();
                    return null;

                case BsonType.RegularExpression:
                    return reader.ReadRegularExpression().ToRegex();

                case BsonType.String:
                    return new Regex(reader.ReadString());

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
                obj is RegexSerializer other &&
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
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Regex value)
        {
            var writer = context.Writer;

            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            switch (_representation)
            {
                case BsonType.RegularExpression:
                    writer.WriteRegularExpression(new BsonRegularExpression(value));
                    break;

                case BsonType.String:
                    writer.WriteString(value.ToString());
                    break;

                default:
                    var message = string.Format("'{0}' is not a valid Regex representation.", _representation);
                    throw new BsonSerializationException(message);
            }
        }

        /// <summary>
        /// Returns a serializer that has been reconfigured with the specified representation.
        /// </summary>
        /// <param name="representation">The representation.</param>
        /// <returns>The reconfigured serializer.</returns>
        public RegexSerializer WithRepresentation(BsonType representation)
        {
            if (representation == _representation)
            {
                return this;
            }
            else
            {
                return new RegexSerializer(representation);
            }
        }

        // explicit interface implementations
        IBsonSerializer IRepresentationConfigurable.WithRepresentation(BsonType representation)
        {
            return WithRepresentation(representation);
        }
    }
}
