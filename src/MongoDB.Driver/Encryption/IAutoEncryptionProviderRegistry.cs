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

namespace MongoDB.Driver.Encryption
{
    /// <summary>
    /// AutoEncryption Provider Registry.
    /// </summary>
    public interface IAutoEncryptionProviderRegistry
    {
        /// <summary>
        /// Registers new AutoEncryption Provider.
        /// </summary>
        /// <param name="factory">Factory method.</param>
        void Register(Func<IMongoClient, AutoEncryptionOptions, IAutoEncryptionLibMongoCryptController> factory);

        /// <summary>
        /// Creates an IAutoCryptClientController.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="autoEncryptionOptions">The AutoEncryptionOptions.</param>
        /// <returns>The IAutoCryptClientController.</returns>
        public IAutoEncryptionLibMongoCryptController CreateAutoCryptClientController(IMongoClient client, AutoEncryptionOptions autoEncryptionOptions);
    }
}
