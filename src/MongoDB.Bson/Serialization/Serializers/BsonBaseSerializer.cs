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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents an abstract base class for implementers of <see cref="IBsonSerializer{TValue}"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    public abstract class BsonBaseSerializer<TValue> : IBsonSerializer<TValue>
    {
        // public properties
        /// <summary>
        /// Gets the type of the values.
        /// </summary>
        /// <value>
        /// The type of the values.
        /// </value>
        public Type ValueType
        {
            get { return typeof(TValue); }
        }

        // public methods
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>An object.</returns>
        public virtual TValue Deserialize(BsonDeserializationContext context)
        {
            var message = string.Format(
                "A serializer of type '{0}' does not support the Deserialize method.",
                BsonUtils.GetFriendlyTypeName(this.GetType()));
            throw new NotSupportedException(message);
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The object.</param>
        public virtual void Serialize(BsonSerializationContext context, TValue value)
        {
            if (value != null)
            {
                var actualType = value.GetType();
                if (actualType != typeof(TValue) && !context.SerializeAsNominalType)
                {
                    var serializer = BsonSerializer.LookupSerializer(actualType);
                    serializer.Serialize(context, value);
                    return;
                }
            }

            var message = string.Format(
                "A serializer of type '{0}' does not support the Serialize method.", 
                BsonUtils.GetFriendlyTypeName(this.GetType()));
            throw new NotSupportedException(message);
        }

        // protected methods
        /// <summary>
        /// Casts the value to TValue.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The value cast to type TValue.</returns>
        protected virtual TValue CastValue(object value)
        {
            try
            {
                return (TValue)value;
            }
            catch (InvalidCastException)
            {
                var actualType = value.GetType();
                var message = string.Format(
                    "A serializer of type '{0}' expects values of type '{1}', not of type '{2}'.", 
                    BsonUtils.GetFriendlyTypeName(this.GetType()), 
                    BsonUtils.GetFriendlyTypeName(typeof(TValue)), 
                    BsonUtils.GetFriendlyTypeName(actualType));
                throw new NotSupportedException(message);
            }
        }

        /// <summary>
        /// Deserializes the discriminated wrapper.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="discriminatorConvention">The discriminator convention.</param>
        /// <returns>A TValue.</returns>
        /// <exception cref="System.FormatException">
        /// </exception>
        protected TValue DeserializeDiscriminatedWrapper(BsonDeserializationContext context, IDiscriminatorConvention discriminatorConvention)
        {
            var bsonReader = context.Reader;
            var nominalType = context.NominalType;
            var actualType = discriminatorConvention.GetActualType(bsonReader, nominalType);

            bsonReader.ReadStartDocument();

            var firstElementName = bsonReader.ReadName();
            if (firstElementName != discriminatorConvention.ElementName)
            {
                var message = string.Format("Expected the first field of a discriminated wrapper to be '{0}', not: '{1}'.", discriminatorConvention.ElementName, firstElementName);
                throw new FormatException(message);
            }
            bsonReader.SkipValue();

            var secondElementName = bsonReader.ReadName();
            if (secondElementName != "_v")
            {
                var message = string.Format("Expected the second field of a discriminated wrapper to be '_v', not: '{0}'.", firstElementName);
                throw new FormatException(message);
            }

            var unwrappedSerializer = BsonSerializer.LookupSerializer(actualType);
            var unwrappedContext = context.CreateChild(actualType);
            var value = unwrappedSerializer.Deserialize(unwrappedContext);

            if (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
            {
                var message = string.Format("Expected a discriminated wrapper to be a document with exactly two fields, '{0}' and '_v'.", discriminatorConvention.ElementName);
                throw new FormatException(message);
            }

            bsonReader.ReadEndDocument();

            return (TValue)value;
        }

        /// <summary>
        /// Serializes the actual type.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="value">The value.</param>
        protected void SerializeActualType(BsonSerializationContext context, object value)
        {
            if (value == null)
            {
                context.Writer.WriteNull();
            }
            else
            {
                var actualType = value.GetType();
                var serializer = BsonSerializer.LookupSerializer(actualType);
                serializer.Serialize(context, value);
            }
        }

        /// <summary>
        /// Serializes the discriminated wrapper.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="value">The value.</param>
        /// <param name="discriminatorConvention">The discriminator convention.</param>
        protected void SerializeDiscriminatedWrapper(BsonSerializationContext context, TValue value, IDiscriminatorConvention discriminatorConvention)
        {
            var bsonWriter = context.Writer;
            var nominalType = context.NominalType;
            var actualType = value.GetType();
            var discriminator = discriminatorConvention.GetDiscriminator(nominalType, actualType);
            var wrappedContext = context.CreateChild(actualType);

            bsonWriter.WriteStartDocument();
            bsonWriter.WriteName(discriminatorConvention.ElementName);
            context.SerializeWithChildContext(BsonValueSerializer.Instance, discriminator);
            bsonWriter.WriteName("_v");
            Serialize(wrappedContext, value); 
            bsonWriter.WriteEndDocument();
        }

        // explicit interface implementations
        object IBsonSerializer.Deserialize(BsonDeserializationContext context)
        {
            return Deserialize(context);
        }

        void IBsonSerializer.Serialize(BsonSerializationContext context, object value)
        {
            var typedValue = CastValue(value);
            Serialize(context, typedValue);
        }
    }
}
