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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace MongoDB.Bson
{
    //FP This could be moved somewhere else, and maybe reordered.
    internal static class InternalExtensions
    {
        #region IBsonIdProvider

        public static bool GetDocumentIdInternal(this IBsonIdProvider provider, object document, IBsonSerializationDomain serializationDomain, out object id,
            out Type idNominalType, out IIdGenerator idGenerator)
        {
            if (provider is IBsonIdProviderInternal internalProvider)
            {
                return internalProvider.GetDocumentId(document, serializationDomain, out id, out idNominalType, out idGenerator);
            }
            return provider.GetDocumentId(document, out id, out idNominalType, out idGenerator);
        }

        #endregion

        #region IDiscriminatorConvention

        public static Type GetActualTypeInternal(this IDiscriminatorConvention discriminatorConvention, IBsonReader bsonReader, Type nominalType, IBsonSerializationDomain serializationDomain)
        {
            if (discriminatorConvention is IDiscriminatorConventionInternal internalConvention)
            {
                return internalConvention.GetActualType(bsonReader, nominalType, serializationDomain);
            }
            return discriminatorConvention.GetActualType(bsonReader, nominalType);
        }

        public static BsonValue GetDiscriminatorInternal(this IDiscriminatorConvention discriminatorConvention, Type nominalType, Type actualType, IBsonSerializationDomain serializationDomain)
        {
            if (discriminatorConvention is IDiscriminatorConventionInternal internalConvention)
            {
                return internalConvention.GetDiscriminator(nominalType, actualType, serializationDomain);
            }
            return discriminatorConvention.GetDiscriminator(nominalType, actualType);
        }

        #endregion

        #region IScalarDiscriminatorConvention

        public static BsonValue[] GetDiscriminatorsForTypeAndSubTypesInternal(this IScalarDiscriminatorConvention discriminatorConvention, Type type, IBsonSerializationDomain serializationDomain)
        {
            if (discriminatorConvention is IScalarDiscriminatorConventionInternal internalConvention)
            {
                return internalConvention.GetDiscriminatorsForTypeAndSubTypes(type, serializationDomain);
            }
            return discriminatorConvention.GetDiscriminatorsForTypeAndSubTypes(type);
        }

        #endregion

        #region IMemberMapConvention

        public static void ApplyInternal(this IMemberMapConvention memberMapConvention, BsonMemberMap memberMap, IBsonSerializationDomain serializationDomain)
        {
            if (memberMapConvention is IMemberMapConventionInternal internalConvention)
            {
                internalConvention.Apply(memberMap, serializationDomain);
            }
            else
            {
                memberMapConvention.Apply(memberMap);
            }
        }

        #endregion

        #region IPostProcessingConvention

        public static void PostProcessInternal(this IPostProcessingConvention postProcessingConvention, BsonClassMap classMap, IBsonSerializationDomain serializationDomain)
        {
            if (postProcessingConvention is IPostProcessingConventionInternal internalConvention)
            {
                internalConvention.PostProcess(classMap, serializationDomain);
            }
            else
            {
                postProcessingConvention.PostProcess(classMap);
            }
        }

        #endregion
    }
}