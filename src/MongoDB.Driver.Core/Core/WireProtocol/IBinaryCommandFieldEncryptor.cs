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
    public interface IBinaryCommandFieldEncryptor
    {
        /// <summary>
        /// Encrypts the fields.
        /// </summary>
        /// <param name="databaseName">The database name.</param>
        /// <param name="unencryptedCommandBytes">The unencrypted command bytes.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An encrypted document.</returns>
        byte[] EncryptFields(string databaseName, byte[] unencryptedCommandBytes, CancellationToken cancellationToken);

        /// <summary>
        /// Encrypts the fields asynchronously.
        /// </summary>
        /// <param name="databaseName">The database name.</param>
        /// <param name="unencryptedCommandBytes">The unencrypted command bytes.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An encrypted document.</returns>
        Task<byte[]> EncryptFieldsAsync(string databaseName, byte[] unencryptedCommandBytes, CancellationToken cancellationToken);
    }
}
