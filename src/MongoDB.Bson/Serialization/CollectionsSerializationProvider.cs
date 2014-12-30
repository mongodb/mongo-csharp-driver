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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Bson.Serialization
{
    /// <summary>
    /// Provides serializers for collections.
    /// </summary>
    public class CollectionsSerializationProvider : BsonSerializationProviderBase
    {
        private static readonly Dictionary<Type, Type> __serializerTypes;

        static CollectionsSerializationProvider()
        {
            __serializerTypes = new Dictionary<Type, Type>
            {
                { typeof(BitArray), typeof(BitArraySerializer) },
                { typeof(ExpandoObject), typeof(ExpandoObjectSerializer) },
                { typeof(Queue), typeof(QueueSerializer) },
                { typeof(Stack), typeof(StackSerializer) },
                { typeof(Queue<>), typeof(QueueSerializer<>) },
                { typeof(ReadOnlyCollection<>), typeof(ReadOnlyCollectionSerializer<>) },
                { typeof(Stack<>), typeof(StackSerializer<>) },
            };
        }

        /// <summary>
        /// Gets a serializer for a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        /// A serializer.
        /// </returns>
        /// <exception cref="BsonSerializationException"></exception>
        public override IBsonSerializer GetSerializer(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            if (type.IsGenericType && type.ContainsGenericParameters)
            {
                var message = string.Format("Generic type {0} has unassigned type parameters.", BsonUtils.GetFriendlyTypeName(type));
                throw new ArgumentException(message, "type");
            }

            Type serializerType;
            if (__serializerTypes.TryGetValue(type, out serializerType))
            {
                return CreateSerializer(serializerType);
            }

            if (type.IsGenericType && !type.ContainsGenericParameters)
            {
                Type serializerTypeDefinition;
                if (__serializerTypes.TryGetValue(type.GetGenericTypeDefinition(), out serializerTypeDefinition))
                {
                    return CreateGenericSerializer(serializerTypeDefinition, type.GetGenericArguments());
                }
            }

            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                switch (type.GetArrayRank())
                {
                    case 1:
                        var arraySerializerDefinition = typeof(ArraySerializer<>);
                        return CreateGenericSerializer(arraySerializerDefinition, elementType);
                    case 2:
                        var twoDimensionalArraySerializerDefinition = typeof(TwoDimensionalArraySerializer<>);
                        return CreateGenericSerializer(twoDimensionalArraySerializerDefinition, elementType);
                    case 3:
                        var threeDimensionalArraySerializerDefinition = typeof(ThreeDimensionalArraySerializer<>);
                        return CreateGenericSerializer(threeDimensionalArraySerializerDefinition, elementType);
                    default:
                        var message = string.Format("No serializer found for array for rank {0}.", type.GetArrayRank());
                        throw new BsonSerializationException(message);
                }
            }

            return GetCollectionSerializer(type);
        }

        private IBsonSerializer GetCollectionSerializer(Type type)
        {
            Type implementedGenericDictionaryInterface = null;
            Type implementedGenericEnumerableInterface = null;
            Type implementedGenericSetInterface = null;
            Type implementedDictionaryInterface = null;
            Type implementedEnumerableInterface = null;

            var implementedInterfaces = new List<Type>(type.GetInterfaces());
            if (type.IsInterface)
            {
                implementedInterfaces.Add(type);
            }

            foreach (var implementedInterface in implementedInterfaces)
            {
                if (implementedInterface.IsGenericType)
                {
                    var genericInterfaceDefinition = implementedInterface.GetGenericTypeDefinition();
                    if (genericInterfaceDefinition == typeof(IDictionary<,>))
                    {
                        implementedGenericDictionaryInterface = implementedInterface;
                    }
                    if (genericInterfaceDefinition == typeof(IEnumerable<>))
                    {
                        implementedGenericEnumerableInterface = implementedInterface;
                    }
                    if (genericInterfaceDefinition == typeof(ISet<>))
                    {
                        implementedGenericSetInterface = implementedInterface;
                    }
                }
                else
                {
                    if (implementedInterface == typeof(IDictionary))
                    {
                        implementedDictionaryInterface = implementedInterface;
                    }
                    if (implementedInterface == typeof(IEnumerable))
                    {
                        implementedEnumerableInterface = implementedInterface;
                    }
                }
            }

            // the order of the tests is important
            if (implementedGenericDictionaryInterface != null)
            {
                var keyType = implementedGenericDictionaryInterface.GetGenericArguments()[0];
                var valueType = implementedGenericDictionaryInterface.GetGenericArguments()[1];
                if (type.IsInterface)
                {
                    var dictionaryDefinition = typeof(Dictionary<,>);
                    var dictionaryType = dictionaryDefinition.MakeGenericType(keyType, valueType);
                    var serializerDefinition = typeof(ImpliedImplementationInterfaceSerializer<,>);
                    return CreateGenericSerializer(serializerDefinition, type, dictionaryType);
                }
                else
                {
                    var serializerDefinition = typeof(DictionaryInterfaceImplementerSerializer<,,>);
                    return CreateGenericSerializer(serializerDefinition, type, keyType, valueType);
                }
            }
            else if (implementedDictionaryInterface != null)
            {
                if (type.IsInterface)
                {
                    var dictionaryType = typeof(Hashtable);
                    var serializerDefinition = typeof(ImpliedImplementationInterfaceSerializer<,>);
                    return CreateGenericSerializer(serializerDefinition, type, dictionaryType);
                }
                else
                {
                    var serializerDefinition = typeof(DictionaryInterfaceImplementerSerializer<>);
                    return CreateGenericSerializer(serializerDefinition, type);
                }
            }
            else if (implementedGenericSetInterface != null)
            {
                var itemType = implementedGenericSetInterface.GetGenericArguments()[0];

                if (type.IsInterface)
                {
                    var hashSetDefinition = typeof(HashSet<>);
                    var hashSetType = hashSetDefinition.MakeGenericType(itemType);
                    var serializerDefinition = typeof(ImpliedImplementationInterfaceSerializer<,>);
                    return CreateGenericSerializer(serializerDefinition, type, hashSetType);
                }
                else
                {
                    var serializerDefinition = typeof(EnumerableInterfaceImplementerSerializer<,>);
                    return CreateGenericSerializer(serializerDefinition, type, itemType);
                }
            }
            else if (implementedGenericEnumerableInterface != null)
            {
                var itemType = implementedGenericEnumerableInterface.GetGenericArguments()[0];

                var readOnlyCollectionType = typeof(ReadOnlyCollection<>).MakeGenericType(itemType);
                if (type == readOnlyCollectionType)
                {
                    var serializerDefinition = typeof(ReadOnlyCollectionSerializer<>);
                    return CreateGenericSerializer(serializerDefinition, itemType);
                }
                else if (readOnlyCollectionType.IsAssignableFrom(type))
                {
                    var serializerDefinition = typeof(ReadOnlyCollectionSubclassSerializer<,>);
                    return CreateGenericSerializer(serializerDefinition, type, itemType);
                }
                else if (type.IsInterface)
                {
                    var listDefinition = typeof(List<>);
                    var listType = listDefinition.MakeGenericType(itemType);
                    var serializerDefinition = typeof(ImpliedImplementationInterfaceSerializer<,>);
                    return CreateGenericSerializer(serializerDefinition, type, listType);
                }
                else
                {
                    var serializerDefinition = typeof(EnumerableInterfaceImplementerSerializer<,>);
                    return CreateGenericSerializer(serializerDefinition, type, itemType);
                }
            }
            else if (implementedEnumerableInterface != null)
            {
                if (type.IsInterface)
                {
                    var listType = typeof(ArrayList);
                    var serializerDefinition = typeof(ImpliedImplementationInterfaceSerializer<,>);
                    return CreateGenericSerializer(serializerDefinition, type, listType);
                }
                else
                {
                    var serializerDefinition = typeof(EnumerableInterfaceImplementerSerializer<>);
                    return CreateGenericSerializer(serializerDefinition, type);
                }
            }

            return null;
        }
    }
}
