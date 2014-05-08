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

using MongoDB.Bson.Serialization.Conventions;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for Interfaces.
    /// </summary>
    public class DiscriminatedInterfaceSerializer<TInterface> : SerializerBase<TInterface> // where TInterface is an interface
    {
        // private fields
        private readonly Type _interfaceType;
        private readonly IDiscriminatorConvention _discriminatorConvention;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DiscriminatedInterfaceSerializer{TInterface}" /> class.
        /// </summary>
        public DiscriminatedInterfaceSerializer()
            : this(BsonSerializer.LookupDiscriminatorConvention(typeof(TInterface)))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscriminatedInterfaceSerializer{TInterface}" /> class.
        /// </summary>
        /// <param name="discriminatorConvention">The discriminator convention.</param>
        /// <exception cref="System.ArgumentException">interfaceType</exception>
        /// <exception cref="System.ArgumentNullException">interfaceType</exception>
        public DiscriminatedInterfaceSerializer(IDiscriminatorConvention discriminatorConvention)
        {
            if (!typeof(TInterface).IsInterface)
            {
                var message = string.Format("{0} is not an interface.", typeof(TInterface).FullName);
                throw new ArgumentException(message, "<TInterface>");
            }

            _interfaceType = typeof(TInterface);
            _discriminatorConvention = discriminatorConvention;
        }

        // public methods
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>
        /// A document.
        /// </returns>
        /// <exception cref="System.FormatException"></exception>
        public override TInterface Deserialize(BsonDeserializationContext context)
        {
            var bsonReader = context.Reader;

            if (bsonReader.GetCurrentBsonType() == BsonType.Null)
            {
                bsonReader.ReadNull();
                return default(TInterface);
            }
            else
            {
                var actualType = _discriminatorConvention.GetActualType(bsonReader, typeof(TInterface));
                if (actualType == _interfaceType)
                {
                    var message = string.Format("Unable to determine actual type of object to deserialize for interface type {0}.", _interfaceType.FullName);
                    throw new FormatException(message);
                }

                var serializer = BsonSerializer.LookupSerializer(actualType);
                return (TInterface)serializer.Deserialize(context);
            }
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The document.</param>
        public override void Serialize(BsonSerializationContext context, TInterface value)
        {
            var bsonWriter = context.Writer;

            if (value == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                context.SerializeWithChildContext(ObjectSerializer.Instance, value);
            }
        }
    }
}
