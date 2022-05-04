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
        public static void EnsureCollectionsValid(IReadOnlyDictionary<string, BsonDocument> schemaMap, IReadOnlyDictionary<string, BsonDocument> encryptedFieldsMap)
        {
            if (schemaMap == null || encryptedFieldsMap == null || schemaMap.Count == 0 || encryptedFieldsMap.Count == 0)
            {
                return;
            }

            var joined = schemaMap.Join(encryptedFieldsMap, o => o.Key, i => i.Key, (kv, i) => new { KeyValue = kv, Inner = i }).ToList();
            if (joined.Count > 0)
            {
                throw new ArgumentException($"SchemaMap and EncryptedFieldsMap cannot both contain the same collections: {string.Join(", ", joined.Select(c => c.KeyValue.Key))}.");
            }
        }

        public static bool TryGetEncryptedFieldsFromAutoEncryptionOptions(CollectionNamespace collectionNamespace, AutoEncryptionOptions autoEncryptionOptions, out BsonDocument encryptedFields)
        {
            var encryptedFieldsMap = autoEncryptionOptions?.EncryptedFieldsMap;
            if (encryptedFieldsMap != null)
            {
                if (encryptedFieldsMap.TryGetValue(collectionNamespace.ToString(), out encryptedFields))
                {
                    return true;                   
                }
            }

            encryptedFields = null;
            return false;
        }

        public static bool TryGetEffectiveEncryptedFields(CollectionNamespace collectionNamespace, BsonDocument encryptedFields, AutoEncryptionOptions autoEncryptionOptions, out BsonDocument effectiveEncryptedFields)
        {
            effectiveEncryptedFields = encryptedFields;

            if (encryptedFields == null)
            {
                var encryptedFieldsMap = autoEncryptionOptions?.EncryptedFieldsMap;
                if (encryptedFieldsMap != null)
                {
                    return encryptedFieldsMap.TryGetValue(collectionNamespace.ToString(), out effectiveEncryptedFields);
                }

                return false;
            }

            return true;
        }
    }
}
