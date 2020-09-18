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

namespace MongoDB.Driver.Linq3.Misc
{
    public static class DocumentSerializerHelper
    {
        public static FieldInfo GetFieldInfo(IBsonSerializer serializer, string memberName)
        {
            if (!(serializer is IBsonDocumentSerializer documentSerializer))
            {
                throw new NotSupportedException($"Serializer for {serializer.ValueType} must implement IBsonSerializer to be used with LINQ.");
            }

            if (!(documentSerializer.TryGetMemberSerializationInfo(memberName, out BsonSerializationInfo serializationInfo)))
            {
                throw new InvalidOperationException($"Serializer for {serializer.ValueType} does not have a member named {memberName}.");
            }

            return new FieldInfo(serializationInfo.ElementName, serializationInfo.Serializer);
        }
    }
}
