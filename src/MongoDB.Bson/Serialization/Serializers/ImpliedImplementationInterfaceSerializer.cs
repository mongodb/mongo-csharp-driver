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
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for Interfaces.
    /// </summary>
    /// <typeparam name="TInterface">The type of the interface.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation.</typeparam>
    public class ImpliedImplementationInterfaceSerializer<TInterface, TImplementation> :
        SerializerBase<TInterface>,
        IBsonArraySerializer,
        IBsonDictionarySerializer,
        IBsonDocumentSerializer,
        IChildSerializerConfigurable
            where TImplementation : class, TInterface
    {
        // private fields
        private readonly IBsonSerializer<TImplementation> _implementationSerializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ImpliedImplementationInterfaceSerializer{TInterface, TImplementation}"/> class.
        /// </summary>
        public ImpliedImplementationInterfaceSerializer()
            : this(BsonSerializer.LookupSerializer<TImplementation>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImpliedImplementationInterfaceSerializer{TInterface, TImplementation}"/> class.
        /// </summary>
        /// <param name="implementationSerializer">The implementation serializer.</param>
        public ImpliedImplementationInterfaceSerializer(IBsonSerializer<TImplementation> implementationSerializer)
        {
            if (!typeof(TInterface).IsInterface)
            {
                var message = string.Format("{0} is not an interface.", typeof(TInterface).FullName);
                throw new ArgumentException(message, "<TInterface>");
            }

            _implementationSerializer = implementationSerializer;
        }

        // public properties
        /// <summary>
        /// Gets the dictionary representation.
        /// </summary>
        /// <value>
        /// The dictionary representation.
        /// </value>
        /// <exception cref="System.NotSupportedException"></exception>
        public DictionaryRepresentation DictionaryRepresentation
        {
            get
            {
                var dictionarySerializer = _implementationSerializer as IBsonDictionarySerializer;
                if (dictionarySerializer != null)
                {
                    return dictionarySerializer.DictionaryRepresentation;
                }

                var message = string.Format(
                    "{0} does not have a DictionaryRepresentation.",
                    BsonUtils.GetFriendlyTypeName(_implementationSerializer.GetType()));
                throw new NotSupportedException(message);
            }
        }

        /// <summary>
        /// Gets the key serializer.
        /// </summary>
        /// <value>
        /// The key serializer.
        /// </value>
        /// <exception cref="System.NotSupportedException"></exception>
        public IBsonSerializer KeySerializer
        {
            get
            {
                var dictionarySerializer = _implementationSerializer as IBsonDictionarySerializer;
                if (dictionarySerializer != null)
                {
                    return dictionarySerializer.KeySerializer;
                }

                var message = string.Format(
                    "{0} does not have a KeySerializer.",
                    BsonUtils.GetFriendlyTypeName(_implementationSerializer.GetType()));
                throw new NotSupportedException(message);
            }
        }

        /// <summary>
        /// Gets the implementation serializer.
        /// </summary>
        /// <value>
        /// The implementation serializer.
        /// </value>
        public IBsonSerializer<TImplementation> ImplementationSerializer
        {
            get { return _implementationSerializer; }
        }

        /// <summary>
        /// Gets the value serializer.
        /// </summary>
        /// <value>
        /// The value serializer.
        /// </value>
        /// <exception cref="System.NotSupportedException"></exception>
        public IBsonSerializer ValueSerializer
        {
            get
            {
                var dictionarySerializer = _implementationSerializer as IBsonDictionarySerializer;
                if (dictionarySerializer != null)
                {
                    return dictionarySerializer.ValueSerializer;
                }

                var message = string.Format(
                    "{0} does not have a ValueSerializer.",
                    BsonUtils.GetFriendlyTypeName(_implementationSerializer.GetType()));
                throw new NotSupportedException(message);
            }
        }

        // public methods
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <param name="args">The deserialization args.</param>
        /// <returns>A deserialized value.</returns>
        /// <exception cref="System.FormatException"></exception>
        public override TInterface Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var bsonReader = context.Reader;

            if (bsonReader.GetCurrentBsonType() == BsonType.Null)
            {
                bsonReader.ReadNull();
                return default(TInterface);
            }
            else
            {
                return _implementationSerializer.Deserialize(context);
            }
        }

        /// <summary>
        /// Gets the serialization info for individual items of the array.
        /// </summary>
        /// <returns>
        /// The serialization info for the items.
        /// </returns>
        public BsonSerializationInfo GetItemSerializationInfo()
        {
            var arraySerializer = _implementationSerializer as IBsonArraySerializer;
            if (arraySerializer != null)
            {
                return arraySerializer.GetItemSerializationInfo();
            }

            return null;
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
            BsonSerializationInfo serializationInfo;
            if (!TryGetMemberSerializationInfo(memberName, out serializationInfo))
            {
                var message = string.Format("{0} is not a member of {1}.", memberName, typeof(TImplementation));
                throw new ArgumentOutOfRangeException("memberName", message);
            }

            return serializationInfo;
        }

        /// <summary>
        /// Tries to get the serialization info for a member.
        /// </summary>
        /// <param name="memberName">Name of the member.</param>
        /// <param name="serializationInfo">The serialization information.</param>
        /// <returns>
        ///   <c>true</c> if the serialization info exists; otherwise <c>false</c>.
        /// </returns>
        public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            var documentSerializer = _implementationSerializer as IBsonDocumentSerializer;
            if (documentSerializer != null)
            {
                return documentSerializer.TryGetMemberSerializationInfo(memberName, out serializationInfo);
            }

            serializationInfo = null;
            return false;
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="args">The serialization args.</param>
        /// <param name="value">The document.</param>
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TInterface value)
        {
            var bsonWriter = context.Writer;

            if (value == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                var actualType = value.GetType();
                if (actualType == typeof(TImplementation))
                {
                    _implementationSerializer.Serialize(context, (TImplementation)value);
                }
                else
                {
                    var serializer = BsonSerializer.LookupSerializer(actualType);
                    serializer.Serialize(context, value);
                }
            }
        }

        /// <summary>
        /// Returns a serializer that has been reconfigured with the specified implementation serializer.
        /// </summary>
        /// <param name="implementationSerializer">The implementation serializer.</param>
        /// <returns>
        /// The reconfigured serializer.
        /// </returns>
        public ImpliedImplementationInterfaceSerializer<TInterface, TImplementation> WithImplementationSerializer(IBsonSerializer<TImplementation> implementationSerializer)
        {
            if (implementationSerializer == ImplementationSerializer)
            {
                return this;
            }
            else
            {
                return new ImpliedImplementationInterfaceSerializer<TInterface, TImplementation>(implementationSerializer);
            }
        }

        // explicit interface implementations
        IBsonSerializer IChildSerializerConfigurable.ChildSerializer
        {
            get { return _implementationSerializer; }
        }

        IBsonSerializer IChildSerializerConfigurable.WithChildSerializer(IBsonSerializer childSerializer)
        {
            return WithImplementationSerializer((IBsonSerializer<TImplementation>)childSerializer);
        }
    }
}