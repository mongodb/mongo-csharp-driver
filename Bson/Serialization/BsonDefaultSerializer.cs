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
    public class BsonDefaultSerializer : IBsonSerializationProvider
    {
        // private static fields
        private static BsonDefaultSerializer __instance = new BsonDefaultSerializer();
        private static Dictionary<Type, IBsonSerializer> __serializers;
        private static Dictionary<Type, Type> __genericSerializerDefinitions;
        private static Dictionary<Type, IDiscriminatorConvention> __discriminatorConventions = new Dictionary<Type, IDiscriminatorConvention>();
        private static Dictionary<BsonValue, HashSet<Type>> __discriminators = new Dictionary<BsonValue, HashSet<Type>>();
        private static HashSet<Type> __typesWithRegisteredKnownTypes = new HashSet<Type>();
        private static HashSet<Type> __discriminatedTypes = new HashSet<Type>();

        // static constructor
        static BsonDefaultSerializer()
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
        public BsonDefaultSerializer()
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the BsonDefaultSerializer class.
        /// </summary>
        public static BsonDefaultSerializer Instance
        {
            get { return __instance; }
        }

        // public static methods
        /// <summary>
        /// Returns whether the given type has any discriminators registered for any of its subclasses.
        /// </summary>
        /// <param name="type">A Type.</param>
        /// <returns>True if the type is discriminated.</returns>
        public static bool IsTypeDiscriminated(Type type)
        {
            return type.IsInterface || __discriminatedTypes.Contains(type);
        }

        /// <summary>
        /// Looks up the actual type of an object to be deserialized.
        /// </summary>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="discriminator">The discriminator.</param>
        /// <returns>The actual type of the object.</returns>
        public static Type LookupActualType(Type nominalType, BsonValue discriminator)
        {
            if (discriminator == null)
            {
                return nominalType;
            }

            // note: EnsureKnownTypesAreRegistered handles its own locking so call from outside any lock
            EnsureKnownTypesAreRegistered(nominalType);

            BsonSerializer.ConfigLock.EnterReadLock();
            try
            {
                Type actualType = null;

                HashSet<Type> hashSet;
                if (__discriminators.TryGetValue(discriminator, out hashSet))
                {
                    foreach (var type in hashSet)
                    {
                        if (nominalType.IsAssignableFrom(type))
                        {
                            if (actualType == null)
                            {
                                actualType = type;
                            }
                            else
                            {
                                string message = string.Format("Ambiguous discriminator '{0}'.", discriminator);
                                throw new BsonSerializationException(message);
                            }
                        }
                    }
                }

                if (actualType == null && discriminator.IsString)
                {
                    actualType = TypeNameDiscriminator.GetActualType(discriminator.AsString); // see if it's a Type name
                }

                if (actualType == null)
                {
                    string message = string.Format("Unknown discriminator value '{0}'.", discriminator);
                    throw new BsonSerializationException(message);
                }

                if (!nominalType.IsAssignableFrom(actualType))
                {
                    string message = string.Format(
                        "Actual type {0} is not assignable to expected type {1}.",
                        actualType.FullName, nominalType.FullName);
                    throw new BsonSerializationException(message);
                }

                return actualType;
            }
            finally
            {
                BsonSerializer.ConfigLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Looks up the discriminator convention for a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>A discriminator convention.</returns>
        public static IDiscriminatorConvention LookupDiscriminatorConvention(Type type)
        {
            BsonSerializer.ConfigLock.EnterReadLock();
            try
            {
                IDiscriminatorConvention convention;
                if (__discriminatorConventions.TryGetValue(type, out convention))
                {
                    return convention;
                }
            }
            finally
            {
                BsonSerializer.ConfigLock.ExitReadLock();
            }

            BsonSerializer.ConfigLock.EnterWriteLock();
            try
            {
                IDiscriminatorConvention convention;
                if (!__discriminatorConventions.TryGetValue(type, out convention))
                {
                    // if there is no convention registered for object register the default one
                    if (!__discriminatorConventions.ContainsKey(typeof(object)))
                    {
                        var defaultDiscriminatorConvention = StandardDiscriminatorConvention.Hierarchical;
                        __discriminatorConventions.Add(typeof(object), defaultDiscriminatorConvention);
                        if (type == typeof(object))
                        {
                            return defaultDiscriminatorConvention;
                        }
                    }

                    if (type.IsInterface)
                    {
                        // TODO: should convention for interfaces be inherited from parent interfaces?
                        convention = __discriminatorConventions[typeof(object)];
                        __discriminatorConventions[type] = convention;
                    }
                    else
                    {
                        // inherit the discriminator convention from the closest parent that has one
                        Type parentType = type.BaseType;
                        while (convention == null)
                        {
                            if (parentType == null)
                            {
                                var message = string.Format("No discriminator convention found for type {0}.", type.FullName);
                                throw new BsonSerializationException(message);
                            }
                            if (__discriminatorConventions.TryGetValue(parentType, out convention))
                            {
                                break;
                            }
                            parentType = parentType.BaseType;
                        }

                        // register this convention for all types between this and the parent type where we found the convention
                        var unregisteredType = type;
                        while (unregisteredType != parentType)
                        {
                            BsonDefaultSerializer.RegisterDiscriminatorConvention(unregisteredType, convention);
                            unregisteredType = unregisteredType.BaseType;
                        }
                    }
                }
                return convention;
            }
            finally
            {
                BsonSerializer.ConfigLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Registers the discriminator for a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="discriminator">The discriminator.</param>
        public static void RegisterDiscriminator(Type type, BsonValue discriminator)
        {
            if (type.IsInterface)
            {
                var message = string.Format("Discriminators can only be registered for classes, not for interface {0}.", type.FullName);
                throw new BsonSerializationException(message);
            }

            BsonSerializer.ConfigLock.EnterWriteLock();
            try
            {
                HashSet<Type> hashSet;
                if (!__discriminators.TryGetValue(discriminator, out hashSet))
                {
                    hashSet = new HashSet<Type>();
                    __discriminators.Add(discriminator, hashSet);
                }

                if (!hashSet.Contains(type))
                {
                    hashSet.Add(type);

                    // mark all base types as discriminated (so we know that it's worth reading a discriminator)
                    for (var baseType = type.BaseType; baseType != null; baseType = baseType.BaseType)
                    {
                        __discriminatedTypes.Add(baseType);
                    }
                }
            }
            finally
            {
                BsonSerializer.ConfigLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Registers the discriminator convention for a type.
        /// </summary>
        /// <param name="type">Type type.</param>
        /// <param name="convention">The discriminator convention.</param>
        public static void RegisterDiscriminatorConvention(Type type, IDiscriminatorConvention convention)
        {
            BsonSerializer.ConfigLock.EnterWriteLock();
            try
            {
                if (!__discriminatorConventions.ContainsKey(type))
                {
                    __discriminatorConventions.Add(type, convention);
                }
                else
                {
                    var message = string.Format("There is already a discriminator convention registered for type {0}.", type.FullName);
                    throw new BsonSerializationException(message);
                }
            }
            finally
            {
                BsonSerializer.ConfigLock.ExitWriteLock();
            }
        }

        // internal static methods
        internal static void EnsureKnownTypesAreRegistered(Type nominalType)
        {
            BsonSerializer.ConfigLock.EnterReadLock();
            try
            {
                if (__typesWithRegisteredKnownTypes.Contains(nominalType))
                {
                    return;
                }
            }
            finally
            {
                BsonSerializer.ConfigLock.ExitReadLock();
            }

            BsonSerializer.ConfigLock.EnterWriteLock();
            try
            {
                if (!__typesWithRegisteredKnownTypes.Contains(nominalType))
                {
                    // only call LookupClassMap for classes with a BsonKnownTypesAttribute
                    var knownTypesAttribute = nominalType.GetCustomAttributes(typeof(BsonKnownTypesAttribute), false);
                    if (knownTypesAttribute != null && knownTypesAttribute.Length > 0)
                    {
                        // known types will be registered as a side effect of calling LookupClassMap
                        BsonClassMap.LookupClassMap(nominalType);
                    }

                    __typesWithRegisteredKnownTypes.Add(nominalType);
                }
            }
            finally
            {
                BsonSerializer.ConfigLock.ExitWriteLock();
            }
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

            if ((type.IsClass || (type.IsValueType && !type.IsPrimitive)) &&
                !typeof(Array).IsAssignableFrom(type) &&
                !typeof(Enum).IsAssignableFrom(type))
            {
                var classMap = BsonClassMap.LookupClassMap(type);
                return new BsonClassMapSerializer(classMap);
            }

            return null;
        }
    }
}
