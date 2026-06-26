/* Copyright 2021-present MongoDB Inc.
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

namespace MongoDB.Driver.Encryption
{
    /// <summary>
    /// Represents an encryption algorithm.
    /// </summary>
    public enum EncryptionAlgorithm
    {
        /// <summary>
        /// Deterministic algorithm.
        /// </summary>
#pragma warning disable CA1707
        AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic,
#pragma warning restore CA1707
        /// <summary>
        /// Random algorithm.
        /// </summary>
#pragma warning disable CA1707
        AEAD_AES_256_CBC_HMAC_SHA_512_Random,
#pragma warning restore CA1707
        /// <summary>
        /// Indexed algorithm.
        /// </summary>
        /// <remarks>
        /// To insert or query with an "Indexed" encrypted payload, use a MongoClient configured with AutoEncryptionOptions.
        /// AutoEncryptionOptions.BypassQueryAnalysis may be true. AutoEncryptionOptions.BypassAutoEncryption must be false.
        /// </remarks>
        Indexed,

        /// <summary>
        /// Unindexed algorithm.
        /// </summary>
        Unindexed,

        /// <summary>
        /// Range algorithm.
        /// </summary>
        /// <remarks>
        /// To insert or query with a "Range" encrypted payload, use a MongoClient configured with AutoEncryptionOptions.
        /// AutoEncryptionOptions.BypassQueryAnalysis may be true. AutoEncryptionOptions.BypassAutoEncryption must be false.
        /// </remarks>
        Range,

        /// <summary>
        /// TextPreview algorithm.
        /// </summary>
        /// <remarks>
        /// This is a deprecated alias for <see cref="String"/> and is translated to "String". Use <see cref="String"/> instead.
        /// </remarks>
        [Obsolete("Use String instead.")]
        TextPreview,

        /// <summary>
        /// String algorithm.
        /// </summary>
        /// <remarks>
        /// To insert or query with a "String" encrypted payload, use a MongoClient configured with AutoEncryptionOptions.
        /// AutoEncryptionOptions.BypassQueryAnalysis may be true. AutoEncryptionOptions.BypassAutoEncryption must be false.
        /// </remarks>
#pragma warning disable CA1720
        String
#pragma warning restore CA1720
    }
}
