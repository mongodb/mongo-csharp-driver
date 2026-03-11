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
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

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
    where TSource : struct
    where TTarget : struct
{
    private readonly BsonType _representation;
    private readonly IBsonSerializer<TSource> _sourceSerializer;

    public BsonType Representation => _representation;

    public NumericConversionSerializer(IBsonSerializer<TSource> sourceSerializer)
    {
        if (!typeof(TSource).IsNumericOrChar())
        {
            throw new ArgumentException($"{typeof(TSource).FullName} is not a numeric type supported by NumericConversionSerializer.", "TSource");
        }
        if (!typeof(TTarget).IsNumericOrChar())
        {
            throw new ArgumentException($"{typeof(TTarget).FullName} is not a numeric type supported by NumericConversionSerializer.", "TTarget");
        }
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
        return (TTarget)Convert.ChangeType(sourceValue, typeof(TTarget));
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TTarget value)
    {
        var sourceValue = Convert.ChangeType(value, typeof(TSource));
        _sourceSerializer.Serialize(context, args, sourceValue);
    }
}
