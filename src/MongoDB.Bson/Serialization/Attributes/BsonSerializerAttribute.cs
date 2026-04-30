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

namespace MongoDB.Bson.Serialization.Attributes
{
    /// <summary>
    /// Specifies the type of the serializer to use for a class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Property | AttributeTargets.Field)]
    public class BsonSerializerAttribute : Attribute, IBsonMemberMapAttribute
    {
        // private fields
        private Type _serializerType;

        // constructors
        /// <summary>
        /// Initializes a new instance of the BsonSerializerAttribute class.
        /// </summary>
        public BsonSerializerAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of the BsonSerializerAttribute class.
        /// </summary>
        /// <param name="serializerType">The type of the serializer to use for a class.</param>
        public BsonSerializerAttribute(Type serializerType)
        {
            _serializerType = serializerType;
        }

        // public properties
        /// <summary>
        /// Gets or sets the type of the serializer to use for a class.
        /// </summary>
        public Type SerializerType
        {
            get { return _serializerType; }
            set { _serializerType = value; }
        }

        // public methods
        /// <summary>
        /// Applies a modification to the member map.
        /// </summary>
        /// <param name="memberMap">The member map.</param>
        public void Apply(BsonMemberMap memberMap)
        {
            var registry = ((IHasSerializationDomain)memberMap).SerializationDomain.SerializerRegistry;
            var serializer = CreateSerializer(memberMap.MemberType, registry);
            memberMap.SetSerializer(serializer);
        }

        /// <summary>
        /// Creates a serializer for a type based on the serializer type specified by the attribute.
        /// </summary>
        /// <param name="type">The type that a serializer should be created for.</param>
        /// <returns>A serializer for the type.</returns>
        internal IBsonSerializer CreateSerializer(Type type)
        {
            return CreateSerializer(type, BsonSerializationDomain.Default.SerializerRegistry);
        }

        /// <summary>
        /// Creates a serializer for a type based on the serializer type specified by the attribute,
        /// preferring (in order) a (IBsonSerializationDomain), (IBsonSerializerRegistry), or parameterless ctor.
        /// </summary>
        /// <param name="type">The type that a serializer should be created for.</param>
        /// <param name="serializerRegistry">The serializer registry whose domain context the new serializer should bind to.</param>
        /// <returns>A serializer for the type.</returns>
        internal IBsonSerializer CreateSerializer(Type type, IBsonSerializerRegistry serializerRegistry)
        {
            var typeInfo = type.GetTypeInfo();
            if (typeInfo.ContainsGenericParameters)
            {
                var message = "Cannot create a serializer because the type to serialize is an open generic type.";
                throw new InvalidOperationException(message);
            }

            var serializerTypeInfo = _serializerType.GetTypeInfo();
            if (serializerTypeInfo.ContainsGenericParameters && !typeInfo.IsGenericType)
            {
                var message = "Cannot create a serializer because the serializer type is an open generic type and the type to serialize is not generic.";
                throw new InvalidOperationException(message);
            }

            Type closedSerializerType;
            if (serializerTypeInfo.ContainsGenericParameters)
            {
                var genericArguments = typeInfo.GetGenericArguments();
                closedSerializerType = _serializerType.MakeGenericType(genericArguments);
            }
            else
            {
                closedSerializerType = _serializerType;
            }

            var domain = (serializerRegistry as IHasSerializationDomain)?.SerializationDomain;
            if (domain != null)
            {
                var domainCtor = closedSerializerType.GetConstructor(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    binder: null,
                    types: new[] { typeof(IBsonSerializationDomain) },
                    modifiers: null);
                if (domainCtor != null)
                {
                    return (IBsonSerializer)domainCtor.Invoke(new object[] { domain });
                }
            }

            var registryCtor = closedSerializerType.GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                types: new[] { typeof(IBsonSerializerRegistry) },
                modifiers: null);
            if (registryCtor != null)
            {
                return (IBsonSerializer)registryCtor.Invoke(new object[] { serializerRegistry });
            }

            return (IBsonSerializer)Activator.CreateInstance(closedSerializerType);
        }
    }
}
