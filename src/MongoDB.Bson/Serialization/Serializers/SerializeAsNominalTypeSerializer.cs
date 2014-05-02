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

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for a class that will be serialized as if it were one of its base classes.
    /// </summary>
    public class SerializeAsNominalTypeSerializer<TActualType, TNominalType> : SerializerBase<TActualType> where TActualType : class, TNominalType
    {
        // private fields
        private readonly IBsonSerializer<TNominalType> _nominalTypeSerializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SerializeAsNominalTypeSerializer{TActualType, TNominalType}"/> class.
        /// </summary>
        public SerializeAsNominalTypeSerializer()
            : this(BsonSerializer.LookupSerializer<TNominalType>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializeAsNominalTypeSerializer{TActualType, TNominalType}"/> class.
        /// </summary>
        /// <param name="nominalTypeSerializer">The base class serializer.</param>
        /// <exception cref="System.ArgumentNullException">baseClassSerializer</exception>
        public SerializeAsNominalTypeSerializer(IBsonSerializer<TNominalType> nominalTypeSerializer)
        {
            if (nominalTypeSerializer == null)
            {
                throw new ArgumentNullException("nominalTypeSerializer");
            }

            _nominalTypeSerializer = nominalTypeSerializer;
        }

        // public methods
        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The value.</param>
        public override void Serialize(BsonSerializationContext context, TActualType value)
        {
            if (value == null)
            {
                var bsonWriter = context.Writer;
                bsonWriter.WriteNull();
            }
            else
            {
                var childContext = context.CreateChild<TNominalType>(b => b.SerializeAsNominalType = true);
                _nominalTypeSerializer.Serialize(childContext, value);
            }
        }
    }
}
