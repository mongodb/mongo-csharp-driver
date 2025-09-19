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

using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver.Linq.Linq3Implementation.Misc;

internal static class IBsonSerializerExtensions
{
    public static bool CanBeAssignedTo(this IBsonSerializer sourceSerializer, IBsonSerializer targetSerializer)
    {
        if (sourceSerializer.Equals(targetSerializer))
        {
            return true;
        }

        if (sourceSerializer.ValueType.IsNumeric() &&
            targetSerializer.ValueType.IsNumeric() &&
            sourceSerializer.HasNumericRepresentation() &&
            targetSerializer.HasNumericRepresentation())
        {
            return true;
        }

        return false;
    }

    public static IBsonSerializer GetItemSerializer(this IBsonSerializer serializer)
        => ArraySerializerHelper.GetItemSerializer(serializer);

    public static bool HasNumericRepresentation(this IBsonSerializer serializer)
    {
        return
            serializer is IHasRepresentationSerializer hasRepresentationSerializer &&
            hasRepresentationSerializer.Representation.IsNumeric();
    }
}
