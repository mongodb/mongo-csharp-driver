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
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Bson.Serialization
{
    /// <summary>
    /// Provides serializers for primitive types.
    /// </summary>
    public class PrimitiveSerializationProvider : BsonSerializationProviderBase
    {
        private static readonly Dictionary<Type, Func<IBsonSerializationDomain, IBsonSerializerRegistry, IBsonSerializer>> __serializerFactories;
        private static readonly Dictionary<Type, Type> __serializerTypeDefinitions;

        static PrimitiveSerializationProvider()
        {
            // non-generic serializers are constructed directly so no constructor reflection is needed (Native AOT safe)
            __serializerFactories = new Dictionary<Type, Func<IBsonSerializationDomain, IBsonSerializerRegistry, IBsonSerializer>>
            {
                { typeof(Boolean), (domain, registry) => new BooleanSerializer() },
                { typeof(Byte), (domain, registry) => new ByteSerializer() },
                { typeof(Byte[]), (domain, registry) => new ByteArraySerializer() },
                { typeof(Char), (domain, registry) => new CharSerializer() },
                { typeof(CultureInfo), (domain, registry) => new CultureInfoSerializer() },
                { typeof(DateTime), (domain, registry) => new DateTimeSerializer() },
#if NET6_0_OR_GREATER
                { typeof(DateOnly), (domain, registry) => new DateOnlySerializer() },
#endif
                { typeof(DateTimeOffset), (domain, registry) => new DateTimeOffsetSerializer() },
                { typeof(Decimal), (domain, registry) => new DecimalSerializer() },
                { typeof(Decimal128), (domain, registry) => new Decimal128Serializer() },
                { typeof(Double), (domain, registry) => new DoubleSerializer() },
                { typeof(Guid), (domain, registry) => new GuidSerializer() },
#if NET6_0_OR_GREATER
                { typeof(Half), (domain, registry) => new HalfSerializer() },
#endif
                { typeof(Int16), (domain, registry) => new Int16Serializer() },
                { typeof(Int32), (domain, registry) => new Int32Serializer() },
                { typeof(Int64), (domain, registry) => new Int64Serializer() },
                { typeof(IPAddress), (domain, registry) => new IPAddressSerializer() },
                { typeof(IPEndPoint), (domain, registry) => new IPEndPointSerializer() },
                { typeof(Object), (domain, registry) => new ObjectSerializer(domain) },
                { typeof(ObjectId), (domain, registry) => new ObjectIdSerializer() },
                { typeof(Regex), (domain, registry) => new RegexSerializer() },
                { typeof(SByte), (domain, registry) => new SByteSerializer() },
                { typeof(Single), (domain, registry) => new SingleSerializer() },
                { typeof(String), (domain, registry) => new StringSerializer() },
#if NET6_0_OR_GREATER
                { typeof(TimeOnly), (domain, registry) => new TimeOnlySerializer() },
#endif
                { typeof(TimeSpan), (domain, registry) => new TimeSpanSerializer() },
                { typeof(UInt16), (domain, registry) => new UInt16Serializer() },
                { typeof(UInt32), (domain, registry) => new UInt32Serializer() },
                { typeof(UInt64), (domain, registry) => new UInt64Serializer() },
                { typeof(Uri), (domain, registry) => new UriSerializer() },
                { typeof(Version), (domain, registry) => new VersionSerializer() }
            };

            __serializerTypeDefinitions = new Dictionary<Type, Type>
            {
                { typeof(KeyValuePair<,>), typeof(KeyValuePairSerializer<,>) },
                { typeof(Nullable<>), typeof(NullableSerializer<>) },
                { typeof(Tuple<>), typeof(TupleSerializer<>) },
                { typeof(Tuple<,>), typeof(TupleSerializer<,>) },
                { typeof(Tuple<,,>), typeof(TupleSerializer<,,>) },
                { typeof(Tuple<,,,>), typeof(TupleSerializer<,,,>) },
                { typeof(Tuple<,,,,>), typeof(TupleSerializer<,,,,>) },
                { typeof(Tuple<,,,,,>), typeof(TupleSerializer<,,,,,>) },
                { typeof(Tuple<,,,,,,>), typeof(TupleSerializer<,,,,,,>) },
                { typeof(Tuple<,,,,,,,>), typeof(TupleSerializer<,,,,,,,>) },
                { typeof(ValueTuple<>), typeof(ValueTupleSerializer<>) },
                { typeof(ValueTuple<,>), typeof(ValueTupleSerializer<,>) },
                { typeof(ValueTuple<,,>), typeof(ValueTupleSerializer<,,>) },
                { typeof(ValueTuple<,,,>), typeof(ValueTupleSerializer<,,,>) },
                { typeof(ValueTuple<,,,,>), typeof(ValueTupleSerializer<,,,,>) },
                { typeof(ValueTuple<,,,,,>), typeof(ValueTupleSerializer<,,,,,>) },
                { typeof(ValueTuple<,,,,,,>), typeof(ValueTupleSerializer<,,,,,,>) },
                { typeof(ValueTuple<,,,,,,,>), typeof(ValueTupleSerializer<,,,,,,,>) }
            };
        }

        /// <inheritdoc/>
        public override IBsonSerializer GetSerializer(Type type, IBsonSerializerRegistry serializerRegistry)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsGenericType && typeInfo.ContainsGenericParameters)
            {
                var message = string.Format("Generic type {0} has unassigned type parameters.", BsonUtils.GetFriendlyTypeName(type));
                throw new ArgumentException(message, "type");
            }

            if (__serializerFactories.TryGetValue(type, out var serializerFactory))
            {
                return serializerFactory(GetSerializationDomain(serializerRegistry), serializerRegistry);
            }

            if (typeInfo.IsGenericType && !typeInfo.ContainsGenericParameters)
            {
                Type serializerTypeDefinition;
                if (__serializerTypeDefinitions.TryGetValue(type.GetGenericTypeDefinition(), out serializerTypeDefinition))
                {
                    return CreateGenericSerializer(serializerTypeDefinition, typeInfo.GetGenericArguments(), serializerRegistry);
                }
            }

            if (typeInfo.IsEnum)
            {
                return CreateGenericSerializer(typeof(EnumSerializer<>), new[] { type }, serializerRegistry);
            }

            return null;
        }
    }
}
