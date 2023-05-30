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
        AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic,
        /// <summary>
        /// Random algorithm.
        /// </summary>
        AEAD_AES_256_CBC_HMAC_SHA_512_Random,
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
        /// RangePreview algorithm.
        /// </summary>
        /// <remarks>
        /// The Range algorithm is experimental only. It is not intended for public use.
        /// To insert or query with an "Indexed" encrypted payload, use a MongoClient configured with AutoEncryptionOptions.
        /// AutoEncryptionOptions.BypassQueryAnalysis may be true. AutoEncryptionOptions.BypassAutoEncryption must be false.
        /// </remarks>
        RangePreview
    }
}
