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

namespace MongoDB.Driver.Linq.Linq3Implementation.Misc
{
    internal static class DocumentSerializerHelper
    {
        public static bool AreMembersRepresentedAsFields(IBsonSerializer serializer, out IBsonDocumentSerializer documentSerializer)
        {
            if (serializer is IDiscriminatedInterfaceSerializer discriminatedInterfaceSerializer)
            {
                return AreMembersRepresentedAsFields(discriminatedInterfaceSerializer.InterfaceSerializer, out documentSerializer);
            }

            if (serializer is IDowncastingSerializer downcastingSerializer)
            {
                return AreMembersRepresentedAsFields(downcastingSerializer.DerivedSerializer, out documentSerializer);
            }

            if (serializer is IImpliedImplementationInterfaceSerializer impliedImplementationSerializer)
            {
                return AreMembersRepresentedAsFields(impliedImplementationSerializer.ImplementationSerializer, out documentSerializer);
            }

            if (serializer is IBsonDictionarySerializer)
            {
                documentSerializer = null;
                return false;
            }

            if (serializer is IKeyValuePairSerializer keyValuePairSerializer)
            {
                if (keyValuePairSerializer.Representation == BsonType.Document)
                {
                    documentSerializer = (IBsonDocumentSerializer)keyValuePairSerializer;
                    return true;
                }
                else
                {
                    documentSerializer = null;
                    return false;
                }
            }

            // for backward compatibility assume that any remaining implementers of IBsonDocumentSerializer represent members as fields
            if (serializer is IBsonDocumentSerializer tempDocumentSerializer)
            {
                documentSerializer = tempDocumentSerializer;
                return true;
            }

            documentSerializer = null;
            return false;
        }

        public static MemberSerializationInfo GetMemberSerializationInfo(IBsonSerializer serializer, string memberName)
        {
            if (!AreMembersRepresentedAsFields(serializer, out var documentSerializer))
            {
                throw new NotSupportedException($"Serializer for {serializer.ValueType} does not represent members as fields.");
            }

            if (!(documentSerializer.TryGetMemberSerializationInfo(memberName, out BsonSerializationInfo serializationInfo)))
            {
                throw new InvalidOperationException($"Serializer for {serializer.ValueType} does not have a member named {memberName}.");
            }

            if (serializationInfo.ElementPath == null)
            {
                return new MemberSerializationInfo(serializationInfo.ElementName, serializationInfo.Serializer);
            }
            else
            {
                return new MemberSerializationInfo(serializationInfo.ElementPath, serializationInfo.Serializer);
            }
        }
    }
}
