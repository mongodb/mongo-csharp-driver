/* Copyright 2019-present MongoDB Inc.
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

using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver.Core.WireProtocol
{
    /// <summary>
    /// Interface for decrypting fields in a binary document.
    /// </summary>
    public interface IBinaryDocumentFieldDecryptor
    {
        /// <summary>
        /// Decrypts the fields.
        /// </summary>
        /// <param name="encryptedDocumentBytes">The encrypted document bytes.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An unencrypted document.</returns>
        byte[] DecryptFields(byte[] encryptedDocumentBytes, CancellationToken cancellationToken);

        /// <summary>
        /// Decrypts the fields asynchronously.
        /// </summary>
        /// <param name="encryptedDocumentBytes">The encrypted document bytes.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An unencrypted document.</returns>
        Task<byte[]> DecryptFieldsAsync(byte[] encryptedDocumentBytes, CancellationToken cancellationToken);
    }
}
