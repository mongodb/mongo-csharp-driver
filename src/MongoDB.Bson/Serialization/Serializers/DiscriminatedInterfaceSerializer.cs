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
using System.Reflection;
using MongoDB.Bson.Serialization.Conventions;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// An interface implemented by DiscriminatedInterfaceSerializer.
    /// </summary>
    public interface IDiscriminatedInterfaceSerializer
    {
        /// <summary>
        /// Gets the interface serializer.
        /// </summary>
        IBsonSerializer InterfaceSerializer { get; }
    }

    /// <summary>
    /// Represents a serializer for Interfaces.
    /// </summary>
    /// <typeparam name="TInterface">The type of the interface.</typeparam>
    public sealed class DiscriminatedInterfaceSerializer<TInterface> :
        SerializerBase<TInterface>,
        IBsonDocumentSerializer,
        IDiscriminatedInterfaceSerializer
            // where TInterface is an interface
    {
        #region static
        private static IBsonSerializer<TInterface> CreateInterfaceSerializer(IBsonSerializationDomain serializationDomain)
        {
            var classMapDefinition = typeof(BsonClassMap<>);
            var classMapType = classMapDefinition.MakeGenericType(typeof(TInterface));
            var classMap = (BsonClassMap)Activator.CreateInstance(classMapType);
            classMap.AutoMap();
            classMap.SetDiscriminatorConvention(serializationDomain.LookupDiscriminatorConvention(typeof(TInterface)));
            classMap.Freeze(serializationDomain);
            return new BsonClassMapSerializer<TInterface>(classMap);
        }
        #endregion

        // private fields
        private readonly Type _interfaceType;
        private readonly IBsonSerializer<TInterface> _interfaceSerializer;
        private readonly Lazy<IDiscriminatorConvention> _discriminatorConvention;
        private readonly Lazy<IBsonSerializer<object>> _objectSerializer;

        private IBsonSerializationDomain _serializationDomain;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DiscriminatedInterfaceSerializer{TInterface}" /> class.
        /// </summary>
        public DiscriminatedInterfaceSerializer()
            : this(discriminatorConvention: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscriminatedInterfaceSerializer{TInterface}" /> class.
        /// </summary>
        /// <param name="discriminatorConvention">The discriminator convention.</param>
        /// <exception cref="System.ArgumentException">interfaceType</exception>
        /// <exception cref="System.ArgumentNullException">interfaceType</exception>
        public DiscriminatedInterfaceSerializer(IDiscriminatorConvention discriminatorConvention)
            : this(discriminatorConvention, CreateInterfaceSerializer(BsonSerializer.DefaultSerializationDomain), objectSerializer: null)  //TODO Is this ok?
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscriminatedInterfaceSerializer{TInterface}" /> class.
        /// </summary>
        /// <param name="discriminatorConvention">The discriminator convention.</param>
        /// <param name="interfaceSerializer">The interface serializer (necessary to support LINQ queries).</param>
        /// <exception cref="System.ArgumentException">interfaceType</exception>
        /// <exception cref="System.ArgumentNullException">interfaceType</exception>
        public DiscriminatedInterfaceSerializer(IDiscriminatorConvention discriminatorConvention, IBsonSerializer<TInterface> interfaceSerializer)
            : this(discriminatorConvention, interfaceSerializer, objectSerializer: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscriminatedInterfaceSerializer{TInterface}" /> class.
        /// </summary>
        /// <param name="discriminatorConvention">The discriminator convention.</param>
        /// <param name="interfaceSerializer">The interface serializer (necessary to support LINQ queries).</param>
        /// <param name="objectSerializer">The serializer that is used to serialize any objects.</param>
        /// <exception cref="System.ArgumentException">interfaceType</exception>
        /// <exception cref="System.ArgumentNullException">interfaceType</exception>
        public DiscriminatedInterfaceSerializer(IDiscriminatorConvention discriminatorConvention, IBsonSerializer<TInterface> interfaceSerializer, IBsonSerializer<object> objectSerializer)
        {
            var interfaceTypeInfo = typeof(TInterface).GetTypeInfo();
            if (!interfaceTypeInfo.IsInterface)
            {
                var message = string.Format("{0} is not an interface.", typeof(TInterface).FullName);
                throw new ArgumentException(message, "<TInterface>");
            }

            _interfaceType = typeof(TInterface);
            _discriminatorConvention = discriminatorConvention != null
                ? new Lazy<IDiscriminatorConvention>(() => discriminatorConvention)
                : new Lazy<IDiscriminatorConvention>(() => GetDiscriminatorConvention(_serializationDomain));
            _objectSerializer = objectSerializer != null
                ? new Lazy<IBsonSerializer<object>>(() => objectSerializer)
                : new Lazy<IBsonSerializer<object>>(() => GetObjectSerializer(_serializationDomain));
            _interfaceSerializer = interfaceSerializer;
        }

        // public properties
        /// <summary>
        /// Gets the interface serializer.
        /// </summary>
        public IBsonSerializer<TInterface> InterfaceSerializer => _interfaceSerializer;

        IBsonSerializer IDiscriminatedInterfaceSerializer.InterfaceSerializer => _interfaceSerializer;

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
            _serializationDomain = context.SerializationDomain;
            var bsonReader = context.Reader;

            if (bsonReader.GetCurrentBsonType() == BsonType.Null)
            {
                bsonReader.ReadNull();
                return default(TInterface);
            }
            else
            {
                var actualType = _discriminatorConvention.Value.GetActualTypeInternal(bsonReader, typeof(TInterface), context.SerializationDomain);
                if (actualType == _interfaceType)
                {
                    var message = string.Format("Unable to determine actual type of object to deserialize for interface type {0}.", _interfaceType.FullName);
                    throw new FormatException(message);
                }

                var serializer = BsonSerializer.LookupSerializer(actualType);
                return (TInterface)serializer.Deserialize(context, args);
            }
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                base.Equals(obj) &&
                obj is DiscriminatedInterfaceSerializer<TInterface> other &&
                object.Equals(_discriminatorConvention.Value, other._discriminatorConvention.Value) &&
                object.Equals(_interfaceSerializer, other._interfaceSerializer);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => 0;

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="args">The serialization args.</param>
        /// <param name="value">The document.</param>
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TInterface value)
        {
            _serializationDomain = context.SerializationDomain;
            var bsonWriter = context.Writer;

            if (value == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                args.NominalType = typeof(object);
                _objectSerializer.Value.Serialize(context, args, value);
            }
        }

        /// <inheritdoc/>
        public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            if (_interfaceSerializer is IBsonDocumentSerializer documentSerializer)
            {
                return documentSerializer.TryGetMemberSerializationInfo(memberName, out serializationInfo);
            }

            serializationInfo = null;
            return false;
        }

        private IDiscriminatorConvention GetDiscriminatorConvention(IBsonSerializationDomain serializationDomain)
        {
            return _interfaceSerializer.GetDiscriminatorConvention(serializationDomain);
        }

        private IBsonSerializer<object> GetObjectSerializer(IBsonSerializationDomain serializationDomain)
        {
            var objectSerializer = serializationDomain.LookupSerializer<object>();
            if (objectSerializer is ObjectSerializer standardObjectSerializer)
            {
                var allowedTypes = (Type type) => typeof(TInterface).IsAssignableFrom(type);
                objectSerializer = standardObjectSerializer
                    .WithDiscriminatorConvention(_discriminatorConvention.Value)
                    .WithAllowedTypes(allowedTypes, allowedTypes);
            }
            else
            {
                throw new BsonSerializationException("Can't set discriminator convention on custom object serializer.");
            }

            return objectSerializer;
        }
    }
}
