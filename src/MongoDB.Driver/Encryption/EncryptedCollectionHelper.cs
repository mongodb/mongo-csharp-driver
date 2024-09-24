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
using System.Linq;
using MongoDB.Bson;

namespace MongoDB.Driver.Encryption
{
    internal static class EncryptedCollectionHelper
    {
        public static BsonDocument AdditionalCreateIndexDocument { get; } = new BsonDocument("__safeContent__", 1);

        public static void EnsureCollectionsValid(IReadOnlyDictionary<string, BsonDocument> schemaMap, IReadOnlyDictionary<string, BsonDocument> encryptedFieldsMap)
        {
            if (schemaMap == null || encryptedFieldsMap == null || schemaMap.Count == 0 || encryptedFieldsMap.Count == 0)
            {
                return;
            }

            var mutualKeys = schemaMap.Keys.Where(k => encryptedFieldsMap.ContainsKey(k));
            if (mutualKeys.Any())
            {
                throw new ArgumentException($"SchemaMap and EncryptedFieldsMap cannot both contain the same collections: {string.Join(", ", mutualKeys)}.");
            }
        }

        public static string GetAdditionalCollectionName(BsonDocument encryptedFields, CollectionNamespace mainCollectionNamespace, HelperCollectionForEncryption helperCollection) =>
            helperCollection switch
            {
                HelperCollectionForEncryption.Esc => encryptedFields.GetValue("escCollection", defaultValue: $"enxcol_.{mainCollectionNamespace.CollectionName}.esc").ToString(),
                HelperCollectionForEncryption.Ecos => encryptedFields.GetValue("ecocCollection", defaultValue: $"enxcol_.{mainCollectionNamespace.CollectionName}.ecoc").ToString(),
                _ => throw new InvalidOperationException($"Not supported encryption helper collection {helperCollection}."),
            };

        public static bool TryGetEffectiveEncryptedFields(CollectionNamespace collectionNamespace, BsonDocument encryptedFields, IReadOnlyDictionary<string, BsonDocument> encryptedFieldsMap, out BsonDocument effectiveEncryptedFields)
        {
            if (encryptedFields != null)
            {
                effectiveEncryptedFields = encryptedFields;
                return true;
            }

            if (encryptedFieldsMap != null)
            {
                return encryptedFieldsMap.TryGetValue(collectionNamespace.ToString(), out effectiveEncryptedFields);
            }

            effectiveEncryptedFields = null;
            return false;
        }

        public static BsonDocument GetEffectiveEncryptedFields(CollectionNamespace collectionNamespace, BsonDocument encryptedFields, IReadOnlyDictionary<string, BsonDocument> encryptedFieldsMap)
        {
            if (TryGetEffectiveEncryptedFields(collectionNamespace, encryptedFields, encryptedFieldsMap, out var effectiveEncryptedFields))
            {
                return effectiveEncryptedFields;
            }
            else
            {
                return null;
            }
        }

        public enum HelperCollectionForEncryption
        {
            Esc,
            Ecos
        }
    }
}
