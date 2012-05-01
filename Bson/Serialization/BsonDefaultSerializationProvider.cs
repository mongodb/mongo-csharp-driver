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
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Bson.Serialization
{
    /// <summary>
    /// Represents the default serialization provider.
    /// </summary>
    internal class BsonDefaultSerializationProvider : IBsonSerializationProvider
    {
        // private static fields
        private static BsonDefaultSerializationProvider __instance = new BsonDefaultSerializationProvider();
        private static Dictionary<Type, IBsonSerializer> __serializers;
        private static Dictionary<Type, Type> __genericSerializerDefinitions;

        // static constructor
        static BsonDefaultSerializationProvider()
        {
            __serializers = new Dictionary<Type, IBsonSerializer>
            {
                { typeof(ArrayList), EnumerableSerializer.Instance },
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
                { typeof(Hashtable), DictionarySerializer.Instance },
                { typeof(IBsonSerializable), BsonIBsonSerializableSerializer.Instance },
                { typeof(ICollection), EnumerableSerializer.Instance },
                { typeof(IDictionary), DictionarySerializer.Instance },
                { typeof(IEnumerable), EnumerableSerializer.Instance },
                { typeof(IList), EnumerableSerializer.Instance },
                { typeof(Image), ImageSerializer.Instance },
                { typeof(Int16), Int16Serializer.Instance },
                { typeof(Int32), Int32Serializer.Instance },
                { typeof(Int64), Int64Serializer.Instance },
                { typeof(IPAddress), IPAddressSerializer.Instance },
                { typeof(IPEndPoint), IPEndPointSerializer.Instance },
                { typeof(ListDictionary), DictionarySerializer.Instance },
                { typeof(Object), ObjectSerializer.Instance },
                { typeof(ObjectId), ObjectIdSerializer.Instance },
                { typeof(OrderedDictionary), DictionarySerializer.Instance },
                { typeof(Queue), QueueSerializer.Instance },
                { typeof(SByte), SByteSerializer.Instance },
                { typeof(Single), SingleSerializer.Instance },
                { typeof(SortedList), DictionarySerializer.Instance },
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
                { typeof(Collection<>), typeof(EnumerableSerializer<>)},
                { typeof(Dictionary<,>), typeof(DictionarySerializer<,>) },
                { typeof(HashSet<>), typeof(EnumerableSerializer<>) },
                { typeof(ICollection<>), typeof(EnumerableSerializer<>) },
                { typeof(IDictionary<,>), typeof(DictionarySerializer<,>) },
                { typeof(IEnumerable<>), typeof(EnumerableSerializer<>) },
                { typeof(IList<>), typeof(EnumerableSerializer<>) },
                { typeof(LinkedList<>), typeof(EnumerableSerializer<>) },
                { typeof(List<>), typeof(EnumerableSerializer<>) },
                { typeof(Nullable<>), typeof(NullableSerializer<>) },
                { typeof(ObservableCollection<>), typeof(EnumerableSerializer<>)},
                { typeof(Queue<>), typeof(QueueSerializer<>) },
                { typeof(SortedDictionary<,>), typeof(DictionarySerializer<,>) },
                { typeof(SortedList<,>), typeof(DictionarySerializer<,>) },
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

            return null;
        }
    }
}
