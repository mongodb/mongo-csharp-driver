/* Copyright 2016 MongoDB Inc.
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
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Support;

namespace MongoDB.Driver
{
    internal static class FieldValueSerializerHelper
    {
        public static IBsonSerializer GetSerializerForValueType(IBsonSerializer fieldSerializer, Type valueType)
        {
            var fieldType = fieldSerializer.ValueType;

            // these will normally be equal unless we've removed some Convert(s) that the compiler put in
            if (fieldType == valueType)
            {
                return fieldSerializer;
            }

            // serialize numeric values without converting them
            if (fieldType.IsNumeric() && valueType.IsNumeric())
            {
                var valueSerializer = BsonSerializer.SerializerRegistry.GetSerializer(valueType);
                if (HasStringRepresentation(fieldSerializer))
                {
                    valueSerializer = WithStringRepresentation(valueSerializer);
                }
                return valueSerializer;
            }

            var fieldTypeInfo = fieldType.GetTypeInfo();
            var fieldSerializerInterfaceType = typeof(IBsonSerializer<>).MakeGenericType(fieldType);
            var valueTypeInfo = valueType.GetTypeInfo();

            // synthesize a NullableSerializer using the field serializer
            if (valueType.IsNullable() && valueType.GetNullableUnderlyingType() == fieldType)
            {
                var nullableSerializerType = typeof(NullableSerializer<>).MakeGenericType(fieldType);
                var nullableSerializerConstructor = nullableSerializerType.GetTypeInfo().GetConstructor(new[] { fieldSerializerInterfaceType });
                return (IBsonSerializer)nullableSerializerConstructor.Invoke(new object[] { fieldSerializer });
            }

            // synthesize an EnumConvertingSerializer using the field serializer
            if (fieldTypeInfo.IsEnum)
            {
                var enumConvertingSerializerType = typeof(EnumConvertingSerializer<,>).MakeGenericType(valueType, fieldType);
                var enumConvertingSerializerConstructor = enumConvertingSerializerType.GetTypeInfo().GetConstructor(new[] { fieldSerializerInterfaceType });
                return (IBsonSerializer)enumConvertingSerializerConstructor.Invoke(new object[] { fieldSerializer });
            }

            // synthesize a NullableEnumConvertingSerializer using the field serializer
            if (fieldType.IsNullableEnum() && valueType.IsNullable())
            {
                var nonNullableFieldType = fieldType.GetNullableUnderlyingType();
                var nonNullableValueType = valueType.GetNullableUnderlyingType();
                var nonNullableFieldSerializer = ((IChildSerializerConfigurable)fieldSerializer).ChildSerializer;
                var nonNullableFieldSerializerInterfaceType = typeof(IBsonSerializer<>).MakeGenericType(nonNullableFieldType);
                var nullableEnumConvertingSerializerType = typeof(NullableEnumConvertingSerializer<,>).MakeGenericType(nonNullableValueType, nonNullableFieldType);
                var nullableEnumConvertingSerializerConstructor = nullableEnumConvertingSerializerType.GetTypeInfo().GetConstructor(new[] { nonNullableFieldSerializerInterfaceType });
                return (IBsonSerializer)nullableEnumConvertingSerializerConstructor.Invoke(new object[] { nonNullableFieldSerializer });
            }

            // synthesize an IEnumerableSerializer serializer using the item serializer from the field serializer
            Type fieldIEnumerableInterfaceType;
            Type valueIEnumerableInterfaceType;
            Type itemType;
            if (
                (fieldIEnumerableInterfaceType = fieldType.FindIEnumerable()) != null &&
                (valueIEnumerableInterfaceType = valueType.FindIEnumerable()) != null &&
                (itemType = fieldIEnumerableInterfaceType.GetSequenceElementType()) == valueIEnumerableInterfaceType.GetSequenceElementType() &&
                fieldSerializer is IChildSerializerConfigurable)
            {
                var itemSerializer = ((IChildSerializerConfigurable)fieldSerializer).ChildSerializer;
                var itemSerializerInterfaceType = typeof(IBsonSerializer<>).MakeGenericType(itemType);
                var ienumerableSerializerType = typeof(IEnumerableSerializer<>).MakeGenericType(itemType);
                var ienumerableSerializerConstructor = ienumerableSerializerType.GetTypeInfo().GetConstructor(new[] { itemSerializerInterfaceType });
                return (IBsonSerializer)ienumerableSerializerConstructor.Invoke(new object[] { itemSerializer });
            }

            // otherwise assume that the value can be cast to the right type for the field serializer
            var castingSerializerType = typeof(CastingSerializer<,>).MakeGenericType(valueType, fieldType);
            var castingSerializerConstructor = castingSerializerType.GetTypeInfo().GetConstructor(new[] { fieldSerializerInterfaceType });
            return (IBsonSerializer)castingSerializerConstructor.Invoke(new object[] { fieldSerializer });
        }

        public static IBsonSerializer<TValue> GetSerializerForValueType<TField, TValue>(IBsonSerializer<TField> fieldSerializer)
        {
            return (IBsonSerializer<TValue>)GetSerializerForValueType(fieldSerializer, typeof(TValue));
        }

        // private static methods
        private static bool HasStringRepresentation(IBsonSerializer serializer)
        {
            var configurableSerializer = serializer as IRepresentationConfigurable;
            if (configurableSerializer != null)
            {
                return configurableSerializer.Representation == BsonType.String;
            }
            else
            {
                return false;
            }
        }

        private static IBsonSerializer WithStringRepresentation(IBsonSerializer serializer)
        {
            var configurableSerializer = serializer as IRepresentationConfigurable;
            if (configurableSerializer != null)
            {
                return configurableSerializer.WithRepresentation(BsonType.String);
            }
            else
            {
                return serializer;
            }
        }

        // nested types
        private class CastingSerializer<TFrom, TTo> : SerializerBase<TFrom>
        {
            private readonly IBsonSerializer<TTo> _serializer;

            public CastingSerializer(IBsonSerializer<TTo> serializer)
            {
                _serializer = serializer;
            }

            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TFrom value)
            {
                _serializer.Serialize(context, args, (TTo)(object)value);
            }
        }

        private class EnumConvertingSerializer<TFrom, TTo> : SerializerBase<TFrom>
        {
            private readonly IBsonSerializer<TTo> _serializer;

            public EnumConvertingSerializer(IBsonSerializer<TTo> serializer)
            {
                _serializer = serializer;
            }

            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TFrom value)
            {
                _serializer.Serialize(context, args, (TTo)Enum.ToObject(typeof(TTo), (object)value));
            }
        }

        private class IEnumerableSerializer<TItem> : SerializerBase<IEnumerable<TItem>>
        {
            private readonly IBsonSerializer<TItem> _itemSerializer;

            public IEnumerableSerializer(IBsonSerializer<TItem> itemSerializer)
            {
                _itemSerializer = itemSerializer;
            }

            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, IEnumerable<TItem> value)
            {
                var bsonWriter = context.Writer;
                if (value == null)
                {
                    bsonWriter.WriteNull();
                }
                else
                {
                    bsonWriter.WriteStartArray();
                    foreach (var item in value)
                    {
                        _itemSerializer.Serialize(context, item);
                    }
                    bsonWriter.WriteEndArray();
                }
            }
        }

        private class NullableEnumConvertingSerializer<TFrom, TTo> : SerializerBase<Nullable<TFrom>> where TFrom : struct where TTo : struct
        {
            private readonly IBsonSerializer<TTo> _nonNullableEnumSerializer;

            public NullableEnumConvertingSerializer(IBsonSerializer<TTo> nonNullableEnumSerializer)
            {
                _nonNullableEnumSerializer = nonNullableEnumSerializer;
            }

            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Nullable<TFrom> value)
            {
                if (value == null)
                {
                    context.Writer.WriteNull();
                }
                else
                {
                    _nonNullableEnumSerializer.Serialize(context, args, (TTo)Enum.ToObject(typeof(TTo), (object)value.Value));
                }
            }
        }
    }
}
