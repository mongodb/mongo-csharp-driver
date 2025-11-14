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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.Serializers;

internal static class HashSetSerializer
{
    public static IBsonSerializer Create(IBsonSerializer itemSerializer)
    {
        var serializerType = typeof(HashSetSerializer<>).MakeGenericType(itemSerializer.ValueType);
        return (IBsonSerializer)Activator.CreateInstance(serializerType, itemSerializer);
    }
}

internal class HashSetSerializer<T> : EnumerableInterfaceImplementerSerializerBase<HashSet<T>, T>
{
    public HashSetSerializer(IBsonSerializer<T> itemSerializer)
        : base(itemSerializer)
    {
    }

    protected override object CreateAccumulator() => new HashSet<T>();

    protected override HashSet<T> FinalizeResult(object accumulator) => (HashSet<T>)accumulator;
}
