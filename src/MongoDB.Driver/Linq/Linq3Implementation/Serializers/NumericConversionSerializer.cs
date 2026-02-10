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
    public static IBsonSerializer Create(Type sourceType, Type targetType, IBsonSerializer sourceSerializer)
    {
        var serializerType = typeof(NumericConversionSerializer<,>).MakeGenericType(sourceType, targetType);
        return (IBsonSerializer)Activator.CreateInstance(serializerType, sourceSerializer);
    }
}

internal class NumericConversionSerializer<TSource, TTarget> : SerializerBase<TTarget>, IHasRepresentationSerializer
{
    private readonly BsonType _representation;
    private readonly IBsonSerializer<TSource> _sourceSerializer;

    public BsonType Representation => _representation;

    public NumericConversionSerializer(IBsonSerializer<TSource> sourceSerializer)
    {
        if (sourceSerializer is not IHasRepresentationSerializer hasRepresentationSerializer)
        {
            throw new NotSupportedException($"Serializer class {sourceSerializer.GetType().Name} does not implement IHasRepresentationSerializer.");
        }

        _sourceSerializer = sourceSerializer;
        _representation = hasRepresentationSerializer.Representation;
    }

    public override TTarget Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var sourceValue = _sourceSerializer.Deserialize(context);
        return (TTarget)Convert(typeof(TSource), typeof(TTarget), sourceValue);
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TTarget value)
    {
        var sourceValue = Convert(typeof(TTarget), typeof(TSource), value);
        _sourceSerializer.Serialize(context, args, sourceValue);
    }

    private object Convert(Type sourceType, Type targetType, object value)
    {
        return (Type.GetTypeCode(sourceType), Type.GetTypeCode(targetType)) switch
        {
            (TypeCode.Decimal, TypeCode.Double) => (object)(double)(decimal)value,
            (TypeCode.Double, TypeCode.Decimal) => (object)(decimal)(double)value,
            (TypeCode.Int16, TypeCode.Int32) => (object)(int)(short)value,
            (TypeCode.Int16, TypeCode.Int64) => (object)(long)(short)value,
            (TypeCode.Int32, TypeCode.Int16) => (object)(short)(int)value,
            (TypeCode.Int32, TypeCode.Int64) => (object)(long)(int)value,
            (TypeCode.Int64, TypeCode.Int16) => (object)(short)(long)value,
            (TypeCode.Int64, TypeCode.Int32) => (object)(int)(long)value,
            _ => throw new NotSupportedException($"Cannot convert {sourceType} to {targetType}."),
        };
    }
}
