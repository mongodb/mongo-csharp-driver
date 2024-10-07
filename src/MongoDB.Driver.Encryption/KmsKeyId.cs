/*
 * Copyright 2020–present MongoDB, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections.Generic;
using System.Linq;

namespace MongoDB.Driver.Encryption
{
    /// <summary>
    /// Represent a kms key.
    /// </summary>
    internal class KmsKeyId
    {
        private readonly IReadOnlyList<byte[]> _alternateKeyNameBytes;
        private readonly byte[] _dataKeyOptionsBytes;
        private readonly byte[] _keyMaterialBytes;

        /// <summary>
        /// Creates an <see cref="KmsKeyId"/> class.
        /// </summary>
        /// <param name="dataKeyOptionsBytes">The byte representation of dataOptions bson document.</param>
        /// <param name="alternateKeyNameBytes">The byte representation of alternate keyName.</param>
        /// <param name="keyMaterialBytes">The byte representation of key material.</param>
        public KmsKeyId(
            byte[] dataKeyOptionsBytes,
            IEnumerable<byte[]> alternateKeyNameBytes = null,
            byte[] keyMaterialBytes = null)
        {
            _dataKeyOptionsBytes = dataKeyOptionsBytes;
            _alternateKeyNameBytes = (alternateKeyNameBytes ?? Enumerable.Empty<byte[]>()).ToList().AsReadOnly();
            _keyMaterialBytes = keyMaterialBytes;
        }

        /// <summary>
        /// Alternate key name bytes.
        /// </summary>
        public IReadOnlyList<byte[]> AlternateKeyNameBytes => _alternateKeyNameBytes;

        /// <summary>
        /// Data key options bytes.
        /// </summary>
        public byte[] DataKeyOptionsBytes => _dataKeyOptionsBytes;

        /// <summary>
        /// Key material bytes.
        /// </summary>
        public byte[] KeyMaterialBytes => _keyMaterialBytes;

        // internal methods
        internal void SetCredentials(ContextSafeHandle context, Status status)
        {
            if (_dataKeyOptionsBytes != null)
            {
                PinnedBinary.RunAsPinnedBinary(context, _dataKeyOptionsBytes, status, (h, pb) => Library.mongocrypt_ctx_setopt_key_encryption_key(h, pb));
            }

            SetAlternateKeyNamesIfConfigured(context, status);

            SetKeyMaterialIfConfigured(context, status);
        }

        // private methods
        private void SetAlternateKeyNamesIfConfigured(ContextSafeHandle context, Status status)
        {
            foreach (var alternateKeyNameBytes in _alternateKeyNameBytes)
            {
                PinnedBinary.RunAsPinnedBinary(context, alternateKeyNameBytes, status, (h, pb) => Library.mongocrypt_ctx_setopt_key_alt_name(h, pb));
            }
        }

        private void SetKeyMaterialIfConfigured(ContextSafeHandle context, Status status)
        {
            if (_keyMaterialBytes != null)
            {
                PinnedBinary.RunAsPinnedBinary(context, _keyMaterialBytes, status, (h, pb) => Library.mongocrypt_ctx_setopt_key_material(h, pb));
            }
        }
    }
}
