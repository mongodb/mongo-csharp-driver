/* Copyright 2020-present MongoDB Inc.
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

namespace MongoDB.Driver.Core.Authentication
{
    /// <summary>
    /// Represents the security context being used for authentication.
    /// </summary>
    public interface ISecurityContext : IDisposable
    {
        /// <summary>
        /// Negotiates the steps of an authentication handshake.
        /// </summary>
        /// <param name="challenge">Challenge received from the MongoDB server.</param>
        /// <returns>Response to send back to the MongoDB server.</returns>
        byte[] Next(byte[] challenge);

        /// <summary>
        /// Whether the security context has been initialized.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Decrypts ciphertext to plaintext.
        /// </summary>
        /// <param name="messageLength">Number of message bytes. Remaining bytes are the security trailer.</param>
        /// <param name="encryptedBytes">Ciphertext to decrypt.</param>
        /// <returns>Decrypted plaintext.</returns>
        byte[] DecryptMessage(int messageLength, byte[] encryptedBytes);

        /// <summary>
        /// Encrypts plaintext to ciphertext.
        /// </summary>
        /// <param name="plainTextBytes">Plaintext to encrypt.</param>
        /// <returns>Encrypted ciphertext.</returns>
        byte[] EncryptMessage(byte[] plainTextBytes);
    }
}
