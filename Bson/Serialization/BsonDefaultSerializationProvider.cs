/* Copyright 2010-2012 10gen Inc.
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
using System.Collections.Specialized;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Net;

using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Bson.Serialization
{
    /// <summary>
    /// Represents the default serialization provider.
    /// </summary>
    internal class BsonDefaultSerializationProvider : IBsonSerializationProvider
    {
        // private static fields
        private static Dictionary<Type, IBsonSerializer> __serializers;
        private static Dictionary<Type, Type> __genericSerializerDefinitions;

        // static constructor
        static BsonDefaultSerializationProvider()
        {
            __serializers = new Dictionary<Type, IBsonSerializer>
            {
                { typeof(BitArray), BitArraySerializer.Instance },
                { typeof(Bitmap), BitmapSerializer.Instance },
                { typeof(Boolean), BooleanSerializer.Instance },
                { typeof(BsonArray), BsonArraySerializer.Instance },
                { typeof(BsonBinaryData), BsonBinaryDataSerializer.Instance },
                { typeof(BsonBoolean), BsonBooleanSerializer.Instance },
                { typeof(BsonDateTime), BsonDateTimeSerializer.Instance },
                { typeof(BsonDocument), BsonDocumentSerializer.Instance },
                { typeof(BsonDocumentWrapper), BsonDocumentWrapperSerializer.Instance },
                { typeof(BsonDouble), BsonDoubleSerializer.Instance },
                { typeof(BsonInt32), BsonInt32Serializer.Instance },
                { typeof(BsonInt64), BsonInt64Serializer.Instance },
                { typeof(BsonJavaScript), BsonJavaScriptSerializer.Instance },
                { typeof(BsonJavaScriptWithScope), BsonJavaScriptWithScopeSerializer.Instance },
                { typeof(BsonMaxKey), BsonMaxKeySerializer.Instance },
                { typeof(BsonMinKey), BsonMinKeySerializer.Instance },
                { typeof(BsonNull), BsonNullSerializer.Instance },
                { typeof(BsonObjectId), BsonObjectIdSerializer.Instance },
                { typeof(BsonRegularExpression), BsonRegularExpressionSerializer.Instance },
                { typeof(BsonString), BsonStringSerializer.Instance },
                { typeof(BsonSymbol), BsonSymbolSerializer.Instance },
                { typeof(BsonTimestamp), BsonTimestampSerializer.Instance },
                { typeof(BsonUndefined), BsonUndefinedSerializer.Instance },
                { typeof(BsonValue), BsonValueSerializer.Instance },
                { typeof(Byte), ByteSerializer.Instance },
                { typeof(Byte[]), ByteArraySerializer.Instance },
                { typeof(Char), CharSerializer.Instance },
                { typeof(CultureInfo), CultureInfoSerializer.Instance },
                { typeof(DateTime), DateTimeSerializer.Instance },
                { typeof(DateTimeOffset), DateTimeOffsetSerializer.Instance },
                { typeof(Decimal), DecimalSerializer.Instance },
                { typeof(Double), DoubleSerializer.Instance },
                { typeof(System.Drawing.Size), DrawingSizeSerializer.Instance },
                { typeof(Guid), GuidSerializer.Instance },
                { typeof(IBsonSerializable), BsonIBsonSerializableSerializer.Instance },
                { typeof(Image), ImageSerializer.Instance },
                { typeof(Int16), Int16Serializer.Instance },
                { typeof(Int32), Int32Serializer.Instance },
                { typeof(Int64), Int64Serializer.Instance },
                { typeof(IPAddress), IPAddressSerializer.Instance },
                { typeof(IPEndPoint), IPEndPointSerializer.Instance },
                { typeof(Object), ObjectSerializer.Instance },
                { typeof(ObjectId), ObjectIdSerializer.Instance },
                { typeof(Queue), QueueSerializer.Instance },
                { typeof(SByte), SByteSerializer.Instance },
                { typeof(Single), SingleSerializer.Instance },
                { typeof(Stack), StackSerializer.Instance },
                { typeof(String), StringSerializer.Instance },
                { typeof(TimeSpan), TimeSpanSerializer.Instance },
                { typeof(UInt16), UInt16Serializer.Instance },
                { typeof(UInt32), UInt32Serializer.Instance },
                { typeof(UInt64), UInt64Serializer.Instance },
                { typeof(Uri), UriSerializer.Instance },
                { typeof(Version), VersionSerializer.Instance }
            };

            __genericSerializerDefinitions = new Dictionary<Type, Type>
            {
                { typeof(KeyValuePair<,>), typeof(KeyValuePairSerializer<,>) },
                { typeof(Nullable<>), typeof(NullableSerializer<>) },
                { typeof(Queue<>), typeof(QueueSerializer<>) },
                { typeof(Stack<>), typeof(StackSerializer<>) }
            };
        }

        // constructors
        /// <summary>
        /// Initializes a new instance of the BsonDefaultSerializer class.
        /// </summary>
        public BsonDefaultSerializationProvider()
        {
        }

        // public methods
        /// <summary>
        /// Gets the serializer for a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The serializer.</returns>
        public IBsonSerializer GetSerializer(Type type)
        {
            IBsonSerializer serializer;
            if (__serializers.TryGetValue(type, out serializer))
            {
                return serializer;
            }

            if (type.IsGenericType)
            {
                var genericTypeDefinition = type.GetGenericTypeDefinition();
                Type genericSerializerDefinition;
                if (__genericSerializerDefinitions.TryGetValue(genericTypeDefinition, out genericSerializerDefinition))
                {
                    var genericSerializerType = genericSerializerDefinition.MakeGenericType(type.GetGenericArguments());
                    return (IBsonSerializer)Activator.CreateInstance(genericSerializerType);
                }
            }

            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                switch (type.GetArrayRank())
                {
                    case 1:
                        var arraySerializerDefinition = typeof(ArraySerializer<>);
                        var arraySerializerType = arraySerializerDefinition.MakeGenericType(elementType);
                        return (IBsonSerializer)Activator.CreateInstance(arraySerializerType);
                    case 2:
                        var twoDimensionalArraySerializerDefinition = typeof(TwoDimensionalArraySerializer<>);
                        var twoDimensionalArraySerializerType = twoDimensionalArraySerializerDefinition.MakeGenericType(elementType);
                        return (IBsonSerializer)Activator.CreateInstance(twoDimensionalArraySerializerType);
                    case 3:
                        var threeDimensionalArraySerializerDefinition = typeof(ThreeDimensionalArraySerializer<>);
                        var threeDimensionalArraySerializerType = threeDimensionalArraySerializerDefinition.MakeGenericType(elementType);
                        return (IBsonSerializer)Activator.CreateInstance(threeDimensionalArraySerializerType);
                    default:
                        var message = string.Format("No serializer found for array for rank {0}.", type.GetArrayRank());
                        throw new BsonSerializationException(message);
                }
            }

            if (type.IsEnum)
            {
                return EnumSerializer.Instance;
            }

            // classes that implement IDictionary or IEnumerable are serialized using either DictionarySerializer or EnumerableSerializer
            // this does mean that any additional public properties the class might have won't be serialized (just like the XmlSerializer)
            var collectionSerializer = GetCollectionSerializer(type);
            if (collectionSerializer != null)
            {
                return collectionSerializer;
            }

            return null;
        }

        // private methods
        private IBsonSerializer GetCollectionSerializer(Type type)
        {
            Type implementedGenericDictionaryInterface = null;
            Type implementedGenericEnumerableInterface = null;
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
                var genericSerializerDefinition = typeof(DictionarySerializer<,>);
                var genericSerializerType = genericSerializerDefinition.MakeGenericType(keyType, valueType);
                return (IBsonSerializer)Activator.CreateInstance(genericSerializerType);
            }
            else if (implementedDictionaryInterface != null)
            {
                return DictionarySerializer.Instance;
            }
            else if (implementedGenericEnumerableInterface != null)
            {
                var valueType = implementedGenericEnumerableInterface.GetGenericArguments()[0];
                var genericSerializerDefinition = typeof(EnumerableSerializer<>);
                var genericSerializerType = genericSerializerDefinition.MakeGenericType(valueType);
                return (IBsonSerializer)Activator.CreateInstance(genericSerializerType);
            }
            else if (implementedEnumerableInterface != null)
            {
                return EnumerableSerializer.Instance;
            }

            return null;
        }
    }
}
