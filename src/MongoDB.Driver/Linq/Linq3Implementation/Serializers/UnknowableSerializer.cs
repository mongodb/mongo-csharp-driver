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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.Serializers;

internal static class UnknowableSerializer
{
    public static IBsonSerializer Create(Type valueType)
    {
        var serializerType = typeof(UnknowableSerializer<>).MakeGenericType(valueType);
        return (IBsonSerializer)Activator.CreateInstance(serializerType);
    }
}

internal interface IUnknowableSerializer
{
}

/// <summary>
/// A serializer assigned to nodes that need a serializer, but for which we have no way of knowing what serializer to use.
/// </summary>
/// <typeparam name="TValue">The value type.</typeparam>
/// <remarks>
/// This serializer differs from the IgnoreNodeSerializer because in this case we know that the node needs a serializer,
/// but we just have no way of knowing which serializer to use.
/// </remarks>
internal class UnknowableSerializer<TValue> : SerializerBase<TValue>, IUnknowableSerializer
{
}
