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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.Serializers;

internal static class NumericConversionSerializer
{
    public static IBsonSerializer Create(Type fromType, Type toType, IBsonSerializer fromSerializer)
    {
        var serializerType = typeof(NumericConversionSerializer<,>).MakeGenericType(fromType, toType);
        return (IBsonSerializer)Activator.CreateInstance(serializerType, fromSerializer);
    }
}

internal class NumericConversionSerializer<TFrom, TTo> : SerializerBase<TTo>, IHasRepresentationSerializer
{
    private readonly IBsonSerializer<TFrom> _fromSerializer;

    public BsonType Representation
    {
        get
        {
            if (_fromSerializer is not IHasRepresentationSerializer hasRepresentationSerializer)
            {
                throw new NotSupportedException($"Serializer class {_fromSerializer.GetType().Name} does not implement IHasRepresentationSerializer.");
            }

            return hasRepresentationSerializer.Representation;
        }
    }

    public NumericConversionSerializer(IBsonSerializer<TFrom> fromSerializer)
    {
        _fromSerializer = fromSerializer;
    }

    public override TTo Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var from = _fromSerializer.Deserialize(context);
        return (TTo)Convert(typeof(TFrom), typeof(TTo), from);
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TTo value)
    {
        var from = Convert(typeof(TTo), typeof(TFrom), value);
        _fromSerializer.Serialize(context, args);
    }

    private object Convert(Type from, Type to, object value)
    {
        return (Type.GetTypeCode(from), Type.GetTypeCode(to)) switch
        {
            (TypeCode.Decimal, TypeCode.Double) => (object)(double)(decimal)value,
            (TypeCode.Double, TypeCode.Decimal) => (object)(decimal)(double)value,
            (TypeCode.Int16, TypeCode.Int32) => (object)(int)(short)value,
            (TypeCode.Int16, TypeCode.Int64) => (object)(long)(short)value,
            (TypeCode.Int32, TypeCode.Int16) => (object)(short)(int)value,
            (TypeCode.Int32, TypeCode.Int64) => (object)(long)(int)value,
            (TypeCode.Int64, TypeCode.Int16) => (object)(short)(long)value,
            (TypeCode.Int64, TypeCode.Int32) => (object)(int)(long)value,
            _ => throw new NotSupportedException($"Cannot convert {from} to {to}"),
        };
    }
}
